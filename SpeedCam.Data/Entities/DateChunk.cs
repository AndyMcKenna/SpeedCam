using System;

namespace SpeedCam.Data.Entities
{
    public class DateChunk : BaseEntity
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public int ProcessingTime { get; set; }
        public int LengthMinutes { get; set; }
        public bool ExportDone { get; set; }
        public DateTime? DateProcessed { get; set; }
    }

    public static class DateChunkExtensions
    {
        public static bool IsInThePast(this DateChunk chunk)
        {
            return chunk.StartDate.AddMinutes(chunk.LengthMinutes) < DateTime.Now;
        }
    }
}
