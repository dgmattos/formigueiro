namespace AntWorld.Domain;
public sealed record NestSpeciesProfile(double HorizontalBias=.65,double DepthBias=.25,double RockCost=3,int DesiredEntrances=2)
{
    public void Validate()
    {
        if (!double.IsFinite(HorizontalBias)||HorizontalBias is <0 or >1||!double.IsFinite(DepthBias)||DepthBias is <-1 or >1||
            !double.IsFinite(RockCost)||RockCost is <=0 or >100||DesiredEntrances is <1 or >5) throw new ArgumentException("Perfil de ninho inválido.");
    }
}
