using System;
using System.Linq.Expressions;
using System.Reflection;

namespace TinyJsonDatabase.Json
{
    public static class ReflectionHelper
    {
        public static PropertyInfo PropertyFromLambda(Expression exp)
        {
            LambdaExpression lambda = exp as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException(nameof(exp));

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr =
                    ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
                throw new ArgumentException(nameof(exp));

            return (PropertyInfo)memberExpr.Member;
        }
    }
}