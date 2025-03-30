using System.Text.Json;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using Binance.Net.Objects.Models.Futures;

namespace Retsuko.Core;

public class LiveBroker: IBroker, ISerializable {
  private LiveBrokerConfig config;

  private BinanceRestClient client;
  private IBinanceRestClientUsdFuturesApi api => client.UsdFuturesApi;

  public double InitialBalance => 0;

  private Portfolio portfolio;

  public LiveBroker(LiveBrokerConfig config) {
    this.config = config;

    client = new BinanceRestClient(options => {
      if (config.isTestNet) {
        options.Environment = BinanceEnvironment.Testnet;
        options.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
          Environment.GetEnvironmentVariable("BINANCE_TESTNET_API_KEY") ?? "",
          Environment.GetEnvironmentVariable("BINANCE_TESTNET_API_SECRET") ?? ""
        );
      } else {
        options.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
          Environment.GetEnvironmentVariable("BINANCE_API_KEY") ?? "",
          Environment.GetEnvironmentVariable("BINANCE_API_SECRET") ?? ""
        );
      }
    });
  }

  public async Task<Trade?> HandleAdvice(Candle candle, Signal signal) {
    var symbol = await Symbol.Get(candle.symbolId);
    var symbolName = symbol.Value.name;

    await api.Account.ChangeInitialLeverageAsync(symbolName, config.leverege);

    var account = await api.Account.GetAccountInfoV3Async();
    var assetInfo = account.Data.Assets.First(x => x.Asset == symbolName);
    var currencyInfo = account.Data.Assets.First(x => x.Asset == "USDT");

    var position = account.Data.Positions.First(x => x.Symbol == symbolName);

    portfolio.totalBalance = (double)account.Data.TotalWalletBalance;
    portfolio.asset = (double?)assetInfo?.WalletBalance ?? 0.0;
    portfolio.currency = (double?)currencyInfo?.WalletBalance ?? 0.0;

    var info = await api.ExchangeData.GetExchangeInfoAsync();
    var filter = info.Data.Symbols.First(x => x.Pair == symbolName);

    CryptoExchange.Net.Objects.WebCallResult<BinanceUsdFuturesOrder>? order = null;

    if (signal.kind == SignalKind.openLong) {
      var expectAmount = portfolio.totalBalance * 0.95 * config.ratio * signal.confidence;
      var quantity = Math.Round(expectAmount / candle.close, filter.QuantityPrecision);

      order = await api.Trading.PlaceOrderAsync(
        symbol: symbol.Value.name,
        side: OrderSide.Buy,
        type: FuturesOrderType.Limit,
        quantity: (decimal)quantity,
        price: (decimal)candle.close,
        timeInForce: TimeInForce.GoodTillCanceled
      );
    } else if (signal.kind == SignalKind.closeLong) {
      order = await api.Trading.PlaceOrderAsync(
        symbol: symbol.Value.name,
        side: OrderSide.Sell,
        type: FuturesOrderType.Limit,
        quantity: (decimal)portfolio.asset,
        price: (decimal)candle.close,
        timeInForce: TimeInForce.GoodTillCanceled
      );
    } else if (signal.kind == SignalKind.openShort) {
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
    } else if (signal.kind == SignalKind.closeShort) {
      order = await api.Trading.PlaceOrderAsync(
        symbol: symbol.Value.name,
        side: OrderSide.Buy,
        type: FuturesOrderType.Limit,
        quantity: position?.PositionAmount ?? 0,
        price: (decimal)candle.close,
        timeInForce: TimeInForce.GoodTillCanceled,
        positionSide: PositionSide.Short
      );
    }

    if (order == null) {
      return null;
    }

    return new Trade(
      candle.ts,
      signal.kind,
      signal.confidence,
      portfolio.asset,
      portfolio.currency,
      candle.close,
      0
    );
  }

  public Portfolio GetPortfolio() {
    return portfolio;
  }

  public string Serialize() {
    return JsonSerializer.Serialize(config);
  }

  public void Deserialize(string state) {
    var x = JsonSerializer.Deserialize<LiveBrokerConfig>(state);
    if (x == null) {
      return;
    }

    config = x;
  }
}
