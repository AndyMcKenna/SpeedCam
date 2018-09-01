using SpeedCam.Data.Db;
using SpeedCam.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpeedCam.ExportPhotos
{
    static class Program
    {
        private static Config Config;
        private static IDatabase Database;
        const string USAGE = "dotnet SpeedCam.ExportPhotos.dll StartDate EndDate MinSpeed MaxSpeed OrderBy[Speed=1, Date=2]";
        const int ORDERBY_SPEED = 1;
        const int ORDERBY_DATE = 2;

        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine(USAGE);
            }

            var arguments = ParseArguments(args);

            Config = Config.Load();
            Database = new Database(Config.DbConnectionString);

            var cars = Database.EntrySearch(arguments.StartDate, arguments.EndDate, arguments.MinSpeed, arguments.MaxSpeed);

            var queryFolder = new DirectoryInfo(Path.Combine(Config.PhotoFolder, "Query"));
            var existingFiles = queryFolder.GetFiles();
            foreach(var file in existingFiles)
            {
                file.Delete();
            }

            foreach (var car in cars)
            {
                var photoName = Path.Combine(Config.PhotoFolder,  car.Id + ".jpg");
                var copyPhotoName = Path.Combine(Config.PhotoFolder, "Query", $"{car.GetFilePrefix(arguments.OrderBy)}_{car.Id}.jpg");
                if (File.Exists(photoName))
                {
                    File.Copy(photoName, copyPhotoName);
                }
            }
        }

        static ParsedArgs ParseArguments(string[] args)
        {
            var arguments = new ParsedArgs();

            if (!DateTime.TryParse(args[0], out arguments.StartDate))
            {
                Console.WriteLine("StartDate is invalid");
                arguments.IsValid = false;
            }

            if (!DateTime.TryParse(args[1], out arguments.EndDate) || arguments.EndDate < arguments.StartDate)
            {
                Console.WriteLine("EndDate is invalid");
                arguments.IsValid = false;
            }

            if (!int.TryParse(args[2], out arguments.MinSpeed) || arguments.MinSpeed < 0 || arguments.MinSpeed > 150)
            {
                Console.WriteLine("MinSpeed is invalid.  Must be an integer between 0 and 150");
                arguments.IsValid = false;
            }

            if (!int.TryParse(args[3], out arguments.MaxSpeed) || arguments.MaxSpeed < 0 || arguments.MaxSpeed < arguments.MinSpeed)
            {
                Console.WriteLine("MaxSpeed is invalid.  Must be an integer > MinSpeed");
                arguments.IsValid = false;
            }

            if (!int.TryParse(args[4], out arguments.OrderBy) || arguments.OrderBy < 1 || arguments.OrderBy > 2)
            {
                Console.WriteLine("OrderBy is invalid.  Must be an integer.  1 = Speed, 2 = Date");
                arguments.IsValid = false;
            }

            return arguments;
        }

        static string GetFilePrefix(this Entry car, int orderBy)
        {
            if (orderBy == ORDERBY_SPEED)
                return Math.Floor(car.Speed).ToString("N0");

            return car.DateAdded.ToString("yyyyMMddHHmmss");
        }
    }

    class ParsedArgs
    {
        public bool IsValid;
        public DateTime StartDate;
        public DateTime EndDate;
        public int MinSpeed;
        public int MaxSpeed;
        public int OrderBy;

        public ParsedArgs()
        {
            IsValid = true;
        }
    }
}
