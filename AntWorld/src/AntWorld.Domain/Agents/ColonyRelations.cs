namespace AntWorld.Domain;
public sealed class ColonyRelations
{
    private readonly Dictionary<(int First,int Second), ColonyRelation> entries = [];
    public IEnumerable<KeyValuePair<(int First,int Second),ColonyRelation>> Entries => entries;
    public ColonyRelation Get(int a,int b) => a == b ? ColonyRelation.Allied : entries.GetValueOrDefault(Key(a,b),ColonyRelation.Neutral);
    public void Set(int a,int b,ColonyRelation relation)
    { if (a <= 0 || b <= 0 || a == b || !Enum.IsDefined(relation)) throw new ArgumentException("Relação inválida."); entries[Key(a,b)] = relation; }
    private static (int,int) Key(int a,int b) => a < b ? (a,b) : (b,a);
}
