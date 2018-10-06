using SpeedCam.Data.Entities;
using SpeedCam.Data.Db;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SpeedCam.VideoAnalyzer.Analyze;
using SpeedCam.Data;

namespace SpeedCam.VideoAnalyzer
{
    class Program
    {
        public static void Main(string[] args)
        {
            var config = Config.Load();
            var carIdentificationService = new CarIdentificationService(config);
            var currentFileName = "";
            var inputDir = new DirectoryInfo(config.ConvertedFolder);

            while (true)
            {
                try
                {
                    var files = inputDir.GetFiles();
                    if (files.Length > 0)
                    {
                        var stopwatch = Stopwatch.StartNew();
                        var file = files.OrderBy(f => f.Name).First();

                        if(args.Length > 0 && args[0] == "b")
                        {
                            file = files.OrderByDescending(f => f.Name).First();
                        }

                        if (args.Length > 0 && args[0] == "m")
                        {
                            file = files.Skip(files.Length / 2).Take(1).First();
                        }

                        if (args.Length > 0 && args[0] == "logan")
                        {
                            file = files.Skip(files.Length / 3).Take(1).First();
                        }

                        currentFileName = file.Name;
                        //file = new FileInfo(@"C:\users\andy\videos\speedcam\20180715150000_20180715153000.mp4");
                        Console.WriteLine($"{DateTime.Now.ToString("hh:mm")}: Analyzing {file.Name}   {files.Length} files left");
                        carIdentificationService.AnalyzeVideo(file.FullName, false);
                        File.Move(file.FullName, Path.Combine(config.AnalyzedFolder, file.Name));
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("hh:mm")}: Not enough files, sleeping for 60 seconds");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    var database = new SqlDatabase(config.DbConnectionString);
                    database.LogInsert(new Log
                    {
                        DateAdded = DateTime.Now,
                        Message = $"File: {currentFileName} - Message: {ex.Message}",
                        StackTrace = ex.StackTrace
                    });
                    Console.ReadLine();
                }
            }
        }
    }
}
