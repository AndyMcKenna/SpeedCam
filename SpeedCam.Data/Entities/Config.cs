using SpeedCam.Data.Db;
using System.IO;

namespace SpeedCam.Data.Entities
{
    public class Config : BaseEntity
    {
        public int StartTimeSunrise { get; set; }
        public int EndTimeSunset { get; set; }
        public decimal LeftDistance { get; set; }
        public decimal RightDistance { get; set; }
        public int ChunkTime { get; set; }

        public int LeftStart { get; set; }
        public int RightStart { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string VideoAddress { get; set; }
        public string VideoUser { get; set; }
        public string VideoPassword { get; set; }
        public int VideoChannel { get; set; }

        public string ExportFolder { get; set; }
        public string ConvertedFolder { get; set; }
        public string ConvertedErrorFolder { get; set; }
        public string AnalyzedFolder { get; set; }
        public string PhotoFolder { get; set; }

        public string DbConnectionString { get; protected set; }

        public static Config Load()
        {
            var connectionString = File.ReadAllText("settings.cfg");
            var database = new SqlDatabase(connectionString);
            var config = database.GetConfig();
            config.DbConnectionString = connectionString;

            return config;
        }
    }
}
