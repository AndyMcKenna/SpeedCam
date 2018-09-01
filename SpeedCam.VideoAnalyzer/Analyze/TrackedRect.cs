using OpenCvSharp;

namespace SpeedCam.VideoAnalyzer.Analyze
{
    public class TrackedRect
    {
        public Rect Rect { get; set; }
        public int Id { get; set; }
    }

    public class Neighbor
    {
        public int Id { get; set; }
        public double Distance { get; set; }
    }
}
