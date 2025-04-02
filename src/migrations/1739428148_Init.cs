namespace Retsuko.Migrations;

public static partial class Migrations {
  public static async Task CreateCandle() {
    using var command = Database.Candle.CreateCommand();
    command.CommandText = @"CREATE TABLE IF NOT EXISTS candle (
      market TEXT,
      symbol TEXT,
      interval INT,
      ts TIMESTAMP,
      open DOUBLE,
      high DOUBLE,
      low DOUBLE,
      close DOUBLE,
      volume DOUBLE,
      PRIMARY KEY (market, symbol, interval, ts)
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = "CREATE INDEX IF NOT EXISTS candle_symbol_index ON candle (market, symbol, interval)";
    await command.ExecuteNonQueryAsync();
  }

  public static async Task CreateBacktest() {
    using var command = Database.Backtest.CreateCommand();
    command.CommandText = @"CREATE TABLE IF NOT EXISTS backtest_run (
      id VARCHAR PRIMARY KEY,
      name TEXT NOT NULL,
      description TEXT NOT NULL,
      created_at TIMESTAMP NOT NULL,
      ended_at TIMESTAMP,
      datasets TEXT NOT NULL,
      strategies TEXT NOT NULL,
      broker_config TEXT NOT NULL,
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS backtest_single (
      id VARCHAR PRIMARY KEY,
      run_id VARCHAR NOT NULL,
      dataset TEXT NOT NULL,
      dataset_start TIMESTAMP NOT NULL,
      dataset_end TIMESTAMP NOT NULL,
      strategy_name TEXT NOT NULL,
      strategy_config TEXT NOT NULL,
      broker_config TEXT NOT NULL,
      metrics TEXT NOT NULL,
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS backtest_trade (
        single_id VARCHAR,
        ts TIMESTAMP NOT NULL,
        signal TEXT NOT NULL,
        confidence DOUBLE NOT NULL,
        asset DOUBLE NOT NULL,
        currency DOUBLE NOT NULL,
        price DOUBLE NOT NULL,
        profit DOUBLE,
        PRIMARY KEY (single_id, ts)
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = "CREATE INDEX IF NOT EXISTS backtest_trade_backtest_single_id ON backtest_trade (single_id)";
    await command.ExecuteNonQueryAsync();
  }

  public static async Task CreatePaperTrader() {
    using var command = Database.PaperTrader.CreateCommand();
    command.CommandText = @"CREATE TABLE IF NOT EXISTS paper_trader (
      id VARCHAR PRIMARY KEY,
      name TEXT NOT NULL,
      description TEXT NOT NULL,
      created_at TIMESTAMP NOT NULL,
      updated_at TIMESTAMP NOT NULL,
      ended_at TIMESTAMP,
      dataset TEXT NOT NULL,
      strategy_name TEXT NOT NULL,
      strategy_config TEXT NOT NULL,
      strategy_state TEXT NOT NULL,
      broker_config TEXT NOT NULL,
      broker_state TEXT NOT NULL,
      metrics TEXT NOT NULL,
      states TEXT NOT NULL,
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS paper_trader_trade (
      id VARCHAR PRIMARY KEY,
      trader_id VARCHAR NOT NULL,
      ts TIMESTAMP NOT NULL,
      signal TEXT NOT NULL,
      confidence DOUBLE NOT NULL,
      asset DOUBLE NOT NULL,
      currency DOUBLE NOT NULL,
      price DOUBLE NOT NULL,
      profit DOUBLE,
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = "CREATE INDEX IF NOT EXISTS paper_trader_trade_trader_id ON paper_trader_trade (trader_id)";
    await command.ExecuteNonQueryAsync();
  }

  public static async Task CreateLiveTrader() {
    using var command = Database.LiveTrader.CreateCommand();
    command.CommandText = @"CREATE TABLE IF NOT EXISTS live_trader (
      id VARCHAR PRIMARY KEY,
      name TEXT NOT NULL,
      description TEXT NOT NULL,
      created_at TIMESTAMP NOT NULL,
      updated_at TIMESTAMP NOT NULL,
      ended_at TIMESTAMP,
      dataset TEXT NOT NULL,
      strategy_name TEXT NOT NULL,
      strategy_config TEXT NOT NULL,
      strategy_state TEXT NOT NULL,
      broker_config TEXT NOT NULL,
      broker_state TEXT NOT NULL,
      metrics TEXT NOT NULL,
      states TEXT NOT NULL
    )";
    await command.ExecuteNonQueryAsync();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS live_trader_trade (
      id VARCHAR PRIMARY KEY,
      trader_id VARCHAR NOT NULL,
      ts TIMESTAMP NOT NULL,
      signal TEXT NOT NULL,
      confidence DOUBLE NOT NULL,
      order_id LONG,
      asset DOUBLE NOT NULL,
      currency DOUBLE NOT NULL,
      price DOUBLE NOT NULL,
      profit DOUBLE
    )";
    await command.ExecuteNonQueryAsync();
    command.CommandText = "CREATE INDEX IF NOT EXISTS live_trader_trade_trader_id ON live_trader_trade (trader_id)";
    await command.ExecuteNonQueryAsync();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS live_trader_order (
      trader_id VARCHAR NOT NULL,
      trade_id VARCHAR NOT NULL,
      order_id LONG NOT NULL,
      root_order_id LONG,
      prev_order_id LONG,
      next_order_id LONG,
      error TEXT,
      closed_at TIMESTAMP,
      cancelled_at TIMESTAMP,
      symbol TEXT NOT NULL,
      pair TEXT NOT NULL,
      price DOUBLE NOT NULL,
      average_price DOUBLE NOT NULL,
      quantity_filled DOUBLE NOT NULL,
      quantity DOUBLE NOT NULL,
      status INT NOT NULL,
      side INT NOT NULL,
      time_in_force INT NOT NULL,
      type INT NOT NULL,
      order_type INT NOT NULL,
      update_time TIMESTAMP NOT NULL,
      create_time TIMESTAMP NOT NULL,
      position_side INT NOT NULL,
      PRIMARY KEY (trader_id, trade_id, order_id)
    )";
    await command.ExecuteNonQueryAsync();
    command.CommandText = "CREATE INDEX IF NOT EXISTS live_trader_order_trader_id ON live_trader_order (trader_id)";
    await command.ExecuteNonQueryAsync();
    command.CommandText = "CREATE INDEX IF NOT EXISTS live_trader_order_root_order_id ON live_trader_order (root_order_id)";
    await command.ExecuteNonQueryAsync();
  }
}
