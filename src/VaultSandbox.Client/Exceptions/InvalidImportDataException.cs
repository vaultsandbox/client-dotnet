namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception when import data validation fails.
/// </summary>
public class InvalidImportDataException : VaultSandboxException
{
    public InvalidImportDataException(string message) : base(message) { }
}
