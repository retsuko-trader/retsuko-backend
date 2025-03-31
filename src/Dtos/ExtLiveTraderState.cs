using Retsuko.Core;

namespace Retsuko.Dtos;

public record struct ExtLiveTraderState {
  public string id { get; init; }
  public string name { get; init; }
  public string description { get; init; }
  public DateTime createdAt { get; init; }
  public DateTime updatedAt { get; init; }
  public DateTime? endedAt { get; init; }
  public LiveTraderConfig config { get; init; }
  public TraderMetrics metrics { get; init; }

  public ExtLiveTraderState(LiveTraderState state) {
    id = state.id;
    name = state.name;
    description = state.description;
    createdAt = state.createdAt;
    updatedAt = state.updatedAt;
    endedAt = state.endedAt;
    config = state.Config;
    metrics = state.Metrics;
  }
}
