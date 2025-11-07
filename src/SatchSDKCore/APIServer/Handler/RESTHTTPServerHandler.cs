using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace SSC.APIServer.Handler;

/// <summary>
/// REST HTTP Server handler
/// </summary>
public class RESTHTTPServerHandler : Network.HTTP.IHTTPServerRequestHandler
{
    public readonly Blueprint.RESTBlueprint MainBlueprint = new Blueprint.RESTBlueprint("Main");

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private ThreadLocal<List<string>>               m_SegmentBuffers        = new(() => new(10));
    private ThreadLocal<Dictionary<string, string>> m_ArgumentsCollectors   = new(() => new());

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    protected override bool TryHandleImplementation(Network.HTTP.HTTPServerRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var l_OgRequest  = context.ListenerRequest;
        var l_RestMethod = GetRestMethodFromHttpMethod(l_OgRequest);
        var l_Segments   = GetSegmentsFromAbsolutePath(l_OgRequest.Url!.AbsolutePath);
        var l_Arguments  = m_ArgumentsCollectors.Value!;

        l_Arguments.Clear();

        if (!MainBlueprint.TryFindRoute(l_RestMethod, CollectionsMarshal.AsSpan(l_Segments), l_Arguments, out var l_Route))
            return false;

        var l_HTTPRequest = new Request.HTTPRequest(context);
        var l_RESTContext = new RouteContext.RESTRouteContext(l_HTTPRequest, l_RestMethod);

        l_Route!.TryInvoke(l_RESTContext, l_Arguments, out var l_Error, out var l_Response);

        if (l_Response != null)
            context.ServerResponse = l_Response.AsRESTResponse?.HTTPServerResponse ?? null;
        else if (!string.IsNullOrEmpty(l_Error))
        {
            Logging.Log(ELogSeverity.Error, $"Failed to execute route '{l_Route.RESTEndpoint}': {l_Error}");
            context.ServerResponse = Response.RESTResponse.CodeResult(l_RESTContext, HttpStatusCode.InternalServerError).HTTPServerResponse;
        }

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get a ERestMethod from HttpMethod
    /// </summary>
    /// <param name="originalRequest">Original request</param>
    /// <returns>Equivalent ERestMethod</returns>
    /// <exception cref="Exception">If no corresponding ERestMethod was found</exception>
    private static Route.ERestMethod GetRestMethodFromHttpMethod(HttpListenerRequest originalRequest)
    {
        switch (originalRequest.HttpMethod)
        {
            case "GET":
                return Route.ERestMethod.Get;

            case "DELETE":
                return Route.ERestMethod.Delete;

            case "POST":
                return Route.ERestMethod.Post;

            case "PUT":
                return Route.ERestMethod.Put;

            case "PATCH":
                return Route.ERestMethod.Patch;

            default:
                throw new Exception($"Unhandled HTTP method {originalRequest.HttpMethod}");
        }
    }
    /// <summary>
    /// Get splitted segment from an absolute Url
    /// </summary>
    /// <param name="absolutePath">Absolut Url</param>
    /// <returns>List of segments</returns>
    protected List<string> GetSegmentsFromAbsolutePath(string absolutePath)
    {
        var l_Result = m_SegmentBuffers.Value!;
        l_Result.Clear();

        for (var l_I = 0; l_I < absolutePath.Length; ++l_I)
        {
            if (absolutePath[l_I] == '/')
                continue;

            var l_NextSeparator = absolutePath.IndexOf('/', l_I);
            if (l_NextSeparator == -1)
            {
                l_Result.Add(absolutePath[l_I..]);
                l_I = absolutePath.Length;
            }
            else
            {
                l_Result.Add(absolutePath[l_I..l_NextSeparator]);
                l_I = l_NextSeparator;
            }
        }

        return l_Result;
    }
}
