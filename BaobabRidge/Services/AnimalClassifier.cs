namespace BaobabRidge.Services;

/// <summary>
/// Maps a Recovery Score onto the animal's Status and Housing Unit.
/// The classification logic lives in exactly one method (Classify) and is
/// called by both the Add and the Update features so the bands can never
/// drift apart. Scores are whole numbers so the bands have no gaps.
/// </summary>
public static class AnimalClassifier
{
    public const string Critical = "Critical";
    public const string Serious = "Serious";
    public const string Stable = "Stable";
    public const string Recovering = "Recovering";
    public const string ReleaseReady = "Release-Ready";

    /// <summary>
    /// All five status names in the order they are reported, so the summary
    /// always lists every status, including those with a count of zero.
    /// </summary>
    public static readonly string[] AllStatuses =
    {
        Critical, Serious, Stable, Recovering, ReleaseReady
    };

    /// <summary>
    /// Calculates the Status and Housing Unit for a given recovery score.
    /// This is the single source of truth for the classification bands:
    /// 0-19 Critical, 20-39 Serious, 40-59 Stable, 60-79 Recovering,
    /// 80-100 Release-Ready.
    /// </summary>
    /// <param name="recoveryScore">A whole number from 0 to 100.</param>
    /// <returns>A tuple of the status name and the housing unit name.</returns>
    public static (string Status, string HousingUnit) Classify(int recoveryScore)
    {
        if (recoveryScore < 20)
            return (Critical, "Intensive Care Unit");

        if (recoveryScore < 40)
            return (Serious, "High-Dependency Ward");

        if (recoveryScore < 60)
            return (Stable, "Recovery Ward");

        if (recoveryScore < 80)
            return (Recovering, "Outdoor Enclosure");

        return (ReleaseReady, "Pre-Release Camp");
    }
}
