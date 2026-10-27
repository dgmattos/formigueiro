using AntWorld.Domain;
namespace AntWorld.Infrastructure;

public sealed class BinarySnapshotFile
{
    private readonly BinarySnapshotCodec codec = new();
    public void Save(string path, SimulationState state)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, codec.Encode(state));
            File.Move(temporary, fullPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public SimulationState Load(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > BinarySnapshotCodec.MaximumBytes + 48) throw new InvalidDataException("Arquivo excede o limite de snapshot.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return codec.Decode(bytes);
    }
}
