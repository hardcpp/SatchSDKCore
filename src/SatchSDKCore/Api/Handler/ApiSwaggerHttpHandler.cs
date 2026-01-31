using System;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.Api.Handler;

/// <summary>
/// Swagger HTTP Server handler
/// </summary>
public class ApiSwaggerHttpHandler : IHttpServerExRequestHandler
{
    public readonly Blueprint.ApiBlueprint MainBlueprint;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mainBlueprint">Main blueprint</param>
    public ApiSwaggerHttpHandler(Blueprint.ApiBlueprint mainBlueprint)
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
    public bool TryHandle(HttpServerExRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ogRequest = context.ListenerRequest;
        if (ogRequest.Url!.AbsolutePath != "/swagger" || ogRequest.HttpMethod != "GET")
            return false;

        var httpMethod = Route.ApiHttpMethod.Get;
        var httpRequest = new Request.ApiHttpRequest(context);
        var httpContext = new RouteContext.ApiHttpRouteContext(httpRequest, httpMethod);

        context.ServerResponse = Response.ApiHttpResponse.Result(
            routeContext: httpContext,
            code: System.Net.HttpStatusCode.OK,
            content: s_HTMLCode,
            contentType: Response.ApiHttpResponse.ContentType_TextHTML
        ).HttpServerExResponse;

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private const string s_HTMLCode = """
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
}