using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;

namespace Retsuko.Core;

public class Broker {
  public static BinanceRestClient Client { get; } = new BinanceRestClient();

  public static IBinanceRestClientUsdFuturesApi API => Client.UsdFuturesApi;

  public static async IAsyncEnumerable<Binance.Net.Interfaces.IBinanceKline> GetKlinesAsync(string symbol, KlineInterval interval, DateTime? startTime, DateTime? endTime) {
    var api = API.ExchangeData;
    var candles = await api.GetKlinesAsync(symbol, interval, startTime, null, 1000);

    if (!candles.Data.Any()) {
      yield break;
    }

    foreach (var candle in candles.Data) {
      if (endTime.HasValue && candle.OpenTime >= endTime) {
        yield break;
      }

      yield return candle;
    }

    var end = candles.Data.Last().OpenTime;
    while (candles.Data.Count() >= 1000) {
      candles = await api.GetKlinesAsync(symbol, interval, end, null, 1000);

      if (!candles.Data.Any()) {
        break;
      }

      end = candles.Data.Last().OpenTime;

      foreach (var candle in candles.Data.Skip(1)) {
        if (endTime.HasValue && candle.OpenTime >= endTime) {
          yield break;
        }

        yield return candle;
      }
    }
  }

  public static async IAsyncEnumerable<IEnumerable<Binance.Net.Interfaces.IBinanceKline>> GetKlineChunksAsync(string symbol, KlineInterval interval, DateTime? startTime) {
    var api = API.ExchangeData;
    var candles = await api.GetKlinesAsync(symbol, interval, startTime, null, 1000);

    if (!candles.Data.Any()) {
      yield break;
    }

    yield return candles.Data;

    var end = candles.Data.Last().OpenTime;
    while (candles.Data.Count() >= 1000) {
      candles = await api.GetKlinesAsync(symbol, interval, end, null, 1000);

      if (!candles.Data.Any()) {
        break;
      }

      end = candles.Data.Last().OpenTime;
      yield return candles.Data.Skip(1);
    }
  }

  public static async IAsyncEnumerable<Binance.Net.Interfaces.IBinanceKline> GetRecentKlinesAsync(string symbol, KlineInterval interval, int count) {
    var api = API.ExchangeData;

    if (count <= 1000) {
      var candles = await api.GetKlinesAsync(symbol, interval, null, null, count);
      foreach (var candle in candles.Data) {
        yield return candle;
      }

      yield break;
    }

    // TODO: impl pagination
  }
}
