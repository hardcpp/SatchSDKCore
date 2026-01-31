using System;
using System.Diagnostics.CodeAnalysis;

namespace SSC.APIServer.Response;

/// <summary>
/// Generic response class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class IResponse
{
    public readonly Request.IRequest Request;

    public RESTResponse? AsRESTResponse
        => this is RESTResponse casted ? casted : null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Request</param>
    public IResponse(Request.IRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Request = request;
    }
}
