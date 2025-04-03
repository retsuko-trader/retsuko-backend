using Binance.Net.Clients;

namespace Retsuko.Core;

public static class LiveOrderTracker {
  public static async void StartTrack(BinanceRestClient client, LiveTraderOrder order) {
    MyLogger.Logger.LogInformation("Start tracking order {orderId}: {symbol}", order.orderId, order.symbol);

    var info = await client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
    var filter = info.Data.Symbols.First(x => x.Pair == order.symbol);

    await Task.Run(async () => {
      order.Insert();

      var curr = order;

      MyLogger.Logger.LogInformation("Inside tracking order {orderId}", order.orderId);
      await Task.Delay(1000);
      while (true) {
        MyLogger.Logger.LogInformation("Start updating order {orderId}", curr.orderId);
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
          MyLogger.Logger.LogInformation("Order {orderId} filled", curr.orderId);
          curr.closedAt = orderResp.Data.UpdateTime;
          await curr.Update();
          break;
        }

        await curr.Update();

        var newPrice = Math.Round(
          (await client.UsdFuturesApi.ExchangeData.GetPriceAsync(curr.symbol)).Data.Price,
          filter.PricePrecision
        );
        var editResp = await client.UsdFuturesApi.Trading.EditOrderAsync(
          symbol: order.symbol,
          side: order.side,
          quantity: orderResp.Data.Quantity,
          price: newPrice,
          orderId: curr.orderId
        );

        var prevPrice = curr.price;
        var next = LiveTraderOrder.From(
          order.traderId,
          order.tradeId,
          editResp,
          order.orderId,
          curr.orderId
        );
        next.Insert();
        if (editResp.Error != null) {
          MyLogger.Logger.LogError("Error while editing order {orderId}: {Error}", curr.orderId, editResp.Error);
        } else {
          MyLogger.Logger.LogInformation("Order {orderId} edited price {prevPrice} to {newPrice}", curr.orderId, prevPrice, newPrice);
          curr = next;
        }


        await Task.Delay(1500);
      }
    });
  }
}
