using SSC.Api.Response;

namespace SSC.Api.Route;

/// <summary>
/// Result of a route invocation.
/// </summary>
public readonly struct ApiRouteInvocationResult
{
    public bool         Success  { get; }
    public string?      Error    { get; }
    public ApiResponse? Response { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="success">Is success?</param>
    /// <param name="error">Optional error string</param>
    /// <param name="response">Optional response</param>
    public ApiRouteInvocationResult(
        bool         success,
        string?      error,
        ApiResponse? response)
    {
        Success  = success;
        Error    = error;
        Response = response;
    }
}
