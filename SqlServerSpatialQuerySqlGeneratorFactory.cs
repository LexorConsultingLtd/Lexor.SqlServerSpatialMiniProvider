using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;

namespace Lexor.Data.SqlServerSpatial
{
    public class SqlServerSpatialQuerySqlGeneratorFactory : SqlServerQuerySqlGeneratorFactory
    {
        private readonly ISqlServerOptions sqlServerOptions;

        public SqlServerSpatialQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies, ISqlServerOptions sqlServerOptions)
            : base(dependencies, sqlServerOptions)
        {
            this.sqlServerOptions = sqlServerOptions;
        }

        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression)
            => new SqlServerSpatialQuerySqlGenerator(Dependencies, selectExpression, sqlServerOptions.RowNumberPagingEnabled);
    }
}
