using System.Globalization;
using System.Text;

namespace Kremetart.Services;

/// <summary>
/// Raised when a data file cannot be read or written. It carries a message
/// that is safe and friendly to show directly to the user (never a stack trace).
/// </summary>
public class DataFileException : Exception
{
    public DataFileException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
