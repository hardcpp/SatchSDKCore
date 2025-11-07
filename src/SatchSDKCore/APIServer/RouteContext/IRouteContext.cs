using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SSC.APIServer.RouteContext;

/// <summary>
/// Generic route context
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class IRouteContext
{
    private Dictionary<string, object?>? m_Objects;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly Request.IRequest Request;

    public JSONRPCRouteContext? AsJSONRPCRouteContext
        => this is JSONRPCRouteContext casted ? casted : null;
    public RESTRouteContext? AsRESTRouteContext
        => this is RESTRouteContext casted ? casted : null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="request">Origin request</param>
    public IRouteContext(Request.IRequest request)
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
        if (m_Objects == null)
            m_Objects = new();

        if (m_Objects.ContainsKey(name))
        {
            Logging.Log(ELogSeverity.Warning, $"[CP_API_SDK.Network][RouteContext.AddObject<{typeof(t_Type).FullName}>] An object with the same type had been registered already");
            return;
        }

        m_Objects.Add(name, @object);
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
        if (m_Objects == null)
            return false;

        if (!m_Objects.TryGetValue(name, out var l_Object))
            return false;

        if (l_Object != null && l_Object.GetType() != typeof(t_Type?))
        {
            Logging.Log(ELogSeverity.Warning, $"[CP_API_SDK.Network][RouteContext.AddObject<{typeof(t_Type).FullName}>] An object with the same type had been registered already");
            return false;
        }

        @object = (t_Type?)l_Object;
        return true;
    }
}
