namespace Retsuko.Strategies.Services;

public static class ConsistencyHelper {
  public static void CheckCandlesTimestamps(List<string> errors, Candle[] candles, int age) {
    if (age < candles.Length - 1) {
      errors.Add($"not enough candles for consistency check; age={age} candlesLength={candles.Length}");
      return;
    }

    var interval = candles.GetByMod(1)!.Ts - candles.GetByMod(0)!.Ts;

    for (var i = 0; i < candles.Length - 1; i++) {
      var index = age - candles.Length + i;
      var curr = candles.GetByMod(index);
      var next = candles.GetByMod(index + 1);

      var tsDiff = next.Ts - curr.Ts;

      if (tsDiff.Seconds == 0) {
        errors.Add($"duplicate timestamp; age={index} ts={curr.Ts}");
      } else if (tsDiff.Seconds < 0) {
        errors.Add($"reversed timestamp order; age={index} ts={curr.Ts} nextTs={next.Ts}");
      } else if (tsDiff.Seconds != interval.Seconds) {
        errors.Add($"inconsistent timestamp interval; age={index} ts={curr.Ts} nextTs={next.Ts} interval={tsDiff.Seconds} expectedInterval={interval.Seconds}");
      }
    }
  }
}
