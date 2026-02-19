using System.Diagnostics.Metrics;

namespace Retsuko.Core;

public static class MetricsMeter {
  public const string Name = "Retsuko.Metrics";

  private static readonly Meter meter;

  private static readonly Gauge<double> portfolioAssetsGauge;
  private static readonly Gauge<double> portfolioCurrencyGauge;
  private static readonly Gauge<double> portfolioTotalBalanceGauge;
  private static readonly Gauge<double> portfolioConfidenceGauge;

  private static readonly Gauge<double> assetPriceGauge;


  static MetricsMeter() {
    meter = new Meter(Name);

    portfolioAssetsGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.assets");
    portfolioCurrencyGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.currency");
    portfolioTotalBalanceGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.total_balance");
    portfolioConfidenceGauge = meter.CreateGauge<double>("retsuko.metrics.portfolio.confidence");

    assetPriceGauge = meter.CreateGauge<double>("retsuko.metrics.asset.price");
  }

  public static async Task Update(AccountPortfolio portfolio) {
    portfolioAssetsGauge.Record(portfolio.assets.Length);
    portfolioCurrencyGauge.Record(portfolio.currency);
    portfolioTotalBalanceGauge.Record(portfolio.totalBalance);
    portfolioConfidenceGauge.Record(1 - portfolio.currency / portfolio.totalBalance);

    var api = Exchanger.LiveClient.UsdFuturesApi;
    var priceData = await api.ExchangeData.GetPriceAsync("BTCUSDT");
    assetPriceGauge.Record((double)priceData.Data.Price);
  }
}
