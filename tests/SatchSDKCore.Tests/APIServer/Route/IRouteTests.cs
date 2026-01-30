using System.Net;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json.Linq;
using SSC.APIServer.Request;
using SSC.APIServer.Response;
using SSC.APIServer.Route;
using SSC.APIServer.RouteContext;
using SSC.APIServer.RouteHook;

namespace SatchSDKCore.Tests.APIServer.Route;

/// <summary>
/// Tests for IRoute abstract class which serves as the base for all route types.
/// Tests verify initialization, parameter handling, invocation, async support, and error handling.
/// </summary>
public class IRouteTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : IRequest
    {
        public override System.Net.IPAddress? GetOriginIPAddress() => null;
    }

    /// <summary>
    /// Test implementation of IRoute for testing the abstract class.
    /// </summary>
    private class TestRoute : IRoute
    {
        public TestRoute(string? asyncTimeoutStr = null) : base(asyncTimeoutStr) { }

        protected override IResponse GetResponseForException(IRouteContext routeContext, Exception p_Exception)
        {
            var restContext = routeContext as RESTRouteContext ?? new RESTRouteContext(new MockRequest(), ERestMethod.Get);
            return RESTResponse.Result(restContext, System.Net.HttpStatusCode.InternalServerError, "exception");
        }

        protected override IResponse GetResponseForBadRequest(IRouteContext routeContext, string error)
        {
            var restContext = routeContext as RESTRouteContext ?? new RESTRouteContext(new MockRequest(), ERestMethod.Get);
            return RESTResponse.Result(restContext, System.Net.HttpStatusCode.BadRequest, error);
        }

        protected override IResponse GetResponseForAsyncTimeout(IRouteContext routeContext)
        {
            var restContext = routeContext as RESTRouteContext ?? new RESTRouteContext(new MockRequest(), ERestMethod.Get);
            return RESTResponse.Result(restContext, System.Net.HttpStatusCode.RequestTimeout, "timeout");
        }
    }

    /// <summary>
    /// Mock route hook for testing route interception.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private class MockRouteHookAttribute : IRouteHook
    {
        public override bool TryIntercept(IRouteContext routeContext, out IResponse interceptionResult)
        {
            interceptionResult = null!;
            return false;
        }
    }

    /// <summary>
    /// Sample route methods for testing various scenarios.
    /// </summary>
    private static class SampleRoutes
    {
        // Simple synchronous route
        public static IResponse SimpleSync(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, "success");
        }

        // Synchronous route without context
        public static IResponse SyncNoContext()
        {
            var mockRequest = new MockRequest();
            var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
            return RESTResponse.Result(context, HttpStatusCode.OK, "no context");
        }

        // Synchronous route with parameters
        public static IResponse SyncWithParams(RESTRouteContext context, int id, string name)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, $"{id}:{name}");
        }

        // Synchronous route with optional parameters
        public static IResponse SyncWithOptional(RESTRouteContext context, int id, string? name = null)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, $"{id}:{name ?? "default"}");
        }

        // Synchronous route with default value parameters
        public static IResponse SyncWithDefault(RESTRouteContext context, int id = 42)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, $"{id}");
        }

        // Asynchronous route with context
        public static Task<IResponse> AsyncWithContext(CancellationToken ct, RESTRouteContext context)
        {
            return Task.FromResult<IResponse>(RESTResponse.Result(context, HttpStatusCode.OK, "async success"));
        }

        // Asynchronous route without context
        public static Task<IResponse> AsyncNoContext(CancellationToken ct)
        {
            var mockRequest = new MockRequest();
            var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
            return Task.FromResult<IResponse>(RESTResponse.Result(context, HttpStatusCode.OK, "async no context"));
        }

        // Asynchronous route with parameters
        public static Task<IResponse> AsyncWithParams(CancellationToken ct, RESTRouteContext context, int id, string name)
        {
            return Task.FromResult<IResponse>(RESTResponse.Result(context, HttpStatusCode.OK, $"{id}:{name}"));
        }

        // Asynchronous route that times out
        public static async Task<IResponse> AsyncTimeout(CancellationToken ct, RESTRouteContext context)
        {
            await Task.Delay(5000, ct);
            return RESTResponse.Result(context, HttpStatusCode.OK, "should timeout");
        }

        // Asynchronous route that throws exception
        public static Task<IResponse> AsyncThrows(CancellationToken ct, RESTRouteContext context)
        {
            throw new InvalidOperationException("Test exception");
        }

        // Synchronous route that throws exception
        public static IResponse SyncThrows(RESTRouteContext context)
        {
            throw new InvalidOperationException("Sync exception");
        }

        // Route with wrong return type
        public static string WrongReturnType()
        {
            return "invalid";
        }

        // Route with hook
        [MockRouteHook]
        public static IResponse RouteWithHook(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, "with hook");
        }

        // Route with parameter starting with p_
        public static IResponse RouteWithPrefixParam(RESTRouteContext context, int p_userId)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, $"{p_userId}");
        }

        // Route with nullable parameter
        public static IResponse RouteWithNullable(RESTRouteContext context, int? p_id)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, $"{p_id?.ToString() ?? "null"}");
        }
    }


    /// <summary>
    /// Verifies that constructor with null timeout creates route.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTimeout_CreatesRoute()
    {
        var route = new TestRoute(null);
        Assert.NotNull(route);
    }

    /// <summary>
    /// Verifies that constructor with timeout string creates route.
    /// </summary>
    [Fact]
    public void Constructor_WithTimeoutString_CreatesRoute()
    {
        var route = new TestRoute("1:30.000");
        Assert.NotNull(route);
    }

    /// <summary>
    /// Verifies that Init throws when method is null.
    /// </summary>
    [Fact]
    public void Init_WithNullMethod_ThrowsArgumentNullException()
    {
        var route = new TestRoute();
        Assert.Throws<ArgumentNullException>(() => route.Init(null!));
    }

    /// <summary>
    /// Verifies that Init can only be called once.
    /// </summary>
    [Fact]
    public void Init_CalledMultipleTimes_OnlyInitializesOnce()
    {
        var route = new TestRoute();
        var method1 = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;
        var method2 = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncNoContext))!;

        route.Init(method1);
        var firstMethod = route.Method;

        route.Init(method2);

        Assert.Same(firstMethod, route.Method);
        Assert.NotSame(method2, route.Method);
    }


    /// <summary>
    /// Verifies that Init correctly identifies async routes without context.
    /// </summary>
    [Fact]
    public void Init_WithAsyncNoContextRoute_SetsPropertiesCorrectly()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.AsyncNoContext))!;

        route.Init(method);

        Assert.True(route.IsAsync);
        Assert.False(route.HasContextParameter);
        Assert.Equal(1, route.UserParametersOffset);
    }

    /// <summary>
    /// Verifies that Init with async timeout string parses correctly.
    /// </summary>
    [Fact]
    public void Init_WithAsyncTimeoutString_ParsesTimeout()
    {
        var route = new TestRoute("0:05.500");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.AsyncWithContext))!;

        route.Init(method);

        Assert.NotNull(route.AsyncTimeout);
        Assert.Equal(TimeSpan.FromMilliseconds(5500), route.AsyncTimeout);
    }

    /// <summary>
    /// Verifies that Init throws when sync route has timeout.
    /// </summary>
    [Fact]
    public void Init_WithSyncRouteAndTimeout_ThrowsException()
    {
        var route = new TestRoute("1:00.000");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;

        var exception = Assert.Throws<Exception>(() => route.Init(method));
        Assert.Contains("timeout but is not async", exception.Message);
    }

    /// <summary>
    /// Verifies that Init correctly processes parameter names with p_ prefix.
    /// </summary>
    [Fact]
    public void Init_WithPrefixedParameterNames_RemovesPrefix()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithPrefixParam))!;

        route.Init(method);

        Assert.Equal("userId", route.ParametersFixedName![1]);
    }

    /// <summary>
    /// Verifies that Init correctly identifies optional parameters.
    /// </summary>
    [Fact]
    public void Init_WithOptionalParameters_MarksThemCorrectly()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncWithOptional))!;

        route.Init(method);

        Assert.False(route.ParametersOptional![1]); // id
        Assert.True(route.ParametersOptional![2]);  // name (optional)
    }

    /// <summary>
    /// Verifies that Init correctly identifies nullable parameters.
    /// </summary>
    [Fact]
    public void Init_WithNullableParameters_MarksThemAsOptional()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithNullable))!;

        route.Init(method);

        Assert.True(route.ParametersOptional![1]); // p_id is nullable
        Assert.Equal(typeof(int), route.ParametersType![1]); // Should extract underlying type
    }

    /// <summary>
    /// Verifies that TryInvoke with JArray succeeds for simple sync route.
    /// </summary>
    [Fact]
    public void TryInvoke_JArray_WithSimpleSyncRoute_Succeeds()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with JArray handles parameters correctly.
    /// </summary>
    [Fact]
    public void TryInvoke_JArray_WithParameters_Succeeds()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncWithParams))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray { 123, "test" };

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with JArray fails when required parameters are missing.
    /// </summary>
    [Fact]
    public void TryInvoke_JArray_WithMissingRequiredParams_Fails()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncWithParams))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("is missing", error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with JArray uses default values for optional parameters.
    /// </summary>
    [Fact]
    public void TryInvoke_JArray_WithOptionalParams_UsesDefaults()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncWithDefault))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with JObject succeeds.
    /// </summary>
    [Fact]
    public void TryInvoke_JObject_WithParameters_Succeeds()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncWithParams))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JObject
        {
            ["id"] = 123,
            ["name"] = "test"
        };

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with Dictionary succeeds.
    /// </summary>
    [Fact]
    public void TryInvoke_Dictionary_WithParameters_Succeeds()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncWithParams))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new Dictionary<string, string>
        {
            ["id"] = "123",
            ["name"] = "test"
        };

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke throws on null context.
    /// </summary>
    [Fact]
    public void TryInvoke_WithNullContext_ThrowsArgumentNullException()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;
        route.Init(method);

        Assert.Throws<ArgumentNullException>(() =>
            route.TryInvoke(null!, new JArray(), out _, out _));
    }

    /// <summary>
    /// Verifies that TryInvoke throws on null JArray parameters.
    /// </summary>
    [Fact]
    public void TryInvoke_WithNullJArrayParameters_ThrowsArgumentNullException()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

        Assert.Throws<ArgumentNullException>(() =>
            route.TryInvoke(context, (JArray)null!, out _, out _));
    }

    /// <summary>
    /// Verifies that TryInvoke throws on null JObject parameters.
    /// </summary>
    [Fact]
    public void TryInvoke_WithNullJObjectParameters_ThrowsArgumentNullException()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

        Assert.Throws<ArgumentNullException>(() =>
            route.TryInvoke(context, (JObject)null!, out _, out _));
    }

    /// <summary>
    /// Verifies that TryInvoke throws on null Dictionary parameters.
    /// </summary>
    [Fact]
    public void TryInvoke_WithNullDictionaryParameters_ThrowsArgumentNullException()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);

        Assert.Throws<ArgumentNullException>(() =>
            route.TryInvoke(context, (IReadOnlyDictionary<string, string>)null!, out _, out _));
    }

    /// <summary>
    /// Verifies that TryInvoke handles synchronous exceptions correctly.
    /// </summary>
    [Fact]
    public void TryInvoke_WithSyncException_ReturnsErrorResponse()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncThrows))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.False(result);
        Assert.NotNull(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke handles async routes correctly.
    /// </summary>
    [Fact]
    public void TryInvoke_WithAsyncRoute_Succeeds()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.AsyncWithContext))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke handles async exceptions correctly.
    /// </summary>
    [Fact]
    public void TryInvoke_WithAsyncException_ReturnsErrorResponse()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.AsyncThrows))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.False(result);
        Assert.NotNull(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that async route with very short timeout returns timeout response.
    /// </summary>
    [Fact]
    public void TryInvoke_WithAsyncTimeout_ReturnsTimeoutResponse()
    {
        var route = new TestRoute("0:00.001"); // 1ms timeout
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.AsyncTimeout))!;
        route.Init(method);

        var mockRequest = new MockRequest();
        var context = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        var result = route.TryInvoke(context, parameters, out var error, out var response);

        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("timeout", error, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that parameter hints are generated correctly.
    /// </summary>
    [Fact]
    public void Init_GeneratesParameterHints_Correctly()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SyncWithParams))!;

        route.Init(method);

        Assert.Equal(string.Empty, route.ParametersHint![0]); // context has empty hint
        Assert.Equal("1:id", route.ParametersHint![1]);
        Assert.Equal("2:name", route.ParametersHint![2]);
    }

    /// <summary>
    /// Verifies that Hooks property is initialized correctly.
    /// </summary>
    [Fact]
    public void Init_WithNoHooks_InitializesEmptyHooksArray()
    {
        var route = new TestRoute();
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleSync))!;

        route.Init(method);

        Assert.NotNull(route.Hooks);
        Assert.Empty(route.Hooks);
    }

    /// <summary>
    /// Verifies that properties are null before initialization.
    /// </summary>
    [Fact]
    public void Properties_BeforeInit_AreNull()
    {
        var route = new TestRoute();

        Assert.Null(route.Method);
        Assert.Null(route.Parameters);
        Assert.Null(route.ParametersType);
        Assert.Null(route.ParametersOptional);
        Assert.Null(route.ParametersFixedName);
        Assert.Null(route.ParametersHint);
        Assert.Null(route.Hooks);
        Assert.False(route.IsAsync);
        Assert.Null(route.AsyncTimeout);
        Assert.False(route.HasContextParameter);
        Assert.Equal(0, route.UserParametersOffset);
    }
}
