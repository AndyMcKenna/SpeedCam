using SpeedCam.Data.Entities;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedCam.VideoAnalyzer.Recording
{
    public class AmcrestNVRClient : IVideoSourceClient
    {
        private static HttpClient HttpClient;
        private readonly Config Config;

        public AmcrestNVRClient(Config config)
        {
            var credCache = new CredentialCache
            {
                { new Uri(config.VideoAddress), "Digest", new NetworkCredential(config.VideoUser, config.VideoPassword) }
            };

            var clientHandler = new HttpClientHandler
            {
                Credentials = credCache,
                PreAuthenticate = true
            };
            HttpClient = new HttpClient(clientHandler)
            {
                Timeout = new TimeSpan(0, 0, 0, 0, -1)
            };

            Config = config;
        }

        public async Task<string> ExportVideo(DateTime startDate, DateTime endDate, int channel)
        {
            var startTime = startDate.ToString(@"yyyy-MM-dd\%20HH:mm:ss");
            var endTime = endDate.ToString(@"yyyy-MM-dd\%20HH:mm:ss");

            Console.WriteLine($"Exporting file for {startDate.ToString("yyyy-MM-dd HH:mm tt")}");

            //pull the video data from the NVR
            var url = $"{Config.VideoAddress}/cgi-bin/loadfile.cgi?action=startLoad&channel={channel}&startTime={startTime}&endTime={endTime}&subtype=0";
            var result = await HttpClient.GetAsync(url);
            Console.WriteLine($"{result.StatusCode}: {result.ReasonPhrase}");
            var fileContents = await result.Content.ReadAsStreamAsync();

            //write it out to disk
            startTime = startDate.ToString("yyyyMMddHHmmss");
            endTime = endDate.ToString("yyyyMMddHHmmss");
            var fileName = $"{startTime}_{endTime}";

            var outputFileName = Path.Combine(Config.ExportFolder, fileName + ".dav");

            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Saving file to {outputFileName}");
            using (var fileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
            {
                fileContents.CopyTo(fileStream);
            }
            Console.WriteLine($"Completed saving file to {outputFileName} in {stopwatch.Elapsed.ToString(@"hh\:mm\:ss")}");

            return outputFileName;
        }

        public string ConvertDAVtoMP4(string fileName)
        {
            var file = new FileInfo(fileName);

            var outputFileName = Path.Combine(Config.ConvertedFolder, Path.GetFileNameWithoutExtension(file.Name) + ".mp4");
            //var completedFile = Path.Combine(file.Directory.FullName, "DoneFiles", file.Name);
            var errorFile = Path.Combine(Config.ConvertedErrorFolder, file.Name);

            outputFileName = Path.Combine(Config.ConvertedFolder, Path.GetFileNameWithoutExtension(file.Name) + ".mp4");
            errorFile = Path.Combine(Config.ConvertedErrorFolder, file.Name);

            //Use ffmpeg to convert from DAV to x264/MP4            
            var stopwatch = Stopwatch.StartNew();
            var videoConvertCommand = $"ffmpeg.exe -y -loglevel 8 -stats -r 30 -f h264 -i {file} -vf \"crop=1920:230:0:220\" -map_chapters -1 -flags +global_header {outputFileName}\"";
            //$"ffmpeg.exe -nostats -y -r 30 -i {tempDir}{fileName}.dav -vcodec libx264 -filter:v \"setpts=1*PTS\" {readyDir}{fileName}.mp4\"";
            //Process.Start("CMD.exe", videoConvertCommand);

            var process = new ProcessStartInfo
            {
                WorkingDirectory = @"C:\Windows\System32",
                FileName = @"C:\Windows\System32\cmd.exe",
                Arguments = "/c " + videoConvertCommand,
                UseShellExecute = false,
            };
            var convertingProcess = Process.Start(process);
            convertingProcess.WaitForExit();


            Thread.Sleep(1000);
            var outputInfo = new FileInfo(outputFileName);
            if (outputInfo.Exists && outputInfo.Length > 500)
            {
                outputInfo = null;
                File.Delete(file.FullName);
            }
            else
            {
                File.Delete(errorFile);
                File.Move(file.FullName, errorFile);
            }

            Console.WriteLine($"{DateTime.Now.ToString("hh:mm")}: Converting {Path.GetFileName(file.Name)} to mp4 took {stopwatch.Elapsed.ToString(@"hh\:mm\:ss")}                   ");

            return outputFileName;
        }
    }
}
