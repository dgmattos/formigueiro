using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class RecognitionService
{
    public RecognitionResult Recognize(Ant observer, Ant target, SimulationState state)
    {
        var chemical = observer.Signature.Similarity(target.Signature);
        var nestmate = observer.ColonyId == target.ColonyId;
        var close = observer.MotherId == target.Id || target.MotherId == observer.Id || observer.FatherId == target.Id || target.FatherId == observer.Id ||
            observer.MotherId.HasValue && observer.MotherId == target.MotherId || observer.FatherId.HasValue && observer.FatherId == target.FatherId;
        var relation = state.Relations.Get(observer.ColonyId,target.ColonyId);
        var familiar = close ? FamiliarityLevel.CloseRelative : nestmate ? FamiliarityLevel.Nestmate :
            observer.LineageId != 0 && observer.LineageId == target.LineageId || chemical > .88 ? FamiliarityLevel.Familiar :
            observer.SpeciesId == target.SpeciesId ? FamiliarityLevel.SameSpecies : FamiliarityLevel.Unknown;
        var territorialThreat=false;
        if(!nestmate && relation==ColonyRelation.Unknown && chemical<.65 && observer.Genes.Aggression>.6)
        {
            Colony? colony=null;
            foreach(var candidate in state.Colonies)
                if(candidate.Id==observer.ColonyId) { colony=candidate;break; }
            territorialThreat=colony is not null && state.World.Distance(target.Position,colony.Nest)<=4 &&
                state.Pheromones[colony.Id].Get(PheromoneType.Territory,state.World.Index(target.Position))>=2;
        }
        var threat = !nestmate && (relation == ColonyRelation.Hostile || territorialThreat);
        var classification = nestmate ? SocialClassification.Nestmate : relation is ColonyRelation.Allied or ColonyRelation.Tolerated ? SocialClassification.Ally :
            threat ? SocialClassification.Threat : close ? SocialClassification.Relative : SocialClassification.Stranger;
        return new(familiar,chemical,nestmate,nestmate ? .9+.1*chemical : .5+.4*Math.Abs(chemical-.5),classification,threat);
    }
}
