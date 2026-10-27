using System.Text.Json;
using System.Text.Json.Serialization;
using AntWorld.Domain;
namespace AntWorld.Infrastructure;

// A bounded, length-prefixed metadata section inside the versioned binary envelope.
// Terrain remains seed + deltas; no generated world cells are duplicated here.
internal static class NestSnapshotCodec
{
    private const int MaximumSectionBytes=4*1024*1024;
    private static readonly JsonSerializerOptions Options=new(){UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    public static void Write<T>(BinaryWriter writer,T value)
    {
        var bytes=JsonSerializer.SerializeToUtf8Bytes(value,Options);
        if(bytes.Length>MaximumSectionBytes)throw new InvalidDataException("Layout de ninho excede limite.");
        writer.Write(bytes.Length);writer.Write(bytes);
    }
    public static T Read<T>(BinaryReader reader)
    {
        var length=WorldSnapshotCodec.Count(reader,MaximumSectionBytes);var bytes=reader.ReadBytes(length);
        if(bytes.Length!=length)throw new EndOfStreamException();
        try{return JsonSerializer.Deserialize<T>(bytes,Options)??throw new InvalidDataException("Metadados do ninho ausentes.");}
        catch(JsonException e){throw new InvalidDataException("Metadados do ninho inválidos.",e);}
    }
    public static void WriteLayout(BinaryWriter writer,NestLayout? layout)
    {
        writer.Write(layout is not null);
        if(layout is null)return;
        // Lists are restored explicitly; runtime graph identity and revision are never inferred.
        Write(writer,new LayoutData(layout.ColonyId,layout.Revision,layout.QueenChamberId,layout.BroodChamberId,layout.FoodStorageChamberId,
            layout.Chambers.ToArray(),layout.Tunnels.ToArray(),layout.Entrances.ToArray(),layout.Construction.Jobs.ToArray(),
            layout.Construction.LastPlanningTick,layout.Construction.Revision,layout.Diagnostics.ToArray()));
    }
    public static NestLayout? ReadLayout(BinaryReader reader)
    {
        if(!reader.ReadBoolean())return null;
        var data=Read<LayoutData>(reader);
        if(data.Chambers is null||data.Tunnels is null||data.Entrances is null||data.Jobs is null||data.Diagnostics is null||
            data.Chambers.Any(c=>c is null||c.Cells is null)||data.Tunnels.Any(t=>t is null||t.Path is null||t.Cells is null)||
            data.Entrances.Any(e=>e is null||e.Path is null)||data.Jobs.Any(j=>j is null))throw new InvalidDataException("Layout incompleto.");
        if(data.Chambers.Length>60||data.Tunnels.Length>256||data.Entrances.Length>5||data.Jobs.Length>65536||data.Diagnostics.Length>64)
            throw new InvalidDataException("Contagens do ninho inválidas.");
        var layout=new NestLayout(data.ColonyId){QueenChamberId=data.Queen,BroodChamberId=data.Brood,FoodStorageChamberId=data.Food};
        foreach(var chamber in data.Chambers)layout.AddChamber(chamber);
        foreach(var tunnel in data.Tunnels)layout.AddTunnel(tunnel);
        foreach(var entrance in data.Entrances)layout.AddEntrance(entrance);
        layout.Construction.Jobs.AddRange(data.Jobs);layout.Construction.LastPlanningTick=data.LastPlanningTick;
        layout.Construction.Revision=data.ConstructionRevision;layout.Diagnostics.AddRange(data.Diagnostics);layout.RestoreRevision(data.Revision);
        return layout;
    }
    private sealed record LayoutData(int ColonyId,int Revision,int Queen,int Brood,int Food,NestChamber[] Chambers,NestTunnel[] Tunnels,
        NestEntrance[] Entrances,NestConstructionJob[] Jobs,long LastPlanningTick,int ConstructionRevision,string[] Diagnostics);
}
