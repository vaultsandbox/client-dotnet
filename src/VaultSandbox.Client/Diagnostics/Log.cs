using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace VaultSandbox.Client.Diagnostics;

/// <summary>
/// Logging helper to centralize optional logging calls.
/// Excluded from code coverage as logging is optional infrastructure.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Log
{
    public static void Debug(ILogger? logger, string message)
    {
        logger?.LogDebug(message);
    }

    public static void Debug<T0>(ILogger? logger, string message, T0 arg0)
    {
        logger?.LogDebug(message, arg0);
    }

    public static void Debug<T0, T1>(ILogger? logger, string message, T0 arg0, T1 arg1)
    {
        logger?.LogDebug(message, arg0, arg1);
    }

    public static void Debug<T0, T1, T2>(ILogger? logger, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        logger?.LogDebug(message, arg0, arg1, arg2);
    }

    public static void Debug<T0, T1, T2, T3>(ILogger? logger, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger?.LogDebug(message, arg0, arg1, arg2, arg3);
    }

    public static void Information<T0>(ILogger? logger, string message, T0 arg0)
    {
        logger?.LogInformation(message, arg0);
    }

    public static void Information<T0, T1>(ILogger? logger, string message, T0 arg0, T1 arg1)
    {
        logger?.LogInformation(message, arg0, arg1);
    }

    public static void Warning<T0>(ILogger? logger, Exception ex, string message, T0 arg0)
    {
        logger?.LogWarning(ex, message, arg0);
    }

    public static void Error<T0>(ILogger? logger, Exception ex, string message, T0 arg0)
    {
        logger?.LogError(ex, message, arg0);
    }

    public static void Trace<T0, T1>(ILogger? logger, string message, T0 arg0, T1 arg1)
    {
        logger?.LogTrace(message, arg0, arg1);
    }
}
