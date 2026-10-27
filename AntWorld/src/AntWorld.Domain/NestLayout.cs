namespace AntWorld.Domain;

/// <summary>Logical nest topology. Physical access remains authoritative in WorldMap.</summary>
public enum NestChamberType : byte { Queen, Brood, FoodStorage, Rest, Fungus, Waste, General }
public enum NestStructureStatus : byte { Planned, InProgress, Completed, Blocked, Cancelled, WaitingDependencies, Available }

/// <summary>
/// Topology is descriptive: a planned edge never implies a traversable path.
/// All completion transitions must be checked against the physical world.
/// </summary>
public sealed class NestLayout
{
    public int ColonyId { get; }
    public List<NestChamber> Chambers { get; } = [];
    public List<NestTunnel> Tunnels { get; } = [];
    public List<NestEntrance> Entrances { get; } = [];
    public int Revision { get; private set; }
    public int QueenChamberId { get; set; } = 1;
    public int BroodChamberId { get; set; } = 2;
    public int FoodStorageChamberId { get; set; } = 3;
    public NestConstructionPlan Construction { get; } = new();
    public List<string> Diagnostics { get; } = [];
    public NestChamber? Designated(int id)
    {
        foreach(var chamber in Chambers)if(chamber.Id==id&&chamber.Status==NestStructureStatus.Completed)return chamber;
        return null;
    }
    public void Touch() => Revision++;
    public void RestoreRevision(int revision) { ArgumentOutOfRangeException.ThrowIfNegative(revision); Revision=revision; }


    public NestLayout(int colonyId) => ColonyId = colonyId;

    public void AddChamber(NestChamber chamber)
    {
        if (Chambers.Any(c => c.Id == chamber.Id))
            throw new ArgumentException("Duplicate chamber ID.", nameof(chamber));
        if (chamber.RadiusX < 1 || chamber.RadiusY < 1)
            throw new ArgumentOutOfRangeException(nameof(chamber));
        Chambers.Add(chamber);
        Revision++;
    }

    public void AddTunnel(NestTunnel tunnel)
    {
        if (Tunnels.Any(t => t.Id == tunnel.Id))
            throw new ArgumentException("Duplicate tunnel ID.", nameof(tunnel));
        if (tunnel.FromChamberId == tunnel.ToChamberId ||
            !Chambers.Any(c => c.Id == tunnel.FromChamberId) ||
            !Chambers.Any(c => c.Id == tunnel.ToChamberId))
            throw new ArgumentException("Tunnel endpoints must be distinct existing chambers.", nameof(tunnel));
        if (Tunnels.Any(t => Math.Min(t.FromChamberId, t.ToChamberId) == Math.Min(tunnel.FromChamberId, tunnel.ToChamberId) &&
                             Math.Max(t.FromChamberId, t.ToChamberId) == Math.Max(tunnel.FromChamberId, tunnel.ToChamberId)))
            throw new ArgumentException("Duplicate chamber connection.", nameof(tunnel));
        Tunnels.Add(tunnel);
        Revision++;
    }

    public void AddEntrance(NestEntrance entrance)
    {
        if (Entrances.Any(e => e.Id == entrance.Id))
            throw new ArgumentException("Duplicate entrance ID.", nameof(entrance));
        if (!Chambers.Any(c => c.Id == entrance.ChamberId))
            throw new ArgumentException("Entrance must connect to an existing chamber.", nameof(entrance));
        Entrances.Add(entrance);
        Revision++;
    }

    public bool IsConnected(bool completedOnly = false)
    {
        var nodes = Chambers.Where(c => c.Status != NestStructureStatus.Cancelled && (!completedOnly || c.Status == NestStructureStatus.Completed)).Select(c => c.Id).ToHashSet();
        if (nodes.Count <= 1) return true;
        var adjacency = nodes.ToDictionary(id => id, _ => new List<int>());
        foreach (var t in Tunnels.Where(t => t.Status != NestStructureStatus.Cancelled && (!completedOnly || t.Status == NestStructureStatus.Completed)))
        {
            if (!nodes.Contains(t.FromChamberId) || !nodes.Contains(t.ToChamberId)) continue;
            adjacency[t.FromChamberId].Add(t.ToChamberId);
            adjacency[t.ToChamberId].Add(t.FromChamberId);
        }
        var seen = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(nodes.First());
        seen.Add(queue.Peek());
        while (queue.TryDequeue(out var id))
            foreach (var next in adjacency[id])
                if (seen.Add(next)) queue.Enqueue(next);
        return seen.Count == nodes.Count;
    }

    public double AlternativeRouteRatio(bool completedOnly = false)
    {
        var nodes = Chambers.Where(c => c.Status != NestStructureStatus.Cancelled && (!completedOnly || c.Status == NestStructureStatus.Completed)).Select(c => c.Id).ToHashSet();
        var edges = Tunnels.Count(t => t.Status != NestStructureStatus.Cancelled && (!completedOnly || t.Status == NestStructureStatus.Completed) &&
                                       nodes.Contains(t.FromChamberId) && nodes.Contains(t.ToChamberId));
        if (edges == 0 || !IsConnected(completedOnly)) return 0;
        return Math.Max(0, edges - nodes.Count + 1) / (double)edges;
    }
}
