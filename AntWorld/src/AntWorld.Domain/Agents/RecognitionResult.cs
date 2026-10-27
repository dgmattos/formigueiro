namespace AntWorld.Domain;
public readonly record struct RecognitionResult(FamiliarityLevel Familiarity, double ChemicalCompatibility, bool KnownMembership,
    double Confidence, SocialClassification Classification, bool PossibleThreat);
