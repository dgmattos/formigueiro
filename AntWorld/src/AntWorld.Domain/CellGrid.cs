namespace AntWorld.Domain;

// Compatibility facade over chunk-backed data, never a second dense copy.
public sealed class CellGrid<T>(int length, Func<int, T> read, Action<int, T> write) : IReadOnlyList<T>
{
    public int Length => length;
    public int Count => length;
    public T this[int index]
    {
        get { Validate(index); return read(index); }
        set { Validate(index); write(index, value); }
    }
    private void Validate(int index) { if ((uint)index >= (uint)length) throw new ArgumentOutOfRangeException(nameof(index)); }
    public IEnumerator<T> GetEnumerator() { for (var i = 0; i < length; i++) yield return read(i); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
