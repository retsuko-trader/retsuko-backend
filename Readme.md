# retsuko-backend

![retsuko](imgs/retsuko.png)

> WIP!

Retsuko, Cryptocurrency algorithmic/systematic/programmatic trading framework

## features

- [x] dataset management
- [x] highly customizable backtesting
  - [x] run bulk backtests
  - [x] debug indicators
- [x] live paper trading
- [x] live trading

## preview

### backtesting single strategy
![backtest single](imgs/backtest_single.png)

### backtesting with debugging indicators
![backtest_debug](imgs/backtest_debug.png)

### backtesting bulk symbols, intervals, strategies
![backtest_bulk](imgs/backtest_bulk.png)

## how to run

Workign on documentations, coming soon

## strategy

There are included strategies, which are used in live production.

Currently with [SuperTrendTurtle](src/Strategies/SuperTrendTurtle.cs):
- CAGR 80.34%
- Max drawdown -28.96%
