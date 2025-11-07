using System;
using System.Collections.Generic;

namespace SSC.APIServer.Blueprint.Internal;

/// <summary>
/// REST Rule tree node
/// </summary>
internal class RESTRuleTreeNode
{
    internal required RESTRuleTreeNode?  Parent;
    internal required string             Key;
    internal required bool               IsArg;
    internal          RESTRuleTreeNode[] Childs     = Array.Empty<RESTRuleTreeNode>();
    internal          RESTBlueprint[]    Blueprints = Array.Empty<RESTBlueprint>();
    internal          Route.RESTRoute[]  Routes     = new Route.RESTRoute[RESTBlueprint.REST_METHOD_COUNT];

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Walk recursivly the tree node from this node
    /// </summary>
    /// <param name="segments">Path segments</param>
    /// <param name="startPosition">Current position</param>
    /// <param name="arguments">Argument collector</param>
    /// <returns>The first valid RESTRoutereeNode found for the given parameters</returns>
    internal RESTRuleTreeNode? Walk(ReadOnlySpan<string> segments, int startPosition, Dictionary<string, string> arguments)
    {
        if (IsArg)
            arguments[Key] = Uri.UnescapeDataString(segments[startPosition - 1]);
        else if (Key != null && segments[startPosition - 1] != Key)
            return null;

        if (startPosition == segments.Length)
        {
            for (var l_I = 0; l_I < Blueprints.Length; ++l_I)
            {
                var l_Res = Blueprints[l_I].m_RouteTreeNode.Walk(segments, startPosition + 0, arguments);
                if (l_Res != null)
                    return l_Res;
            }

            return this;
        }

        if ((segments.Length - startPosition) >= 1)
        {
            for (var l_I = 0; l_I < Childs.Length; ++l_I)
            {
                var l_Res = Childs[l_I].Walk(segments, startPosition + 1, arguments);
                if (l_Res != null)
                    return l_Res;
            }
        }

        for (var l_I = 0; l_I < Blueprints.Length; ++l_I)
        {
            var l_Res = Blueprints[l_I].m_RouteTreeNode.Walk(segments, startPosition + 0, arguments);
            if (l_Res != null)
                return l_Res;
        }

        return null;
    }
}