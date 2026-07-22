using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SSC.Api.Blueprint;
using SSC.Api.Request;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;
using SSC.Misc;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Api.Handler;

/// <summary>
/// HTTP Server handler
/// </summary>
public class ApiHttpHandler : IHttpServerExRequestHandler, IFreezable
{
    private const int MaxRetainedScratchBuffers      = 64;
    private const int InitialRetainedSegmentCapacity = 10;
    private const int MaxRetainedSegmentCapacity     = 128;
    private const int MaxRetainedArgumentCapacity    = 64;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly ConcurrentBag<RequestScratch> _scratchBuffers = new();
    private          int                           _retainedScratchBufferCount;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly ApiHttpBlueprint MainBlueprint = new("Main");
    public          bool             IsFrozen => MainBlueprint.IsFrozen;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public void Freeze()
        => MainBlueprint.Freeze();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpServerExRequestContext context,
        CancellationToken          cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        RequestScratch                      scratch = RentScratch();
        ApiHttpRoute                        route;
        ApiHttpRouteContext                 httpContext;
        EApiHttpMethod                      httpMethod;
        ValueTask<ApiRouteInvocationResult> invocationTask;

        try
        {
            HttpListenerRequest originalRequest = context.ListenerRequest;
            FillSegmentsFromAbsolutePath(
                originalRequest.Url!.AbsolutePath,
                scratch.Segments);

            // First match only the path. This lets us distinguish 404 from 405.
            if (!MainBlueprint.TryFindRoutes(
                    CollectionsMarshal.AsSpan(scratch.Segments),
                    scratch.Arguments,
                    out ApiHttpRoute?[]? routes))
            {
                // The path does not exist. Let HttpServerExCore produce its
                // normal 404 response.
                return false;
            }

            // The path exists, but the client used an extension or unsupported
            // method such as PROPFIND.
            if (!TryGetHttpMethod(
                    originalRequest.HttpMethod,
                    out httpMethod))
            {
                context.ServerResponse =
                    CreateMethodNotAllowedResponse(routes);

                return true;
            }

            // Explicitly registered OPTIONS routes take priority.
            // Otherwise generate an automatic OPTIONS response.
            if (httpMethod                          == EApiHttpMethod.Options &&
                routes[(int)EApiHttpMethod.Options] == null)
            {
                context.ServerResponse =
                    CreateAutomaticOptionsResponse(routes);

                return true;
            }

            ApiHttpRoute? matchedRoute = routes[(int)httpMethod];

            // Explicit HEAD route takes priority. Otherwise execute GET and
            // suppress its response body.
            if (httpMethod   == EApiHttpMethod.Head &&
                matchedRoute == null)
                matchedRoute = routes[(int)EApiHttpMethod.Get];

            if (matchedRoute == null)
            {
                // The path exists, but not for this method.
                context.ServerResponse =
                    CreateMethodNotAllowedResponse(routes);

                return true;
            }

            route = matchedRoute;

            var httpRequest = new ApiHttpRequest(context);

            httpContext = new ApiHttpRouteContext(
                httpRequest,
                httpMethod);

            invocationTask = route.TryInvokeAsync(
                httpContext,
                scratch.Arguments,
                cancellationToken);
        }
        finally
        {
            ReturnScratch(scratch);
        }

        ApiRouteInvocationResult invocation = await invocationTask.ConfigureAwait(false);

        string?      error    = invocation.Error;
        ApiResponse? response = invocation.Response;

        if (response != null)
        {
            HttpServerExResponse? serverResponse =
                response.AsHttpResponse()?.HttpServerExResponse;

            if (serverResponse != null &&
                httpMethod     == EApiHttpMethod.Head)
            {
                serverResponse =
                    serverResponse.WithSuppressedBody();
            }

            context.ServerResponse = serverResponse;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            Logging.Log(
                ELogSeverity.Error,
                $"Failed to execute route '{route.HttpEndpoint}': {error}");

            HttpServerExResponse serverResponse = ApiHttpResponse.CodeResult(
                httpContext,
                HttpStatusCode.InternalServerError
            ).HttpServerExResponse;

            // HEAD responses must not contain a body, including error responses.
            if (httpMethod == EApiHttpMethod.Head) serverResponse = serverResponse.WithSuppressedBody();

            context.ServerResponse = serverResponse;
        }

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to convert an HTTP method string to an API HTTP method.
    /// </summary>
    private static bool TryGetHttpMethod(
        string             originalMethod,
        out EApiHttpMethod method)
    {
        switch (originalMethod)
        {
            case "GET":
                method = EApiHttpMethod.Get;
                return true;

            case "HEAD":
                method = EApiHttpMethod.Head;
                return true;

            case "POST":
                method = EApiHttpMethod.Post;
                return true;

            case "PUT":
                method = EApiHttpMethod.Put;
                return true;

            case "PATCH":
                method = EApiHttpMethod.Patch;
                return true;

            case "DELETE":
                method = EApiHttpMethod.Delete;
                return true;

            case "OPTIONS":
                method = EApiHttpMethod.Options;
                return true;

            default:
                method = default;
                return false;
        }
    }

    /// <summary>
    /// Create a 405 Method Not Allowed response.
    /// </summary>
    private static HttpServerExResponse CreateMethodNotAllowedResponse(
        ApiHttpRoute?[] routes)
    {
        var headers = new Dictionary<string, string>(1) { ["Allow"] = BuildAllowHeader(routes) };

        return new HttpServerExResponse(
            HttpStatusCode.MethodNotAllowed,
            new StringContent(
                "405 Method Not Allowed",
                Encoding.UTF8,
                "text/plain"),
            Encoding.UTF8,
            headers);
    }

    /// <summary>
    /// Create an automatic OPTIONS response.
    /// </summary>
    private static HttpServerExResponse CreateAutomaticOptionsResponse(
        ApiHttpRoute?[] routes)
    {
        var headers = new Dictionary<string, string>(1) { ["Allow"] = BuildAllowHeader(routes) };

        return new HttpServerExResponse(
            HttpStatusCode.NoContent,
            null,
            null,
            headers);
    }

    /// <summary>
    /// Build the Allow header for a matched route path.
    /// </summary>
    private static string BuildAllowHeader(
        ApiHttpRoute?[] routes)
    {
        var result = new StringBuilder(48);

        bool HasRoute(EApiHttpMethod method)
            => routes[(int)method] != null;

        void Append(string method)
        {
            if (result.Length > 0) result.Append(", ");

            result.Append(method);
        }

        if (HasRoute(EApiHttpMethod.Get)) Append("GET");

        // HEAD is automatically supported when GET exists.
        if (HasRoute(EApiHttpMethod.Head) ||
            HasRoute(EApiHttpMethod.Get))
            Append("HEAD");

        if (HasRoute(EApiHttpMethod.Post)) Append("POST");

        if (HasRoute(EApiHttpMethod.Put)) Append("PUT");

        if (HasRoute(EApiHttpMethod.Patch)) Append("PATCH");

        if (HasRoute(EApiHttpMethod.Delete)) Append("DELETE");

        // OPTIONS is automatically available for every matched path.
        Append("OPTIONS");

        return result.ToString();
    }

    /// <summary>
    /// Fill a segment list from an absolute path parts
    /// </summary>
    /// <param name="absolutePath">Absolute path</param>
    /// <param name="result">Target list</param>
    protected static void FillSegmentsFromAbsolutePath(
        string       absolutePath,
        List<string> result)
    {
        result.Clear();

        for (int i = 0; i < absolutePath.Length; ++i)
        {
            if (absolutePath[i] == '/') continue;

            int nextSeparator = absolutePath.IndexOf('/', i);
            if (nextSeparator == -1)
            {
                result.Add(absolutePath[i..]);
                i = absolutePath.Length;
            }
            else
            {
                result.Add(absolutePath[i..nextSeparator]);
                i = nextSeparator;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Rent a scratch buffer for handling the request, or create an extra one if none available
    /// </summary>
    /// <returns>Rented scratch</returns>
    private RequestScratch RentScratch()
    {
        if (_scratchBuffers.TryTake(out RequestScratch? scratch))
        {
            Interlocked.Decrement(ref _retainedScratchBufferCount);
            return scratch;
        }

        return new RequestScratch();
    }

    /// <summary>
    /// Return a rented scratch buffer
    /// </summary>
    /// <param name="scratch">Buffer to return</param>
    private void ReturnScratch(RequestScratch scratch)
    {
        if (scratch.Segments.Capacity > MaxRetainedSegmentCapacity)
            scratch.Segments = new List<string>(InitialRetainedSegmentCapacity);
        else
            scratch.Segments.Clear();

        if (scratch.Arguments.EnsureCapacity(0) > MaxRetainedArgumentCapacity)
            scratch.Arguments = new Dictionary<string, string>();
        else
            scratch.Arguments.Clear();

        if (Interlocked.Increment(ref _retainedScratchBufferCount) <=
            MaxRetainedScratchBuffers)
        {
            _scratchBuffers.Add(scratch);
            return;
        }

        Interlocked.Decrement(ref _retainedScratchBufferCount);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private sealed class RequestScratch
    {
        public List<string>               Segments  { get; set; } = new(InitialRetainedSegmentCapacity);
        public Dictionary<string, string> Arguments { get; set; } = new();
    }
}
