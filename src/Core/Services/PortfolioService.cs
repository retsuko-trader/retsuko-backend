using Binance.Net.Enums;
using Retsuko.Core;
using Retsuko.Plugins;

public static class PortfolioService {
  public static async Task<AccountPortfolio> Get() {
    using var span = MyTracer.Tracer.StartActiveSpan("PortfolioService.Get");

    var client = Exchanger.LiveClient;
    var api = client.UsdFuturesApi;

    var account = await Exchanger.RetryOnError(() => api.Account.GetAccountInfoV3Async());
    if (!account.Success) {
      MyLogger.Logger.LogError("Error in PortfolioService.Get: binance account api error code={code} message={message} data={data}", account.Error!.Code, account.Error.Message, account.Error.Data);
      EventDispatcher.Exception(null, new Exception($"Error in PortfolioService.Get: binance account api error code={account.Error.Code} message={account.Error.Message}"));
      return new AccountPortfolio([], 0, 0);
    }

    var positions = await Exchanger.RetryOnError(() => api.Trading.GetPositionsAsync());
    if (!positions.Success) {
      MyLogger.Logger.LogError("Error in PortfolioService.Get: binance positions api error code={code} message={message} data={data}", positions.Error!.Code, positions.Error.Message, positions.Error.Data);
      EventDispatcher.Exception(null, new Exception($"Error in PortfolioService.Get: binance positions api error code={positions.Error.Code} message={positions.Error.Message}"));
      return new AccountPortfolio([], 0, 0);
    }

    var assets = positions.Data.Select(AccountPortfolioAsset.From).ToArray();

    var currency = account.Data.Assets.FirstOrDefault(x => x.Asset == "USDT");
    var totalBalance = account.Data.TotalMarginBalance;

    return new AccountPortfolio(
      assets: assets,
      currency: (double?)currency?.AvailableBalance ?? 0.0,
      totalBalance: (double)totalBalance
    );
  }
}
