public abstract class Trader {
  public ICandleLoader loader { get; protected set; }
  public IStrategy strategy { get; protected set; }
  public IBroker broker { get; protected set; }

  protected List<Trade> trades;
  private Candle? firstCandle = null;
  private Candle? lastCandle = null;

  public Trader(
    ICandleLoader loader,
    IStrategy strategy,
    IBroker broker
  ) {
    this.loader = loader;
    this.strategy = strategy;
    this.broker = broker;
  }

  public virtual async Task Init() {
    await loader.Init();
  }

  public async Task Tick() {
    if (!await loader.Read()) {
      // TODO: end
      return;
    }

    var candle = await loader.LoadOne();
    var signal = await strategy.Update(candle);
    if (signal != null) {
      var trade = await broker.HandleAdvice(candle, signal);
      if (trade.HasValue) {
        var lastTrade = trades[^1];

        if (lastTrade.signal == SignalKind.@long || lastTrade.signal == SignalKind.@short) {
          var currBalance = trade.Value.TotalBalance;
          var prevBalance = lastTrade.TotalBalance;
          var profit = (currBalance - prevBalance) / prevBalance;

          lastTrade.profit = profit;
          trades[^1] = lastTrade;
        }
        trades.Add(trade.Value);
      }

      ProcessMetrics(candle, trade);
    }

    lastCandle = candle;
  }

  protected void ProcessMetrics(Candle candle, Trade? trade) {
    if (!firstCandle.HasValue || !lastCandle.HasValue) {
      return;
    }
  }
}
