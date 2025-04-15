namespace Retsuko.Core;

public abstract class Trader {
  public IStrategy strategy { get; protected set; }
  public IBroker broker { get; protected set; }

  protected List<Trade> trades;
  protected Candle? firstCandle = null;
  protected Candle? lastCandle = null;

  protected TraderMetrics metrics;

  public Trader(
    IStrategy strategy,
    IBroker broker
  ) {
    this.strategy = strategy;
    this.broker = broker;

    trades = [];
    metrics = TraderMetrics.Empty with {
      startBalance = broker.InitialBalance,
      endBalance = broker.InitialBalance,
      asset = broker.GetPortfolio().asset,
      currency = broker.GetPortfolio().currency,
      totalBalance = broker.InitialBalance,
      totalProfit = 1,
    };
  }

  public virtual async Task Preload(ICandleLoader loader) {
    await loader.Init();

    var candles = await loader.Preload();

    foreach (var candle in candles) {
      await strategy.Preload(candle);
    }
  }

  public async virtual Task<Trade?> Tick(Candle candle) {
    if (!firstCandle.HasValue) {
      firstCandle = candle;
    }

    Trade? trade = null;

    var signal = await strategy.Update(candle);
    if (signal != null) {
      trade = await broker.HandleAdvice(candle, signal);
      if (trade.HasValue) {
        if (trades.Count > 0) {
          var lastTrade = trades[^1];

          if (lastTrade.signal == SignalKind.openLong || lastTrade.signal == SignalKind.openShort) {
            var currBalance = trade.Value.TotalBalance;
            var prevBalance = lastTrade.TotalBalance;
            var profit = (currBalance - prevBalance) / prevBalance;

            lastTrade.profit = profit;
            trades[^1] = lastTrade;
          }
        }

        trades.Add(trade.Value);
      }

      ProcessMetrics(candle, trade);
    }

    lastCandle = candle;
    return trade;
  }

  protected virtual void ProcessMetrics(Candle candle, Trade? trade) {
    if (!firstCandle.HasValue || !lastCandle.HasValue) {
      return;
    }

    var portfolio = broker.GetPortfolio();
    var balance = portfolio.currency + portfolio.asset * candle.close;

    metrics.asset = portfolio.asset;
    metrics.currency = portfolio.currency;
    metrics.totalBalance = balance;

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

    if (firstCandle.Value.close != 0) {
      metrics.marketChange = (lastCandle.Value.close - firstCandle.Value.close) / firstCandle.Value.close;
    }

    var startBalance = broker.InitialBalance;

    if (startBalance == 0) {
      startBalance = 1;
    }
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

    var helper = new MetricsHelper(startBalance, ref portfolio, ref metrics, ref trades, firstCandle.Value, lastCandle.Value);
    metrics.cagr = helper.cagr();
    metrics.avgTrades = helper.avgTrades();
    metrics.sortino = helper.sortino();
    metrics.sharpe = helper.sharpe();

    metrics.endBalance = portfolio.totalBalance;
  }
}
