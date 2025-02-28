public static class Debugger {
  public static void DebugATR() {
    var atr = Indicators.ATR(5);
    var update = (double high, double low, double close) => {
      atr.Update(new Candle(Market.futures, "", Binance.Net.Enums.KlineInterval.OneMinute, DateTime.Now, 0, high, low, close, 0));
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
      sma.Update(new Candle(Market.futures, "", Binance.Net.Enums.KlineInterval.OneMinute, DateTime.Now, 0, 0, 0, d, 0));
      Console.WriteLine($"{sma.Ready} {d} {sma.Value}");
    }
  }

  public static async Task DebugBacktester() {
    var trader = new Backtester(new BacktestConfig(
      new DatasetConfig(Market.futures, "BTCUSDT", Binance.Net.Enums.KlineInterval.EightHour, DateTime.Parse("2021-01-01"), DateTime.Parse("2021-01-31")),
      new StrategyConfig("Turtle", StrategyLoader.GetDefaultConfig("Turtle")!),
      new PaperBrokerConfig(1000, 0.001, false, true)
    ));

    await trader.Init();
    while (!trader.IsEnded) {
      await trader.Tick();
      Console.ReadLine();
    }
  }
}
