using Binance.Net.Enums;

public static class KlineIntervalExtension {
  public static TimeSpan ToTimeSpan(this KlineInterval interval) {
    return interval switch {
      KlineInterval.OneSecond => TimeSpan.FromSeconds(1),
      KlineInterval.OneMinute => TimeSpan.FromMinutes(1),
      KlineInterval.ThreeMinutes => TimeSpan.FromMinutes(3),
      KlineInterval.FiveMinutes => TimeSpan.FromMinutes(5),
      KlineInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
      KlineInterval.ThirtyMinutes => TimeSpan.FromMinutes(30),
      KlineInterval.OneHour => TimeSpan.FromHours(1),
      KlineInterval.TwoHour => TimeSpan.FromHours(2),
      KlineInterval.FourHour => TimeSpan.FromHours(4),
      KlineInterval.SixHour => TimeSpan.FromHours(6),
      KlineInterval.EightHour => TimeSpan.FromHours(8),
      KlineInterval.TwelveHour => TimeSpan.FromHours(12),
      KlineInterval.OneDay => TimeSpan.FromDays(1),
      KlineInterval.ThreeDay => TimeSpan.FromDays(3),
      KlineInterval.OneWeek => TimeSpan.FromDays(7),
      KlineInterval.OneMonth => TimeSpan.FromDays(30),
      _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
    };
  }

  public static string ToIntervalString(this KlineInterval interval) {
    return interval switch {
      KlineInterval.OneSecond => "1s",
      KlineInterval.OneMinute => "1m",
      KlineInterval.ThreeMinutes => "3m",
      KlineInterval.FiveMinutes => "5m",
      KlineInterval.FifteenMinutes => "15m",
      KlineInterval.ThirtyMinutes => "30m",
      KlineInterval.OneHour => "1h",
      KlineInterval.TwoHour => "2h",
      KlineInterval.FourHour => "4h",
      KlineInterval.SixHour => "6h",
      KlineInterval.EightHour => "8h",
      KlineInterval.TwelveHour => "12h",
      KlineInterval.OneDay => "1d",
      KlineInterval.ThreeDay => "3d",
      KlineInterval.OneWeek => "1w",
      KlineInterval.OneMonth => "1mo",
      _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
    };
  }

  public static KlineInterval ToKlineInterval(this string raw) {
    return raw switch {
      "1s" => KlineInterval.OneSecond,
      "1m" => KlineInterval.OneMinute,
      "3m" => KlineInterval.ThreeMinutes,
      "5m" => KlineInterval.FiveMinutes,
      "15m" => KlineInterval.FifteenMinutes,
      "30m" => KlineInterval.ThirtyMinutes,
      "1h" => KlineInterval.OneHour,
      "2h" => KlineInterval.TwoHour,
      "4h" => KlineInterval.FourHour,
      "6h" => KlineInterval.SixHour,
      "8h" => KlineInterval.EightHour,
      "12h" => KlineInterval.TwelveHour,
      "1d" => KlineInterval.OneDay,
      "3d" => KlineInterval.ThreeDay,
      "1w" => KlineInterval.OneWeek,
      "1mo" => KlineInterval.OneMonth,
      _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, null)
    };
  }
}
