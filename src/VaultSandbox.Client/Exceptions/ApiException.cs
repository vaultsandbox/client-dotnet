namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception for HTTP API errors.
/// </summary>
public class ApiException : VaultSandboxException
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    public ApiException(int statusCode, string message, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public ApiException(int statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
