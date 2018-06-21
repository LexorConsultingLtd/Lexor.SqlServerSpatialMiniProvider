using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lexor.Data.SqlServerSpatial
{
    public class SqlServerSpatialColumn
    {
        public static IEnumerable<(IEntityType, PropertyInfo, GeometryType)> GetSpatialColumns(IModel model)
        {
            var result = new List<(IEntityType, PropertyInfo, GeometryType)>();

            foreach (var entity in model.GetEntityTypes())
            {
                var spatialColumns = entity
                    .GetProperties()
                    .Where(IsSpatialColumn)
                    .Select(p => p.PropertyInfo)
                    .ToList();
                foreach (var column in spatialColumns)
                {
                    result.Add((entity, column, column.GetCustomAttribute<SqlServerSpatialColumnAttribute>().GeometryType));
                }
            }

            return result;
        }

        public static bool IsSpatialColumn(IProperty property) =>
            property.PropertyInfo?.GetCustomAttribute<SqlServerSpatialColumnAttribute>() != null;
    }
}
