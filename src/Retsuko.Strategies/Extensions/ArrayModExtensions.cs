namespace Retsuko.Strategies;

public static class ArrayModExtension {
  public static ref T GetByMod<T>(this T[] array, int index) {
    return ref array[(index % array.Length + array.Length) % array.Length];
  }
}
