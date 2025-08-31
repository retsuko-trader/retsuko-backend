namespace Retsuko.Core;

using Binance.Net.Enums;

public static class LiveCandleDispatcher {
  public static async Task Dispatch(string id, Symbol symbol, KlineInterval interval, Candle candle) {
    var kind = TraderIdHelper.Parse(id);

    if (kind == TraderIdHelper.IdKind.PaperTrader) {
      await HandlePaperTrader(id, symbol, interval, candle);
    } else if (kind == TraderIdHelper.IdKind.LiveTraderTest || kind == TraderIdHelper.IdKind.LiveTraderLive) {
      await HandleLiveTrader(id, symbol, interval, candle);
    }
  }

  private static async Task HandlePaperTrader(string id, Symbol symbol, KlineInterval interval, Candle candle) {
    var trader = await PaperTrader.Load(id);
    if (trader == null) {
      MyLogger.Logger.LogError("failed to load paper trader; {id}", id);
      return;
    }

    await trader.Tick(candle);
    await trader.FinalizeMetrics();
    var state = await trader.Serialize();
    await state.Update();
  }

  private static async Task HandleLiveTrader(string id, Symbol symbol, KlineInterval interval, Candle candle) {
    var trader = await LiveTrader.Load(id);
    if (trader == null) {
      MyLogger.Logger.LogError("failed to load live trader; {id}", id);
      return;
    }

    await trader.Tick(candle);
    await trader.FinalizeMetrics();
    var state = await trader.Serialize();
    await state.Update();
  }
}
