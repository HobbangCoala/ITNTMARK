using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
//using System.IO.Directory;
using System.Runtime.CompilerServices;
using ITNTUTIL;

#pragma warning disable 1998
#pragma warning disable 4014


namespace ITNTCOMMON
{
    public sealed class ITNTTraceLog
    {
        private static readonly Lazy<ITNTTraceLog>
            lazy =
            new Lazy<ITNTTraceLog>
            (() => new ITNTTraceLog());

        public static ITNTTraceLog Instance { get { return lazy.Value; } }
        public string LogFileName { get; set; }

        private string logfilename;
        private int logLevel;
        private bool busedFile = false;
        private DateTime createTime = DateTime.Now;
        //FileStream LogFileStream = null;
        //StreamWriter LogWriter = null;
        static object locker = new object();

        private ITNTTraceLog()
        {
            OpenFile();
            string val = "";
            Util.GetPrivateProfileValue("OPTION", "LOGLEVEL", "0", ref val, Constants.PARAMS_INI_FILE);
            logLevel = Convert.ToInt32(val);
        }

        ITNTTraceLog(string strFileName)
        {
            logfilename = strFileName;
            OpenFile();
        }

        ~ITNTTraceLog()
        {
            CloseFile();
        }

        //ITNTTraceLog GetInstnace()
        //{
        //    if(Instance == null)
        //        Instance = new ITNTTraceLog();

        //    return Instance;
        //}

        //public void TraceCallerInfo(string msg,
        //    [CallerMemberName] string callerName = "",
        //    [CallerFilePath] string callFile = "",
        //    [CallerLineNumber] int sourceLine = 0)
        //{
        //    string traceMsg = string.Format(DateTime.Now.ToString("hh:mm:ss ") +
        //                        "[{0}] [{1}] {2}",
        //                        callerName,
        //                        sourceLine.ToString(),
        //                        msg);
        //    WriteFile(traceMsg);
        //    //Debug.WriteLine(traceMsg);
        //}

        //public void TraceCallerInfo(string msg, [CallerLineNumber] int sourceLine = 0, params object[] args)
        //{
        //    string traceMsg = string.Format(DateTime.Now.ToString("hh:mm:ss ") +
        //                        "[{0}] [{1}] {2}",
        //                        sourceLine.ToString(),
        //                        msg);
        //    WriteFile(traceMsg);
        //    //Debug.WriteLine(traceMsg);
        //}

        //public void TraceCallerInfo2(string msg,
        //            [CallerMemberName] string callerName = "",
        //            [CallerFilePath] string callFile = "",
        //            [CallerLineNumber] int sourceLine = 0, params object[] args)
        //{
        //    string traceMsg = string.Format(DateTime.Now.ToString("hh:mm:ss ") +
        //                        "[{0}] [{1}] ",
        //                        callerName,
        //                        sourceLine.ToString());
        //    string temp = string.Format(msg, args);
        //    traceMsg += temp;
        //    WriteFile(traceMsg);
        //    //Debug.WriteLine(traceMsg);
        //}

        //public void Trace(string msg, params object[] args)
        //{
        //    int lineNumber = (new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber();
        //    string datetimestring = DateTime.Now.ToString("yyyyMMdd hh:mm:ss");
        //    string traceMsg = string.Format("{0} ", datetimestring);
        //    string temp = string.Format(msg, args);
        //    traceMsg += temp;
        //    WriteFile(traceMsg);
        //    //Debug.WriteLine(traceMsg);
        //}

        public async Task Trace(int level, string msg, params object[] args)
        {
            if (logLevel < level)
                return;
            //int lineNumber = (new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber();
            
            DateTime dt = DateTime.Now;
            if((dt.Day != createTime.Day) & (busedFile == false))
            {
                lock(locker)
                {
                    CloseFile();
                    OpenFile();
                }
            }

            string datetimestring = DateTime.Now.ToString("yyyyMMdd HH:mm:ss.fff");
            string traceMsg = string.Format("{0} ", datetimestring);
            string temp = string.Format(msg, args);
            traceMsg += temp;
            WriteFile(traceMsg);
            //Debug.WriteLine(traceMsg);
        }

        public void TraceHex(int level, string msg, int dataLength, byte[] data, params object[] args)
        {
            string temp = "";
            string onedata;
            if (logLevel < level)
                return;

            temp = msg;
            for (int i = 0; i < dataLength; i++)
            {
                if(i == (dataLength-1))
                    onedata = string.Format("{0:X2}", data[i]);
                else
                    onedata = string.Format("{0:X2} ", data[i]);
                temp += onedata;
            }
            Trace(level, temp, args);
            //string traceMsg = string.Format(DateTime.Now.ToString("hh:mm:ss ") + msg + " " + temp);
            ////Debug.WriteLine(traceMsg);
            //WriteFile(traceMsg);
        }

        private bool OpenFile()
        {
            FileStream LogFileStream = null;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            DateTime dt = DateTime.Now;
            createTime = dt;
            curDir = curDir + "Logging\\" + dt.ToString("yyyy");
            curDir = curDir + "\\" + dt.ToString("MM");
            //curDir = curDir + "\\" + dt.ToString("dd");
            if (System.IO.Directory.Exists(curDir) == false)
                System.IO.Directory.CreateDirectory(curDir);
            //string path = "";
            try
            {
                //Util.GetPrivateProfileValue("OPTION", "LOGPath", curDir, ref path, "VisionConfig.ini");
                //logfilename = path + "\\VisionLog_" + dt.ToString("yyyyMMdd") + ".log";
                logfilename = curDir + "\\" + dt.ToString("dd") + ".dat";
                using (LogFileStream = new FileStream(logfilename, FileMode.Append))
                {
                    if (LogFileStream == null)
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OpenFile() exception" + ex.HResult.ToString());
                return false;
            }

            return true;
        }

        private bool CloseFile()
        {
            //if (LogWriter != null)
            //{
            //    LogWriter.Close();
            //    LogWriter.Dispose();
            //}
            //if (LogFileStream != null)
            //{
            //    LogFileStream.Close();
            //    LogFileStream.Dispose();
            //    LogFileStream = null;
            //}
            return true;
        }

        private bool WriteFile(string data)
        {
            try
            {
                busedFile = true;
                //using (StreamWriter wr = new StreamWriter(logfilename))
                //{
                //    // WriteLine()을 써서 한 라인씩 문자열을 쓴다.                
                //    wr.WriteLine(data);
                //}
                string[] msg = new string[1];
                msg[0] = data;
                lock(locker)
                {
                    File.AppendAllLines(logfilename, msg);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("WriteFile() exception = " + ex.HResult.ToString() + "||||" + data);
            }
            busedFile = false;
            return true;
        }
    }
}
