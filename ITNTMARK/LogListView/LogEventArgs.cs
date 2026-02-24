using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITNTMARK
{
    //public enum LogLevel
    //{
    //    Info,
    //    Warning,
    //    Error
    //}

    public class LogEventArgs : EventArgs
    {
        public LogLevel Level { get; }
        public string Message { get; }

        public LogEventArgs(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }
    }

    public class LogItem
    {
        public string Message { get; set; } = string.Empty;
        public LogLevel Level { get; set; }
    }
}
