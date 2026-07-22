using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SSC.Api.Blueprint;
using SSC.Api.Request;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Api.Handler;

/// <summary>
/// Swagger HTTP Server handler
/// </summary>
public class ApiSwaggerHttpHandler : IHttpServerExRequestHandler
{
    private const string HTML_CODE = """
                                     <!DOCTYPE html>
                                     <html lang="en">
                                     <head>
                                         <meta charset="utf-8" />
                                         <meta name="viewport" content="width=device-width, initial-scale=1" />
                                         <meta name="description" content="SwaggerUI" />
                                         <title>SwaggerUI</title>
                                         <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5.11.0/swagger-ui.css" />
                                     </head>
                                     <body>
                                     <div id="swagger-ui"></div>
                                     <script src="https://unpkg.com/swagger-ui-dist@5.11.0/swagger-ui-bundle.js" crossorigin></script>
                                     <script src="https://unpkg.com/swagger-ui-dist@5.11.0/swagger-ui-standalone-preset.js" crossorigin></script>
                                     <script>
                                         window.onload = () => {
                                         window.ui = SwaggerUIBundle({
                                             url: 'https://petstore3.swagger.io/api/v3/openapi.json',
                                             dom_id: '#swagger-ui',
                                             presets: [
                                                 SwaggerUIBundle.presets.apis,
                                                 SwaggerUIStandalonePreset
                                             ],
                                             layout: "StandaloneLayout",
                                         });
                                         };
                                     </script>
                                     </body>
                                     </html>
                                     """;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly ApiBlueprint MainBlueprint;

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mainBlueprint">Main blueprint</param>
    public ApiSwaggerHttpHandler(ApiBlueprint mainBlueprint)
    {
        ArgumentNullException.ThrowIfNull(mainBlueprint);

        MainBlueprint = mainBlueprint;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    public ValueTask<bool> TryHandleAsync(
        HttpServerExRequestContext context,
        CancellationToken          cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpListenerRequest ogRequest = context.ListenerRequest;
        if (ogRequest.Url!.AbsolutePath != "/swagger" || ogRequest.HttpMethod != "GET")
            return ValueTask.FromResult(false);

        var httpMethod  = EApiHttpMethod.Get;
        var httpRequest = new ApiHttpRequest(context);
        var httpContext = new ApiHttpRouteContext(httpRequest, httpMethod);

        context.ServerResponse = ApiHttpResponse.ContentResult(
            httpContext,
            HttpStatusCode.OK,
            HTML_CODE,
            ApiHttpResponse.ContentType_TextHTML
        ).HttpServerExResponse;

        return ValueTask.FromResult(true);
    }
}
