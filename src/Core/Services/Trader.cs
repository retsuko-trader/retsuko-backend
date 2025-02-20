public abstract class Trader {
  public ICandleLoader loader { get; protected set; }
  public IStrategy strategy { get; protected set; }
  public IBroker broker { get; protected set; }

  public bool IsEnded { get; protected set; }

  protected List<Trade> trades;
  protected Candle? firstCandle = null;
  protected Candle? lastCandle = null;

  protected TraderMetrics metrics;

  public Trader(
    ICandleLoader loader,
    IStrategy strategy,
    IBroker broker
  ) {
    this.loader = loader;
    this.strategy = strategy;
    this.broker = broker;

    trades = new List<Trade>();
    metrics = TraderMetrics.Empty;
  }

  public virtual async Task Init() {
    await loader.Init();
  }

  public async Task Tick() {
    if (!await loader.Read()) {
      IsEnded = true;
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

    var portfolio = broker.GetPortfolio();
    var balance = portfolio.currency + portfolio.asset * candle.close;

    if (trade.HasValue) {
      metrics.totalTrades += 1;
    }

    if (balance < metrics.minBalance) {
      metrics.minBalance = balance;
      metrics.minBalanceTs = candle.ts;
    }
    if (balance > metrics.maxBalance) {
      metrics.maxBalance = balance;
      metrics.maxBalanceTs = candle.ts;
    }
    metrics.marketChange = (lastCandle.Value.close - firstCandle.Value.close) / firstCandle.Value.close;

    var startBalance = broker.InitialBalance;
    var profit = (balance - startBalance) / startBalance;
    metrics.totalProfit = profit;

    var drawdown = (balance - metrics.maxBalance) / metrics.maxBalance;
    if (drawdown < metrics.drawdown) {
      metrics.drawdown = drawdown;
      metrics.drawdownHigh = metrics.maxBalance;
      metrics.drawdownLow = balance;
      metrics.drawdownStartTs = metrics.maxBalanceTs;
      metrics.drawdownEndTs = candle.ts;
    }

    var helper = new MetricsHelper(startBalance, ref portfolio, ref metrics, firstCandle.Value, lastCandle.Value);
    metrics.cagr = helper.cagr();
    metrics.avgTrades = helper.avgTrades();
  }
}
