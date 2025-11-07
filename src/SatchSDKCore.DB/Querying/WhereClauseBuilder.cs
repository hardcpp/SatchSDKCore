using SSC.DB.Querying;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SSC.DB.Expressions;

internal class WhereClauseBuilder
{
    /// <summary>
    /// Build a WHERE query clause from expression
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="expression">Expression root</param>
    public static void Build(DBQueryBuilder queryBuilder, Expression expression)
    {
        queryBuilder.Query.Append(" WHERE ");

        var l_StartPosition = queryBuilder.Query.Length;
        VisitNode(queryBuilder, expression);

        if (queryBuilder.Query[l_StartPosition] == '(' && queryBuilder.Query[^1] == ')')
        {
            queryBuilder.Query.Remove(l_StartPosition, 1);
            queryBuilder.Query.Remove(queryBuilder.Query.Length - 1, 1);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Visit an expression node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitNode(DBQueryBuilder queryBuilder, Expression node)
    {
        if (node == null)
            return;

        if (node is BinaryExpression l_BinaryNode)
            VisitBinaryNode(queryBuilder, l_BinaryNode);
        else if (node is ConstantExpression l_ConstantNode)
            VisitConstantNode(queryBuilder, l_ConstantNode);
        else if (node is MemberExpression l_MemberNode)
            VisitMemberNode(queryBuilder, l_MemberNode);
        else if (node is MethodCallExpression l_MethodCallNode)
            VisitMethodCallNode(queryBuilder, l_MethodCallNode);
        else
            throw new ArgumentException($"Unsuported expression type {node.GetType().Name}");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Visit a binary node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitBinaryNode(DBQueryBuilder queryBuilder, BinaryExpression node)
    {
        var l_LeftNode  = node.Left;
        var l_RightNode = node.Right;

        if (IsNullConstant(l_LeftNode))
        {
            l_RightNode = l_LeftNode;
            l_LeftNode  = node.Right;
        }

        var l_NeedParenthesis   = l_LeftNode is BinaryExpression || l_RightNode is BinaryExpression;
        var l_StartPosition     = queryBuilder.Query.Length;

        VisitNode(queryBuilder, l_LeftNode);

        switch (node.NodeType)
        {
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                queryBuilder.Query.Append(" AND ");
                break;

            case ExpressionType.Or:
            case ExpressionType.OrElse:
                queryBuilder.Query.Append(" OR ");
                break;

            case ExpressionType.Equal:
                if (IsNullConstant(l_RightNode))
                    queryBuilder.Query.Append(" IS ");
                else
                    queryBuilder.Query.Append(" = ");
                break;

            case ExpressionType.NotEqual:
                if (IsNullConstant(l_RightNode))
                    queryBuilder.Query.Append(" IS NOT ");
                else
                    queryBuilder.Query.Append(" != ");
                break;

            case ExpressionType.LessThan:
                queryBuilder.Query.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                queryBuilder.Query.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                queryBuilder.Query.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
                queryBuilder.Query.Append(" >= ");
                break;

            case ExpressionType.Add:
                l_NeedParenthesis = true;
                queryBuilder.Query.Append(" + ");
                break;

            case ExpressionType.Subtract:
                l_NeedParenthesis = true;
                queryBuilder.Query.Append(" - ");
                break;

            case ExpressionType.Multiply:
                l_NeedParenthesis = true;
                queryBuilder.Query.Append(" * ");
                break;

            case ExpressionType.Divide:
                l_NeedParenthesis = true;
                queryBuilder.Query.Append(" / ");
                break;

            default:
                throw new ArgumentException($"Unsuported expression type {node.NodeType}");
        }

        VisitNode(queryBuilder, l_RightNode);

        if (l_NeedParenthesis)
        {
            queryBuilder.Query.Insert(l_StartPosition, '(');
            queryBuilder.Query.Append(')');
        }
    }
    /// <summary>
    /// Visist a constant node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    private static void VisitConstantNode(DBQueryBuilder queryBuilder, ConstantExpression node)
    {
        if (node.Value == null)
            queryBuilder.Query.Append("NULL");
        else if (node.Type.IsPrimitive && node.Type != typeof(char))
        {
            var l_ParameterName = queryBuilder.GenerateParameterName("Where");
            queryBuilder.AddParameter(l_ParameterName, node.Value);
            queryBuilder.Query.Append(l_ParameterName);
        }
        else if (node.Type == typeof(string))
        {
            var l_ParameterName = queryBuilder.GenerateParameterName("Where");
            queryBuilder.AddParameter(l_ParameterName, node.Value);
            queryBuilder.Query.Append(l_ParameterName);
        }
        else
            throw new ArgumentException($"Unsuported constant '{node.Value}' of type {node.Type.Name}");
    }
    /// <summary>
    /// Visit a member node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitMemberNode(DBQueryBuilder queryBuilder, MemberExpression node)
    {
        var l_ToAnalyse = node.Expression ?? node;

        switch (l_ToAnalyse.NodeType)
        {
            case ExpressionType.Parameter:
                if (node.Member.DeclaringType != queryBuilder.DBModelMetadata!.ModelType)
                    throw new Exception($"Field {node.Member} is not part of model {queryBuilder.DBModelMetadata!.Identifier}!");

                var l_FieldMetadata = queryBuilder.DBModelMetadata!.FieldAttributes.FirstOrDefault(x => x.Name == node.Member.Name);
                if (l_FieldMetadata == null)
                    throw new Exception($"Can not find field {node.Member} in model {queryBuilder.DBModelMetadata!.Identifier}!");

                queryBuilder.DBQueryDialect.Field(queryBuilder, l_FieldMetadata);
                return;

            case ExpressionType.Constant:
            case ExpressionType.MemberAccess:
                var l_Value         = ResolveExpressionValue(node);
                var l_ParameterName = queryBuilder.GenerateParameterName("Where");
                queryBuilder.AddParameter(l_ParameterName, l_Value);
                queryBuilder.Query.Append(l_ParameterName);
                return;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", node.Member.Name));

    }
    /// <summary>
    /// Visit a method call node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitMethodCallNode(DBQueryBuilder queryBuilder, MethodCallExpression node)
    {
        if (node.Object is not MemberExpression l_MemberExpression)
            throw new Exception("Unsupported!");

        if (l_MemberExpression.Member.DeclaringType != queryBuilder.DBModelMetadata!.ModelType)
            throw new Exception($"Field {l_MemberExpression.Member} is not part of model {queryBuilder.DBModelMetadata!.Identifier}!");

        var l_FieldMetadata = queryBuilder.DBModelMetadata!.FieldAttributes.FirstOrDefault(x => x.Name == l_MemberExpression.Member.Name);
        if (l_FieldMetadata == null)
            throw new Exception($"Can not find field {l_MemberExpression.Member} in model {queryBuilder.DBModelMetadata!.Identifier}!");

        if (node.Method.DeclaringType == typeof(string))
        {
            if (l_FieldMetadata.FieldType != typeof(string))
                throw new Exception($"Can not apply {node.Method.Name} transform on field {l_MemberExpression.Member}!");

            switch (node.Method.Name)
            {
                case "ToLower":
                    queryBuilder.DBQueryDialect.FieldValueLower(queryBuilder, l_FieldMetadata);
                    break;
            }
        }
        else
            throw new Exception($"Unsupported method call {node.Method.DeclaringType!.Name}{node.Method.Name}!");
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Resolve an expression value
    /// </summary>
    /// <param name="node">Current node</param>
    /// <returns>Resolved value or null</returns>
    /// <exception cref="NotSupportedException">If the current expression is not supported</exception>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static object? ResolveExpressionValue(Expression node)
    {
        switch (node)
        {
            case ConstantExpression l_ConstantExpression:
                return l_ConstantExpression.Value;

            case MemberExpression l_MemberExpression:
                if (l_MemberExpression.Expression is ConstantExpression l_SubConstantExpression)
                {
                    if (l_MemberExpression.Member is FieldInfo l_FieldInfo)
                        return l_FieldInfo.GetValue(l_SubConstantExpression.Value);
                    else if (l_MemberExpression.Member is PropertyInfo l_PropertyInfo)
                        return l_PropertyInfo.GetValue(l_SubConstantExpression.Value);
                }
                break;

            case MethodCallExpression l_MethodCallExpression:
                return Expression.Lambda(l_MethodCallExpression).Compile().DynamicInvoke();

            case null:
                return null;
        }

        throw new NotSupportedException();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Is an expression a null constant
    /// </summary>
    /// <param name="expression">Expression</param>
    /// <returns>True if the expression is a null constant</returns>
    private static bool IsNullConstant(Expression expression)
        => expression.NodeType == ExpressionType.Constant && ((ConstantExpression)expression).Value == null;
}
