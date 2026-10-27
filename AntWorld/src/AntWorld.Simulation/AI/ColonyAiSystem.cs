using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class ColonyAiSystem
{
    private readonly Dictionary<int,(int Population,int Brood,int Threats)> counts=[];
    private readonly Ant[] neighbors=new Ant[32];
    private readonly RecognitionService recognition=new();
    public void Step(SimulationState state,AgentSpatialIndex index)
    {
        counts.Clear();
        foreach(var a in state.Ants)
        {
            var c=counts.GetValueOrDefault(a.ColonyId);
            counts[a.ColonyId]=(c.Population+1,c.Brood+(a.IsTransportableBrood?1:0),c.Threats);
        }
        foreach(var colony in state.Colonies)
        {
            var c=counts.GetValueOrDefault(colony.Id);
            var threats=0;
            if(colony.Queen is {} queen)
            {
                var n=index.Nearby(state.World,colony.Nest,neighbors,4,(int)(state.Tick%int.MaxValue));
                for(var i=0;i<n;i++) if(recognition.Recognize(queen,neighbors[i],state).PossibleThreat) threats++;
            }
            var food=Math.Clamp(1-colony.Food/Math.Max(1,c.Population*5.0),0,1);
            colony.Priorities=new(food,Math.Min(1,threats*.2),Math.Clamp(c.Population/100.0,0,.8),Math.Min(1,c.Brood/Math.Max(1,c.Population*.2)),Math.Clamp(.3+food*.5-state.RainIntensity*.2,0,1));
            // Slow shared chemical drift tied to the environment; individual assimilation is gradual.
            if(state.Tick%SimulationState.TicksPerDay==0)
            {
                var bit=(int)(state.Tick/SimulationState.TicksPerDay%64);
                colony.Signature=colony.Signature with { PatternB=colony.Signature.PatternB^(1UL<<bit) };
            }
        }
    }
}
