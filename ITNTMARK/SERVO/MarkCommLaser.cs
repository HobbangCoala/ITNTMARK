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
using System.Runtime.InteropServices;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public class MarkCommLaser
    {
        public event MarkControllerStatusDataEventHandler MarkControllerDataArrivedEventFunc;

        private byte NAKError = 0;
        private byte[] RecvEventData = new byte[2048];
        private byte[] recvCommandData = new byte[2048];
        private int RecvEventLength = 0;
        private int RecvFrameLength = 0;
        private readonly object bufferLock = new object();
        private readonly object cmdLock = new object();

        //int cmdIndex = 0;

        volatile bool doingCommand = false;

        Thread statusThread;
        volatile bool doingThread = false;

        protected volatile byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected volatile byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;

        protected SerialPort Port = new SerialPort();
        protected RingBuffer cb;
        private readonly object comLock = new object();
        private object evtLock = new object();
        public bool IsOpen = false;
        byte[] bmovingcmd = new byte[64];
        public bool ErrorLaserSource = false;

        bool bLaserError = false;
        bool bMotorError = false;

        //string sErrorDevice = "LASER MARK CONTROLLER";
        string sDeviceName = DeviceName.Device_SERVO;
        string sDeviceCode = DeviceCode.Device_SERVO;

        public MarkCommLaser()
        {
            Port = new SerialPort();
            cb = new RingBuffer(0x2000);

            SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;

            string value = "";
            //Util.GetPrivateProfileValue("COMMAND", "MOVECOMMAND", "RrHJCMKUhjk@", ref value, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("COMMAND", "MOVECOMMAND", "RrHJCMKUhjk@", ref value, Constants.PARAMS_INI_FILE);
            Encoding.UTF8.GetBytes(value, 0, value.Length, bmovingcmd, 0);
        }

        ~MarkCommLaser()
        {
            if ((Port != null) && (Port.IsOpen))
                ClosePort();
        }

        public int OpenMarkDevice()
        {
            int retval = 0;
            string value = "";
            string portnum = "COM";
            int baud = 19200;

            Util.GetPrivateProfileValue("MARK", "PORT", "COM1", ref portnum, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("MARK", "BAUDRATE", "115200", ref value, Constants.PARAMS_INI_FILE);
            Int32.TryParse(value, out baud);

            retval = OpenDevice(portnum, baud, 8, Parity.None, StopBits.One);
            return retval;
        }

        public async Task<int> OpenMarkDeviceAsync()
        {
            int retval = 0;
            string value = "";
            string portnum = "COM";
            int baud = 19200;

            Util.GetPrivateProfileValue("MARK", "PORT", "COM1", ref portnum, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("MARK", "BAUDRATE", "115200", ref value, Constants.PARAMS_INI_FILE);
            Int32.TryParse(value, out baud);

            retval = await OpenDeviceAsync(portnum, baud, 8, Parity.None, StopBits.One);
            return retval;
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
            string className = "MarkCommLaser";// "MarkCommLaser";
            string funcName = "OpenDevice";// ;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;

#if TEST_DEBUG_MARK
            retval = 0;
            doingThread = true;
            statusThread = new Thread(ThreadStatusCheck2);
            statusThread.Start();

#else
            if ((Port != null) && Port.IsOpen)
                return 0;

            retval = OpenPort(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
            if (retval == 0)
            {
                //statusThread = new Thread(ThreadStatusCheck2);
                //statusThread.Start();
                //doingThread = true;
                Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                IsOpen = true;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<int> OpenDeviceAsync(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string className = "MarkCommLaser";
            string funcName = "OpenDeviceAsync";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;

#if TEST_DEBUG_MARK
            retval = 0;
#else
            if ((Port != null) && (Port.IsOpen))
                return 0;

            retval = await OpenPortAsync(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
            if (retval == 0)
            {
                //statusThread = new Thread(ThreadStatusCheck2);
                //statusThread.Start();
                //doingThread = true;
                Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
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
                Port.ReceivedBytesThreshold = 1;

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
                Port.ReceivedBytesThreshold = 1;

                if (Port.IsOpen)
                    return 0;

                Task task = Task.Run(() =>
                {
                    Port.Open();
                });

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

                // [FIX] 분할 수신 대비: ReceiveCommData 처리 중 추가 데이터가 도착했을 수 있으므로 재확인
                size = port.BytesToRead;
                while (size > 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "DataReceivedHandler", "RECV AFTER : " + size.ToString(), Thread.CurrentThread.ManagedThreadId);
                    readsize = port.Read(recv, 0, size);
                    cb.Put(recv, readsize);
                    size = port.BytesToRead;
                    totsize += readsize;
                    ReceiveCommData();
                    size = port.BytesToRead;
                }
            }
        }

        /// <summary>
        /// Close Port
        /// </summary>
        /// <returns></returns>
        public int CloseDevice()
        {
            string className = "MarkCommLaser";
            string funcName = "CloseDevice";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

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

            //lock (thisLock)
            //{
            //}
        }


        public void SetLaserErrorStatus(bool bError)
        {
            try
            {
                bLaserError = bError;
            }
            catch (Exception ex)
            {
            }
        }

        public void SetMotorErrorStatus(bool bError)
        {
            try
            {
                bMotorError = bError;
            }
            catch (Exception ex)
            {
            }
        }

        public bool GetLaserErrorStatus()
        {
            return bLaserError;
        }

        public bool GetMotorErrorStatus()
        {
            return bMotorError;
        }

        public void SetReadyErrorStatus(bool err)
        {
            //bReadyError = err;
        }

        public string[] GetSerialPorts()
        {
            string[] portNames = SerialPort.GetPortNames();
            return portNames;
        }

        /// <summary>
        /// 1. 수신 데이터 크기 확인
        /// 2. 수신 데이터 중 SOH 있는지 확인
        /// 3. SOH 보다 이전에 수신된 데이터는 삭제
        /// 4. SOH 다음 데이터 (전체 길이) 확인
        /// 
        /// 수신 데이터 분석 및 수신 버퍼에 저장
        /// </summary>
        protected void ReceiveCommData()
        {
            string className = "MarkCommLaser";
            string funcName = "ReceiveCommData";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString());

            int count = 0;
            //int retval = 0;
            int size = 0;
            int i = 0;
            byte tmp = 0;
            byte[] recv = new byte[0x2000];
            int idxSOH = -1;
            int idxCR = -1;
            byte[] blengs = new byte[2];
            int dataLength = 0;
            string sleng = "";

            //byte[] tmp = new byte[2];
            try
            {
                //count = cb.GetSize();
                //while (() > 7)
                while((count = cb.GetSize()) > 0)
                {
                    cb.Look(ref recv, count);
                    if (recv.Contains((byte)ASCII.SOH))
                    {
                        idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH, 0, count);
                        if (idxSOH >= 0)
                        {
                            if (idxSOH > 0)
                            {
                                for (i = 0; i < idxSOH; i++)
                                    cb.Get(ref tmp);
                                count = cb.GetSize();
                                continue;
                                //idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                            }

                            Array.Copy(recv, 1, blengs, 0, 2);
                            sleng = Encoding.UTF8.GetString(blengs, 0, 2);
                            try
                            {
                                dataLength = Convert.ToInt32(sleng, 16);
                            }
                            catch(Exception ex)
                            {
                                dataLength = 0;
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                            }

                            if (recv.Contains((byte)ASCII.CR))
                            {
                                //idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                                idxCR = Array.IndexOf(recv, (byte)ASCII.CR, idxSOH, count - idxSOH);//, idxSOH);
                                if (idxCR < 0)
                                    return;
                            }
                            else
                                return;

                            if (idxCR > idxSOH)
                            {
                                size = idxCR - idxSOH + 1;
                                //if (((recv[idxSOH + 4] >= '0') && (recv[idxSOH + 4] <= '9')) || (recv[idxSOH + 4] <= '@'))
                                if ((recv[idxSOH + 4] >= '0') && (recv[idxSOH + 4] <= '9'))
                                {
                                    size = cb.Look(ref RecvEventData, size);
                                    if (size < 18)
                                    {
                                        //ITNTTraceLog.Instance.Trace(0, "MarkComm::ReceiveCommData() SIZE={0}CR={1}", size, idxCR);
                                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SIZE={0}/CR={1}", size, idxCR), Thread.CurrentThread.ManagedThreadId);
                                        return;
                                    }
                                    size = cb.Get(ref RecvEventData, size);
                                    RecvEventLength = size;
                                    //ITNTTraceLog.Instance.Trace(2, "MarkComm::ReceiveCommData() CR={0} SO={1}", idxCR, idxSOH);
                                    ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("CR={0}/SO={1}", idxCR, idxSOH), Thread.CurrentThread.ManagedThreadId);

                                    if (recv[idxSOH + 4] == '8')
                                    {
                                        RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_S38;
                                        //markMutex.ReleaseMutex();
                                    }
                                    else if (recv[idxSOH + 4] == '0')
                                    {
                                        RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_S30;
                                        //markMutex.ReleaseMutex();
                                    }

                                    MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();
                                    arg.execmd = RecvEventData[idxSOH + 3];
                                    arg.stscmd = RecvEventData[idxSOH + 4];
                                    Array.Copy(RecvEventData, arg.receiveBuffer, RecvEventLength);
                                    arg.receiveSize = RecvEventLength;
                                    RecvEventData.Initialize();
                                    RecvEventLength = 0;
                                    idxCR = -1;
                                    idxSOH = -1;
                                    ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()  SEND MARK :  ", arg.receiveSize, arg.receiveBuffer);
                                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ST", Thread.CurrentThread.ManagedThreadId);
                                    OnMarkControllerStatusDataEventHandler(arg);
                                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ED", Thread.CurrentThread.ManagedThreadId);
                                }
                                else
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("2 CR={0}/SO={1}", idxCR, idxSOH), Thread.CurrentThread.ManagedThreadId);

                                    cb.Get(ref recvCommandData, size);
                                    RecvFrameLength = size;
                                    RecvEventLength = size;
                                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;

                                    if (recvCommandData[idxSOH + 3] == '@')
                                    {
                                        MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();
                                        arg.execmd = recvCommandData[idxSOH + 3];
                                        arg.stscmd = recvCommandData[idxSOH + 4];
                                        Array.Copy(recvCommandData, arg.receiveBuffer, RecvEventLength);
                                        arg.receiveSize = RecvEventLength;
                                        recvCommandData.Initialize();
                                        RecvEventLength = 0;
                                        idxCR = -1;
                                        idxSOH = -1;
                                        ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()  SEND MARK :  ", arg.receiveSize, arg.receiveBuffer);
                                        OnMarkControllerStatusDataEventHandler(arg);
                                    }

                                    idxCR = -1;
                                    idxSOH = -1;
                                }
                            }
                        }
                        else// if (idxSOH < 0)
                            return;
                    }
                    else
                    {
                        //Debug.WriteLine("!!!!");
                        ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOH NONE /" + count.ToString(), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    recv.Initialize();
                }
            }
            catch (Exception ex)
            {
                //IsEventFlag = false;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }


        public async Task<ITNTResponseArgs> SendCommandMsg(byte cmd, string func, byte[] sendData, int sendLength, int loglevel)//int loglevel, byte cmd, byte[] sendData, int sendLength)
        {
            string className = "MarkCommLaser";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsg";     // MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //int size = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            int count = 0;
            int i = 0;
            byte[] sendmsg = new byte[sendLength+16];
            int leng = 0;
            string strTmp = "";
            byte[] byTmp = new byte[2];
            byte bcc = 0;
            string sCurrentFunc = "SEND COMMAND";

            try
            {
                //leng = sendLength + 6;
                leng = sendLength / 4;
                strTmp = leng.ToString("X2");
                byTmp = Encoding.UTF8.GetBytes(strTmp);

                sendmsg[i++] = (byte)ASCII.SOH;
                sendmsg[i++] = byTmp[0];
                sendmsg[i++] = byTmp[1];
                sendmsg[i++] = cmd;
                sendmsg[i++] = (byte)ASCII.STX;
                Array.Copy(sendData, 0, sendmsg, i, sendLength);
                i += sendLength;
                sendmsg[i++] = (byte)ASCII.ETX;
                bcc = GetBCC(sendmsg, 1, i);
                strTmp = bcc.ToString("X2");
                byTmp = Encoding.UTF8.GetBytes(strTmp);
                //Encoding.UTF8.GetBytes(strLeng, 0, 2, byTmp, 2);

                sendmsg[i++] = byTmp[0];
                sendmsg[i++] = byTmp[1];
                sendmsg[i++] = (byte)ASCII.CR;

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                count = WritePort(sendmsg, 0, i);
                if (count <= 0)
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                    SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_ERR;

                    retval.execResult = (int)COMMUNICATIONERROR.ERR_SEND_DATA_FAIL;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_SEND_DATA_FAIL;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ")" + " : SEND FAIL";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");

                    return retval;
                }
                SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_END;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.execResult = (int)COMMUNICATIONERROR.ERR_EXECUTE_EXCEPTION;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendCommandMsgAsync(int loglevel, byte cmd, byte[] sendData, int sendLength)
        {
            string className = "ServoControll";
            string funcName = "SendCommandMsgAsync";     // MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(3, "{loglevel}::{1}()  {2}", className, funcName, "START : " + cmd.ToString());

            //int size = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            int count = 0;
            int i = 0;
            byte[] sendmsg = new byte[128];
            int leng = 0;
            string strTmp = "";
            byte[] byTmp = new byte[2];
            byte bcc = 0;
            string sCurrentFunc = "SEND COMMAND 2";
            try
            {
                leng = sendLength + 6;
                strTmp = leng.ToString("X2");
                byTmp = Encoding.UTF8.GetBytes(strTmp);

                sendmsg[i++] = (byte)ASCII.SOH;
                sendmsg[i++] = byTmp[0];
                sendmsg[i++] = byTmp[1];
                sendmsg[i++] = cmd;
                sendmsg[i++] = (byte)ASCII.STX;
                Array.Copy(sendData, 0, sendmsg, i, sendLength);
                i += sendLength;
                sendmsg[i++] = (byte)ASCII.ETX;

                bcc = GetBCC(sendmsg, 1, i - 1);
                strTmp = bcc.ToString("X2");

                Encoding.UTF8.GetBytes(strTmp, 0, 2, byTmp, 2);
                sendmsg[i++] = byTmp[0];
                sendmsg[i++] = byTmp[1];

                //sendmsg[i++] = GetBCC(sendmsg, 1, i - 1);
                sendmsg[i++] = (byte)ASCII.CR;

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                count = WritePort(sendmsg, 0, i);
                if (count <= 0)
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                    SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_ERR;
                    if(count < 0)
                    {
                        retval.execResult = count;
                        retval.errorInfo.devErrorInfo.execResult = count;
                    }
                    else
                    {
                        retval.execResult = (int)COMMUNICATIONERROR.ERR_SEND_DATA_FAIL;
                        retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_SEND_DATA_FAIL;
                    }
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd.ToString() + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd.ToString() + ")" + " : SEND FAIL";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");

                    return retval;
                }
                SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_END;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;//(int)COMMUNICATIONERROR.ERR_EXECUTE_EXCEPTION;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd.ToString() + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd.ToString() + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="loglevel"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<ITNTResponseArgs> RecvResponseMsg(byte cmd, string func, int loglevel, int timeout1 = 2, int timeout2 = 6)//int loglevel, byte cmd, int timeout1 = 2, int timeout2 = 6)
        {
            //int retval = 0;
            string className = "MarkCommLaser";
            string funcName = "RecvResponseMsg";
            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] respMsg = new byte[2048];
            //int respSize = 0;
            byte endStatus1 = 0;
            byte endStatus2 = 0;
            int timeout = timeout1;
            bool movingCmd = false;
            string sCurrentFunc = "RECEIVE RESPONSE";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + cmd.ToString(), Thread.CurrentThread.ManagedThreadId);

                endStatus1 = (byte)RECVFLAG.RECVFLAG_RECV_END;
                endStatus2 = (byte)RECVFLAG.RECVFLAG_RECV_S38;

                if (bmovingcmd.Contains<byte>(cmd))
                {
                    timeout = timeout2;
                    movingCmd = true;
                    //endStatus1 = (byte)RECVFLAG.RECVFLAG_RECV_END;
                    //endStatus2 = (byte)RECVFLAG.RECVFLAG_RECV_S38;
                }
                else
                {
                    timeout = timeout1;
                    //endStatus1 = (byte)RECVFLAG.RECVFLAG_RECV_END;
                    //endStatus2 = 0;
                }

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if ((RecvFlag == endStatus1) || (RecvFlag == endStatus2))
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();
                sw.Reset();
                if ((RecvFlag != endStatus1) && (RecvFlag != endStatus2))
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")"; 
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ")" + " : RECEIVE TIMEOUT";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");

                    return retval;
                }

                if(movingCmd == true)
                {
                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                    {
                        //if ((RecvFlag == endStatus1) || (RecvFlag == endStatus2))
                        if(RecvFlag == endStatus2)
                            break;

                        await Task.Delay(10);
                    }
                    sw.Stop();
                    sw.Reset();
                    //if ((RecvFlag != endStatus1) && (RecvFlag != endStatus2))
                    if(RecvFlag != endStatus2)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT 2", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT2;
                        retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT2;
                        retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                        retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                        retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ")" + " : RECEIVE TIMEOUT";
                        retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                        return retval;
                    }
                }

                Array.Copy(recvCommandData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK (SIZE = {0})", retval.recvSize), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> RecvResponseMsgAsync(byte cmd, string func, int loglevel, int timeout = 2)//int loglevel, byte cmd, int timeout = 2)
        {
            //int retval = 0;
            string className = "MarkCommLaser";
            string funcName = "RecvResponseMsgAsync";// ;
            Stopwatch sw = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] respMsg = new byte[2048];
            //int respSize = 0;
            byte endStatus1 = 0;
            byte endStatus2 = 0;
            string sCurrentFunc = "RECEIVE RESPONSE 2";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + cmd.ToString(), Thread.CurrentThread.ManagedThreadId);

                endStatus1 = (byte)RECVFLAG.RECVFLAG_RECV_END;
                endStatus2 = (byte)RECVFLAG.RECVFLAG_RECV_S38;

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    //if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                    if ((RecvFlag == endStatus1) || (RecvFlag == endStatus2))
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if ((RecvFlag != endStatus1) && (RecvFlag != endStatus2))
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ")" + " : RECEIVE TIMEOUT";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                //if (RecvFlag == endStatus2)
                //{
                //    Array.Copy(RecvCommandData, retval.recvBuffer, RecvFrameLength);
                //    retval.recvSize = RecvFrameLength;
                //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK (SIZE = {0})", retval.recvSize));
                //    return retval;
                //}
                //else if(RecvFlag == endStatus1)
                //{
                //    sw2.Start();
                //    while (sw2.Elapsed < TimeSpan.FromSeconds(timeout))
                //    {
                //        //if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                //        if (RecvFlag == endStatus2)
                //            break;

                //        await Task.Delay(10);
                //    }
                //    sw2.Stop();

                //    if (RecvFlag != endStatus2)
                //    {
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT");
                //        retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                //        return retval;
                //    }
                //}
                //else
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT");
                //    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                //    return retval;
                //}

                ////if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
                //if ((RecvFlag != endStatus1) && (RecvFlag != endStatus2))
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT");
                //    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                //    return retval;
                //}

                Array.Copy(recvCommandData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK (SIZE = {0})", retval.recvSize), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> RecvResponse4Marking(byte cmd, string func, byte lastcmd, int loglevel, int timeout1 = 2, int timeout2 = 6)// loglevel, byte cmd, byte lastcmd, int timeout1 = 2, int timeout2 = 6)
        {
            //int retval = 0;
            string className = "MarkCommLaser";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "RecvResponse4Marking";// MethodBase.GetCurrentMethod().Name;
            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] respMsg = new byte[2048];
            //int respSize = 0;
            byte endStatus1 = 0;
            byte endStatus2 = 0;
            int timeout = timeout1;
            bool movingCmd = false;
            byte[] status = new byte[4];
            int istatus = 0;
            string sCurrentFunc = "RECEIVE MARKING";

            try
            {
                //ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + cmd.ToString(), Thread.CurrentThread.ManagedThreadId);

                endStatus1 = (byte)RECVFLAG.RECVFLAG_RECV_END;
                endStatus2 = (byte)RECVFLAG.RECVFLAG_RECV_S30;

                if (lastcmd != 0)
                {
                    timeout = timeout2;
                    movingCmd = true;
                }
                else
                {
                    timeout = timeout1;
                }

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout1))
                {
                    if (RecvFlag == endStatus1)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();
                sw.Reset();
                if (RecvFlag != endStatus1)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ")" + " : RECEIVE TIMEOUT";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                if (movingCmd == true)
                {
                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                    {
                        //if ((RecvFlag == endStatus1) || (RecvFlag == endStatus2))
                        if (RecvFlag == endStatus2)
                            break;

                        await Task.Delay(10);
                    }
                    sw.Stop();
                    sw.Reset();
                    //if ((RecvFlag != endStatus1) && (RecvFlag != endStatus2))
                    if (RecvFlag != endStatus2)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT2", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT2;
                        retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT2;
                        retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                        retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                        retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ")" + " : RECEIVE TIMEOUT 2";
                        retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                        return retval;
                    }
                }

                Array.Copy(recvCommandData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MARK(" + retval.recvSize.ToString() + "): " + retval.recvString, Thread.CurrentThread.ManagedThreadId);

                if (retval.recvBuffer[4]== (byte)ASCII.ACK)
                {
                    Array.Copy(retval.recvBuffer, 6, status, 0, 4);
                    string tmp = Encoding.UTF8.GetString(status, 0, 4);
                    istatus = Convert.ToInt32(tmp, 16);

                    //if(BitConverter.IsLittleEndian)
                    //    Array.Reverse(status);
                    //istatus = BitConverter.ToInt32(tmp);

                    if ((istatus & 0x8000) != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MARKING LASER ERROR!!!! - " + istatus.ToString(), Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -0x11;
                        retval.errorInfo.sErrorMessage = "MARKING LASER ERROR!!!! - " + istatus.ToString();
                        return retval;
                    }

                    if ((istatus & 0x07) != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MARKING MOTOR ERROR!!!! - " + istatus.ToString(), Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -0x12;
                        retval.errorInfo.sErrorMessage = "MARKING MOTOR ERROR!!!! - " + istatus.ToString();
                        return retval;
                    }
                }
                else if(retval.recvBuffer[4]== (byte)ASCII.NAK)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MARKING RECV NAK ERROR!!!!", Thread.CurrentThread.ManagedThreadId);
                    //retval.execResult = -0x15;
                    //retval.errorInfo.sErrorMessage = "MARKING RECV NAK ERROR!!!!";
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_NAK;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_NAK;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ")" + " : RECEIVE NAK";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }
                //ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK (SIZE = {0})", retval.recvSize), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            return retval;
        }

        //public async Task<ITNTResponseArgs> RecvResponseMsgAsync(int loglevel, int timeout = 2)
        //{
        //    //int retval = 0;
        //    string className = "MarkCommLaser";
        //    string funcName = "RecvResponseMsgAsync";// MethodBase.GetCurrentMethod().Name;
        //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //    Stopwatch sw = new Stopwatch();
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    byte[] respMsg = new byte[2048];
        //    //int respSize = 0;
        //    byte endStatus1 = 0;
        //    byte endStatus2 = 0;

        //    try
        //    {
        //        endStatus1 = (byte)RECVFLAG.RECVFLAG_RECV_END;
        //        endStatus2 = (byte)RECVFLAG.RECVFLAG_RECV_S38;

        //        sw.Start();
        //        while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
        //        {
        //            //if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
        //            if ((RecvFlag == endStatus1) || (RecvFlag == endStatus2))
        //                break;

        //            await Task.Delay(10);
        //        }
        //        sw.Stop();

        //        //if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
        //        if ((RecvFlag != endStatus1) && (RecvFlag != endStatus2))
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
        //            return retval;
        //        }

        //        Array.Copy(RecvCommandData, retval.recvBuffer, RecvFrameLength);
        //        retval.recvSize = RecvFrameLength;
        //        ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK (SIZE = {0})", retval.recvSize), Thread.CurrentThread.ManagedThreadId);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //        return retval;
        //    }
        //    return retval;
        //}


        protected int WritePort(byte[] buffer, int offset, int count)
        {
            if (!Port.IsOpen)
                return (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
            lock (comLock)
            {
                Port.Write(buffer, offset, count);
            }
            if (Port.BytesToWrite > 0)
            {
                return (int)COMMUNICATIONERROR.ERR_SEND_DATA_FAIL;
            }
            return count;
        }




        public async Task<ITNTResponseArgs> ExecuteCommandFunc(byte cmd, string func, byte[] sendData, int sendLength, int loglevel, int timeoutsec)
        {
            string className = "MarkCommLaser";
            string funcName = "ExecuteCommandFunc";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "EXECUTE FUNCTION";
            //int retrycount = 0;
            //Stopwatch sw = new Stopwatch();

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START - {0}", cmd), Thread.CurrentThread.ManagedThreadId);

                retval = await ExecuteCommandMsg(cmd, sCurrentFunc, sendData, sendLength, loglevel, timeoutsec);
                if(retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR)
                {
                    retval = await ExecuteCommandMsg(cmd, sCurrentFunc, sendData, sendLength, loglevel, timeoutsec);
                    if (retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR)
                    {
                        retval = await ExecuteCommandMsg(cmd, sCurrentFunc, sendData, sendLength, loglevel, timeoutsec);
                        if (retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR)
                        {
                            return retval;
                        }
                    }
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.execResult = ex.HResult;
                doingCommand = false;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ExecuteCommandFuncAsync(byte cmd, string func, byte[] sendData, int sendLength, int loglevel, int timeoutsec)//int loglevel, int timeoutsec, byte type, byte[] sendData, int sendLength)
        {
            string className = "MarkCommLaser";// "MarkCommLaser";
            string funcName = "ExecuteCommandFuncAsync";// ;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //int retrycount = 0;
            //Stopwatch sw = new Stopwatch();
            string sCurrentFunc = "EXECUTE FUNCTION 2";

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START - {0}", cmd), Thread.CurrentThread.ManagedThreadId);

                retval = await ExecuteCommandMsgAsync(cmd, sCurrentFunc, sendData, sendLength, loglevel, timeoutsec);
                if (retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR)
                {
                    retval = await ExecuteCommandMsgAsync(cmd, sCurrentFunc, sendData, sendLength, loglevel, timeoutsec);
                    if (retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR)
                    {
                        retval = await ExecuteCommandMsgAsync(cmd, sCurrentFunc, sendData, sendLength, loglevel, timeoutsec);
                        if (retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR)
                        {
                            return retval;
                        }
                    }
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.execResult = ex.HResult;
                doingCommand = false;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }








        public async Task<ITNTResponseArgs> ExecuteCommandMsg(byte cmd, string func, byte[] sendData, int sendLength, int loglevel, int timeoutsec)
        {
            string className = "MarkCommLaser";
            string funcName = "ExecuteCommandMsg";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //int retrycount = 0;
            Stopwatch sw = new Stopwatch();
            string sCurrentFunc = "EXECUTE COMMAND";

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START - {0}", cmd), Thread.CurrentThread.ManagedThreadId);

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
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") ERROR = DEVICE IS BUSY";
                    //retval.errorInfo.devErrorInfo.sErrorMessage = "DEVICE IS BUSY (" + func + ")";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                if (Port.IsOpen == false)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") ERROR = DEVICE IS NOT CONNECTED";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    doingCommand = false;
                    return retval;
                }

                doingCommand = true;

                InitializeExecuteCommand();
                retval = await SendCommandMsg(cmd, sCurrentFunc, sendData, sendLength, loglevel);
                if (retval.execResult != 0)
                {
                    doingCommand = false;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendCommandMsg ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                retval = await RecvResponseMsg(cmd, sCurrentFunc, loglevel, timeoutsec);
                if (retval.execResult != 0)
                {
                    doingCommand = false;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RecvResponseMsg ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                doingCommand = false;
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END - {0}", cmd), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.execResult = ex.HResult;
                doingCommand = false;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(byte cmd, string func, byte[] sendData, int sendLength, int loglevel, int timeoutsec)//int loglevel, int timeoutsec, byte type, byte[] sendData, int sendLength)
        {
            string className = "MarkCommLaser";// "MarkCommLaser";
            string funcName = "ExecuteCommandMsg";// ;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //int retrycount = 0;
            Stopwatch sw = new Stopwatch();
            string sCurrentFunc = "EXECUTE COMMAND 2";

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START - {0}", cmd), Thread.CurrentThread.ManagedThreadId);

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
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") ERROR = DEVICE IS BUSY";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                if (Port.IsOpen == false)
                {
                    doingCommand = false;
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") ERROR = DEVICE IS NOT CONNECTED";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                //markMutex.WaitOne(10000);
                doingCommand = true;

                //lock (cmdLock)
                {
                    //for (retrycount = 0; retrycount < 3; retrycount++)
                    {
                        InitializeExecuteCommand();
                        retval = await SendCommandMsg(cmd, sCurrentFunc, sendData, sendLength, loglevel);
                        if (retval.execResult != 0)
                        {
                            doingCommand = false;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendCommandMsg ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            return retval;
                            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval.execResult, retrycount), Thread.CurrentThread.ManagedThreadId);
                            //continue;
                        }
                        retval = await RecvResponseMsgAsync(cmd, sCurrentFunc, loglevel, timeoutsec);
                        if (retval.execResult != 0)
                        //    break;
                        //else if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
                        //{
                        //    lock (bufferLock)
                        //    {
                        //        cb.Clear();
                        //    }
                        //}
                        //else
                        {
                            doingCommand = false;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RecvResponseMsg ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            return retval;
                        }
                    }

                    //if (retval.execResult != 0)
                    //{
                    //    lock (bufferLock)
                    //    {
                    //        cb.Clear();
                    //    }
                    //}
                    doingCommand = false;
                    ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END - {0}", cmd), Thread.CurrentThread.ManagedThreadId);
                    ////R r H J C M K U h j k
                    //if ((type != (byte)'R') && (type != (byte)'r') && (type != (byte)'H') &&
                    //    (type != (byte)'J') && (type != (byte)'C') && (type != (byte)'M') &&
                    //    (type != (byte)'K') && (type != (byte)'U') && (type != (byte)'h') &&
                    //    (type != (byte)'j') && (type != (byte)'k')) //R r H J C M K U h j k )
                    //{
                    //    //markMutex.ReleaseMutex();
                    //}
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCommand = false;
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ExecuteCommand4Marking(byte cmd, string func, byte lastcmd, byte[] sendData, int sendLength, int loglevel, int timeoutsec, int timeoutsec2)
        {
            string className = "MarkCommLaser";
            string funcName = "ExecuteCommandMsg";
            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START - {0}", cmd), Thread.CurrentThread.ManagedThreadId);

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            Stopwatch sw = new Stopwatch();
            string sCurrentFunc = "EXECUTE MARKING";

            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeoutsec2))
                {
                    if (doingCommand == false)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (doingCommand == true)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") ERROR = DEVICE IS BUSY";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                if (Port.IsOpen == false)
                {
                    doingCommand = false;
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + func + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") ERROR = DEVICE IS NOT CONNECTED";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                //markMutex.WaitOne(10000);
                doingCommand = true;

                //lock (cmdLock)
                {
                    for (retrycount = 0; retrycount < 2; retrycount++)
                    {
                        InitializeExecuteCommand();
                        retval = await SendCommandMsg(cmd, sCurrentFunc, sendData, sendLength, loglevel);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval.execResult, retrycount), Thread.CurrentThread.ManagedThreadId);
                            continue;
                        }
                        retval = await RecvResponse4Marking(cmd, sCurrentFunc, lastcmd, loglevel, timeoutsec, timeoutsec2);
                        if (retval.execResult == 0)
                            break;
                        else if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
                        {
                            lock (bufferLock)
                            {
                                cb.Clear();
                            }
                        }
                        else
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval.execResult, retrycount), Thread.CurrentThread.ManagedThreadId);
                    }
                    if (retval.execResult != 0)
                    {
                        lock (bufferLock)
                        {
                            cb.Clear();
                        }
                    }
                    doingCommand = false;
                    ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END - {0}", cmd), Thread.CurrentThread.ManagedThreadId);
                    ////R r H J C M K U h j k
                    //if ((type != (byte)'R') && (type != (byte)'r') && (type != (byte)'H') &&
                    //    (type != (byte)'J') && (type != (byte)'C') && (type != (byte)'M') &&
                    //    (type != (byte)'K') && (type != (byte)'U') && (type != (byte)'h') &&
                    //    (type != (byte)'j') && (type != (byte)'k')) //R r H J C M K U h j k )
                    //{
                    //    //markMutex.ReleaseMutex();
                    //}
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                doingCommand = false;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = func;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + func + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
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
            recvCommandData.Initialize();
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

        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ITNTResponseArgs> GetCurrentSetting()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
            //GetMarkStatus();
#else
            //retval = await ExecuteCommandFunc(0, 5, (byte)'0', sdata, sleng);
#endif
            return retval;
        }


        //        public async void ThreadStatusCheck()
        //        {
        //            int retval = 0;
        //            while(doingThread)
        //            {
        //#if TEST_DEBUG_MARK
        //#else
        //                if ((Port == null) || (!Port.IsOpen))
        //                {
        //                    await Task.Delay(100);
        //                    continue;
        //                }
        //#endif
        //                await GetCurrentSetting();
        //                await Task.Delay(3000);
        //            }
        //        }

        public async void ThreadStatusCheck2()
        {
            int retval = 0;
            ITNTResponseArgs recv = new ITNTResponseArgs();
            MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();

            while (doingThread)
            {
                //if ((Port == null) || (!Port.IsOpen))
                //{
                //    await Task.Delay(100);
                //    continue;
                //}

                recv = await GetStatus();
                if (recv.execResult == 0)
                {
                    arg.receiveSize = recv.recvSize;
                    arg.execmd = recv.exeCmd;
                    arg.stscmd = recv.stsCmd;
                    Array.Copy(recv.recvBuffer, arg.receiveBuffer, recv.recvSize);
                    OnMarkControllerStatusDataEventHandler(arg);
                }

                //await SendVIN();
                await Task.Delay(2000);
            }
        }

        public async Task<ITNTResponseArgs> GetStatus()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();

#if TEST_DEBUG_MARK
            string value = "";
            int use = 0;
            Util.GetPrivateProfileValue("MARKER", "USE", "0", ref value, "TEST.ini");
            int.TryParse(value, out use);
            //            int use2 = (int)Util.GetPrivateProfileValueUINT("MARKER", "USE", 0, "TEST.ini");
            if (use == 0)
            {
                Util.GetPrivateProfileValue("MARKER", "USE", "0", ref value, "TEST.ini");
                retval.execResult = -1;
                return retval;
            }

            Util.GetPrivateProfileValue("MARKER", "EXECOMMAND", "R", ref value, "TEST.ini");
            byte.TryParse(value, out retval.exeCmd);

            Util.GetPrivateProfileValue("MARKER", "STSCOMMAND", "9", ref value, "TEST.ini");
            byte.TryParse(value, out retval.stsCmd);

            Util.GetPrivateProfileValue("MARKER", "MARKSTATUS", "012345000000000000", ref value, "TEST.ini");

            retval.recvString = value;
            byte[] test = Encoding.UTF8.GetBytes(value);
            Array.Copy(test, retval.recvBuffer, test.Length);
            retval.recvSize = test.Length;

            //ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()  SEND MARK :  ", arg.receiveSize, arg.receiveBuffer);
#endif
            //byte[] sendData = new byte[16];
            //retval = await ExecuteCommandFunc(2, 5, 0x53, sendData, 0);
            return retval;
        }

        private void OnMarkControllerStatusDataEventHandler(MarkControllerRecievedEvnetArgs e)
        {
            MarkControllerDataArrivedEventFunc?.Invoke(this, e);
        }

//        public async Task<ITNTResponseArgs> LoadFontData(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(2, 5, (byte)'D', sdata, sleng);
//#endif
//            return retval;
//        }


//        public async Task<ITNTResponseArgs> Opmode(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'A', sdata, sleng);
//#endif
//            return retval;
//        }

//        public async Task<ITNTResponseArgs> StrikeNo(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'N', sdata, sleng);
//#endif
//            return retval;
//        }

        //        public async Task<ITNTResponseArgs> RunStart(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandFunc(0, 5, (byte)'R', sdata, sleng);
        //#endif
        //            return retval;
        //        }


//        public async Task<ITNTResponseArgs> SolOnOffTime(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'S', sdata, sleng);
//#endif
//            return retval;
//        }

        //public async Task<ITNTResponseArgs> FreeSpeed(string cmd, string Speed)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandFunc(0, 20, cmd, Speed);
        //    return retval;
        //}

        //        public async Task<ITNTResponseArgs> LoadSpeed(byte[]sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandFunc(0, 5, (byte)'L', sdata, sleng);
        //#endif
        //            return retval;
        //        }

        //public async Task<ITNTResponseArgs> Resume(string cmd, string R_value)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandFunc(0, 20, cmd, R_value);
        //    return retval;
        //}

        //        public async Task<ITNTResponseArgs> GoHome(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandFunc(1, 5, (byte)'H', sdata, sleng);
        //#endif
        //            return retval;
        //        }

//        public async Task<ITNTResponseArgs> Jog_XY(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'J', sdata, sleng);
//#endif
//            return retval;
//        }

        public async Task<ITNTResponseArgs> MoveHead(short xval, short yval, short zval)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            string sCurrentFunc = "MOVE HEAD";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "MoveHead", "START", Thread.CurrentThread.ManagedThreadId);
                sstrring = xval.ToString("X4") + yval.ToString("X4") + zval.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'J', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "MoveHead", "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }


//        public async Task<ITNTResponseArgs> TestSolFet(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'O',  sdata, sleng);
//#endif
//            return retval;
//        }


//        public async Task<ITNTResponseArgs> TestBox4(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'C', sdata, sleng);
//#endif
//            return retval;
//        }

        //        public async Task<ITNTResponseArgs> GoPoint(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandFunc(0, 5, (byte)'M', sdata, sleng);
        //#endif
        //            return retval;
        //        }

        //        public async Task<ITNTResponseArgs> GoParking(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandFunc(0, 5, (byte)'K', sdata, sleng);
        //#endif
        //            return retval;
        //        }

//        public async Task<ITNTResponseArgs> GearRatio(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'G', sdata, sleng);
//#endif
//            return retval;
//        }

//        public async Task<ITNTResponseArgs> SetMaxMinXY(byte[] sdata, int sleng)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//#if TEST_DEBUG_MARK
//#else
//            retval = await ExecuteCommandFunc(1, 5, (byte)'X', sdata, sleng);
//#endif
//            return retval;
//        }

        public async Task<ITNTResponseArgs> FontFlush()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[16];
            int sleng = 0;
            string sCurrentFunc = "FONT FLUSH"; 
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandFunc((byte)'B', sCurrentFunc, sdata, sleng, 1, 2);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> Inport()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[16];
            int sleng = 0;
            string sCurrentFunc = "INPORT";
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandFunc((byte)'I', sCurrentFunc, sdata, sleng, 1, 2);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> ScanJog(byte direction, short resolution, double scanstpelength)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string value = "";
            //double stepLength = 0.0d;
            short srtValue = 0;
            string sstrring = "";
            string sCurrentFunc = "SCAN JOG";

            try
            {
                if (direction == 0)
                    srtValue = (short)(-(resolution * scanstpelength));
                else
                    srtValue = (short)(resolution * scanstpelength);
                sstrring = srtValue.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'j', sCurrentFunc, sdata, sdata.Length, 1, 2);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }

//        public async Task<ITNTResponseArgs> Profile_Speed(byte flag, short ispeed, short tspeed, short acspeed, short despeed)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//            byte[] sdata = new byte[64];
//            string sstrring = "";
//            byte cmd = 0;
//            try
//            {
//                if (flag == 0)
//                    cmd = (byte)'l';
//                else
//                    cmd = (byte)'f';

//                sstrring = ispeed.ToString("X4") + tspeed.ToString("X4") + acspeed.ToString("X4") + despeed.ToString("X4");
//                sdata = Encoding.UTF8.GetBytes(sstrring);
//#if TEST_DEBUG_MARK
//#else
//                retval = await ExecuteCommandFunc(1, 2, cmd, sdata, sdata.Length);
//#endif
//                return retval;
//            }
//            catch (Exception ex)
//            {
//                retval.execResult = ex.HResult;
//                return retval;
//            }
//        }


        public async Task<ITNTResponseArgs> MoveScan2Home(short homeposition)
        {
            string className = "MarkCommLaser";
            string funcName = "MoveScan2Home";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            string sCurrentFunc = "SCAN HOME";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                sstrring = homeposition.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'h', sCurrentFunc, sdata, sdata.Length, 0, 3);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ScanProfile(short length, int timeout)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            string sCurrentFunc = "SCAN PROFILE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "ScanProfile", "START", Thread.CurrentThread.ManagedThreadId);
                sstrring = length.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'U', sCurrentFunc, sdata, sdata.Length, 0, timeout);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "ScanProfile", "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }

        //        public async Task<ITNTResponseArgs> MoveScanProfile(short position, short steplength)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //            byte[] sdata = new byte[64];
        //            string sstrring = "";
        //            try
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "MoveScanProfile", "START", Thread.CurrentThread.ManagedThreadId);
        //                sstrring = ((short)(position * steplength)).ToString("X4");
        //                sdata = Encoding.UTF8.GetBytes(sstrring);
        //#if TEST_DEBUG_MARK
        //#else
        //                retval = await ExecuteCommandFunc(0, 5, (byte)'k', sdata, sdata.Length);
        //#endif
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "MoveScanProfile", "END", Thread.CurrentThread.ManagedThreadId);
        //                return retval;
        //            }
        //            catch (Exception ex)
        //            {
        //                retval.execResult = ex.HResult;
        //                return retval;
        //            }
        //        }

        public async Task<ITNTResponseArgs> MoveScanProfile(short position, int timeout)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            string sCurrentFunc = "MOVE PROFILE";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "MoveScanProfile", "START", Thread.CurrentThread.ManagedThreadId);
                sstrring = position.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'k', sCurrentFunc, sdata, sdata.Length, 0, timeout);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "MoveScanProfile", "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> TestSolFet(short Fet, bool Sol, bool Igno=false)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            short zero = 0;
            short one = 1;
            string sCurrentFunc = "TEST SOL FET";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "TestSolFet", "START", Thread.CurrentThread.ManagedThreadId);
                if (Sol)
                    sendstring = Fet.ToString("X4") + one.ToString("X4");
                else
                    sendstring = Fet.ToString("X4") + zero.ToString("X4");

                if (Igno)
                    sendstring += one.ToString("X4");
                else
                    sendstring += zero.ToString("X4");

                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                //retval = await ExecuteCommandFunc(0, 20, (byte)'O', sdata, sdata.Length);
				retval = await ExecuteCommandFunc((byte)'O', sCurrentFunc, sdata, sdata.Length, 0, 3);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "TestSolFet", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoHome(short posX, short posY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "GO HOME";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoHome", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'H', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoHome", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoHome(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "GO HOME";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoHome", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + posZ.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'H', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoHome", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoHomeAll(short posX, short posY, short posU)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "GO HOME ALL";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoHomeAll", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + posU.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'E', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoHomeAll", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParking(short posX, short posY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "GO PARKING";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoParking", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'K', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoParking", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParking(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string className = "MarkCommLaser";
            string funcName = "GoParking";
            string sCurrentFunc = "GO PARKING";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + posZ.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'K',sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Btn_Start_Click = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParkingAsync(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string className = "MarkCommLaser";
            string funcName = "GoParkingAsync";
            string sCurrentFunc = "GO PARKING 2";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + posZ.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFuncAsync((byte)'K', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Btn_Start_Click = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> RunStart(short count)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "RUN START";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "RunStart", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = count.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'R', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "RunStart", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            return retval;
        }

        //        public async Task<ITNTResponseArgs> RunStart_S(string markpoint)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //            byte[] sdata = new byte[64];
        //            string sendstring = "";
        //            try
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "RunStart", "START", Thread.CurrentThread.ManagedThreadId);
        //                sendstring = markpoint;
        //                sdata = Encoding.UTF8.GetBytes(sendstring);
        //#if TEST_DEBUG_MARK
        //#else
        //                retval = await ExecuteCommandFunc(0, 5, (byte)'@', sdata, sdata.Length);
        //#endif
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "RunStart", "END", Thread.CurrentThread.ManagedThreadId);
        //            }
        //            catch (Exception ex)
        //            {
        //                retval.execResult = ex.HResult;
        //            }
        //            return retval;
        //        }

        public async Task<ITNTResponseArgs> RunStart_S(string markdata, bool cleanFireFlag, byte loglevel)
        {
            string className = "MarkCommLaser";
            string funcName = "RunStart_S";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            //string sendstring = "";
            //bool runflag = false;
            //int count = 0;
            string teststring = "";
            int markcount = 0;
            string sCurrentFunc = "MARK RUN";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

#if TEST_DEBUG_MARK
#else
                markcount = 1;// (cleanFireFlag == false) ? markinfo.senddata.sendDataFire.Count : markinfo.senddata.sendDataClean.Count;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COUNT = " + markcount.ToString(), Thread.CurrentThread.ManagedThreadId);

                if(markdata.Length >= 5)
                {
                    teststring = markdata.Substring(0, 5);
                    if (teststring == "00000")
                    {
                        retval.execResult = -1;
                        return retval;
                    }
                }

                if(markdata.Length >= 10)
                {
                    teststring = markdata.Substring(5, 10);
                    if (teststring == "00000")
                    {
                        retval.execResult = -2;
                        return retval;
                    }
                }

                sdata = Encoding.UTF8.GetBytes(markdata);
                retval = await ExecuteCommand4Marking((byte)'@', sCurrentFunc, 1, sdata, sdata.Length, loglevel, 4, 5);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MARK ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> RunStart_S(MarkVINInformEx markinfo, bool cleanFireFlag, byte loglevel)
        {
            string className = "MarkCommLaser";
            string funcName = "RunStart_S";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            //bool runflag = false;
            //int count = 0;
            string teststring = "";
            int markcount = 0;
            string sCurrentFunc = "MARK RUN";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - COUNT = " + markcount .ToString(), Thread.CurrentThread.ManagedThreadId);

                markcount = (cleanFireFlag == false) ? markinfo.senddata.sendDataFire.Count : markinfo.senddata.sendDataClean.Count;

                for (int i = 0; i < markcount; i++)
                {
                    //if (((MainWindow)System.Windows.Application.Current.MainWindow).GetLaserStatus() != "0")
                    if(bLaserError == true)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LASER ERROR !!!!", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -17;
                        retval.errorInfo.sErrorMessage = "LASER ERROR";
                        return retval;
                    }

                    //if (((MainWindow)System.Windows.Application.Current.MainWindow).GetMotorStatus() != "0")
                    if(bMotorError == true)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MOTOR ERROR !!!!", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -18;
                        retval.errorInfo.sErrorMessage = "MOTOR ERROR";
                        return retval;
                    }

                    //sendstring = (cleanFireFlag == false) ? markinfo.senddata.sendDataFire.ElementAt(count++) : markinfo.senddata.sendDataClean.ElementAt(count++);
                    sendstring = (cleanFireFlag == false) ? markinfo.senddata.sendDataFire.ElementAt(i) : markinfo.senddata.sendDataClean.ElementAt(i);
                    teststring = sendstring.Substring(0, 5);
                    if (teststring == "00000")
                    {
                        retval.execResult = -1;
                        return retval;
                    }

                    teststring = sendstring.Substring(5, 10);
                    if (teststring == "00000")
                    {
                        retval.execResult = -2;
                        return retval;
                    }

                    sdata = Encoding.UTF8.GetBytes(sendstring);
                    //retval = await ExecuteCommandAsync(0, 5, (byte)'@', sdata, sdata.Length);
#if TEST_DEBUG_MARK
                    retval.execResult = 0;
                    Task.Delay(300);
#else

                    if (i == (markcount - 1))
                        retval = await ExecuteCommand4Marking((byte)'@', sCurrentFunc, 1, sdata, sdata.Length, loglevel, 4, 5);
                    else
                        retval = await ExecuteCommand4Marking((byte)'@', sCurrentFunc, 0, sdata, sdata.Length, loglevel, 4, 5);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MARK ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        return retval;
                    }
#endif
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SetWorkArea(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "SetWorkArea";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "SetWorkArea", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + posZ.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'W', sCurrentFunc, sdata, sdata.Length, 0, 1);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "SetWorkArea", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> Move2LimitXY(short maxX, short maxY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "Move2LimitXY";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "Move2LimitXY", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = maxX.ToString("X4") + maxY.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'W', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "Move2LimitXY", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> Move2LimitU(short maxu)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "Move2LimitU";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "Move2LimitU", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = maxu.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'w', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "Move2LimitU", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            return retval;
        }
        public async Task<ITNTResponseArgs> GoPoint(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "GoPoint";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoPoint", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + posZ.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'M', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoPoint", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> GoPoint(int posX, int posY, int posZ, int pos)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            string sCurrentFunc = "GoPoint";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoPoint", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + posZ.ToString("X4") + pos.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'M', sCurrentFunc, sdata, sdata.Length, 0, 5);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GoPoint", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> LoadSpeed(byte cmd, short initSpeed, short targetSpeed, short accel, short decel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "LoadSpeed";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "LoadSpeed", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = initSpeed.ToString("X4") + targetSpeed.ToString("X4") + accel.ToString("X4") + decel.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc(cmd, sCurrentFunc, sdata, sdata.Length, 0, 1);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "LoadSpeed", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> LoadFontData(short chidx, short ptidx, short posX, short poxY, short flag)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "LoadFontData";
            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "LoadFontData", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = chidx.ToString("X4") + ptidx.ToString("X4") + posX.ToString("X4") + poxY.ToString("X4") + flag.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'D', sCurrentFunc, sdata, sdata.Length, 0, 1);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "LoadFontData", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> LoadFontData(short chidx, short ptidx, int posX, int poxY, int posZ, short flag)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "LoadFontData";
            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "LoadFontData", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = chidx.ToString("X2") + ptidx.ToString("X2") + posX.ToString("X4") + poxY.ToString("X4") + posZ.ToString("X4") + flag.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'D', sCurrentFunc, sdata, sdata.Length, 0, 1);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "LoadFontData", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SolOnOffTime(short solontime, short solofftime)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "SolOnOffTime";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "SolOnOffTime", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = solontime.ToString("X4") + solofftime.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'S', sCurrentFunc, sdata, sdata.Length, 0, 1);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "SolOnOffTime", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> StrikeNo(short count)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "StrikeNo";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "StrikeNo", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = count.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'N', sCurrentFunc, sdata, sdata.Length, 0, 1);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "StrikeNo", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetDensity(short density)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "SetDensity";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "StrikeNo", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = density.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'N', sCurrentFunc, sdata, sendstring.Length, 0, 1);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "StrikeNo", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<ITNTResponseArgs> dwellTimeSet(short time)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "dwellTimeSet";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "dwellTimeSet", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = time.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'d', sCurrentFunc, sdata, sdata.Length, 0, 1);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "dwellTimeSet", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<string> GetFWVersion()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string ver = "";
            string sCurrentFunc = "GetFWVersion";
            //string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GetFWVersion", "START", Thread.CurrentThread.ManagedThreadId);
                //sendstring = time.ToString("X4");
                //sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'V', sCurrentFunc, sdata, 0, 0, 1);
#endif
                ver = Encoding.UTF8.GetString(retval.recvBuffer, 8, 2);
                int iver = Convert.ToInt32(ver, 16);
                ver = iver.ToString("D3");
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GetFWVersion", "END", Thread.CurrentThread.ManagedThreadId);
            return ver;
        }

        public async Task<ITNTResponseArgs> GetFWVersion2()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sCurrentFunc = "GetFWVersion2";
            //string ver = "";
            //string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GetFWVersion", "START", Thread.CurrentThread.ManagedThreadId);
                //sendstring = time.ToString("X4");
                //sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'V', sCurrentFunc, sdata, 0, 0, 1);
#endif
                //ver = Encoding.UTF8.GetString(retval.recvBuffer, 8, 2);
                //int iver = Convert.ToInt32(ver, 16);
                //ver = iver.ToString("D3");
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "GetFWVersion", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<ITNTResponseArgs> Test4RealMarkArea()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sCurrentFunc = "Test4RealMarkArea";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "Test4PinMark", "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'C', sCurrentFunc, sdata, 0, 0, 5);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "Test4PinMark", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        ///////////// Set Phase Compensation value /////////////////////
        [StructLayout(LayoutKind.Explicit)]
        struct PhaseStruct
        {
            [FieldOffset(0)] public Single phase;
            [FieldOffset(0)] public UInt16 phaseLow;
            [FieldOffset(2)] public UInt16 phaseHigh;

            public PhaseStruct(Single data) : this()
            {
                phase = data;
            }
        }

        public async Task<ITNTResponseArgs> SetPhaseComp(Single p_value)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            string sCurrentFunc = "SetPhaseComp";

            try
            {
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "SetPhaseComp", "START", Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "SetPhaseComp", "Value = " + p_value.ToString(), Thread.CurrentThread.ManagedThreadId);

                PhaseStruct p = new PhaseStruct(p_value);
                sendstring = p.phaseLow.ToString("X4") + p.phaseHigh.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandFunc((byte)'#', sCurrentFunc, sdata, sendstring.Length, 0, 2);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkCommLaser", "Test4PinMark", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            return retval;
        }
    }
}
