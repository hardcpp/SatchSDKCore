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

    public ApiHttpResponse? AsHttpResponse
        => this is ApiHttpResponse casted ? casted : null;

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
