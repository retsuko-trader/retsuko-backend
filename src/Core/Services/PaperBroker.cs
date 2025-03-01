using System.Dynamic;
using System.Text.Json;

namespace Retsuko.Core;

public class PaperBroker: IBroker, ISerializable {
  record struct Position(
    double ts,
    PositionKind kind,
    double confidence
  );

  PaperBrokerConfig config;
  Portfolio portfolio;
  Position? position;

  public double InitialBalance => config.initialBalance;

  public PaperBroker(PaperBrokerConfig config) {
    this.config = config;
    portfolio = new Portfolio(0, config.initialBalance, config.initialBalance);
    position = null;
  }

  public async Task<Trade?> HandleAdvice(Candle candle, Signal signal) {
    var kind = signal.kind;
    var confidence = signal.confidence;

    var handled = false;
    if (kind == SignalKind.@long) {
      handled = HandleLong(candle, confidence);
    } else if (kind == SignalKind.@short) {
      handled = HandleShort(candle, confidence);
    } else if (kind == SignalKind.closeLong) {
      if (position?.kind == PositionKind.@long) {
        sell(candle.close, new SellOption { assetDesired = 0 });
        position = null;
        handled = true;
      }
    } else if (kind == SignalKind.closeShort) {
      if (position?.kind == PositionKind.@short) {
        buy(candle.close, new BuyOption { currencyDesired = portfolio.totalBalance });
        position = null;
        handled = true;
      }
    }

    portfolio.totalBalance = totalBalance(candle.close);
    if (!handled) {
      return null;
    }

    return new Trade(
      candle.ts,
      kind,
      confidence,
      portfolio.asset,
      portfolio.currency,
      candle.close,
      0
    );
  }

  protected bool HandleLong(Candle candle, double confidence) {
    var prevConfidence = position?.confidence ?? 0;
    if (position?.kind == PositionKind.@long && prevConfidence >= confidence) {
      if (config.validTradeOnly) {
        return false;
      }

      portfolio.totalBalance = totalBalance(candle.close);
      return true;
    }

    buy(candle.close, new BuyOption { currencyDesired = portfolio.totalBalance * (1 - confidence) });
    position = new Position(candle.ts.ToUnixTimestamp(), PositionKind.@long, confidence);
    return true;
  }

  protected bool HandleShort(Candle candle, double confidence) {
    var prevConfidence = position?.confidence ?? 0;
    if (position?.kind == PositionKind.@short && prevConfidence >= confidence) {
      if (config.validTradeOnly) {
        return false;
      }

      portfolio.totalBalance = totalBalance(candle.close);
      return true;
    }

    if (config.enableMargin) {
      var minAsset = -totalBalance(candle.close) / candle.close;
      var assetDesired = minAsset * confidence;
      sell(candle.close, new SellOption { assetDesired = assetDesired });
    } else {
      if (portfolio.asset <= 0) {
        return false;
      }
      sell(candle.close, new SellOption { assetDesired = 0 });
    }

    position = new Position(candle.ts.ToUnixTimestamp(), PositionKind.@short, confidence);
    return true;
  }

  protected double extractFee(double amount) {
    return Math.Floor(amount * 1e8 * (1 - config.fee)) / 1e8;
  }

  protected double addFee(double amount) {
    return Math.Ceiling(amount * 1e8 / (1 - config.fee)) / 1e8;
  }

  protected double totalBalance(double close) {
    return portfolio.currency + portfolio.asset * close;
  }

  protected void buy(double close, params BuyOption[] options) {
    if (options.Length == 0) {
      return;
    }

    var totalAmount = 0.0;
    var totalPrice = 0.0;

    foreach (var option in options) {
      var amount = 0.0;
      var price = 0.0;

      if (option.asset.HasValue) {
        amount += option.asset.Value;
        price += addFee(option.asset.Value * close);
      }
      if (option.assetBeforeFee.HasValue) {
        var asset = extractFee(option.assetBeforeFee.Value);
        amount += asset;
        price += asset * close;
      }
      if (option.currency.HasValue) {
        amount += extractFee(option.currency.Value / close);
        price += option.currency.Value;
      }

      if (option.assetDesired.HasValue) {
        amount = option.assetDesired.Value - portfolio.asset;
        price = addFee(amount * close);
      }
      if (option.currencyDesired.HasValue) {
        price = portfolio.currency - option.currencyDesired.Value;
        amount = extractFee(price / close);
      }

      totalAmount += amount;
      totalPrice += price;
    }

    portfolio.asset += totalAmount;
    portfolio.currency -= totalPrice;
    portfolio.totalBalance = totalBalance(close);
  }

  protected void sell(double close, params SellOption[] options) {
    if (options.Length == 0) {
      return;
    }

    var totalAmount = 0.0;
    var totalPrice = 0.0;

    foreach (var option in options) {
      var amount = 0.0;
      var price = 0.0;

      if (option.asset.HasValue) {
        amount += option.asset.Value;
        price += extractFee(option.asset.Value * close);
      }
      if (option.currency.HasValue) {
        amount += extractFee(option.currency.Value / close);
        price += option.currency.Value;
      }
      if (option.currencyBeforeFee.HasValue) {
        var asset = option.currencyBeforeFee.Value / close;
        amount += asset;
        price += addFee(asset * close);
      }

      if (option.assetDesired.HasValue) {
        amount = portfolio.asset - option.assetDesired.Value;
        price = extractFee(amount * close);
      }
      if (option.currencyDesired.HasValue) {
        price = option.currencyDesired.Value - portfolio.currency;
        amount = extractFee(price / close);
      }

      totalAmount += amount;
      totalPrice += price;
    }

    portfolio.asset -= totalAmount;
    portfolio.currency += totalPrice;
    portfolio.totalBalance = totalBalance(close);
  }

  public Portfolio GetPortfolio() {
    return portfolio;
  }

  public string Serialize() {
    return JsonSerializer.Serialize(new {
      config,
      portfolio,
      position
    });
  }

  public void Deserialize(string data) {
    dynamic? obj = JsonSerializer.Deserialize<ExpandoObject>(data);
    if (obj == null) {
      return;
    }

    config = obj.config;
    portfolio = obj.portfolio;
    position = obj.position;
  }

  protected record struct BuyOption {
    public double? asset;
    public double? assetBeforeFee;
    public double? assetDesired;
    public double? currency;
    public double? currencyDesired;
  }

  protected record struct SellOption {
    public double? asset;
    public double? assetDesired;
    public double? currency;
    public double? currencyBeforeFee;
    public double? currencyDesired;
  }
}
