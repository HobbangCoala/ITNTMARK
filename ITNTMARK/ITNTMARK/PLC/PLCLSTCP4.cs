using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    class PLCLSTCP4
    {
        PLCDataArrivedCallbackHandler callbackHandler;
        PLCConnectionStatusChangedEventHandler connectionHandler;

        public const int PLC_MODE_WRITE = 1;
        public const int PLC_MODE_READ = 0;

        const int RETURN_SIZE = 8;

        //READ STATUS
        public string PLC_ADDRESS_SIGNAL = "0";
        public string PLC_ADDRESS_CARTYPE = "1";
        public string PLC_ADDRESS_LINKSTATUS = "2";
        public string PLC_ADDRESS_AUTOMANUAL = "3";
        public string PLC_ADDRESS_FRAMETYPE = "4";
        public string PLC_ADDRESS_PCERROR = "5";
        public string PLC_ADDRESS_SEQUENCE = "6";
        //public string PLC_ADDRESS_BODYNUM = "6";
        public string PLC_ADDRESS_CHINA = "14";                 //0 = Normal, 1 = China
        public string PLC_ADDRESS_USELASERNO = "15";
        public string PLC_ADDRESS_VIN = "4";
        public string PLC_ADDRESS_BODYNO = "4";

        //WRITE STATUS
        public string PLC_ADDRESS_MATCHRESULT = "16";
        public string PLC_ADDRESS_MARKSTATUS = "17";
        public string PLC_ADDRESS_VISIONRESULT = "18";
        public string PLC_ADDRESS_MARKSTATUS_2 = "19";
        public string PLC_ADDRESS_VISIONRESULT_2 = "20";
        public string PLC_ADDRESS_SETLINK = "21";
        public string PLC_ADDRESS_SETAIR = "22";
        public string PLC_ADDRESS_SCANCOMPLETE = "23";
        public string PLC_ADDRESS_MESCOUNTERROR = "24";
        public string PLC_ADDRESS_REQMOVEROBOT = "25";
        public string PLC_ADDRESS_SETERRORCODE = "26";
        public string PLC_ADDRESS_LASERPOWEROFF = "527";

        //READ STATUS
        public int PLC_LENGTH_SIGNAL = 2;
        public int PLC_LENGTH_CARTYPE = 2;
        public int PLC_LENGTH_LINKSTATUS = 2;
        public int PLC_LENGTH_AUTOMANUAL = 2;
        public int PLC_LENGTH_FRAMETYPE = 2;
        public int PLC_LENGTH_PCERROR = 2;
        public int PLC_LENGTH_SEQUENCE = 2;
        public int PLC_LENGTH_BODYNUM = 2;
        public int PLC_LENGTH_CHINA = 2;
        public int PLC_LENGTH_USELASERNO = 2;
        public int PLC_LENGTH_VIN = 19;
        public int PLC_LENGTH_BODYNO = 2;

        //WRITE STATUS
        public int PLC_LENGTH_MATCHRESULT = 16;
        public int PLC_LENGTH_MARKSTATUS = 17;
        public int PLC_LENGTH_VISIONRESULT = 18;
        public int PLC_LENGTH_MARKSTATUS_2 = 19;
        public int PLC_LENGTH_VISIONRESULT_2 = 20;
        public int PLC_LENGTH_SETLINK = 21;
        public int PLC_LENGTH_SETAIR = 22;
        public int PLC_LENGTH_SCANCOMPLETE = 23;
        public int PLC_LENGTH_MESCOUNTERROR = 24;
        public int PLC_LENGTH_REQMOVEROBOT = 25;
        public int PLC_LENGTH_SETERRORCODE = 26;
        public int PLC_LENGTH_LASERPOWEROFF = 1;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        public const int SIGNAL_PLC2PC_NORMAL = 0;
        public const int SIGNAL_PLC2PC_NEXTVIN = 1;                // 각인준비 OK _ 넥스트 빈
        public const int SIGNAL_PLC2PC_MARK_1 = 2;                // 각인 시작
        public const int SIGNAL_PLC2PC_VISION_1 = 4;                // 각인 완료
        public const int SIGNAL_PLC2PC_NOFRAME = 8;                // 비상 정지
        public const int SIGNAL_PLC2PC_EMERGENCY_STOP = 16;               // 비상 정지
        public const int SIGNAL_PLC2PC_MARK_2 = 32;               // 각인 시작
        public const int SIGNAL_PLC2PC_VISION_2 = 64;               // 각인 완료
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        protected RingBuffer recvBuffer;

        bool isConnected = false;
        private Socket workSocket;
        private const string _companyID = "LSIS-XGT";

        byte[] RecvFrameData = new byte[2048];
        int RecvFrameLength = 0;
        byte commError = 0;

        byte[] RecvFrameDataHex = new byte[2048];
        int RecvFrameLengthHex = 0;

        bool DoingPLCStatusThread = false;
        Thread _plcStatusThread = null;

        string sHeaderData = "00FF00";
        string sHeaderData4Digits = "00FF";
        csConnStatus connStatus = csConnStatus.Closed;
        //byte[] HeaderData = new byte[4] { (byte)'0', (byte)'0', (byte)'F', (byte)'F' };
        private readonly object bufferLock = new object();

        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        static bool doingCmdFlag = false;
        string sDeviceName = DeviceName.Device_PLC;
        string sDeviceCode = DeviceCode.Device_PLC;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public PLCLSTCP4(PLCDataArrivedCallbackHandler callback)
        {
            callbackHandler = callback;
            connectionHandler = null;
            recvBuffer = new RingBuffer(4096);
            LoadOption();
        }


        public PLCLSTCP4(PLCDataArrivedCallbackHandler callback, PLCConnectionStatusChangedEventHandler statusCallback)
        {
            callbackHandler = callback;
            connectionHandler = statusCallback;
            recvBuffer = new RingBuffer(4096);
            LoadOption();
        }

        private void LoadOption()
        {
            string value = "";
            string datasize = "1";

            try
            {
                datasize = "1";

                //READ
                Util.GetPrivateProfileValue("ADDRESS", "SIGNAL", "0", ref PLC_ADDRESS_SIGNAL, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "CARTYPE", "1", ref PLC_ADDRESS_CARTYPE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "LINKSTATUS", "2", ref PLC_ADDRESS_LINKSTATUS, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "AUTOMANUAL", "3", ref PLC_ADDRESS_AUTOMANUAL, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "FRAMETYPE", "4", ref PLC_ADDRESS_FRAMETYPE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "PCERROR", "5", ref PLC_ADDRESS_PCERROR, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SEQUENCE", "6", ref PLC_ADDRESS_SEQUENCE, Constants.PLCVAL_INI_FILE);
                //Util.GetPrivateProfileValue("ADDRESS", "BODYNUM", "6", ref PLC_ADDRESS_BODYNUM, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "CHINA", "14", ref PLC_ADDRESS_CHINA, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "USELASERNO", "15", ref PLC_ADDRESS_USELASERNO, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "VIN", "4", ref PLC_ADDRESS_VIN, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "BODYNO", "4", ref PLC_ADDRESS_BODYNO, Constants.PLCVAL_INI_FILE);

                //WRITE
                Util.GetPrivateProfileValue("ADDRESS", "MATCHRESULT", "16", ref PLC_ADDRESS_MATCHRESULT, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "MARKSTATUS", "17", ref PLC_ADDRESS_MARKSTATUS, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "VISIONRESULT", "18", ref PLC_ADDRESS_VISIONRESULT, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "MARKSTATUS2", "19", ref PLC_ADDRESS_MARKSTATUS_2, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "VISIONRESULT2", "20", ref PLC_ADDRESS_VISIONRESULT_2, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SETLINK", "21", ref PLC_ADDRESS_SETLINK, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SETAIR", "22", ref PLC_ADDRESS_SETAIR, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SCANCOMPLETE", "23", ref PLC_ADDRESS_SCANCOMPLETE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "MESCOUNTERROR", "24", ref PLC_ADDRESS_MESCOUNTERROR, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "REQMOVEROBOT", "25", ref PLC_ADDRESS_REQMOVEROBOT, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SETERRORCODE", "26", ref PLC_ADDRESS_SETERRORCODE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "LASERPOWEROFF", "527", ref PLC_ADDRESS_LASERPOWEROFF, Constants.PLCVAL_INI_FILE);

                //READ
                Util.GetPrivateProfileValue("LENGTH", "SIGNAL", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_SIGNAL);
                Util.GetPrivateProfileValue("LENGTH", "CARTYPE", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_CARTYPE);
                Util.GetPrivateProfileValue("LENGTH", "LINKSTATUS", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_LINKSTATUS);
                Util.GetPrivateProfileValue("LENGTH", "AUTOMANUAL", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_AUTOMANUAL);
                Util.GetPrivateProfileValue("LENGTH", "FRAMETYPE", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_FRAMETYPE);
                Util.GetPrivateProfileValue("LENGTH", "PCERROR", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_PCERROR);
                Util.GetPrivateProfileValue("LENGTH", "SEQUENCE", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_SEQUENCE);
                Util.GetPrivateProfileValue("LENGTH", "BODYNUM", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_BODYNUM);
                Util.GetPrivateProfileValue("LENGTH", "CHINA", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_CHINA);
                Util.GetPrivateProfileValue("LENGTH", "BODYNO", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_BODYNO);
                Util.GetPrivateProfileValue("LENGTH", "USELASERNO", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_USELASERNO);
                //Util.GetPrivateProfileValue("LENGTH", "RESERVED02", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_RESERV02);
                Util.GetPrivateProfileValue("LENGTH", "VIN", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_VIN);

                //WRITE
                Util.GetPrivateProfileValue("LENGTH", "MATCHRESULT", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_MATCHRESULT);
                Util.GetPrivateProfileValue("LENGTH", "MARKSTATUS", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_MARKSTATUS);
                Util.GetPrivateProfileValue("LENGTH", "VISIONRESULT", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_VISIONRESULT);
                Util.GetPrivateProfileValue("LENGTH", "MARKSTATUS2", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_MARKSTATUS_2);
                Util.GetPrivateProfileValue("LENGTH", "VISIONRESULT2", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_VISIONRESULT_2);
                Util.GetPrivateProfileValue("LENGTH", "SETLINK", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_SETLINK);
                Util.GetPrivateProfileValue("LENGTH", "SETAIR", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_SETAIR);
                Util.GetPrivateProfileValue("LENGTH", "SCANCOMPLETE", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_SCANCOMPLETE);
                Util.GetPrivateProfileValue("LENGTH", "MESCOUNTERROR", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_MESCOUNTERROR);
                Util.GetPrivateProfileValue("LENGTH", "REQMOVEROBOT", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_REQMOVEROBOT);
                Util.GetPrivateProfileValue("LENGTH", "SETERRORCODE", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_SETERRORCODE);
                Util.GetPrivateProfileValue("LENGTH", "LASERPOWEROFF", datasize, ref value, Constants.PLCVAL_INI_FILE); int.TryParse(value, out PLC_LENGTH_LASERPOWEROFF);
            }
            catch (Exception ex)
            {

            }
        }

        public async Task<int> OpenPLCAsync(short timeout)
        {
            string className = "PLCLSTCP";
            string funcName = "OpenPLCAsync";
            int retval = 0;
            string IPAddr = "", Port = "", Slot = "";
            int iport = 0;
            StateObject state = new StateObject();
            Stopwatch sw = new Stopwatch();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
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

                    statusArg.newstatus = csConnStatus.Connected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }

                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return 0;
                }
                else
                {
                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }

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
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

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
                    statusArg.newstatus = csConnStatus.Connected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }

                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return 0;
                }
                else
                {
                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }

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
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

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
                    statusArg.newstatus = csConnStatus.Connected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }

                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return 0;
                }
                else
                {
                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }

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
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

            if (threadflag != 0)
                DoingPLCStatusThread = false;
            if (isConnected == true)
            {
                if ((GetWorkSocket() != null) && (GetWorkSocket().Connected))
                {
                    //GetWorkSocket().Disconnect(false);
                    GetWorkSocket().Shutdown(SocketShutdown.Both);
                    GetWorkSocket().Close();
                    SetWorkSocket(null);
                }
            }
            isConnected = false;
            statusArg.newstatus = csConnStatus.Disconnected;
            if (statusArg.newstatus != connStatus)
            {
                statusArg.oldstatus = connStatus;
                connStatus = statusArg.newstatus;
                connectionHandler?.Invoke(statusArg);
            }
        }

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

        private void SetWorkSocket(Socket sc)
        {
            Volatile.Write(ref workSocket, sc);
        }

        private Socket GetWorkSocket()
        {
            return Volatile.Read(ref workSocket);
        }


        public void MakeData()
        {
            byte[] RX = new byte[1024];
            string NAK_ErrorCotent;
            CommandType ResponseType;
            DataType dataType;
            List<AddressData> DataList = new List<AddressData>();
            string ResponseStatus;
            short BlockCount;
            string Message;

            try
            {
                if (RX == null) return;

                NAK_ErrorCotent = string.Empty;

                List<AddressData> lstData = new List<AddressData>();

                //RX 응답 중 19번째가지는 헤더프레임 정보, 20번째부터 데이터 프레임.
                //받은 응답이 없으면, 즉 에러가 발생시 

                if (RX?.Length == 0)
                {
                    NAK_ErrorCotent = "서버로 부터 응답을 받지 못했습니다.";
                    return;
                }
                if (RX?[20] == (short)CommandType.ReadResponse)
                {
                    ResponseType = CommandType.ReadResponse;
                }
                else if (RX?[20] == (short)CommandType.WriteResponse)
                {
                    ResponseType = CommandType.WriteResponse;
                }
                else
                {
                    ResponseType = CommandType.ReadResponse;
                }

                byte[] vdataType = new byte[2];
                vdataType[0] = RX[22];
                vdataType[1] = RX[23];


                foreach (DataType item in Enum.GetValues(typeof(DataType)))
                {
                    string vb = BitConverter.ToString(BitConverter.GetBytes((short)item));
                    string va = BitConverter.ToString(vdataType);
                    if (vb.Equals(va))
                    {
                        dataType = item;
                        break;
                    }
                }


                if (RX?[26] != 0x00 || RX?[27] != 0x00)
                {
                    //에러응답
                    ResponseStatus = "NAK";
                    DataList = lstData;
                    //에러메세지 확인
                    switch (RX?[28])
                    {
                        case 0x12:
                            NAK_ErrorCotent = "(0x12)연속읽기인데 바이트 타입이 아닌 경우";
                            break;
                        case 0x11:
                            NAK_ErrorCotent = "(0x11)변수명이 4보다 작거나 16보다 큰 경우와 같이 어드레스에 관련된 에러";
                            break;
                        case 0x10:
                            NAK_ErrorCotent = "(0x10)없는 디바이스를 요청하는 경우와 같이 디바이스에 관련된 에러";
                            break;
                        case 0x78:
                            NAK_ErrorCotent = "(0x78)unknown command";
                            break;
                        case 0x77:
                            NAK_ErrorCotent = "(0x77)체크섬 오류";
                            break;
                        case 0x76:
                            NAK_ErrorCotent = "(0x76)length 정보 오류";
                            break;
                        case 0x75:
                            NAK_ErrorCotent = "(0x75) “LGIS-GLOFA”가 아니거나 “LSIS-XGT”가 아닌 경우";
                            break;
                        case 0x24:
                            NAK_ErrorCotent = "(0x24)데이터 타입 에러";
                            break;
                        default:
                            NAK_ErrorCotent = "알려지지 않은 에러코드, LS산전 고객센터에 문의 / " + Convert.ToString(RX[28]);
                            break;

                    }
                }
                else
                {
                    //28번 index 부터 데이터로 정의
                    int index = 28;

                    //정상응답
                    ResponseStatus = "ACK";
                    byte[] blockCount = new byte[2];  //블럭카운터
                    byte[] dataByteCount = new byte[2];  //데이터 크기
                    int unitdatatype = BitConverter.ToInt16(vdataType, 0);
                    unitdatatype = (unitdatatype == 0x0014) ? 0x0001 : unitdatatype;    //continuous read

                    byte[] data = new byte[unitdatatype];  //블럭카운터

                    Array.Copy(RX, index, blockCount, 0, 2);
                    BlockCount = BitConverter.ToInt16(blockCount, 0);

                    index = index + 2;

                    //블럭카운터 만큼의 데이터 갯수가 존재한다.

                    //Read일 경우 데이터 생성
                    if (ResponseType == CommandType.ReadResponse)
                    {
                        for (int i = 0; i < BlockCount; i++)
                        {
                            Array.Copy(RX, index, dataByteCount, 0, 2);
                            int biteSize = BitConverter.ToInt16(dataByteCount, 0); //데이터 크기.

                            index = index + 2;
                            int continueloop = biteSize / unitdatatype;

                            for (int j = 0; j < continueloop; j++)
                            {
                                Array.Copy(RX, index, data, 0, unitdatatype);

                                index = index + unitdatatype;  //다음 인덱스 

                                string dataContent = BitConverter.ToString(data).Replace("-", String.Empty);

                                AddressData dataValue = new AddressData();
                                dataValue.Data = dataContent;
                                dataValue.DataByteArray = data;

                                lstData.Add(dataValue);
                            }
                        }
                    }
                    DataList = lstData;

                }
            }
            catch (Exception ex)
            {

                Message = "Error: " + ex.Message.ToString() + "AAA";
            }


        }


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
            byte[] tmpdata = new byte[512];

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

                    recvBuffer.Look(ref look, count+32);
                    index = Array.IndexOf(look, (byte)'L');
                    if (index < 0)
                    {
                        recvBuffer.Get(ref look, count);
                        return;
                    }
                    else if (index > 0)
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

                    if (count < frameLeng)
                    {
                        return;
                    }

                    if ((look[26] != 0x00) || (look[27] != 0x00))
                    {
                        commError = look[28];
                        RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                        recvBuffer.Get(ref tmpdata, count);
                        return;
                    }
                    else
                        commError = 0;

                    //unitdatatype = BitConverter.ToInt16(vdataType, 0);

                    //unitdatatype = (unitdatatype == 0x0014) ? 0x0001 : unitdatatype;    //continuous read

                    if (look?[20] == (short)CommandType.ReadResponse)
                        Array.Copy(look, frameLeng - 2, dataValue, 0, 2);

                    //short sval = ((short)(dataValue[0] << 8 + dataValue[1]));
                    //string strval = sval.ToString("D4");
                    framedata[2] = dataValue[1];
                    framedata[3] = dataValue[0];

                    RecvFrameLengthHex = dataValue.Length;
                    Array.Copy(dataValue, RecvFrameDataHex, RecvFrameLengthHex);

                    string tmps = BitConverter.ToString(framedata).Replace("-", string.Empty);
                    byte[] tmpby = Encoding.UTF8.GetBytes(tmps);

                    int leng = tmpby.Length;
                    if (tmpby.Length > RETURN_SIZE)
                        leng = RETURN_SIZE;
                    Array.Copy(tmpby, RecvFrameData, leng);


                    //commError = 0x00;
                    recvBuffer.Get(ref look, frameLeng);
                    ////Array.Copy(framedata, 0, RecvFrameData, 0, 8);
                    ////RecvFrameData[6] = (byte)(dataValue[1] + 0x30);
                    ////RecvFrameData[7] = (byte)(dataValue[0] + 0x30);
                    //Array.Copy(dataValue, 0, RecvFrameData, 6, 2);
                    RecvFrameLength = RETURN_SIZE;
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

        protected virtual void OnPLCStatusDataArrivedCallbackFunc(ITNTResponseArgs e)
        {
            //PLCDataArrivedEventHandler handler = PLCDataArrivedEventFunc;
            //if (handler != null)
            //    handler(this, e);
            callbackHandler?.Invoke(e);
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
                retval4status = await ReadSignalFromPLCAsync(2);
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



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///

        #region LS PLC DATA FORMAT
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
                if (pDataCount <= 0)
                    pDataCount = 1;

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

        private byte[] CreateWriteDataFormat2(CommandType emFunc, DataType emDatatype, ITNTSendArgs sArg, MemoryType emMemtype, int pDataCount)
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
                if (pDataCount <= 0)
                    pDataCount = 1;

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

                DataByteArray = new byte[sArg.dataSize];

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

                byte[] count = BitConverter.GetBytes(pDataCount);
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
                if (pDataCount <= 0)
                    pDataCount = 1;
                byte[] command = BitConverter.GetBytes((short)emFunc);      //명령어 읽기,쓰기  //StringToByteArray((int)emFunc, true);  
                byte[] dataType = BitConverter.GetBytes((short)emDatatype); //데이터 타입       //StringToByteArray((int)emDatatype, true);  
                byte[] reserved = BitConverter.GetBytes((short)0);          //예약영역 고정(0x0000)
                byte[] blockcount = BitConverter.GetBytes((short)1);        //블록수 
                byte[] datacount = BitConverter.GetBytes((short)pDataCount);        //블록수 

                //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
                if(emDatatype == DataType.Continue)
                    vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length + datacount.Length;
                else
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
                if (emDatatype == DataType.Continue)
                    AddByte(datacount, ref idx, ref data);

                return data;
            }
            catch (Exception ex)
            {
                data = null;
                return data;
            }
        }

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

        #endregion


        ///////////////////////////////////////////////////////////////////////////////////////////////////
        #region COMMUNICATION FUNCTIONS
        ///
        private void InitializeExecuteCommand()
        {
            //SendFlag = SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
            RecvFrameData.Initialize();
            RecvFrameLength = 0;
            RecvFrameDataHex.Initialize();
            RecvFrameLengthHex = 0;
            //NAKError = 0;
        }

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

                if (isConnected == false)
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

        public async Task<ITNTResponseArgs> SendCommandMsgAsync(byte RWFlag, ITNTSendArgs sArg, int loglevel, int timeout = 2, DataType dataType=DataType.Word)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //int retval = 0;
            //int i = 0;
            //byte[] sendmsg = new byte[1024];
            byte[] sendmsg = null;
            String smsg = "";
            byte[] sdata = null;
            byte[] header = null;
            int idx = 0;
            string sCurrentFunc = "SEND COMMAND";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, "START : " + sArg.AddrString);
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
                    sdata = CreateWriteDataFormat(CommandType.Write, dataType, sArg, MemoryType.InternalContact, sArg.dataSize);
                    header = CreateHeader(1, sdata.Length);
                }
                else
                {
                    sdata = CreateReadDataFormat(CommandType.Read, dataType, sArg, MemoryType.InternalContact, sArg.dataSize);
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

        public ITNTResponseArgs SendCommandMsg(byte RWFlag, ITNTSendArgs sArg, int loglevel, int timeout = 2, DataType dataType = DataType.Word)
        {
            string className = "PLCLSTCP";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsg";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, "START");

            //ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTResponseArgs retval = new ITNTResponseArgs();
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
                        retval.execResult = (int)COMMUNICATIONERROR.ERR_BUFFER_SIZE_ERROR;
                        return retval;
                    }
                    sdata = CreateWriteDataFormat(CommandType.Write, dataType, sArg, MemoryType.InternalContact, 0);
                    header = CreateHeader(1, sdata.Length);
                }
                else
                {
                    sdata = CreateReadDataFormat(CommandType.Read, dataType, sArg, MemoryType.InternalContact, 0);
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
                retval.execResult = Send(sendmsg, sendmsg.Length);
                if (retval.execResult <= 0)
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
                retval.execResult = ex.HResult;
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


                //if (commError != 0)
                //{
                //    retval.execResult = commError;
                //    //retval.sErrorMessage = "commError";
                //    retval.errorInfo.devErrorInfo.sErrorMessage = "commError";
                //    return retval;
                //}

                Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
                Array.Copy(RecvFrameDataHex, retval.recvBuffHex, RecvFrameLengthHex);
                //retval.recvString = Encoding.ASCII.GetString(retval.recvBuffer, 0, 8);
                //retval.recvString = BitConverter.ToString(retval.recvBuffer, 0, 8).Replace("-", String.Empty);
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, RETURN_SIZE);
                retval.recvSize = RETURN_SIZE;
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
                retval.recvSize = RETURN_SIZE;
                //Array.Copy(state.buffer, retval.recvBuffer, recvLeng);
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, RETURN_SIZE);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RWFlag"></param>
        /// <param name="sArg"></param>
        /// <param name="loglevel"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(byte RWFlag, ITNTSendArgs sArg, int timeout = 2, DataType dataType = DataType.Word, CancellationToken token =default)
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
                    retval = await SendCommandMsgAsync(RWFlag, sArg, sArg.loglevel, timeout, dataType);
                    if (retval.execResult <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    retval = await RecvResponseMsgAsync(RWFlag, sArg.loglevel, timeout);
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

        public async Task<ITNTResponseArgs> ExecuteCommandFuncAsync(byte RWFlag, ITNTSendArgs sArg, int timeout = 2, DataType dataType = DataType.Word, CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ExecuteCommandMsgAsync";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sErrorDevFunc = "EXECUTE COMMAND";

            try
            {
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sErrorDevFunc;

				//if (siemensPLC == null)
                //{
                //    retval.errorInfo.rawErrorCode = -1;
                //    retval.execResult = -1;
                //    retval.errorInfo.sErrorMessage = "SIMENS PLC OBJECT IS NULL";
                //    return retval;
                //}

                //if (siemensPLC.IsConnected == false)
                //{
                //    retval.errorInfo.rawErrorCode = -2;
                //    retval.execResult = -2;
                //    retval.errorInfo.sErrorMessage = "SIMENS PLC DOES NOT CONNECTED";
                //    return retval;
                //}

                retval = await ExecuteCommandMsgAsync(RWFlag, sArg, timeout, dataType, token);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(RWFlag, sArg, timeout, dataType, token);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(RWFlag, sArg, timeout, dataType, token);
                }

                if ((retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR) && (retval.execResult != (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY))
                {
                    ClosePLC(0);
                    isConnected = false;

                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public ITNTResponseArgs ExecuteCommandMsgSync(byte RWFlag, ITNTSendArgs sArg, int timeout = 2)
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

                //doingCmdFlag = true;
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    InitializeExecuteCommand();
                    retval = SendCommandMsg(RWFlag, sArg, sArg.loglevel, timeout);
                    if (retval.execResult <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    retval = RecvResponseMsg(RWFlag, sArg.loglevel, timeout);
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

        public ITNTResponseArgs ExecuteCommandFuncSync(byte RWFlag, ITNTSendArgs sArg, int timeout = 2, DataType dataType = DataType.Word)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ExecuteCommandMsgAsync";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                //if (siemensPLC == null)
                //{
                //    retval.errorInfo.rawErrorCode = -1;
                //    retval.execResult = -1;
                //    retval.errorInfo.sErrorMessage = "SIMENS PLC OBJECT IS NULL";
                //    return retval;
                //}

                //if (siemensPLC.IsConnected == false)
                //{
                //    retval.errorInfo.rawErrorCode = -2;
                //    retval.execResult = -2;
                //    retval.errorInfo.sErrorMessage = "SIMENS PLC DOES NOT CONNECTED";
                //    return retval;
                //}

                retval = ExecuteCommandMsgSync(RWFlag, sArg, timeout);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = ExecuteCommandMsgSync(RWFlag, sArg, timeout);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = ExecuteCommandMsgSync(RWFlag, sArg, timeout);
                }

                if ((retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR) && (retval.execResult != (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY))
                {
                    ClosePLC(0);
                    isConnected = false;

                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        connectionHandler?.Invoke(statusArg);
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //        public async Task<ITNTResponseArgs> ReadSignalFromPLC()
        //        {
        //            //byte[] recv = new byte[32];
        //            ITNTResponseArgs retval = new ITNTResponseArgs(256);
        //            ITNTSendArgs sArg = new ITNTSendArgs();
        //            try
        //            {
        //#if TEST_DEBUG_PLC
        //            string value = "";
        //            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
        //            //value = Encoding.UTF8.GetBytes(value);
        //            retval.recvString = value;
        //            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
        //            retval.recvSize = value.Length;
        //#else
        //                sArg.AddrString = PLC_ADDRESS_SIGNAL;
        //                retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
        //                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
        //                {
        //                    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
        //                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
        //                        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 3, 2);
        //                }
        //#endif
        //            }
        //            catch (Exception ex)
        //            {
        //                retval.execResult = ex.HResult;
        //            }
        //            return retval;
        //        }

        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg, DataType dataType=DataType.Word, CancellationToken token=default)
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

                if (sendArg.timeout == 0)
                    sendArg.timeout = 2;
                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sendArg, sendArg.timeout, dataType, token);
                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                //retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sendArg, sendArg.loglevel);
                //if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //{
                //    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sendArg, sendArg.loglevel);
                //    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sendArg, sendArg.loglevel);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCAsync(string strAdd, DataType dataType = DataType.Word, CancellationToken token = default)
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

                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                //retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                //if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //{
                //    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                //    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                //}
                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 2, dataType, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }


        public ITNTResponseArgs ReadPLC(ITNTSendArgs sendArg, DataType dataType = DataType.Word)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sArg = new ITNTSendArgs();
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                Thread.Sleep(500);
#else

                if (sendArg.timeout == 0)
                    sendArg.timeout = 2;

                retval = ExecuteCommandFuncSync(PLC_MODE_READ, sendArg, sendArg.timeout, dataType);
                //sArg.AddrString = strAdd;

                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                //retval = ExecuteCommandMsgSync(PLC_MODE_READ, sArg, 2);
                //if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //{
                //    retval = ExecuteCommandMsgSync(PLC_MODE_READ, sArg, 2);
                //    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //        retval = ExecuteCommandMsgSync(PLC_MODE_READ, sArg, 2);
                //}
                //retval = ExecuteCommandFuncSync(PLC_MODE_READ, sArg);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public ITNTResponseArgs ReadPLC(string strAdd, DataType dataType = DataType.Word)
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

                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                //retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                //if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //{
                //    retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                //    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //        retval = await ExecuteCommandMsgAsync(PLC_MODE_READ, sArg, 2);
                //}
                retval = ExecuteCommandFuncSync(PLC_MODE_READ, sArg, 2, dataType);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg, DataType dataType = DataType.Word, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //string addr = "";
            try
            {
#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sendArg, 1, dataType, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync(string sAddress, string sWriteData, DataType dataType = DataType.Word, CancellationToken token=default)
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

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, dataType, token);

                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                //retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
                //if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //{
                //    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
                //    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                //        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
                //}
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
                retval = ExecuteCommandMsgSync(PLC_MODE_WRITE, sArg, 0);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = ExecuteCommandMsgSync(PLC_MODE_WRITE, sArg, 0);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = ExecuteCommandMsgSync(PLC_MODE_WRITE, sArg, 0);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }


        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_SIGNAL;
                int.TryParse(PLC_ADDRESS_SIGNAL, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_SIGNAL;
                //sArg.plcDBNum = PLC_DBNUM_SIGNAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    stmp = retval.recvBuffer[0].ToString("X2");
                //    retval.recvString = sHeaderData + stmp;
                //    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                //    if (retdata.Length >= RETURN_SIZE)
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //    else
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_CARTYPE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_CARTYPE;
                //sArg.plcDBNum = PLC_DBNUM_CARTYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CARTYPE", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType4(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_CARTYPE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_CARTYPE;
                //sArg.plcDBNum = PLC_DBNUM_CARTYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CARTYPE", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadLinkStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_LINKSTATUS;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_LINKSTATUS;
                //sArg.plcDBNum = PLC_DBNUM_LINKSTATUS;
                //int.TryParse(sArg.AddrString, out sArg.Address);

#if TEST_DEBUG_PLC
                string value = "00FF0000";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_LINKSTATUS", value, ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadAutoSignalFromPLC(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_AUTOMANUAL;
                int.TryParse(PLC_ADDRESS_AUTOMANUAL, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_AUTOMANUAL;
                //sArg.plcDBNum = PLC_DBNUM_AUTOMANUAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_AUTOMANUAL", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    stmp = retval.recvBuffer[0].ToString("X2");
                //    retval.recvString = sHeaderData + stmp;
                //    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                //    if (retdata.Length >= RETURN_SIZE)
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //    else
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                //    //retval.recvSize = RETURN_SIZE;
                //    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //    //    tmpSize = HeaderData.Length;
                //    //else
                //    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                //    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCFrameType(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_FRAMETYPE;
                int.TryParse(PLC_ADDRESS_FRAMETYPE, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_FRAMETYPE;
                //sArg.plcDBNum = PLC_DBNUM_FRAMETYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_FRAMETYPE", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    stmp = retval.recvBuffer[0].ToString("X2");
                //    retval.recvString = sHeaderData + stmp;
                //    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                //    if (retdata.Length >= RETURN_SIZE)
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //    else
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                //    //retval.recvSize = RETURN_SIZE;
                //    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //    //    tmpSize = HeaderData.Length;
                //    //else
                //    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                //    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCPCError(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_PCERROR;
                int.TryParse(PLC_ADDRESS_PCERROR, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_PCERROR;
                //sArg.plcDBNum = PLC_DBNUM_PCERROR;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_PCERROR", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    stmp = retval.recvBuffer[0].ToString("X2");
                //    retval.recvString = sHeaderData + stmp;
                //    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                //    if (retdata.Length >= RETURN_SIZE)
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //    else
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                //    //retval.recvSize = RETURN_SIZE;
                //    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //    //    tmpSize = HeaderData.Length;
                //    //else
                //    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                //    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence(int loglevel = 0, CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ReadPLCSequence";
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            byte bytmp = 0;
            short shtmp = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_SEQUENCE;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_SEQUENCE;
                //sArg.plcDBNum = PLC_DBNUM_SEQUENCE;
                //sArg.dataSize = 4;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQUENCE", "", ref value, "TEST.ini");
                retval.recvString = value;
                //retval.recvSize = value.Length;
                byte[] tmp = Encoding.UTF8.GetBytes(retval.recvString);

                //retval.recvBuffer[0] = (byte)'0';
                //retval.recvBuffer[1] = (byte)'0';
                //retval.recvBuffer[2] = (byte)'0';
                //retval.recvBuffer[3] = (byte)'0';
                Array.Copy(tmp, 0, retval.recvBuffer, 0, tmp.Length);

                //Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);
                retval.recvSize = RETURN_SIZE;

                //retval.recvBuffer[0] = 0x00;
                //retval.recvBuffer[1] = 0x00;
                //retval.recvBuffer[2] = 0x82;
                //retval.recvBuffer[3] = 0x30;

                //for (int i = 0; i < 4; i++)
                //{
                //    if (i > retval.recvBuffer.Length)
                //        continue;
                //    bytmp = retval.recvBuffer[i];
                //    stmp = bytmp.ToString("X2");
                //    retval.recvString += stmp;
                //}

#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvString = "";
                //    retval.recvSize = RETURN_SIZE;

                //    string temp = retval.recvBuffer[0].ToString("X2") + retval.recvBuffer[1].ToString("X2") + retval.recvBuffer[2].ToString("X2") + retval.recvBuffer[3].ToString("X2");
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "BEFORE : " + temp, Thread.CurrentThread.ManagedThreadId);
                //    Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);

                //    temp = retval.recvBuffer[0].ToString("X2") + retval.recvBuffer[1].ToString("X2") + retval.recvBuffer[2].ToString("X2") + retval.recvBuffer[3].ToString("X2");
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AFTER : " + temp, Thread.CurrentThread.ManagedThreadId);

                //    for (int i = 0; i < 4; i++)
                //    {
                //        if (i > retval.recvBuffer.Length)
                //            continue;
                //        bytmp = retval.recvBuffer[i];
                //        stmp = bytmp.ToString("X2");
                //        retval.recvString += stmp;
                //    }

                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "STRING : " + retval.recvString, Thread.CurrentThread.ManagedThreadId);

                //    //shtmp = BitConverter.ToInt16(retval.recvBuffer, 0);
                //    //retval.recvString = "00FF" + shtmp.ToString("D4");



                //    //stmp = retval.recvBuffer[0].ToString("X2");
                //    //retval.recvString = sHeaderData + stmp;
                //    //byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                //    //if (retval.recvString.Length < RETURN_SIZE)

                //    //Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                //    //retval.recvSize = RETURN_SIZE;
                //    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //    //    tmpSize = HeaderData.Length;
                //    //else
                //    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                //    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence4(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            byte bytmp = 0;
            byte[] bytmps;
            short shtmp = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_SEQUENCE;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_SEQUENCE;
                //sArg.plcDBNum = PLC_DBNUM_SEQUENCE;
                //sArg.dataSize = 4;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                retval.recvSize = RETURN_SIZE;
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQUENCE", "", ref value, "TEST.ini");
                retval.recvString = value;
                bytmps = new byte[value.Length+1];
                Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, bytmps, 0);
                Array.Copy(bytmps, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = value.Length;

                //Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);

                //retval.recvBuffer[0] = 0x00;
                //retval.recvBuffer[1] = 0x00;
                //retval.recvBuffer[2] = 0x82;
                //retval.recvBuffer[3] = 0x30;

                //for (int i = 0; i < 4; i++)
                //{
                //    if (i > retval.recvBuffer.Length)
                //        continue;
                //    bytmp = retval.recvBuffer[i];
                //    stmp = bytmp.ToString("X2");
                //    retval.recvString += stmp;
                //}
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    short sdata = BitConverter.ToInt16(retval.recvBuffer, 0);
                //    stmp = sdata.ToString("X4");
                //    retval.recvString = "00FF" + stmp;
                //    byte[] retdata = new byte[retval.recvString.Length + 1];
                //    Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, retdata, 0);
                //    if (retval.recvString.Length >= RETURN_SIZE)
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //    else
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retval.recvString.Length);

                //    //retval.recvString = "";
                //    //retval.recvSize = RETURN_SIZE;

                //    //shtmp = BitConverter.ToInt16(retval.recvBuffer, 0);
                //    //stmp = shtmp.ToString("X4");

                //    //retval.recvString = "00FF" + stmp;

                //    ////byte[] bytmp;
                //    //Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, bytmps, 0);

                //    //temp = retval.recvBuffer[0].ToString("X2") + retval.recvBuffer[1].ToString("X2") + retval.recvBuffer[2].ToString("X2") + retval.recvBuffer[3].ToString("X2");
                //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCSIEMENSTCP", "ReadAutoSignalFromPLC", "AFTER : " + temp, Thread.CurrentThread.ManagedThreadId);

                //    //for (int i = 0; i < 4; i++)
                //    //{
                //    //    if (i > retval.recvBuffer.Length)
                //    //        continue;
                //    //    bytmp = retval.recvBuffer[i];
                //    //    stmp = bytmp.ToString("X2");
                //    //    retval.recvString += stmp;
                //    //}

                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCSIEMENSTCP", "ReadAutoSignalFromPLC", "STRING : " + retval.recvString, Thread.CurrentThread.ManagedThreadId);
                //}
#endif
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCSIEMENSTCP", "ReadAutoSignalFromPLC", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCChinaFlag(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            short shtmp = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_CHINA;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_CHINA;
                //sArg.plcDBNum = PLC_DBNUM_CHINA;
                //sArg.dataSize = 1;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CHINA", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    shtmp = BitConverter.ToInt16(retval.recvBuffer, 0);
                //    retval.recvString = "00FF" + shtmp.ToString("D4");


                //    //stmp = retval.recvBuffer[0].ToString("X2");
                //    //retval.recvString = sHeaderData + stmp;
                //    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                //    if (retdata.Length >= RETURN_SIZE)
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //    else
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                //    //retval.recvSize = RETURN_SIZE;
                //    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //    //    tmpSize = HeaderData.Length;
                //    //else
                //    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                //    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

//        public async Task<ITNTResponseArgs> ReadPLCBodyNumber(int loglevel = 0, CancellationToken token = default)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs(16);
//            ITNTSendArgs sArg = new ITNTSendArgs(16);
//            //byte[] tmpData = new byte[RETURN_SIZE];
//            //int tmpSize = 0;
//            string stmp = "";
//            byte bytmp = 0;

//            try
//            {
//                sArg.AddrString = PLC_ADDRESS_BODYNUM;
//                int.TryParse(sArg.AddrString, out sArg.Address);
//                sArg.dataSize = PLC_LENGTH_BODYNUM;
//                //sArg.plcDBNum = PLC_DBNUM_BODYNUM;
//                //sArg.dataSize = 4;
//                sArg.loglevel = loglevel;

//#if TEST_DEBUG_PLC
//                string value = "";
//                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_BODYNUM", "00FF0000", ref value, "TEST.ini");
//                //value = Encoding.UTF8.GetBytes(value);
//                retval.recvString = value;
//                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
//                retval.recvSize = value.Length;
//#else

//                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 2, DataType.Word, token);
//                //if (retval.execResult == 0)
//                //{
//                //    retval.recvSize = RETURN_SIZE;
//                //    Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);

//                //    for (int i = 0; i < retval.recvSize; i++)
//                //    {
//                //        if (i > retval.recvBuffer.Length)
//                //            continue;
//                //        bytmp = retval.recvBuffer[i];
//                //        stmp = bytmp.ToString("X2");
//                //        retval.recvString += stmp;
//                //    }

//                //    //retval.recvSize = RETURN_SIZE;
//                //    ////stmp = retval.recvBuffer[0].ToString("X2");
//                //    //stmp = Encoding.UTF8.GetString(retval.recvBuffer, 0, 4);
//                //    //retval.recvString = "00FF" + stmp;
//                //    //byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
//                //    //if (retdata.Length >= RETURN_SIZE)
//                //    //    Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
//                //    //else
//                //    //    Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
//                //}
//#endif
//            }
//            catch (Exception ex)
//            {
//                retval.execResult = ex.HResult;
//            }
//            return retval;
//        }

        public async Task<ITNTResponseArgs> ReadVINAsync(CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ReadPLCAsync";
            byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sArg = new ITNTSendArgs();
            //string addr = "";
            try
            {
                sArg.AddrString = PLC_ADDRESS_VIN;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_VIN;
                //sArg.plcDBNum = PLC_DBNUM_VIN;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_VIN", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
                await Task.Delay(500);
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.msgtype = 0;

                //    if (retval.recvSize >= sArg.dataSize)
                //    {
                //        Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);
                //        //Array.Copy(recvdata, retval.recvBuffer, sArg.dataSize);
                //        retval.recvSize = sArg.dataSize;
                //        retval.recvString = "00FF" + Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //    }
                //    else
                //    {
                //        Array.Reverse(retval.recvBuffer, 0, retval.recvSize);
                //        if (retval.recvSize > 0)
                //        {
                //            Array.Copy(retval.recvBuffer, retval.recvBuffer, retval.recvSize);
                //            //retval.recvSize = retval.recvSize;
                //            retval.recvString = "00FF" + Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //        }
                //        //else
                //        //{
                //        //    continue;
                //        //}
                //    }


                //    //retval.recvString.Reverse();
                //    //stmp = retval.recvString;
                //    //retval.recvString = "00FF" + stmp;
                //    retval.recvSize += 4;

                //    //stmp = retval.recvBuffer[0].ToString("X2");
                //    //retval.recvString = sHeaderData + stmp;
                //    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                //    if (retdata.Length >= retval.recvSize)
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retval.recvSize);
                //    else
                //        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.errorInfo.rawErrorCode = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = ex.Message;
            }

            return retval;
        }

//        public async Task<ITNTResponseArgs> ReadPLCBodyNumber(int loglevel = 0, CancellationToken token = default)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs(16);
//            ITNTSendArgs sArg = new ITNTSendArgs(16);
//            //byte[] tmpData = new byte[RETURN_SIZE];
//            //int tmpSize = 0;
//            string stmp = "";
//            byte bytmp = 0;

//            try
//            {
//                sArg.AddrString = PLC_ADDRESS_BODYNUM;
//                int.TryParse(sArg.AddrString, out sArg.Address);
//                sArg.dataSize = PLC_LENGTH_BODYNUM;
//                //sArg.plcDBNum = PLC_DBNUM_BODYNUM;
//                //sArg.dataSize = 4;
//                sArg.loglevel = loglevel;

//#if TEST_DEBUG_PLC
//                string value = "";
//                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_BODYNUM", "00FF0000", ref value, "TEST.ini");
//                //value = Encoding.UTF8.GetBytes(value);
//                retval.recvString = value;
//                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
//                retval.recvSize = value.Length;
//#else

//                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 2, DataType.Word, token);
//                //if (retval.execResult == 0)
//                //{
//                //    retval.recvSize = RETURN_SIZE;
//                //    Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);

//                //    for (int i = 0; i < retval.recvSize; i++)
//                //    {
//                //        if (i > retval.recvBuffer.Length)
//                //            continue;
//                //        bytmp = retval.recvBuffer[i];
//                //        stmp = bytmp.ToString("X2");
//                //        retval.recvString += stmp;
//                //    }

//                //    //retval.recvSize = RETURN_SIZE;
//                //    ////stmp = retval.recvBuffer[0].ToString("X2");
//                //    //stmp = Encoding.UTF8.GetString(retval.recvBuffer, 0, 4);
//                //    //retval.recvString = "00FF" + stmp;
//                //    //byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
//                //    //if (retdata.Length >= RETURN_SIZE)
//                //    //    Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
//                //    //else
//                //    //    Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
//                //}
//#endif
//            }
//            catch (Exception ex)
//            {
//                retval.execResult = ex.HResult;
//            }
//            return retval;
//        }


        public async Task<ITNTResponseArgs> ReadBodyNum(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_BODYNO;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_BODYNO;
                //sArg.plcDBNum = PLC_DBNUM_LINKSTATUS;
                //int.TryParse(sArg.AddrString, out sArg.Address);

#if TEST_DEBUG_PLC
                string value = "00FF0000";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_BODYNO", value, ref value, "TEST.ini");
                retval.recvString = value;
                byte[] tmp = Encoding.UTF8.GetBytes(retval.recvString);
                Array.Copy(tmp, 0, retval.recvBuffHex, 0, tmp.Length);
                Array.Copy(tmp, 0, retval.recvBuffer, 0, tmp.Length);
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = loglevel;
                for (int i = 0; i < 5; i++)
                {
                    ITNTResponseArgs tmp = new ITNTResponseArgs();
                    tmp = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
                    if (tmp.execResult != 0)
                        return retval;
                    else
                    {
                        retval.recvString += tmp.recvString;
                        Array.Copy(tmp.recvBuffHex, 0, retval.recvBuffHex, i * 2, 2);
                        retval.recvSize += tmp.recvSize;
                        sArg.Address++;
                        sArg.AddrString = sArg.Address.ToString();
                    }
                }
#endif
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffHex, 0, 10);// tmp.recvBuffHex
                retval.recvSize = retval.recvString.Length;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadUseLaserNum(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_USELASERNO;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_USELASERNO;
                //sArg.plcDBNum = PLC_DBNUM_LINKSTATUS;
                //int.TryParse(sArg.AddrString, out sArg.Address);

#if TEST_DEBUG_PLC
                string value = "00FF0000";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_USELASERNO", value, ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = loglevel;

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }



        /// ////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="result"></param>
        /// <param name="loglevel"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<ITNTResponseArgs> SendMatchingResult(byte result, int loglevel = 0, CancellationToken token = default)
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_MATCHRESULT;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (result == PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_OK)
                    sArg.sendBuffer[0] = 1;
                else
                    sArg.sendBuffer[0] = 2;

                sArg.dataSize = PLC_LENGTH_MATCHRESULT;
                //sArg.plcDBNum = PLC_DBNUM_MATCHRESULT;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                value = string.Format("{0:D4}", result);
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MATCHRESULT", value, "TEST.ini");
                retval.recvString = value;
                retval.recvSize = value.Length;
#else

                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = 0;

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //        tmpSize = HeaderData.Length;
                //    else
                //        tmpSize = RETURN_SIZE - HeaderData.Length;

                //    Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            byte[] tmpData = new byte[RETURN_SIZE];
            int tmpSize = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_FRAMETYPE;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_FRAMETYPE;
                //sArg.plcDBNum = PLC_DBNUM_FRAMETYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = 0;

#if TEST_DEBUG_PLC
                string value = "";
                value = string.Format("{0:D4}", frameType);
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_FRAMETYPE", value, "TEST.ini");
                retval.recvString = value;
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    //retval.recvSize = RETURN_SIZE;
                //    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //    //    tmpSize = HeaderData.Length;
                //    //else
                //    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                //    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}

                ////string val = frameType.PadLeft(4, '0');
                //byte[] temp = Encoding.UTF8.GetBytes(frameType);
                //Array.Copy(temp, sArg.sendBuffer, temp.Length);
                //sArg.sendString = frameType;
                //sArg.dataSize = temp.Length;

                //retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2, token);

                ////retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                ////if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                ////{
                ////    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                ////    if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                ////        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                ////}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendPCError2PLC(string plcvalue, int loglevel, CancellationToken token = default)
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;

            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", plcvalue);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D210", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_PCERROR;
                byte[] temp = Encoding.UTF8.GetBytes(plcvalue);
                Array.Copy(temp, sArg.sendBuffer, temp.Length);
                sArg.sendString = plcvalue;
                sArg.dataSize = PLC_LENGTH_PCERROR;
                //sArg.plcDBNum = PLC_DBNUM_PCERROR;
                //sArg.dataSize = temp.Length;

                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = 0;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //        tmpSize = HeaderData.Length;
                //    else
                //        tmpSize = RETURN_SIZE - HeaderData.Length;

                //    Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendSignal(byte signal, int loglevel = 0, CancellationToken token = default)
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
                sArg.AddrString = PLC_ADDRESS_SIGNAL;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.sendBuffer[0] = signal;
                sArg.dataSize = PLC_LENGTH_SIGNAL;
                //sArg.plcDBNum = PLC_DBNUM_SIGNAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 2, DataType.Word, token);
                //retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                //if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                //{
                //    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                //    if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                //        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status, byte order, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                if (order == 2)
                {
                    sArg.AddrString = PLC_ADDRESS_MARKSTATUS_2;
                    sArg.dataSize = PLC_LENGTH_MARKSTATUS_2;
                    //sArg.plcDBNum = PLC_DBNUM_MARKSTATUS_2;
                }
                else
                {
                    sArg.AddrString = PLC_ADDRESS_MARKSTATUS;
                    sArg.dataSize = PLC_LENGTH_MARKSTATUS;
                    //sArg.plcDBNum = PLC_DBNUM_MARKSTATUS;
                }

                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (status == PLCMELSEQSerial.PLC_MARK_STATUS_DOING)
                    sArg.sendBuffer[0] = 1;
                else if (status == PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE)
                    sArg.sendBuffer[0] = 2;
                else
                    sArg.sendBuffer[0] = 0;

                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + status.ToString("X4");
                if (order == 2)
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MARKSTATUS_2", value, "TEST.ini");
                else
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MARKSTATUS", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result, byte order = 0, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];
            try
            {
                if (order == 2)
                {
                    sArg.AddrString = PLC_ADDRESS_VISIONRESULT_2;
                    sArg.dataSize = PLC_LENGTH_VISIONRESULT_2;
                    //sArg.plcDBNum = PLC_DBNUM_VISIONRESULT_2;
                }
                else
                {
                    sArg.AddrString = PLC_ADDRESS_VISIONRESULT;
                    sArg.dataSize = PLC_LENGTH_VISIONRESULT;
                    //sArg.plcDBNum = PLC_DBNUM_VISIONRESULT;
                }

                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (result == "O")
                    //sArg.sendString = "1";
                    sArg.sendBuffer[0] = 1;
                else
                    //sArg.sendString = "2";
                    sArg.sendBuffer[0] = 2;
                //sArg.dataSize = DATA_SIZE;
                //sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF";
                if (result == "O")
                    value += 1.ToString("X4");
                else
                    value += 2.ToString("X4");
                if (result == "O")
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_VISIONRESULT_2", value, "TEST.ini");
                else
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_VISIONRESULT", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus_2nd(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_MARKSTATUS_2;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (status == PLCMELSEQSerial.PLC_MARK_STATUS_DOING)
                    sArg.sendBuffer[0] = 1;
                else if (status == PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE)
                    sArg.sendBuffer[0] = 2;
                else
                    sArg.sendBuffer[0] = 0;

                sArg.dataSize = PLC_LENGTH_MARKSTATUS_2;
                //sArg.plcDBNum = PLC_DBNUM_MARKSTATUS_2;
                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + status.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MARKSTATUS_2", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult_2nd(string result, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_VISIONRESULT_2;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (result == "O")
                    sArg.sendBuffer[0] = 1;
                else
                    sArg.sendBuffer[0] = 2;
                sArg.dataSize = PLC_LENGTH_VISIONRESULT_2;
                //sArg.plcDBNum = PLC_DBNUM_VISIONRESULT_2;
                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF";
                if (result == "O")
                    value += 1.ToString("X4");
                else
                    value += 2.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_VISIONRESULT_2", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            //byte[] recv = new byte[256];
            //string value = "";
            try
            {
                sArg.AddrString = address;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[1] = error;

                sArg.dataSize = PLC_LENGTH_SETERRORCODE;
                //sArg.plcDBNum = PLC_DBNUM_SETERRORCODE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                //value = error.ToString("X4");
                //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                //retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = 4;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            //byte[] recv = new byte[256];
            //string value = "";
            try
            {
                sArg.AddrString = PLC_ADDRESS_SETERRORCODE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[1] = error;
                sArg.dataSize = PLC_LENGTH_SETERRORCODE;
                //sArg.plcDBNum = PLC_DBNUM_SETERRORCODE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                //value = error.ToString("X4");
                //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                //retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = 4;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
                sArg.AddrString = PLC_ADDRESS_REQMOVEROBOT;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = distance;
                sArg.dataSize = PLC_LENGTH_REQMOVEROBOT;
                //sArg.plcDBNum = PLC_DBNUM_REQMOVEROBOT;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = distance.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendScanComplete(byte scanstatus, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
                sArg.AddrString = PLC_ADDRESS_SCANCOMPLETE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = scanstatus;
                sArg.dataSize = PLC_LENGTH_SCANCOMPLETE;
                //sArg.plcDBNum = PLC_DBNUM_SCANCOMPLETE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                //string value = error.ToString("X4");
                //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                //retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = 4;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SetLinkAsync(byte link, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_SETLINK;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = link;
                sArg.dataSize = PLC_LENGTH_SETLINK;
                //sArg.plcDBNum = PLC_DBNUM_SETLINK;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + link.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendCountWanring(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            try
            {
                sArg.AddrString = PLC_ADDRESS_SETLINK;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = status;
                sArg.dataSize = PLC_LENGTH_SETLINK;
                //sArg.plcDBNum = PLC_DBNUM_SETLINK;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + status.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif

            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = ex.Message;
                retval.errorInfo.rawErrorCode = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendAirAsync(byte air, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_SETAIR;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = air;
                sArg.dataSize = PLC_LENGTH_SETAIR;
                //sArg.plcDBNum = PLC_DBNUM_SETAIR;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + air.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendPLCSignalAsync(byte signal, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_SIGNAL;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_SIGNAL;
                //sArg.plcDBNum = PLC_DBNUM_SIGNAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = signal;
                //sArg.dataSize = PLC_LENGTH_SETAIR;
                //sArg.plcDBNum = PLC_DBNUM_SETAIR;
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + signal.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
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
                if (IP.Length > 0)
                    Util.WritePrivateProfileValue("PLCCOMM", "SERVERIP", IP, Constants.PARAMS_INI_FILE);
                if (Port > 0)
                    Util.WritePrivateProfileValue("PLCCOMM", "SERVERPORT", Port.ToString(), Constants.PARAMS_INI_FILE);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SetLaserPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_LASERPOWEROFF;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = status;
                sArg.dataSize = PLC_LENGTH_LASERPOWEROFF;
                //sArg.plcDBNum = PLC_DBNUM_SETAIR;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + status.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_LASERPOWEROFF", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, DataType.Word, token);
#endif
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

    }
}
