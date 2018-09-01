using OpenCvSharp;

namespace SpeedCam.VideoAnalyzer.Analyze
{
    public static class RectStatic
    {
        public static Rect2d ToRect2D(this Rect rect)
        {
            return new Rect2d(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Rect ToRect(this Rect2d rect)
        {
            return new Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }

        public static Point GetCenter(this Rect rect)
        {
            return new Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
        }
    }
}
