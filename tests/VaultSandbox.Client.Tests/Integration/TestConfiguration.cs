using DotNetEnv;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Test configuration loaded from .env file.
/// </summary>
public static class TestConfiguration
{
    private static readonly Lazy<TestSettings> _settings = new(LoadSettings);

    public static TestSettings Settings => _settings.Value;

    private static TestSettings LoadSettings()
    {
        // Look for .env file in project root (walk up from test assembly)
        var directory = AppContext.BaseDirectory;
        while (directory != null)
        {
            var envPath = Path.Combine(directory, ".env");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                break;
            }
            directory = Directory.GetParent(directory)?.FullName;
        }

        var url = Environment.GetEnvironmentVariable("VAULTSANDBOX_URL");
        var apiKey = Environment.GetEnvironmentVariable("VAULTSANDBOX_API_KEY");
        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
        var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT");

        return new TestSettings
        {
            BaseUrl = url ?? "http://localhost:3000",
            ApiKey = apiKey ?? string.Empty,
            SmtpHost = smtpHost ?? "localhost",
            SmtpPort = int.TryParse(smtpPort, out var port) ? port : 25,
            IsConfigured = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(apiKey)
        };
    }
}

public record TestSettings
{
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public required string SmtpHost { get; init; }
    public required int SmtpPort { get; init; }
    public required bool IsConfigured { get; init; }
}
