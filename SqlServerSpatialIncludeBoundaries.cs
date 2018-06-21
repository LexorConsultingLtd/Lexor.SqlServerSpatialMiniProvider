using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lexor.Data.SqlServerSpatial
{
    // Abandoned -- for now -- since the Dapper queries seem to work adequately
    public static class SqlServerSpatialQueryableExtensions
    {
        internal static readonly MethodInfo IncludeContainingBoundaryMethodInfo
            = typeof(SqlServerSpatialQueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods(nameof(IncludeContainingBoundary))
                .Single(mi => mi.GetParameters().Any(
                    pi => pi.Name == "navigationPropertyPath" && pi.ParameterType != typeof(string)));

        public static IIncludableQueryable<TEntity, TProperty> IncludeContainingBoundary<TEntity, TProperty>(
            this IQueryable<TEntity> source,
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (navigationPropertyPath == null) throw new ArgumentNullException(nameof(navigationPropertyPath));

            return new IncludableQueryable<TEntity, TProperty>(
                source.Provider is EntityQueryProvider
                    ? source.Provider.CreateQuery<TEntity>(
                        Expression.Call(
                            instance: null,
                            method: IncludeContainingBoundaryMethodInfo.MakeGenericMethod(typeof(TEntity),
                                typeof(TProperty)),
                            arguments: new[] { source.Expression, Expression.Quote(navigationPropertyPath) }))
                    : source);
        }


        internal static readonly MethodInfo WithLockMethodInfo
            = typeof(SqlServerSpatialQueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods(nameof(WithLock))
                .Single(mi => mi.GetParameters().Any(
                    pi => pi.Name == "tableName" && pi.ParameterType == typeof(string)));

        public static IQueryable<TEntity> WithLock<TEntity>(this IQueryable<TEntity> source,
            string tableName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Provider.CreateQuery<TEntity>(
                Expression.Call(
                    null,
                    WithLockMethodInfo.MakeGenericMethod(typeof(TEntity)),
                    source.Expression,
                    Expression.Constant(tableName, typeof(string))));
        }

        private class IncludableQueryable<TEntity, TProperty> : IIncludableQueryable<TEntity, TProperty>,
            IAsyncEnumerable<TEntity>
        {
            private readonly IQueryable<TEntity> _queryable;

            public IncludableQueryable(IQueryable<TEntity> queryable)
            {
                _queryable = queryable;
            }

            public Expression Expression => _queryable.Expression;
            public Type ElementType => _queryable.ElementType;
            public IQueryProvider Provider => _queryable.Provider;

            public IEnumerator<TEntity> GetEnumerator() => _queryable.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetEnumerator()
                => ((IAsyncEnumerable<TEntity>)_queryable).GetEnumerator();
        }
    }

    public class IncludeContainingBoundaryExpressionNode : ResultOperatorExpressionNodeBase
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            SqlServerSpatialQueryableExtensions.IncludeContainingBoundaryMethodInfo
        };

        private readonly LambdaExpression _navigationPropertyPathLambda;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IncludeContainingBoundaryExpressionNode(
            MethodCallExpressionParseInfo parseInfo,
            LambdaExpression navigationPropertyPathLambda)
            : base(parseInfo, null, null)
        {
            _navigationPropertyPathLambda = navigationPropertyPathLambda;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            var prm = Expression.Parameter(typeof(object));
            var pathFromQuerySource = Resolve(prm, prm, clauseGenerationContext);

            if (!(_navigationPropertyPathLambda.Body is MemberExpression navigationPropertyPath))
            {
                throw new InvalidOperationException(CoreStrings.InvalidPropertyExpression(_navigationPropertyPathLambda));
            }

            var includeResultOperator = new IncludeResultOperator(
                _navigationPropertyPathLambda.GetComplexPropertyAccess("?not sure what goes here?").Select(p => p.Name),
                pathFromQuerySource);

            clauseGenerationContext.AddContextInfo(this, includeResultOperator);

            return includeResultOperator;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override Expression Resolve(
            ParameterExpression inputParameter,
            Expression expressionToBeResolved,
            ClauseGenerationContext clauseGenerationContext)
            => Source.Resolve(
                inputParameter,
                expressionToBeResolved,
                clauseGenerationContext);
    }


    public class SqlServerSpatialMethodInfoBasedNodeTypeRegistryFactory : MethodInfoBasedNodeTypeRegistryFactory
    {
        /// <inheritdoc />
        public SqlServerSpatialMethodInfoBasedNodeTypeRegistryFactory(MethodInfoBasedNodeTypeRegistry methodInfoBasedNodeTypeRegistry)
            : base(methodInfoBasedNodeTypeRegistry) { }

        /// <inheritdoc />
        public override INodeTypeProvider Create()
        {
            RegisterMethods(IncludeContainingBoundaryExpressionNode.SupportedMethods, typeof(IncludeContainingBoundaryExpressionNode));

            var innerProviders
                = new INodeTypeProvider[]
                {
                    base.Create(),
                    MethodNameBasedNodeTypeRegistry.CreateFromRelinqAssembly()
                };

            return new CompoundNodeTypeProvider(innerProviders);
        }
    }
}