using System.Globalization;
using System.Text;
using BaobabRidge.Models;

namespace BaobabRidge.Services;

/// <summary>
/// The figures calculated for the Centre manager's summary report. Holding them
/// in one object lets the form display the numbers and lets the writer save the
/// identical figures to summary.txt.
/// </summary>
public class SummaryResult
{
    public int TotalAnimals { get; init; }
    public double AverageAge { get; init; }
    public double AverageRecoveryScore { get; init; }
    public IReadOnlyDictionary<string, int> CountPerStatus { get; init; } =
        new Dictionary<string, int>();
    public bool HasAnimals => TotalAnimals > 0;
}

/// <summary>
/// Calculates the summary report and writes it to summary.txt in the same
/// folder as the running .exe. The file is overwritten each time and uses an
/// exact fixed layout with two-decimal, full-stop averages regardless of the
/// computer's regional settings.
/// </summary>
public static class SummaryService
{
    // Labels are padded to this width so every figure lines up in one column.
    private const int LabelWidth = 26;
    private static readonly string SeparatorLine = new('-', 40);

    /// <summary>
    /// Calculates totals, averages and per-status counts from the given animals.
    /// Both averages are handled safely for an empty list (the caller shows N/A).
    /// </summary>
    public static SummaryResult Calculate(IReadOnlyCollection<Animal> animals)
    {
        int total = animals.Count;
        double averageAge = total > 0 ? animals.Average(a => a.Age) : 0;
        double averageScore = total > 0 ? animals.Average(a => a.RecoveryScore) : 0;

        var counts = new Dictionary<string, int>();
        foreach (string status in AnimalClassifier.AllStatuses)
            counts[status] = animals.Count(a => a.Status == status);

        return new SummaryResult
        {
            TotalAnimals = total,
            AverageAge = averageAge,
            AverageRecoveryScore = averageScore,
            CountPerStatus = counts
        };
    }

    /// <summary>
    /// Builds the exact text of the summary report. Kept separate from writing so
    /// it can be unit-tested without touching the file system.
    /// </summary>
    /// <param name="animals">The animals to summarise.</param>
    /// <param name="generatedAt">The moment the report was requested.</param>
    public static string BuildReport(IReadOnlyCollection<Animal> animals, DateTime generatedAt)
    {
        SummaryResult result = Calculate(animals);

        var sb = new StringBuilder();
        sb.AppendLine("BAOBAB RIDGE WILDLIFE REHABILITATION CENTRE");
        sb.AppendLine("ANIMAL SUMMARY REPORT");
        sb.AppendLine($"Generated: {generatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}");
        sb.AppendLine(SeparatorLine);
        sb.AppendLine(Row("Total animals:", result.TotalAnimals.ToString(CultureInfo.InvariantCulture)));

        // Averages use the invariant culture so the decimal separator is always a full stop.
        sb.AppendLine(Row("Average age (years):",
            result.HasAnimals ? result.AverageAge.ToString("F2", CultureInfo.InvariantCulture) : "N/A"));
        sb.AppendLine(Row("Average recovery score:",
            result.HasAnimals ? result.AverageRecoveryScore.ToString("F2", CultureInfo.InvariantCulture) : "N/A"));

        sb.AppendLine(SeparatorLine);
        sb.AppendLine("Animals per status:");
        foreach (string status in AnimalClassifier.AllStatuses)
        {
            int count = result.CountPerStatus.TryGetValue(status, out int c) ? c : 0;
            sb.AppendLine(Row($"  {status}:", count.ToString(CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds one report line by padding the label so its value starts in the
    /// same column as every other line.
    /// </summary>
    private static string Row(string label, string value) => label.PadRight(LabelWidth) + value;

    /// <summary>
    /// Writes the summary report to summary.txt beside the running .exe,
    /// overwriting any previous file. Returns the path written for confirmation.
    /// </summary>
    public static string WriteReport(IReadOnlyCollection<Animal> animals, DateTime generatedAt)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "summary.txt");
        string content = BuildReport(animals, generatedAt);

        try
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DataFileException(
                "summary.txt could not be saved. It may be open in another program, or you " +
                "may not have permission to write to the application folder.", ex);
        }

        return path;
    }
}
