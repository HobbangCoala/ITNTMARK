using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTUTIL;
using ITNTCOMMON;
using ITNTCOMMM;
using System.IO.Ports;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace ITNTMARK
{
    public class PLCMELSEQSerial// : Serial
    {
        //---------------------------------------------------------------------------------------------------'---------------------------------------------------------------------------------------------------
        // System에서 사용될 PLC --> PC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string SIGNAL_CLEAR = "0";

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PLC --> PC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string PLC_ADDRESS_D100 = "D0100";              // PLC -> PC

        public const int SIGNAL_PLC2PC_NORMAL = 0;
        public const int SIGNAL_PLC2PC_NEXTVIN = 1;                 // 각인준비 OK _ 넥스트 빈
        public const int SIGNAL_PLC2PC_PRINTING = 2;                // 각인중
        public const int SIGNAL_PLC2PC_VISION = 4;                // 각인 완료
        public const int SIGNAL_PLC2PC_NOFRAME = 8;                 // 비상 정지
        public const int SIGNAL_PLC2PC_EMERGENCY_STOP = 16;         // 비상 정지

        public const string PLC_ADDRESS_SEQ = "D0300";                // Seq no
        public const string PLC_ADDRESS_CAR = "D0400";                // Car type
        public const string PLC_ADDRESS_VIN_FIRST3 = "D0500";         // Vin First 3Char
        public const string PLC_ADDRESS_VIN_LAST3 = "D0600";          // Vin First 3Char

        public const string PLC_ADDRESS_D120 = "D0120";              // PLC -> PC(수동=1, 자동=0)

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string PLC_ADDRESS_D200 = "D0200";              // PC 정보

        public const int SIGNAL_PC2PLC_READY = 0;                   // 운전준비
        public const int SIGNAL_PC2PLC_MATCHING_OK = 1;             // 각인 시작
        public const int SIGNAL_PC2PLC_MATCHING_NG = 2;             // NG
        public const int SIGNAL_PC2PLC_PC_ERROR = 4;                // PC TOTAL ERROR

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 차종(타입)데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string PLC_ADDRESS_D210 = "D0210";              // PC 정보 not used
        public const string PLC_ADDRESS_D800 = "D0800";              // car type

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 차종(타입)데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        //public const string PLC_ADDRESS_D220_MATCH = "D0220";              // 1

        public const string PLC_ADDRESS_D230_MARK = "D0230";              // PC 정보 (각인상태) 1 - 각인중, 2 - 각인완료

        public const string PLC_ADDRESS_D240_VISION = "D0240";              // vision result 1 - OK, 2 - NG, 4 - Shalow

        public const string PLC_ADDRESS_D250_SCAN_COMP = "D0250";              // scancomplete = 1

        public const byte PLC_MARK_STATUS_IDLE = 0;
        public const byte PLC_MARK_STATUS_DOING = 1;
        public const byte PLC_MARK_STATUS_COMPLETE = 2;

        public const int PLC_MODE_WRITE = 1;
        public const int PLC_MODE_READ = 0;

        public const int PLC_D_LEN_01 = 01;
        public const int PLC_D_LEN_20 = 20;
        public const int PLC_D_LEN_16 = 10;

        private readonly object cmdLock = new object();
        private readonly object bufferLock = new object();
        byte[] RecvFrameData = new byte[2048];
        int RecvFrameLength = 0;
        //bool doingCommand = false;
        
        bool DoingPLCStatusThread = false;
        Thread _plcStatusThread = null;

        private byte NAKError = 0;
        public bool IsReadSensor = false;

        protected static SerialPort Port = new SerialPort();
        protected RingBuffer cb;
        private readonly object comLock = new object();
        private object thisLock = new object();

        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;

        bool doingCmdFlag = false;

        int plcLogFlag = 0;

        //public event PLCStatusChangedEventHandler PLCStatusChagnedEventFunc;
        //public event PLCErrorReceivedEventHandler PLCErrorReceivedEventFunc;
        //public event StartSignalReceivedEventHandler StartSignalReceivedEventFunc;

        //public event PLCDataArrivedEventHandler PLCDataArrivedEventFunc;
        PLCDataArrivedCallbackHandler callbackHandler;

        public PLCMELSEQSerial(PLCDataArrivedCallbackHandler callback)
        {
            callbackHandler = callback;
            Port = new SerialPort();
            cb = new RingBuffer(4096);
            plcLogFlag = (int)Util.GetPrivateProfileValueUINT("PLCCOMM", "LOGLEVEL", 0, ITNTCOMMON.Constants.PARAMS_INI_FILE);
        }

        ~PLCMELSEQSerial()
        {
            if ((Port != null) && (Port.IsOpen))
                ClosePort();
        }

        public async Task<int> OpenPLCAsync(string port, int baud, int databits)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);

                DoingPLCStatusThread = true;
                _plcStatusThread = new Thread(PLCStatusThread);
                _plcStatusThread.Start();

                return 0;
#else
                if ((Port != null) && (Port.IsOpen))
                    return 0;
                //retval = await OpenDevice(port, baud, databits, Parity.None, StopBits.One);
                retval = OpenDevice(port, baud, databits, Parity.None, StopBits.One);
                if (retval == 0)
                {
                    //statusThread = new Thread(ThreadStatusCheck);
                    //statusThread.Start();
                    //doingThread = true;
                    Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    //byte[] buffer = new byte[64];
                    //byte[] sbuf = new byte[4];
                    //sbuf[0] = 0x30;
                    //sbuf[1] = 0x30;
                    //sbuf[2] = 0x30;
                    //sbuf[3] = 0x30;
                    //await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D220, sbuf, 1, 0);

                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                }
#endif

            }
            catch (Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }

        public  int OpenDevice(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //ITNTResponseArgs retval = new ITNTResponseArgs();
            int retval = 0;
            try
            {
#if TEST_DEBUG_PLC
                Thread.Sleep(500);
                retval = 0;
#else
                retval = OpenPort(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
                if (retval == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                }
#endif
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
                return retval;
            }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<int> OpenDeviceASync(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //ITNTResponseArgs retval = new ITNTResponseArgs();
            int retval = 0;
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
                retval = 0;
#else
                retval = await OpenPortAsync(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
                if (retval == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                }
#endif
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
                return retval;
            }

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

                if (Port.IsOpen)
                    return 0;

                Port.Open();
                if (Port.IsOpen)
                {
                    //Port.DataReceived += new SerialDataReceivedEventHandler(OnDataReceivedHandler);
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

        /// <summary>
        /// Close Port
        /// </summary>
        /// <returns></returns>
        public int CloseDevice()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            Port.DataReceived -= DataReceivedHandler;
            //DoReading = false;
            //_readThread.Join(); //block until exits
            //_readThread.Abort();
            //_readThread = null;
#if TEST_DEBUG_PLC
            int retval = 0;
#else

            if ((Port == null) || (Port.IsOpen == false))
                return 0;

            //doingThread = false;
            //statusThread.Join();

            int retval = 0;
            lock (cmdLock)
            {
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

        //public int WritePLC<T>(int address, int count, T buff)
        //{
        //    int retval = 0;
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else
                if (sendArg.Address > 0)
                    addr = string.Format("{0}", sendArg.Address);
                else
                    addr = sendArg.AddrString;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, sendArg.loglevel);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, sendArg.loglevel);
                    if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, sendArg.loglevel);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        //public int ReadPLC(int address, int count, ref PLCReadDataArgs buff)
        //{
        //    int retval = 0;
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else

                if (sendArg.Address > 0)
                    addr = string.Format("{0}", sendArg.Address);
                else
                    addr = sendArg.AddrString;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, sendArg.loglevel);
                if(retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, sendArg.loglevel);
                    if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, sendArg.loglevel);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public void ReceiveCommData()
        {
            string className = "ITNTSerialComm";
            string funcName = "ReceiveCommData";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString());

            int count = 0;
            byte tmp = 0;
            bool recvCTL = false;
            int i = 0;
            string msg = "";
            byte[] look = new byte[1024];
            try
            {
                lock (bufferLock)
                {
                    count = cb.GetSize();
                    if (count <= 0)
                    {
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-0");
                        return;
                    }

                    cb.Look(ref look, count);
                    msg = Encoding.UTF8.GetString(look, 0, count);
                    PLCTrace.Instance.Trace(plcLogFlag, "PLC RECV : " + msg);
                    //1. Check STX/ACK/NAK
                    if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_REQ)
                    {
                        for (i = 0; i < count; i++)
                        {
                            //tmp = data[i];
                            cb.Look(ref tmp);
                            if (tmp == default(byte))   //NULL
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-1", Thread.CurrentThread.ManagedThreadId);
                                return;
                            }

                            if (tmp == (byte)ASCII.STX)             //STX then set flag
                            {
                                recvCTL = true;
                                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_STX;
                                cb.Get(ref tmp);
                                break;
                            }
                            else if (tmp == (byte)ASCII.ACK)
                            {
                                recvCTL = true;
                                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ACK;
                                cb.Get(ref tmp);
                                break;
                            }
                            else if (tmp == (byte)ASCII.NAK)
                            {
                                recvCTL = true;
                                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_NAK;
                                NAKError = 1;
                                cb.Get(ref tmp);
                                break;
                            }
                            else
                                cb.Get(ref tmp);
                        }
                        if (recvCTL == false)           //No STX
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-2", Thread.CurrentThread.ManagedThreadId);
                            return;
                        }
                    }

                    //2. Check Data
                    count = cb.GetSize();
                    byte[] RecvBuff = new byte[count];
                    cb.Look(ref RecvBuff, count);
                    for (i = 0; i < count; i++)
                    {
                        //tmp = cb.Look();
                        tmp = RecvBuff[i];
                        if (tmp == default(byte))   //NULL
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-3", Thread.CurrentThread.ManagedThreadId);
                            return;
                        }
                        if ((RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_ACK) && (i + 1 == 4))
                        {
                            RecvFrameData[i] = tmp;
                            RecvFrameLength = i + 1;
                            RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                            break;
                        }
                        else if ((RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_NAK) && (i + 1 == 6))
                        {
                            RecvFrameData[i] = tmp;
                            RecvFrameLength = i + 1;
                            RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                            break;
                        }
                        else if ((RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_STX) && (tmp == (byte)ASCII.ETX))
                        {
                            RecvFrameLength = i;
                            RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                            break;
                        }
                        if (RecvFrameData.Length > i)
                            RecvFrameData[i] = tmp;
                    }

                    if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
                    {
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-4 : " + RecvFlag.ToString());
                        return;
                    }
                    for (int j = 0; j < i; j++)
                        cb.Get(ref tmp);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-5");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
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

        public int SendCommandMsg(byte RWFlag, string address, byte[] msg, int size, int loglevel)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;
            int i = 0;
            byte[] sendmsg = new byte[1024];

            try
            {
                sendmsg[i++] = (byte)ASCII.ENQ;
                sendmsg[i++] = 0x30;
                sendmsg[i++] = 0x30;
                sendmsg[i++] = 0x46;
                sendmsg[i++] = 0x46;
                sendmsg[i++] = 0x57;
                if (RWFlag == 0)
                    sendmsg[i++] = 0x52;            // READ Command
                else
                    sendmsg[i++] = 0x57;            // WRITE Command
                sendmsg[i++] = 0x30;

                byte[] addrTemp = System.Text.Encoding.UTF8.GetBytes(address);
                if (address.Length >= 5)
                {
                    Array.Copy(addrTemp, 0, sendmsg, i, 5);
                    i += 5;
                }
                else
                {
                    Array.Copy(addrTemp, 0, sendmsg, i, address.Length);
                    i += address.Length;
                    for (int k = 0; k < (5 - address.Length); k++)
                        sendmsg[i++] = 0x30;
                }

                string sizeTemp = string.Format("{0, 2:D2}", size);
                addrTemp = System.Text.Encoding.UTF8.GetBytes(sizeTemp);
                Array.Copy(addrTemp, 0, sendmsg, i, 2);
                i += 2;

                if (RWFlag == 1)
                {
                    Array.Copy(msg, 0, sendmsg, i, size * 4);
                    i += size * 4;
                }

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                retval = WritePort(sendmsg, 0, i);
                if (retval <= 0)
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                    return retval;
                }

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
            }
            catch(Exception ex)
            {
                RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return 0;
        }

        private int RecvResponseMsg(byte RWFlag, ref byte[] respMsg, ref int respSize, int loglevel, int timeout = 2)
        {
            //int retval = 0;
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
            {
                //if ((RecvFlag == RECVFLAG_RECV_STX) || 
                //    (RecvFlag == RECVFLAG_RECV_ACK) || 
                //    (RecvFlag == RECVFLAG_RECV_NAK))
                //    break;
                if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_REQ)
                    break;
                Task.Delay(10);
            }
            sw.Stop();

            if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_REQ)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ACK_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                return (int)COMPORTERROR.ERR_RECV_ACK_TIMEOUT;
            }

            sw.Restart();
            while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
            {
                if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                    break;
                Task.Delay(10);
            }
            sw.Stop();

            if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                return (int)COMPORTERROR.ERR_RECV_RESPONSE_TIMEOUT;
            }

            if (NAKError == 1)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV NAK", Thread.CurrentThread.ManagedThreadId);
                return (int)COMPORTERROR.ERR_RECV_NAK;
            }

            Array.Copy(RecvFrameData, respMsg, RecvFrameLength);
            respSize = RecvFrameLength;

            //ITNTTraceLog.Instance.TraceHex(0, "{0}::{1}()  {2}", respSize, ref respMsg, className, funcName, string.Format("RECV : {0} / {1}", respMsg, respSize));
            return 0;
        }

        public async Task<int> SendCommandMsgAsync(byte RWFlag, string address, byte[] msg, int size, int loglevel, int timeout=2)
        {
            string className = "PLCMELSEQSerial";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //ITNTResponseArgs retval = new ITNTResponseArgs();
            int retval = 0;
            int i = 0;
            byte[] sendmsg = new byte[1024];
            String smsg = "";

            try
            {
                if (size <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : SIZE <= 0", Thread.CurrentThread.ManagedThreadId);
                    retval = (int)COMPORTERROR.ERR_BUFFER_SIZE_ERROR;
                    return retval;
                }

                if (Port.IsOpen == false)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : PORT CLOSED", Thread.CurrentThread.ManagedThreadId);
                    retval = (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
                    return retval;
                }


                //Stopwatch sw = new Stopwatch();
                //sw.Start();
                //while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                //{
                //    if (!doingCmdFlag)
                //        break;

                //    await Task.Delay(50);
                //}
                //sw.Stop();

                //if (doingCmdFlag)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY");
                //    retval = (int)COMPORTERROR.ERR_COMMAND_BUSY;
                //    return retval;
                //}

                //doingCmdFlag = true;

                sendmsg[i++] = (byte)ASCII.ENQ;
                sendmsg[i++] = 0x30;
                sendmsg[i++] = 0x30;
                sendmsg[i++] = 0x46;
                sendmsg[i++] = 0x46;
                sendmsg[i++] = 0x57;
                if (RWFlag == 0)
                    sendmsg[i++] = 0x52;            // READ Command
                else
                    sendmsg[i++] = 0x57;            // WRITE Command
                sendmsg[i++] = 0x30;

                byte[] addrTemp = System.Text.Encoding.UTF8.GetBytes(address);
                if (address.Length >= 5)
                {
                    Array.Copy(addrTemp, 0, sendmsg, i, 5);
                    i += 5;
                }
                else
                {
                    Array.Copy(addrTemp, 0, sendmsg, i, address.Length);
                    i += address.Length;
                    for (int k = 0; k < (5 - address.Length); k++)
                        sendmsg[i++] = 0x30;
                }

                //string sizeTemp = string.Format("{0, 2:D2}", size);
                //addrTemp = System.Text.Encoding.UTF8.GetBytes(sizeTemp);
                //Array.Copy(addrTemp, 0, sendmsg, i, 2);
                //i += 2;
                //string sizeTemp = string.Format("{0, D4}", size);
                string sizeTemp = size.ToString("D2");
                addrTemp = System.Text.Encoding.UTF8.GetBytes(sizeTemp);
                Array.Copy(addrTemp, 0, sendmsg, i, 2);
                i += 2;

                if (RWFlag == 1)
                {
                    Array.Copy(msg, 0, sendmsg, i, size * 4);
                    i += size * 4;
                }

                smsg = Encoding.UTF8.GetString(sendmsg, 0, i);
                PLCTrace.Instance.Trace(plcLogFlag, "PLC SEND : " + smsg);

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                retval = WritePort(sendmsg, 0, i);
                if (retval <= 0)
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                    //doingCmdFlag = false;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : RECV IDLE", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
            }
            catch (Exception ex)
            {
                RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
                //doingCmdFlag = false;
                return retval;
            }

            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async Task<ITNTResponseArgs> RecvResponseMsgAsync(byte RWFlag, int loglevel, int timeout = 2)
        {
            string className = "PLCMELSEQSerial";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "RecvResponseMsgAsync";// MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    //if ((RecvFlag == RECVFLAG_RECV_STX) || 
                    //    (RecvFlag == RECVFLAG_RECV_ACK) || 
                    //    (RecvFlag == RECVFLAG_RECV_NAK))
                    //    break;
                    if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_REQ)
                        break;
                    await Task.Delay(10);
                }
                sw.Stop();

                if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_REQ)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ACK_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMPORTERROR.ERR_RECV_ACK_TIMEOUT;
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                    return retval;
                }

                sw.Restart();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                        break;
                    await Task.Delay(10);
                }
                sw.Stop();

                if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMPORTERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                    return retval;
                }

                if (NAKError == 1)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV NAK", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMPORTERROR.ERR_RECV_NAK;
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                    return retval;
                }

                Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                sw.Stop();
            }
            //ITNTTraceLog.Instance.TraceHex(0, "{0}::{1}()  {2}", respSize, ref respMsg, className, funcName, string.Format("RECV : {0} / {1}", respMsg, respSize));
            return retval;
        }


        public int ExecuteCommandMsg(byte RWFlag, string address, byte[] msg, int size, ref byte[] respMsg, ref int respSize, int loglevel, int timeout=2)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;
            int retrycount = 0;

            if (size <= 0)
            {
                return (int)COMPORTERROR.ERR_BUFFER_SIZE_ERROR;
            }

            if (Port.IsOpen == false)
            {
                return (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
            }

            lock (cmdLock)
            {
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    InitializeExecuteCommand();
                    retval = SendCommandMsg(RWFlag, address, msg, size, loglevel);
                    if (retval != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    retval = RecvResponseMsg(RWFlag, ref respMsg, ref respSize, loglevel, timeout);
                    if (retval == 0)
                        break;
                    else if (retval == (int)COMPORTERROR.ERR_RECV_NAK)
                    {
                        lock (bufferLock)
                        {
                            cb.Clear();
                        }
                    }
                    else
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(byte RWFlag, string address, byte[] msg, int size, int loglevel, int timeout = 2)
        {
            string className = "PLCMELSEQSerial";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ExecuteCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;

            try
            {
                //if (size <= 0)
                //{
                //    retval.execResult = (int)COMPORTERROR.ERR_BUFFER_SIZE_ERROR;
                //    return retval;
                //}

                //if (Port.IsOpen == false)
                //{
                //    retval.execResult = (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
                //    return retval;
                //}


                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (!doingCmdFlag)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();
                if (doingCmdFlag)
                {
                    retval.execResult = (int)COMPORTERROR.ERR_COMMAND_BUSY;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                doingCmdFlag = true;
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    InitializeExecuteCommand();
                    retval.execResult = await SendCommandMsgAsync(RWFlag, address, msg, size, loglevel, timeout);
                    if (retval.execResult <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    retval = await RecvResponseMsgAsync(RWFlag, loglevel, timeout);
                    //doingCmdFlag = false;
                    if (retval.execResult == 0)
                        break;
                    //else if (retval.execResult == (int)COMPORTERROR.ERR_RECV_NAK)
                    //{
                    //    lock (bufferLock)
                    //    {
                    //        cb.Clear();
                    //    }
                    //}
                    else
                    {
                        //doingCmdFlag = false;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        if (retval.execResult == (int)COMPORTERROR.ERR_RECV_NAK)
                        {
                            await Task.Delay(200);
                        }
                    }
                }
                doingCmdFlag = false;
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCmdFlag = false;
            }

            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        /// <summary>
        /// 
        /// </summary>
        private void InitializeExecuteCommand()
        {
            //SendFlag = SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
            RecvFrameData.Initialize();
            RecvFrameLength = 0;
            NAKError = 0;
        }


        ///// <summary>
        ///// 증분 좌표 직선이동 LINE
        ///// </summary>
        ///// <param name="posX"></param>
        ///// <param name="posY"></param>
        ///// <param name="density"></param>
        ///// <param name="movetype"></param>
        ///// <returns></returns>
        //public int MoveLineToIncreasePosition(long posX, long posY, long density, long movetype)
        //{
        //    int retval = 0;
        //    byte cmd = 0x02;
        //    return retval;
        //}


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public int SolenoidOnOff(long onoff, long time)
        //{
        //    int retval = 0;
        //    byte cmd = 0x0A;
        //    return retval;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public int OriginReturnSolenoidUp()
        //{
        //    int retval = 0;
        //    byte cmd = 0x07;
        //    return retval;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public int OriginReturnXY()
        //{
        //    int retval = 0;
        //    byte cmd = 0x08;
        //    return retval;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public int SendSignal2ExternalIO(long onoff, long channel)
        //{
        //    int retval = 0;
        //    byte cmd = 0x09;
        //    return retval;
        //}

        ///// <summary>
        ///// SOL TEST
        ///// </summary>
        ///// <returns></returns>
        //public int SolenoidTest()
        //{
        //    int retval = 0;
        //    byte cmd = 0x04;
        //    return retval;
        //}

        //public async Task<COMMAND_RESULT> GetCurrentSetting()
        //{
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    byte cmd = 0x30;
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, "0", "0", "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}

        //public async Task<COMMAND_RESULT> GetCurrentPosition()
        //{
        //    byte cmd = 0x35;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, "0", "0", "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}


        //public async Task<COMMAND_RESULT> GetCurrentPosition2()
        //{
        //    byte cmd = 0x36;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, "0", "0", "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}


        //public async Task<COMMAND_RESULT> SetTargetSpeed(long speed)
        //{
        //    byte cmd = 0x91;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, speed.ToString(), "0", "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}

        //public async Task<COMMAND_RESULT> SetDensity(long density)
        //{
        //    byte cmd = 0x92;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, density.ToString(), "0", "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}

        //public async Task<COMMAND_RESULT> SetSolenoidOnOffTime(long solOnTime, long solOffTime)
        //{
        //    byte cmd = 0x93;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, solOnTime.ToString(), solOffTime.ToString(), "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}

        //public async Task<COMMAND_RESULT> ResetYPosition(long YOrgPos)
        //{
        //    byte cmd = 0x95;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, "0", YOrgPos.ToString(), "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}


        //public async Task<COMMAND_RESULT> SetInitialSpeed(long speed)
        //{
        //    byte cmd = 0x98;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, speed.ToString(), "0", "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}

        //public async Task<COMMAND_RESULT> DryRun(long dryrun)
        //{
        //    byte cmd = 0x9A;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, dryrun.ToString(), "0", "0", "0", "0", "0", "0", "0", "");
        //    return retval;
        //}

        //public async Task<COMMAND_RESULT> SetHead(long head, long resolution, long maxX, long maxY)
        //{
        //    byte cmd = 0xA0;
        //    COMMAND_RESULT retval = new COMMAND_RESULT();
        //    retval = await ExecuteCommandMsg2(0, 20, cmd, head.ToString(), resolution.ToString(), maxX.ToString(), maxY.ToString(), "0", "0", "0", "0", "");
        //    return retval;
        //}

        private async void PLCStatusThread()
        {
            string className = "PLCControl";
            string funcName = "PLCStatusThread";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //byte[] recv_status = new byte[256];
            //byte[] recv_error = new byte[256];
            //byte[] recv_signal = new byte[256];
            //byte[] recv_sensor = new byte[256];
            //byte[] recv_polling = new byte[256];
            //int recvSize = 0;
            ITNTResponseArgs retval4status = new ITNTResponseArgs(128);
            ITNTResponseArgs retval4auto = new ITNTResponseArgs(128);
            ITNTSendArgs sndval4status = new ITNTSendArgs();
            string statusmsg = "";
            string errormsg = "";
            string sensormsg = "";
            string signalmsg = "";
            string statusmsgOld = "";
            string errormsgOld = "";
            string sensormsgOld = "";
            //string signalmsgOld = "";
            //PLCChangedEvnetArgs arg = new PLCChangedEvnetArgs();
            //PLCDataArrivedEventArgs arg = new PLCDataArrivedEventArgs();

            while (DoingPLCStatusThread)
            {
                //recv_status.Initialize();
                //recv_error.Initialize();
                //recv_signal.Initialize();
                //recv_sensor.Initialize();
                //recv_polling.Initialize();

                //retval = ReadStatus(ref recv_status, ref recvSize);
                //if (retval == 0)
                //{
                //    statusmsg = Encoding.UTF8.GetString(recv_status);
                //    if (statusmsg != statusmsgOld)
                //    {
                //        arg.receiveMsg = statusmsg;
                //        OnPLCStatusChanged(arg);
                //        statusmsgOld = statusmsg;
                //    }
                //}

                //if (!DoingPLCStatusThread)
                //    break;
                //await Task.Delay(200);
                //if (!DoingPLCStatusThread)
                //    break;

                //if (IsReadSensor)
                //{
                //    retval = ReadSensor(ref recv_sensor, ref recvSize);
                //    if (retval == 0)
                //    {
                //        sensormsg = Encoding.UTF8.GetString(recv_sensor);
                //        if (sensormsg != sensormsgOld)
                //        {
                //            arg.receiveMsg = sensormsg;
                //            OnDeviceSensorChanged(arg);
                //            sensormsgOld = sensormsg;
                //        }
                //    }

                //    if (!DoingPLCStatusThread)
                //        break;
                //    await Task.Delay(200);
                //    if (!DoingPLCStatusThread)
                //        break;
                //}

                //retval = ReadErrorMessage(ref recv_error, ref recvSize);
                //if (retval == 0)
                //{
                //    errormsg = Encoding.UTF8.GetString(recv_error);
                //    if (errormsg != errormsgOld)
                //    {
                //        arg.receiveMsg = errormsg;
                //        OnPLCErrorReceived(arg);
                //        errormsgOld = errormsg;
                //    }
                //}

                if (!DoingPLCStatusThread)
                    break;
                await Task.Delay(200);
                if (!DoingPLCStatusThread)
                    break;

                retval4status.Initialize();
                retval4status = await ReadSignalFromPLC();
                if (retval4status.execResult == 0)
                {
                    //signalmsg = Encoding.UTF8.GetString(retval4status.recvBuffer);
                    //arg.recvData = signalmsg;
                    retval4status.recvType = 1;
                    OnPLCStatusDataArrivedCallbackFunc(retval4status);
                    //OnPLCStatusDataArrivedEventFunc(arg);
                    //if (signalmsg != signalmsgOld)
                    //{
                    //    arg.receiveMsg = signalmsg;
                    //    OnStartSignalReceived(arg);
                    //    signalmsgOld = signalmsg;
                    //}
                }

                //if (!DoingPLCStatusThread)
                //    break;
                //await Task.Delay(200);
                //if (!DoingPLCStatusThread)
                //    break;, recv, PLC_D_LEN_01, ref recvMsg, ref Length
                //sndval4status.AddrString = PLC_ADDRESS_D100;
                //sndval4status.dataSize = PLC_D_LEN_01;
                //retval4status = new ITNTResponseArgs(32);
                //retval4status = await ReadPLCAsync(sndval4status);
                //if (retval4status.execResult == 0)
                //{
                //    statusmsg = Encoding.UTF8.GetString(retval4status.recvBuffer);
                //    arg.recvData = statusmsg;
                //    arg.recvSize = retval4status.recvSize;
                //    if(statusmsgOld != statusmsg)
                //    {
                //        OnPLCDataArrivedEventFunc(arg);
                //        statusmsgOld = statusmsg;
                //    }
                //}

                if (!DoingPLCStatusThread)
                    break;
                await Task.Delay(200);
                if (!DoingPLCStatusThread)
                    break;

                //retval4auto.Initialize();
                //retval4auto = await ReadAutoSignalFromPLC();
                //if (retval4auto.execResult == 0)
                //{
                //    //signalmsg = Encoding.UTF8.GetString(retval4status.recvBuffer);
                //    //arg.recvData = signalmsg;
                //    retval4auto.recvType = 2;
                //    OnPLCStatusDataArrivedCallbackFunc(retval4auto);
                //    //OnPLCStatusDataArrivedEventFunc(arg);
                //    //if (signalmsg != signalmsgOld)
                //    //{
                //    //    arg.receiveMsg = signalmsg;
                //    //    OnStartSignalReceived(arg);
                //    //    signalmsgOld = signalmsg;
                //    //}
                //}

                //retval = SendPolling(ref recv_polling, ref recvSize);
                //if (retval == 0)
                //{
                //    if(recvSize <= 0)
                //    {
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "POLLING FAILED");
                //    }
                //}

                //if (!DoingPLCStatusThread)
                //    break;
                //await Task.Delay(200);
                //if (!DoingPLCStatusThread)
                //    break;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            _plcStatusThread = null;
        }

        public async Task<ITNTResponseArgs> ReadSignalFromPLC()
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
            //value = Encoding.UTF8.GetBytes(value);
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length / 2;
#else
            retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_D100, recv, PLC_D_LEN_01, 3, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_D100, recv, PLC_D_LEN_01, 3, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_D100, recv, PLC_D_LEN_01, 3, 2);
            }
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadAutoSignalFromPLC()
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_D120", "00FF0000", ref value, "TEST.ini");
            //value = Encoding.UTF8.GetBytes(value);
            retval.recvString = value;
            //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length / 2;
#else
            retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_D120, recv, PLC_D_LEN_01, 3, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_D120, recv, PLC_D_LEN_01, 3, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_D120, recv, PLC_D_LEN_01, 3, 2);
            }
#endif
            return retval;
        }

        //        public int ReadDataFromPLC(string address, int size, byte[] recvMsg, ref int Length)
        //        {
        //            byte[] recv = new byte[32];
        //            int retval = 0;
        //#if TEST_DEBUG_PLC
        //            string value = "";
        //            Util.GetPrivateProfileValue("PLC", "SIGNAL", "00FF0000", ref value, "TEST.ini");
        //            recvMsg = Encoding.UTF8.GetBytes(value);
        //            Length = 4;
        //#else
        //            retval = ExecuteCommandMsg(PLC_MODE_READ, address, recv, size, ref recvMsg, ref Length, 3);
        //#endif
        //            return retval;
        //        }

        ///
        //////
        //protected virtual void OnPLCStatusDataArrivedEventFunc(PLCDataArrivedEventArgs e)
        //{
        //    PLCDataArrivedEventHandler handler = PLCDataArrivedEventFunc;
        //    if (handler != null)
        //        handler(this, e);
        //}

        protected virtual void OnPLCStatusDataArrivedCallbackFunc(ITNTResponseArgs e)
        {
            //PLCDataArrivedEventHandler handler = PLCDataArrivedEventFunc;
            //if (handler != null)
            //    handler(this, e);
            callbackHandler?.Invoke(e);
        }

        public async Task<ITNTResponseArgs> SendMatchingResult(byte result)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", result);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            string val = result.ToString("X4");
            byte[] temp = Encoding.UTF8.GetBytes(val);
            Array.Copy(temp, recv, temp.Length);
            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 0, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 0, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 0, 2);
            }
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", frameType);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D800", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            string val = frameType.PadLeft(4, '0');
            byte[] temp = Encoding.UTF8.GetBytes(val);
            Array.Copy(temp, recv, temp.Length);

            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D800, recv, PLC_D_LEN_01, 0, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D800, recv, PLC_D_LEN_01, 0, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D800, recv, PLC_D_LEN_01, 0, 2);
            }
#endif
            return retval;
        }

//        public async Task<ITNTResponseArgs> SendMarkFinish(int finishValue)
//        {
//            byte[] recv = new byte[32];
//            ITNTResponseArgs retval = new ITNTResponseArgs(64);
//#if TEST_DEBUG_PLC
//            string value = "";
//            value = string.Format("{0:D4}", finishValue);
//            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D800", value, "TEST.ini");
//            retval.recvString = value;
//            retval.recvSize = 4;
//#else
            
//            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D800, recv, PLC_D_LEN_01, 3, 2);
//#endif
//            return retval;
//        }

        public async Task<ITNTResponseArgs> SendPLCValue2PLC(string plcvalue)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", plcvalue);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D210", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            string val = plcvalue.PadLeft(4, '0');
            byte[] temp = Encoding.UTF8.GetBytes(val);
            Array.Copy(temp, recv, temp.Length);
            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D210, recv, PLC_D_LEN_01, 3, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D210, recv, PLC_D_LEN_01, 3, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D210, recv, PLC_D_LEN_01, 3, 2);
            }
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CAR", "00000008", ref value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            sendArg.AddrString = PLC_ADDRESS_CAR;
            sendArg.dataSize = PLC_D_LEN_01;
            retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_CAR, recv, PLC_D_LEN_01, 3, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_CAR, recv, PLC_D_LEN_01, 3, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, PLC_ADDRESS_CAR, recv, PLC_D_LEN_01, 3, 2);
            }
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendSignal(byte signal)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "00FF"+signal.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = signal.ToString("X4");
            byte[] temp = Encoding.UTF8.GetBytes(val);
            Array.Copy(temp, recv, temp.Length);

            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 3, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 3, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 3, 2);
            }
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "00FF" + status.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = status.ToString("X4");
            byte[] temp = Encoding.UTF8.GetBytes(val);
            Array.Copy(temp, recv, temp.Length);

            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D230_MARK, recv, PLC_D_LEN_01, 0, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D230_MARK, recv, PLC_D_LEN_01, 0, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D230_MARK, recv, PLC_D_LEN_01, 0, 2);
            }
#endif
            return retval;
            //ITNTResponseArgs retval = new ITNTResponseArgs();
            //if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
            //    retval = await melseqSerial.SendSignal(status);
            //return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "00FF";
            if (result == "O")
                value += 1.ToString("X4");
            else
                value += 2.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D240_VISION", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            if (result == "O")
                value = 1.ToString("X4");
            else
                value = 2.ToString("X4");
            byte[] temp = Encoding.UTF8.GetBytes(value);
            Array.Copy(temp, recv, temp.Length);

            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D240_VISION, recv, PLC_D_LEN_01, 0, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D240_VISION, recv, PLC_D_LEN_01, 0, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D240_VISION, recv, PLC_D_LEN_01, 0, 2);
            }
#endif
            return retval;
            //ITNTResponseArgs retval = new ITNTResponseArgs();
            //if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
            //    retval = await melseqSerial.SendSignal(status);
            //return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sendArg = new ITNTSendArgs(32);
            byte[] recv = new byte[64];
            string value = "";

#if TEST_DEBUG_PLC
            value = error.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            value = error.ToString("X4");
            byte[] temp = Encoding.UTF8.GetBytes(value);
            Array.Copy(temp, recv, temp.Length);

            retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 0, 2);
            if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
            {
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 0, 2);
                if (retval.execResult == (int)COMPORTERROR.ERR_COMMAND_BUSY)
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, PLC_ADDRESS_D200, recv, PLC_D_LEN_01, 0, 2);
            }
#endif
            return retval;
            //ITNTResponseArgs retval = new ITNTResponseArgs();
            //if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
            //    retval = await melseqSerial.SendSignal(status);
            //return retval;
        }


        //protected virtual void OnDeviceSensorChanged(PLCChangedEvnetArgs e)
        //{
        //    DeviceSensorChangedEventHandler handler = DeviceSensorChagnedEventFunc;
        //    if (handler != null)
        //        handler(this, e);
        //}

        //protected virtual void OnPLCStatusChanged(PLCChangedEvnetArgs e)
        //{
        //    PLCStatusChangedEventHandler handler = PLCStatusChagnedEventFunc;
        //    if (handler != null)
        //        handler(this, e);
        //}

        //protected virtual void OnPLCErrorReceived(PLCChangedEvnetArgs e)
        //{
        //    PLCErrorReceivedEventHandler handler = PLCErrorReceivedEventFunc;
        //    if (handler != null)
        //        handler(this, e);
        //}

        //protected virtual void OnStartSignalReceived(PLCChangedEvnetArgs e)
        //{
        //    StartSignalReceivedEventHandler handler = StartSignalReceivedEventFunc;
        //    if (handler != null)
        //        handler(this, e);
        //}

    }
}
