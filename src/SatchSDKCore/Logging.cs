using System;

namespace SSC;

/// <summary>
/// Log severity
/// </summary>
public enum ELogSeverity
{
    Debug,
    Verbose,
    Warning,
    Error,
    Success
}

/// <summary>
/// Logging class proxy
/// </summary>
public static class Logging
{
    public static event Action<ELogSeverity, string>?    OnLogMessage;
    public static event Action<ELogSeverity, Exception>? OnLogException;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Log a message
    /// </summary>
    /// <param name="severity">Message severity</param>
    /// <param name="message">Message to log</param>
    public static void Log(ELogSeverity severity, string message)
        => OnLogMessage?.Invoke(severity, message);
    /// <summary>
    /// Log an exception
    /// </summary>
    /// <param name="severity">Exception severity</param>
    /// <param name="exception">Exception to log</param>
    public static void Log(ELogSeverity severity, Exception exception)
        => OnLogException?.Invoke(severity, exception);
}
