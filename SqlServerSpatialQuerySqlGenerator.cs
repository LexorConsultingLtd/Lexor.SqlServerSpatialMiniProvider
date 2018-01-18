using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Linq.Expressions;

namespace Lexor.Data.SqlServerSpatial
{
    public class SqlServerSpatialQuerySqlGenerator : SqlServerQuerySqlGenerator
    {
        public SqlServerSpatialQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies, SelectExpression selectExpression, bool rowNumberPagingEnabled)
            : base(dependencies, selectExpression, rowNumberPagingEnabled) { }

        public override Expression VisitColumn(ColumnExpression columnExpression)
        {
            base.VisitColumn(columnExpression);

            // Check if this is a spatial column, and alter the query to return the WKT if so
            if (SqlServerSpatialColumn.IsSpatialColumn(columnExpression.Property))
            {
                Sql.Append(".STAsText()");
            }
            return columnExpression;
        }
    }
}
