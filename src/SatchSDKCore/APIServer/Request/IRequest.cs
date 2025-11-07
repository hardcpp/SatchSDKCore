using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SSC.APIServer.Request;

/// <summary>
/// Generic request class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class IRequest
{
    public HTTPRequest? AsHTTPRequest
        => this is HTTPRequest casted ? casted : null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    public IRequest()
    {

    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get origin IP address
    /// </summary>
    /// <returns>Origin IP address</returns>
    public abstract IPAddress? GetOriginIPAddress();
}
