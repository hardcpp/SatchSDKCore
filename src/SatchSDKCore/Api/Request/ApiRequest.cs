using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SSC.Api.Request;

/// <summary>
/// Generic request class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiRequest
{
    public ApiHttpRequest? AsHttpRequest
        => this is ApiHttpRequest casted ? casted : null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    public ApiRequest()
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