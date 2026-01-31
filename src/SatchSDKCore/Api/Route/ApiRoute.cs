using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SSC.Api.Route;

/// <summary>
/// Generic route class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiRoute : Attribute
{
    public MethodInfo? Method { get; private set; }
    public ParameterInfo[]? Parameters { get; private set; }
    public Type[]? ParametersType { get; private set; }
    public bool[]? ParametersOptional { get; private set; }
    public string[]? ParametersFixedName { get; private set; }
    public string[]? ParametersHint { get; private set; }
    public RouteHook.ApiRouteHook[]? Hooks { get; private set; }
    public bool IsAsync { get; private set; }
    public TimeSpan? AsyncTimeout { get; private set; }
    public bool HasContextParameter { get; private set; }
    public int UserParametersOffset { get; private set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private ThreadLocal<object[]>? _invokeBuffer;
    private readonly string? _asyncTimeoutStr;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="asyncTimeoutStr">Timeout for async</param>
    public ApiRoute(string? asyncTimeoutStr = null)
    {
        _asyncTimeoutStr = asyncTimeoutStr;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Init this route
    /// </summary>
    /// <param name="method">Execution method</param>
    /// <exception cref="Exception">If the return type is wrong</exception>
    /// <exception cref="Exception">If a parameter type is wrong</exception>
    /// <exception cref="Exception">If has timeout but not async</exception>
    public void Init(MethodInfo method)
    {
        if (Method != null)
            return;

        ArgumentNullException.ThrowIfNull(method);

        Method = method;
        Parameters = method.GetParameters();
        ParametersType = new Type[Parameters.Length];
        ParametersOptional = new bool[Parameters.Length];
        ParametersFixedName = new string[Parameters.Length];
        ParametersHint = new string[Parameters.Length];

        // Check if the method is async first
        static bool IsTaskType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type returnType)
            => returnType.GetMethod(nameof(Task.GetAwaiter)) != null;

        // Suppress IL2072: We're only checking for GetAwaiter which is a standard method on Task types
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072",
            Justification = "Checking for GetAwaiter method is a standard pattern to detect Task types")]
        static bool CheckIfAsync(MethodInfo methodInfo) => IsTaskType(methodInfo.ReturnType);

        IsAsync = CheckIfAsync(method);

        // Validate return type based on whether it's async or not
        if (IsAsync)
        {
            // For async methods, check if it's Task<ApiResponse>
            if (!method.ReturnType.IsGenericType ||
                method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>) ||
                !method.ReturnType.GetGenericArguments()[0].IsAssignableTo(typeof(Response.ApiResponse)))
            {
                throw new Exception($"Route {method.Name} has wrong return Type, should be of Task<ApiResponse>");
            }
        }
        else
        {
            // For sync methods, check if it's ApiResponse
            if (!Method.ReturnType.IsAssignableTo(typeof(Response.ApiResponse)))
                throw new Exception($"Route {method.Name} has wrong return Type, should be of ApiResponse");
        }

        var hooks = Method.GetCustomAttributes<RouteHook.ApiRouteHook>(true);
        Hooks = hooks.Any() ? hooks.ToArray() : Array.Empty<RouteHook.ApiRouteHook>();
        if (IsAsync)
        {
            if (Parameters.Length == 0 || Parameters[0].ParameterType != typeof(CancellationToken))
                throw new Exception($"Route {method.Name} has wrong parameter Type, Arg 0 should be of CancellationToken");

            if (Parameters.Length >= 2 && typeof(RouteContext.ApiRouteContext).IsAssignableFrom(Parameters[1].ParameterType))
                HasContextParameter = true;

            UserParametersOffset = HasContextParameter ? 2 : 1;

            AsyncTimeout = _asyncTimeoutStr != null
                ? TimeSpan.ParseExact(_asyncTimeoutStr, @"m\:s\.fff", System.Globalization.CultureInfo.InvariantCulture)
                : TimeSpan.FromSeconds(60);
        }
        else
        {
            if (Parameters.Length > 0 && typeof(RouteContext.ApiRouteContext).IsAssignableFrom(Parameters[0].ParameterType))
                HasContextParameter = true;

            UserParametersOffset = HasContextParameter ? 1 : 0;

            if (_asyncTimeoutStr != null)
                throw new Exception($"Route {method.Name} has timeout but is not async");
        }

        for (var i = 0; i < Parameters.Length; ++i)
        {
            var parameter = Parameters[i]!;
            var isNullable = parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
            var hasDefaultValue = parameter.HasDefaultValue;

            ParametersType[i] = isNullable ? parameter.ParameterType.GenericTypeArguments[0] : parameter.ParameterType;
            ParametersOptional[i] = isNullable || hasDefaultValue;
            ParametersFixedName[i] = parameter.Name!.StartsWith("p_") ? parameter.Name[2..] : parameter.Name;

            if (i < UserParametersOffset)
            {
                ParametersHint[i] = string.Empty;
                continue;
            }

            ParametersHint[i] = $"{(i - UserParametersOffset) + 1}:{ParametersFixedName[i]}";
        }

        _invokeBuffer = new ThreadLocal<object[]>(() => new object[Parameters.Length]);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Invoke the route
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="outError">Output error message</param>
    /// <param name="outResponse">Output response</param>
    public bool TryInvoke(RouteContext.ApiRouteContext routeContext, JArray parameters, out string? outError, out Response.ApiResponse? outResponse)
    {
        ArgumentNullException.ThrowIfNull(routeContext);
        ArgumentNullException.ThrowIfNull(parameters);

        if (!TryTransferParameters(parameters, out outError))
        {
            outResponse = GetResponseForBadRequest(routeContext, outError ?? "Bad parameters");
            return false;
        }

        return TryInvokeInternal(routeContext, out outError, out outResponse);
    }
    /// <summary>
    /// Call the handler
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="outError">Output error message</param>
    /// <param name="outResponse">Output response</param>
    public bool TryInvoke(RouteContext.ApiRouteContext routeContext, JObject parameters, out string? outError, out Response.ApiResponse? outResponse)
    {
        ArgumentNullException.ThrowIfNull(routeContext);
        ArgumentNullException.ThrowIfNull(parameters);

        if (!TryTransferParameters(parameters, out outError))
        {
            outResponse = GetResponseForBadRequest(routeContext, outError ?? "Bad parameters");
            return false;
        }

        return TryInvokeInternal(routeContext, out outError, out outResponse);
    }
    /// <summary>
    /// Call the handler
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="outError">Output error message</param>
    /// <param name="outResponse">Output response</param>
    public bool TryInvoke(RouteContext.ApiRouteContext routeContext, IReadOnlyDictionary<string, string> parameters, out string? outError, out Response.ApiResponse? outResponse)
    {
        ArgumentNullException.ThrowIfNull(routeContext);
        ArgumentNullException.ThrowIfNull(parameters);

        if (!TryTransferParameters(parameters, out outError))
        {
            outResponse = GetResponseForBadRequest(routeContext, outError ?? "Bad parameters");
            return false;
        }

        return TryInvokeInternal(routeContext, out outError, out outResponse);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Do final call
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="outError">Output error message</param>
    /// <param name="outResponse">Output result</param>
    /// <returns>Call status</returns>
    private bool TryInvokeInternal(RouteContext.ApiRouteContext routeContext, out string? outError, out Response.ApiResponse? outResponse)
    {
        outError = null;
        outResponse = null;

        object[] invokeBuffer = _invokeBuffer!.Value!;

        if (HasContextParameter)
            invokeBuffer[IsAsync ? 1 : 0] = routeContext;

        for (var i = 0; i < Hooks!.Length; ++i)
        {
            if (!Hooks![i].TryIntercept(routeContext, out var interceptResult))
                continue;

            if (interceptResult == null)
            {
                Logging.Log(ELogSeverity.Error, $"[ApiRoute.TryInvokeInternal] Invalid result type for hook {Hooks[i].GetType().FullName}");

                Array.Clear(invokeBuffer);

                return false;
            }

            outResponse = interceptResult;
            Array.Clear(invokeBuffer);

            return true;
        }

        var cancellationTokenSource = null as CancellationTokenSource;
        if (IsAsync)
        {
            cancellationTokenSource = new CancellationTokenSource();
            invokeBuffer[0] = cancellationTokenSource.Token;
        }

        var returnValue = null as object;
        try
        {
            returnValue = Method!.Invoke(null, invokeBuffer);
        }
        catch (Exception exception)
        {
            // Rethrow critical exceptions that should not be handled as regular route failures.
            if (exception is OutOfMemoryException ||
                exception is StackOverflowException ||
                exception is ThreadAbortException ||
                exception is ThreadInterruptedException)
            {
                throw;
            }

            Logging.Log(ELogSeverity.Error, $"[ApiRoute.TryInvokeInternal] Route {Method!.GetType().FullName} failed with exception:");
            Logging.Log(ELogSeverity.Error, exception);

            outError = "Request failed";
            outResponse = GetResponseForException(routeContext, exception);

            return false;
        }

        if (returnValue != null)
        {
            if (IsAsync)
            {
                var task = returnValue as Task<Response.ApiResponse>;
                if (task!.Exception?.InnerException != null)
                    throw task.Exception?.InnerException!;
                if (AsyncTimeout.HasValue)
                    task.Wait(AsyncTimeout.Value);
                else
                    task.Wait();

                if (task.IsCompletedSuccessfully)
                    outResponse = task.Result;
                else
                {
                    if (task.Exception?.InnerException != null || task.Exception != null)
                    {
                        var exception = task.Exception?.InnerException ?? task.Exception;
                        Logging.Log(ELogSeverity.Error, $"[ApiRoute.TryInvokeInternal] Route {Method.GetType().FullName} failed with exception:");
                        Logging.Log(ELogSeverity.Error, exception!);

                        outError = "Request failed";
                        outResponse = GetResponseForException(routeContext, exception!);

                        return false;
                    }

                    if (AsyncTimeout.HasValue)
                        cancellationTokenSource!.Cancel();

                    outError = "Request failed/timeout";
                    outResponse = GetResponseForAsyncTimeout(routeContext);

                    cancellationTokenSource!.Dispose();

                    return false;
                }
            }
            else
                outResponse = returnValue as Response.ApiResponse;
        }

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Transfer input parameters checking type
    /// </summary>
    /// <param name="inParameters">Input parameters</param>
    /// <param name="outError">Error if any</param>
    /// <returns>True if succeeded</returns>
    protected bool TryTransferParameters(JArray? inParameters, out string? outError)
    {
        object[] invokeBuffer = _invokeBuffer!.Value!;
        Array.Clear(invokeBuffer);

        outError = null;

        for (var i = UserParametersOffset; i < Parameters!.Length; ++i)
        {
            var parameterInfo = Parameters[i];
            var parameterHint = ParametersHint![i];

            if (inParameters == null || (i - UserParametersOffset) >= inParameters.Count)
            {
                if (!parameterInfo.HasDefaultValue && !ParametersOptional![i])
                {
                    outError = $"Parameter {parameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                invokeBuffer[i] = ParametersOptional![i] ? null : parameterInfo.DefaultValue!;
#pragma warning restore CS8601
            }
            else
            {
                var result = Reflection.TypeConverter.TryGetValueAsFromJToken(
                    ParametersType![i],
                    inParameters[i - UserParametersOffset],
                    parameterHint,
                    out outError,
                    ref invokeBuffer[i]
                );

                if (!result)
                    return false;
            }
        }

        return true;
    }
    /// <summary>
    /// Transfer input parameters checking type
    /// </summary>
    /// <param name="inParameters">Input parameters</param>
    /// <param name="outError">Error if any</param>
    /// <returns>True if succeeded</returns>
    protected bool TryTransferParameters(JObject? inParameters, out string? outError)
    {
        object[] invokeBuffer = _invokeBuffer!.Value!;
        Array.Clear(invokeBuffer);

        outError = null;

        for (var i = UserParametersOffset; i < Parameters!.Length; ++i)
        {
            var parameterInfo = Parameters[i];
            var parameterFixedName = ParametersFixedName![i];
            var parameterHint = ParametersHint![i];

            if (inParameters == null || !inParameters.ContainsKey(parameterFixedName))
            {
                if (!parameterInfo.HasDefaultValue && !ParametersOptional![i])
                {
                    outError = $"Parameter {parameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                invokeBuffer[i] = ParametersOptional![i] ? null : parameterInfo.DefaultValue!;
#pragma warning restore CS8601
            }
            else
            {
                var result = Reflection.TypeConverter.TryGetValueAsFromJToken(
                    ParametersType![i],
                    inParameters![parameterFixedName]!,
                    parameterHint,
                    out outError,
                    ref invokeBuffer[i]
                );

                if (!result)
                    return false;
            }
        }

        return true;
    }
    /// <summary>
    /// Transfer input parameters checking type
    /// </summary>
    /// <param name="inParameters">Input parameters</param>
    /// <param name="outError">Error if any</param>
    /// <returns>True if succeeded</returns>
    protected bool TryTransferParameters(IReadOnlyDictionary<string, string>? inParameters, out string? outError)
    {
        object[] invokeBuffer = _invokeBuffer!.Value!;
        Array.Clear(invokeBuffer);

        outError = null;

        for (var i = UserParametersOffset; i < Parameters!.Length; ++i)
        {
            var parameterInfo = Parameters[i];
            var parameterFixedName = ParametersFixedName![i];
            var parameterHint = ParametersHint![i];

            if (inParameters == null || !inParameters.TryGetValue(parameterFixedName, out var dictInParameter))
            {
                if (!parameterInfo.HasDefaultValue && !ParametersOptional![i])
                {
                    outError = $"Parameter {parameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                invokeBuffer[i] = ParametersOptional![i] ? null : parameterInfo.DefaultValue!;
#pragma warning restore CS8601
            }
            else
            {
                var result = Reflection.TypeConverter.TryGetValueAsFromString(
                    ParametersType![i],
                    dictInParameter ?? string.Empty,
                    parameterHint,
                    out outError,
                    ref invokeBuffer[i]
                );

                if (!result)
                    return false;
            }
        }

        return true;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get exception response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="p_Exception">Exception if any</param>
    /// <returns></returns>
    protected abstract Response.ApiResponse GetResponseForException(RouteContext.ApiRouteContext routeContext, Exception p_Exception);
    /// <summary>
    /// Get response for bad request
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="error">Error message</param>
    /// <returns></returns>
    protected abstract Response.ApiResponse GetResponseForBadRequest(RouteContext.ApiRouteContext routeContext, string error);
    /// <summary>
    /// Get async timeout response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <returns></returns>
    protected abstract Response.ApiResponse GetResponseForAsyncTimeout(RouteContext.ApiRouteContext routeContext);
}
