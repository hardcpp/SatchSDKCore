using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SSC.APIServer.Blueprint;

/// <summary>
/// Generic blueprint base class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class IBlueprint
{
    public abstract Type BlueprintType { get; }
    public abstract Type RouteType     { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly string  Name;
    public readonly string? Prefix;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Name of the blueprint</param>
    /// <param name="prefix">Prefix of this blueprint if any</param>
    public IBlueprint(string name, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name   = name;
        Prefix = prefix;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a blueprint in this blueprint by reference
    /// </summary>
    /// <param name="blueprint">Blueprint to add</param>
    /// <exception cref="Exception">If the blueprint type does not match this one</exception>
    public void AddBlueprint(IBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        if (!BlueprintType.IsAssignableFrom(blueprint.GetType()))
        {
            throw new Exception(
                $"Blueprint '{blueprint.Name}' of type {blueprint.GetType().FullName} " +
                $"can not be registered in a blueprint '{Name}' of type {GetType().FullName}"
            );
        }

        RegisterBlueprint(blueprint);
    }
    /// <summary>
    /// Add routes of type
    /// </summary>
    /// <typeparam name="t_Type">Type to lookup</typeparam>
    /// <exception cref="Exception">If not methods where found in the type</exception>
    public void AddRoutesOf
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] t_Type>
        ()
        where t_Type : class
    {
        var l_TypeInfo = typeof(t_Type);
        var l_Methods  = l_TypeInfo.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (l_Methods == null || l_Methods.Length == 0)
            throw new Exception($"No methods found in type {l_TypeInfo.FullName} for collecting routes");

        for (var l_MI = 0; l_MI < l_Methods.Length; l_MI++)
        {
            var l_Method = l_Methods[l_MI];
            var l_Attributes = l_Method.GetCustomAttributes(RouteType);

            foreach (Route.IRoute l_Route in l_Attributes)
            {
                if (!RouteType.IsAssignableFrom(l_Route.GetType()))
                    throw new Exception($"Route of type {l_Route.GetType().FullName} can not be registered in a blueprint of type {GetType().FullName}");

                l_Route.Init(l_Method);
                RegisterRoute(l_Route);
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Register blueprint
    /// </summary>
    /// <param name="blueprint">Blueprint to register</param>
    protected abstract void RegisterBlueprint(IBlueprint blueprint);
    /// <summary>
    /// Register route
    /// </summary>
    /// <param name="route">Route to register</param>
    protected abstract void RegisterRoute(Route.IRoute route);
}
