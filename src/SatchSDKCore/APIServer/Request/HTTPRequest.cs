using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SSC.APIServer.Request;

/// <summary>
/// HTTPServer Request class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class HTTPRequest : IRequest
{
    public readonly Net.HTTPServer.HTTPServerRequestContext Context;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">HTTPServer request context</param>
    public HTTPRequest(Net.HTTPServer.HTTPServerRequestContext context)
        : base()
    {
        ArgumentNullException.ThrowIfNull(context);

        Context = context;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get origin IP address
    /// </summary>
    /// <returns>Origin IP address</returns>
    public override IPAddress? GetOriginIPAddress()
        => Context.GetOriginIPAddress();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try get header values combined into one comma-separated list
    /// </summary>
    /// <param name="headerName">Header name</param>
    /// <param name="outValue">Out value</param>
    /// <returns>True if the header was found</returns>
    public bool TryGetHeaderValue(string headerName, out string? outValue)
        => Context.TryGetHeaderValue(headerName, out outValue);
}
