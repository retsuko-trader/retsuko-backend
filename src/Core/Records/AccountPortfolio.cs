using Binance.Net.Objects.Models.Futures;

namespace Retsuko.Core;

public record struct AccountPortfolioAsset(
  string symbol,
  int leverage,
  double amount,
  double entryPrice,
  double marketPrice,
  double initialBalance,
  double currentBalance,
  double profitBalance,
  double profit
) {
  public static AccountPortfolioAsset From(BinancePositionV3 position) {
    return new AccountPortfolioAsset(
      symbol: position.Symbol,
      leverage: (int)(position.Leverage ?? -1),
      amount: (double)position.PositionAmt,
      entryPrice: (double)position.EntryPrice,
      marketPrice: (double)position.MarkPrice,
      initialBalance: (double)position.InitialMargin - (double)position.UnrealizedProfit,
      currentBalance: (double)position.InitialMargin,
      profitBalance: (double)position.UnrealizedProfit,
      profit: (double)position.UnrealizedProfit / (double)position.InitialMargin
    );
  }
}

public record struct AccountPortfolio(
  AccountPortfolioAsset[] assets,
  double currency,
  double totalBalance
);
