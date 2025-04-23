using Binance.Net.Enums;
using Retsuko.Core;

public static class PortfolioService {
  public static async Task<AccountPortfolio> Get() {
    var client = Exchanger.LiveClient;
    var api = client.UsdFuturesApi;

    var account = await api.Account.GetAccountInfoV3Async();

    var positions = await api.Trading.GetPositionsAsync();
    var assets = positions.Data.Select(AccountPortfolioAsset.From).ToArray();

    var currency = account.Data.Assets.FirstOrDefault(x => x.Asset == "USDT");
    var totalBalance = account.Data.TotalWalletBalance;

    return new AccountPortfolio(
      assets: assets,
      currency: (double?)currency?.AvailableBalance ?? 0.0,
      totalBalance: (double)totalBalance
    );
  }
}
