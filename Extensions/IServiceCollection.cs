using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Lexor.Data.SqlServerSpatial.Extensions
{
    public static class IServiceCollectionExtensions
    {
        // Force use of custom services for SqlServerSpatial mini-provider
        public static IServiceCollection AddEntityFrameworkSqlServerSpatial(this IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IQuerySqlGeneratorFactory, SqlServerSpatialQuerySqlGeneratorFactory>()
                .AddEntityFrameworkSqlServer(); // Include default SQL Server services
            return serviceCollection;
        }
    }
}
