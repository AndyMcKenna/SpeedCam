using Innovative.Geometry;
using Innovative.SolarCalculator;
using SpeedCam.Data.Db;
using SpeedCam.Data.Entities;
using SpeedCam.VideoAnalyzer.Analyze;
using SpeedCam.VideoAnalyzer.Recording;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedCam.Main
{
    class Program
    {
        private static Config Config;
        private static CarIdentificationService CarIdentificationService;
        private static IVideoSourceClient ExportService;
        private static IDatabase Database;
        private static bool IsDebug;

        static async Task Main(string[] args)
        {
            Config = Config.Load();
            CarIdentificationService = new CarIdentificationService(Config);
            ExportService = new AmcrestNVRClient(Config);
            Database = new SqlDatabase(Config.DbConnectionString);

            IsDebug = args.Any(a => a == "-debug");

            var specificFlagIndex = Array.IndexOf(args, "-s");

            //process normally
            if (specificFlagIndex == -1)
            {
                //if the last chunk was not completed, delete it so it can start over
                var latestChunk = Database.GetLatestDateChunk();
                if(ShouldDeleteLatestChunk(latestChunk))
                {
                    Database.DeleteDateChunk(latestChunk);
                }

                while (true)
                {
                    try
                    {
                        var makeUp = Database.GetNextMakeUp();
                        if (makeUp != null)
                        {
                            await ProcessMakeUp(makeUp);
                        }
                        else
                        {
                            await ProcessNextChunk();
                        }
                    }
                    catch (Exception ex)
                    {
                        Database.LogInsert(new Log
                        {
                            DateAdded = DateTime.Now,
                            Message = $"Error in main loop: {ex.Message}",
                            StackTrace = ex.StackTrace
                        });
                        Console.WriteLine($"ERROR: {ex.Message}");
                    }

                    DrawSleepyDots(5);
                    Console.WriteLine("");
                }
            }
            else //process a specific datetime
            {
                if(args.Length < specificFlagIndex + 3)
                {
                    Console.WriteLine("The Specific flag requires a datetime and length");
                    return;
                }
                else
                {
                    var startDate = DateTime.Parse(args[specificFlagIndex + 1]);
                    var chunkTime = int.Parse(args[specificFlagIndex + 2]);

                    await ProcessFootage(startDate, chunkTime, () => { return; });
                }
            }
        }

        static async Task ProcessMakeUp(MakeUp makeUp)
        {
            makeUp.MachineName = GetMachineId();
            Database.UpdateMakeUp(makeUp);
            try
            {
                await ProcessFootage(makeUp.StartDate, makeUp.LengthMinutes, () => MakeUpExported(makeUp));
                Database.DeleteMakeUp(makeUp);
            }
            catch (Exception ex)
            {
                Database.LogInsert(new Log
                {
                    DateAdded = DateTime.Now,
                    Message = $"Make Up: {makeUp.StartDate} - Message: {ex.Message}",
                    StackTrace = ex.StackTrace
                });
                makeUp.ExportDone = false;
                makeUp.MachineName = null;
                Database.UpdateMakeUp(makeUp);
            }
        }

        static async Task ProcessNextChunk()
        {
            var nextChunk = Database.GetNextDateChunk();
            if(nextChunk == null)
            {
                return;
            }

            nextChunk.LengthMinutes = Config.ChunkTime;
            nextChunk.MachineName = GetMachineId();
            nextChunk = ValidateDateChunk(nextChunk);
            if(nextChunk == null)
            {
                return;
            }

            Database.InsertDateChunk(nextChunk);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await ProcessFootage(nextChunk.StartDate, nextChunk.LengthMinutes, () => ChunkExported(nextChunk));
            }
            catch (Exception ex)
            {
                Database.LogInsert(new Log
                {
                    DateAdded = DateTime.Now,
                    Message = $"Chunk: {nextChunk.StartDate} - Message: {ex.Message}",
                    StackTrace = ex.StackTrace
                });

                Database.InsertMakeUp(new MakeUp
                {
                    StartDate = nextChunk.StartDate,
                    LengthMinutes = nextChunk.LengthMinutes,
                    ExportDone = false
                });
            }
            finally
            {
                nextChunk.DateProcessed = DateTime.Now;
                nextChunk.ProcessingTime = (int)stopwatch.Elapsed.TotalSeconds;

                Database.UpdateDateChunk(nextChunk);
            }
        }

        static async Task ProcessFootage(DateTime startDate, int lengthMinutes, Action exportCompleted, bool logResults = true)
        {
            var rawFile = await ExportService.ExportVideo(startDate, startDate.AddMinutes(lengthMinutes), Config.VideoChannel);
            exportCompleted();
            DrawSleepyDots(5);
            try
            {
                var convertedFile = ExportService.ConvertDAVtoMP4(rawFile);
                var convertedInfo = new FileInfo(convertedFile);
                if (!convertedInfo.Exists || convertedInfo.Length < 500)
                {
                    var errorMessage = $"Error converting DAV to MP4 for {convertedFile}";
                    Database.LogInsert(new Log
                    {
                        DateAdded = DateTime.Now,
                        Message = errorMessage,
                        StackTrace = ""
                    });
                    Console.WriteLine(errorMessage);
                }
                else
                { 
                    DrawSleepyDots(2);
                    CarIdentificationService.AnalyzeVideo(convertedFile, IsDebug);
                }
                //File.Delete(convertedFile);
            }
            catch
            {
                File.Delete(rawFile);
                throw;
            }
        }

        static void ChunkExported(DateChunk chunk)
        {
            chunk.ExportDone = true;
            Database.UpdateDateChunk(chunk);
        }

        static void MakeUpExported(MakeUp makeUp)
        {
            makeUp.ExportDone = true;
            Database.UpdateMakeUp(makeUp);
        }

        static void DrawSleepyDots(int seconds)
        {
            Console.Write($"{DateTime.Now.ToString("HH:mm:ss tt")}: ");
            for (int i = 0; i < seconds; i++)
            {
                Thread.Sleep(1000);
                Console.Write(".");
            }
        }

        static (DateTime Start, DateTime End) GetSolarTimes(DateTime date)
        {
            var solarTimes = new SolarTimes(date, new Angle(Config.Latitude), new Angle(Config.Longitude));
            return (
                Start: solarTimes.Sunrise.AddMinutes(Config.StartTimeSunrise),
                End: solarTimes.Sunset.AddMinutes(Config.EndTimeSunset)
            );
        }

        static DateChunk ValidateDateChunk(DateChunk chunk)
        {
            var solar = GetSolarTimes(chunk.StartDate);

            //if it starts too early, move it up to our first time of the day
            if (chunk.StartDate < solar.Start.AddMinutes(Config.StartTimeSunrise))
            {
                chunk.StartDate = solar.Start.AddMinutes(Config.StartTimeSunrise);
                return chunk.IsInThePast() ? chunk : null;
            }

            //if it starts after the end of the day, move it to tomorrow
            if (chunk.StartDate > solar.End.AddMinutes(Config.EndTimeSunset))
            {
                chunk.StartDate = chunk.StartDate.Date.AddDays(1);
                return ValidateDateChunk(chunk);
            }

            //if it would end after the end of the day, cut the length to stop then
            if (chunk.StartDate.AddMinutes(chunk.LengthMinutes) > solar.End.AddMinutes(Config.EndTimeSunset))
            {
                chunk.LengthMinutes = (int)(solar.End.AddMinutes(Config.EndTimeSunset) - chunk.StartDate).TotalMinutes + 1;
                return chunk.IsInThePast() ? chunk : null;
            }

            //it's the middle of the day
            return chunk.IsInThePast() ? chunk : null;
        }

        static string GetMachineId()
        {
            return $"{Environment.MachineName}:{Process.GetCurrentProcess().Id}";
        }

        static bool ShouldDeleteLatestChunk(DateChunk chunk)
        {
            if (chunk == null)
                return false;

            //This should be updated to make sure only OUR program on dotnet core is counted but it's good enough for now
            var alreadyRunning = Process.GetProcessesByName("dotnet").Count() > 1;
            var sameMachine = chunk.MachineName?.StartsWith(Environment.MachineName) ?? false;

            return chunk.ProcessingTime == 0 && !alreadyRunning && sameMachine;
        }
    }
}
