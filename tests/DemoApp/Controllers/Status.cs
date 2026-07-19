using System.Net;
using SSC.Api.Blueprint;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;

namespace DemoApp.Controllers;

internal abstract class Status
{
    internal static readonly ApiBlueprint Blueprint;

    static Status()
    {
        Blueprint = new ApiHttpBlueprint("status");
        Blueprint.AddRoutesOf<Status>();
    }

    [ApiHttpRoute(EApiHttpMethod.Get, "/")]
    public static ApiHttpResponse GetStatus(ApiHttpRouteContext context)
    {
        return ApiHttpResponse.Result(context, HttpStatusCode.OK, "all good");
    }
}
