# retsuko-backend

WIP

## terminologies

- Market: "futures" | "spot"
- Symbol: symbol of coin
- Interval: 1m | 3m | ...
- Candle: kline
- Dataset: Group of candles by (market, symbol, interval)
- Strategy: algorithm model
- Backtest: single backtest for one dataset, one strategy
- BulkBacktest: multiple backtests for datasets and strategies
- Broker: market exchange broker, live binance or mock broker for paper trading
- Trader: a set of candles - stretagy - broker
