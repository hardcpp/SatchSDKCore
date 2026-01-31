using System;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.APIServer.Handler;

/// <summary>
/// Swagger HttpServerEx Server handler
/// </summary>
public class SwaggerHTTPServerHandler : IHttpServerExRequestHandler
{
    public readonly Blueprint.IBlueprint MainBlueprint;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mainBlueprint">Main blueprint</param>
    public SwaggerHTTPServerHandler(Blueprint.IBlueprint mainBlueprint)
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

        var restMethod = Route.ERestMethod.Get;
        var httpRequest = new Request.HTTPRequest(context);
        var restContext = new RouteContext.RESTRouteContext(httpRequest, restMethod);

        context.ServerResponse = Response.RESTResponse.Result(
            routeContext: restContext,
            code: System.Net.HttpStatusCode.OK,
            content: s_HTMLCode,
            contentType: Response.RESTResponse.ContentType_TextHTML
        ).HTTPServerResponse;

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
