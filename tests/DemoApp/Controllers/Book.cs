using System.Net;
using SSC.Api.Blueprint;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;

namespace DemoApp.Controllers;

internal abstract class Book
{
    internal static readonly ApiBlueprint Blueprint;

    static Book()
    {
        Blueprint = new ApiHttpBlueprint("book", "book/<chapterId>");
        Blueprint.AddRoutesOf<Book>();
    }

    [ApiHttpRoute(EApiHttpMethod.Get, "/aa/<id>/<tt>/<aay>")]
    [ApiHttpRoute(EApiHttpMethod.Get, "/ee/<id>/<tt>/<aay>")]
    public static ApiHttpResponse Get(ApiHttpRouteContext context, bool aay, string id, int tt, string? chapterId)
    {
        //DBInstance.Get("Main").GetSession();

        return ApiHttpResponse.Result(context, HttpStatusCode.OK, $"{id} {tt} {(aay.ToString())} chapterId {chapterId}");
    }



    [ApiHttpRoute(EApiHttpMethod.Get, "/")]
    public static ApiHttpResponse GetAll(ApiHttpRouteContext context)
    {
        return ApiHttpResponse.CodeResult(context, HttpStatusCode.OK);
    }


    [ApiHttpRoute(EApiHttpMethod.Post, "/")]
    public static ApiHttpResponse Create(ApiHttpRouteContext context)
    {
        return ApiHttpResponse.CodeResult(context, HttpStatusCode.OK);
    }

    [ApiHttpRoute(EApiHttpMethod.Patch, "/<id>")]
    public static ApiHttpResponse Patch(ApiHttpRouteContext context)
    {
        return ApiHttpResponse.CodeResult(context, HttpStatusCode.OK);
    }

    [ApiHttpRoute(EApiHttpMethod.Put, "/<id>")]
    public static ApiHttpResponse Put(ApiHttpRouteContext context)
    {
        return ApiHttpResponse.CodeResult(context, HttpStatusCode.OK);
    }


    [ApiHttpRoute(EApiHttpMethod.Delete, "/<id>")]
    public static ApiHttpResponse Delete(ApiHttpRouteContext context, string id)
    {
        return ApiHttpResponse.CodeResult(context, HttpStatusCode.OK);
    }
}
