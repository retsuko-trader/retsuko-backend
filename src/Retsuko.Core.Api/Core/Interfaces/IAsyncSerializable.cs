namespace Retsuko.Core;

public interface IAsyncSerializable {
  Task<string> Serialize();
  Task Deserialize(string data);
}

public interface IAsyncSerializable<T> {
  Task<T> Serialize();
  Task Deserialize(T data);
}
