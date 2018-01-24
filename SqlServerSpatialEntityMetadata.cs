using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace Lexor.Data.SqlServerSpatial
{
    public class SqlServerSpatialEntityMetadata
    {
        public IEntityType EntityType { get; }
        public string TableName { get; }
        public string KeyFieldName { get; }
        public string GeometryFieldName { get; }
        public GeometryType GeometryType { get; }
        private readonly List<IProperty> _properties;
        public IReadOnlyCollection<IProperty> Properties => _properties.AsReadOnly();

        public SqlServerSpatialEntityMetadata(
            IEntityType entityType,
            string tableName,
            string keyFieldName,
            string geometryFieldName,
            GeometryType geometryType,
            List<IProperty> properties)
        {
            EntityType = entityType;
            TableName = tableName;
            KeyFieldName = keyFieldName;
            GeometryFieldName = geometryFieldName;
            GeometryType = geometryType;
            _properties = properties;
        }

        public string SelectField(IProperty property) => property.Name.Equals(GeometryFieldName)
            ? $"{property.Name}.STAsText() as {property.Name}" : property.Name;

        public string GetSelectFieldList(string prefix = "")
        {
            var fields = Properties
                .Select(SelectField)
                .Select(p => string.IsNullOrEmpty(prefix) ? p : $"{prefix}.{p}")
                .ToList();
            return string.Join(", ", fields);
        }
    }
}
