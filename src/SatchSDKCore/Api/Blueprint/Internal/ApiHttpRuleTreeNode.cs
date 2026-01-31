using System;
using System.Collections.Generic;
using System.Web;

namespace SSC.Api.Blueprint.Internal;

/// <summary>
/// REST rule tree node
/// </summary>
internal class ApiHttpRuleTreeNode
{
    internal ApiHttpRuleTreeNode? Parent;
    internal string Key = null!;
    internal bool IsArg;

    internal ApiHttpRuleTreeNode[] Childs = Array.Empty<ApiHttpRuleTreeNode>();
    internal ApiHttpBlueprint[] Blueprints = Array.Empty<ApiHttpBlueprint>();
    internal Route.ApiHttpRoute?[] Routes = new Route.ApiHttpRoute?[ApiHttpBlueprint.HTTP_METHOD_COUNT];

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Walk the tree node recursively
    /// </summary>
    /// <param name="segments">Request segments</param>
    /// <param name="segmentIndex">Current segment index</param>
    /// <param name="argumentsCollector">Arguments collector</param>
    /// <returns>Found RESTRuleTreeNode, null if not found</returns>
    internal ApiHttpRuleTreeNode? Walk(ReadOnlySpan<string> segments, int segmentIndex, Dictionary<string, string> argumentsCollector)
    {
        if (segmentIndex == segments.Length)
            return this;

        var current = segments[segmentIndex];

        // First, try to match child nodes
        for (var cI = 0; cI < Childs.Length; ++cI)
        {
            var child = Childs[cI];

            if (child.IsArg)
            {
                // URL decode the argument value before storing it
                argumentsCollector[child.Key] = HttpUtility.UrlDecode(current);

                var result = child.Walk(segments, segmentIndex + 1, argumentsCollector);
                if (result != null)
                    return result;

                argumentsCollector.Remove(child.Key);
            }
            else if (child.Key == current)
            {
                var result = child.Walk(segments, segmentIndex + 1, argumentsCollector);
                if (result != null)
                    return result;
            }
        }

        // If no child matched, try blueprints at this node
        for (var bI = 0; bI < Blueprints.Length; ++bI)
        {
            var blueprint = Blueprints[bI];

            // Try to match the remaining segments through the blueprint
            var remainingSegments = segments.Slice(segmentIndex);
            var result = blueprint.m_RouteTreeNode.Walk(remainingSegments, 0, argumentsCollector);

            if (result != null)
                return result;
        }

        return null;
    }
}