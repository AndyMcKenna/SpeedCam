using OpenCvSharp;
using SpeedCam.Data.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;

namespace SpeedCam.VideoAnalyzer.Analyze
{
    public class CarTracker
    {
        public int Id { get; set; }
        public Entry Car { get; set; }
        public int StartFrame { get; set; }
        public int EndFrame { get; set; }
        public Rect Rect { get; set; }
        public bool IsUpdated { get; set; }
        public int LastUpdated { get; set; }
        public bool IsInvalid { get; set; }

        const decimal TRACKING_DISTANCE = 54m;

        public double GetDistance(Rect possibleCar)
        {
            var possibleCenter = possibleCar.GetCenter();
            var trackerCenter = Rect.GetCenter();

            return Math.Abs(possibleCenter.X - trackerCenter.X);
        }

        public bool IsPossibleMatch(Rect possibleCar)
        {
            var possibleCenter = possibleCar.GetCenter();
            var trackerCenter = Rect.GetCenter();

            //going the wrong direction or advanced too far
            return ((Car.Direction == "L" && possibleCenter.X >= trackerCenter.X - 100 && possibleCenter.X <= trackerCenter.X + 40) 
                 || (Car.Direction == "R" && possibleCenter.X >= trackerCenter.X - 40 && possibleCenter.X <= trackerCenter.X + 100));
        }

        public void UpdateEvents(int currentFrame, Mat frame, Config config)
        {
            if(Car.Direction == "L")
            {
                if(StartFrame == 0 && Rect.X < config.RightStart && Rect.X > config.RightStart - 100)
                {
                    StartFrame = currentFrame;
                }

                if (EndFrame == 0 && StartFrame != 0 && Rect.X < config.LeftStart && Rect.X > config.LeftStart - 100)
                {
                    EndFrame = currentFrame;
                    Car.Speed = GetSpeed(TRACKING_DISTANCE, GetSecondsElapsed(EndFrame - StartFrame));
                    UpdatePicture();
                }

                if (Rect.GetCenter().X < 900 && Car.Picture == null)
                {
                    Car.Picture = TakePicture(frame);
                }
            }
            else
            {
                if (StartFrame == 0 && Rect.X + Rect.Width >= config.LeftStart && Rect.X + Rect.Width < config.LeftStart + 100)
                {
                    StartFrame = currentFrame;
                }

                if (EndFrame == 0 && StartFrame != 0 && Rect.X + Rect.Width > config.RightStart && Rect.X + Rect.Width < config.RightStart + 100)
                {
                    EndFrame = currentFrame;
                    Car.Speed = GetSpeed(TRACKING_DISTANCE, GetSecondsElapsed(EndFrame - StartFrame));
                    UpdatePicture();
                }

                if (Rect.GetCenter().X > 900 && Car.Picture == null)
                {
                    Car.Picture = TakePicture(frame);
                }
            }
        }

        private decimal GetSecondsElapsed(int frameCount)
        {
            return frameCount / 30m;
        }

        private decimal GetSpeed(decimal distance, decimal seconds)
        {
            if (seconds == 0)
                return 0;

            var speed = distance / seconds * 3600 / 5280m;
            return speed;
        }

        private byte[] TakePicture(Mat frame)
        {
            var center = Rect.GetCenter();
            var picRect = new Rect(center.X - 225, 0, 450, 229);
            if(Rect.Width >= 450)
            {
                picRect = Car.Direction == "L" ?
                          new Rect(Rect.X, 0, 450, 229) :
                          new Rect(Rect.X + Rect.Width - 450, 0, 450, 229);
            }

            if(picRect.X < 0 || picRect.X + picRect.Width > 1920 || picRect.Y < 0 || picRect.Y + picRect.Height > 230)
            {
                return null;
            }

            try
            {
                var cropped = new Mat(frame, picRect);
                return cropped.ToBytes();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error cropping picture: {ex.Message}");
                return null;
            }
        }

        //draw on speed and date
        private void UpdatePicture()
        {
            if(Car.Picture == null)
            {
                return;
            }

            Font drawFont = new Font("Arial", 16, FontStyle.Bold);
            var outlinePen = new Pen(Color.Black, 5)
            {
                LineJoin = LineJoin.Round
            };

            using (var imageStream = new MemoryStream(Car.Picture))
            using (var outputStream = new MemoryStream())
            using (var image = Image.FromStream(imageStream))
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.InterpolationMode = InterpolationMode.High;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                GraphicsPath p = new GraphicsPath();
                p.AddString(
                    Car.DateAdded.ToString("M/d/yy h:mm tt"),
                    FontFamily.GenericSansSerif,
                    (int)FontStyle.Regular,
                    graphics.DpiY * 16 / 72,
                    new System.Drawing.Point(5, 200),
                    new StringFormat());
                graphics.DrawPath(outlinePen, p);
                graphics.FillPath(new SolidBrush(Color.White), p);

                p = new GraphicsPath();
                p.AddString(
                    $"{Math.Floor(Car.Speed):N0} MPH",
                    FontFamily.GenericSansSerif,
                    (int)FontStyle.Regular,
                    graphics.DpiY * 16 / 72,
                    new System.Drawing.Point(360, 200),
                    new StringFormat());
                graphics.DrawPath(outlinePen, p);
                graphics.FillPath(new SolidBrush(Color.White), p);

                image.Save(outputStream, ImageFormat.Png);

                Car.Picture = outputStream.ToArray();
                Car.PhotoUpdated = true;
            }            
        }

        /// <summary>
        /// Check if the Rect has hit the bottom or top of the frame, meaning it's going into a garage or coming out of a yard
        /// </summary>
        public void ValidateTracker(int frameHeight)
        {
            if(Rect.Bottom > frameHeight - 5)
            {
                IsInvalid = true;
            }
        }
    }

    public static class CarTrackerExtensions
    {
        public static bool HasPossibleOverlaps(this List<CarTracker> trackerList)
        {
            var rectList = trackerList.Select(t => new Rect { X = t.Rect.X - 15, Width = t.Rect.Width + 30, Y = t.Rect.Y, Height = t.Rect.Height }).ToList();
            var duplicatedList = new List<Rect>(rectList);
            duplicatedList.AddRange(rectList);

            Cv2.GroupRectangles(duplicatedList, 1, 1);

            return rectList.Count != duplicatedList.Count;
        }
    }
}
