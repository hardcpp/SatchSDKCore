using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.APIServer.Route;

/// <summary>
/// Generic route class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class IRoute : Attribute
{
    public MethodInfo?             Method               { get; private set; }
    public ParameterInfo[]?        Parameters           { get; private set; }
    public Type[]?                 ParametersType       { get; private set; }
    public bool[]?                 ParametersOptional   { get; private set; }
    public string[]?               ParametersFixedName  { get; private set; }
    public string[]?               ParametersHint       { get; private set; }
    public RouteHook.IRouteHook[]? Hooks                { get; private set; }
    public bool                    IsAsync              { get; private set; }
    public TimeSpan?               AsyncTimeout         { get; private set; }
    public bool                    HasContextParameter  { get; private set; }
    public int                     UserParametersOffset { get; private set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private ThreadLocal<object[]>? m_InvokeBuffer;
    private string?                m_AsyncTimeoutStr = null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="asyncTimeoutStr">Timeout for async</param>
    public IRoute(string? asyncTimeoutStr = null)
    {
        m_AsyncTimeoutStr = asyncTimeoutStr;
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

        Method              = method;
        Parameters          = method.GetParameters();
        ParametersType      = new Type[Parameters.Length];
        ParametersOptional  = new bool[Parameters.Length];
        ParametersFixedName = new string[Parameters.Length];
        ParametersHint      = new string[Parameters.Length];

        if (!Method.ReturnType.IsAssignableTo(typeof(Response.IResponse)))
            throw new Exception($"Route {method.Name} has wrong return Type, should be of IResponse");

        var l_Hooks = Method.GetCustomAttributes<RouteHook.IRouteHook>(true);
        Hooks = l_Hooks.Any() ? l_Hooks.ToArray() : Array.Empty<RouteHook.IRouteHook>();

        IsAsync = method.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
        if (IsAsync)
        {
            if (Parameters.Length == 0 || Parameters[0].ParameterType != typeof(CancellationToken))
                throw new Exception($"Route {method.Name} has wrong parameter Type, Arg 0 should be of CancellationToken");

            if (Parameters.Length >= 2 && typeof(RouteContext.IRouteContext).IsAssignableFrom(Parameters[1].ParameterType))
                HasContextParameter = true;

            UserParametersOffset = HasContextParameter ? 2 : 1;

            if (m_AsyncTimeoutStr != null)
                AsyncTimeout = m_AsyncTimeoutStr != null ? TimeSpan.ParseExact(m_AsyncTimeoutStr, @"m\:s\.fff", System.Globalization.CultureInfo.InvariantCulture) : null;
            else
                AsyncTimeout = TimeSpan.FromSeconds(60);
        }
        else
        {
            if (Parameters.Length > 0 && typeof(RouteContext.IRouteContext).IsAssignableFrom(Parameters[0].ParameterType))
                HasContextParameter = true;

            UserParametersOffset = HasContextParameter ? 1 : 0;

            if (m_AsyncTimeoutStr != null)
                throw new Exception($"Route {method.Name} has timeout but is not async");
        }

        for (var l_I = 0; l_I < Parameters.Length; ++l_I)
        {
            var l_Parameter  = Parameters[l_I]!;
            var l_IsOptional = l_Parameter.ParameterType.IsGenericType && l_Parameter.ParameterType.GetGenericTypeDefinition().Equals(typeof(Nullable<>));

            ParametersType[l_I]      = l_IsOptional ? l_Parameter.ParameterType.GenericTypeArguments[0] : l_Parameter.ParameterType;
            ParametersOptional[l_I]  = l_IsOptional;
            ParametersFixedName[l_I] = l_Parameter.Name!.StartsWith("p_") ? l_Parameter.Name[2..] : l_Parameter.Name;

            if (l_I < UserParametersOffset)
            {
                ParametersHint[l_I] = string.Empty;
                continue;
            }

            ParametersHint[l_I] = $"{(l_I - UserParametersOffset) + 1}:{ParametersFixedName[l_I]}";
        }

        m_InvokeBuffer = new ThreadLocal<object[]>(() => new object[Parameters.Length]);
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
    public bool TryInvoke(RouteContext.IRouteContext routeContext, JArray parameters, out string? outError, out Response.IResponse? outResponse)
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
    public bool TryInvoke(RouteContext.IRouteContext routeContext, JObject parameters, out string? outError, out Response.IResponse? outResponse)
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
    public bool TryInvoke(RouteContext.IRouteContext routeContext, IReadOnlyDictionary<string, string> parameters, out string? outError, out Response.IResponse? outResponse)
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
    private bool TryInvokeInternal(RouteContext.IRouteContext routeContext, out string? outError, out Response.IResponse? outResponse)
    {
        outError    = null;
        outResponse = null;

        object[] l_InvokeBuffer = m_InvokeBuffer!.Value!;

        if (HasContextParameter)
            l_InvokeBuffer[IsAsync ? 1 : 0] = routeContext;

        for (var l_I = 0; l_I < Hooks!.Length; ++l_I)
        {
            if (!Hooks![l_I].TryIntercept(routeContext, out var l_InterceptResult))
                continue;

            if (l_InterceptResult == null)
            {
                Logging.Log(ELogSeverity.Error, $"[IRoute.TryInvokeInternal] Invalid result type for hook {Hooks[l_I].GetType().FullName}");

                Array.Clear(l_InvokeBuffer);

                return false;
            }

            outResponse = l_InterceptResult;
            Array.Clear(l_InvokeBuffer);

            return true;
        }

        var l_CancellationTokenSource = null as CancellationTokenSource;
        if (IsAsync)
        {
            l_CancellationTokenSource = new CancellationTokenSource();
            l_InvokeBuffer[0]         = l_CancellationTokenSource.Token;
        }

        var l_Return = null as object;
        try
        {
            l_Return = Method!.Invoke(null, l_InvokeBuffer);
        }
        catch (Exception l_Exception)
        {
            Logging.Log(ELogSeverity.Error, $"[IRoute.TryInvokeInternal] Route {Method!.GetType().FullName} failed with exception:");
            Logging.Log(ELogSeverity.Error, l_Exception);

            outError = "Request failed";
            outResponse = GetResponseForException(routeContext, l_Exception);

            return false;
        }

        if (l_Return != null)
        {
            var l_ReturnValue = null as object;
            if (IsAsync)
            {
                var l_Task = l_Return as Task<Response.IResponse>;
                if (l_Task!.Exception?.InnerException != null)
                    throw l_Task.Exception?.InnerException!;
                else if (AsyncTimeout.HasValue)
                    l_Task.Wait(AsyncTimeout.Value);
                else
                    l_Task.Wait();

                if (l_Task.IsCompletedSuccessfully)
                    outResponse = l_Task.Result;
                else
                {
                    if (l_Task.Exception?.InnerException != null || l_Task.Exception != null)
                    {
                        var l_Exception = l_Task.Exception?.InnerException ?? l_Task.Exception;
                        Logging.Log(ELogSeverity.Error, $"[IRoute.TryInvokeInternal] Route {Method.GetType().FullName} failed with exception:");
                        Logging.Log(ELogSeverity.Error, l_Exception!);

                        outError    = "Request failed";
                        outResponse = GetResponseForException(routeContext, l_Exception!);

                        return false;
                    }
                    else if (AsyncTimeout.HasValue)
                        l_CancellationTokenSource?.Cancel();

                    outError    = "Request failed/timeout";
                    outResponse = GetResponseForAsyncTimeout(routeContext);

                    return false;
                }
            }
            else
                outResponse = l_Return as Response.IResponse;
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
        object[] l_InvokeBuffer = m_InvokeBuffer!.Value!;
        Array.Clear(l_InvokeBuffer);

        outError = null;

        for (var l_I = UserParametersOffset; l_I < Parameters!.Length; ++l_I)
        {
            var l_ParameterInfo      = Parameters[l_I];
            var l_ParameterFixedName = ParametersFixedName![l_I];
            var l_ParameterHint      = ParametersHint![l_I];

            if (inParameters == null || (l_I - UserParametersOffset) >= inParameters.Count)
            {
                if (!l_ParameterInfo.HasDefaultValue && !ParametersOptional![l_I])
                {
                    outError = $"Parameter {l_ParameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                l_InvokeBuffer[l_I] = ParametersOptional![l_I] ? null : l_ParameterInfo.DefaultValue!;
#pragma warning restore CS8601
            }
            else
            {
                var l_Result = Reflection.TypeConverter.TryGetValueAsFromJToken(
                    ParametersType![l_I],
                    inParameters[l_I - UserParametersOffset],
                    l_ParameterHint,
                    out outError,
                    ref l_InvokeBuffer[l_I]
                );

                if (!l_Result)
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
        object[] l_InvokeBuffer = m_InvokeBuffer!.Value!;
        Array.Clear(l_InvokeBuffer);

        outError = null;

        for (var l_I = UserParametersOffset; l_I < Parameters!.Length; ++l_I)
        {
            var l_ParameterInfo      = Parameters[l_I];
            var l_ParameterFixedName = ParametersFixedName![l_I];
            var l_ParameterHint      = ParametersHint![l_I];

            if (inParameters == null || !inParameters.ContainsKey(l_ParameterFixedName))
            {
                if (!l_ParameterInfo.HasDefaultValue && !ParametersOptional![l_I])
                {
                    outError = $"Parameter {l_ParameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                l_InvokeBuffer[l_I] = ParametersOptional![l_I] ? null : l_ParameterInfo.DefaultValue!;
#pragma warning restore CS8601
            }
            else
            {
                var l_Result = Reflection.TypeConverter.TryGetValueAsFromJToken(
                    ParametersType![l_I],
                    inParameters![l_ParameterFixedName]!,
                    l_ParameterHint,
                    out outError,
                    ref l_InvokeBuffer[l_I]
                );

                if (!l_Result)
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
        object[] l_InvokeBuffer = m_InvokeBuffer!.Value!;
        Array.Clear(l_InvokeBuffer);

        outError = null;

        for (var l_I = UserParametersOffset; l_I < Parameters!.Length; ++l_I)
        {
            var l_ParameterInfo      = Parameters[l_I];
            var l_ParameterFixedName = ParametersFixedName![l_I];
            var l_ParameterHint      = ParametersHint![l_I];

            if (inParameters == null || !inParameters.ContainsKey(l_ParameterFixedName))
            {
                if (!l_ParameterInfo.HasDefaultValue && !ParametersOptional![l_I])
                {
                    outError = $"Parameter {l_ParameterHint} is missing";
                    return false;
                }

#pragma warning disable CS8601
                l_InvokeBuffer[l_I] = ParametersOptional![l_I] ? null : l_ParameterInfo.DefaultValue!;
#pragma warning restore CS8601
            }
            else
            {
                var l_Result = Reflection.TypeConverter.TryGetValueAsFromString(
                    ParametersType![l_I],
                    inParameters?[l_ParameterFixedName] ?? string.Empty,
                    l_ParameterHint,
                    out outError,
                    ref l_InvokeBuffer[l_I]
                );

                if (!l_Result)
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
    protected abstract Response.IResponse GetResponseForException(RouteContext.IRouteContext routeContext, Exception p_Exception);
    /// <summary>
    /// Get response for bad request
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <param name="error">Error message</param>
    /// <returns></returns>
    protected abstract Response.IResponse GetResponseForBadRequest(RouteContext.IRouteContext routeContext, string error);
    /// <summary>
    /// Get async timeout response
    /// </summary>
    /// <param name="routeContext">Route context</param>
    /// <returns></returns>
    protected abstract Response.IResponse GetResponseForAsyncTimeout(RouteContext.IRouteContext routeContext);
}
