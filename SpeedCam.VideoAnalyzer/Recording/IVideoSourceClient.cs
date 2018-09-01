using System;
using System.Threading.Tasks;

namespace SpeedCam.VideoAnalyzer.Recording
{
    public interface IVideoSourceClient
    {
        Task<string> ExportVideo(DateTime startDate, DateTime endDate, int channel);
        string ConvertDAVtoMP4(string fileName);
    }
}
