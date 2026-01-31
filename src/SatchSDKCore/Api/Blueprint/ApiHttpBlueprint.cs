using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SSC.Api.Blueprint;

/// <summary>
/// HTTP blueprint
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class ApiHttpBlueprint : ApiBlueprint
{
    public static readonly int HTTP_METHOD_COUNT = Enum.GetValues<Route.EApiHttpMethod>().Length;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override Type BlueprintType => typeof(ApiHttpBlueprint);
    public override Type RouteType => typeof(Route.ApiHttpRoute);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    internal Internal.ApiHttpRuleTreeNode m_RouteTreeNode;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="prefix"></param>
    /// <exception cref="Exception"></exception>
    public ApiHttpBlueprint(string name, string? prefix = null)
        : base(name, prefix)
    {
        if (prefix != null && prefix.Length > 0)
        {
            if (prefix[0] == '/' || prefix[^1] == '/')
                throw new Exception($"ApiHttpBlueprint prefixes can not start or end by '/' -> {prefix}");
        }

        /// Create the root tree node
        m_RouteTreeNode = new Internal.ApiHttpRuleTreeNode
        {
            Parent = null,
            Key = null!,
            IsArg = false
        };

        /// Register the prefix
        TryGetHttpRuleTreeNodeFor(ComposeRule(Prefix), createMissings: true, out _);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Register blueprint
    /// </summary>
    /// <param name="blueprint">Blueprint to register</param>
    protected override void RegisterBlueprint(ApiBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        var httpBlueprint = (ApiHttpBlueprint)blueprint;

        if (!TryGetHttpRuleTreeNodeFor(ComposeRule(Prefix), createMissings: true, out var targetRuleTreeNode))
            throw new Exception($"Failed to register ApiHttpBlueprint {httpBlueprint.Name}");

        if (Array.IndexOf(targetRuleTreeNode!.Blueprints, httpBlueprint) != -1)
            throw new Exception($"ApiHttpBlueprint '{httpBlueprint.Name}' is already registered in ApiHttpBlueprint '{Name}'");

        Array.Resize(ref targetRuleTreeNode.Blueprints, targetRuleTreeNode.Blueprints.Length + 1);
        targetRuleTreeNode.Blueprints[^1] = httpBlueprint;
    }
    /// <summary>
    /// Register route
    /// </summary>
    /// <param name="route">Route to register</param>
    protected override void RegisterRoute(Route.ApiRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var httpRoute = (Route.ApiHttpRoute)route;
        var rule = ComposeRule(Prefix, httpRoute.HttpEndpoint);

        if (!TryGetHttpRuleTreeNodeFor(rule, createMissings: true, out var targetRuleTreeNode))
            throw new Exception($"Failed to register HTTP rule {httpRoute.HttpMethod}:{rule}");

        if (targetRuleTreeNode.Routes[(int)httpRoute.HttpMethod] != null)
            throw new Exception($"A route for HTTP rule {httpRoute.HttpMethod}:{rule} already exist");

        targetRuleTreeNode.Routes[(int)httpRoute.HttpMethod] = httpRoute;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to get a ApiHttpRuleTreeNode from composed rule
    /// </summary>
    /// <param name="composedRule">Compose rule</param>
    /// <param name="createMissings">Create missing nodes on the path?</param>
    /// <param name="result">Output result</param>
    /// <returns>True if a ApiHttpRuleTreeNode was found</returns>
    private bool TryGetHttpRuleTreeNodeFor(string composedRule, bool createMissings, [NotNullWhen(true)] out Internal.ApiHttpRuleTreeNode? result)
    {
        result = null;

        ArgumentNullException.ThrowIfNull(composedRule);

        var parts = composedRule.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentNode = m_RouteTreeNode;

        for (var segI = 0; segI < parts.Length; segI++)
        {
            var currentSegment = parts[segI];
            var key = currentSegment;
            var isArg = currentSegment[0] == '<' && currentSegment[^1] == '>';

            if (isArg)
                key = key[1..^1];

            var existingSub = currentNode!.Childs?.FirstOrDefault(x => x.Key == key);

            if (existingSub != null)
                currentNode = existingSub;
            else if (createMissings)
            {
                var nextSub = new Internal.ApiHttpRuleTreeNode
                {
                    Parent = currentNode,
                    Key = key,
                    IsArg = isArg
                };

                Array.Resize(ref currentNode!.Childs, currentNode!.Childs!.Length + 1);
                currentNode.Childs[^1] = nextSub;

                currentNode = nextSub;
            }
            else
                return false;
        }

        result = currentNode;
        return result != null;
    }
    /// <summary>
    /// Try to find a route
    /// </summary>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="segments">Rule segments</param>
    /// <param name="argumentsCollector">Arguments collector</param>
    /// <param name="httpRoute">Output found ApiHttpRoute</param>
    /// <returns>True if a route is found</returns>
    public bool TryFindRoute(
        Route.EApiHttpMethod httpMethod,
        ReadOnlySpan<string> segments,
        Dictionary<string, string> argumentsCollector,
        [NotNullWhen(true)] out Route.ApiHttpRoute? httpRoute)
    {
        httpRoute = null;

        var routeTreeNode = m_RouteTreeNode.Walk(segments, 0, argumentsCollector);
        if (routeTreeNode == null)
            return false;

        httpRoute = routeTreeNode.Routes[(int)httpMethod];
        return httpRoute != null;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Compose a rule from segments
    /// </summary>
    /// <param name="segments">Input segments</param>
    /// <returns></returns>
    private string ComposeRule(params string?[] segments)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < segments.Length; i++)
        {
            var current = segments[i];
            if (string.IsNullOrEmpty(current))
                continue;

            if (i != 0)
                builder.Append('/');
            if (current.Length >= 2 && current[0] == '/' && current[^1] == '/')
                builder.Append(current, 1, current.Length - 2);
            else if (current[0] == '/')
                builder.Append(current, 1, current.Length - 1);
            else if (current[^1] == '/')
                builder.Append(current, 0, current.Length - 1);
            else
                builder.Append(current);
        }

        return builder.ToString();
    }
}
