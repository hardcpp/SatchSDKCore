using System;
using System.Diagnostics.CodeAnalysis;

namespace SSC.Api.Response;

/// <summary>
/// Generic response class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiResponse
{
    public readonly Request.ApiRequest Request;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Request</param>
    public ApiResponse(Request.ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Request = request;
    }
}
