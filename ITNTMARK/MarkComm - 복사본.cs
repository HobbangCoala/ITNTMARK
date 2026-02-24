using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ITNTCOMMM;
using ITNTCOMMON;
using ITNTUTIL;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Windows;

namespace ITNTMARK
{
    public class MarkComm// : Serial
    {
        public event MarkControllerStatusDataEventHandler MarkControlletDataArrivedEventFunc;

        private byte NAKError = 0;
        private byte[] RecvEventData = new byte[2048];
        private byte[] RecvCommandData = new byte[2048];
        private int RecvEventLength = 0;
        private int RecvFrameLength = 0;
        private readonly object bufferLock = new object();
        private readonly object cmdLock = new object();

        //int cmdIndex = 0;

        bool doingCommand = false;

        Thread statusThread;
        bool doingThread = false;

        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;

        protected static SerialPort Port = new SerialPort();
        protected RingBuffer cb;
        private readonly object comLock = new object();
        private readonly object cbLock = new object();
        private object thisLock = new object();
        public bool IsOpen = false;
        //bool IsEventFlag = false;

        public MarkComm()
        {
            Port = new SerialPort();
            cb = new RingBuffer(0x2000);

            SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        }

        ~MarkComm()
        {
            if ((Port != null) && (Port.IsOpen))
                ClosePort();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="baud"></param>
        /// <param name="databits"></param>
        /// <param name="parity"></param>
        /// <param name="stopbit"></param>
        /// <param name="readtimeout"></param>
        /// <param name="writetimeout"></param>
        /// <returns></returns>
        public int OpenDevice(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string ClassName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "START");

            int retval = 0;

#if TEST_DEBUG_MARK
            retval = 0;
#else
            if ((Port != null) && Port.IsOpen)
                return 0;

            retval = OpenPort(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
            if (retval == 0)
            {
                statusThread = new Thread(ThreadStatusCheck2);
                statusThread.Start();
                doingThread = true;
                Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                IsOpen = true;
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("OpenPort SUCCESS : {0}", retval));
            }
            else
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("OpenPort ERROR : {0}", retval));
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "END");
            return retval;
        }

        public async Task<int> OpenDeviceAsync(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string ClassName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "START");

            int retval = 0;

#if TEST_DEBUG_MARK
            retval = 0;
#else
            if ((Port != null) && (Port.IsOpen))
                return 0;

            retval = await OpenPortAsync(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
            if (retval == 0)
            {
                statusThread = new Thread(ThreadStatusCheck);
                statusThread.Start();
                doingThread = true;
                Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("OpenPort SUCCESS : {0}", retval));
            }
            else
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("OpenPort ERROR : {0}", retval));
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "END");
            return retval;
        }

        protected int OpenPort(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            int retval = 0;

            try
            {
                Port.PortName = port;// new SerialComm2(port);
                Port.Parity = parity;
                Port.StopBits = stopbit;
                Port.BaudRate = baud;
                Port.DataBits = databits;
                Port.Handshake = Handshake.None;
                Port.ReadTimeout = readtimeout;
                Port.WriteTimeout = writetimeout;

                if (Port.IsOpen)
                    return 0;

                Port.Open();
                if (Port.IsOpen)
                {
                    retval = 0;
                }
                else
                    retval = -1;
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
            }

            return retval;
        }

        protected async Task<int> OpenPortAsync(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            int retval = 0;

            try
            {
                Port.PortName = port;// new SerialComm2(port);
                Port.Parity = parity;
                Port.StopBits = stopbit;
                Port.BaudRate = baud;
                Port.DataBits = databits;
                Port.Handshake = Handshake.None;
                Port.ReadTimeout = readtimeout;
                Port.WriteTimeout = writetimeout;

                if (Port.IsOpen)
                    return 0;

                Task task = new Task(new Action(delegate
                {
                    Port.Open();
                }));

                await task;
                if (Port.IsOpen)
                    retval = 0;
                else
                    retval = -1;
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
            }

            //Port.DataReceived += new SerialDataReceivedEventHandler(OnDataReceivedHandler);

            return retval;
        }


        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] recv = new byte[0x2000];
            byte[] tmp = new byte[0x2000];
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

        /// <summary>
        /// Close Port
        /// </summary>
        /// <returns></returns>
        public int CloseDevice()
        {
            string ClassName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "START");

            doingThread = false;

            //DoReading = false;
            //_readThread.Join(); //block until exits
            //_readThread.Abort();
            //_readThread = null;
#if TEST_DEBUG_MARK
            int retval = 0;
#else

            if ((Port == null) || (Port.IsOpen == false))
                return 0;

            doingThread = false;
            //statusThread.Join();

            int retval = 0;
            lock (cmdLock)
            {
                ClosePort();
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "END");
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

            //lock (thisLock)
            //{
            //}
        }

        public string[] GetSerialPorts()
        {
            string[] portNames = SerialPort.GetPortNames();
            return portNames;
        }

        /// <summary>
        /// Thread for Reading Serial Port
        /// </summary>
        //private async void ReadPortThread()
        //{
        //    int readsize = 0;
        //    while (DoReading)
        //    {
        //        if (Port.IsOpen && (Port.BytesToRead > 0))
        //        {
        //            byte[] readBuffer = new byte[Port.ReadBufferSize + 1];
        //            try
        //            {
        //                // If there are bytes available on the serial port,
        //                // Read returns up to "count" bytes, but will not block (wait)
        //                // for the remaining bytes. If there are no bytes available
        //                // on the serial port, Read will block until at least one byte
        //                // is available on the port, up until the ReadTimeout milliseconds
        //                // have elapsed, at which time a TimeoutException will be thrown.
        //                int count = ReadPort(Port.ReadBufferSize, ref readsize, ref readBuffer);
        //                //ITNTTraceLog.Instance.TraceHex(0, "ITNTSerialComm::ReadPortThread()  RECV DATA : ", readsize, ref readBuffer, "");
        //                if (readsize > 0)
        //                {
        //                    lock (bufferLock)
        //                    {
        //                        //cb.Put(readBuffer, 0, readsize);
        //                        cb.Put(readBuffer, readsize);
        //                    }
        //                    ReceiveCommData();
        //                }
        //            }
        //            catch (TimeoutException) { }
        //        }
        //        else
        //        {
        //            //TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, 50);
        //            //Thread.Sleep(waitTime);
        //            await Task.Delay(50);
        //        }
        //    }
        //}

        /// <summary>
        /// 수신 데이터 분석 및 수신 버퍼에 저장
        /// </summary>
        //public void ReceiveCommData2()
        //{
        //    string ClassName = "ITNTSerialComm";
        //    string FuncName = "ReceiveCommData";
        //    //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "START : " + cb.Count().ToString());

        //    int count = 0;
        //    int retval = 0;
        //    byte[] tmp = new byte[2];
        //    try
        //    {
        //        count = cb.GetSize();
        //        if (count <= 0)
        //        {
        //            return;
        //        }

        //        retval = cb.LookReverse(tmp, 2);
        //        if ((retval >= 2) && (tmp[0] == 0x05) && (tmp[1] == LF))
        //        {
        //            cb.Get(ref RecvCommandData, count);
        //            RecvFlag = RECVFLAG_RECV_END;
        //            RecvFrameLength = count;
        //        }
        //        else
        //            return;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
        //    }
        //    //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "END");
        //}

        /// <summary>
        /// 수신 데이터 분석 및 수신 버퍼에 저장
        /// </summary>
        protected void ReceiveCommData()
        {
            string ClassName = "ITNTSerialComm";
            string FuncName = "ReceiveCommData";
            //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "START : " + cb.Count().ToString());

            int count = 0;
            //int retval = 0;
            int size = 0;
            int i = 0;
            byte tmp = 0;
            byte[] recv = new byte[0x2000];
            int idxSOH = 0;
            int idxCR = 0;
            //byte[] tmp = new byte[2];
            try
            {
                while ((count = cb.GetSize()) > 7)
                {
                    cb.Look(ref recv, count);
                    if (recv.Contains((byte)ASCII.SOH))
                    {
                        idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                        if (idxSOH > 0)
                        {
                            for (i = 0; i < idxSOH; i++)
                                cb.Get(ref tmp);
                            idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                        }
                        else if (idxSOH < 0)
                            return;

                        if (recv.Contains((byte)ASCII.CR))
                        {
                            //idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                            idxCR = Array.IndexOf(recv, (byte)ASCII.CR);//, idxSOH);
                            if (idxCR < 0)
                                return;
                        }
                        else
                            return;

                        if (idxCR > idxSOH)
                        {
                            size = idxCR - idxSOH + 1;
                            if ((recv[idxSOH + 3] >= '0') && (recv[idxSOH + 3] <= '9'))
                            {
                                //if (IsEventFlag)
                                //    return;
                                //IsEventFlag = true;
                                cb.Get(ref RecvEventData, size);
                                RecvEventLength = size;

                                MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();
                                arg.cmd = RecvEventData[idxSOH + 3];
                                Array.Copy(RecvEventData, arg.receiveBuffer, RecvEventLength);
                                arg.receiveSize = RecvEventLength;
                                RecvEventData.Initialize();
                                RecvEventLength = 0;
                                //ITNTTraceLog.Instance.Trace(0, string.Format("S{0}", size));
                                //ITNTTraceLog.Instance.TraceHex(0, "MarkComm::ReceiveCommData()  SEND MARK :  ", arg.receiveSize, arg.receiveBuffer);
                                OnMarkControllerStatusDataEventHandler(arg);
                                //IsEventFlag = false;
                            }
                            else
                            {
                                cb.Get(ref RecvCommandData, size);
                                RecvFrameLength = size;
                                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("!!!!");
                        return;
                    }
                    recv.Initialize();
                }
            }
            catch (Exception ex)
            {
                //IsEventFlag = false;
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
            }
            //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "END");
        }

        public async Task<int> SendCommandMsg(int loglevel, byte type, byte[]sendData, int sendLength)
        {
            string ClassName = "MarkController";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = "SendCommandMsg";     // MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(3, "{loglevel}::{1}()  {2}", ClassName, FuncName, "START");

            //int size = 0;
            int retval = 0;
            int i = 0;
            byte[] sendmsg = new byte[128];
            int leng = 0;
            string strLeng = "";
            byte[] byLeng = new byte[2];
            try
            {
                leng = sendLength + 5;
                strLeng = leng.ToString("X2");
                byLeng = Encoding.UTF8.GetBytes(strLeng);

                sendmsg[i++] = (byte)ASCII.SOH;
                sendmsg[i++] = byLeng[0];
                sendmsg[i++] = byLeng[1];
                sendmsg[i++] = type;
                sendmsg[i++] = (byte)ASCII.STX;
                Array.Copy(sendData, 0, sendmsg, i, sendLength);
                i += sendLength;
                sendmsg[i++] = (byte)ASCII.ETX; 
                sendmsg[i++] = GetBCC(sendmsg, 1, i-1); 
                sendmsg[i++] = (byte)ASCII.CR; 

                retval = WritePort(sendmsg, 0, i);
                if (retval <= 0)
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                    SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_ERR;
                    return retval;
                }
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_END;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, "END");
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loglevel"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<ITNTResponseArgs> RecvResponseMsg(int loglevel, int timeout = 2)
        {
            //int retval = 0;
            string ClassName = "MarkComm";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = "RecvResponseMsg";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, "START");
            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] respMsg = new byte[2048];
            int respSize = 0;
            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "RESPONSE_TIMEOUT");
                    retval.execResult = (int)COMPORTERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    return retval;
                }

                Array.Copy(RecvCommandData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("RECV OK (SIZE = {0})", retval.recvSize));
                //respSize = RecvFrameLength-6;
                //if(respSize < 0)
                //{
                //    respSize = 0;
                //}
                //if(respSize > 0)
                //{
                //    retval.recvString = Encoding.UTF8.GetString(respMsg, 4, respSize);
                //    ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("RECV : {0} / {1}", retval.recvString, respSize));
                //}
                //else
                //    ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("RECV OK"));
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval.execResult = ex.HResult;
                return retval;
            }
            return retval;
        }

        protected int WritePort(byte[] buffer, int offset, int count)
        {
            if (!Port.IsOpen)
                return (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
            lock (comLock)
            {
                Port.Write(buffer, offset, count);
            }
            if (Port.BytesToWrite > 0)
            {
                return (int)COMPORTERROR.ERR_SEND_DATA_FAIL;
            }
            return count;
        }

        public async Task<ITNTResponseArgs> ExecuteCommandMsg(int loglevel, int timeoutsec, byte type, byte[] sendData, int sendLength)
        {
            string ClassName = "MarkController";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = "ExecuteCommandMsg";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("START - {0}", type));

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeoutsec))
                {
                    if (doingCommand == false)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (doingCommand == true)
                {
                    retval.execResult = (int)COMPORTERROR.ERR_COMMAD_BUSY;
                    return retval;
                }

                if (Port.IsOpen == false)
                {
                    retval.execResult = (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
                    doingCommand = false;
                    return retval;
                }

                doingCommand = true;

                //lock (cmdLock)
                {
                    for (retrycount = 0; retrycount < 3; retrycount++)
                    {
                        InitializeExecuteCommand();
                        retval.execResult = await SendCommandMsg(loglevel, type, sendData, sendLength);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval.execResult, retrycount));
                            continue;
                        }
                        retval = await RecvResponseMsg(loglevel, timeoutsec);
                        if (retval.execResult == 0)
                            break;
                        else if (retval.execResult == (int)COMPORTERROR.ERR_RECV_NAK)
                        {
                            lock (bufferLock)
                            {
                                cb.Clear();
                            }
                        }
                        else
                            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval.execResult, retrycount));
                    }
                    if(retval.execResult != 0)
                    {
                        lock (bufferLock)
                        {
                            cb.Clear();
                        }
                    }
                    doingCommand = false;
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("END - {0}", type));
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval.execResult = ex.HResult;
                doingCommand = false;
                return retval;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitializeExecuteCommand()
        {
            //SendFlag = SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
            RecvCommandData.Initialize();
            RecvFrameLength = 0;
            NAKError = 0;
            //cmdIndex = 0;
        }

        public byte GetBCC(byte[] inputStream)
        {
            byte bcc = 0;

            if (inputStream != null && inputStream.Length > 0)
            {
                for (int i = 0; i < inputStream.Length; i++)
                    bcc ^= inputStream[i];
            }
            return bcc;
        }

        public byte GetBCC(byte[] inputStream, int offset, int count)
        {
            byte bcc = 0;
            if ((inputStream != null) && (inputStream.Length > 0))
            {
                for (int i = offset; i < count; i++)
                {
                    if (i < inputStream.Length)
                        bcc ^= inputStream[i];
                    else
                        break;
                }
            }
            return bcc;
        }

        public async Task<ITNTResponseArgs> GetCurrentSetting()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            //retval = await ExecuteCommandMsg(0, 5, (byte)'0', sdata, sleng);
#endif
            return retval;
        }


        public async void ThreadStatusCheck()
        {
            int retval = 0;
            while(doingThread)
            {
                if ((Port == null) || (!Port.IsOpen))
                {
                    await Task.Delay(100);
                    continue;
                }

                await GetCurrentSetting();
                await Task.Delay(3000);
            }
        }

        public async void ThreadStatusCheck2()
        {
            int retval = 0;
            while (doingThread)
            {
                if ((Port == null) || (!Port.IsOpen))
                {
                    await Task.Delay(100);
                    continue;
                }

                //await GetStatus();
                //await SendVIN();
                await Task.Delay(3000);
            }
        }

        public async Task<ITNTResponseArgs> GetStatus()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            byte[] sendData = new byte[16];
            retval = await ExecuteCommandMsg(2, 5, 0x53, sendData, 0);
            return retval;
        }

        private void OnMarkControllerStatusDataEventHandler(MarkControllerRecievedEvnetArgs e)
        {
            MarkControlletDataArrivedEventFunc?.Invoke(this, e);
        }

        public async Task<ITNTResponseArgs> LoadFontData(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(2, 5, (byte)'D', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> Opmode(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'A', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> StrikeNo(byte[]sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'N', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> RunStart(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'R', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> SolOnOffTime(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'S', sdata, sleng);
#endif
            return retval;
        }

        //public async Task<ITNTResponseArgs> FreeSpeed(string cmd, string Speed)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandMsg(0, 20, cmd, Speed);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> LoadSpeed(byte[]sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'L', sdata, sleng);
#endif
            return retval;
        }

        //public async Task<ITNTResponseArgs> Resume(string cmd, string R_value)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandMsg(0, 20, cmd, R_value);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> GoHome(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'H', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> Jog_XY(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'J', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> TestSolFet(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'O', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> TestBox4(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'C', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> GoPoint(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'M', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParking(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'K', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> GearRatio(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'G', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SetMaxMinXY(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'X', sdata, sleng);
#endif
            return retval;
        }

        //public async Task<ITNTResponseArgs> GetFwVersion(byte[] sdata, int sleng)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandMsg(0, 2, cmd, sdata, sleng);
        //    return retval;
        //}
        //public async Task<ITNTResponseArgs> Inport(byte[] sdata, int sleng)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandMsg(0, 2, cmd, sdata, sleng);
        //    return retval;
        //}


        //public void GetStartPointLinear(int count, Point CP, Point START_XY, double PITCH, double ANG, ref List<Point> POS)
        //{
        //    //POS = new MarkController.MPOINT[STR_COUNT];
        //    int i;
        //    try
        //    {
        //        for (i = 0; i <= count - 1; i++)
        //        {
        //            Point pt = new Point();
        //            pt = Rotate_Point(START_XY.X + (i * PITCH), START_XY.Y, CP.X, CP.Y, ANG);
        //            POS.Add(pt);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("GetStartPointLinear Exception - {0}, {1}", ex.HResult, ex.Message);
        //    }
        //}

        //public Point Rotate_Point(double tx, double ty, double cx, double cy, double deg)
        //{
        //    //MarkController.MPOINT returnValue = default;
        //    Point returnValue = new Point();
        //    double nx;
        //    double ny;
        //    double q;

        //    try
        //    {
        //        q = deg * System.Math.PI / 180;

        //        System.Double cosq = System.Math.Cos(q);
        //        System.Double sinq = System.Math.Sin(q);
        //        tx -= cx;
        //        ty -= cy;

        //        nx = tx * cosq - ty * sinq;// double.Parse(Microsoft.VisualBasic.Strings.Format(tx * cosq - ty * sinq, "000.0000"));
        //        ny = ty * cosq + tx * sinq;//double.Parse(Microsoft.VisualBasic.Strings.Format(ty * cosq + tx * sinq, "000.0000"));
        //        nx += cx;
        //        ny += cy;
        //        returnValue.X = nx;
        //        returnValue.Y = ny;
        //    }
        //    catch(Exception ex)
        //    {
        //        Debug.WriteLine("Rotate_Point Exception - {0}, {1}", ex.HResult, ex.Message);
        //    }
        //    return returnValue;
        //}


        ///////////// Profile Scan/////////////////////
        public async Task<ITNTResponseArgs> Scan(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'U', sdata, sleng);
#endif
            return retval;
        }
        public async Task<ITNTResponseArgs> Profile_Speed(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'i', sdata, sleng);
#endif
            return retval;
        }
        public async Task<ITNTResponseArgs> Home_U(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'U', sdata, sleng);
#endif
            return retval;
        }
        public async Task<ITNTResponseArgs> Jog_U(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'j', sdata, sleng);
#endif
            return retval;
        }
        public async Task<ITNTResponseArgs> GoParking_U(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'k', sdata, sleng);
#endif
            return retval;
        }
        public async Task<ITNTResponseArgs> GearRatio_U(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(0, 5, (byte)'g', sdata, sleng);
#endif
            return retval;
        }

    }
}

