using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ITNTCOMMON;
using ITNTUTIL;
using System.Threading;
using System.Diagnostics;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    //enum COMMUNICATIONERROR
    //{
    //    ERR_SEND_DATA_NO_ERROR = 0,
    //    ERR_SEND_DATA_FAIL = -0x0001,
    //    ERR_RECV_ACK_TIMEOUT = -0x0002,
    //    ERR_RECV_RESPONSE_TIMEOUT = -0x0003,
    //    ERR_BUFFER_SIZE_ERROR = -0x0004,
    //    ERR_RECV_BCC_ERROR = -0x0005,
    //    ERR_CMD_MISSMATCH = -0x0006,
    //    ERR_PORT_NOT_OPENED = -0x0007,
    //    ERR_RECV_NAK = -0x0008,
    //    ERR_COMMAND_BUSY = -0x0009,
    //    ERR_DOING_COMMAND = -0x000A,
    //    ERR_NOT_READY = -0x000B,
    //    ERR_RECV_DATA_NONE = -0x000C,
    //}

    class PLCLSTCP
    {
        //private PLCDataArrivedCallbackHandler EventArrivalCallback;
        PLCDataArrivedCallbackHandler callbackHandler;


        //---------------------------------------------------------------------------------------------------'---------------------------------------------------------------------------------------------------
        // System에서 사용될 PLC --> PC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string SIGNAL_CLEAR = "0";

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PLC --> PC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string PLC_ADDRESS_D100 = "500";              // PLC -> PC

        public const int SIGNAL_PLC2PC_NORMAL = 0;
        public const int SIGNAL_PLC2PC_NEXTVIN = 1;                 // 각인준비 OK _ 넥스트 빈
        public const int SIGNAL_PLC2PC_MARK_1 = 2;                // 각인중
        public const int SIGNAL_PLC2PC_VISION = 4;                // 각인 완료
        public const int SIGNAL_PLC2PC_NOFRAME = 8;                 // 비상 정지
        public const int SIGNAL_PLC2PC_EMERGENCY_STOP = 16;         // 비상 정지

        public const string PLC_ADDRESS_SEQ = "506";                // Seq no
        public const string PLC_ADDRESS_CAR = "501";                // Car type
        public const string PLC_ADDRESS_VIN_FIRST3 = "500";         // Vin First 3Char
        public const string PLC_ADDRESS_VIN_LAST3 = "600";          // Vin First 3Char

        public const string PLC_ADDRESS_D120 = "503";              // PLC -> PC(수동=1, 자동=0)

        public const string PLC_ADDRESS_D520_LINK_STATUS = "502";               // Linking Status Address : 1 - Linking, 2 - Unlinking
        public const string PLC_ADDRESS_D200_CHINA = "514";              // PC 정보

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string PLC_ADDRESS_D200 = "516";              // PC 정보

        public const int SIGNAL_PC2PLC_READY = 0;                   // 운전준비
        public const int SIGNAL_PC2PLC_MATCHING_OK = 1;             // 각인 시작
        public const int SIGNAL_PC2PLC_MATCHING_NG = 2;             // NG
        public const int SIGNAL_PC2PLC_PC_ERROR = 4;                // PC TOTAL ERROR

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 차종(타입)데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string PLC_ADDRESS_D210 = "210";              // PC 정보 not used
        public const string PLC_ADDRESS_D800 = "800";              // car type

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 차종(타입)데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        //public const string PLC_ADDRESS_D220_MATCH = "D0220";              // 1

        public const string PLC_ADDRESS_D230_MARK = "517";              // PC 정보 (각인상태) 1 - 각인중, 2 - 각인완료
        public const string PLC_ADDRESS_D240_VISION = "518";              // vision result 1 - OK, 2 - NG, 4 - Shalow

        public const string PLC_ADDRESS_D230_MARK_2 = "519";              // PC 정보 (각인상태) 1 - 각인중, 2 - 각인완료
        public const string PLC_ADDRESS_D240_VISION_2 = "520";              // vision result 1 - OK, 2 - NG, 4 - Shalow

        public const string PLC_ADDRESS_D610_LINK_COMMAND = "521";              // Linking Address : 1 - Linking, 2 - Unlinking


        public const string PLC_ADDRESS_D610_AIR_COMMAND = "522";              // Linking Address : 1 - Linking, 2 - Unlinking


        public const string PLC_ADDRESS_D250_SCAN_COMP = "250";              // scancomplete = 1

        public const string PLC_PC_ERROR_ADDRESS = "700";              // PC ERROR

        public const byte PLC_MARK_STATUS_IDLE = 0;
        public const byte PLC_MARK_STATUS_DOING = 1;
        public const byte PLC_MARK_STATUS_COMPLETE = 2;

        bool isConnected = false;
        private Socket workSocket;
        //RingBuffer rb = new RingBuffer(1204);
        private const string _companyID = "LSIS-XGT";
        private readonly object bufferLock = new object();
        private readonly object cmdLock = new object();

        bool DoingPLCStatusThread = false;
        Thread _plcStatusThread = null;

        public const int PLC_MODE_WRITE = 1;
        public const int PLC_MODE_READ = 0;


        byte[] RecvFrameData = new byte[2048];
        int RecvFrameLength = 0;
        byte commError = 0;
        //bool doingCommand = false;

        protected RingBuffer recvBuffer;
        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        bool doingCmdFlag = false;
        string sDeviceName = DeviceName.Device_PLC;
        string sDeviceCode = DeviceCode.Device_PLC;


        public PLCLSTCP(PLCDataArrivedCallbackHandler callback)
        {
            callbackHandler = callback;
            recvBuffer = new RingBuffer(4096);
        }

        public async Task<int> OpenPLCAsync(short timeout)
        {
            string className = "PLCLSTCP";
            string funcName = "OpenPLCAsync";
            int retval = 0;
            string IPAddr = "", Port = "", Slot = "";
            int iport = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            StateObject state = new StateObject();
            Stopwatch sw = new Stopwatch();

            try
            {
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return 0;
                }

#if TEST_DEBUG_PLC
                await Task.Delay(500);

                DoingPLCStatusThread = true;
                _plcStatusThread = new Thread(PLCStatusThread);
                _plcStatusThread.Start();

                return 0;
#else
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref IPAddr, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref Port, Constants.PARAMS_INI_FILE);
                int.TryParse(Port, out iport);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + IPAddr + ", PORT = " + iport.ToString(), Thread.CurrentThread.ManagedThreadId);

                IPAddress ipAddress = IPAddress.Parse(IPAddr);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, iport);

                // Create a TCP/IP socket.  
                state.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remoteEP;
                e.UserToken = state.workSocket;
                e.Completed += ConnectComplete;
                state.workSocket.ConnectAsync(e);

                //state.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallBack), state.workSocket);

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (isConnected == true)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();

                if(isConnected == true)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return 0;
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }
#endif
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }

        public async Task<int> OpenPLCAsync(string IP, int wport, short timeout)
        {
            string className = "PLCLSTCP";
            string funcName = "OpenPLCAsync";
            int retval = 0;
            string IPAddr = "", Port = "";
            int iport = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            StateObject state = new StateObject();
            Stopwatch sw = new Stopwatch();

            try
            {
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return 0;
                }

#if TEST_DEBUG_PLC
                await Task.Delay(500);

                DoingPLCStatusThread = true;
                _plcStatusThread = new Thread(PLCStatusThread);
                _plcStatusThread.Start();

                return 0;
#else
                //Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref IPAddr, Constants.PARAMS_INI_FILE);
                //Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref Port, Constants.PARAMS_INI_FILE);
                //int.TryParse(Port, out iport);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + IPAddr + ", PORT = " + iport.ToString(), Thread.CurrentThread.ManagedThreadId);

                IPAddress ipAddress = IPAddress.Parse(IPAddr);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, iport);

                // Create a TCP/IP socket.  
                state.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remoteEP;
                e.UserToken = state.workSocket;
                e.Completed += ConnectComplete;
                state.workSocket.ConnectAsync(e);

                //state.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallBack), state.workSocket);

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (isConnected == true)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();

                if (isConnected == true)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return 0;
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }
#endif
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }

        public async Task<int> OpenPLC(short timeout)
        {
            string className = "PLCLSTCP";
            string funcName = "OpenPLCAsync";
            int retval = 0;
            string IPAddr = "", Port = "", Slot = "";
            int iport = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            StateObject state = new StateObject();
            Stopwatch sw = new Stopwatch();

            try
            {
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return 0;
                }

#if TEST_DEBUG_PLC
                await Task.Delay(500);

                DoingPLCStatusThread = true;
                _plcStatusThread = new Thread(PLCStatusThread);
                _plcStatusThread.Start();

                return 0;
#else
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref IPAddr, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref Port, Constants.PARAMS_INI_FILE);
                int.TryParse(Port, out iport);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + IPAddr + ", PORT = " + iport.ToString(), Thread.CurrentThread.ManagedThreadId);

                IPAddress ipAddress = IPAddress.Parse(IPAddr);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, iport);

                // Create a TCP/IP socket.  
                state.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remoteEP;
                e.UserToken = state.workSocket;
                e.Completed += ConnectComplete;
                state.workSocket.ConnectAsync(e);

                //state.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallBack), state.workSocket);

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (isConnected == true)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();

                if (isConnected == true)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return 0;
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }
#endif
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }


        public void ClosePLC(byte threadflag)
        {
            if (threadflag != 0)
                DoingPLCStatusThread = false;
            if (isConnected == true)
            {
                if((GetWorkSocket() != null) && (GetWorkSocket().Connected))
                {
                    //GetWorkSocket().Disconnect(false);
                    GetWorkSocket().Shutdown(SocketShutdown.Both);
                    GetWorkSocket().Close();
                    SetWorkSocket(null);
                }
            }
            isConnected = false;
        }


        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else
                //if (sendArg.Address > 0)
                //    addr = string.Format("{0}", sendArg.Address);
                //else
                //    addr = sendArg.AddrString;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sendArg, sendArg.loglevel);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sendArg, sendArg.loglevel);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sendArg, sendArg.loglevel);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCAsync(string strAdd)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sArg = new ITNTSendArgs();
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else

                sArg.AddrString = strAdd;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }


        public ITNTResponseArgs ReadPLC(string strAdd)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sArg = new ITNTSendArgs();
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                Thread.Sleep(500);
#else

                sArg.AddrString = strAdd;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = ExecuteCommandMsg(PLC_MODE_READ, sArg, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = ExecuteCommandMsg(PLC_MODE_READ, sArg, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = ExecuteCommandMsg(PLC_MODE_READ, sArg, 2);
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
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else

                //if (sendArg.Address > 0)
                //    addr = string.Format("{0}", sendArg.Address);
                //else
                //    addr = sendArg.AddrString;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sendArg, sendArg.loglevel);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sendArg, sendArg.loglevel);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sendArg, sendArg.loglevel);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync(string sAddress, string sWriteData)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sArg = new ITNTSendArgs();
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else

                //if (sendArg.Address > 0)
                //    addr = string.Format("{0}", sendArg.Address);
                //else
                //    addr = sendArg.AddrString;
                sArg.AddrString = sAddress;
                sArg.sendString = sWriteData;
                sArg.dataSize = sWriteData.Length;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }


        public ITNTResponseArgs WritePLC(string sAddress, string sWriteData)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sArg = new ITNTSendArgs();
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                Thread.Sleep(500);
#else

                //if (sendArg.Address > 0)
                //    addr = string.Format("{0}", sendArg.Address);
                //else
                //    addr = sendArg.AddrString;
                sArg.AddrString = sAddress;
                sArg.sendString = sWriteData;
                sArg.dataSize = sWriteData.Length;

                //retval.execResult = ExecuteCommandMsg(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, 0);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, 0);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, 0);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }


        //public void MakeData()
        //{
        //    try
        //    {
        //        if (RX == null) return;

        //        NAK_ErrorCotent = string.Empty;

        //        List<XgtAddressData> lstData = new List<XgtAddressData>();

        //        //RX 응답 중 19번째가지는 헤더프레임 정보, 20번째부터 데이터 프레임.
        //        //받은 응답이 없으면, 즉 에러가 발생시 

        //        if (RX?.Length == 0)
        //        {
        //            NAK_ErrorCotent = "서버로 부터 응답을 받지 못했습니다.";
        //            return;
        //        }
        //        if (RX?[20] == (short)XGT_Request_Func.ReadResponse)
        //        {
        //            ResponseType = XGT_Request_Func.ReadResponse;
        //        }
        //        if (RX?[20] == (short)XGT_Request_Func.WriteResponse)
        //        {
        //            ResponseType = XGT_Request_Func.WriteResponse;
        //        }

        //        byte[] vdataType = new byte[2];
        //        vdataType[0] = RX[22];
        //        vdataType[1] = RX[23];


        //        foreach (XGT_DataType item in Enum.GetValues(typeof(XGT_DataType)))
        //        {
        //            string vb = BitConverter.ToString(BitConverter.GetBytes((short)item));
        //            string va = BitConverter.ToString(vdataType);
        //            if (vb.Equals(va))
        //            {
        //                DataType = item;
        //                break;
        //            }
        //        }


        //        if (RX?[26] != 0x00 || RX?[27] != 0x00)
        //        {
        //            //에러응답
        //            ResponseStatus = "NAK";
        //            DataList = lstData;
        //            //에러메세지 확인
        //            switch (RX?[28])
        //            {
        //                case 0x12:
        //                    NAK_ErrorCotent = "(0x12)연속읽기인데 바이트 타입이 아닌 경우";
        //                    break;
        //                case 0x11:
        //                    NAK_ErrorCotent = "(0x11)변수명이 4보다 작거나 16보다 큰 경우와 같이 어드레스에 관련된 에러";
        //                    break;
        //                case 0x10:
        //                    NAK_ErrorCotent = "(0x10)없는 디바이스를 요청하는 경우와 같이 디바이스에 관련된 에러";
        //                    break;
        //                case 0x78:
        //                    NAK_ErrorCotent = "(0x78)unknown command";
        //                    break;
        //                case 0x77:
        //                    NAK_ErrorCotent = "(0x77)체크섬 오류";
        //                    break;
        //                case 0x76:
        //                    NAK_ErrorCotent = "(0x76)length 정보 오류";
        //                    break;
        //                case 0x75:
        //                    NAK_ErrorCotent = "(0x75) “LGIS-GLOFA”가 아니거나 “LSIS-XGT”가 아닌 경우";
        //                    break;
        //                case 0x24:
        //                    NAK_ErrorCotent = "(0x24)데이터 타입 에러";
        //                    break;
        //                default:
        //                    NAK_ErrorCotent = "알려지지 않은 에러코드, LS산전 고객센터에 문의 / " + Convert.ToString(RX[28]);
        //                    break;

        //            }
        //        }
        //        else
        //        {
        //            //28번 index 부터 데이터로 정의
        //            int index = 28;

        //            //정상응답
        //            ResponseStatus = "ACK";
        //            byte[] blockCount = new byte[2];  //블럭카운터
        //            byte[] dataByteCount = new byte[2];  //데이터 크기
        //            int unitdatatype = BitConverter.ToInt16(vdataType, 0);
        //            unitdatatype = (unitdatatype == 0x0014) ? 0x0001 : unitdatatype;    //continuous read

        //            byte[] data = new byte[unitdatatype];  //블럭카운터

        //            Array.Copy(RX, index, blockCount, 0, 2);
        //            BlockCount = BitConverter.ToInt16(blockCount, 0);

        //            index = index + 2;

        //            //블럭카운터 만큼의 데이터 갯수가 존재한다.

        //            //Read일 경우 데이터 생성
        //            if (ResponseType == XGT_Request_Func.ReadResponse)
        //            {
        //                for (int i = 0; i < BlockCount; i++)
        //                {
        //                    Array.Copy(RX, index, dataByteCount, 0, 2);
        //                    int biteSize = BitConverter.ToInt16(dataByteCount, 0); //데이터 크기.

        //                    index = index + 2;
        //                    int continueloop = biteSize / unitdatatype;

        //                    for (int j = 0; j < continueloop; j++)
        //                    {
        //                        Array.Copy(RX, index, data, 0, unitdatatype);

        //                        index = index + unitdatatype;  //다음 인덱스 

        //                        string dataContent = BitConverter.ToString(data).Replace("-", String.Empty);

        //                        XgtAddressData dataValue = new XgtAddressData();
        //                        dataValue.Data = dataContent;
        //                        dataValue.DataByteArray = data;

        //                        lstData.Add(dataValue);
        //                    }
        //                }
        //            }
        //            DataList = lstData;

        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        Message = "Error: " + ex.Message.ToString() + "AAA";
        //    }


        //}

        public void ReceiveCommData()
        {
            string className = "PLCLSTCP";
            string funcName = "ReceiveCommData";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString());

            int count = 0;
            //int i = 0;
            byte[] look = new byte[1024];
            int index = 0;
            byte[] dataValue = new byte[2];  //블럭카운터
            int frameLeng = 0;
            byte[] framedata = { 0x00, 0xFF, 0x30, 0x30 };//, 0x30, 0x30, 0x30, 0x30 };

            try
            {
                lock (bufferLock)
                {
                    count = recvBuffer.GetSize();
                    if (count <= 0)
                    {
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-0");
                        return;
                    }

                    recvBuffer.Look(ref look, count);
                    index = Array.IndexOf(look, (byte)'L');
                    if(index < 0)
                    {
                        recvBuffer.Get(ref look, count);
                        return;
                    }
                    else if(index > 0)
                    {
                        recvBuffer.Get(ref look, count);
                        count = recvBuffer.GetSize();
                        index = Array.IndexOf(look, (byte)'L');
                    }

                    if (count > index)
                    {
                        if (look[index + 1] != 'S')
                        {
                            recvBuffer.Get(ref look, count);
                            return;
                        }
                    }
                    else
                        return;

                    if (count < 28)
                        return;


                    //vdataType[0] = look[22];
                    //vdataType[1] = look[23];

                    if (look?[20] == (short)CommandType.ReadResponse)
                    {
                        frameLeng = 34;
                    }
                    else if (look?[20] == (short)CommandType.WriteResponse)
                    {
                        frameLeng = 30;
                    }


                    if ((look[26] != 0x00) || (look[27] != 0x00))
                    {
                        commError = look[28];
                        RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                        recvBuffer.Get(ref RecvFrameData, count);
                        return;
                    }


                    //unitdatatype = BitConverter.ToInt16(vdataType, 0);

                    //unitdatatype = (unitdatatype == 0x0014) ? 0x0001 : unitdatatype;    //continuous read

                    if (count < frameLeng)
                    {
                        return;
                    }

                    if (look?[20] == (short)CommandType.ReadResponse)
                        Array.Copy(look, frameLeng-2, dataValue, 0, 2);

                    //short sval = ((short)(dataValue[0] << 8 + dataValue[1]));
                    //string strval = sval.ToString("D4");
                    framedata[2] = dataValue[1];
                    framedata[3] = dataValue[0];

                    string tmps = BitConverter.ToString(framedata).Replace("-", string.Empty);
                    byte[] tmpby = Encoding.UTF8.GetBytes(tmps);

                    int leng = tmpby.Length;
                    if (tmpby.Length > 8)
                        leng = 8;
                    Array.Copy(tmpby, RecvFrameData, leng);


                    commError = 0x00;
                    recvBuffer.Get(ref look, frameLeng);
                    ////Array.Copy(framedata, 0, RecvFrameData, 0, 8);
                    ////RecvFrameData[6] = (byte)(dataValue[1] + 0x30);
                    ////RecvFrameData[7] = (byte)(dataValue[0] + 0x30);
                    //Array.Copy(dataValue, 0, RecvFrameData, 6, 2);
                    RecvFrameLength = 8;
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                    return;
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-5");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
        }

        public ITNTResponseArgs AnalizeRecvData(byte[]recv, int size)
        {
            string className = "PLCLSTCP";
            string funcName = "AnalizeRecvData";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString());
            CommandType ResponseType = 0;
            DataType datatype = 0;
            //int count = 0;
            //byte tmp = 0;
            //bool recvCTL = false;
            //int i = 0;
            //string msg = "";
            //byte[] look = new byte[1024];
            try
            {
                if ((recv == null) || (recv.Length <= 0)|| (size <= 0))
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_DATA_NONE;
                    return retval;
                }

                //NAK_ErrorCotent = string.Empty;
                //List<XgtAddressData> lstData = new List<XgtAddressData>();

                //RX 응답 중 19번째가지는 헤더프레임 정보, 20번째부터 데이터 프레임.
                //받은 응답이 없으면, 즉 에러가 발생시 

                //if (RX?.Length == 0)
                //{
                //    NAK_ErrorCotent = "서버로 부터 응답을 받지 못했습니다.";
                //    return;
                //}

                if (recv?[20] == (short)CommandType.ReadResponse)
                {
                    ResponseType = CommandType.ReadResponse;
                }
                if (recv?[20] == (short)CommandType.WriteResponse)
                {
                    ResponseType = CommandType.WriteResponse;
                }

                byte[] vdataType = new byte[2];
                vdataType[0] = recv[22];
                vdataType[1] = recv[23];


                foreach (DataType item in Enum.GetValues(typeof(DataType)))
                {
                    string vb = BitConverter.ToString(BitConverter.GetBytes((short)item));
                    string va = BitConverter.ToString(vdataType);
                    if (vb.Equals(va))
                    {
                        datatype = item;
                        break;
                    }
                }

                if ((recv?[26] != 0x00) || (recv?[27] != 0x00))
                {
                    //에러응답
                    retval.execResult = recv[28];
                }
                else
                {
                    //28번 index 부터 데이터로 정의
                    int index = 28;

                    //정상응답
                    retval.execResult = 0;

                    //ResponseStatus = "ACK";
                    byte[] blockCount = new byte[2];  //블럭카운터
                    byte[] dataByteCount = new byte[2];  //데이터 크기
                    int unitdatatype = BitConverter.ToInt16(vdataType, 0);
                    unitdatatype = (unitdatatype == 0x0014) ? 0x0001 : unitdatatype;    //continuous read

                    byte[] data = new byte[unitdatatype];  //블럭카운터

                    Array.Copy(recv, index, blockCount, 0, 2);
                    int BlockCount = 0;
                    BlockCount = BitConverter.ToInt16(blockCount, 0);

                    index = index + 2;

                    //블럭카운터 만큼의 데이터 갯수가 존재한다.

                    //Read일 경우 데이터 생성
                    if (ResponseType == CommandType.ReadResponse)
                    {
                        for (int i = 0; i < BlockCount; i++)
                        {
                            Array.Copy(recv, index, dataByteCount, 0, 2);
                            int biteSize = BitConverter.ToInt16(dataByteCount, 0); //데이터 크기.

                            index = index + 2;
                            int continueloop = biteSize / unitdatatype;

                            for (int j = 0; j < continueloop; j++)
                            {
                                Array.Copy(recv, index, data, 0, unitdatatype);

                                index = index + unitdatatype;  //다음 인덱스 

                                string dataContent = BitConverter.ToString(data).Replace("-", String.Empty);

                                AddressData dataValue = new AddressData();
                                dataValue.Data = dataContent;
                                dataValue.DataByteArray = data;

                                //lstData.Add(dataValue);
                            }
                        }
                    }
                    //DataList = lstData;

                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-5");
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                return retval;
            }
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
        }

        int AnalyzeErrorCheck(byte[] recv, int size, ref byte[]dest)
        {
            int retval = 0;
            int idx = 0;

            if (recv.Length < 34)
                retval = -1;

            if (size < 28)
                retval = -2;

            if (recv?[26] != 0x00 || recv?[27] != 0x00)
                retval = recv[28];
            else
                retval = 0;

            Array.Copy(recv, 22, dest, idx, 2); idx += 2;
            Array.Copy(recv, 28, dest, idx, 2); idx += 2;
            Array.Copy(recv, 32, dest, idx, 2); idx += 2;
            Array.Copy(recv, 30, dest, idx, 2); idx += 2;

            return retval;
        }

        public async Task<ITNTResponseArgs> SendCommandMsgAsync(byte RWFlag, ITNTSendArgs sArg, int loglevel, int timeout = 2)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, "START : " + sArg.AddrString);

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //int retval = 0;
            int i = 0;
            //byte[] sendmsg = new byte[1024];
            byte[] sendmsg = null;
            String smsg = "";
            byte[] sdata = null;
            byte[] header = null;
            int idx = 0;
            string sCurrentFunc = "SEND COMMAND";

            try
            {
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;

                if (RWFlag == PLC_MODE_WRITE)
                {
                    if (sArg.dataSize <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : SIZE <= 0", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = (int)COMMUNICATIONERROR.ERR_BUFFER_SIZE_ERROR;
                        retval.errorInfo.sErrorMessage = "ERROR : SIZE <= 0";
                        return retval;
                    }
                    sdata = CreateWriteDataFormat(CommandType.Write, DataType.Word, sArg, MemoryType.InternalContact, 0);
                    header = CreateHeader(1, sdata.Length);
                }
                else
                {
                    sdata = CreateReadDataFormat(CommandType.Read, DataType.Word, sArg, MemoryType.InternalContact, 0);
                    header = CreateHeader(1, sdata.Length);
                }

                sendmsg = new byte[header.Length + sdata.Length];

                //어플레케이션 헤더와 데이터 정보를 합쳐서 전송 Frame을 만든다.
                AddByte(header, ref idx, ref sendmsg);
                AddByte(sdata, ref idx, ref sendmsg);

                if (RWFlag == PLC_MODE_WRITE)
                {
                    string tmp = Encoding.UTF8.GetString(sendmsg, 0, idx);
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
                //    retval = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                //    return retval;
                //}

                //doingCmdFlag = true;

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                retval.execResult = Send(sendmsg, sendmsg.Length);
                if (retval.execResult <= 0)
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                    //doingCmdFlag = false;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : RECV IDLE", Thread.CurrentThread.ManagedThreadId);
                    retval.errorInfo.sErrorMessage = "Send FAILURE";
                    return retval;
                }

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
            }
            catch (Exception ex)
            {
                RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                //retval.sErrorMessage = string.Format("SendCommandMsgAsync EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message);
                //doingCmdFlag = false;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                return retval;
            }

            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, "END");
            return retval;
        }


        public int SendCommandMsg(byte RWFlag, ITNTSendArgs sArg, int loglevel, int timeout = 2)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsg";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, "START");

            //ITNTResponseArgs retval = new ITNTResponseArgs();
            int retval = 0;
            int i = 0;
            //byte[] sendmsg = new byte[1024];
            byte[] sendmsg = null;
            String smsg = "";
            byte[] sdata = null;
            byte[] header = null;
            int idx = 0;

            try
            {
                if (RWFlag == PLC_MODE_WRITE)
                {
                    if (sArg.dataSize <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : SIZE <= 0", Thread.CurrentThread.ManagedThreadId);
                        retval = (int)COMMUNICATIONERROR.ERR_BUFFER_SIZE_ERROR;
                        return retval;
                    }
                    sdata = CreateWriteDataFormat(CommandType.Write, DataType.Word, sArg, MemoryType.InternalContact, 0);
                    header = CreateHeader(1, sdata.Length);
                }
                else
                {
                    sdata = CreateReadDataFormat(CommandType.Read, DataType.Word, sArg, MemoryType.InternalContact, 0);
                    header = CreateHeader(1, sdata.Length);
                }

                sendmsg = new byte[header.Length + sdata.Length];

                //어플레케이션 헤더와 데이터 정보를 합쳐서 전송 Frame을 만든다.
                AddByte(header, ref idx, ref sendmsg);
                AddByte(sdata, ref idx, ref sendmsg);

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
                //    retval = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                //    return retval;
                //}

                //doingCmdFlag = true;

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                retval = Send(sendmsg, sendmsg.Length);
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
                //doingCmdFlag = false;
                return retval;
            }

            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, "END");
            return retval;
        }

        private async Task<ITNTResponseArgs> RecvResponseMsgAsync(byte RWFlag, int loglevel, int timeout = 2)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "RecvResponseMsgAsync";// MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            int iret = 0;
            string sCurrentFunc = "RECEIVE DATA";

            try
            {
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
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
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;

                    retval.errorInfo.devErrorInfo.sErrorMessage = "RESPONSE_TIMEOUT";

                    //retval.sErrorMessage = "RESPONSE_TIMEOUT";

                    return retval;
                }

                //if (NAKError == 1)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV NAK");
                //    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_NAK;
                //    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                //    return retval;
                //}
                //iret = AnalyzeErrorCheck(RecvFrameData, RecvFrameLength, ref retval.recvBuffer);
                //if (iret != 0)
                //    retval.execResult = iret;
                //else
                //    retval.execResult = 0;
                if(commError != 0)
                {
                    retval.execResult = commError;
                    //retval.sErrorMessage = "commError";
                    retval.errorInfo.devErrorInfo.sErrorMessage = "commError";
                    return retval;
                }

                Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
                //retval.recvString = Encoding.ASCII.GetString(retval.recvBuffer, 0, 8);
                //retval.recvString = BitConverter.ToString(retval.recvBuffer, 0, 8).Replace("-", String.Empty);
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, 8);
                retval.recvSize = 8;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                //retval.sErrorMessage = string.Format("RECV RESPONSE EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message);
                sw.Stop();
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
            }
            //ITNTTraceLog.Instance.TraceHex(0, "{0}::{1}()  {2}", respSize, ref respMsg, className, funcName, string.Format("RECV : {0} / {1}", respMsg, respSize));
            return retval;
        }

        private ITNTResponseArgs RecvResponseMsg(byte RWFlag, int loglevel, int timeout = 2)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "RecvResponseMsgAsync";// MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            int iret = 0;
            int recvLeng = 0;

            try
            {
                StateObject state = new StateObject();
                recvLeng = GetWorkSocket().Receive(state.buffer);
                if (recvLeng <= 0)
                {
                    retval.execResult = -5;
                    return retval;
                }

                 Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = 8;
                //Array.Copy(state.buffer, retval.recvBuffer, recvLeng);
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, 8);
                //retval.execResult = 0;
                //Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
                //retval.recvSize = RecvFrameLength;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                sw.Stop();
            }
            //ITNTTraceLog.Instance.TraceHex(0, "{0}::{1}()  {2}", respSize, ref respMsg, className, funcName, string.Format("RECV : {0} / {1}", respMsg, respSize));
            return retval;
        }

        //public int ExecuteCommandMsg(byte RWFlag, string address, byte[] msg, int size, ref byte[] respMsg, ref int respSize, int loglevel, int timeout = 2)
        //{
        //    string className = "PLCLSTCP";
        //    string funcName = "ExecuteCommandMsg";
        //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

        //    int retval = 0;
        //    int retrycount = 0;

        //    if (size <= 0)
        //    {
        //        return (int)COMMUNICATIONERROR.ERR_BUFFER_SIZE_ERROR;
        //    }

        //    if (Port.IsOpen == false)
        //    {
        //        return (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
        //    }

        //    lock (cmdLock)
        //    {
        //        for (retrycount = 0; retrycount < 3; retrycount++)
        //        {
        //            InitializeExecuteCommand();
        //            retval = SendCommandMsg(RWFlag, address, msg, size, loglevel);
        //            if (retval != 0)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount));
        //                continue;
        //            }
        //            retval = RecvResponseMsg(RWFlag, ref respMsg, ref respSize, loglevel, timeout);
        //            if (retval == 0)
        //                break;
        //            else if (retval == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
        //            {
        //                lock (bufferLock)
        //                {
        //                    cb.Clear();
        //                }
        //            }
        //            else
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, retrycount));
        //        }
        //        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
        //        return retval;
        //    }
        //}

        //public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(byte RWFlag, string address, byte[] msg, int size, int loglevel, int timeout = 2)
        //{
        //    string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "ExecuteCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
        //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    int retrycount = 0;

        //    try
        //    {
        //        //if (size <= 0)
        //        //{
        //        //    retval.execResult = (int)COMMUNICATIONERROR.ERR_BUFFER_SIZE_ERROR;
        //        //    return retval;
        //        //}

        //        //if (Port.IsOpen == false)
        //        //{
        //        //    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
        //        //    return retval;
        //        //}


        //        Stopwatch sw = new Stopwatch();
        //        sw.Start();
        //        while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
        //        {
        //            if (!doingCmdFlag)
        //                break;

        //            await Task.Delay(50);
        //        }
        //        sw.Stop();
        //        if (doingCmdFlag)
        //        {
        //            retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY");
        //            return retval;
        //        }

        //        doingCmdFlag = true;
        //        for (retrycount = 0; retrycount < 3; retrycount++)
        //        {
        //            InitializeExecuteCommand();
        //            retval.execResult = await SendCommandMsgAsync(RWFlag, address, msg, size, loglevel, timeout);
        //            if (retval.execResult <= 0)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount));
        //                continue;
        //            }
        //            retval = await RecvResponseMsgAsync(RWFlag, loglevel, timeout);
        //            //doingCmdFlag = false;
        //            if (retval.execResult == 0)
        //                break;
        //            //else if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
        //            //{
        //            //    lock (bufferLock)
        //            //    {
        //            //        cb.Clear();
        //            //    }
        //            //}
        //            else
        //            {
        //                //doingCmdFlag = false;
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, retrycount));
        //                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
        //                {
        //                    await Task.Delay(200);
        //                }
        //            }
        //        }
        //        doingCmdFlag = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //        doingCmdFlag = false;
        //    }

        //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
        //    return retval;
        //}


        public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(byte RWFlag, ITNTSendArgs sArg, int loglevel, int timeout = 2)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ExecuteCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            string sErrorDevFunc = "EXECUTE COMMAND";

            try
            {
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sErrorDevFunc;

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
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY", Thread.CurrentThread.ManagedThreadId);
                    retval.errorInfo.devErrorInfo.sErrorMessage = "ERR_COMMAND_BUSY";
                    //retval.errorInfo.sErrorDevFunc = sErrorDevFunc;
                    //retval.errorInfo.sErrorDevice = sErrorDevice;
                    //retval.sErrorMessage = "ERR_COMMAND_BUSY";
                    return retval;
                }

                doingCmdFlag = true;
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    InitializeExecuteCommand();
                    retval = await SendCommandMsgAsync(RWFlag, sArg, loglevel, timeout);
                    if (retval.execResult <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    retval = await RecvResponseMsgAsync(RWFlag, loglevel, timeout);
                    //doingCmdFlag = false;
                    if (retval.execResult == 0)
                        break;
                    //else if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
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
                        //if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
                        //{
                        //    await Task.Delay(200);
                        //}
                    }
                }
                doingCmdFlag = false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCmdFlag = false;
                retval.errorInfo.devErrorInfo.sErrorMessage = sErrorDevFunc + " EXCEPTION = " + ex.Message;
            }

            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
            return retval;
        }

        public ITNTResponseArgs ExecuteCommandMsg(byte RWFlag, ITNTSendArgs sArg, int loglevel, int timeout = 2)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ExecuteCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            string sErrorDevFunc = "EXECUTE COMMAND";

            try
            {
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
                //    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY");
                //    return retval;
                //}

                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sErrorDevFunc;

                //doingCmdFlag = true;
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    InitializeExecuteCommand();
                    retval.execResult = SendCommandMsg(RWFlag, sArg, loglevel, timeout);
                    if (retval.execResult <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    retval = RecvResponseMsg(RWFlag, loglevel, timeout);
                    //doingCmdFlag = false;
                    if (retval.execResult == 0)
                        break;
                    //else if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
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
                        //if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
                        //{
                        //    await Task.Delay(200);
                        //}
                    }
                }
                doingCmdFlag = false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCmdFlag = false;
                retval.errorInfo.sErrorCode = sErrorDevFunc + " EXCEPTION = " + ex.Message;
            }

            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
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
            //NAKError = 0;
        }

        private async void PLCStatusThread()
        {
            string className = "PLCLSTCP";
            string funcName = "PLCStatusThread";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTResponseArgs retval4status = new ITNTResponseArgs(128);
            //ITNTResponseArgs retval4auto = new ITNTResponseArgs(128);
            //ITNTSendArgs sndval4status = new ITNTSendArgs();
            //string statusmsg = "";
            //string errormsg = "";
            //string sensormsg = "";
            //string signalmsg = "";
            //string statusmsgOld = "";
            //string errormsgOld = "";
            //string sensormsgOld = "";

            while (DoingPLCStatusThread)
            {
                if (!DoingPLCStatusThread)
                    break;
                //await Task.Delay(200);
                //if (!DoingPLCStatusThread)
                //    break;

                retval4status.Initialize();
                retval4status = await ReadSignalFromPLC();
                if (retval4status.execResult == 0)
                {
                    retval4status.recvType = 1;
                    OnPLCStatusDataArrivedCallbackFunc(retval4status);
                }

                if (!DoingPLCStatusThread)
                    break;
                await Task.Delay(200);
                if (!DoingPLCStatusThread)
                    break;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            _plcStatusThread = null;
        }


        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel, CancellationToken token = default)
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
            //value = Encoding.UTF8.GetBytes(value);
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_D100;
                sArg.loglevel = loglevel;
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, loglevel, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadSignalFromPLC()
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
            //value = Encoding.UTF8.GetBytes(value);
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_D100;
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadAutoSignalFromPLC()
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_D120", "00FF0000", ref value, "TEST.ini");
            //value = Encoding.UTF8.GetBytes(value);
            retval.recvString = value;
            //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_D120;
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
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
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();

            try
            {
                sArg.AddrString = PLC_ADDRESS_D200;
                int.TryParse(sArg.AddrString, out sArg.Address);

                if (result == SIGNAL_PC2PLC_MATCHING_OK)
                    sArg.sendBuffer[0] = 1;
                else
                    sArg.sendBuffer[0] = 2;
                sArg.dataSize = 1;

#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", result);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch(Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType)
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", frameType);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D800", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = value.Length;
#else
                //string val = frameType.PadLeft(4, '0');
                byte[] temp = Encoding.UTF8.GetBytes(frameType);
                Array.Copy(temp, sArg.sendBuffer, temp.Length);
                sArg.sendString = frameType;
                sArg.dataSize = temp.Length;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch(Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        //        public async Task<ITNTResponseArgs> SendMarkFinish(int finishValue)
        //        {
        //            byte[] recv = new byte[32];
        //            ITNTResponseArgs retval = new ITNTResponseArgs(256);
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

        public async Task<ITNTResponseArgs> SendPCError2PLC(string plcvalue)
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", plcvalue);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D210", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_D210;
                //string val = plcvalue.PadLeft(4, '0');
                byte[] temp = Encoding.UTF8.GetBytes(plcvalue);
                Array.Copy(temp, sArg.sendBuffer, temp.Length);
                sArg.sendString = plcvalue;
                sArg.dataSize = temp.Length;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 1);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 1);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 1);
                }
#endif
            }
            catch(Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];

            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CAR", "00000008", ref value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_CAR;
                //sendArg.dataSize = PLC_D_LEN_01;
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];

            try
            {
                sArg.dataSize = 4;
#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQ", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_SEQ;
                //sendArg.dataSize = PLC_D_LEN_01;
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCChinaFlag()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];

            try
            {
                sArg.dataSize = 4;
#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQ", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_D200_CHINA;
                //sendArg.dataSize = PLC_D_LEN_01;
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> SendSignal(byte signal)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            try
            {
#if TEST_DEBUG_PLC
            string value = "00FF" + signal.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                //string val = signal.ToString("X");
                //byte[] temp = Encoding.UTF8.GetBytes(val);
                //Array.Copy(temp, sArg.sendBuffer, temp.Length);
                sArg.sendBuffer[0] = signal;
                sArg.dataSize = 1;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status, byte order=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                if(order == 2)
                    sArg.AddrString = PLC_ADDRESS_D230_MARK_2;
                else
                    sArg.AddrString = PLC_ADDRESS_D230_MARK;

                int.TryParse(sArg.AddrString, out sArg.Address);

                //if (status == PLC_MARK_STATUS_DOING)
                //    sArg.sendBuffer[0] = 1;
                //else if (status == PLC_MARK_STATUS_COMPLETE)
                //    sArg.sendBuffer[0] = 2;
                //else
                //    sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = status;

                sArg.dataSize = 1;

#if TEST_DEBUG_PLC
            string value = "00FF" + status.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                //string val = status.ToString("X4");
                //byte[] temp = Encoding.UTF8.GetBytes(val);
                //Array.Copy(temp, recv, temp.Length);
                //if (status == PLC_MARK_STATUS_DOING)
                //{
                //    sArg.AddrString = "602";
                //    sArg.Address = 602;
                //}
                //else if(status == PLC_MARK_STATUS_COMPLETE)
                //{
                //    sArg.AddrString = "603";
                //    sArg.Address = 603;
                //}
                //else
                //{
                //    sArg.AddrString = "600";
                //    sArg.Address = 600;
                //}

                //sArg.sendBuffer[0] = status;
                //sArg.dataSize = 1;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result, byte order=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];
            try
            {
                if(order == 2)
                    sArg.AddrString = PLC_ADDRESS_D240_VISION_2;
                else
                    sArg.AddrString = PLC_ADDRESS_D240_VISION;

                int.TryParse(sArg.AddrString, out sArg.Address);

                if (result == "O")
                    sArg.sendBuffer[0] = 1;
                else
                    sArg.sendBuffer[0] = 2;
                sArg.dataSize = 1;

#if TEST_DEBUG_PLC
            string value = "00FF";
            if (result == "O")
                value += 1.ToString("X4");
            else
                value += 2.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D240_VISION", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                //byte[] temp;
                ////string value = "";
                //if (result == "O")
                //    temp = BitConverter.GetBytes(1);// 1.ToString("");
                //else
                //    temp = BitConverter.GetBytes(2);// 2.ToString("");
                //                                    //value = 2.ToString("");
                //                                    //byte[] temp = Encoding.UTF8.GetBytes(value);
                //Array.Copy(temp, sArg.sendBuffer, temp.Length);
                //sArg.dataSize = temp.Length;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendAirAsync(byte air)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_D610_AIR_COMMAND;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.sendBuffer[0] = air;
                sArg.dataSize = 1;

#if TEST_DEBUG_PLC
            string value = "00FF";
            //if (result == "O")
            //    value += 1.ToString("X4");
            //else
            //    value += 2.ToString("X4");

            value += air.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_AIR_COMMAND", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                //byte[] temp;
                ////string value = "";
                //if (result == "O")
                //    temp = BitConverter.GetBytes(1);// 1.ToString("");
                //else
                //    temp = BitConverter.GetBytes(2);// 2.ToString("");
                //                                    //value = 2.ToString("");
                //                                    //byte[] temp = Encoding.UTF8.GetBytes(value);
                //Array.Copy(temp, sArg.sendBuffer, temp.Length);
                //sArg.dataSize = temp.Length;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            //byte[] recv = new byte[256];
            //string value = "";
            try
            {
#if TEST_DEBUG_PLC
            //value = error.ToString("X4");
            //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            //retval.recvString = value;
            //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            //retval.recvSize = 4;
#else
                //value = error.ToString("X4");
                //byte[] temp = Encoding.UTF8.GetBytes(value);
                //Array.Copy(temp, recv, temp.Length);

                sArg.AddrString = address;
                sArg.sendBuffer[0] = error;
                sArg.dataSize = 1;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            //byte[] recv = new byte[256];
            //string value = "";
            try
            {
#if TEST_DEBUG_PLC
            //value = error.ToString("X4");
            //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            //retval.recvString = value;
            //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            //retval.recvSize = 4;
#else
                //value = error.ToString("X4");
                //byte[] temp = Encoding.UTF8.GetBytes(value);
                //Array.Copy(temp, recv, temp.Length);

                sArg.AddrString = PLC_PC_ERROR_ADDRESS;
                sArg.sendBuffer[0] = error;
                sArg.dataSize = 1;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
#if TEST_DEBUG_PLC
            string value = distance.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendScanComplete()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
#if TEST_DEBUG_PLC
            //string value = error.ToString("X4");
            //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            //retval.recvString = value;
            //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            //retval.recvSize = 4;
#else
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SetCommSettingTCP(string IP, int Port)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
                if(IP.Length > 0)
                    Util.WritePrivateProfileValue("PLCCOMM", "SERVERIP", IP, Constants.PARAMS_INI_FILE);
                if(Port > 0)
                    Util.WritePrivateProfileValue("PLCCOMM", "SERVERPORT", Port.ToString(), Constants.PARAMS_INI_FILE);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public ITNTResponseArgs GetCommSettingTCP(ref string IP, ref int Port)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            string value = "";

            try
            {
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "", ref IP, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "", ref value, Constants.PARAMS_INI_FILE);
                int.TryParse(value, out Port);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public ITNTResponseArgs CheckConnection()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            string value = "";

            try
            {
                bool conn = isConnected;


                if (GetWorkSocket() == null)
                {
                    retval.recvString = "SOCKET NULL";
                    retval.execResult = -1;
                    return retval;
                }

                if (GetWorkSocket().Connected == false)
                {
                    retval.recvString = "CONNECTION FALSE";
                    retval.execResult = -2;
                    return retval;
                }

                retval.recvString = "CONNECTION GOOD";
                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.recvString = "CONNECTION EXCEPTION";
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> SetLinkAsync(byte link)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_D610_LINK_COMMAND;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //if (link == PLCControlManager.SIGNAL_PC2PLC_OFF)
                //    sArg.sendBuffer[0] = 1;
                //else
                //    sArg.sendBuffer[0] = 2;
                sArg.sendBuffer[0] = link;
                sArg.dataSize = 1;

#if TEST_DEBUG_PLC
            string value = "00FF" + link.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadLinkStatusAsync()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_D520_LINK_STATUS;
                int.TryParse(sArg.AddrString, out sArg.Address);

#if TEST_DEBUG_PLC
            string value = "00FF0000";
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, sArg.loglevel, 2);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, sArg.loglevel, 2);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, sArg.loglevel, 2);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


























        //public int Connect(string serverIP, int serverPort, int timeout = 3000)
        //{
        //    int retval = 0;
        //    string className = "PLCLSTCP";
        //    string funcName = "OpenMESAsync";
        //    string value = "";

        //    try
        //    {
        //        ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "START");
        //        if (isConnected)
        //        {
        //            ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "ALEADY CONNECTED");
        //            return 0;
        //        }

        //        Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.0.50", ref value, Constants.PARAMS_INI_FILE);
        //        serverIP = value;
        //        Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "256000", ref value, Constants.PARAMS_INI_FILE);
        //        int.TryParse(value, out serverPort);

        //        ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "IP = " + serverIP + ", PORT = " + serverPort.ToString());

        //        IPAddress ipAddress = IPAddress.Parse(serverIP);
        //        IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

        //        // Create a TCP/IP socket.  
        //        workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //        //workSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

        //        //SocketAsyncEventArgs e = new SocketAsyncEventArgs();
        //        //e.RemoteEndPoint = remoteEP;
        //        //e.UserToken = workSocket;
        //        //e.Completed += ConnectCallBack;
        //        //workSocket.ConnectAsync(e);
        //        workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallBack), workSocket);

        //        ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "CONNECTED");
        //        return 0;






        //        //IPAddress ipaddr;
        //        //if(bconnected)
        //        //{
        //        //    return 0;
        //        //}

        //        //if (IPAddress.TryParse(serverIP, out ipaddr))
        //        //{
        //        //    tcpSocket = new Socket(IPAddress.Parse(serverIP).AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        //        //    IPAddress ipAddress = IPAddress.Parse(serverIP);
        //        //    IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);
        //        //    EndPoint ep = 
        //        //    tcpSocket.BeginConnect()

        //        //    //tcpSocket.Connect(new IPEndPoint(IPAddress.Parse(IPAddr), port));
        //        //    tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout);
        //        //    tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);
        //        //    tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
        //        //}

        //        //bconnected = tcpSocket.Connected;

        //        //// IPAddress 
        //    }
        //    catch (Exception ex)
        //    {
        //        //bconnected = false;
        //        ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
        //    }

        //    return retval;
        //}


        //public int Disconnect()
        //{
        //    int retval = 0;

        //    return retval;
        //}

        private async void ConnectComplete(object sender, SocketAsyncEventArgs e)
        {
            string className = "PLCLSTCP";
            string funcName = "ConnectComplete";
            bool bret = false;
            byte[] socketBuffer = new byte[256];
            try
            {
                isConnected = e.SocketError == SocketError.Success;
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCLSTCP", "connectComplete", "isConnected = true", Thread.CurrentThread.ManagedThreadId);
                    workSocket = (Socket)e.UserToken;

                    SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
                    arg.UserToken = workSocket;
                    arg.Completed += ReceiveComplete;
                    arg.SetBuffer(socketBuffer, 0, socketBuffer.Length);
                    bret = workSocket.ReceiveAsync(arg);
                    if (bret == false)
                    {

                    }
                    //mesRunningTimer.Start();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public async void ConnectCallBack(IAsyncResult ar)/*(object sender, SocketAsyncEventArgs e)*/
        {
            string className = "PLCLSTCP";
            string funcName = "ConnectCallBack";
            StateObject stateobj = new StateObject();

            try
            {
                Socket client = (Socket)ar.AsyncState;
                // Complete the connection.  
                client.EndConnect(ar);
                SetWorkSocket(client);
                isConnected = true;

                client.BeginReceive(stateobj.buffer, 0, stateobj.BufferSize, 0, new AsyncCallback(ReceiveCallback), client);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private async void ReceiveComplete(object sender, SocketAsyncEventArgs e)
        {
            string className = "PLCLSTCP";
            string funcName = "ReceiveComplete";
            SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
            string value = "";
            bool bret = false;
            byte[] socketBuffer = new byte[256];
            string IPAddr = "";
            string Port = "";
            int iport = 0;
            Socket wsocket;

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV SIZE : " + e.BytesTransferred.ToString(), Thread.CurrentThread.ManagedThreadId);

            try
            {
                //ReceiveCommData();
                if (e.BytesTransferred <= 0)
                {
                    if ((workSocket != null) && workSocket.Connected)
                    {
                        //ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SOCKET CLOSE");
                        //workSocket.Shutdown(SocketShutdown.Both);
                        //workSocket.Close();
                        //mesRunningTimer.Stop();
                        //asyncstate.workSocket.Close();
                        ClosePLC(0);

                        Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref IPAddr, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref Port, Constants.PARAMS_INI_FILE);
                        int.TryParse(Port, out iport);

                        ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + IPAddr + ", PORT = " + iport.ToString(), Thread.CurrentThread.ManagedThreadId);

                        IPAddress ipAddress = IPAddress.Parse(IPAddr);
                        IPEndPoint remoteEP = new IPEndPoint(ipAddress, iport);

                        // Create a TCP/IP socket.  
                        wsocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        e.RemoteEndPoint = remoteEP;
                        e.UserToken = workSocket;
                        e.Completed += ConnectComplete;
                        workSocket.ConnectAsync(e);
                    }
                    ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSE", Thread.CurrentThread.ManagedThreadId);
                    //isConnected = false;
                    return;
                }

                value = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV : " + value, Thread.CurrentThread.ManagedThreadId);

                lock (bufferLock)
                {
                    recvBuffer.Put(e.Buffer, e.BytesTransferred);
                }
                ReceiveCommData();

                arg.UserToken = workSocket;
                arg.Completed += ReceiveComplete;
                arg.SetBuffer(socketBuffer, 0, socketBuffer.Length);
                bret = workSocket.ReceiveAsync(arg);
                if (!bret)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV ASYNC ERROR", Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void ReceiveCallback(IAsyncResult ar)
        {
            string className = "PLCLSTCP";
            string funcName = "ReceiveCallBack";
            //SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
            string value = "";
            bool bret = false;
            int bytesRead = 0;
            Socket client;
            StateObject state = new StateObject();
            string IPAddr = "";
            string Port = "";
            int iport = 0;

            try
            {
                state = (StateObject)ar.AsyncState;
                client = state.workSocket;
                // Read data from the remote device.  
                bytesRead = client.EndReceive(ar);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV SIZE : " + bytesRead.ToString(), Thread.CurrentThread.ManagedThreadId);

                if (bytesRead > 0)
                {
                    //// There might be more data, so store the data received so far.  
                    //state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    lock (bufferLock)
                    {
                        recvBuffer.Put(state.buffer, bytesRead);
                    }
                    // Get the rest of the data.  
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                    client.BeginReceive(state.buffer, 0, state.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    //ReceiveCommData();
                }
                else
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV COMPLETE", Thread.CurrentThread.ManagedThreadId);
                    //if (client.Connected == true)
                    //{
                    //    client.Disconnect(false);
                    //    client.Close();
                    //    isConnected = false;
                    //    SetWorkSocket(client);
                    //    ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SOCKET CLOSE - RECEIVE <= 0");
                    //}
                    //client.BeginReceive(state.buffer, 0, state.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    ClosePLC(0);

                    Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref IPAddr, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref Port, Constants.PARAMS_INI_FILE);
                    int.TryParse(Port, out iport);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + IPAddr + ", PORT = " + iport.ToString(), Thread.CurrentThread.ManagedThreadId);

                    IPAddress ipAddress = IPAddress.Parse(IPAddr);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, iport);

                    // Create a TCP/IP socket.  
                    state.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    state.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallBack), state.workSocket);

                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        //async Task<ITNTResponseArgs> ExecuteCommandAsync(CommandType cmdtype, byte[]senddata, short timeout)
        //{
        //    string className = "PLCLSTCP";
        //    string funcName = "ExecuteCommandAsync";

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    string data = "";
        //    int sendleng = 0;

        //    try
        //    {
        //        ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "START");
        //        if (GetWorkSocket() == null)
        //        {
        //            retval.execResult = -1;
        //            return retval;
        //        }

        //        if (GetWorkSocket().Connected == false)
        //        {
        //            retval.execResult = -2;
        //            return retval;
        //        }

        //        for(int i = 0; i < 3; i++)
        //        {
        //            sendleng = Send(GetWorkSocket(), data);
        //            if (sendleng <= 0)
        //            {
        //                continue;
        //            }


        //        }


        //    }
        //    catch (Exception ex)
        //    {

        //        ITNTMESLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
        //    }
        //    return retval;
        //}


        //private int Send(Socket client, byte[] sdata, int leng)
       private int Send(byte[] sdata, int leng)
        {
            // Convert the string data to byte data using ASCII encoding.  
            int sentleng = 0;
            string className = "PLCLSTCP";
            string funcName = "Send";

            try
            {
                //byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.  
                //if (GetWorkSocket() == null)
                //    return -1;
                //if (GetWorkSocket().Connected == false)
                //    return -2;

                if(isConnected == false)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }

                sentleng = GetWorkSocket().Send(sdata, leng, SocketFlags.None);
                return sentleng;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId); 
                return -3;
            }
        }


        //private async Task<ITNTResponseArgs>ReceiveCommand(short timeout)
        //{
        //    string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "RecvResponseMsgAsync";// MethodBase.GetCurrentMethod().Name;
        //    Stopwatch sw = new Stopwatch();
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    try
        //    {
        //        sw.Start();
        //        while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
        //        {
        //            if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
        //                break;
        //            await Task.Delay(10);
        //        }
        //        sw.Stop();

        //        if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT");
        //            retval.execResult = (int).ERR_RECV_RESPONSE_TIMEOUT;
        //            RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
        //            return retval;
        //        }

        //        AnalyzeRecvData();

        //        if (NAKError == 1)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV NAK");
        //            retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_NAK;
        //            RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
        //            return retval;
        //        }

        //        Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
        //        retval.recvSize = RecvFrameLength;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
        //        sw.Stop();
        //    }
        //    //ITNTTraceLog.Instance.TraceHex(0, "{0}::{1}()  {2}", respSize, ref respMsg, className, funcName, string.Format("RECV : {0} / {1}", respMsg, respSize));
        //    return retval;
        //}


        private void SetWorkSocket(Socket sc)
        {
            Volatile.Write(ref workSocket, sc);
        }

        private Socket GetWorkSocket()
        {
            return Volatile.Read(ref workSocket);
        }

        //private void SetRecvFlag(byte flag)
        //{
        //    Volatile.Write(ref recvFlag, flag);
        //}

        //private byte GetRecvFlag()
        //{
        //    return Volatile.Read(ref recvFlag);
        //}





        /////////
        ///
                //어플리케이션 헤더 만들기
        public byte[] CreateHeader(int pInvokeID, int pDataByteLenth)
        {
            byte[] CompanyID = Encoding.UTF8.GetBytes(_companyID);      //Company ID (8 Byte)
            byte[] Reserved = BitConverter.GetBytes((short)0);          //Reserved 예약영역  2 Byte -> Company ID : total 10 Byte
            byte[] PLCInfo = BitConverter.GetBytes((short)0);           // PLC Info >> Client 0x00;
            byte[] CPUInfo = new byte[1];
            byte[] SOF = new byte[1];
            byte[] InvokeID = BitConverter.GetBytes((short)pInvokeID);
            byte[] Length = BitConverter.GetBytes((short)pDataByteLenth); //Application Data Format 바이트 크기
            byte[] FEnetPosition = new byte[1];
            byte[] Reserved2 = new byte[1];

            CPUInfo[0] = 0xA4;            //CPU INFO 1 Byte , XGI communication module
            SOF[0] = 0x33;                //Source of Frame (Fixed value, Client -> Server)
            FEnetPosition[0] = 0x00;      //Bit0~3 : 이더넷 모듈의 슬롯 번호 ,  Bit4~7 : 이더넷 모듈의 베이스 번호
            Reserved2[0] = 0x00;          //Byte Sum of Application Header(BCC)

            //헤더 프레임의 길이 계산.
            int vLenth = CompanyID.Length + Reserved.Length + PLCInfo.Length + CPUInfo.Length + SOF.Length
                                  + InvokeID.Length + Length.Length + FEnetPosition.Length + Reserved2.Length;

            byte[] header = new byte[vLenth];

            int idx = 0;
            AddByte(CompanyID, ref idx, ref header);
            AddByte(Reserved, ref idx, ref header);
            AddByte(PLCInfo, ref idx, ref header);
            AddByte(CPUInfo, ref idx, ref header);
            AddByte(SOF, ref idx, ref header);
            AddByte(InvokeID, ref idx, ref header);
            AddByte(Length, ref idx, ref header);
            AddByte(FEnetPosition, ref idx, ref header);

            int checksum = 0;

            for (int i = 0; i < idx; i++) { checksum += header[i]; }

            Reserved2[0] = (byte)(checksum % 256);                      //BCC = Sum(CompanyID,PLCInfo,CPUInfo,SOF,InvokeID,Length,FEnetPosition)

            AddByte(Reserved2, ref idx, ref header);


            return header;
        }


        //어플리케이션 데이터 READ 포맷 만들기
        private byte[] CreateReadDataFormat(CommandType emFunc, DataType emDatatype, List<AddressData> pAddressList, MemoryType emMemtype, int pDataCount)
        {
            int vLenth = 0;     //데이타 포맷 프레임의 크기
            List <AddressData> lstAddress = new List<AddressData>();

            byte[] command = BitConverter.GetBytes((short)emFunc);                  //StringToByteArray((int)emFunc, true);  //명령어 읽기,쓰기
            byte[] dataType = BitConverter.GetBytes((short)emDatatype);             //StringToByteArray((int)emDatatype, true);  //데이터 타입
            byte[] reserved = BitConverter.GetBytes((short)0);                      //예약영역 고정(0x0000)
            byte[] blockcount = BitConverter.GetBytes((short)pAddressList.Count);   //블록수 

            //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
            vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

            foreach (AddressData addr in pAddressList)
            {
                string vAddress = CreateValueName(emDatatype, emMemtype, addr.Address);

                AddressData XgtAddr = new AddressData();
                XgtAddr.AddressString = vAddress;

                lstAddress.Add(XgtAddr);

                vLenth += XgtAddr.AddressByteArray.Length + XgtAddr.LengthByteArray.Length;
            }

            if (DataType.Continue == emDatatype && CommandType.Read == emFunc)
            {
                vLenth += 2;  //연속읽기 인 경우 2바이트 추가.(데이터 갯수)
            }

            byte[] data = new byte[vLenth];


            int idx = 0;
            AddByte(command, ref idx, ref data);
            AddByte(dataType, ref idx, ref data);
            AddByte(reserved, ref idx, ref data);
            AddByte(blockcount, ref idx, ref data);

            foreach (AddressData addr in lstAddress)
            {
                AddByte(addr.LengthByteArray, ref idx, ref data);
                AddByte(addr.AddressByteArray, ref idx, ref data);
            }

            /* 연속 읽기의 경우 읽을 갯수 지정. */
            if (DataType.Continue == emDatatype)
            {
                //데이터 타입이 연속 읽기 인 경우.
                byte[] vDataCount = BitConverter.GetBytes((short)pDataCount);
                AddByte(vDataCount, ref idx, ref data);
            }

            return data;
        }


        //어플리케이션 데이터 READ 포맷 만들기
        //private byte[] CreateReadDataFormat(XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XgtAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
        private byte[] CreateReadDataFormat(CommandType emFunc, DataType emDatatype, ITNTSendArgs sArg, MemoryType emMemtype, int pDataCount)
        {
            int vLenth = 0;  //데이타 포맷 프레임의 크기
            string vAddress = "";
            string value = "";
            int oDataLength = 0;         //입력받은 값의 바이트 배열의 크기.
            byte[] data = null;
            int idx = 0;
            byte[] LengthByteArray = null;
            byte[] AddressByteArray = null;
            byte[] DataByteArray = null;
            //데이터 쓰기일 경우 입력 데이터의 크기를 구한다.
            int nInput = 0;                     //입력받은 데이터가 숫자형일경우 받을 변수
            string strInput = string.Empty;     //입력받은 데이터가 문자형일 경우 받을 변수.

            try
            {
                byte[] command = BitConverter.GetBytes((short)emFunc);      //명령어 읽기,쓰기  //StringToByteArray((int)emFunc, true);  
                byte[] dataType = BitConverter.GetBytes((short)emDatatype); //데이터 타입       //StringToByteArray((int)emDatatype, true);  
                byte[] reserved = BitConverter.GetBytes((short)0);          //예약영역 고정(0x0000)
                byte[] blockcount = BitConverter.GetBytes((short)1);        //블록수 

                //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
                vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

                if (sArg.AddrString.Length > 0)
                    vAddress = CreateValueName(emDatatype, emMemtype, sArg.AddrString);
                else
                {
                    value = sArg.Address.ToString("X");
                    vAddress = CreateValueName(emDatatype, emMemtype, value);
                }

                AddressByteArray = Encoding.UTF8.GetBytes(vAddress);
                LengthByteArray = BitConverter.GetBytes((short)AddressByteArray.Length);

                vLenth += AddressByteArray.Length + LengthByteArray.Length;

                data = new byte[vLenth];

                AddByte(command, ref idx, ref data);
                AddByte(dataType, ref idx, ref data);
                AddByte(reserved, ref idx, ref data);
                AddByte(blockcount, ref idx, ref data);
                AddByte(LengthByteArray, ref idx, ref data);
                AddByte(AddressByteArray, ref idx, ref data);

                return data;
            }
            catch (Exception ex)
            {
                data = null;
                return data;
            }
        }

        //private byte[] CreateReadDataFormat(CommandType emFunc, DataType emDatatype, string pAddress, byte[]msg, MemoryType emMemtype, int pDataCount)
        //{
        //    List<AddressData> lstAddress = new List<AddressData>();
        //    byte[] data = null;
        //    int idx = 0;
        //    int vLenth = 0;

        //    try
        //    {
        //        byte[] command = BitConverter.GetBytes((short)emFunc);          //StringToByteArray((int)emFunc, true);  //명령어 읽기,쓰기
        //        byte[] dataType = BitConverter.GetBytes((short)emDatatype);     //StringToByteArray((int)emDatatype, true);  //데이터 타입
        //        byte[] reserved = BitConverter.GetBytes((short)0);              //예약영역 고정(0x0000)
        //        byte[] blockcount = BitConverter.GetBytes((short)1);            //블록수 

        //        //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
        //        vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

        //        string vAddress = CreateValueName(emDatatype, emMemtype, pAddress);
        //        AddressData XgtAddr = new AddressData();
        //        XgtAddr.AddressString = vAddress;
        //        lstAddress.Add(XgtAddr);

        //        vLenth += XgtAddr.AddressByteArray.Length + XgtAddr.LengthByteArray.Length;

        //        if ((DataType.Continue == emDatatype) && (CommandType.Read == emFunc))
        //        {
        //            vLenth += 2;  //연속읽기 인 경우 2바이트 추가.(데이터 갯수)
        //        }

        //        data = new byte[vLenth];
        //        AddByte(command, ref idx, ref data);
        //        AddByte(dataType, ref idx, ref data);
        //        AddByte(reserved, ref idx, ref data);
        //        AddByte(blockcount, ref idx, ref data);

        //        foreach (AddressData addr in lstAddress)
        //        {
        //            AddByte(addr.LengthByteArray, ref idx, ref data);
        //            AddByte(addr.AddressByteArray, ref idx, ref data);
        //        }

        //        /* 연속 읽기의 경우 읽을 갯수 지정. */
        //        if (DataType.Continue == emDatatype)
        //        {
        //            //데이터 타입이 연속 읽기 인 경우.
        //            byte[] vDataCount = BitConverter.GetBytes((short)pDataCount);
        //            AddByte(vDataCount, ref idx, ref data);
        //        }
        //    }
        //    catch(Exception ex)
        //    {

        //    }

        //    return data;
        //}



        //어플리케이션 데이터 WRITE 포맷 만들기
        //private byte[] CreateWriteDataFormat(XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XgtAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
        private byte[] CreateWriteDataFormat(CommandType emFunc, DataType emDatatype, ITNTSendArgs sArg, MemoryType emMemtype, int pDataCount)
        {
            int vLenth = 0;  //데이타 포맷 프레임의 크기
            string vAddress = "";
            string value = "";
            short oDataLength = 0;         //입력받은 값의 바이트 배열의 크기.
            byte[] data = null;
            int idx = 0;
            byte[] LengthByteArray = null;
            byte[] AddressByteArray = null;
            byte[] DataByteArray = null;
            //데이터 쓰기일 경우 입력 데이터의 크기를 구한다.
            int nInput = 0;                     //입력받은 데이터가 숫자형일경우 받을 변수
            //string strInput = string.Empty;     //입력받은 데이터가 문자형일 경우 받을 변수.
            int sizetimes = 1;

            try
            {
                byte[] command = BitConverter.GetBytes((short)emFunc);          //StringToByteArray((int)emFunc, true);  //명령어 읽기,쓰기
                byte[] dataType = BitConverter.GetBytes((short)emDatatype);     //StringToByteArray((int)emDatatype, true);  //데이터 타입
                byte[] reserved = BitConverter.GetBytes((short)0);              //예약영역 고정(0x0000)
                byte[] blockcount = BitConverter.GetBytes((short)1);            //블록수 


                switch (emDatatype)
                {
                    case DataType.Bit:
                        sizetimes = 1;
                        break;
                    case DataType.Byte:
                        sizetimes = 1;
                        break;
                    case DataType.Word:
                        sizetimes = 2;
                        break;
                    case DataType.DWord:
                        sizetimes = 4;
                        break;
                    case DataType.LWord:
                        sizetimes = 8;
                        break;
                    case DataType.Continue:  // 연속읽기에는 ByteType만... 
                        sizetimes = 1;
                        break;
                    default:
                        sizetimes = 1;
                        break;
                }

                //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
                vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

                if (sArg.AddrString.Length > 0)
                    vAddress = CreateValueName(emDatatype, emMemtype, sArg.AddrString);
                else
                {
                    value = sArg.Address.ToString("X");
                    vAddress = CreateValueName(emDatatype, emMemtype, value);
                }

                AddressByteArray = Encoding.UTF8.GetBytes(vAddress);
                LengthByteArray = BitConverter.GetBytes((short)AddressByteArray.Length);
                //addr.AddressString = vAddress;

                //object oData = new object();        //입력받은 값이 숫자형인지 문자형이지 확실치 않아 Object 로 선언

                if (sArg.sendString.Length <= 0)
                    value = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize * sizetimes);
                else
                    value = sArg.sendString;

                if (!int.TryParse(value, out nInput))                    //문자형일 경우
                    DataByteArray = Encoding.UTF8.GetBytes(value);
                else                                                    //숫자형일 경우
                    DataByteArray = BitConverter.GetBytes((short)nInput);

                //입력값의 바이트 배열의 크기
                oDataLength = (short)DataByteArray.Length;
                vLenth += AddressByteArray.Length + LengthByteArray.Length + 2 + oDataLength; //데이터 갯수 + 데이터 길이
                data = new byte[vLenth];

                AddByte(command, ref idx, ref data);
                AddByte(dataType, ref idx, ref data);
                AddByte(reserved, ref idx, ref data);
                AddByte(blockcount, ref idx, ref data);
                AddByte(LengthByteArray, ref idx, ref data);
                AddByte(AddressByteArray, ref idx, ref data);

                byte[] count = BitConverter.GetBytes(oDataLength);
                // Array.Reverse(count);
                AddByte(count, ref idx, ref data);
                AddByte(DataByteArray, ref idx, ref data);

                return data;
            }
            catch (Exception ex)
            {
                data = null;
                return data;
            }
        }


        //private byte[] CreateWriteDataFormat(CommandType emFunc, DataType emDatatype, string pAddress, MemoryType emMemtype, int pDataCount)
        //private byte[] CreateWriteDataFormat(CommandType emFunc, DataType emDatatype, string pAddress, byte[]msg, MemoryType emMemtype, int pDataCount)
        //{

        //    int vLenth = 0;  //데이타 포맷 프레임의 크기

        //    byte[] command = BitConverter.GetBytes((short)emFunc);     //StringToByteArray((int)emFunc, true);  //명령어 읽기,쓰기
        //    byte[] dataType = BitConverter.GetBytes((short)emDatatype);//StringToByteArray((int)emDatatype, true);  //데이터 타입

        //    byte[] reserved = BitConverter.GetBytes((short)0);  //예약영역 고정(0x0000)
        //    byte[] blockcount = BitConverter.GetBytes((short)1); //블록수 

        //    //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
        //    vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

        //    List<AddressData> lstAddress = new List<AddressData>();
        //    string vAddress = CreateValueName(emDatatype, emMemtype, pAddress);

        //    addr.AddressString = vAddress;


        //    object oData = new object(); //입력받은 값이 숫자형인지 문자형이지 확실치 않아 Object 로 선언
        //    int oDataLength = 0;         //입력받은 값의 바이트 배열의 크기.

        //    //데이터 쓰기일 경우 입력 데이터의 크기를 구한다.
        //    int nInput = 0;    //입력받은 데이터가 숫자형일경우 받을 변수
        //    string strInput = string.Empty;  //입력받은 데이터가 문자형일 경우 받을 변수.

        //    if (!int.TryParse(addr.Data, out nInput))
        //    {
        //        //문자형일 경우
        //        strInput = addr.Data;
        //        oData = Encoding.ASCII.GetBytes(strInput);
        //    }
        //    else
        //    {
        //        //숫자형일 경우
        //        oData = BitConverter.GetBytes((short)nInput);

        //    }

        //    if (emDatatype == DataType.Bit)
        //    {
        //        addr.DataByteArray = new byte[1];
        //        addr.DataByteArray[0] = ((byte[])oData)[0];
        //    }
        //    else
        //    {
        //        addr.DataByteArray = (byte[])oData;
        //    }

        //    //입력값의 바이트 배열의 크기
        //    oDataLength = ((byte[])oData).Length;

        //    vLenth += addr.AddressByteArray.Length + addr.LengthByteArray.Length + 2 + oDataLength; //데이터 갯수 + 데이터 길이

        //    lstAddress.Add(addr);

        //    foreach (AddressData addr in pAddressList)
        //    {

        //    }

        //    if (DataType.Continue == emDatatype)
        //    {
        //        vLenth += 2;  //연속읽기 인 경우 2바이트 추가.(데이터 갯수)
        //    }

        //    byte[] data = new byte[vLenth];


        //    int idx = 0;
        //    AddByte(command, ref idx, ref data);
        //    AddByte(dataType, ref idx, ref data);
        //    AddByte(reserved, ref idx, ref data);
        //    AddByte(blockcount, ref idx, ref data);


        //    foreach (AddressData addr in lstAddress)
        //    {
        //        AddByte(addr.LengthByteArray, ref idx, ref data);
        //        AddByte(addr.AddressByteArray, ref idx, ref data);
        //    }



        //    foreach (AddressData addr in lstAddress)
        //    {
        //        //데이터 쓰기일 경우
        //        byte[] count = BitConverter.GetBytes((short)addr.DataByteArray.Length);

        //        // Array.Reverse(count);
        //        AddByte(count, ref idx, ref data);
        //        AddByte(addr.DataByteArray, ref idx, ref data);
        //    }


        //    return data;
        //}

        /// <summary>
        /// 메모리 어드레스 변수이름을 생성한다.
        /// </summary>
        /// <param name="dataType">데이터타입</param>
        /// <param name="memType">메모리타입</param>
        /// <param name="pAddress">주소번지</param>
        /// <returns></returns>
        private string CreateValueName(DataType dataType, MemoryType memType, string pAddress)
        {
            string vReturn = string.Empty;

            string vMemTypeChar = this.GetMemTypeChar(memType); //메모리타입
            string vDataTypeChar = this.GetTypeChar(dataType);  //데이터타입

            return $"%{vMemTypeChar}{vDataTypeChar}{pAddress}";
        }

        /// <summary>
        /// 데이터 형식에 따른 Char 반환
        /// </summary>
        /// <param name="type">데이터타입</param>
        /// <returns></returns>
        private string GetTypeChar(DataType type)
        {
            string vReturn = string.Empty; // 기본값은  Bit

            switch (type)
            {
                case DataType.Bit:
                    vReturn = Data_TypeClass.Bit;
                    break;
                case DataType.Byte:
                    vReturn = Data_TypeClass.Byte;
                    break;
                case DataType.Word:
                    vReturn = Data_TypeClass.Word;
                    break;
                case DataType.DWord:
                    vReturn = Data_TypeClass.DWord;
                    break;
                case DataType.LWord:
                    vReturn = Data_TypeClass.LWord;
                    break;
                case DataType.Continue:  // 연속읽기에는 ByteType만... 
                    vReturn = Data_TypeClass.Byte;
                    break;
                default:
                    vReturn = Data_TypeClass.Bit;
                    break;
            }

            return vReturn;
        }

        /// <summary>
        /// 메모리 타입에에 따른 Char 반환
        /// </summary>
        /// <param name="type">메모리타입</param>
        /// <returns></returns>
        private string GetMemTypeChar(MemoryType type)
        {
            string vReturn = string.Empty;
            switch (type)
            {
                case MemoryType.InternalContact:
                    vReturn = Memory_TypeClass.InternalContact;
                    break;
                case MemoryType.KeepContact:
                    vReturn = Memory_TypeClass.KeepContact;
                    break;
                case MemoryType.SystemFlag:
                    vReturn = Memory_TypeClass.SystemFlag;
                    break;
                case MemoryType.AnalogRegister:
                    vReturn = Memory_TypeClass.AnalogRegister;
                    break;
                case MemoryType.HighLink:
                    vReturn = Memory_TypeClass.HighLink;
                    break;
                case MemoryType.P2PAddress:
                    vReturn = Memory_TypeClass.P2PAddress;
                    break;
                case MemoryType.FlashMemory:
                    vReturn = Memory_TypeClass.FlashMemory;
                    break;
            }

            return vReturn;
        }

        /// <summary>
        /// 바이트 합치기
        /// </summary>
        /// <param name="item">개별바이트</param>
        /// <param name="idx">전체바이트에 개별바이트를 합칠 인덱스</param>
        /// <param name="header">전체바이트</param>
        /// <returns>전체 바이트 </returns>
        private byte[] AddByte(byte[] item, ref int idx, ref byte[] header)
        {
            Array.Copy(item, 0, header, idx, item.Length);
            idx += item.Length;

            return header;
        }
    }
}
