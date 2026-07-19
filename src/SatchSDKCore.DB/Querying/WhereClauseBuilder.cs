using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SSC.DB.Attributes;
using SSC.DB.Querying;

namespace SSC.DB.Expressions;

internal class WhereClauseBuilder
{
    /// <summary>
    /// BuildSingle a WHERE query clause from expression
    /// </summary>
    /// <param name="query">Query builder instance</param>
    /// <param name="expression">Expression root</param>
    public static void Build(DbQuery query, Expression expression)
    {
        query.Query.Append(" WHERE ");

        int startPosition = query.Query.Length;
        VisitNode(query, expression);

        if (query.Query[startPosition] == '(' && query.Query[^1] == ')')
        {
            query.Query.Remove(startPosition, 1);
            query.Query.Remove(query.Query.Length - 1, 1);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Visit an expression node
    /// </summary>
    /// <param name="query">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitNode(DbQuery query, Expression? node)
    {
        switch (node)
        {
            case null:
                return;

            case BinaryExpression binaryNode:
                VisitBinaryNode(query, binaryNode);
                break;
            case ConstantExpression constantNode:
                VisitConstantNode(query, constantNode);
                break;
            case MemberExpression memberNode:
                VisitMemberNode(query, memberNode);
                break;
            case MethodCallExpression methodCallNode:
                VisitMethodCallNode(query, methodCallNode);
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
    /// <param name="query">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitBinaryNode(DbQuery query, BinaryExpression node)
    {
        Expression leftNode = node.Left;
        Expression rightNode = node.Right;

        if (node.NodeType == ExpressionType.Add && node.Type == typeof(string))
        {
            query.DbQueryDialect.StringConcatStart(query);
            VisitNode(query, leftNode);
            query.DbQueryDialect.StringConcatSeparator(query);
            VisitNode(query, rightNode);
            query.DbQueryDialect.StringConcatEnd(query);
            return;
        }

        if (IsNullConstant(leftNode))
        {
            rightNode = leftNode;
            leftNode = node.Right;
        }

        bool needParenthesis = leftNode is BinaryExpression || rightNode is BinaryExpression;
        int startPosition = query.Query.Length;

        VisitNode(query, leftNode);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (node.NodeType)
        {
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                query.Query.Append(" AND ");
                break;

            case ExpressionType.Or:
            case ExpressionType.OrElse:
                query.Query.Append(" OR ");
                break;

            case ExpressionType.Equal:
                query.Query.Append(IsNullConstant(rightNode) ? " IS " : " = ");
                break;

            case ExpressionType.NotEqual:
                query.Query.Append(IsNullConstant(rightNode) ? " IS NOT " : " != ");
                break;

            case ExpressionType.LessThan:
                query.Query.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                query.Query.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                query.Query.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
                query.Query.Append(" >= ");
                break;

            case ExpressionType.Add:
                needParenthesis = true;
                query.Query.Append(" + ");
                break;

            case ExpressionType.Subtract:
                needParenthesis = true;
                query.Query.Append(" - ");
                break;

            case ExpressionType.Multiply:
                needParenthesis = true;
                query.Query.Append(" * ");
                break;

            case ExpressionType.Divide:
                needParenthesis = true;
                query.Query.Append(" / ");
                break;

            default:
                throw new ArgumentException($"Unsupported expression type {node.NodeType}");
        }

        VisitNode(query, rightNode);

        if (!needParenthesis)
        {
            return;
        }

        query.Query.Insert(startPosition, '(');
        query.Query.Append(')');
    }

    /// <summary>
    /// Visits a constant node.
    /// </summary>
    /// <param name="query">Query builder instance</param>
    /// <param name="node">Current node</param>
    private static void VisitConstantNode(DbQuery query, ConstantExpression node)
    {
        if (node.Value == null)
        {
            query.Query.Append("NULL");
        }
        else if (node.Type.IsPrimitive && node.Type != typeof(char))
        {
            string parameterName = query.GenerateParameterName("Where");
            query.AddParameter(parameterName, node.Value);
            query.Query.Append(parameterName);
        }
        else if (node.Type == typeof(string))
        {
            string parameterName = query.GenerateParameterName("Where");
            query.AddParameter(parameterName, node.Value);
            query.Query.Append(parameterName);
        }
        else
        {
            throw new ArgumentException($"Unsupported constant '{node.Value}' of type {node.Type.Name}");
        }
    }

    /// <summary>
    /// Visit a member node
    /// </summary>
    /// <param name="query">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitMemberNode(DbQuery query, MemberExpression node)
    {
        Expression toAnalyse = node.Expression ?? node;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (toAnalyse.NodeType)
        {
            case ExpressionType.Parameter:
                if (node.Member.DeclaringType != query.DbModelMetadata!.ModelType)
                {
                    throw new Exception(
                        $"Field {node.Member} is not part of model {query.DbModelMetadata!.Identifier}!");
                }

                DbFieldAttribute? fieldMetadata =
                    query.DbModelMetadata!.FieldAttributes.FirstOrDefault(x => x.FieldInfo.Name == node.Member.Name);
                if (fieldMetadata == null)
                {
                    throw new Exception(
                        $"Can not find field {node.Member} in model {query.DbModelMetadata!.Identifier}!");
                }

                query.DbQueryDialect.Field(query, fieldMetadata);
                break;

            case ExpressionType.Constant:
            case ExpressionType.MemberAccess:
                object? value = ResolveExpressionValue(node);
                string parameterName = query.GenerateParameterName("Where");
                query.AddParameter(parameterName, value);
                query.Query.Append(parameterName);
                break;

            default:
                throw new NotSupportedException($"The member '{node.Member.Name}' is not supported");
        }
    }

    /// <summary>
    /// Visit a method call node
    /// </summary>
    /// <param name="query">Query builder instance</param>
    /// <param name="node">Current node</param>
    /// <exception cref="ArgumentException">If the expression type is not supported</exception>
    private static void VisitMethodCallNode(DbQuery query, MethodCallExpression node)
    {
        if (node.Object is not MemberExpression memberExpression)
        {
            throw new Exception("Unsupported!");
        }

        if (memberExpression.Member.DeclaringType != query.DbModelMetadata!.ModelType)
        {
            throw new Exception(
                $"Field {memberExpression.Member} is not part of model {query.DbModelMetadata!.Identifier}!");
        }

        DbFieldAttribute? fieldMetadata =
            query.DbModelMetadata!.FieldAttributes.FirstOrDefault(x =>
                x.FieldInfo.Name == memberExpression.Member.Name);
        if (fieldMetadata == null)
        {
            throw new Exception(
                $"Can not find field {memberExpression.Member} in model {query.DbModelMetadata!.Identifier}!");
        }

        if (node.Method.DeclaringType == typeof(string))
        {
            if (fieldMetadata.FieldType != typeof(string))
            {
                throw new Exception($"Can not apply {node.Method.Name} transform on field {memberExpression.Member}!");
            }

            switch (node.Method.Name)
            {
                case "ToLower":
                    query.DbQueryDialect.FieldValueLower(query, fieldMetadata);
                    break;
            }
        }
        else
        {
            throw new Exception($"Unsupported method call {node.Method.DeclaringType!.Name}{node.Method.Name}!");
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Resolve an expression value
    /// </summary>
    /// <param name="node">Current node</param>
    /// <returns>Resolved value or null</returns>
    /// <exception cref="NotSupportedException">If the current expression is not supported</exception>
    [UnconditionalSuppressMessage("AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "<Pending>")]
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
