public static class TraderIdHelper {
  public static string GeneratePaperTraderId() {
    return $"pt-{new Visus.Cuid.Cuid2()}";
  }

  public static string GenerateLiveTraderId(bool test) {
    var prefix = test ? "ltt" : "ltl";
    return $"{prefix}-{new Visus.Cuid.Cuid2()}";
  }

  public static IdKind Parse(string id) {
    if (id.StartsWith("pt-")) {
      return IdKind.PaperTrader;
    }

    if (id.StartsWith("ltt-")) {
      return IdKind.LiveTraderTest;
    }

    if (id.StartsWith("ltl-")) {
      return IdKind.LiveTraderLive;
    }

    throw new ArgumentException("Invalid trader id");
  }

  public enum IdKind {
    PaperTrader,
    LiveTraderTest,
    LiveTraderLive,
  }
}