using System.Collections;
namespace AntWorld.Domain;
// Four sparse concentration layers; zero cells cost no storage. No particle objects.
public sealed class PheromoneField(int length) : IEnumerable<double>
{
    private readonly Dictionary<int, double>[] layers = [[],[],[],[]];
    private readonly List<int> scratch = [];
    public int Length { get; } = length;
    public int ActiveCellCount => layers[0].Count+layers[1].Count+layers[2].Count+layers[3].Count;
    public long LastUpdateTick { get; set; }
    public double this[int index] { get => Get(PheromoneType.FoodTrail,index); set => Set(PheromoneType.FoodTrail,index,value); }
    public double Get(PheromoneType type,int index) => layers[(int)type].GetValueOrDefault(index);
    public IEnumerable<KeyValuePair<int,double>> Entries(PheromoneType type) => layers[(int)type];
    public void Set(PheromoneType type,int index,double value)
    {
        if (!Enum.IsDefined(type) || index < 0 || index >= Length || !double.IsFinite(value) || value < 0 || value > 15) throw new ArgumentOutOfRangeException(nameof(value));
        if (value < .001) layers[(int)type].Remove(index); else layers[(int)type][index] = value;
    }
    public void Deposit(PheromoneType type,int index,double amount) => Set(type,index,Math.Min(15,Get(type,index)+amount));
    public void Evaporate(long tick,double rain)
    {
        if (tick <= LastUpdateTick) return;
        var seconds = (tick-LastUpdateTick)*SimulationState.TickSeconds;
        for(var t=0;t<4;t++)
        {
            var factor=Math.Exp(-(t==1 ? .12 : .015)*(1+rain)*seconds);
            scratch.Clear(); scratch.AddRange(layers[t].Keys);
            foreach(var index in scratch) Set((PheromoneType)t,index,layers[t][index]*factor);
        }
        LastUpdateTick=tick;
    }
    public IEnumerator<double> GetEnumerator() { for(var i=0;i<Length;i++) yield return this[i]; }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
