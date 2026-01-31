using System.Net;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SSC.Api.Request;
using SSC.Api.Response;
using SSC.Api.Route;
using SSC.Api.RouteContext;

namespace SSC.Tests.Api.Route;

/// <summary>
/// Tests for ApiHttpRoute class which represents REST API route definitions with
/// HTTP methods, endpoints, and parameter handling. Tests verify constructor validation,
/// route initialization, parameter processing, and error response generation.
/// </summary>
public class ApiHttpRouteTests
{
    /// <summary>
    /// Mock request implementation for testing purposes.
    /// </summary>
    private class MockRequest : ApiRequest
    {
        public override IPAddress? GetOriginIPAddress() => null;
    }


    /// <summary>
    /// Sample route methods for testing
    /// </summary>
    private static class SampleRoutes
    {
        [ApiHttpRoute(EApiHttpMethod.Get, "/test")]
        public static ApiResponse SimpleRoute(ApiHttpRouteContext context)
        {
            return ApiHttpResponse.Result(context, HttpStatusCode.OK, "test");
        }

        [ApiHttpRoute(EApiHttpMethod.Post, "/test-params")]
        public static ApiResponse RouteWithParams(ApiHttpRouteContext context, int id, string name)
        {
            return ApiHttpResponse.Result(context, HttpStatusCode.OK, $"{id}:{name}");
        }

        [ApiHttpRoute(EApiHttpMethod.Get, "/test-optional")]
        public static ApiResponse RouteWithOptionalParams(ApiHttpRouteContext context, int id, string? name = null)
        {
            return ApiHttpResponse.Result(context, HttpStatusCode.OK, $"{id}:{name ?? "default"}");
        }

        [ApiHttpRoute(EApiHttpMethod.Get, "/test-async", "0:30.000")]
        public static Task<ApiResponse> AsyncRoute(CancellationToken ct, ApiHttpRouteContext context)
        {
            return Task.FromResult<ApiResponse>(ApiHttpResponse.Result(context, HttpStatusCode.OK, "async"));
        }

        public static ApiResponse InvalidReturnType()
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");

        // Assert
        Assert.Equal(EApiHttpMethod.Get, route.HttpMethod);
        Assert.Equal("/test", route.HttpEndpoint);
    }

    /// <summary>
    /// Verifies that constructor validates endpoints start with slash.
    /// </summary>
    [Fact]
    public void Constructor_WithoutLeadingSlash_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<Exception>(() => new ApiHttpRoute(EApiHttpMethod.Get, "test"));
        Assert.Contains("Malformated route path", exception.Message);
    }

    /// <summary>
    /// Verifies that constructor rejects endpoints ending with slash.
    /// </summary>
    [Fact]
    public void Constructor_WithTrailingSlash_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<Exception>(() => new ApiHttpRoute(EApiHttpMethod.Get, "/test/"));
        Assert.Contains("Malformated route path", exception.Message);
    }

    /// <summary>
    /// Verifies that constructor accepts empty string endpoint.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyString_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<Exception>(() => new ApiHttpRoute(EApiHttpMethod.Get, ""));
        Assert.Contains("Malformated route path", exception.Message);
    }

    /// <summary>
    /// Verifies that constructor accepts root endpoint.
    /// </summary>
    [Fact]
    public void Constructor_WithRootEndpoint_CreatesRoute()
    {
        // Act
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/");

        // Assert
        Assert.Equal("/", route.HttpEndpoint);
    }

    /// <summary>
    /// Verifies all REST methods can be used.
    /// </summary>
    [Theory]
    [InlineData(EApiHttpMethod.Get)]
    [InlineData(EApiHttpMethod.Post)]
    [InlineData(EApiHttpMethod.Put)]
    [InlineData(EApiHttpMethod.Patch)]
    [InlineData(EApiHttpMethod.Delete)]
    public void Constructor_WithDifferentMethods_CreatesRoute(EApiHttpMethod method)
    {
        // Act
        var route = new ApiHttpRoute(method, "/test");

        // Assert
        Assert.Equal(method, route.HttpMethod);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, endpoint);

        // Assert
        Assert.Equal(endpoint, route.HttpEndpoint);
    }

    /// <summary>
    /// Verifies that constructor accepts async timeout.
    /// </summary>
    [Fact]
    public void Constructor_WithAsyncTimeout_CreatesRoute()
    {
        // Act
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test", "1:30.000");

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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");

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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);

        // Use reflection to call protected method
        var getResponseMethod = typeof(ApiHttpRoute).GetMethod(
            "GetResponseForException",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;

        // Act
        var response = getResponseMethod.Invoke(route, new object[] { mockContext, new Exception("test") }) as ApiResponse;

        // Assert
        Assert.NotNull(response);
        Assert.IsType<ApiHttpResponse>(response);
    }

    /// <summary>
    /// Verifies that GetResponseForBadRequest returns BadRequest.
    /// </summary>
    [Fact]
    public void GetResponseForBadRequest_ReturnsBadRequest()
    {
        // Arrange
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);

        // Use reflection to call protected method
        var getResponseMethod = typeof(ApiHttpRoute).GetMethod(
            "GetResponseForBadRequest",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;

        // Act
        var response = getResponseMethod.Invoke(route, new object[] { mockContext, "test error" }) as ApiResponse;

        // Assert
        Assert.NotNull(response);
        Assert.IsType<ApiHttpResponse>(response);
    }


    /// <summary>
    /// Verifies that TryInvoke works with simple route.
    /// </summary>
    [Fact]
    public void TryInvoke_WithSimpleRoute_ReturnsSuccess()
    {
        // Arrange
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Post);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Post);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Post);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Post, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            route.TryInvoke(mockContext, (JArray)null!, out _, out _));
    }

    /// <summary>
    /// Verifies that ApiHttpRoute is an attribute.
    /// </summary>
    [Fact]
    public void RESTRoute_IsAttribute()
    {
        // Assert
        Assert.True(typeof(ApiHttpRoute).IsSubclassOf(typeof(Attribute)));
    }

    /// <summary>
    /// Verifies that ApiHttpRoute can be applied to methods.
    /// </summary>
    [Fact]
    public void RESTRoute_CanBeAppliedToMethods()
    {
        // Arrange
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;

        // Act
        var attributes = method.GetCustomAttributes<ApiHttpRoute>();

        // Assert
        Assert.NotEmpty(attributes);
    }

    /// <summary>
    /// Verifies that multiple ApiHttpRoute attributes can be applied.
    /// </summary>
    [Fact]
    public void RESTRoute_AllowsMultiple()
    {
        // Assert
        var attributeUsage = typeof(ApiHttpRoute).GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(attributeUsage);
        Assert.True(attributeUsage.AllowMultiple);
    }

    /// <summary>
    /// Verifies that ApiHttpRoute inherits from ApiRoute.
    /// </summary>
    [Fact]
    public void RESTRoute_InheritsFromIRoute()
    {
        // Assert
        Assert.True(typeof(ApiHttpRoute).IsSubclassOf(typeof(ApiRoute)));
    }

    /// <summary>
    /// Verifies that ApiHttpMethod enum has all expected values.
    /// </summary>
    [Fact]
    public void ERestMethod_HasAllExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(EApiHttpMethod), EApiHttpMethod.Get));
        Assert.True(Enum.IsDefined(typeof(EApiHttpMethod), EApiHttpMethod.Post));
        Assert.True(Enum.IsDefined(typeof(EApiHttpMethod), EApiHttpMethod.Put));
        Assert.True(Enum.IsDefined(typeof(EApiHttpMethod), EApiHttpMethod.Patch));
        Assert.True(Enum.IsDefined(typeof(EApiHttpMethod), EApiHttpMethod.Delete));
    }

    /// <summary>
    /// Verifies that GetResponseForAsyncTimeout returns RequestTimeout.
    /// </summary>
    [Fact]
    public void GetResponseForAsyncTimeout_ReturnsRequestTimeout()
    {
        // Arrange
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.SimpleRoute))!;
        route.Init(method);

        // Use reflection to call protected method
        var getResponseMethod = typeof(ApiHttpRoute).GetMethod(
            "GetResponseForAsyncTimeout",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;

        // Act
        var response = getResponseMethod.Invoke(route, new object[] { mockContext }) as ApiResponse;

        // Assert
        Assert.NotNull(response);
        Assert.IsType<ApiHttpResponse>(response);
    }

    /// <summary>
    /// Verifies that constructor with null endpoint throws exception.
    /// </summary>
    [Fact]
    public void Constructor_WithNullEndpoint_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<Exception>(() => new ApiHttpRoute(EApiHttpMethod.Get, null!));
    }

    /// <summary>
    /// Verifies that TryInvoke with JObject and missing optional param succeeds.
    /// </summary>
    [Fact]
    public void TryInvoke_JObject_WithMissingOptionalParam_Succeeds()
    {
        // Arrange
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithOptionalParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.RouteWithOptionalParams))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
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
        var route = new ApiHttpRoute(EApiHttpMethod.Get, "/test-async", "0:30.000");
        var method = typeof(SampleRoutes).GetMethod(nameof(SampleRoutes.AsyncRoute))!;
        route.Init(method);
        var mockRequest = new MockRequest();
        var mockContext = new ApiHttpRouteContext(mockRequest, EApiHttpMethod.Get);
        var parameters = new JArray();

        // Act
        var result = route.TryInvoke(mockContext, parameters, out var error, out var response);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.NotNull(response);
    }
}
