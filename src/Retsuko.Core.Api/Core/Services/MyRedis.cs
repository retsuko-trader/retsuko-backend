using StackExchange.Redis;

namespace Retsuko.Core;

public static class MyRedis {
  public static ConnectionMultiplexer Connection { get; private set; }

  static MyRedis() {
    Connection = ConnectionMultiplexer.Connect(new ConfigurationOptions {
      Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
      EndPoints = { $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}" },
    });
  }
}
