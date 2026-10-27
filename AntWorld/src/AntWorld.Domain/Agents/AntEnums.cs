namespace AntWorld.Domain;
public enum AntCaste : byte { Queen, Worker, Soldier, MaleReproductive }
public enum LifeStage : byte { Egg, Larva, Pupa, Adult }
public enum BiologicalSex : byte { Female, Male }
public enum MovementState : byte { Idle, Walking, Climbing, Carrying, Fleeing, Attacking, Digging }
public enum AntTask : byte { Rest, Move, Explore, Collect, Carry, Deposit, Dig, Build, Attack, Defend, Flee, FeedLarva, RecoverHealth, LayEgg, Feed, Drink }
public enum SkillType : byte { Digging, Carrying, Combat, Exploration, BroodCare, Construction, Reproduction, Navigation, Defense, ResourceDetection }
public enum FamiliarityLevel : byte { Unknown, SameSpecies, Familiar, Nestmate, CloseRelative }
public enum ColonyRelation : byte { Unknown, Neutral, Tolerated, Allied, Hostile }
public enum SocialClassification : byte { Nestmate, Ally, Relative, Stranger, Threat }
public enum PheromoneType : byte { FoodTrail, Alarm, HomeTrail, Territory }
public enum SimulationLevel : byte { Visible, Nearby, Distant, Aggregate }
[Flags] public enum AntCapability : uint { None=0, Move=1, Explore=2, Collect=4, Carry=8, Dig=16, Build=32, Attack=64, Defend=128, BroodCare=256, Reproduce=512, Feed=1024, Rest=2048, Recover=4096 }
