using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Lexor.Data.SqlServerSpatial
{
    public abstract class SqlServerSpatialDbContext : DbContext
    {
        protected static Dictionary<Type, (string, string, string, GeometryType)> SpatialColumns { get; private set; }

        public SqlServerSpatialDbContext(DbContextOptions options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SpatialColumns = new Dictionary<Type, (string, string, string, GeometryType)>();
            foreach (var (entity, property, geometryType) in SqlServerSpatialColumn.GetSpatialColumns(modelBuilder.Model))
            {
                var tableName = modelBuilder.Entity(entity.ClrType).Metadata.SqlServer().TableName;
                var keyName = entity.FindPrimaryKey().Properties[0].Name;
                SpatialColumns.Add(entity.ClrType, (tableName, keyName, property.Name, geometryType));

                var propertyBuilder = modelBuilder
                    .Entity(entity.Name)
                    .Property(property.Name);

                if (propertyBuilder.Metadata.ClrType != typeof(string))
                    throw new InvalidCastException($"Field '{property.Name}' decorated with attribute '{nameof(SqlServerSpatialColumnAttribute)}' is defined as '{propertyBuilder.Metadata.ClrType.Name}' but must be defined as 'string' [{entity.Name}]");

                propertyBuilder
                    .HasColumnType("geometry") // Force column to be the SQL Server spatial data type
                    .IsRequired(false);
                propertyBuilder.Metadata.BeforeSaveBehavior = PropertySaveBehavior.Ignore;

                PerformAdditionalSpatialColumnProcessing(modelBuilder, entity, property);
            }
        }

        protected virtual void PerformAdditionalSpatialColumnProcessing(ModelBuilder modelBuilder, IEntityType entityType, PropertyInfo property) { }

        public async Task UpdatePointGeometry(int id, Type entityType, string geometryColumnName, decimal x, decimal y)
        {
            var value = $"POINT({x} {y})";
            await UpdateGeometry(id, entityType, geometryColumnName, value);
        }

        private async Task UpdateGeometry(int id, Type entityType, string geometryColumnName, string value)
        {
            var model = Model.FindEntityType(entityType);
            var tableName = model.SqlServer().TableName;
            var keyColumnName = model.FindPrimaryKey().Properties[0].Name;
            var sql = $"update {tableName} set {geometryColumnName} = geometry::STGeomFromText('{value}', 0) where {keyColumnName} = {id}";
            await Database.ExecuteSqlCommandAsync(sql);
        }

        public async Task<TBoundary> GetContainingBoundaryAsync<TBounded, TBoundary>(object key) where TBounded : class where TBoundary : class
        {
            var (boundedTable, boundedKeyField, boundedGeometryColumn, _) = SpatialColumns[typeof(TBounded)];
            var (boundaryTable, _, boundaryGeometryColumn, _) = SpatialColumns[typeof(TBoundary)];
            var sql = $@"select a.* from {boundaryTable} a join {boundedTable} b on a.[{boundaryGeometryColumn}].STContains(b.[{boundedGeometryColumn}]) = 1 where b.[{boundedKeyField}] = @p0";
            var result = await Set<TBoundary>()
                .FromSql(sql, key)
                .FirstOrDefaultAsync();
            return result;
        }
    }
}
