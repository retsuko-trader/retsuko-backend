namespace Retsuko.Strategies.Core;

public interface ISerializable {
  string Serialize();
  void Deserialize(string data);
}

public interface ISerializable<T> {
  T Serialize();
  void Deserialize(T data);
}
