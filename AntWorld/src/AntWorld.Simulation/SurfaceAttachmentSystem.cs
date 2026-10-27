using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class SurfaceAttachmentSystem
{
    public static void Step(WorldMap world, Ant ant)
    {
        if (!ant.IsAlive || ant.CarrierId.HasValue) return;
        ant.Support = world.ContactSide(ant.Position, ant.Support);
        if (ant.Support != SupportSide.None) return;
        // Loss of the last support causes a fall, never autonomous walking through unsupported air.
        var below = new CellPosition(ant.Position.X, ant.Position.Y + 1);
        ant.Movement = MovementState.Falling; ant.MovementCredit = 0;
        if (!world.IsOpen(below)) return;
        ant.PreviousPosition = ant.Position; ant.Position = world.Normalize(below);
        ant.Support = world.ContactSide(ant.Position);
    }
}
