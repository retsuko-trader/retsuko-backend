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
    using var trader = await PaperTrader.Load(id);
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
    var prevState = await LiveTraderState.Get(id);
    using var trader = await LiveTrader.Load(id);
    if (trader == null) {
      MyLogger.Logger.LogError("failed to load live trader; {id}", id);
      return;
    }

    var trade = await trader.Tick(candle);
    await trader.FinalizeMetrics();

    var nextState = await trader.Serialize();
    await nextState.Update();

    LiveTraderHistory.Create(id, prevState.Value, nextState, new LiveTraderHistory.MessageTick(
      candle: candle,
      trade: trade,
      force: false
    ));
  }
}
