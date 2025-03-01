namespace Retsuko.Core;

public interface ISerializable {
  string Serialize();
  void Deserialize(string data);
}
