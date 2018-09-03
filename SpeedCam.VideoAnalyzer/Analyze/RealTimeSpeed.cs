using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedCam.VideoAnalyzer.Analyze
{
    /// <summary>
    /// Used to know when to show the speed of the latest car on the frame
    /// </summary>
    public class RealTimeSpeed
    {
        public int StopFrame { get; set; }
        public int Speed { get; set; }
        public bool IsDebug { get; set; }

        public RealTimeSpeed(bool isDebug)
        {
            IsDebug = isDebug;
        }

        public void DrawSpeeds(Mat frame)
        {
            if (!IsDebug || StopFrame == 0)
                return;

            Cv2.PutText(frame, $"{Speed} MPH", new Point(1400,220), HersheyFonts.HersheyPlain, 2, Scalar.White, 2, LineTypes.AntiAlias);
            StopFrame--;
        }
    }
}
