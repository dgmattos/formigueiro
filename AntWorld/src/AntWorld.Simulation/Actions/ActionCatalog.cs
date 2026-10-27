using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class ActionCatalog
{
    private readonly AntAction[] actions = [new RestAction(),new MoveAction(),new ExploreAction(),new CollectAction(),new CarryAction(),new DepositAction(),new DigAction(),
        new BuildAction(),new AttackAction(),new DefendAction(),new FleeAction(),new FeedLarvaAction(),new RecoverHealthAction(),new LayEggAction(),new FeedAction(),new DrinkAction()];
    public AntAction Get(AntTask task) => actions[(int)task];
}
