using System.Globalization;
using System.Xml;
using AntWorld.Domain;
namespace AntWorld.Infrastructure;

public sealed class WorldSvgExporter
{
    public void Export(string path, SimulationState state)
    {
        var world = state.World;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var w = XmlWriter.Create(path, new XmlWriterSettings { Indent = false });
        w.WriteStartElement("svg", "http://www.w3.org/2000/svg");
        w.WriteAttributeString("viewBox", $"0 0 {world.Width * 6} {world.Height * 6 + 60}");
        w.WriteStartElement("title"); w.WriteString($"AntWorld • seed {world.Configuration.Seed} • tick {state.Tick}"); w.WriteEndElement();
        Rect(w, 0, 0, world.Width * 6, world.Height * 6 + 60, "#142b35");
        for (var i = 0; i < world.Terrain.Length; i++)
        {
            var x = i % world.Width; var y = i / world.Width;
            var color = world.Terrain[i] switch
            {
                TerrainKind.Soil => "#67513a", TerrainKind.Clay => "#473c35", TerrainKind.Rock => "#31343b",
                TerrainKind.Sand => "#a38b51", TerrainKind.Tunnel => "#191b22", _ => y >= world.Surface[x] ? "#191b22" : null
            };
            if (color is not null) Rect(w, x * 6, y * 6 + 60, 6, 6, color);
            if (y == world.Surface[x]) Rect(w, x * 6, y * 6 + 60, 6, 2, world.Biomes[x] switch
            { BiomeKind.Desert => "#d0b56a", BiomeKind.Tundra => "#d6e9e4", BiomeKind.Swamp => "#628467", _ => "#83a54d" });
            if (world.Food[i] > .5) Rect(w, x * 6 + 1, y * 6 + 62, 4, 4, "#e9c86d");
        }
        var colonies = state.Colonies.ToDictionary(c => c.Id);
        foreach (var ant in state.Ants) Rect(w, ant.Position.X * 6 + 1, ant.Position.Y * 6 + 62, 4, 2, colonies[ant.ColonyId].Species == 0 ? "#ff947b" : "#72dfb1");
        foreach (var colony in state.Colonies)
        {
            Rect(w, colony.Nest.X * 6 - 4, colony.Nest.Y * 6 + 59, 10, 7, colony.IsAlive ? (colony.Species == 0 ? "#ff947b" : "#72dfb1") : "#888888");
        }
        w.WriteStartElement("text"); w.WriteAttributeString("x", "20"); w.WriteAttributeString("y", "35"); w.WriteAttributeString("fill", "#e6ece5"); w.WriteAttributeString("font-family", "monospace"); w.WriteAttributeString("font-size", "18");
        w.WriteString($"ANTWORLD  /  SEED {world.Configuration.Seed}  /  TICK {state.Tick}  /  {state.Ants.Count} FORMIGAS"); w.WriteEndElement();
        w.WriteEndElement();
    }
    private static void Rect(XmlWriter w, int x, int y, int width, int height, string fill)
    {
        w.WriteStartElement("rect");
        w.WriteAttributeString("x", x.ToString(CultureInfo.InvariantCulture)); w.WriteAttributeString("y", y.ToString(CultureInfo.InvariantCulture));
        w.WriteAttributeString("width", width.ToString(CultureInfo.InvariantCulture)); w.WriteAttributeString("height", height.ToString(CultureInfo.InvariantCulture)); w.WriteAttributeString("fill", fill);
        w.WriteEndElement();
    }
}
