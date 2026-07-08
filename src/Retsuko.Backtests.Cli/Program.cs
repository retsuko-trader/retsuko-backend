using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Binance.Net.Enums;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Retsuko;
using Retsuko.Core;
using Retsuko.Migrations;

using StrategyCatalog = Retsuko.Strategies.Services.StrategyLoader;
using StrategyImplementation = Retsuko.Strategies.Services.IStrategy;

MyLogger.Logger = NullLogger.Instance;
SetDevelopmentWorkingDirectory();

var jsonOptions = new JsonSerializerOptions {
  PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  PropertyNameCaseInsensitive = true,
  WriteIndented = true,
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

return await Cli.Run(args, jsonOptions);

static void SetDevelopmentWorkingDirectory() {
  var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
  var projectFile = Path.Combine(projectDirectory, "Retsuko.Strategies.Cli.csproj");

  if (File.Exists(projectFile)) {
    Directory.SetCurrentDirectory(projectDirectory);
  }
}

public record SingleBacktestRunRequest(
  BacktestConfig config,
  bool hideTrades = false,
  bool debug = false
);

public static class Cli {
  private const int BatchSize = 200;

  public static async Task<int> Run(string[] args, JsonSerializerOptions jsonOptions) {
    var rootCommand = BuildRootCommand(jsonOptions);
    return await rootCommand.Parse(args.Length == 0 ? ["--help"] : args).InvokeAsync();
  }

  private static RootCommand BuildRootCommand(JsonSerializerOptions jsonOptions) {
    var rootCommand = new RootCommand("Run Retsuko strategy backtests.");
    rootCommand.Subcommands.Add(BuildListCommand(jsonOptions));
    rootCommand.Subcommands.Add(BuildConfigCommand(jsonOptions));
    rootCommand.Subcommands.Add(BuildRunCommand(jsonOptions));
    rootCommand.Subcommands.Add(BuildBulkCommand(jsonOptions));
    return rootCommand;
  }

  private static Command BuildListCommand(JsonSerializerOptions jsonOptions) {
    var command = new Command("list", "Write available strategy names and default configs.");

    command.SetAction(parseResult => Execute(() => {
      var strategies = StrategyCatalog.GetStrategyEntries();
      WriteJson(strategies, null, jsonOptions);
      return Task.FromResult(0);
    }));

    return command;
  }

  private static Command BuildConfigCommand(JsonSerializerOptions jsonOptions) {
    var strategyNameArgument = new Argument<string>("strategy-name") {
      Description = "Strategy name from the list command."
    };
    var outputOption = CreateOutputOption();

    var command = new Command("config", "Write a starter single-backtest config.");
    command.Arguments.Add(strategyNameArgument);
    command.Options.Add(outputOption);

    command.SetAction(parseResult => Execute(() => {
      var strategyName = parseResult.GetRequiredValue(strategyNameArgument);
      var output = parseResult.GetValue(outputOption);
      return Task.FromResult(WriteDefaultConfig(strategyName, output, jsonOptions));
    }));

    return command;
  }

  private static Command BuildRunCommand(JsonSerializerOptions jsonOptions) {
    var configPathArgument = new Argument<string>("config-path") {
      Description = "Path to BacktestConfig JSON or SingleBacktestRunRequest JSON."
    };
    var debugOption = new Option<bool>("--debug") {
      Description = "Collect strategy debug indicators."
    };
    var hideTradesOption = new Option<bool>("--hide-trades") {
      Description = "Omit trades from the JSON report."
    };
    var outputOption = CreateOutputOption();

    var command = new Command("run", "Run one backtest and write the report as JSON.");
    command.Arguments.Add(configPathArgument);
    command.Options.Add(debugOption);
    command.Options.Add(hideTradesOption);
    command.Options.Add(outputOption);

    command.SetAction((parseResult, cancellationToken) => Execute(async () => {
      var configPath = parseResult.GetRequiredValue(configPathArgument);
      var debug = parseResult.GetValue(debugOption);
      var hideTrades = parseResult.GetValue(hideTradesOption);
      var output = parseResult.GetValue(outputOption);

      return await RunSingle(configPath, debug, hideTrades, output, jsonOptions);
    }));

    return command;
  }

  private static Command BuildBulkCommand(JsonSerializerOptions jsonOptions) {
    var configPathArgument = new Argument<string>("config-path") {
      Description = "Path to BulkBacktestConfig JSON."
    };
    var debugOption = new Option<bool>("--debug") {
      Description = "Collect strategy debug indicators while running."
    };
    var outputOption = CreateOutputOption();

    var command = new Command("bulk", "Run a bulk backtest and store the results.");
    command.Arguments.Add(configPathArgument);
    command.Options.Add(debugOption);
    command.Options.Add(outputOption);

    command.SetAction((parseResult, cancellationToken) => Execute(async () => {
      var configPath = parseResult.GetRequiredValue(configPathArgument);
      var debug = parseResult.GetValue(debugOption);
      var output = parseResult.GetValue(outputOption);

      return await RunBulk(configPath, debug, output, jsonOptions);
    }));

    return command;
  }

  private static Option<string?> CreateOutputOption() {
    return new Option<string?>("--output", "-o") {
      Description = "Write JSON to a file instead of stdout."
    };
  }

  private static async Task<int> Execute(Func<Task<int>> action) {
    try {
      return await action();
    } catch (Exception ex) {
      Console.Error.WriteLine(ex.Message);
      return 1;
    }
  }

  private static int WriteDefaultConfig(string strategyName, string? output, JsonSerializerOptions jsonOptions) {
    var config = StrategyCatalog.GetDefaultConfig(strategyName);

    if (config == null) {
      Console.Error.WriteLine($"Strategy '{strategyName}' was not found.");
      return 1;
    }

    var request = new SingleBacktestRunRequest(new BacktestConfig(
      new DatasetConfig(Market.futures, 0, KlineInterval.EightHour, DateTime.Parse("2021-01-01"), DateTime.Parse("2021-01-31")),
      new StrategyConfig(strategyName, config),
      new PaperBrokerConfig(1000, 0.001, false, false)
    ));

    WriteJson(request, output, jsonOptions);
    return 0;
  }

  private static async Task<int> RunSingle(string configPath, bool debug, bool hideTrades, string? output, JsonSerializerOptions jsonOptions) {
    var request = ReadSingleBacktestRequest(configPath, jsonOptions);

    request = request with {
      debug = request.debug || debug,
      hideTrades = request.hideTrades || hideTrades,
    };

    var report = await RunBacktest(request.config, request.debug);
    if (request.hideTrades) {
      report = report with { trades = [] };
    }

    WriteJson(report, output, jsonOptions);
    return 0;
  }

  private static async Task<int> RunBulk(string configPath, bool debug, string? output, JsonSerializerOptions jsonOptions) {
    var config = ReadJsonFile<BulkBacktestConfig>(configPath, jsonOptions);

    await Migrations.CreateBacktest();
    var run = BacktestRun.Create(config);
    run.Insert();

    var singleCount = 0;
    foreach (var dataset in config.datasets) {
      foreach (var strategy in config.strategies) {
        var singleConfig = new BacktestConfig(dataset, strategy, config.broker);
        var report = await RunBacktest(singleConfig, debug);
        var single = BacktestSingle.Create(run.id, singleConfig, report.metrics);
        single.Insert();

        var trades = report.trades.Select(trade => BacktestTrade.Create(single.id, trade));
        BacktestTrade.InsertBulk(trades);
        singleCount++;

        Console.Error.WriteLine($"Completed {strategy.name} on symbolId={dataset.symbolId}, interval={dataset.interval}.");
      }
    }

    await run.UpdateEnd();

    WriteJson(new {
      run,
      singles = singleCount,
    }, output, jsonOptions);
    return 0;
  }

  private static async Task<TraderReport> RunBacktest(BacktestConfig config, bool debug) {
    using var loader = new BacktestCandleLoader(config.dataset);
    var backtester = new LocalBacktester(config);

    await backtester.Init(debug);
    await backtester.Preload(loader);

    await foreach (var chunk in loader.BatchLoad(BatchSize)) {
      await backtester.TickBulk(chunk);
    }

    await backtester.FinalizeMetrics();
    return await backtester.GetReport();
  }

  private static SingleBacktestRunRequest ReadSingleBacktestRequest(string path, JsonSerializerOptions jsonOptions) {
    var json = File.ReadAllText(path);
    using var document = JsonDocument.Parse(json);

    if (document.RootElement.TryGetProperty("config", out _)) {
      return JsonSerializer.Deserialize<SingleBacktestRunRequest>(json, jsonOptions)
        ?? throw new InvalidOperationException($"Failed to parse single backtest request from '{path}'.");
    }

    var config = JsonSerializer.Deserialize<BacktestConfig>(json, jsonOptions);
    return new SingleBacktestRunRequest(config);
  }

  private static T ReadJsonFile<T>(string path, JsonSerializerOptions jsonOptions) {
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<T>(json, jsonOptions)
      ?? throw new InvalidOperationException($"Failed to parse {typeof(T).Name} from '{path}'.");
  }

  private static void WriteJson<T>(T value, string? output, JsonSerializerOptions jsonOptions) {
    var json = JsonSerializer.Serialize(value, jsonOptions);
    if (output == null) {
      Console.WriteLine(json);
      return;
    }

    File.WriteAllText(output, json);
  }
}

public class LocalBacktester: Trader<LocalStrategyRunner> {
  private readonly BacktestConfig config;

  public LocalBacktester(BacktestConfig config): base(
    LocalStrategyRunner.Create(config.strategy.name, config.strategy.config),
    new PaperBroker(config.broker)
  ) {
    this.config = config;
  }

  public override async Task Preload(ICandleLoader loader) {
    if (!await loader.Init()) {
      throw new InvalidOperationException($"Dataset symbolId={config.dataset.symbolId} was not found.");
    }

    foreach (var candle in await loader.Preload()) {
      await strategy.Preload(candle);
    }
  }

  public async Task TickBulk(IEnumerable<Candle> candles) {
    foreach (var candle in candles) {
      await Tick(candle);
    }
  }

  public async Task<TraderReport> GetReport() {
    await ValueTask.CompletedTask;
    return new TraderReport(
      config,
      trades,
      metrics,
      await strategy.GetDebugIndicators()
    );
  }
}

public class LocalStrategyRunner: IStrategy {
  private readonly StrategyImplementation strategy;
  private readonly Queue<StrategyUpdateResult> outputs = new();
  private readonly Dictionary<(string name, int index), List<DebugIndicatorEntry>> debugIndicators = [];
  private bool debug;

  private LocalStrategyRunner(StrategyImplementation strategy) {
    this.strategy = strategy;
  }

  public static LocalStrategyRunner Create(string name, string config) {
    var strategy = StrategyCatalog.CreateStrategy(name, config);
    if (strategy == null) {
      throw new InvalidOperationException($"Strategy '{name}' was not found.");
    }

    return new LocalStrategyRunner(strategy);
  }

  public async Task Init(string? state = null, bool debug = false) {
    this.debug = debug;

    if (!string.IsNullOrWhiteSpace(state)) {
      strategy.Deserialize(state);
    }

    await ValueTask.CompletedTask;
  }

  public async Task Preload(Candle candle) {
    await strategy.Preload(ToGrpcCandle(candle));
  }

  public async Task Update(Candle candle) {
    var grpcCandle = ToGrpcCandle(candle);
    var signal = await strategy.Update(grpcCandle);

    outputs.Enqueue(new StrategyUpdateResult(candle, signal));

    if (!debug) {
      return;
    }

    var indicators = await strategy.Debug(grpcCandle);
    foreach (var indicator in indicators) {
      var key = (indicator.name, indicator.index);
      if (!debugIndicators.TryGetValue(key, out var values)) {
        values = [];
        debugIndicators[key] = values;
      }

      values.Add(new DebugIndicatorEntry(
        new DateTimeOffset(candle.ts.ToUniversalTime()).ToUnixTimeMilliseconds(),
        indicator.value
      ));
    }
  }

  public async Task<StrategyUpdateResult?> GetUpdateResult() {
    await ValueTask.CompletedTask;
    return outputs.Count == 0 ? null : outputs.Dequeue();
  }

  public async Task FinishInputs() {
    await ValueTask.CompletedTask;
  }

  public async Task<string> GetFinalState() {
    await ValueTask.CompletedTask;
    return strategy.Serialize();
  }

  public async Task<DebugIndicator[]> GetDebugIndicators() {
    await ValueTask.CompletedTask;
    return debugIndicators.Select(entry => new DebugIndicator(
      entry.Key.name,
      entry.Key.index,
      entry.Value.ToArray()
    )).ToArray();
  }

  private static GCandle ToGrpcCandle(Candle candle) {
    return new GCandle {
      Market = (int)candle.market,
      SymbolId = candle.symbolId,
      Interval = (int)candle.interval,
      Ts = Timestamp.FromDateTime(candle.ts.ToUniversalTime()),
      Open = candle.open,
      High = candle.high,
      Low = candle.low,
      Close = candle.close,
      Volume = candle.volume,
    };
  }
}
