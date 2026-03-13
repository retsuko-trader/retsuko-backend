using System.Diagnostics.Metrics;

namespace Retsuko.Core;

public static class MetricsMeter {
  public const string Name = "Retsuko.Metrics";

  private static readonly Meter meter;

  private static readonly Gauge<double> portfolioAssetsGauge;
  private static readonly Gauge<double> portfolioProfitGauge;
  private static readonly Gauge<double> portfolioProfitBalanceGauge;
  private static readonly Gauge<double> portfolioCurrencyGauge;
  private static readonly Gauge<double> portfolioTotalBalanceGauge;
  private static readonly Gauge<double> portfolioConfidenceGauge;

  private static readonly Gauge<double> assetPriceGauge;


  static MetricsMeter() {
    meter = new Meter(Name);

    portfolioAssetsGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.assets");
    portfolioProfitGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.profit");
    portfolioProfitBalanceGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.profit_balance");
    portfolioCurrencyGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.currency");
    portfolioTotalBalanceGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.total_balance");
    portfolioConfidenceGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.confidence");

    assetPriceGauge = meter.CreateGauge<double>("retsuko.metrics.asset.price");
  }

  public static async Task Update(AccountPortfolio portfolio) {
    if (portfolioAssetsGauge.Tags != null) {
      foreach (var tag in portfolioAssetsGauge.Tags) {
        portfolioAssetsGauge.Record(0, tag);
        portfolioProfitBalanceGauge.Record(0, tag);
        portfolioProfitGauge.Record(0, tag);
      }
    }

    foreach (var asset in portfolio.assets) {
      var tag = new KeyValuePair<string, object?>("symbol", asset.symbol);
      portfolioAssetsGauge.Record(asset.currentBalance, tag);
      portfolioProfitBalanceGauge.Record(asset.profitBalance, tag);
      portfolioProfitGauge.Record(asset.profit, tag);
    }

    portfolioCurrencyGauge.Record(portfolio.currency);
    portfolioTotalBalanceGauge.Record(portfolio.totalBalance);
    portfolioConfidenceGauge.Record(1 - portfolio.currency / portfolio.totalBalance);

    var api = Exchanger.LiveClient.UsdFuturesApi;
    var priceData = await api.ExchangeData.GetPriceAsync("BTCUSDT");
    assetPriceGauge.Record((double)priceData.Data.Price);
  }
}
