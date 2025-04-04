using System.Data.Common;
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

  public static LiveTraderOrder From(DbDataReader reader) {
    return new LiveTraderOrder(
      traderId: reader.GetString(0),
      tradeId: reader.GetString(1),
      orderId: reader.GetInt64(2),
      rootOrderId: reader.GetInt64(3),
      prevOrderId: reader.IsDBNull(4) ? null : reader.GetInt64(4),
      nextOrderId: reader.IsDBNull(5) ? null : reader.GetInt64(5),
      error: reader.IsDBNull(6) ? null : reader.GetString(6),
      closedAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7),
      cancelledAt: reader.IsDBNull(8) ? null : reader.GetDateTime(8),
      symbol: reader.GetString(9),
      pair: reader.GetString(10),
      price: reader.GetDouble(11),
      averagePrice: reader.GetDouble(12),
      quantityFilled: reader.GetDouble(13),
      quantity: reader.GetDouble(14),
      status: (OrderStatus)reader.GetInt32(15),
      side: (OrderSide)reader.GetInt32(16),
      timeInForce: (TimeInForce)reader.GetInt32(17),
      type: (FuturesOrderType)reader.GetInt32(18),
      orderType: (FuturesOrderType)reader.GetInt32(19),
      updateTime: reader.GetDateTime(20),
      createTime: reader.GetDateTime(21),
      positionSide: (PositionSide)reader.GetInt32(22)
    );
  }

  public static async Task<IReadOnlyList<LiveTraderOrder>> List(string traderId) {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = $"SELECT * FROM {TableName} WHERE trader_id = $traderId ORDER BY create_time ASC";
    command.Parameters.Add(new DuckDBParameter("traderId", traderId));
    var reader = await command.ExecuteReaderAsync();
    var orders = new List<LiveTraderOrder>();

    while (reader.Read()) {
      var order = From(reader);
      orders.Add(order);
    }

    await reader.CloseAsync();
    return orders;
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
      .AppendValue((int)status)
      .AppendValue((int)side)
      .AppendValue((int)timeInForce)
      .AppendValue((int)type)
      .AppendValue((int)orderType)
      .AppendValue(updateTime)
      .AppendValue(createTime)
      .AppendValue((int)positionSide)
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
