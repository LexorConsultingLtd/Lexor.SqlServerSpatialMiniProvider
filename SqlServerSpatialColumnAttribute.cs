using System;

namespace Lexor.Data.SqlServerSpatial
{
    public enum GeometryType
    {
        None,
        Point,
        Line,
        Area,
        Compound
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SqlServerSpatialColumnAttribute : Attribute
    {
        public SqlServerSpatialColumnAttribute(GeometryType geometryType)
        {
            GeometryType = geometryType;
        }

        public GeometryType GeometryType { get; set; }
    }
}
