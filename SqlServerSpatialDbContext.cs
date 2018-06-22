using Lexor.Data.SqlServerSpatial.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lexor.Data.SqlServerSpatial
{
    public abstract class SqlServerSpatialDbContext : DbContext
    {
        public static Dictionary<Type, SqlServerSpatialEntityMetadata> SpatialEntities { get; private set; }
        private SqlServerSpatialSettings Settings { get; }

        protected SqlServerSpatialDbContext(DbContextOptions options, IOptions<SqlServerSpatialSettings> settings)
            : base(options)
        {
            Settings = settings.Value;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SpatialEntities = new Dictionary<Type, SqlServerSpatialEntityMetadata>();
            foreach (var (entity, property, geometryType) in SqlServerSpatialColumn.GetSpatialColumns(modelBuilder.Model))
            {
                var tableName = modelBuilder.Entity(entity.ClrType).Metadata.SqlServer().TableName;
                var keyName = entity.FindPrimaryKey().Properties[0].Name;
                var metadata = new SqlServerSpatialEntityMetadata(entity, tableName, keyName, property.Name, geometryType, entity.GetProperties().ToList());
                SpatialEntities.Add(entity.ClrType, metadata);

                var propertyBuilder = modelBuilder
                    .Entity(entity.Name)
                    .Property(property.Name);

                if (propertyBuilder.Metadata.ClrType != typeof(string))
                    throw new InvalidCastException($"Field '{property.Name}' decorated with attribute '{nameof(SqlServerSpatialColumnAttribute)}' is defined as '{propertyBuilder.Metadata.ClrType.Name}' but must be defined as 'string' [{entity.Name}]");

                propertyBuilder
                    .HasColumnType("geometry") // Force column to be the SQL Server spatial data type
                    .IsRequired(false);
                propertyBuilder.Metadata.BeforeSaveBehavior = PropertySaveBehavior.Ignore;

                PerformAdditionalSpatialColumnProcessing(modelBuilder, entity, property, metadata);
            }
        }

        protected virtual void PerformAdditionalSpatialColumnProcessing(ModelBuilder modelBuilder, IEntityType entityType, PropertyInfo property, SqlServerSpatialEntityMetadata metadata)
        { }

        public void EnsureSpatialIndexesExist()
        {
            foreach (var spatialEntity in SpatialEntities)
            {
                EnsureSpatialIndexExists(spatialEntity.Value);
            }
        }

        private void EnsureSpatialIndexExists(SqlServerSpatialEntityMetadata metadata)
        {
            // Ensure spatial index exists, create it if not
            var spatialIndexName = $"{metadata.TableName}{metadata.GeometryFieldName}SpatialIndex";
            var sql = $@"
if not exists (select 1 from sys.indexes where name = '{spatialIndexName}')
begin
create spatial index [{spatialIndexName}]
on [{metadata.TableName}]([{metadata.GeometryFieldName}])
using geometry_auto_grid
with (bounding_box = {SpatialIndexBoundingBox})
end
";
#pragma warning disable EF1000 // Possible SQL injection vulnerability.
            Database.ExecuteSqlCommand(sql);
#pragma warning restore EF1000 // Possible SQL injection vulnerability.
        }

        protected string SpatialIndexBoundingBox =>
            $"(xmin={Settings.SpatialIndex.XMin}, xmax={Settings.SpatialIndex.XMax}, ymin={Settings.SpatialIndex.YMin}, ymax={Settings.SpatialIndex.YMax})";

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
#pragma warning disable EF1000 // Possible SQL injection vulnerability.
            await Database.ExecuteSqlCommandAsync(sql);
#pragma warning restore EF1000 // Possible SQL injection vulnerability.
        }
    }
}
