using System.Text.Json;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;

namespace VaultSandbox.Client.Extensions;

/// <summary>
/// Extension methods for inbox export/import operations.
/// </summary>
public static class InboxExportExtensions
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Exports the inbox data to a stream.
    /// </summary>
    /// <param name="inbox">The inbox to export.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task ExportToStreamAsync(
        this IInbox inbox,
        Stream stream,
        CancellationToken ct = default)
    {
        var export = await inbox.ExportAsync();
        await JsonSerializer.SerializeAsync(stream, export, s_jsonOptions, ct);
    }

    /// <summary>
    /// Exports the inbox data to a JSON string.
    /// </summary>
    /// <param name="inbox">The inbox to export.</param>
    /// <returns>JSON string containing the export data.</returns>
    public static async Task<string> ExportToJsonAsync(this IInbox inbox)
    {
        var export = await inbox.ExportAsync();
        return JsonSerializer.Serialize(export, s_jsonOptions);
    }

    /// <summary>
    /// Parses inbox export data from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed export data.</returns>
    /// <exception cref="InvalidImportDataException">Thrown if parsing fails.</exception>
    public static async Task<InboxExport> ParseExportFromStreamAsync(
        Stream stream,
        CancellationToken ct = default)
    {
        try
        {
            var export = await JsonSerializer.DeserializeAsync<InboxExport>(
                stream,
                s_jsonOptions,
                ct);

            return export ?? throw new InvalidImportDataException(
                "Failed to parse inbox export data: null result");
        }
        catch (JsonException ex)
        {
            throw new InvalidImportDataException(
                $"Failed to parse inbox export data: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses inbox export data from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed export data.</returns>
    /// <exception cref="InvalidImportDataException">Thrown if parsing fails.</exception>
    public static InboxExport ParseExportFromJson(string json)
    {
        try
        {
            var export = JsonSerializer.Deserialize<InboxExport>(json, s_jsonOptions);

            return export ?? throw new InvalidImportDataException(
                "Failed to parse inbox export data: null result");
        }
        catch (JsonException ex)
        {
            throw new InvalidImportDataException(
                $"Failed to parse inbox export data: {ex.Message}");
        }
    }
}
