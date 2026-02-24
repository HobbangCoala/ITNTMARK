using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ITNTMARK
{
    public enum LogLevel { Info, Warning, Error }

    public class LogEntry
    {
        //public DateTime Time { get; set; }
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        //public string ErrorCode { get; set; }
        //public string Solution { get; set; }
        //public ImageSource Icon { get; set; }
    }
}
