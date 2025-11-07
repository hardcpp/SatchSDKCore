using System.Net;

namespace SSC.Network.HTTP;

/// <summary>
/// HTTP Server request context
/// </summary>
public class HTTPServerRequestContext
{
    public readonly HttpListenerContext ListenerContext;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public HttpListenerRequest  ListenerRequest  => ListenerContext.Request;
    public HttpListenerResponse ListenerResponse => ListenerContext.Response;
    public HTTPServerResponse?  ServerResponse;
    public bool                 ConnectionUpgraded { get; internal set; } = false;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="listenerContext">Original context</param>
    public HTTPServerRequestContext(HttpListenerContext listenerContext)
    {
        ListenerContext = listenerContext;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get origin IP address
    /// </summary>
    /// <returns>Origin IP address</returns>
    public IPAddress? GetOriginIPAddress()
        => ListenerRequest.RemoteEndPoint?.Address;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try get header values combined into one comma-separated list
    /// </summary>
    /// <param name="headerName">Header name</param>
    /// <param name="outValue">Out value</param>
    /// <returns>True if the header was found</returns>
    public bool TryGetHeaderValue(string headerName, out string? outValue)
    {
        outValue = ListenerRequest.Headers.Get(headerName);
        return outValue != null;
    }
}
