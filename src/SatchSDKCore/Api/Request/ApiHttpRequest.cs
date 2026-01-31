using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using SSC.Net.HttpEx;

namespace SSC.Api.Request;

/// <summary>
/// HttpServerEx Request class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ApiHttpRequest : ApiRequest
{
    public readonly HttpServerExRequestContext Context;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">HttpServerEx request context</param>
    public ApiHttpRequest(HttpServerExRequestContext context)
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

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get Post content as string
    /// </summary>
    /// <returns></returns>
    public string GetBody()
    {
        var requestBody = null as string;
        using (var streamReader = new StreamReader(Context.ListenerRequest.InputStream, Context.ListenerRequest.ContentEncoding))
        {
            requestBody = streamReader.ReadToEnd();
            streamReader.Close();
        }

        return requestBody;
    }
}