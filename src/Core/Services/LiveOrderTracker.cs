using Binance.Net.Clients;

namespace Retsuko.Core;

public static class LiveOrderTracker {
  public static void StartTrack(BinanceRestClient client, LiveTraderOrder order) {
    _ = Task.Run(async () => {
      order.Insert();

      var curr = order;

      await Task.Delay(1000);
      while (true) {
        var orderResp = await client.UsdFuturesApi.Trading.GetOrderAsync(curr.symbol, curr.orderId);
        if (orderResp.Error != null) {
          // insufficient balance
          if (orderResp.Error.Code == -2018) {
            MyLogger.Logger.LogError("Insufficient balance while tracking order {orderId}: {Error}", curr.orderId, orderResp.Error);
            curr.cancelledAt = DateTime.Now;
            await curr.Update();
            return;
          }

          MyLogger.Logger.LogError("Error while tracking order {orderId}: {Error}", curr.orderId, orderResp.Error);
          return;
        }

        curr.status = orderResp.Data.Status;
        curr.quantityFilled = (double)orderResp.Data.QuantityFilled;
        curr.updateTime = orderResp.Data.UpdateTime;

        if (orderResp.Data.Status == Binance.Net.Enums.OrderStatus.Filled) {
          curr.closedAt = orderResp.Data.UpdateTime;
          await curr.Update();
          break;
        }

        await curr.Update();

        var newPrice = orderResp.Data.AveragePrice;
        var editResp = await client.UsdFuturesApi.Trading.EditOrderAsync(
          order.symbol,
          order.side,
          orderResp.Data.Quantity,
          newPrice
        );

        curr = LiveTraderOrder.From(
          order.traderId,
          order.tradeId,
          editResp,
          order.orderId,
          curr.orderId
        );
        curr.Insert();
        if (editResp.Error != null) {
          MyLogger.Logger.LogError("Error while editing order {orderId}: {Error}", curr.orderId, editResp.Error);
          return;
        }

        await Task.Delay(1500);
      }
    });
  }
}
