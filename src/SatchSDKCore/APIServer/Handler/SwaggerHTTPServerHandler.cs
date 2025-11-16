using SatchSDKCore.Net.HttpEx;
using System;

namespace SSC.APIServer.Handler;

/// <summary>
/// Swagger HttpServerEx Server handler
/// </summary>
public class SwaggerHTTPServerHandler : IHttpServerExRequestHandler
{
    public readonly Blueprint.IBlueprint MainBlueprint;

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
    protected override bool TryHandleImplementation(HttpServerExRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var l_OgRequest = context.ListenerRequest;
        if (l_OgRequest.Url!.AbsolutePath != "/swagger" || l_OgRequest.HttpMethod != "GET")
            return false;

        var l_RestMethod  = Route.ERestMethod.Get;
        var l_HTTPRequest = new Request.HTTPRequest(context);
        var l_RESTContext = new RouteContext.RESTRouteContext(l_HTTPRequest, l_RestMethod);

        context.ServerResponse = Response.RESTResponse.Result(
            routeContext: l_RESTContext,
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
