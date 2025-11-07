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
    public override Type RouteType     => typeof(Route.RESTRoute);

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
            Key    = null!,
            IsArg  = false
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

        var l_RESTBlueprint = blueprint as RESTBlueprint;

        if (!TryGetRESTRuleTreeNodeFor(ComposeRule(Prefix), createMissings: true, out var l_TargetRuleTreeNode))
            throw new Exception($"Failed to register RESTBlueprint {l_RESTBlueprint!.Name}");

        if (Array.IndexOf(l_TargetRuleTreeNode!.Blueprints, l_RESTBlueprint!) != -1)
            throw new Exception($"RESTBlueprint '{l_RESTBlueprint!.Name}' is already registered in RESTBlueprint '{Name}'");

        Array.Resize(ref l_TargetRuleTreeNode.Blueprints, l_TargetRuleTreeNode.Blueprints.Length + 1);
        l_TargetRuleTreeNode.Blueprints[^1] = l_RESTBlueprint!;
    }
    /// <summary>
    /// Register route
    /// </summary>
    /// <param name="route">Route to register</param>
    protected override void RegisterRoute(Route.IRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var l_RESTRoute = route as Route.RESTRoute;
        var l_Rule = ComposeRule(Prefix, l_RESTRoute!.RESTEndpoint);

        if (!TryGetRESTRuleTreeNodeFor(l_Rule, createMissings: true, out var l_TargetRuleTreeNode))
            throw new Exception($"Failed to register REST rule {l_RESTRoute.RESTMethod}:{l_Rule}");

        if (l_TargetRuleTreeNode!.Routes[(int)l_RESTRoute.RESTMethod] != null)
            throw new Exception($"A route for REST rule {l_RESTRoute.RESTMethod}:{l_Rule} already exist");

        l_TargetRuleTreeNode.Routes[(int)l_RESTRoute.RESTMethod] = l_RESTRoute;
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

        var l_Parts = composedRule.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var l_CurrentNode = m_RouteTreeNode;

        for (var l_SegI = 0; l_SegI < l_Parts.Length; l_SegI++)
        {
            var l_CurrentSegment = l_Parts[l_SegI];
            var l_Key            = l_CurrentSegment;
            var l_IsArg          = l_CurrentSegment[0] == '<' && l_CurrentSegment[^1] == '>';

            if (l_IsArg)
                l_Key = l_Key[1..^1];

            var l_ExistingSub = l_CurrentNode!.Childs?.FirstOrDefault(x => x.Key == l_Key);

            if (l_ExistingSub != null)
                l_CurrentNode = l_ExistingSub;
            else if (createMissings)
            {
                var l_NextSub = new Internal.RESTRuleTreeNode()
                {
                    Parent = l_CurrentNode,
                    Key    = l_Key,
                    IsArg  = l_IsArg
                };

                Array.Resize(ref l_CurrentNode!.Childs, l_CurrentNode!.Childs!.Length + 1);
                l_CurrentNode.Childs[^1] = l_NextSub;

                l_CurrentNode = l_NextSub;
            }
            else
                return false;
        }

        result = l_CurrentNode;
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

        var l_RouteTreeNode = m_RouteTreeNode.Walk(segments, 0, argumentsCollector);
        if (l_RouteTreeNode == null)
            return false;

        restRoute = l_RouteTreeNode.Routes[(int)restMethod];
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
        var l_Builder = new StringBuilder();
        for (int l_I = 0; l_I < segments.Length; l_I++)
        {
            var l_Current = segments[l_I];
            if (string.IsNullOrEmpty(l_Current))
                continue;

            if (l_I != 0)
                l_Builder.Append("/");
            if (l_Current.Length >= 2 && l_Current[0] == '/' && l_Current[^1] == '/')
                l_Builder.Append(l_Current, 1, l_Current.Length - 2);
            else if (l_Current[0] == '/')
                l_Builder.Append(l_Current, 1, l_Current.Length - 1);
            else if (l_Current[^1] == '/')
                l_Builder.Append(l_Current, 0, l_Current.Length - 1);
            else
                l_Builder.Append(l_Current);
        }

        return l_Builder.ToString();
    }
}
