namespace BaobabRidge.Models;

/// <summary>
/// Represents a single animal in the rehabilitation centre's care.
/// The Status and HousingUnit values are filled by the classifier from the
/// RecoveryScore; the user never types them.
/// </summary>
public class Animal
{
    public string AnimalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public int Age { get; set; }
    public int RecoveryScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public string HousingUnit { get; set; } = string.Empty;
}
