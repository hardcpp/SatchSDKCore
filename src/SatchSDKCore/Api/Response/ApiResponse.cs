using System;
using System.Diagnostics.CodeAnalysis;
using SSC.Api.Request;

namespace SSC.Api.Response;

/// <summary>
/// Generic response class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiResponse
{
    public readonly ApiRequest Request;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Request</param>
    public ApiResponse(ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Request = request;
    }
}
