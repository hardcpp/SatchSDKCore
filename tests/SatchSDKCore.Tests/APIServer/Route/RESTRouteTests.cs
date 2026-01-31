using System.Net;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SSC.APIServer.Request;
using SSC.APIServer.Response;
using SSC.APIServer.Route;
using SSC.APIServer.RouteContext;

namespace SSC.Tests.APIServer.Route;

/// <summary>
/// Tests for RESTRoute class which represents REST API route definitions with
/// HTTP methods, endpoints, and parameter handling. Tests verify constructor validation,
/// route initialization, parameter processing, and error response generation.
/// </summary>
public class RESTRouteTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : IRequest
    {
        public override IPAddress? GetOriginIPAddress() => null;
    }


    /// <summary>
    /// Sample route methods for testing
    /// </summary>
    private static class SampleRoutes
    {
        [RESTRoute(ERestMethod.Get, "/test")]
        public static IResponse SimpleRoute(RESTRouteContext context)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, "test");
        }

        [RESTRoute(ERestMethod.Post, "/test-params")]
        public static IResponse RouteWithParams(RESTRouteContext context, int id, string name)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, $"{id}:{name}");
        }

        [RESTRoute(ERestMethod.Get, "/test-optional")]
        public static IResponse RouteWithOptionalParams(RESTRouteContext context, int id, string? name = null)
        {
            return RESTResponse.Result(context, HttpStatusCode.OK, $"{id}:{name ?? "default"}");
        }

        [RESTRoute(ERestMethod.Get, "/test-async", "0:30.000")]
        public static Task<IResponse> AsyncRoute(CancellationToken ct, RESTRouteContext context)
        {
            return Task.FromResult<IResponse>(RESTResponse.Result(context, HttpStatusCode.OK, "async"));
        }

        public static IResponse InvalidReturnType()
        {
            return null!;
        }
    }

    /// <summary>
    /// Verifies that constructor creates route with valid endpoint.
    /// </summary>
    [Fact]
    public void Constructor_WithValidEndpoint_CreatesRoute()
    {
        // Act
        var route = new RESTRoute(ERestMethod.Get, "/test");

        // Assert
        Assert.Equal(ERestMethod.Get, route.RESTMethod);
        Assert.Equal("/test", route.RESTEndpoint);
    }

    /// <summary>
    /// Verifies that constructor validates endpoints start with slash.
    /// </summary>
    [Fact]
    public void Constructor_WithoutLeadingSlash_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<Exception>(() => new RESTRoute(ERestMethod.Get, "test"));
        Assert.Contains("Malformated route path", exception.Message);
    }

    /// <summary>
    /// Verifies that constructor rejects endpoints ending with slash.
    /// </summary>
    [Fact]
    public void Constructor_WithTrailingSlash_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<Exception>(() => new RESTRoute(ERestMethod.Get, "/test/"));
        Assert.Contains("Malformated route path", exception.Message);
    }

    /// <summary>
    /// Verifies that constructor accepts empty string endpoint.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyString_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<Exception>(() => new RESTRoute(ERestMethod.Get, ""));
        Assert.Contains("Malformated route path", exception.Message);
    }

    /// <summary>
    /// Verifies that constructor accepts root endpoint.
    /// </summary>
    [Fact]
    public void Constructor_WithRootEndpoint_CreatesRoute()
    {
        // Act
        var route = new RESTRoute(ERestMethod.Get, "/");

        // Assert
        Assert.Equal("/", route.RESTEndpoint);
    }

    /// <summary>
    /// Verifies all REST methods can be used.
    /// </summary>
    [Theory]
    [InlineData(ERestMethod.Get)]
    [InlineData(ERestMethod.Post)]
    [InlineData(ERestMethod.Put)]
    [InlineData(ERestMethod.Patch)]
    [InlineData(ERestMethod.Delete)]
    public void Constructor_WithDifferentMethods_CreatesRoute(ERestMethod method)
    {
        // Act
        var route = new RESTRoute(method, "/test");

        // Assert
        Assert.Equal(method, route.RESTMethod);
    }

    /// <summary>
    /// Verifies that constructor accepts complex endpoints.
    /// </summary>
    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/users/123")]
    [InlineData("/api/v1/users/{id}")]
    [InlineData("/api/v1/users/{id}/posts")]
    public void Constructor_WithComplexEndpoints_CreatesRoute(string endpoint)
    {
        // Act
        var route = new RESTRoute(ERestMethod.Get, endpoint);

        // Assert
        Assert.Equal(endpoint, route.RESTEndpoint);
    }

    /// <summary>
    /// Verifies that constructor accepts async timeout.
    /// </summary>
    [Fact]
    public void Constructor_WithAsyncTimeout_CreatesRoute()
    {
        // Act
        var route = new RESTRoute(ERestMethod.Get, "/test", "1:30.000");

        // Assert
        Assert.NotNull(route);
    }

    /// <summary>
    /// Verifies that Init method initializes route metadata.
    /// </summary>
    [Fact]
    public void Init_WithValidMethod_InitializesRoute()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;

        // Act
        route.Init(method);

        // Assert
        Assert.NotNull(route.Method);
        Assert.NotNull(route.Parameters);
        Assert.NotNull(route.ParametersType);
        Assert.Equal(method, route.Method);
    }

    /// <summary>
    /// Verifies that Init can only be called once.
    /// </summary>
    [Fact]
    public void Init_CalledTwice_IgnoresSecondCall()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method1 = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        var method2 = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;

        // Act
        route.Init(method1);
        route.Init(method2);

        // Assert
        Assert.Equal(method1, route.Method);
    }

    /// <summary>
    /// Verifies that Init throws when method is null.
    /// </summary>
    [Fact]
    public void Init_WithNullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => route.Init(null!));
    }

    /// <summary>
    /// Verifies that Init correctly detects HasContextParameter.
    /// </summary>
    [Fact]
    public void Init_WithContextParameter_SetsHasContextParameter()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;

        // Act
        route.Init(method);

        // Assert
        Assert.True(route.HasContextParameter);
    }


    /// <summary>
    /// Verifies that GetResponseForException returns InternalServerError.
    /// </summary>
    [Fact]
    public void GetResponseForException_ReturnsInternalServerError()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);

        // Use reflection to call protected method
        var getResponseMethod = typeof(RESTRoute).GetMethod(
            "GetResponseForException",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;

        // Act
        var response = getResponseMethod.Invoke(route, new object[] { mockContext, new Exception("test") }) as IResponse;

        // Assert
        Assert.NotNull(response);
        Assert.IsType<RESTResponse>(response);
    }

    /// <summary>
    /// Verifies that GetResponseForBadRequest returns BadRequest.
    /// </summary>
    [Fact]
    public void GetResponseForBadRequest_ReturnsBadRequest()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);

        // Use reflection to call protected method
        var getResponseMethod = typeof(RESTRoute).GetMethod(
            "GetResponseForBadRequest",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;

        // Act
        var response = getResponseMethod.Invoke(route, new object[] { mockContext, "test error" }) as IResponse;

        // Assert
        Assert.NotNull(response);
        Assert.IsType<RESTResponse>(response);
    }


    /// <summary>
    /// Verifies that TryInvoke works with simple route.
    /// </summary>
    [Fact]
    public void TryInvoke_WithSimpleRoute_ReturnsSuccess()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Post);
        var parameters = new JArray();

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with missing required parameters fails.
    /// </summary>
    [Fact]
    public void TryInvoke_WithMissingRequiredParams_ReturnsFalse()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Post);
        var parameters = new JArray();

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void TryInvoke_WithValidParams_ReturnsSuccess()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Post);
        var parameters = new JArray { 123, "test" };

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with JObject parameters works.
    /// </summary>
    [Fact]
    public void TryInvoke_WithJObjectParams_ReturnsSuccess()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JObject
        {
            ["id"] = 123,
            ["name"] = "test"
        };

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with dictionary parameters works.
    /// </summary>
    [Fact]
    public void TryInvoke_WithDictionaryParams_ReturnsSuccess()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new Dictionary<string, string>
        {
            ["id"] = "123",
            ["name"] = "test"
        };

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
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
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);
        var parameters = new JArray();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            route.TryInvoke(null!, parameters, out _, out _));
    }

    /// <summary>
    /// Verifies that TryInvoke throws on null parameters (JArray).
    /// </summary>
    [Fact]
    public void TryInvoke_WithNullJArrayParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            route.TryInvoke(mockContext, (JArray)null!, out _, out _));
    }

    /// <summary>
    /// Verifies that RESTRoute is an attribute.
    /// </summary>
    [Fact]
    public void RESTRoute_IsAttribute()
    {
        // Assert
        Assert.True(typeof(RESTRoute).IsSubclassOf(typeof(Attribute)));
    }

    /// <summary>
    /// Verifies that RESTRoute can be applied to methods.
    /// </summary>
    [Fact]
    public void RESTRoute_CanBeAppliedToMethods()
    {
        // Arrange
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;

        // Act
        var attributes = method.GetCustomAttributes<RESTRoute>();

        // Assert
        Assert.NotEmpty(attributes);
    }

    /// <summary>
    /// Verifies that multiple RESTRoute attributes can be applied.
    /// </summary>
    [Fact]
    public void RESTRoute_AllowsMultiple()
    {
        // Assert
        var attributeUsage = typeof(RESTRoute).GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(attributeUsage);
        Assert.True(attributeUsage.AllowMultiple);
    }

    /// <summary>
    /// Verifies that RESTRoute inherits from IRoute.
    /// </summary>
    [Fact]
    public void RESTRoute_InheritsFromIRoute()
    {
        // Assert
        Assert.True(typeof(RESTRoute).IsSubclassOf(typeof(IRoute)));
    }

    /// <summary>
    /// Verifies that ERestMethod enum has all expected values.
    /// </summary>
    [Fact]
    public void ERestMethod_HasAllExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ERestMethod), ERestMethod.Get));
        Assert.True(Enum.IsDefined(typeof(ERestMethod), ERestMethod.Post));
        Assert.True(Enum.IsDefined(typeof(ERestMethod), ERestMethod.Put));
        Assert.True(Enum.IsDefined(typeof(ERestMethod), ERestMethod.Patch));
        Assert.True(Enum.IsDefined(typeof(ERestMethod), ERestMethod.Delete));
    }

    /// <summary>
    /// Verifies that GetResponseForAsyncTimeout returns RequestTimeout.
    /// </summary>
    [Fact]
    public void GetResponseForAsyncTimeout_ReturnsRequestTimeout()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);

        // Use reflection to call protected method
        var getResponseMethod = typeof(RESTRoute).GetMethod(
            "GetResponseForAsyncTimeout",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;

        // Act
        var response = getResponseMethod.Invoke(route, new object[] { mockContext }) as IResponse;

        // Assert
        Assert.NotNull(response);
        Assert.IsType<RESTResponse>(response);
    }

    /// <summary>
    /// Verifies that constructor with null endpoint throws exception.
    /// </summary>
    [Fact]
    public void Constructor_WithNullEndpoint_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<Exception>(() => new RESTRoute(ERestMethod.Get, null!));
    }

    /// <summary>
    /// Verifies that TryInvoke with JObject and missing optional param succeeds.
    /// </summary>
    [Fact]
    public void TryInvoke_JObject_WithMissingOptionalParam_Succeeds()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithOptionalParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JObject
        {
            ["id"] = 123
        };

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that TryInvoke with Dictionary and missing optional param succeeds.
    /// </summary>
    [Fact]
    public void TryInvoke_Dictionary_WithMissingOptionalParam_Succeeds()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithOptionalParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new Dictionary<string, string>
        {
            ["id"] = "123"
        };

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }

    /// <summary>
    /// Verifies that async route invocation works correctly.
    /// </summary>
    [Fact]
    public void TryInvoke_WithAsyncRoute_Succeeds()
    {
        // Arrange
        var route = new RESTRoute(ERestMethod.Get, "/test-async", "0:30.000");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.AsyncRoute))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new RESTRouteContext(mockRequest, ERestMethod.Get);
        var parameters = new JArray();

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }
}
