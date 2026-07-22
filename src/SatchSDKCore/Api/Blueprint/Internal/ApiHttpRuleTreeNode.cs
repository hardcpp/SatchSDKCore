using System;
using System.Collections.Generic;
using System.Web;
using SSC.Api.Route;

namespace SSC.Api.Blueprint.Internal;

/// <summary>
/// REST rule tree node
/// </summary>
internal class ApiHttpRuleTreeNode
{
    internal readonly ApiHttpRoute?[]    Routes     = new ApiHttpRoute?[ApiHttpBlueprint.HTTP_METHOD_COUNT];
    internal          ApiHttpBlueprint[] Blueprints = Array.Empty<ApiHttpBlueprint>();

    internal ApiHttpRuleTreeNode[] Childs = Array.Empty<ApiHttpRuleTreeNode>();
    internal bool                  IsArg;
    internal string                Key = null!;
    internal ApiHttpRuleTreeNode?  Parent;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Walk the tree node recursively
    /// </summary>
    /// <param name="segments">Request segments</param>
    /// <param name="segmentIndex">Current segment index</param>
    /// <param name="argumentsCollector">Arguments collector</param>
    /// <returns>Found RESTRuleTreeNode, null if not found</returns>
    internal ApiHttpRuleTreeNode? Walk(
        ReadOnlySpan<string>       segments,
        int                        segmentIndex,
        Dictionary<string, string> argumentsCollector)
    {
        if (segmentIndex == segments.Length)
            return this;

        string current = segments[segmentIndex];

        // First, try to match child nodes
        for (int cI = 0; cI < Childs.Length; ++cI)
        {
            ApiHttpRuleTreeNode child = Childs[cI];

            if (child.IsArg)
            {
                // URL decode the argument value before storing it
                argumentsCollector[child.Key] = HttpUtility.UrlDecode(current);

                ApiHttpRuleTreeNode? result = child.Walk(segments, segmentIndex + 1, argumentsCollector);
                if (result != null)
                    return result;

                argumentsCollector.Remove(child.Key);
            }
            else if (child.Key == current)
            {
                ApiHttpRuleTreeNode? result = child.Walk(segments, segmentIndex + 1, argumentsCollector);
                if (result != null)
                    return result;
            }
        }

        // If no child matched, try blueprints at this node
        for (int bI = 0; bI < Blueprints.Length; ++bI)
        {
            ApiHttpBlueprint blueprint = Blueprints[bI];

            // Try to match the remaining segments through the blueprint
            ReadOnlySpan<string> remainingSegments = segments.Slice(segmentIndex);
            ApiHttpRuleTreeNode? result = blueprint._routeTreeNode.Walk(remainingSegments, 0, argumentsCollector);

            if (result != null)
                return result;
        }

        return null;
    }
}
