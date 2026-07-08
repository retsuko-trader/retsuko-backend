namespace Retsuko;

public static class DateTimeExtension {
  public static double ToUnixTimestamp(this DateTime dateTime) {
    return dateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
  }

  public static DateTime FromUnixTimestamp(double timestamp) {
    return DateTime.UnixEpoch.AddSeconds(timestamp);
  }
}
