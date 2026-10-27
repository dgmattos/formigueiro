namespace AntWorld.Domain;

public static class ResourceIds
{
    public const ushort Food = 1, Iron = 2, OrganicMatter = 3, Fungus = 4, RedMushroom = 5, BrownMushroom = 6;
    public static bool IsMushroom(ushort id) => id is RedMushroom or BrownMushroom;
    public static bool IsEdible(ushort id) => id == Food || IsMushroom(id);
}
