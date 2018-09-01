using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedCam.Data.Entities
{
    public class Log
    {
        public int Id { get; set; }
        public DateTime DateAdded { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
