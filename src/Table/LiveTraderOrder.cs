using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;

namespace Retsuko.Core;

public record struct LiveTraderOrder(
  string traderId,
  string tradeId,
  double orderId,
  string? error,
  bool closed,
  string symbol,
  string pair,
  double price,
  double averagePrice,
  double quantityFilled,
  double quantity,
  OrderSide side,
  TimeInForce timeInForce,
  FuturesOrderType type,
  FuturesOrderType orderType,
  DateTime updateTime,
  DateTime createTime,
  PositionSide positionSide
) {
  public static string TableName => "live_trader_order";

  public static LiveTraderOrder From(string traderId, string tradeId, CryptoExchange.Net.Objects.WebCallResult<BinanceUsdFuturesOrder> order) {
    var now = DateTime.Now;
    if (order.Error != null) {
      return new LiveTraderOrder(
        traderId: traderId,
        tradeId: tradeId,
        orderId: -1,
        error: order.Error.Message,
        closed: false,
        symbol: "",
        pair: "",
        price: 0,
        averagePrice: 0,
        quantityFilled: 0,
        quantity: 0,
        side: OrderSide.Buy,
        timeInForce: TimeInForce.GoodTillCanceled,
        type: FuturesOrderType.Limit,
        orderType: FuturesOrderType.Limit,
        updateTime: now,
        createTime: now,
        positionSide: PositionSide.Both
      );
    }

    var orderData = order.Data;
    return new LiveTraderOrder(
      traderId: traderId,
      tradeId: tradeId,
      orderId: orderData.Id,
      error: null,
      closed: false,
      symbol: orderData.Symbol,
      pair: orderData.Symbol,
      price: (double)orderData.Price,
      averagePrice: (double)orderData.AveragePrice,
      quantityFilled: (double)orderData.QuantityFilled,
      quantity: (double)orderData.Quantity,
      side: orderData.Side,
      timeInForce: orderData.TimeInForce,
      type: orderData.Type,
      orderType: orderData.Type,
      updateTime: orderData.UpdateTime,
      createTime: orderData.CreateTime,
      positionSide: orderData.PositionSide
    );
  }

  public void Insert() {
    using var appender = Database.LiveTrader.CreateAppender(TableName);
    var row = appender.CreateRow();
    row.AppendValue(traderId)
      .AppendValue(tradeId)
      .AppendValue(orderId)
      .AppendValue(error)
      .AppendValue(closed)
      .AppendValue(symbol)
      .AppendValue(pair)
      .AppendValue(price)
      .AppendValue(averagePrice)
      .AppendValue(quantityFilled)
      .AppendValue(quantity)
      .AppendValue(side)
      .AppendValue(timeInForce)
      .AppendValue(type)
      .AppendValue(orderType)
      .AppendValue(updateTime)
      .AppendValue(createTime)
      .AppendValue(positionSide)
      .EndRow();
    appender.Close();
  }
}
