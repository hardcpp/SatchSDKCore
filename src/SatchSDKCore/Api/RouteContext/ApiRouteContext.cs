using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SSC.Api.RouteContext;

/// <summary>
/// Generic route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiRouteContext
{
    private Dictionary<string, object?>? _objects;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly Request.ApiRequest Request;

    public ApiJsonRpcRouteContext? AsJsonRpcRouteContext
        => this is ApiJsonRpcRouteContext casted ? casted : null;
    public ApiHttpRouteContext? AsHttpRouteContext
        => this is ApiHttpRouteContext casted ? casted : null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public ApiRouteContext(Request.ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Request = request;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add object to the context
    /// </summary>
    /// <typeparam name="t_Type">Object type</typeparam>
    /// <param name="object">Object to add</param>
    public void AddObject<t_Type>(string name, t_Type? @object)
    {
        if (_objects == null)
            _objects = new();

        if (_objects.ContainsKey(name))
        {
            Logging.Log(ELogSeverity.Warning, $"[CP_API_SDK.Network][RouteContext.AddObject<{typeof(t_Type).FullName}>] An object with the same type had been registered already");
            return;
        }

        _objects.Add(name, @object);
    }
    /// <summary>
    /// Try get object from the context
    /// </summary>
    /// <typeparam name="t_Type">Object type</typeparam>
    /// <param name="object">Result object</param>
    /// <returns>True if the object was found</returns>
    public bool TryGetObject<t_Type>(string name, out t_Type? @object)
    {
        @object = default;
        if (_objects == null)
            return false;

        if (!_objects.TryGetValue(name, out var outObject))
            return false;

        if (outObject != null && outObject.GetType() != typeof(t_Type?))
        {
            Logging.Log(ELogSeverity.Warning, $"[CP_API_SDK.Network][RouteContext.AddObject<{typeof(t_Type).FullName}>] An object with the same type had been registered already");
            return false;
        }

        @object = (t_Type?)outObject;
        return true;
    }
}
