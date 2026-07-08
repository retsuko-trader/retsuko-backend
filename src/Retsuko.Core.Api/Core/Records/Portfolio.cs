namespace Retsuko.Core;

public record struct Portfolio(
  double asset,
  double currency,
  double totalBalance
);
