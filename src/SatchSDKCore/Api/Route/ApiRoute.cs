using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SSC.Api.Response;
using SSC.Api.RouteContext;
using SSC.Api.RouteHook;
using SSC.Reflection;

namespace SSC.Api.Route;

/// <summary>
/// Generic route class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiRoute : Attribute
{
    private const int MaxRetainedInvokeBuffers = 32;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly string?                  _asyncTimeoutStr;
    private          Func<object[], object?>? _compiledInvoker;

    private ConcurrentBag<object[]>? _invokeBuffers;
    private int                      _retainedInvokeBufferCount;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public MethodInfo?      Method               { get; private set; }
    public ParameterInfo[]? Parameters           { get; private set; }
    public Type[]?          ParametersType       { get; private set; }
    public bool[]?          ParametersOptional   { get; private set; }
    public string[]?        ParametersFixedName  { get; private set; }
    public string[]?        ParametersHint       { get; private set; }
    public ApiRouteHook[]?  Hooks                { get; private set; }
    public bool             IsAsync              { get; private set; }
    public TimeSpan?        AsyncTimeout         { get; private set; }
    public bool             HasContextParameter  { get; private set; }
    public int              UserParametersOffset { get; private set; }

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

        if (!method.IsStatic)
        {
            throw new Exception(
                $"Route method '{method.DeclaringType?.FullName}.{method.Name}' must be static");
        }

        if (method.ContainsGenericParameters)
        {
            throw new Exception(
                $"Route method '{method.DeclaringType?.FullName}.{method.Name}' cannot contain unbound generic parameters");
        }

        Method              = method;
        Parameters          = method.GetParameters();
        ParametersType      = new Type[Parameters.Length];
        ParametersOptional  = new bool[Parameters.Length];
        ParametersFixedName = new string[Parameters.Length];
        ParametersHint      = new string[Parameters.Length];

        IsAsync = method.ReturnType == typeof(Task<ApiResponse>);

        if (!IsAsync &&
            !method.ReturnType.IsAssignableTo(typeof(ApiResponse)))
        {
            throw new Exception(
                $"Route '{method.DeclaringType?.FullName}.{method.Name}' " +
                "must return ApiResponse or Task<ApiResponse>");
        }

        IEnumerable<ApiRouteHook> hooks = Method.GetCustomAttributes<ApiRouteHook>(true);
        Hooks = hooks.Any() ? hooks.ToArray() : Array.Empty<ApiRouteHook>();
        if (IsAsync)
        {
            if (Parameters.Length == 0 || Parameters[0].ParameterType != typeof(CancellationToken))
            {
                throw new Exception(
                    $"Route {method.Name} has wrong parameter Type, Arg 0 should be of CancellationToken");
            }

            if (Parameters.Length >= 2 && typeof(ApiRouteContext).IsAssignableFrom(Parameters[1].ParameterType))
                HasContextParameter = true;

            UserParametersOffset = HasContextParameter ? 2 : 1;

            AsyncTimeout = _asyncTimeoutStr != null
                ? TimeSpan.ParseExact(_asyncTimeoutStr, @"m\:s\.fff", CultureInfo.InvariantCulture)
                : TimeSpan.FromSeconds(60);
        }
        else
        {
            if (Parameters.Length > 0 && typeof(ApiRouteContext).IsAssignableFrom(Parameters[0].ParameterType))
                HasContextParameter = true;

            UserParametersOffset = HasContextParameter ? 1 : 0;

            if (_asyncTimeoutStr != null)
                throw new Exception($"Route {method.Name} has timeout but is not async");
        }

        for (int i = 0; i < Parameters.Length; ++i)
        {
            ParameterInfo parameter = Parameters[i]!;

            if (parameter.ParameterType.IsByRef || parameter.IsOut)
            {
                throw new Exception(
                    $"Route method '{method.DeclaringType?.FullName}.{method.Name}' " +
                    $"contains unsupported ref/out parameter '{parameter.Name}'");
            }

            bool isNullable = parameter.ParameterType.IsGenericType &&
                              parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>);
            bool hasDefaultValue = parameter.HasDefaultValue;

            ParametersType[i] = isNullable ? parameter.ParameterType.GenericTypeArguments[0] : parameter.ParameterType;
            ParametersOptional[i] = isNullable || hasDefaultValue;
            ParametersFixedName[i] = parameter.Name!.StartsWith("p_") ? parameter.Name[2..] : parameter.Name;

            if (i < UserParametersOffset)
            {
                ParametersHint[i] = string.Empty;
                continue;
            }

            ParametersHint[i] = $"{i - UserParametersOffset + 1}:{ParametersFixedName[i]}";
        }

        _invokeBuffers = new ConcurrentBag<object[]>();

        if (RuntimeFeature.IsDynamicCodeCompiled)
            _compiledInvoker = CompileInvoker(method);
    }

    /// <summary>
    /// Compile a route method into a common invocation signature.
    /// Generated code is conceptually equivalent to:
    /// return RouteMethod(
    /// (Parameter0Type)arguments[0],
    /// (Parameter1Type)arguments[1],
    /// ...);
    /// </summary>
    private static Func<object[], object?> CompileInvoker(
        MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (!method.IsStatic)
        {
            throw new ArgumentException(
                "Only static route methods are supported",
                nameof(method));
        }

        ParameterInfo[] methodParameters = method.GetParameters();

        ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
        var                 callArguments      = new Expression[methodParameters.Length];

        for (int i = 0; i < methodParameters.Length; i++)
        {
            BinaryExpression bufferAccess = Expression.ArrayIndex(
                argumentsParameter,
                Expression.Constant(i));

            callArguments[i] = Expression.Convert(
                bufferAccess,
                methodParameters[i].ParameterType);
        }

        MethodCallExpression methodCall  = Expression.Call(method, callArguments);
        UnaryExpression      boxedResult = Expression.Convert(methodCall, typeof(object));

        return Expression
            .Lambda<Func<object[], object?>>(boxedResult, argumentsParameter)
            .Compile();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public ValueTask<ApiRouteInvocationResult> TryInvokeAsync(
        ApiRouteContext   routeContext,
        JArray            parameters,
        CancellationToken cancellationToken = default)
        => TryInvokeWithBufferAsync(routeContext, parameters, cancellationToken);

    public ValueTask<ApiRouteInvocationResult> TryInvokeAsync(
        ApiRouteContext   routeContext,
        JObject           parameters,
        CancellationToken cancellationToken = default)
        => TryInvokeWithBufferAsync(routeContext, parameters, cancellationToken);

    public ValueTask<ApiRouteInvocationResult> TryInvokeAsync(
        ApiRouteContext                     routeContext,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken                   cancellationToken = default)
        => TryInvokeWithBufferAsync(routeContext, parameters, cancellationToken);

    private ValueTask<ApiRouteInvocationResult> TryInvokeWithBufferAsync(
        ApiRouteContext   routeContext,
        object            parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(routeContext);
        ArgumentNullException.ThrowIfNull(parameters);

        object[]? invokeBuffer = RentInvokeBuffer();
        try
        {
            string? error;
            bool transferred = parameters switch
            {
                JArray array    => TryTransferParameters(array,   invokeBuffer, out error),
                JObject jobject => TryTransferParameters(jobject, invokeBuffer, out error),
                IReadOnlyDictionary<string, string> dictionary =>
                    TryTransferParameters(dictionary, invokeBuffer, out error),
                _ => throw new ArgumentException("Unsupported route parameters", nameof(parameters))
            };

            if (!transferred)
            {
                ApiResponse response = GetResponseForBadRequest(
                    routeContext,
                    error ?? "Bad parameters");
                ReturnInvokeBuffer(invokeBuffer);
                invokeBuffer = null;
                return ValueTask.FromResult(new ApiRouteInvocationResult(
                                                false,
                                                error,
                                                response));
            }

            object[] transferredBuffer = invokeBuffer;
            invokeBuffer = null;
            return TryInvokeInternalAsync(
                routeContext,
                transferredBuffer,
                cancellationToken);
        }
        catch
        {
            if (invokeBuffer != null)
                ReturnInvokeBuffer(invokeBuffer);
            throw;
        }
    }

    private ValueTask<ApiRouteInvocationResult> TryInvokeInternalAsync(
        ApiRouteContext   routeContext,
        object[]          rentedInvokeBuffer,
        CancellationToken cancellationToken)
    {
        object[]?                invokeBuffer      = rentedInvokeBuffer;
        CancellationTokenSource? routeCancellation = null;

        try
        {
            if (HasContextParameter)
                invokeBuffer[IsAsync ? 1 : 0] = routeContext;

            for (int i = 0; i < Hooks!.Length; ++i)
            {
                if (!Hooks[i].TryIntercept(routeContext, out ApiResponse? interceptResult))
                    continue;

                if (interceptResult == null)
                {
                    Logging.Log(
                        ELogSeverity.Error,
                        "[ApiRoute.TryInvokeInternalAsync] Invalid result from hook " +
                        $"'{Hooks[i].GetType().FullName}'");
                    return ValueTask.FromResult(new ApiRouteInvocationResult(
                                                    false,
                                                    "Invalid route hook result",
                                                    null));
                }

                return ValueTask.FromResult(new ApiRouteInvocationResult(
                                                true,
                                                null,
                                                interceptResult));
            }

            if (IsAsync)
            {
                routeCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);
                if (AsyncTimeout.HasValue)
                    routeCancellation.CancelAfter(AsyncTimeout.Value);
                invokeBuffer[0] = routeCancellation.Token;
            }

            object? returnValue = _compiledInvoker != null
                ? _compiledInvoker(invokeBuffer)
                : Method!.Invoke(null, invokeBuffer);

            ReturnInvokeBuffer(invokeBuffer);
            invokeBuffer = null;

            if (!IsAsync)
            {
                if (returnValue is not ApiResponse response)
                {
                    return ValueTask.FromResult(new ApiRouteInvocationResult(
                                                    false,
                                                    "Route returned an invalid response",
                                                    null));
                }

                return ValueTask.FromResult(new ApiRouteInvocationResult(
                                                true,
                                                null,
                                                response));
            }

            if (returnValue is not Task<ApiResponse> task)
            {
                return ValueTask.FromResult(new ApiRouteInvocationResult(
                                                false,
                                                "Async route returned an invalid task",
                                                null));
            }

            CancellationTokenSource ownedCancellation = routeCancellation!;
            routeCancellation = null;
            return AwaitRouteAsync(
                routeContext,
                task,
                ownedCancellation,
                cancellationToken);
        }
        catch (Exception exception)
        {
            return ValueTask.FromResult(HandleRouteException(routeContext, exception));
        }
        finally
        {
            if (invokeBuffer != null)
                ReturnInvokeBuffer(invokeBuffer);
            routeCancellation?.Dispose();
        }
    }

    private async ValueTask<ApiRouteInvocationResult> AwaitRouteAsync(
        ApiRouteContext         routeContext,
        Task<ApiResponse>       routeTask,
        CancellationTokenSource routeCancellation,
        CancellationToken       serverCancellationToken)
    {
        try
        {
            ApiResponse response = await routeTask
                .WaitAsync(routeCancellation.Token)
                .ConfigureAwait(false);

            if (response == null)
            {
                return new ApiRouteInvocationResult(
                    false,
                    "Route returned no response",
                    null);
            }

            return new ApiRouteInvocationResult(true, null, response);
        }
        catch (OperationCanceledException) when (serverCancellationToken.IsCancellationRequested)
        {
            ObserveLateFault(routeTask);
            throw;
        }
        catch (OperationCanceledException) when (routeCancellation.IsCancellationRequested)
        {
            ObserveLateFault(routeTask);
            return new ApiRouteInvocationResult(
                false,
                "Request failed/timeout",
                GetResponseForAsyncTimeout(routeContext));
        }
        catch (Exception exception)
        {
            return HandleRouteException(routeContext, exception);
        }
        finally
        {
            routeCancellation.Dispose();
        }
    }

    private ApiRouteInvocationResult HandleRouteException(
        ApiRouteContext routeContext,
        Exception       exception)
    {
        Exception routeException = exception is TargetInvocationException
        {
            InnerException: not null
        } invocationException
            ? invocationException.InnerException!
            : exception;

        if (routeException is OutOfMemoryException or StackOverflowException or
                              ThreadAbortException or ThreadInterruptedException)
            ExceptionDispatchInfo.Capture(routeException).Throw();

        Logging.Log(
            ELogSeverity.Error,
            $"[ApiRoute] Route '{Method!.DeclaringType?.FullName}.{Method.Name}' failed with exception:");
        Logging.Log(ELogSeverity.Error, routeException);

        return new ApiRouteInvocationResult(
            false,
            "Request failed",
            GetResponseForException(routeContext, routeException));
    }

    private static void ObserveLateFault(Task task)
    {
        if (task.IsCompleted)
        {
            _ = task.Exception;
            return;
        }

        _ = task.ContinueWith(
            static completedTask => _ = completedTask.Exception,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously |
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Transfer input parameters checking type
    /// </summary>
    /// <param name="inParameters">Input parameters</param>
    /// <param name="outError">Error if any</param>
    /// <returns>True if succeeded</returns>
    protected bool TryTransferParameters(JArray? inParameters, object[] invokeBuffer, out string? outError)
    {
        Array.Clear(invokeBuffer, 0, Parameters!.Length);

        outError = null;

        for (int i = UserParametersOffset; i < Parameters!.Length; ++i)
        {
            ParameterInfo parameterInfo = Parameters[i];
            string        parameterHint = ParametersHint![i];

            if (inParameters == null || i - UserParametersOffset >= inParameters.Count)
            {
                if (!parameterInfo.HasDefaultValue && !ParametersOptional![i])
                {
                    outError = $"Parameter {parameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                invokeBuffer[i] = parameterInfo.HasDefaultValue
                    ? parameterInfo.DefaultValue
                    : null;
#pragma warning restore CS8601
            }
            else
            {
                bool result = TypeConverter.TryGetValueAsFromJToken(
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
    protected bool TryTransferParameters(JObject? inParameters, object[] invokeBuffer, out string? outError)
    {
        Array.Clear(invokeBuffer, 0, Parameters!.Length);

        outError = null;

        for (int i = UserParametersOffset; i < Parameters!.Length; ++i)
        {
            ParameterInfo parameterInfo      = Parameters[i];
            string        parameterFixedName = ParametersFixedName![i];
            string        parameterHint      = ParametersHint![i];

            if (inParameters == null || !inParameters.ContainsKey(parameterFixedName))
            {
                if (!parameterInfo.HasDefaultValue && !ParametersOptional![i])
                {
                    outError = $"Parameter {parameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                invokeBuffer[i] = parameterInfo.HasDefaultValue
                    ? parameterInfo.DefaultValue
                    : null;
#pragma warning restore CS8601
            }
            else
            {
                bool result = TypeConverter.TryGetValueAsFromJToken(
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
    protected bool TryTransferParameters(
        IReadOnlyDictionary<string, string>? inParameters,
        object[]                             invokeBuffer,
        out string?                          outError)
    {
        Array.Clear(invokeBuffer, 0, Parameters!.Length);

        outError = null;

        for (int i = UserParametersOffset; i < Parameters!.Length; ++i)
        {
            ParameterInfo parameterInfo      = Parameters[i];
            string        parameterFixedName = ParametersFixedName![i];
            string        parameterHint      = ParametersHint![i];

            if (inParameters == null || !inParameters.TryGetValue(parameterFixedName, out string? dictInParameter))
            {
                if (!parameterInfo.HasDefaultValue && !ParametersOptional![i])
                {
                    outError = $"Parameter {parameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                invokeBuffer[i] = parameterInfo.HasDefaultValue
                    ? parameterInfo.DefaultValue
                    : null;
#pragma warning restore CS8601
            }
            else
            {
                bool result = TypeConverter.TryGetValueAsFromString(
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
    /// <param name="exception">Exception if any</param>
    /// <returns></returns>
    protected abstract ApiResponse GetResponseForException(ApiRouteContext routeContext, Exception exception);

    /// <summary>
    /// Get response for bad request
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="error">Error message</param>
    /// <returns></returns>
    protected abstract ApiResponse GetResponseForBadRequest(ApiRouteContext routeContext, string error);

    /// <summary>
    /// Get async timeout response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <returns></returns>
    protected abstract ApiResponse GetResponseForAsyncTimeout(ApiRouteContext routeContext);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Rent an invocation buffer
    /// </summary>
    /// <returns>Rented buffer</returns>
    private object[] RentInvokeBuffer()
    {
        if (_invokeBuffers!.TryTake(out object[]? buffer))
        {
            Interlocked.Decrement(ref _retainedInvokeBufferCount);
            return buffer;
        }

        return new object[Parameters!.Length];
    }

    /// <summary>
    /// Return an invocation buffer
    /// </summary>
    /// <param name="buffer">Buffer to return</param>
    private void ReturnInvokeBuffer(object[] buffer)
    {
        Array.Clear(buffer, 0, Parameters!.Length);
        if (Interlocked.Increment(ref _retainedInvokeBufferCount) <=
            MaxRetainedInvokeBuffers)
        {
            _invokeBuffers!.Add(buffer);
            return;
        }

        Interlocked.Decrement(ref _retainedInvokeBufferCount);
    }
}
