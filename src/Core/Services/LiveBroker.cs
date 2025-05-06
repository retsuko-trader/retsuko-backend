using System.Text.Json;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using Binance.Net.Objects.Models.Futures;
using Retsuko.Core.Events;
using Retsuko.Plugins;

namespace Retsuko.Core;

public class LiveBroker: IBroker, ISerializable {
  record struct Position(
    double ts,
    PositionKind kind,
    double confidence
  );

  public LiveTrader trader;

  private LiveBrokerConfig config;

  public BinanceRestClient client { get; private set; }
  private IBinanceRestClientUsdFuturesApi api => client.UsdFuturesApi;

  public double InitialBalance { get; private set; } = 1;
  Position? position;

  private Portfolio portfolio;

  public LiveBroker(LiveBrokerConfig config) {
    this.config = config;

    client = config.isTestNet ? Exchanger.TestNetClient : Exchanger.LiveClient;
  }

  public async Task<Trade?> HandleAdvice(Candle candle, Signal signal) {
    var symbol = await Symbol.Get(candle.symbolId);
    // TODO: fix asset name
    var symbolName = symbol.Value.name;
    var asset = symbolName.Replace("USDT", "");

    await api.Account.ChangeInitialLeverageAsync(symbolName, config.leverage);

    var account = await api.Account.GetAccountInfoV3Async();
    var assetInfo = account.Data.Assets.First(x => x.Asset == asset);
    var currencyInfo = account.Data.Assets.First(x => x.Asset == "USDT");

    var position = account.Data.Positions.FirstOrDefault(x => x.Symbol == symbolName);

    portfolio.totalBalance = (double)account.Data.TotalWalletBalance;
    portfolio.asset = (double?)position?.PositionAmount ?? 0.0;
    portfolio.currency = (double?)currencyInfo?.AvailableBalance ?? 0.0;

    var info = await api.ExchangeData.GetExchangeInfoAsync();
    var filter = info.Data.Symbols.First(x => x.Pair == symbolName);

    CryptoExchange.Net.Objects.WebCallResult<BinanceUsdFuturesOrder>? order = null;

    if (InitialBalance == 1) {
      InitialBalance = portfolio.totalBalance;
    }

    // TODO: check current open orders
    if (signal.kind == SignalKind.openLong) {
      var prevConfidence = this.position?.confidence ?? 0;
      if (this.position?.kind == PositionKind.@long && prevConfidence >= signal.confidence) {
        MyLogger.Logger.LogInformation("Skip open long {signal.confidence} < {prevConfidence}", signal.confidence, prevConfidence);
        return null;
      }

      var positionConfidence = portfolio.currency / portfolio.totalBalance;
      var expectAmount = portfolio.totalBalance * config.leverage * 0.95 * config.ratio * (signal.confidence - positionConfidence);

      if (expectAmount < 0) {
        MyLogger.Logger.LogInformation("Skip open long {confidence} < {positionConfidence}", signal.confidence, positionConfidence);
        return null;
      }

      var quantity = Math.Round(expectAmount / candle.close, filter.QuantityPrecision);

      order = await api.Trading.PlaceOrderAsync(
        symbol: symbol.Value.name,
        side: OrderSide.Buy,
        type: FuturesOrderType.Limit,
        quantity: (decimal)quantity,
        price: (decimal)candle.close,
        timeInForce: TimeInForce.GoodTillCanceled
      );
      this.position = new Position(candle.ts.ToUnixTimestamp(), PositionKind.@long, signal.confidence);

      // don't use short, yet
    } else if (signal.kind == SignalKind.closeLong || signal.kind == SignalKind.openShort) {
      if (this.position == null || this.position.Value.kind != PositionKind.@long) {
        MyLogger.Logger.LogInformation("Skip close long {signal} {position}", signal.kind, this.position);
        return null;
      }

      order = await api.Trading.PlaceOrderAsync(
        symbol: symbol.Value.name,
        side: OrderSide.Sell,
        type: FuturesOrderType.Limit,
        quantity: position?.PositionAmount ?? 0,
        price: (decimal)candle.close,
        timeInForce: TimeInForce.GoodTillCanceled
      );
      this.position = null;
    } else if (signal.kind == SignalKind.openShort) {
      var prevConfidence = this.position?.confidence ?? 0;
      if (this.position?.kind == PositionKind.@short && prevConfidence >= signal.confidence) {
        MyLogger.Logger.LogInformation("Skip open short {confidence} < {prevConfidence}", signal.confidence, prevConfidence);
        return null;
      }

      var expectAmount = portfolio.totalBalance * 0.95 * config.ratio * signal.confidence;
      var quantity = Math.Round(expectAmount / candle.close, filter.QuantityPrecision);

      order = await api.Trading.PlaceOrderAsync(
        symbol: symbol.Value.name,
        side: OrderSide.Sell,
        type: FuturesOrderType.Limit,
        quantity: (decimal)quantity,
        price: (decimal)candle.close,
        timeInForce: TimeInForce.GoodTillCanceled
      );
      this.position = new Position(candle.ts.ToUnixTimestamp(), PositionKind.@short, signal.confidence);
    } else if (signal.kind == SignalKind.closeShort) {
      if (this.position == null || this.position.Value.kind != PositionKind.@short) {
        MyLogger.Logger.LogInformation("Skip close short {signal} {position}", signal.kind, this.position);
        return null;
      }

      order = await api.Trading.PlaceOrderAsync(
        symbol: symbol.Value.name,
        side: OrderSide.Buy,
        type: FuturesOrderType.Limit,
        quantity: position?.PositionAmount ?? 0,
        price: (decimal)candle.close,
        timeInForce: TimeInForce.GoodTillCanceled,
        positionSide: PositionSide.Short
      );
      this.position = null;
    }

    EventDispatcher.Event(new LiveBrokerGotSignalEvent(
      trader,
      this,
      candle.ts,
      symbol.Value,
      signal,
      account.Data,
      assetInfo,
      currencyInfo,
      position,
      order
    ));

    if (order == null) {
      MyLogger.Logger.LogError("Order is null");
      return null;
    }

    return new Trade(
      candle.ts,
      signal.kind,
      signal.confidence,
      portfolio.asset,
      portfolio.currency,
      candle.close,
      0,
      order
    );
  }

  record InnerState(
    LiveBrokerConfig config,
    double initialBalance,
    Position? position
  );

  public Portfolio GetPortfolio() {
    return portfolio;
  }

  public string Serialize() {
    return JsonSerializer.Serialize<InnerState>(new (config, InitialBalance, position));
  }

  public void Deserialize(string state) {
    var x = JsonSerializer.Deserialize<InnerState>(state);
    if (x == null) {
      return;
    }

    config = x.config;
    InitialBalance = x.initialBalance;
    position = x.position;
  }
}
