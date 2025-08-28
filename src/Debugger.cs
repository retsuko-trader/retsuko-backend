using System.IO.Compression;
using System.Text.Json;
using System.Xml;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Retsuko;
using Retsuko.Core;
using Retsuko.Core.Indicators;
using Retsuko.Dtos;

public static class Debugger {
  public static void DebugATR() {
    var atr = Indicators.ATR(5);
    var update = (double high, double low, double close) => {
      atr.Update(new Candle(Market.futures, -1, Binance.Net.Enums.KlineInterval.OneMinute, DateTime.Now, 0, high, low, close, 0));
      return atr;
    };

    (double, double, double)[] data = [
      (82.15, 81.29, 81.59),
      (81.89, 80.64, 81.06),
      (83.03, 81.31, 82.87),
      (83.30, 82.65, 83.00),
      (83.85, 83.07, 83.61),
      (83.90, 83.11, 83.15),
      (83.33, 82.49, 82.84),
      (84.30, 82.30, 83.99),
      (84.84, 84.15, 84.55),
      (85.00, 84.11, 84.36),
      (85.90, 84.03, 85.53),
      (86.58, 85.39, 86.54),
      (86.98, 85.76, 86.89),
      (88.00, 87.17, 87.77),
      (87.87, 87.01, 87.29),
    ];
    int i =0;
    foreach (var (high, low, close) in data) {
      update(high, low, close);
      Console.WriteLine($"{atr.Ready} {high} {atr.Value}");
      i += 1;
    }
    return;
  }

  public static void DebugSMA() {
    var sma = Indicators.SMA(5);

    double[] data = [
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9,
      10,
    ];

    foreach (var d in data) {
      sma.Update(new Candle(Market.futures, -1, Binance.Net.Enums.KlineInterval.OneMinute, DateTime.Now, 0, 0, 0, d, 0));
      Console.WriteLine($"{sma.Ready} {d} {sma.Value}");
    }
  }

  public static async Task DebugBacktester() {
    var config = new BacktestConfig(
      new DatasetConfig(Market.futures, 0, Binance.Net.Enums.KlineInterval.EightHour, DateTime.Parse("2021-01-01"), DateTime.Parse("2021-01-31")),
      new StrategyConfig("Turtle", (await StrategyLoader.GetDefaultConfig("Turtle"))!),
      new PaperBrokerConfig(1000, 0.001, false, true)
    );

    var loader = new BacktestCandleLoader(config.dataset);
    var trader = new Backtester(config);

    await trader.Preload(loader);
    while (await loader.Read()) {
      await trader.Tick(await loader.LoadOne());
      Console.ReadLine();
    }
  }

  public static async Task ValidatePaperTrader() {
    var config = new BacktestConfig(
      new DatasetConfig(Market.futures, 0, Binance.Net.Enums.KlineInterval.EightHour, DateTime.Parse("2019-01-01"), DateTime.Parse("2026-01-31")),
      new StrategyConfig("SuperTrendTurtle", (await StrategyLoader.GetDefaultConfig("SuperTrendTurtle"))!),
      new PaperBrokerConfig(1000, 0.001, false, true)
    );

    var paperTraderConfig = new PapertraderConfig(
      new("", ""),
      new(),
      config.strategy,
      config.broker
    );

    var loader = new BacktestCandleLoader(config.dataset);
    await loader.Init();

    var paperTrader = PaperTrader.Create(paperTraderConfig);
    var backtester = new Backtester(config);

    var state = paperTrader.Serialize();

    while (await loader.Read()) {
      var candle = await loader.LoadOne();

      paperTrader = PaperTrader.Create(paperTraderConfig);
      paperTrader.Deserialize(state);

      var backtestTrade = await backtester.Tick(candle);
      var paperTraderTrade = await paperTrader.Tick(candle);
      state = paperTrader.Serialize();

      // if (backtestTrade.HasValue) {
      //   Console.WriteLine($"Backtest: {backtestTrade.Value.TotalBalance}");
      // }
      // if (paperTraderTrade.HasValue) {
      //   Console.WriteLine($"PaperTrader: {paperTraderTrade.Value.TotalBalance}");
      // }

      var backtestPortfolio = backtester.broker.GetPortfolio();
      var paperTraderPortfolio = paperTrader.broker.GetPortfolio();

      if (backtestPortfolio.totalBalance != paperTraderPortfolio.totalBalance) {
        Console.WriteLine($"Balance mismatch: {backtestPortfolio.totalBalance} {paperTraderPortfolio.totalBalance}");
        Console.ReadLine();
      }
    }
  }

  public static async Task ValidatePaperTraderBacktesting() {
    var datasetConfig = new DatasetConfig(Market.futures, 0, Binance.Net.Enums.KlineInterval.EightHour, DateTime.Parse("2020-01-01T00:00:00"), DateTime.Parse("2020-02-01T00:00:00"));
    var config = new PapertraderConfig(
      new("", ""),
      new(Market.futures, 0, Binance.Net.Enums.KlineInterval.EightHour, 0),
      new StrategyConfig("SuperTrendTurtle", (await StrategyLoader.GetDefaultConfig("SuperTrendTurtle"))!),
      new PaperBrokerConfig(1000, 0.001, false, true)
    );
    var symbol = await Symbol.Get(0);

    var trader = PaperTrader.Create(config);

    trader.Serialize().Insert();

    var loader = new BacktestCandleLoader(datasetConfig);
    await loader.Init();

    while (await loader.Read()) {
      var candle = await loader.LoadOne();

      await LiveCandleDispatcher.Dispatch(trader.Id, symbol.Value, datasetConfig.interval, candle);
    }

    var state = await PaperTraderState.Get(trader.Id);
    if (state == null) {
      Console.WriteLine("Failed to load state");
      return;
    }

    Console.WriteLine(JsonSerializer.Serialize(new ExtPaperTraderState(state.Value)));
  }

  public static async Task TestBinance() {
    var client = new BinanceRestClient(options => {
      options.Environment = BinanceEnvironment.Testnet;
      options.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
        Environment.GetEnvironmentVariable("BINANCE_TESTNET_API_KEY") ?? "",
        Environment.GetEnvironmentVariable("BINANCE_TESTNET_API_SECRET") ?? ""
      );
    });

    var api = client.UsdFuturesApi;

    var info = await api.ExchangeData.GetExchangeInfoAsync();
    var filter = info.Data.Symbols.First(x => x.Pair == "BTCUSDT");

    Console.WriteLine($"pricePrecision: {filter.PricePrecision} quantityPrecision: {filter.QuantityPrecision} ");

    var balances = await api.Account.GetBalancesAsync();
    Console.WriteLine(JsonSerializer.Serialize(balances.Data.Where(x => x.WalletBalance > 0), new JsonSerializerOptions() {
      WriteIndented = true
    }));

    // await api.Account.ChangeInitialLeverageAsync("BTCUSDT", 20);

    var rate = 0.01m;

    var balance = balances.Data.First(x => x.Asset == "USDT").WalletBalance;

    var price = Math.Round((await api.ExchangeData.GetPriceAsync("BTCUSDT")).Data.Price, filter.PricePrecision);

    var account = await api.Account.GetAccountInfoV3Async();
    var totalBalance = account.Data.TotalCrossWalletBalance;

    Console.WriteLine(JsonSerializer.Serialize(account.Data.Assets, new JsonSerializerOptions() {
      WriteIndented = true
    }));
    return;

    var quantity = Math.Round(totalBalance * rate / price, filter.QuantityPrecision);
    Console.WriteLine($"Price: {price} Balance: {balance} Quantity: {quantity}");

    var shortOrder = await api.Trading.PlaceOrderAsync(
      symbol: "BTCUSDT",
      side: Binance.Net.Enums.OrderSide.Sell,
      type: Binance.Net.Enums.FuturesOrderType.Limit,
      quantity: (decimal)quantity,
      price: price,
      timeInForce: Binance.Net.Enums.TimeInForce.GoodTillCanceled
    );
    if (shortOrder.Error != null) {
      Console.WriteLine(shortOrder.Error);
      return;
    }

    Console.ReadLine();

    account = await api.Account.GetAccountInfoV3Async();
    Console.WriteLine(account.Data.Assets.First(x => x.Asset == "BTC").MarginBalance);

    return;

    var order = await api.Trading.PlaceOrderAsync(
      symbol: "BTCUSDT",
      side: Binance.Net.Enums.OrderSide.Buy,
      type: Binance.Net.Enums.FuturesOrderType.Limit,
      quantity: quantity,
      price: price,
      timeInForce: Binance.Net.Enums.TimeInForce.GoodTillCanceled
    );


    var positions = await api.Trading.GetPositionsAsync();
    var pos = positions.Data.First(x => x.Symbol == "BTCUSDT");
    Console.WriteLine(pos.PositionAmt);

    Console.ReadLine();

    account = await api.Account.GetAccountInfoV3Async();

    Console.WriteLine(JsonSerializer.Serialize(account.Data, new JsonSerializerOptions() {
      WriteIndented = true
    }));

    await api.Trading.PlaceOrderAsync(
      symbol: "BTCUSDT",
      side: Binance.Net.Enums.OrderSide.Sell,
      type: Binance.Net.Enums.FuturesOrderType.Limit,
      quantity: pos.PositionAmt,
      price: price,
      timeInForce: Binance.Net.Enums.TimeInForce.GoodTillCanceled
    );
  }

  public static async Task TestDownloader() {
    // list: s3 api
    // https://s3-ap-northeast-1.amazonaws.com/data.binance.vision?delimiter=/&prefix=data/futures/um/daily/klines/
    var url = "https://data.binance.vision/data/futures/um/daily/klines/BTCUSDT/8h/BTCUSDT-8h-2025-04-05.zip";

    using var client = new HttpClient();
    using var resp = await client.GetAsync(url);
    using var zs = await resp.Content.ReadAsStreamAsync();
    using var zip = new ZipArchive(zs);

    var i = 0;
    foreach (var entry in zip.Entries) {
      using var fs = entry.Open();
      using var sr = new StreamReader(fs);

      await sr.ReadLineAsync();

      while (!sr.EndOfStream) {
        var line = await sr.ReadLineAsync();
        var row = line!.Split(',');

        var openTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(row[0]));
        var open = double.Parse(row[1]);
        var high = double.Parse(row[2]);
        var low = double.Parse(row[3]);
        var close = double.Parse(row[4]);
        var volume = double.Parse(row[5]);
        var closeTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(row[6]));
        var quoteVolume = double.Parse(row[7]);
        var count = int.Parse(row[8]);
        var takerBuyVolume = double.Parse(row[9]);
        var takerBuyQuoteVolume = double.Parse(row[10]);

        Console.WriteLine($"{openTime} {open} {high} {low} {close} {volume} {closeTime} {quoteVolume} {count} {takerBuyVolume} {takerBuyQuoteVolume}");

        if (i++ > 10) {
          break;
        }
      }
    }
  }
}
