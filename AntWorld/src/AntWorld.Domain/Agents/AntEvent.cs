namespace AntWorld.Domain;
public readonly record struct AntEvent(long Tick,int AntId,AntTask Action,int? TargetId);
