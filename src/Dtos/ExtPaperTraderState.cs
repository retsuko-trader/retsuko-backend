using Retsuko.Core;

namespace Retsuko.Dtos;

public record struct ExtPaperTraderState {
  public string id { get; init; }
  public string name { get; init; }
  public string description { get; init; }
  public DateTime created_at { get; init; }
  public DateTime updated_at { get; init; }
  public DateTime? ended_at { get; init; }
  public PapertraderConfig config { get; init; }
  public TraderMetrics metrics { get; init; }

  public ExtPaperTraderState(PaperTraderState state) {
    id = state.id;
    name = state.name;
    description = state.description;
    created_at = state.created_at;
    updated_at = state.updated_at;
    ended_at = state.ended_at;
    config = state.Config;
    metrics = state.Metrics;
  }
}
