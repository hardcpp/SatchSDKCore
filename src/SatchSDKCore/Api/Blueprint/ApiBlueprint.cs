using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SSC.Api.Blueprint;

/// <summary>
/// Generic blueprint base class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiBlueprint
{
    public abstract Type BlueprintType { get; }
    public abstract Type RouteType { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly string Name;
    public readonly string? Prefix;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Name of the blueprint</param>
    /// <param name="prefix">Prefix of this blueprint if any</param>
    public ApiBlueprint(string name, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        Prefix = prefix;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a blueprint in this blueprint by reference
    /// </summary>
    /// <param name="blueprint">Blueprint to add</param>
    /// <exception cref="Exception">If the blueprint type does not match this one</exception>
    public void AddBlueprint(ApiBlueprint blueprint)
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
    /// <typeparam name="TType">Type to lookup</typeparam>
    /// <exception cref="Exception">If not methods where found in the type</exception>
    public void AddRoutesOf
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TType>
        ()
        where TType : class
    {
        var typeInfo = typeof(TType);
        var methods = typeInfo.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (methods == null || methods.Length == 0)
            throw new Exception($"No methods found in type {typeInfo.FullName} for collecting routes");

        for (var mI = 0; mI < methods.Length; mI++)
        {
            var method = methods[mI];
            var attributes = method.GetCustomAttributes(RouteType);

            foreach (Route.ApiRoute route in attributes)
            {
                if (!RouteType.IsAssignableFrom(route.GetType()))
                    throw new Exception($"Route of type {route.GetType().FullName} can not be registered in a blueprint of type {GetType().FullName}");

                route.Init(method);
                RegisterRoute(route);
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Register blueprint
    /// </summary>
    /// <param name="blueprint">Blueprint to register</param>
    protected abstract void RegisterBlueprint(ApiBlueprint blueprint);
    /// <summary>
    /// Register route
    /// </summary>
    /// <param name="route">Route to register</param>
    protected abstract void RegisterRoute(Route.ApiRoute route);
}
