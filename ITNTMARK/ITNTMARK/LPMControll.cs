using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTCOMMM;
using ITNTCOMMON;
using ITNTUTIL;
//using SerialPortLib;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.Reflection;
//using SerialPortLib;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public class LPMControll
    {
        public event LPMControllerDataReceivedEventHandler LPMControllerDataReceivedEventFunc;

        //SerialPortInput serialPortLPM = new SerialPortInput();

        private byte[] RecvEventData = new byte[2048*16];
        private byte[] RecvCommandData = new byte[2048*16];
        private int RecvEventLength = 0;
        private int RecvFrameLength = 0;
        private readonly object bufferLock = new object();
        private readonly object cmdLock = new object();

        bool doingCommand = false;

        Thread statusThread;
        bool doingThread = false;

        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;

        protected static SerialPort Port = new SerialPort();
        protected RingBuffer cb;
        private readonly object comLock = new object();
        private object evtLock = new object();
        public bool IsOpen = false;
        bool isChecking = false;

        public LPMControll()
        {
            Port = new SerialPort();
            cb = new RingBuffer(2048*16);

            SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        }

        ~LPMControll()
        {
            if ((Port != null) && (Port.IsOpen))
                ClosePort();
        }


        public int OpenDevice()
        {
            string className = "LPMControll";
            string funcName = "OpenDevice";

            int retval = 0;
            string value = "";
            string portnum = "COM";
            int baud = 19200;

            try
            {
                Util.GetPrivateProfileValue("LPM", "PORT", "COM2", ref portnum, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("LPM", "BAUDRATE", "115200", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out baud);

                if ((Port != null) && Port.IsOpen)
                    return 0;

                Port.PortName = portnum;// new SerialComm2(port);
                Port.Parity = Parity.None;
                Port.StopBits = StopBits.One;
                Port.BaudRate = baud;
                Port.DataBits = 8;
                Port.Handshake = Handshake.None;
                //Port.ReadTimeout = readtimeout;
                //Port.WriteTimeout = writetimeout;

                if (Port.IsOpen)
                    return 0;
#if TEST_DEBUG_LPM

#else
                Port.Open();
                if (Port.IsOpen)
                {
                    retval = 0;
                    Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler2);
                    IsOpen = true;

                    var message = System.Text.Encoding.UTF8.GetBytes("Z");
                    SendMessage(message);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                    retval = -1;
                }
#endif
            }
            catch(Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            return retval;
        }

        public int CloseDevice()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            doingThread = false;

#if TEST_DEBUG_LPM
            int retval = 0;
#else

            if ((Port == null) || (Port.IsOpen == false))
                return 0;

            doingThread = false;
            //statusThread.Join();

            int retval = 0;
            lock (cmdLock)
            {
                Port.DataReceived -= DataReceivedHandler;
                ClosePort();
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        protected int ClosePort()
        {
            try
            {
                if (!Port.IsOpen)
                    return 0;

                Port.Close();
                if (!Port.IsOpen)
                    return 0;
                else
                    return -1;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }


        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] recv = new byte[2048];
            byte[] tmp = new byte[2048];
            int readsize = 0;
            int totsize = 0;
            lock (bufferLock)
            {
                SerialPort port = (SerialPort)sender;
                int size = port.BytesToRead;
                if (size <= 0)
                    return;
                while (size > 0)
                {
                    readsize = port.Read(recv, 0, size);
                    cb.Put(recv, readsize);
                    size = port.BytesToRead;
                    totsize += readsize;
                }
                ReceiveCommData();
            }
        }

        public void DataReceivedHandler2(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] recv = new byte[4096];
            byte[] tmp = new byte[4096];
            int readsize = 0;
            int tmpsize = 0;
            int totsize = 0;
            lock (bufferLock)
            {
                SerialPort port = (SerialPort)sender;
                int size = port.BytesToRead;
                if (size <= 0)
                    return;
                if (isChecking == true)
                {
                    if (size > tmp.Length)
                        tmpsize = tmp.Length;
                    else
                        tmpsize = size;
                    readsize = port.Read(tmp, 0, tmpsize);
                    return;
                }

                while (size > 0)
                {
                    if (size >= tmp.Length)
                        tmpsize = tmp.Length;
                    else
                        tmpsize = size;
                    readsize = port.Read(tmp, 0, tmpsize);
                    //cb.Put(recv, readsize);
                    if((readsize + totsize) < recv.Length)
                    {
                        Array.Copy(tmp, 0, recv, totsize, readsize);
                        totsize += readsize;
                    }
                    size = port.BytesToRead;
                }
                ReceiveCommData2(recv, totsize);
            }
        }


        protected void ReceiveCommData()
        {
            string className = "LPMControll";
            string funcName = "ReceiveCommData";

            int count = 0;
            byte[] recv = new byte[2048];
            string[] tmpstr;
            int idx = 0;
            byte[] gettmp = new byte[2048];
            LPMControllerRecievedEvnetArgs arg = new LPMControllerRecievedEvnetArgs();

            try
            {
                //cb.Look(ref recv, count);
                lock (bufferLock)
                {
                    while ((count = cb.GetSize()) >= 3)
                    {
                        cb.Look(ref recv, count);
                        tmpstr = BitConverter.ToString(recv, 0, count).Split('-');
                        while (idx < tmpstr.Length)
                        {
                            var cmd = Convert.ToInt32(tmpstr[idx++], 16);
                            switch ((char)cmd)
                            {
                                case 'M':
                                    var hex3 = HexAscii2Bin(tmpstr[idx++]) * 256 + HexAscii2Bin(tmpstr[idx++]) * 16 + HexAscii2Bin(tmpstr[idx++]);
                                    //var x = Mode_File.slope * (double)hex3 + Mode_File.intercept + Mode_File.PeakPower * 0.99;     // ADC => %
                                    arg.execmd = (char)cmd;
                                    arg.value = hex3;

                                    OnLPMControllerDataReceivedEventHandler(arg);
                                    idx += 2;
                                    break;
                                case 'P':
                                    break;
                                case 'Z':
                                    arg.execmd = (char)cmd;
                                    arg.value = 0;
                                    OnLPMControllerDataReceivedEventHandler(arg);
                                    idx += 2;
                                    break;
                                default:
                                    break;
                            }
                        }
                        cb.Get(ref gettmp, idx);
                        recv.Initialize();
                        cb.Look(ref recv, count);
                    }
                }
            }
            catch (Exception ex)
            {
                //IsEventFlag = false;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected void ReceiveCommData2(byte[] portrecv, int size)
        {
            string className = "LPMControll";
            string funcName = "ReceiveCommData";

            //int count = 0;
            byte[] recv = new byte[2048];
            string[] tmpstr;
            int idx = 0;
            //byte[] gettmp = new byte[2048];
            LPMControllerRecievedEvnetArgs arg = new LPMControllerRecievedEvnetArgs();

            try
            {
                if (size < 3)
                    return;
                isChecking = true;
                tmpstr = BitConverter.ToString(portrecv).Split('-');
                idx = 0;
                while (idx < tmpstr.Length)
                {
                    var cmd = Convert.ToInt32(tmpstr[idx++], 16);
                    switch ((char)cmd)
                    {
                        case 'M':
                            var hex3 = HexAscii2Bin(tmpstr[idx++]) * 256 + HexAscii2Bin(tmpstr[idx++]) * 16 + HexAscii2Bin(tmpstr[idx++]);
                            //var x = Mode_File.slope * (double)hex3 + Mode_File.intercept + Mode_File.PeakPower * 0.99;     // ADC => %
                            arg.execmd = (char)cmd;
                            arg.value = hex3;

                            OnLPMControllerDataReceivedEventHandler(arg);
                            idx += 2;
                            break;
                        case 'P':
                            break;
                        case 'Z':
                            arg.execmd = (char)cmd;
                            arg.value = 0;
                            OnLPMControllerDataReceivedEventHandler(arg);
                            idx += 2;
                            break;
                        default:
                            break;
                    }
                }

                //cb.Look(ref recv, count);
                //lock (bufferLock)
                //{
                //    while ((count = cb.GetSize()) >= 3)
                //    {
                //        cb.Look(ref recv, count);
                //        tmpstr = BitConverter.ToString(recv, 0, count).Split('-');
                //        while (idx < tmpstr.Length)
                //        {
                //            var cmd = Convert.ToInt32(tmpstr[idx++], 16);
                //            switch ((char)cmd)
                //            {
                //                case 'M':
                //                    var hex3 = HexAscii2Bin(tmpstr[idx++]) * 256 + HexAscii2Bin(tmpstr[idx++]) * 16 + HexAscii2Bin(tmpstr[idx++]);
                //                    //var x = Mode_File.slope * (double)hex3 + Mode_File.intercept + Mode_File.PeakPower * 0.99;     // ADC => %
                //                    arg.execmd = (char)cmd;
                //                    arg.value = hex3;

                //                    OnLPMControllerDataReceivedEventHandler(arg);
                //                    idx += 2;
                //                    break;
                //                case 'P':
                //                    break;
                //                case 'Z':
                //                    arg.execmd = (char)cmd;
                //                    arg.value = 0;
                //                    OnLPMControllerDataReceivedEventHandler(arg);
                //                    idx += 2;
                //                    break;
                //                default:
                //                    break;
                //            }
                //        }
                //        cb.Get(ref gettmp, idx);
                //        recv.Initialize();
                //        cb.Look(ref recv, count);
                //    }
                //}
            }
            catch (Exception ex)
            {
                //IsEventFlag = false;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            isChecking = false;
        }

        public bool SendMessage(byte[] message)
        {
            string className = "LPMControll";
            string funcName = "SendMessage";

            bool success = false;
            if (Port.IsOpen)
            {
                try
                {
                    Port.Write(message, 0, message.Length);
                    success = true;
                    //LogDebug(BitConverter.ToString(message));
                }
                catch (Exception ex)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                    //LogError(e);
                }
            }
            return success;
        }


        private void OnLPMControllerDataReceivedEventHandler(LPMControllerRecievedEvnetArgs e)
        {
            LPMControllerDataReceivedEventFunc?.Invoke(this, e);
        }

        int HexAscii2Bin(string hex)
        {
            var t1 = (hex[0] >= 'A') ? hex[0] - 'A' + 10 : hex[0] - '0';
            var t2 = (hex[1] >= 'A') ? hex[1] - 'A' + 10 : hex[1] - '0';
            var t3 = t1 * 16 + t2;
            return (t3 >= (int)'A') ? t3 - (int)'A' + 10 : t3 - (int)'0';
        }

    }
}
