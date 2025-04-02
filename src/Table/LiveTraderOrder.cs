using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using DuckDB.NET.Data;

namespace Retsuko.Core;

public record struct LiveTraderOrder(
  string traderId,
  string tradeId,
  long orderId,
  long rootOrderId,
  long? prevOrderId,
  long? nextOrderId,
  string? error,
  DateTime? closedAt,
  DateTime? cancelledAt,
  string symbol,
  string pair,
  double price,
  double averagePrice,
  double quantityFilled,
  double quantity,
  OrderStatus status,
  OrderSide side,
  TimeInForce timeInForce,
  FuturesOrderType type,
  FuturesOrderType orderType,
  DateTime updateTime,
  DateTime createTime,
  PositionSide positionSide
) {
  public static string TableName => "live_trader_order";

  public static LiveTraderOrder From(
    string traderId,
    string tradeId,
    CryptoExchange.Net.Objects.WebCallResult<BinanceUsdFuturesOrder> order,
    long? rootOrderId = null,
    long? prevOrderId = null
  ) {
    var now = DateTime.Now;
    if (order.Error != null) {
      return new LiveTraderOrder(
        traderId: traderId,
        tradeId: tradeId,
        orderId: -1,
        rootOrderId: rootOrderId ?? -1,
        prevOrderId: prevOrderId,
        nextOrderId: null,
        error: order.Error.Message,
        closedAt: null,
        cancelledAt: null,
        symbol: "",
        pair: "",
        price: 0,
        averagePrice: 0,
        quantityFilled: 0,
        quantity: 0,
        status: OrderStatus.PendingNew,
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
      rootOrderId: rootOrderId ?? orderData.Id,
      prevOrderId: prevOrderId,
      nextOrderId: null,
      error: null,
      closedAt: null,
      cancelledAt: null,
      symbol: orderData.Symbol,
      pair: orderData.Symbol,
      price: (double)orderData.Price,
      averagePrice: (double)orderData.AveragePrice,
      quantityFilled: (double)orderData.QuantityFilled,
      quantity: (double)orderData.Quantity,
      status: orderData.Status,
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
      .AppendValue(rootOrderId)
      .AppendValue(prevOrderId)
      .AppendValue(nextOrderId)
      .AppendValue(error)
      .AppendValue(closedAt)
      .AppendValue(cancelledAt)
      .AppendValue(symbol)
      .AppendValue(pair)
      .AppendValue(price)
      .AppendValue(averagePrice)
      .AppendValue(quantityFilled)
      .AppendValue(quantity)
      .AppendValue(status)
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

  public async Task Update() {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = $@"
      UPDATE {TableName} SET
        next_order_id = $nextOrderId,
        closed_at = $closedAt,
        cancelled_at = $cancelledAt,
        quantity_filled = $quantityFilled,
        status = $status,
        update_time = $updateTime
      WHERE trader_id = $traderId AND trade_id = $tradeId AND order_id = $orderId";

    command.Parameters.Add(new DuckDBParameter("nextOrderId", nextOrderId));
    command.Parameters.Add(new DuckDBParameter("closedAt", closedAt));
    command.Parameters.Add(new DuckDBParameter("cancelledAt", cancelledAt));
    command.Parameters.Add(new DuckDBParameter("quantityFilled", quantityFilled));
    command.Parameters.Add(new DuckDBParameter("status", status));
    command.Parameters.Add(new DuckDBParameter("updateTime", updateTime));
    command.Parameters.Add(new DuckDBParameter("traderId", traderId));
    command.Parameters.Add(new DuckDBParameter("tradeId", tradeId));
    command.Parameters.Add(new DuckDBParameter("orderId", orderId));

    await command.ExecuteNonQueryAsync();
  }
}
