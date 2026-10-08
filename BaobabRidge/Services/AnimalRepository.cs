using System.Globalization;
using System.Text;
using BaobabRidge.Models;

namespace BaobabRidge.Services;

/// <summary>
/// Reads and writes animals.txt. The file lives in the same folder as the
/// running .exe and holds one animal per line with seven pipe-separated fields:
/// ID | Name | Species | Age | Score | Status | Housing Unit.
/// All file access is wrapped so I/O problems become a friendly
/// <see cref="DataFileException"/> instead of crashing the program.
/// </summary>
public class AnimalRepository
{
    private const char Separator = '|';
    private const int FieldCount = 7;

    /// <summary>The full path of animals.txt beside the running executable.</summary>
    public string FilePath { get; }

    /// <summary>Creates a repository for a specific file path (used by tests).</summary>
    public AnimalRepository(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Creates a repository that stores animals.txt in the application's own folder,
    /// i.e. the same folder as the running .exe (never a machine-specific path).
    /// </summary>
    public static AnimalRepository InApplicationFolder()
    {
        string folder = AppContext.BaseDirectory;
        return new AnimalRepository(Path.Combine(folder, "animals.txt"));
    }

    /// <summary>
    /// Loads every valid record from animals.txt. If the file does not exist it is
    /// created empty and an empty list is returned (no error). Malformed lines are
    /// skipped rather than crashing; the number skipped is returned so the caller
    /// can tell the user once.
    /// </summary>
    /// <param name="skippedLineCount">The number of malformed lines that were skipped.</param>
    /// <returns>The valid animals, in file order.</returns>
    public List<Animal> LoadAll(out int skippedLineCount)
    {
        var animals = new List<Animal>();
        skippedLineCount = 0;

        try
        {
            if (!File.Exists(FilePath))
            {
                // Create an empty file so the application has somewhere to save to.
                using (File.Create(FilePath)) { }
                return animals;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(FilePath, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new DataFileException(
                    "animals.txt could not be opened. It may be in use by another program, " +
                    "or you may not have permission to read it.", ex);
            }

            foreach (string line in lines)
            {
                // Blank lines are tolerated here but never written back.
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (TryParseLine(line, out Animal? animal) && animal != null)
                    animals.Add(animal);
                else
                    skippedLineCount++;
            }
        }
        catch (DataFileException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DataFileException(
                "animals.txt could not be read. Check that the file is not open in another program " +
                "and that you have permission to access it.", ex);
        }

        return animals;
    }

    /// <summary>
    /// Parses one line into an <see cref="Animal"/>. Returns false for a malformed
    /// line: wrong field count, a non-numeric age or score, a score outside 0-100,
    /// or an invalid Animal ID. The stored status and housing unit are accepted as-is
    /// so that assessor-supplied files load without changes.
    /// </summary>
    private static bool TryParseLine(string line, out Animal? animal)
    {
        animal = null;
        string[] parts = line.Split(Separator);
        if (parts.Length != FieldCount)
            return false;

        string id = parts[0].Trim().ToUpperInvariant();
        if (!AnimalIdIsValid(id))
            return false;

        if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int age)
            || age < 0 || age > 100)
            return false;

        if (!int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int score)
            || score < 0 || score > 100)
            return false;

        animal = new Animal
        {
            AnimalId = id,
            Name = parts[1].Trim(),
            Species = parts[2].Trim(),
            Age = age,
            RecoveryScore = score,
            Status = parts[5].Trim(),
            HousingUnit = parts[6].Trim()
        };
        return true;
    }

    /// <summary>
    /// Checks that an Animal ID matches WR- followed by exactly four digits.
    /// Used when loading so lines with an invalid ID are skipped.
    /// </summary>
    private static bool AnimalIdIsValid(string id)
    {
        if (id.Length != 7 || !id.StartsWith("WR-", StringComparison.Ordinal))
            return false;

        for (int i = 3; i < id.Length; i++)
        {
            if (!char.IsDigit(id[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Appends one animal to animals.txt, leaving all existing lines untouched.
    /// </summary>
    public void Append(Animal animal)
    {
        try
        {
            using var writer = new StreamWriter(FilePath, append: true, Encoding.UTF8);
            writer.WriteLine(ToLine(animal));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DataFileException(
                "The animal could not be saved to animals.txt. The file may be in use by " +
                "another program, or you may not have permission to write to it.", ex);
        }
    }

    /// <summary>
    /// Rewrites animals.txt from the supplied list, replacing the entire contents.
    /// Used by Update and Delete so only the intended record changes.
    /// </summary>
    public void SaveAll(IEnumerable<Animal> animals)
    {
        try
        {
            var builder = new StringBuilder();
            foreach (Animal animal in animals)
                builder.AppendLine(ToLine(animal));

            File.WriteAllText(FilePath, builder.ToString(), Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DataFileException(
                "animals.txt could not be updated. The file may be in use by another program, " +
                "or you may not have permission to write to it.", ex);
        }
    }

    /// <summary>
    /// Formats an animal as one pipe-separated line for the data file.
    /// </summary>
    private static string ToLine(Animal animal)
    {
        // Culture-invariant formatting keeps integers stable across regional settings.
        return string.Join(Separator,
            animal.AnimalId,
            animal.Name,
            animal.Species,
            animal.Age.ToString(CultureInfo.InvariantCulture),
            animal.RecoveryScore.ToString(CultureInfo.InvariantCulture),
            animal.Status,
            animal.HousingUnit);
    }
}
