using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using SSC.Api.Route;
using SSC.Misc;

namespace SSC.Api.Blueprint;

/// <summary>
/// Generic blueprint base class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ApiBlueprint : IFreezable
{
    private static readonly object s_ConfigurationLock = new();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly List<ApiBlueprint> _childBlueprints = new();
    private          bool               _isFrozen;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly string  Name;
    public readonly string? Prefix;

    public abstract Type BlueprintType { get; }
    public abstract Type RouteType     { get; }
    public          bool IsFrozen      => Volatile.Read(ref _isFrozen);


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
    public void AddBlueprint(ApiBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        lock (s_ConfigurationLock)
        {
            ThrowIfFrozen();

            if (ReferenceEquals(this, blueprint) ||
                blueprint.ContainsBlueprint(this, new HashSet<ApiBlueprint>()))
            {
                throw new InvalidOperationException(
                    "Blueprint registration cannot create a cycle");
            }

            if (!BlueprintType.IsAssignableFrom(blueprint.GetType()))
            {
                throw new Exception(
                    $"Blueprint '{blueprint.Name}' of type {blueprint.GetType().FullName} " +
                    $"can not be registered in a blueprint '{Name}' of type {GetType().FullName}"
                );
            }

            RegisterBlueprint(blueprint);
            _childBlueprints.Add(blueprint);
        }
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
        lock (s_ConfigurationLock)
        {
            ThrowIfFrozen();

            Type typeInfo = typeof(TType);
            MethodInfo[]? methods =
                typeInfo.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (methods == null || methods.Length == 0)
                throw new Exception($"No methods found in type {typeInfo.FullName} for collecting routes");

            for (int mI = 0; mI < methods.Length; mI++)
            {
                MethodInfo             method     = methods[mI];
                IEnumerable<Attribute> attributes = method.GetCustomAttributes(RouteType);

                foreach (ApiRoute route in attributes)
                {
                    if (!RouteType.IsAssignableFrom(route.GetType()))
                    {
                        throw new Exception(
                            $"Route of type {route.GetType().FullName} can not be registered in a blueprint of type {GetType().FullName}");
                    }

                    route.Init(method);
                    RegisterRoute(route);
                }
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public void Freeze()
    {
        lock (s_ConfigurationLock)
        {
            if (_isFrozen)
                return;

            foreach (ApiBlueprint childBlueprint in _childBlueprints)
                childBlueprint.Freeze();

            Volatile.Write(ref _isFrozen, true);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Check if a blueprint contains an this blueprint (recursive)
    /// </summary>
    /// <param name="target">Target blueprint</param>
    /// <param name="visited">Hashset of visited blueprints</param>
    /// <returns>True if contained</returns>
    private bool ContainsBlueprint(
        ApiBlueprint          target,
        HashSet<ApiBlueprint> visited)
    {
        if (!visited.Add(this))
            return false;
        if (ReferenceEquals(this, target))
            return true;

        foreach (ApiBlueprint childBlueprint in _childBlueprints)
        {
            if (childBlueprint.ContainsBlueprint(target, visited))
                return true;
        }

        return false;
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
    protected abstract void RegisterRoute(ApiRoute route);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Throw if the blueprint is frozen
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected void ThrowIfFrozen()
    {
        if (IsFrozen)
            throw new InvalidOperationException($"Blueprint '{Name}' is frozen");
    }
}
