using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Sql.Internal;

namespace Lexor.Data.SqlServerSpatial
{
    public class SqlServerSpatialQuerySqlGeneratorFactory : SqlServerQuerySqlGeneratorFactory
    {
        private ISqlServerOptions SqlServerOptions { get; }

        public SqlServerSpatialQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies, ISqlServerOptions sqlServerOptions)
            : base(dependencies, sqlServerOptions)
        {
            SqlServerOptions = sqlServerOptions;
        }

        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression)
            => new SqlServerSpatialQuerySqlGenerator(Dependencies, selectExpression, SqlServerOptions.RowNumberPagingEnabled);
    }
}
