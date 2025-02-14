using System.Globalization;

public record struct StrategyConfig(
  string name,
  Dictionary<string, NumberFormatInfo> config
) {
}
