using SatchSDKCore.Net.HttpEx;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SSC.APIServer.Response;

/// <summary>
/// REST response class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class RESTResponse : IResponse
{
    public const string ContentType_AppJson  = "application/json";
    public const string ContentType_TextHTML = "text/html";

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly HttpServerExResponse HTTPServerResponse;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="code">Response code</param>
    /// <param name="content">Content</param>
    /// <param name="contentEncoding">Optional encoding</param>
    public RESTResponse(Request.IRequest request, HttpStatusCode code, HttpContent? content, Encoding? contentEncoding)
        : base(request)
    {
        HTTPServerResponse = new Net.HttpEx.HttpServerExResponse(code, content, contentEncoding);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Make a basic code result
    /// </summary>
    /// <param name="code">Result code</param>
    /// <returns></returns>
    public static RESTResponse CodeResult(RouteContext.IRouteContext routeContext, HttpStatusCode code)
        => new(routeContext.Request, code, new StringContent(code.ToString(), Encoding.UTF8), Encoding.UTF8);
    public static RESTResponse Result(RouteContext.IRouteContext routeContext, HttpStatusCode code, string content, string contentType = "text/plain")
        => new(routeContext.Request, code, new StringContent(content.ToString(), Encoding.UTF8, contentType), Encoding.UTF8);
}
