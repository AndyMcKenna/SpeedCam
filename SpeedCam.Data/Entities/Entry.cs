using System;

namespace SpeedCam.Data.Entities
{
    public class Entry : BaseEntity
    {
        public int Id { get; set; }
        public DateTime DateAdded { get; set; }
        public string Direction { get; set; }
        public decimal Speed { get; set; }
        public byte[] Picture { get; set; }
        public bool PhotoUpdated { get; set; }
    }
}
