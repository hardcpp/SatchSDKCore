using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SSC.Api.Request;

namespace SSC.Api.RouteContext;

/// <summary>
/// Generic route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiRouteContext
{
    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private Dictionary<string, object?>? _objects;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly ApiRequest Request;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public ApiRouteContext(ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Request = request;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add object to the context
    /// </summary>
    /// <typeparam name="TObjectType">Object type</typeparam>
    /// <param name="object">Object to add</param>
    public void AddObject<TObjectType>(string name, TObjectType? @object)
    {
        if (_objects == null)
            _objects = new Dictionary<string, object?>();

        if (_objects.ContainsKey(name))
        {
            Logging.Log(ELogSeverity.Warning,
                        $"[CP_API_SDK.Network][RouteContext.AddObject<{typeof(TObjectType).FullName}>] An object with the same type had been registered already");
            return;
        }

        _objects.Add(name, @object);
    }

    /// <summary>
    /// Try get object from the context
    /// </summary>
    /// <typeparam name="TObjectType">Object type</typeparam>
    /// <param name="object">Result object</param>
    /// <returns>True if the object was found</returns>
    public bool TryGetObject<TObjectType>(string name, out TObjectType? @object)
    {
        @object = default;
        if (_objects == null)
            return false;

        if (!_objects.TryGetValue(name, out object? outObject))
            return false;

        if (outObject != null && outObject.GetType() != typeof(TObjectType?))
        {
            Logging.Log(ELogSeverity.Warning,
                        $"[CP_API_SDK.Network][RouteContext.AddObject<{typeof(TObjectType).FullName}>] An object with the same type had been registered already");
            return false;
        }

        @object = (TObjectType?)outObject;
        return true;
    }
}
