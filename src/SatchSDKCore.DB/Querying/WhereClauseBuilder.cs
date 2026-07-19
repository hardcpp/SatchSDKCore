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
    public static void Build(DbQueryBuilder queryBuilder, Expression expression)
    {
        queryBuilder.Query.Append(" WHERE ");

        var startPosition = queryBuilder.Query.Length;
        VisitNode(queryBuilder, expression);

        if (queryBuilder.Query[startPosition] == '(' && queryBuilder.Query[^1] == ')')
        {
            queryBuilder.Query.Remove(startPosition, 1);
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
    private static void VisitNode(DbQueryBuilder queryBuilder, Expression? node)
    {
        switch (node)
        {
            case null:
                return;

            case BinaryExpression binaryNode:
                VisitBinaryNode(queryBuilder, binaryNode);
                break;
            case ConstantExpression constantNode:
                VisitConstantNode(queryBuilder, constantNode);
                break;
            case MemberExpression memberNode:
                VisitMemberNode(queryBuilder, memberNode);
                break;
            case MethodCallExpression methodCallNode:
                VisitMethodCallNode(queryBuilder, methodCallNode);
                break;

            default:
                throw new ArgumentException($"Unsupported expression type {node.GetType().Name}");
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Visit a binary node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitBinaryNode(DbQueryBuilder queryBuilder, BinaryExpression node)
    {
        var leftNode = node.Left;
        var rightNode = node.Right;

        if (IsNullConstant(leftNode))
        {
            rightNode = leftNode;
            leftNode = node.Right;
        }

        var needParenthesis = leftNode is BinaryExpression || rightNode is BinaryExpression;
        var startPosition = queryBuilder.Query.Length;

        VisitNode(queryBuilder, leftNode);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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
                queryBuilder.Query.Append(IsNullConstant(rightNode) ? " IS " : " = ");
                break;

            case ExpressionType.NotEqual:
                queryBuilder.Query.Append(IsNullConstant(rightNode) ? " IS NOT " : " != ");
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
                needParenthesis = true;
                queryBuilder.Query.Append(" + ");
                break;

            case ExpressionType.Subtract:
                needParenthesis = true;
                queryBuilder.Query.Append(" - ");
                break;

            case ExpressionType.Multiply:
                needParenthesis = true;
                queryBuilder.Query.Append(" * ");
                break;

            case ExpressionType.Divide:
                needParenthesis = true;
                queryBuilder.Query.Append(" / ");
                break;

            default:
                throw new ArgumentException($"Unsupported expression type {node.NodeType}");
        }

        VisitNode(queryBuilder, rightNode);

        if (!needParenthesis)
            return;

        queryBuilder.Query.Insert(startPosition, '(');
        queryBuilder.Query.Append(')');
    }
    /// <summary>
    /// Visist a constant node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    private static void VisitConstantNode(DbQueryBuilder queryBuilder, ConstantExpression node)
    {
        if (node.Value == null)
            queryBuilder.Query.Append("NULL");
        else if (node.Type.IsPrimitive && node.Type != typeof(char))
        {
            var parameterName = queryBuilder.GenerateParameterName("Where");
            queryBuilder.AddParameter(parameterName, node.Value);
            queryBuilder.Query.Append(parameterName);
        }
        else if (node.Type == typeof(string))
        {
            var parameterName = queryBuilder.GenerateParameterName("Where");
            queryBuilder.AddParameter(parameterName, node.Value);
            queryBuilder.Query.Append(parameterName);
        }
        else
            throw new ArgumentException($"Unsupported constant '{node.Value}' of type {node.Type.Name}");
    }
    /// <summary>
    /// Visit a member node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitMemberNode(DbQueryBuilder queryBuilder, MemberExpression node)
    {
        var toAnalyse = node.Expression ?? node;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (toAnalyse.NodeType)
        {
            case ExpressionType.Parameter:
                if (node.Member.DeclaringType != queryBuilder.DbModelMetadata!.ModelType)
                    throw new Exception($"Field {node.Member} is not part of model {queryBuilder.DbModelMetadata!.Identifier}!");

                var fieldMetadata = queryBuilder.DbModelMetadata!.FieldAttributes.FirstOrDefault(x => x.Name == node.Member.Name);
                if (fieldMetadata == null)
                    throw new Exception($"Can not find field {node.Member} in model {queryBuilder.DbModelMetadata!.Identifier}!");

                queryBuilder.DbQueryDialect.Field(queryBuilder, fieldMetadata);
                break;

            case ExpressionType.Constant:
            case ExpressionType.MemberAccess:
                var value = ResolveExpressionValue(node);
                var parameterName = queryBuilder.GenerateParameterName("Where");
                queryBuilder.AddParameter(parameterName, value);
                queryBuilder.Query.Append(parameterName);
                break;

            default:
                throw new NotSupportedException($"The member '{node.Member.Name}' is not supported");
        }
    }
    /// <summary>
    /// Visit a method call node
    /// </summary>
    /// <param name="queryBuilder">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitMethodCallNode(DbQueryBuilder queryBuilder, MethodCallExpression node)
    {
        if (node.Object is not MemberExpression memberExpression)
            throw new Exception("Unsupported!");

        if (memberExpression.Member.DeclaringType != queryBuilder.DbModelMetadata!.ModelType)
            throw new Exception($"Field {memberExpression.Member} is not part of model {queryBuilder.DbModelMetadata!.Identifier}!");

        var fieldMetadata = queryBuilder.DbModelMetadata!.FieldAttributes.FirstOrDefault(x => x.Name == memberExpression.Member.Name);
        if (fieldMetadata == null)
            throw new Exception($"Can not find field {memberExpression.Member} in model {queryBuilder.DbModelMetadata!.Identifier}!");

        if (node.Method.DeclaringType == typeof(string))
        {
            if (fieldMetadata.FieldType != typeof(string))
                throw new Exception($"Can not apply {node.Method.Name} transform on field {memberExpression.Member}!");

            switch (node.Method.Name)
            {
                case "ToLower":
                    queryBuilder.DbQueryDialect.FieldValueLower(queryBuilder, fieldMetadata);
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
            case ConstantExpression constantExpression:
                return constantExpression.Value;

            case MemberExpression memberExpression:
                if (memberExpression.Expression is ConstantExpression subConstantExpression)
                {
                    switch (memberExpression.Member)
                    {
                        case FieldInfo fieldInfo:
                            return fieldInfo.GetValue(subConstantExpression.Value);
                        case PropertyInfo propertyInfo:
                            return propertyInfo.GetValue(subConstantExpression.Value);
                    }
                }
                break;

            case MethodCallExpression methodCallExpression:
                return Expression.Lambda(methodCallExpression).Compile().DynamicInvoke();

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
