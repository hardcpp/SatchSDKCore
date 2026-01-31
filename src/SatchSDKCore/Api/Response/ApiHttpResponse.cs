using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using SSC.Net.HttpEx;

namespace SSC.Api.Response;

/// <summary>
/// HTTP response class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ApiHttpResponse : ApiResponse
{
    public const string ContentType_AppJson = "application/json";
    public const string ContentType_TextHTML = "text/html";

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly HttpServerExResponse HttpServerExResponse;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="code">Response code</param>
    /// <param name="content">Content</param>
    /// <param name="contentEncoding">Optional encoding</param>
    public ApiHttpResponse(Request.ApiRequest request, HttpStatusCode code, HttpContent? content, Encoding? contentEncoding)
        : base(request)
    {
        HttpServerExResponse = new HttpServerExResponse(code, content, contentEncoding);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Make a basic code result
    /// </summary>
    /// <param name="code">Result code</param>
    /// <returns></returns>
    public static ApiHttpResponse CodeResult(RouteContext.ApiRouteContext routeContext, HttpStatusCode code)
        => new(routeContext.Request, code, new StringContent(code.ToString(), Encoding.UTF8), Encoding.UTF8);
    public static ApiHttpResponse Result(RouteContext.ApiRouteContext routeContext, HttpStatusCode code, string content, string contentType = "text/plain")
        => new(routeContext.Request, code, new StringContent(content, Encoding.UTF8, contentType), Encoding.UTF8);
}
