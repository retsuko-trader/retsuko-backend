using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;

public class Broker {
  public static BinanceRestClient Client { get; } = new BinanceRestClient();

  public static IBinanceRestClientUsdFuturesApi API => Client.UsdFuturesApi;
}
