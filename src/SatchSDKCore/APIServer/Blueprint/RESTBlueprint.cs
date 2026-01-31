using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SSC.APIServer.Blueprint;

/// <summary>
/// REST blueprint
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class RESTBlueprint : IBlueprint
{
    public static int REST_METHOD_COUNT = Enum.GetValues<Route.ERestMethod>().Length;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override Type BlueprintType => typeof(RESTBlueprint);
    public override Type RouteType => typeof(Route.RESTRoute);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    internal Internal.RESTRuleTreeNode m_RouteTreeNode;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="prefix"></param>
    /// <exception cref="Exception"></exception>
    public RESTBlueprint(string name, string? prefix = null)
        : base(name, prefix)
    {
        if (prefix != null && prefix.Length > 0)
        {
            if (prefix[0] == '/' || prefix[^1] == '/')
                throw new Exception($"RESTBlueprint prefixes can not start or end by '/' -> {prefix}");
        }

        /// Create the root tree node
        m_RouteTreeNode = new Internal.RESTRuleTreeNode()
        {
            Parent = null,
            Key = null!,
            IsArg = false
        };

        /// Register the prefix
        TryGetRESTRuleTreeNodeFor(ComposeRule(Prefix), createMissings: true, out _);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Register blueprint
    /// </summary>
    /// <param name="blueprint">Blueprint to register</param>
    protected override void RegisterBlueprint(IBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        var restBlueprint = blueprint as RESTBlueprint;

        if (!TryGetRESTRuleTreeNodeFor(ComposeRule(Prefix), createMissings: true, out var targetRuleTreeNode))
            throw new Exception($"Failed to register RESTBlueprint {restBlueprint!.Name}");

        if (Array.IndexOf(targetRuleTreeNode!.Blueprints, restBlueprint!) != -1)
            throw new Exception($"RESTBlueprint '{restBlueprint!.Name}' is already registered in RESTBlueprint '{Name}'");

        Array.Resize(ref targetRuleTreeNode.Blueprints, targetRuleTreeNode.Blueprints.Length + 1);
        targetRuleTreeNode.Blueprints[^1] = restBlueprint!;
    }
    /// <summary>
    /// Register route
    /// </summary>
    /// <param name="route">Route to register</param>
    protected override void RegisterRoute(Route.IRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var rESTRoute = route as Route.RESTRoute;
        var rule = ComposeRule(Prefix, rESTRoute!.RESTEndpoint);

        if (!TryGetRESTRuleTreeNodeFor(rule, createMissings: true, out var targetRuleTreeNode))
            throw new Exception($"Failed to register REST rule {rESTRoute.RESTMethod}:{rule}");

        if (targetRuleTreeNode!.Routes[(int)rESTRoute.RESTMethod] != null)
            throw new Exception($"A route for REST rule {rESTRoute.RESTMethod}:{rule} already exist");

        targetRuleTreeNode.Routes[(int)rESTRoute.RESTMethod] = rESTRoute;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to get a RESTRuleTreeNode from composed rule
    /// </summary>
    /// <param name="composedRule">Compose rule</param>
    /// <param name="createMissings">Create missing nodes on the path?</param>
    /// <param name="result">Output result</param>
    /// <returns>True if a RESTRuleTreeNode was found</returns>
    private bool TryGetRESTRuleTreeNodeFor(string composedRule, bool createMissings, out Internal.RESTRuleTreeNode? result)
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
                var nextSub = new Internal.RESTRuleTreeNode()
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
    /// <param name="restMethod">REST method</param>
    /// <param name="segments">Rule segments</param>
    /// <param name="argumentsCollector">Arguments collector</param>
    /// <param name="restRoute">Output found RESTRoute</param>
    /// <returns>True if a route is found</returns>
    public bool TryFindRoute(
        Route.ERestMethod restMethod,
        ReadOnlySpan<string> segments,
        Dictionary<string, string> argumentsCollector,
        out Route.RESTRoute? restRoute)
    {
        restRoute = null;

        var routeTreeNode = m_RouteTreeNode.Walk(segments, 0, argumentsCollector);
        if (routeTreeNode == null)
            return false;

        restRoute = routeTreeNode.Routes[(int)restMethod];
        return restRoute != null;
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
                builder.Append("/");
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
