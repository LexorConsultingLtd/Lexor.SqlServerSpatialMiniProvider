namespace Lexor.Data.SqlServerSpatial.Settings
{
    public class SqlServerSpatialSettings
    {
        public SpatialIndex SpatialIndex { get; set; }
    }

    public class SpatialIndex
    {
        public string XMin { get; set; }
        public string XMax { get; set; }
        public string YMin { get; set; }
        public string YMax { get; set; }
    }
}
