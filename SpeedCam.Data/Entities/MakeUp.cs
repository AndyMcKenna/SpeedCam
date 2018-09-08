using System;

namespace SpeedCam.Data.Entities
{
    public class MakeUp : BaseEntity
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public int LengthMinutes { get; set; }
        public bool ExportDone { get; set; }
        public DateTime? DateProcessed { get; set; }
        public string MachineName { get; set; }
    }
}
