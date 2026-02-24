using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTUTIL;
using ITNTCOMMON;
using System.Diagnostics;
using System.IO;

namespace ITNTMARK
{
    class ITNTCompleteLog
    {
        private static readonly Lazy<ITNTCompleteLog>
        lazy =
        new Lazy<ITNTCompleteLog>
        (() => new ITNTCompleteLog());

        public static ITNTCompleteLog Instance { get { return lazy.Value; } }
        public string LogFileName { get; set; }

        private string logfilename;
        private int logLevel;
        private bool busedFile = false;
        private DateTime createTime = DateTime.Now;
        //FileStream LogFileStream = null;
        //StreamWriter LogWriter = null;
        static object locker = new object();

        private ITNTCompleteLog()
        {
            OpenFile();
            string val = "";
            Util.GetPrivateProfileValue("OPTION", "JOBLOGLEVEL", "1", ref val, Constants.PARAMS_INI_FILE);
            logLevel = Convert.ToInt32(val);
        }

        ITNTCompleteLog(string strFileName)
        {
            logfilename = strFileName;
            OpenFile();
        }

        ~ITNTCompleteLog()
        {
            CloseFile();
        }

        public async Task TraceAsync(int level, string msg, params object[] args)
        {
            if (logLevel < level)
                return;

            DateTime dt = DateTime.Now;
            try
            {
                if ((dt.Day != createTime.Day) & (busedFile == false))
                {
                    lock (locker)
                    {
                        CloseFile();
                        OpenFile();
                    }
                }

                string datetimestring = DateTime.Now.ToString("yyyyMMdd HH:mm:ss.fff    ");
                string traceMsg = string.Format("{0} ", datetimestring);
                string temp = string.Format(msg, args);
                traceMsg += temp;
                await WriteFileAsync(traceMsg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Trace() exception" + ex.HResult.ToString());
            }
            //WriteFile(traceMsg);
        }

        public void Trace(int level, string msg, params object[] args)
        {
            if (logLevel < level)
                return;

            DateTime dt = DateTime.Now;
            try
            {
                if ((dt.Day != createTime.Day) & (busedFile == false))
                {
                    lock (locker)
                    {
                        CloseFile();
                        OpenFile();
                    }
                }

                string datetimestring = DateTime.Now.ToString("yyyyMMdd HH:mm:ss.fff    ");
                string traceMsg = string.Format("{0} ", datetimestring);
                string temp = string.Format(msg, args);
                traceMsg += temp;
                WriteFile(traceMsg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Trace() exception" + ex.HResult.ToString());
            }
            //WriteFile(traceMsg);
        }

        public void TraceHex(int level, string msg, int dataLength, byte[] data, params object[] args)
        {
            string temp = "";
            string onedata;
            if (logLevel < level)
                return;

            try
            {
                for (int i = 0; i < dataLength; i++)
                {
                    if (i == (dataLength - 1))
                        onedata = string.Format("{0:X2}", data[i]);
                    else
                        onedata = string.Format("{0:X2} ", data[i]);
                    temp += onedata;
                }
                Trace(level, msg, args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TraceHex() exception" + ex.HResult.ToString());
            }
        }

        private bool OpenFile()
        {
            FileStream LogFileStream = null;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            DateTime dt = DateTime.Now;
            createTime = dt;
            curDir = curDir + "JOBLOG\\" + dt.ToString("yyyy");
            curDir = curDir + "\\" + dt.ToString("MM");
            //            curDir = curDir + "\\" + dt.ToString("dd");
            if (System.IO.Directory.Exists(curDir) == false)
                System.IO.Directory.CreateDirectory(curDir);
            //string path = "";
            try
            {
                //Util.GetPrivateProfileValue("OPTION", "ERRLOGPATH", curDir, ref path, ".\\Parameter\\Params.ini");
                //logfilename = path + "\\" + dt.ToString("yyyyMMdd") + ".dat";
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
            return true;
        }

        async Task WriteFileAsync(string data)
        {
            try
            {
                busedFile = true;
                byte[] encodedText = Encoding.Unicode.GetBytes(data);

                using (FileStream sourceStream = new FileStream(logfilename, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteFile() exception = " + ex.HResult.ToString() + "||||" + data);
            }
            busedFile = false;
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
                lock (locker)
                {
                    File.AppendAllLines(logfilename, msg);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteFile() exception = " + ex.HResult.ToString() + "||||" + data);
                return false;
            }
            busedFile = false;
            return true;
        }

    }
}
