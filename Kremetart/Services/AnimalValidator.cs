using System.Text.RegularExpressions;
using Kremetart.Models;

namespace Kremetart.Services;

/// <summary>
/// The outcome of validating the add/edit form. When the input is valid it
/// carries a cleaned <see cref="Animal"/>; when it is not it names the field
/// that failed and a friendly message explaining the problem.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public string FieldName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public Animal? Animal { get; init; }

    /// <summary>Creates a successful result carrying the cleaned animal.</summary>
    public static ValidationResult Valid(Animal animal) =>
        new() { IsValid = true, Animal = animal };

    /// <summary>Creates a failed result naming the field and reason.</summary>
    public static ValidationResult Invalid(string fieldName, string message) =>
        new() { IsValid = false, FieldName = fieldName, ErrorMessage = message };
}

/// <summary>
/// Applies the validation rules from the brief to raw form input.
/// Text is trimmed before it is checked. The same rules are used by both the
/// Add and the Update feature, so they are implemented once, here.
/// </summary>
public static class AnimalValidator
{
    // WR- followed by exactly four digits. Case is normalised to upper case first.
    private static readonly Regex IdPattern = new(@"^WR-\d{4}$", RegexOptions.Compiled);
    private const int MaxNameLength = 30;
    private const int MaxAge = 100;
    private const int MaxScore = 100;

    /// <summary>
    /// Validates every field of a new animal. When <paramref name="existingIds"/>
    /// is supplied, the Animal ID is additionally checked for uniqueness (this is
    /// only needed for Add, because an existing record's ID cannot be changed).
    /// </summary>
    /// <param name="rawId">The Animal ID as typed.</param>
    /// <param name="rawName">The Name as typed.</param>
    /// <param name="rawSpecies">The Species as typed.</param>
    /// <param name="rawAge">The Age as typed.</param>
    /// <param name="rawScore">The Recovery Score as typed.</param>
    /// <param name="existingIds">
    /// Known IDs to check uniqueness against, or null to skip the uniqueness check.
    /// </param>
    /// <returns>Either the cleaned animal or the first validation failure found.</returns>
    public static ValidationResult Validate(
        string? rawId,
        string? rawName,
        string? rawSpecies,
        string? rawAge,
        string? rawScore,
        IEnumerable<string>? existingIds = null)
    {
        // Animal ID: trim, upper-case, then match WR-#### and check uniqueness.
        string id = (rawId ?? string.Empty).Trim().ToUpperInvariant();
        if (id.Length == 0)
            return ValidationResult.Invalid("Animal ID", "Animal ID is required.");

        if (!IdPattern.IsMatch(id))
            return ValidationResult.Invalid("Animal ID",
                "Animal ID must be the letters WR- followed by exactly 4 digits (for example WR-0007).");

        if (existingIds != null &&
            existingIds.Any(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationResult.Invalid("Animal ID",
                $"Animal ID {id} already exists. Animal IDs must be unique.");
        }

        // Name: required, 1-30 characters, no pipe character.
        string name = (rawName ?? string.Empty).Trim();
        if (name.Length == 0)
            return ValidationResult.Invalid("Name", "Name is required.");

        if (name.Length > MaxNameLength)
            return ValidationResult.Invalid("Name", $"Name must be 1-{MaxNameLength} characters.");

        if (name.Contains('|'))
            return ValidationResult.Invalid("Name", "Name must not contain the pipe character (|).");

        // Species: required, 1-30 characters, no pipe character.
        string species = (rawSpecies ?? string.Empty).Trim();
        if (species.Length == 0)
            return ValidationResult.Invalid("Species", "Species is required.");

        if (species.Length > MaxNameLength)
            return ValidationResult.Invalid("Species", $"Species must be 1-{MaxNameLength} characters.");

        if (species.Contains('|'))
            return ValidationResult.Invalid("Species", "Species must not contain the pipe character (|).");

        // Age: required whole number 0-100. int.TryParse rejects decimals and text.
        string ageText = (rawAge ?? string.Empty).Trim();
        if (ageText.Length == 0)
            return ValidationResult.Invalid("Age", "Age is required.");

        if (!int.TryParse(ageText, out int age) || age < 0 || age > MaxAge)
            return ValidationResult.Invalid("Age",
                $"Age must be a whole number between 0 and {MaxAge}.");

        // Recovery Score: required whole number 0-100.
        string scoreText = (rawScore ?? string.Empty).Trim();
        if (scoreText.Length == 0)
            return ValidationResult.Invalid("Recovery Score", "Recovery Score is required.");

        if (!int.TryParse(scoreText, out int score) || score < 0 || score > MaxScore)
            return ValidationResult.Invalid("Recovery Score",
                $"Recovery Score must be a whole number between 0 and {MaxScore}.");

        // Everything passed: derive Status and Housing Unit then build the record.
        (string status, string housingUnit) = AnimalClassifier.Classify(score);

        var animal = new Animal
        {
            AnimalId = id,
            Name = name,
            Species = species,
            Age = age,
            RecoveryScore = score,
            Status = status,
            HousingUnit = housingUnit
        };

        return ValidationResult.Valid(animal);
    }
}
