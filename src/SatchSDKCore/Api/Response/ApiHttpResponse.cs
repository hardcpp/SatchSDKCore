using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using SSC.Api.Request;
using SSC.Api.RouteContext;
using SSC.Net.HttpEx;

namespace SSC.Api.Response;

/// <summary>
/// HTTP response class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ApiHttpResponse : ApiResponse
{
    public const string ContentType_AppJson  = "application/json";
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
    public ApiHttpResponse(ApiRequest request, HttpStatusCode code, HttpContent? content, Encoding? contentEncoding)
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
    /// <returns>Built ApiHttpResponse</returns>
    public static ApiHttpResponse CodeResult(ApiRouteContext routeContext, HttpStatusCode code)
        => new(routeContext.Request, code, new StringContent(code.ToString(), Encoding.UTF8), Encoding.UTF8);

    /// <summary>
    /// Make a content result
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="code">Result code</param>
    /// <param name="content">Content</param>
    /// <param name="contentType">Type of the content</param>
    /// <returns>Built ApiHttpResponse</returns>
    public static ApiHttpResponse ContentResult(
        ApiRouteContext routeContext,
        HttpStatusCode  code,
        string          content,
        string          contentType = "text/plain")
        => new(routeContext.Request, code, new StringContent(content, Encoding.UTF8, contentType), Encoding.UTF8);
}

public static class ApiHttpResponseExtensions
{
    /// <summary>
    /// Cast ApiResponse to ApiHttpResponse
    /// </summary>
    /// <param name="self">The ApiResponse instance</param>
    /// <returns>ApiHttpResponse if the cast is successful, otherwise null</returns>
    public static ApiHttpResponse? AsHttpResponse(this ApiResponse self)
        => self as ApiHttpResponse;
}
