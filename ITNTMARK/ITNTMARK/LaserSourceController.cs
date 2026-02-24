using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ITNTUTIL;
using ITNTCOMMON;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014


namespace ITNTMARK
{
    [Flags]
    //public enum LASERSTATUS : UInt32
    //{
    //    CommandBufferOverload           = 0b0000_0000_0000_0000_0000_0000_0000_0001, // 0
    //    OverHeat                        = 0b0000_0000_0000_0000_0000_0000_0000_0010,
    //    EmissionOnOff                   = 0b0000_0000_0000_0000_0000_0000_0000_0100,
    //    HighBackReflectionLevel         = 0b0000_0000_0000_0000_0000_0000_0000_1000,
    //    AnalogPowerControlEnableDisable = 0b0000_0000_0000_0000_0000_0000_0001_0000,
    //    PulseTooLong                    = 0b0000_0000_0000_0000_0000_0000_0010_0000, // skip 6,7
    //    AimingBeamOnOff                 = 0b0000_0000_0000_0000_0000_0001_0000_0000, // 8
    //    PulseTooShort                   = 0b0000_0000_0000_0000_0000_0010_0000_0000,
    //    PulseModeCWMode                 = 0b0000_0000_0000_0000_0000_0100_0000_0000,
    //    PowerSupplyOnOff                = 0b0000_0000_0000_0000_0000_1000_0000_0000,
    //    ModulationEnabledDisabled       = 0b0000_0000_0000_0000_0001_0000_0000_0000, // 12 skip 13,14
    //    Emmision3SecStartUpInOut        = 0b0000_0000_0000_0000_1000_0000_0000_0000,
    //    GateModeEnableDisable           = 0b0000_0000_0000_0001_0000_0000_0000_0000, // 16
    //    HighPulseEnergy                 = 0b0000_0000_0000_0010_0000_0000_0000_0000,
    //    HWEmissionCtrlEnabledDisabled   = 0b0000_0000_0000_0100_0000_0000_0000_0000,
    //    PowerSupplyFailure              = 0b0000_0000_0000_1000_0000_0000_0000_0000,
    //    FrontPanelDisplayLockedUnlocked = 0b0000_0000_0001_0000_0000_0000_0000_0000,
    //    KeyswitchIsRemOnPosition        = 0b0000_0000_0010_0000_0000_0000_0000_0000,
    //    WaveformPulseModeOnOff          = 0b0000_0000_0100_0000_0000_0000_0000_0000,
    //    DutyCycleTooHigh                = 0b0000_0000_1000_0000_0000_0000_0000_0000,
    //    LowTemerature                   = 0b0000_0001_0000_0000_0000_0000_0000_0000, // 24
    //    PowerSupplyAlarm                = 0b0000_0010_0000_0000_0000_0000_0000_0000, // 25 skip 26
    //    HWAimingBeamCtrlEnabledDisabled = 0b0000_1000_0000_0000_0000_0000_0000_0000, // 27 skip 28
    //    CriticalError                   = 0b0010_0000_0000_0000_0000_0000_0000_0000,
    //    FiberInterlockActiveOK          = 0b0100_0000_0000_0000_0000_0000_0000_0000,
    //    HighAveragePower                = 0b1000_0000_0000_0000_0000_0000_0000_0000, // 31

    //    StatusNormalOn = PulseModeCWMode | GateModeEnableDisable | FrontPanelDisplayLockedUnlocked | KeyswitchIsRemOnPosition | WaveformPulseModeOnOff,
    //    StatusNormalOff = AnalogPowerControlEnableDisable | PowerSupplyOnOff | ModulationEnabledDisabled | HWEmissionCtrlEnabledDisabled | HWAimingBeamCtrlEnabledDisabled | FiberInterlockActiveOK,
    //    //StatusError = OverHeat | PulseTooShort | HighPulseEnergy | PowerSupplyFailure | DutyCycleTooHigh | LowTemerature | PowerSupplyAlarm | CriticalError | HighAveragePower
    //    StatusError = OverHeat | PulseTooLong | PulseTooShort | HighPulseEnergy | PowerSupplyFailure | DutyCycleTooHigh | LowTemerature | PowerSupplyAlarm | CriticalError | HighAveragePower
    //}

    public enum LASERSTATUS : UInt32
    {
        CommandBufferOverload           = 0b0000_0000_0000_0000_0000_0000_0000_0001, // 0
        OverHeat                        = 0b0000_0000_0000_0000_0000_0000_0000_0010,
        EmissionOnOff                   = 0b0000_0000_0000_0000_0000_0000_0000_0100,
        HighBackReflectionLevel         = 0b0000_0000_0000_0000_0000_0000_0000_1000,
        AnalogPowerControlEnableDisable = 0b0000_0000_0000_0000_0000_0000_0001_0000,
        PulseTooLong                    = 0b0000_0000_0000_0000_0000_0000_0010_0000, // skip 6,7
        AimingBeamOnOff                 = 0b0000_0000_0000_0000_0000_0001_0000_0000, // 8
        PulseTooShort                   = 0b0000_0000_0000_0000_0000_0010_0000_0000,
        PulseModeCWMode                 = 0b0000_0000_0000_0000_0000_0100_0000_0000,
        ModulationEnabledDisabled       = 0b0000_0000_0000_0000_0001_0000_0000_0000, // 12 skip 13,14

        GateModeEnableDisable           = 0b0000_0000_0000_0001_0000_0000_0000_0000, // 16
        HighPulseEnergy                 = 0b0000_0000_0000_0010_0000_0000_0000_0000,
        HWEmissionCtrlEnabledDisabled   = 0b0000_0000_0000_0100_0000_0000_0000_0000,
        PowerSupplyFailure              = 0b0000_0000_0000_1000_0000_0000_0000_0000,
        DutyCycleTooHigh                = 0b0000_0000_1000_0000_0000_0000_0000_0000,
        LowTemerature                   = 0b0000_0001_0000_0000_0000_0000_0000_0000, // 24
        PowerSupplyAlarm                = 0b0000_0010_0000_0000_0000_0000_0000_0000, // 25 skip 26
        HWAimingBeamCtrlEnabledDisabled = 0b0000_1000_0000_0000_0000_0000_0000_0000, // 27 skip 28
        AimingBeamAlarm                 = 0b0001_0000_0000_0000_0000_0000_0000_0000, // 28
        CriticalError                   = 0b0010_0000_0000_0000_0000_0000_0000_0000,
        FiberInterlockActiveOK          = 0b0100_0000_0000_0000_0000_0000_0000_0000,
        HighAveragePower                = 0b1000_0000_0000_0000_0000_0000_0000_0000, // 31

#if LASER_YLR
        PowerSupplyOnOff                = 0b0000_0000_0000_0000_0000_1000_0000_0000, // 11
        Emission3SecStartUpInOut        = 0b0000_0000_0000_0000_1000_0000_0000_0000, // 15
        FrontPanelDisplayLockedUnlocked = 0b0000_0000_0001_0000_0000_0000_0000_0000,
        KeyswitchIsRemOnPosition        = 0b0000_0000_0010_0000_0000_0000_0000_0000,
        WaveformPulseModeOnOff          = 0b0000_0000_0100_0000_0000_0000_0000_0000,
        //PowerSupplyAlarm                = 0b0000_0010_0000_0000_0000_0000_0000_0000, // 25 skip 26

        StatusNormalOn  = PulseModeCWMode | GateModeEnableDisable | FrontPanelDisplayLockedUnlocked | KeyswitchIsRemOnPosition | WaveformPulseModeOnOff,
        StatusNormalOff = AnalogPowerControlEnableDisable | PowerSupplyOnOff | ModulationEnabledDisabled | HWEmissionCtrlEnabledDisabled | HWAimingBeamCtrlEnabledDisabled | FiberInterlockActiveOK,
        StatusError     = OverHeat | PulseTooLong | PulseTooShort | HighPulseEnergy | PowerSupplyFailure | DutyCycleTooHigh | LowTemerature | PowerSupplyAlarm | CriticalError | HighAveragePower
#else   // YLM Series  
        PowerSupplyOutOfRangeOK             = 0b0000_0000_0000_0000_0000_1000_0000_0000, // 11
        CompatibilityModeEnabledDisabled    = 0b0000_0000_0000_0000_0010_0000_0000_0000, // 13
        GNDLeakge                           = 0b0000_0100_0000_0000_0000_0000_0000_0000, // 26 

        StatusNormalOn = PulseModeCWMode | GateModeEnableDisable,
        StatusNormalOff = AnalogPowerControlEnableDisable | PowerSupplyOutOfRangeOK | ModulationEnabledDisabled | HWEmissionCtrlEnabledDisabled | HWAimingBeamCtrlEnabledDisabled | FiberInterlockActiveOK | CompatibilityModeEnabledDisabled | GNDLeakge | AimingBeamAlarm,
        //StatusError = OverHeat | PulseTooLong | PulseTooShort | HighPulseEnergy | PowerSupplyFailure | DutyCycleTooHigh | LowTemerature | CriticalError | HighAveragePower
        StatusError = CommandBufferOverload | OverHeat | HighBackReflectionLevel | PulseTooLong | PowerSupplyOutOfRangeOK | HighPulseEnergy | PowerSupplyFailure | DutyCycleTooHigh | LowTemerature | PowerSupplyAlarm | GNDLeakge | AimingBeamAlarm | HighAveragePower,
#endif
    }

    //public class CommandStack
    //{
    //    public string command;
    //    public byte[] senddata;

    //    public CommandStack()
    //    {
    //        command = "";
    //        senddata = new byte[128];
    //    }

    //    public CommandStack(int size)
    //    {
    //        command = "";
    //        if (size <= 0)
    //            senddata = new byte[128];
    //        else
    //            senddata = new byte[size];
    //    }
    //}


    public class LaserSourceController
    {
        //public event LaserSourceControllerEventHandler LaserSourceControllerEventFunc;
        public event LaserControllerStatusEventHandler LaserControllerStatusEventFunc;
        public event LaserConnectionStatusChangedEventHandler LaserConnectionStatusChangedEventFunc;

        RingBuffer rb = null;
        StateObject asyncstate = null;
        bool bConnected = false;
        bool doingCmdFlag = false;
        bool doingCmdFlag2 = false;
        string serverIP = "";
        int serverPort = 0;

        private readonly object lockbuff = new object();

        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        byte[] RecvFrameData = new byte[2048];
        int RecvFrameLength = 0;
        string currentCommand = "";

        bool doingthreadStatusLaser = false;
        bool doingthreadPowerLaser = false;
        bool doingthreadTemperatureLaser = false;

        Thread _threadStatus = null;
        Thread _threadPower = null;
        Thread _threadTemperature = null;
        bool doingthread = false;
        bool closeFlag = false;
        string sDeviceCode = DeviceCode.Device_LASER;
        string sDeviceName = DeviceName.Device_LASER;

        csConnStatus currenStatus = csConnStatus.Closed;
        csConnStatus backStatus = csConnStatus.Closed;

        private readonly SemaphoreSlim _commandLock = new SemaphoreSlim(1, 1);
        //CommandStack[] cmdStack = new CommandStack[16];

        //Queue<CommandStack> cmdQueue = new Queue<CommandStack>();
        Queue<string> cmdQueue = new Queue<string>();

        object LockQueue = new object();

        private bool reconnecting = false; // 재접속 중 여부 플래그
        //string sErrBusy = "01";
        //string sErrApp = "02";
        //string sErrSend = "03";
        //string sErrRecv = "04";
        //string sErrExcept = "05";

        public LaserSourceController()//distanceSensorDataArrivedCallbackHandler DataCallback)//, ClientConnectionHandler ConnectCallback)
        {
            asyncstate = new StateObject();
            rb = new RingBuffer(2048);
            //for (int i = 0; i < cmdStack.Length; i++)
            //    cmdStack[i] = new CommandStack();
        }

        public async Task<ITNTResponseArgs> StartClient(int timeoutsec)
        {
            string className = "LaserSourceController";
            string funcName = "StartClient";
            string value = "";
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            string sCurrentFunc = "OPEN LASER";
            // Connect to a remote device.  
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("LASERSOURCE", "SERVERIP", "192.168.3.230", ref value, Constants.PARAMS_INI_FILE);
                //IP = value;
                serverIP = value;
                Util.GetPrivateProfileValue("LASERSOURCE", "SERVERPORT", "10001", ref value, Constants.PARAMS_INI_FILE);
                int.TryParse(value, out serverPort);

#if TEST_DEBUG_LASER
                //doingthread = true;
                //Thread thread = new Thread(new ThreadStart(ThreadPolling));
                //thread.Start();

                doingthreadStatusLaser = true;
                doingthreadPowerLaser = true;
                doingthreadTemperatureLaser = true;

                if(_threadStatus == null)
                    _threadStatus = new Thread(new ThreadStart(ThreadPollingStatus));
                if (_threadPower == null)
                    _threadPower = new Thread(new ThreadStart(ThreadPollingPower));
                if (_threadTemperature == null)
                    _threadTemperature = new Thread(new ThreadStart(ThreadPollingTemperature));

                _threadStatus.Start();
                _threadPower.Start();
                _threadTemperature.Start();

                bConnected = true;
#else
                IPAddress ipAddress = IPAddress.Parse(serverIP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                // Create a TCP/IP socket.  
                asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                //connectDone.WaitOne();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeoutsec))
                {
                    //if (!isConnected)
                    if (asyncstate.workSocket != null)
                    {
                        if (asyncstate.workSocket.Connected)
                        {
                            bConnected = true;
                            break;
                        }
                    }
                    //if (Volatile.Read(ref isConnected) == true)
                    //    break;

                    await Task.Delay(50);
                }
                sw.Stop();
#endif

                if (bConnected == true)
                {
                    retval.execResult = 0;

                    currenStatus = csConnStatus.Connected;
                    if(currenStatus != backStatus)
                    {
                        ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                        e.newstatus = currenStatus;
                        e.oldstatus = backStatus;
                        backStatus = currenStatus;
                        LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                    }
                    return retval;
                }
                else
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "OPEN LASER ERROR = " + "CONNECT FAIL";
                    return retval;
                }
            }
            catch (Exception ex)
            {
                if (!closeFlag && !reconnecting)
                {
                    reconnecting = true;
                    Task.Run(async () => await TryReconnectAsync());
                }

                //Console.WriteLine(e.ToString());
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
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

//        public async Task<ITNTResponseArgs> StartClient(string IP, int port, int timeoutsec)
//        {
//            string className = "LaserSourceController";
//            string funcName = "StartClient";
//            bool bConnected = false;
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//            string sCurrentFunc = "OPEN LASER 2";

//            // Connect to a remote device.  
//            try
//            {
//                serverIP = IP;
//                serverPort = port;

//#if TEST_DEBUG_LASER
//                //doingthread = true;
//                //Thread thread = new Thread(new ThreadStart(ThreadPolling));
//                //thread.Start();

//                bConnected = true;
//#else
//                IPAddress ipAddress = IPAddress.Parse(serverIP);
//                IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

//                // Create a TCP/IP socket.  
//                asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//                // Connect to the remote endpoint.  
//                asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
//                //connectDone.WaitOne();
//                Stopwatch sw = new Stopwatch();
//                sw.Start();
//                while (sw.Elapsed < TimeSpan.FromSeconds(timeoutsec))
//                {
//                    //if (!isConnected)
//                    if (asyncstate.workSocket != null)
//                    {
//                        if (asyncstate.workSocket.Connected)
//                        {
//                            bConnected = true;
//                            break;
//                        }
//                    }
//                    //if (Volatile.Read(ref isConnected) == true)
//                    //    break;

//                    await Task.Delay(50);
//                }
//                sw.Stop();
//#endif

//                if (bConnected == true)
//                {
//                    retval.execResult = 0;
//                    return retval;
//                }
//                else
//                {
//                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
//                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
//                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
//                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
//                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_CAUSE1_DOWN + (-retval.execResult).ToString("X2");
//                    retval.errorInfo.devErrorInfo.sErrorMessage = "OPEN LASER ERROR = " + "CONNECT FAIL";
//                    return retval;

//                    //retval.execResult = (int)TCPCOMMERROR.ERR_PORT_NOT_OPENED;
//                    //retval.errorInfo.sErrorDevice = sDeviceName;
//                    //retval.errorInfo.sErrorDevFunc = sCurrentFunc;
//                    //retval.errorInfo.sErrorCode = sDeviceCode + "D0" + (-retval.execResult).ToString("D2");
//                    //retval.errorInfo.sErrorDevMsg = "OPEN LASER ERROR = " + "CONNECT FAIL";
//                    //return retval;
//                }
//            }
//            catch (Exception ex)
//            {
//                //Console.WriteLine(e.ToString());
//                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
//                retval.execResult = ex.HResult;
//                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
//                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
//                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
//                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
//                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
//                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

//                //retval.execResult = ex.HResult;
//                //retval.errorInfo.sErrorDevice = sDeviceName;
//                //retval.errorInfo.sErrorDevFunc = sCurrentFunc;
//                //retval.errorInfo.sErrorCode = sDeviceCode + "E0" + (-retval.execResult).ToString("D2");
//                //retval.errorInfo.sErrorDevMsg = "OPEN LASER ERROR = " + "EXCEPTION ERROR";
//                return retval;
//            }
//        }

        public void CloseClient(byte closeType)
        {
            string className = "LaserSourceController";
            string funcName = "CloseClient";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "CLOSE LASER";

            try
            {
                if(closeType != 0)
                {
                    closeFlag = true;
                }

                doingthreadTemperatureLaser = false;
                doingthreadPowerLaser = false;
                doingthreadStatusLaser = false;

                //if(_threadStatus != null)
                //    _threadStatus.Join();
                currenStatus = csConnStatus.Disconnected;
                if (currenStatus != backStatus)
                {
                    ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                    e.newstatus = currenStatus;
                    e.oldstatus = backStatus;
                    backStatus = currenStatus;
                    LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                }

                if (asyncstate.workSocket != null)
                {
                    if (asyncstate.workSocket.Connected == true)
                    {
                        //asyncstate.workSocket.Shutdown(SocketShutdown.Both);
                        asyncstate.workSocket.Close();
                    }
                }
                //asyncstate.workSocket = null;
                bConnected = false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }

            //asyncstate.workSocket.Close();
        }

        private async void ConnectCallback(IAsyncResult ar)
        {
            string className = "LaserSourceController";
            string funcName = "ConnectCallback";
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                asyncstate.workSocket = (Socket)ar.AsyncState;
                //SetTcpKeepAlive(asyncstate.workSocket, 1000, 5);
                if (!asyncstate.workSocket.Connected)
                {
                    if (!closeFlag)
                    {
                        currenStatus = csConnStatus.Disconnected;
                        if (currenStatus != backStatus)
                        {
                            ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                            e.newstatus = currenStatus;
                            e.oldstatus = backStatus;
                            backStatus = currenStatus;
                            LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                        }

                        if (!reconnecting)
                        {
                            reconnecting = true;
                            Task.Run(async () => await TryReconnectAsync());
                        }
                    }
                    //else
                    //    CloseClient(1);
                    return;
                }

                asyncstate.workSocket.NoDelay = true;

                // Complete the connection.  
                try
                {
                    asyncstate.workSocket.EndConnect(ar);
                }
                catch(Exception ex)
                {

                }

                //doingthread = true;
                doingthreadStatusLaser = true;
                doingthreadPowerLaser = true;
                doingthreadTemperatureLaser = true;

                if(_threadStatus == null)
                    _threadStatus = new Thread(new ThreadStart(ThreadPollingStatus));
                if (_threadPower == null)
                    _threadPower = new Thread(new ThreadStart(ThreadPollingPower));
                if (_threadTemperature == null)
                    _threadTemperature = new Thread(new ThreadStart(ThreadPollingTemperature));

                _threadStatus.Start();
                _threadPower.Start();
                _threadTemperature.Start();

                bConnected = true;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "isConnected = true", Thread.CurrentThread.ManagedThreadId);
                Receive();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Recv() Start", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //Console.WriteLine(ex.ToString());

                currenStatus = csConnStatus.Disconnected;
                if (currenStatus != backStatus)
                {
                    ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                    e.newstatus = currenStatus;
                    e.oldstatus = backStatus;
                    backStatus = currenStatus;
                    LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                }

                if (!closeFlag && !reconnecting)
                {
                    reconnecting = true;
                    Task.Run(async () => await TryReconnectAsync());
                }
            }
        }


        public async void ThreadPollingStatus()
        {
            string className = "LaserSourceController";
            string funcName = "ThreadPollingStatus";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string[] st;
            LaserControllerStatusEvnetArgs eventArg = new LaserControllerStatusEvnetArgs();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                while (doingthreadStatusLaser)
                {
                    //sendArg = new LaserSourceControllerEvnetArgs();
                    eventArg.Initialize();
                    eventArg.datatype = 0;
                    if ((doingthreadStatusLaser == false) || (closeFlag == true))
                        break;

                    if (closeFlag == true)
                        break;

#if TEST_DEBUG_LASER
                    retval = await ReadDeviceStatus4Debug(2);

                    if(retval.recvString.Length > 0)
                    {
                        eventArg.recvdata1 = retval.recvString;
                        LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                    }
#else
                    retval = await ReadDeviceStatus(2);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN STA 1", Thread.CurrentThread.ManagedThreadId);
                        retval = await ReadDeviceStatus(2);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN STA 2!!!!!!", Thread.CurrentThread.ManagedThreadId);
                            retval = await ReadDeviceStatus(2);
                        }
                    }
                    if (retval.execResult == 0)
                    {
                        st = retval.recvString.Split(':');
                        if (st.Length > 1)
                        {
                            eventArg.recvdata1 = st[1];
                            LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                        }
                        else
                            eventArg.recvdata1 = "ERROR";
                    }
                    //else if(retval.execResult != (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                    else
                    {
                        eventArg.recvdata1 = "ERROR";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                    }
#endif

#if LASER_YLR
                    _ = await ReadDeviceResetStatus(2);
#endif
                    //LaserControllerStatusEventFunc?.Invoke(this, eventArg);

                    if ((doingthreadStatusLaser == false) || (closeFlag == true))
                        break;
                    await Task.Delay(1000);
                    //await MyDeley(1000);
                }
                _threadStatus = null;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _threadStatus = null;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public async void ThreadPollingTemperature()
        {
            string className = "LaserSourceController";
            string funcName = "ThreadPollingTemperature";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string[] st;
            LaserControllerStatusEvnetArgs eventArg = new LaserControllerStatusEvnetArgs();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                while (doingthreadTemperatureLaser)
                {
                    //sendArg = new LaserSourceControllerEvnetArgs();
                    eventArg.Initialize();
                    eventArg.datatype = 2;

                    if ((doingthreadTemperatureLaser == false) || (closeFlag == true))
                        break;
#if TEST_DEBUG_LASER
#else
                    //retval = await ReadLaserTemperature(2);
                    retval = await ReadBoardTemperature(2);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN RBT 1", Thread.CurrentThread.ManagedThreadId);
                        retval = await ReadBoardTemperature(2);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN RBT 2!!!!!!!", Thread.CurrentThread.ManagedThreadId);
                            retval = await ReadBoardTemperature(2);
                        }
                    }
                    if ((retval.execResult == 0) && (retval.recvString.Length >= 4))
                    {
                        eventArg.recvdata1 = retval.recvString.Substring(4);
                        LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                    }
                    else
                    //else if (retval.execResult != (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                    {
                        eventArg.recvdata1 = "ERROR";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                    }
#endif
                    if ((doingthreadTemperatureLaser == false) || (closeFlag == true))
                        break;
                    await Task.Delay(1000);
                    //await MyDeley(1000);
                }
                _threadTemperature = null;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _threadTemperature = null;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public async void ThreadPollingPower()
        {
            string className = "LaserSourceController";
            string funcName = "ThreadPollingPower";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string[] st;
            LaserControllerStatusEvnetArgs eventArg = new LaserControllerStatusEvnetArgs();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                while (doingthreadPowerLaser)
                {
                    if ((doingthreadPowerLaser == false) || (closeFlag == true))
                        break;
                    //sendArg = new LaserSourceControllerEvnetArgs();
                    eventArg.Initialize();
                    eventArg.datatype = 1;

#if TEST_DEBUG_LASER
                    retval = await ReadPeakPower4Debug(2);

                    if(retval.recvString.Length > 0)
                    {
                        eventArg.recvdata2 = retval.recvString;
                        LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                    }
#else
#if LASER_YLR
                    retval = await readouputpower(0);
                    if ((retval.execresult == 0) && (retval.recvstring.length >= 4))
                        eventarg.recvdata1 = retval.recvstring.substring(4);
                    else
                        eventarg.recvdata1 = "error";
#endif
                    retval = await ReadPeakPower(2);
                    if((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN RPP 1", Thread.CurrentThread.ManagedThreadId);
                        retval = await ReadPeakPower(2);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN RPP 2!!!!!!!!!", Thread.CurrentThread.ManagedThreadId);
                            retval = await ReadPeakPower(2);
                        }
                    }
                    if ((retval.execResult == 0) && (retval.recvString.Length >= 4))
                    {
                        eventArg.recvdata2 = retval.recvString.Substring(4);
                        LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                    }
                    else
                    //else if (retval.execResult != (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        eventArg.recvdata2 = "ERROR";
                        //LaserControllerStatusEventFunc?.Invoke(this, eventArg);
                        //eventArg.recvdata2 = retval.recvString.Substring(4);
                    }
                    //LaserControllerStatusEventFunc?.Invoke(this, eventArg);
#endif
                    if ((doingthreadPowerLaser == false) || (closeFlag == true))
                        break;
                    await Task.Delay(1000);
                    //await MyDeley(1000);
                }
                _threadPower = null;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _threadPower = null;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task MyDeley(long milliseconds)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            while (sw.ElapsedMilliseconds < milliseconds)
            {
                await Task.Delay(50);
            }
            sw.Stop();
        }

        public void Receive()
        {
            try
            {
                // Create the state object.  
                //StateObject state = new StateObject();
                //state.workSocket = client;

                // Begin receiving the data from the remote device.  
                //byte[] bytebuff = new byte[1024];
                asyncstate.workSocket.BeginReceive(asyncstate.buffer, 0, asyncstate.buffer.Length, 0, new AsyncCallback(ReceiveCallback), asyncstate);
            }
            catch (Exception ex)
            {
                currenStatus = csConnStatus.Disconnected;
                if (currenStatus != backStatus)
                {
                    ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                    e.newstatus = currenStatus;
                    e.oldstatus = backStatus;
                    backStatus = currenStatus;
                    LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                }

                if (!closeFlag && !reconnecting)
                {
                    reconnecting = true;
                    Task.Run(async () => await TryReconnectAsync());
                }

                Console.WriteLine(ex.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            string className = "LaserSourceController";
            string funcName = "ReceiveCallback";
            string msg = "";
            int readLeng = 0;
            string value = "";
            //int checkFlag = 0;
            //int idx = 0;
            try
            {
                StateObject so = (StateObject)ar.AsyncState;
                Socket s = so.workSocket;
                try
                {
                    try
                    {
                        readLeng = s.EndReceive(ar);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                catch(Exception ex)
                {
                    readLeng = 0;
                }
                if (readLeng > 0)
                {
                    msg = Encoding.UTF8.GetString(so.buffer, 0, readLeng);
                    ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV : " + msg, Thread.CurrentThread.ManagedThreadId);
                    if (msg.Length <= 0)
                        return;

                    lock (lockbuff)
                    {
                        rb.Put(so.buffer, readLeng);
                    }
                    ReceiveCommData();

                    if (closeFlag == false)
                        s.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                }
                else
                {
                    currenStatus = csConnStatus.Disconnected;
                    if (currenStatus != backStatus)
                    {
                        ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                        e.newstatus = currenStatus;
                        e.oldstatus = backStatus;
                        backStatus = currenStatus;
                        LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                    }

                    if (!closeFlag && !reconnecting)
                    {
                        reconnecting = true;
                        Task.Run(async () => await TryReconnectAsync());
                    }

                    //if (closeFlag == false)
                    //    s.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                currenStatus = csConnStatus.Disconnected;
                if (currenStatus != backStatus)
                {
                    ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                    e.newstatus = currenStatus;
                    e.oldstatus = backStatus;
                    backStatus = currenStatus;
                    LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                }

                if (!closeFlag && !reconnecting)
                {
                    reconnecting = true;
                    Task.Run(async () => await TryReconnectAsync());
                }
            }
        }

        private async Task TryReconnectAsync()
        {
            string className = "LaserSourceController";
            string funcName = "TryReconnectAsync";

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECONNECT START", Thread.CurrentThread.ManagedThreadId);
                CloseClient(0); // 기존 연결 닫기

                while (!closeFlag)
                {
                    try
                    {

                        Thread.Sleep(1000);
                        IPAddress ipAddress = IPAddress.Parse(serverIP);
                        IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);
                        asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        //asyncstate.workSocket.Connect(remoteEP);
                        await asyncstate.workSocket.ConnectAsync(remoteEP);

                        bConnected = true;
                        reconnecting = false;

                        asyncstate.workSocket.NoDelay = true;

                        currenStatus = csConnStatus.Connected;
                        if (currenStatus != backStatus)
                        {
                            ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                            e.newstatus = currenStatus;
                            e.oldstatus = backStatus;
                            backStatus = currenStatus;
                            LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                        }

                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECONNECTED SUCCESS", Thread.CurrentThread.ManagedThreadId);

                        Receive(); // 수신 루프 재시작

                        doingthreadStatusLaser = true;
                        doingthreadPowerLaser = true;
                        doingthreadTemperatureLaser = true;

                        if (_threadStatus == null)
                            _threadStatus = new Thread(new ThreadStart(ThreadPollingStatus));
                        if (_threadPower == null)
                            _threadPower = new Thread(new ThreadStart(ThreadPollingPower));
                        if (_threadTemperature == null)
                            _threadTemperature = new Thread(new ThreadStart(ThreadPollingTemperature));

                        _threadStatus.Start();
                        _threadPower.Start();
                        _threadTemperature.Start();

                        return;
                    }
                    catch
                    {
                        //retryCount++;
                        CloseClient(0); // 기존 연결 닫기

                        await Task.Delay(1000);
                    }
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECONNECT FAILED", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  RECONNECT EXCEPTION = {2}", className, funcName, ex.Message, Thread.CurrentThread.ManagedThreadId);
            }
            finally
            {
                reconnecting = false;
            }
        }

        public void ReceiveCommData()
        {
            string className = "LaserSourceController";
            string funcName = "ReceiveCommData";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

            byte tmp = 0;
            int i = 0;
            byte[] look = new byte[2048];
            int idx = 0;
            int pos = -1;
            int leng = 0;
            try
            {
                lock (lockbuff)
                {
                    while ((leng = rb.GetSize()) > 0)
                    {
                        //leng = rb.GetSize();
                        rb.Look(ref look, leng);
                        if ((leng >= 4) && (look.Contains<byte>(0x0D)) && ((pos = Array.IndexOf<byte>(look, 0x0D)) >= 3))
                        {
                            if ((pos = Array.IndexOf<byte>(look, 0x0D)) >= 3)
                            {
                                for (i = 0; i <= pos; i++)
                                {
                                    rb.Get(ref tmp);
                                    if (tmp == 0x0D)
                                    {
                                        RecvFrameData[idx] = tmp;
                                        break;
                                    }
                                    RecvFrameData[idx] = tmp;
                                    idx++;
                                }

                                RecvFrameLength = idx;
                                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                            }
                        }
                        else
                        {
                            if (pos < 0)
                            {
                                rb.Get(ref look, leng);
                            }
                            else
                            {
                                for (i = 0; i <= pos; i++)
                                {
                                    rb.Get(ref tmp);
                                    if (tmp == 0x0D)
                                        break;
                                }
                            }
                        }
                    }
                    //int leng = rb.GetSize();
                    //rb.Look(ref look, leng);
                    //if((leng >= 4) && (look.Contains<byte>(0x0D)) && ((pos = Array.IndexOf<byte>(look, 0x0D)) >= 3))
                    //{
                    //    if((pos = Array.IndexOf<byte>(look, 0x0D)) >= 3)
                    //    {
                    //        for (i = 0; i <= pos; i++)
                    //        {
                    //            rb.Get(ref tmp);
                    //            if (tmp == 0x0D)
                    //                break;
                    //            RecvFrameData[idx] = tmp;
                    //            idx++;
                    //        }

                    //        RecvFrameLength = idx;
                    //        RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                    //    }
                    //}
                    //else
                    //{
                    //    if (pos < 0)
                    //    {
                    //        rb.Get(ref look, leng);
                    //    }
                    //    else
                    //    {
                    //        for (i = 0; i <= pos; i++)
                    //        {
                    //            rb.Get(ref tmp);
                    //            if (tmp == 0x0D)
                    //                break;
                    //        }
                    //    }
                    //}
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-5", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void Send(string data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            // Begin sending the data to the remote device.  
            string className = "LaserSourceController";
            string funcName = "Send";
            string msg = "";
            string value = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RemoteEndPoint = " + asyncstate.workSocket.RemoteEndPoint.ToString(), Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LocalEndPoint = " + asyncstate.workSocket.LocalEndPoint.ToString(), Thread.CurrentThread.ManagedThreadId);

                byte[] byteData = Encoding.ASCII.GetBytes(data);
                msg = Encoding.UTF8.GetString(byteData, 0, byteData.Length);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND : " + msg, Thread.CurrentThread.ManagedThreadId);
                asyncstate.workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                if (closeFlag == false)
                {
                    if ((asyncstate.workSocket != null) && (asyncstate.workSocket.Connected == true))
                    {
                        CloseClient(0);
                    }
                    //asyncstate.workSocket.c
                    Util.GetPrivateProfileValue("LASER", "SERVERIP", "192.168.3.230", ref value, Constants.PARAMS_INI_FILE);
                    //IP = value;
                    serverIP = value;
                    Util.GetPrivateProfileValue("LASER", "SERVERPORT", "10001", ref value, Constants.PARAMS_INI_FILE);
                    int.TryParse(value, out serverPort);

                    IPAddress ipAddress = IPAddress.Parse(serverIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                    // Create a TCP/IP socket.  
                    asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.  
                    //if (closeFlag == false)
                    asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                }
                else
                {
                    if ((asyncstate.workSocket != null) && (asyncstate.workSocket.Connected == true))
                    {
                        CloseClient(1);
                    }
                }
            }
        }


        public ITNTResponseArgs Send(byte[] data, int size, int loglevel)
        {
            string className = "LaserSourceController";
            string funcName = "Send";
            string msg = "";
            SocketFlags flags = new SocketFlags();
            int sendCount = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "SEND COMMAND";

            try
            {
                msg = Encoding.UTF8.GetString(data, 0, size);
                if (asyncstate.workSocket == null)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    //string tmp = Encoding.UTF8.GetString(msg, 0, size);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : ERR_PORT_NOT_OPENED " + msg + ", ING CMD = " + currentCommand, Thread.CurrentThread.ManagedThreadId);
                    currentCommand = "";

                    //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    //retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS NOT OPENED (" + msg + ")";

                    //retval.errorInfo.sErrorDevMsg = "DEVICE BUSY 2 : " + cmd;
                    //retval.errorInfo.sErrorCode = sDeviceCode + "01" + "02";

                    currenStatus = csConnStatus.Disconnected;
                    if (currenStatus != backStatus)
                    {
                        ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                        e.newstatus = currenStatus;
                        e.oldstatus = backStatus;
                        backStatus = currenStatus;
                        LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                    }

                    if (!closeFlag && !reconnecting)
                    {
                        reconnecting = true;
                        Task.Run(async () => await TryReconnectAsync());
                    }

                    return retval;
                }

                if (asyncstate.workSocket.Connected == false)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    //string tmp = Encoding.UTF8.GetString(msg, 0, size);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : ERR_PORT_NOT_OPENED2 " + msg + ", ING CMD = " + currentCommand, Thread.CurrentThread.ManagedThreadId);
                    currentCommand = "";

                    //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + msg + ")";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS NOT OPENED2 (" + msg + ")";

                    //retval.errorInfo.sErrorDevMsg = "DEVICE BUSY 2 : " + cmd;
                    //retval.errorInfo.sErrorCode = sDeviceCode + "01" + "02";

                    currenStatus = csConnStatus.Disconnected;
                    if (currenStatus != backStatus)
                    {
                        ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                        e.newstatus = currenStatus;
                        e.oldstatus = backStatus;
                        backStatus = currenStatus;
                        LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                    }

                    if (!closeFlag && !reconnecting)
                    {
                        reconnecting = true;
                        Task.Run(async () => await TryReconnectAsync());
                    }

                    return retval;
                }
                sendCount = asyncstate.workSocket.Send(data, size, flags);

                if (sendCount < size)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}() PARTIAL SEND: {2}", className, funcName, msg, Thread.CurrentThread.ManagedThreadId);

                    // 일부만 전송되었을 경우도 실패로 간주
                    if (!closeFlag && !reconnecting)
                    {
                        reconnecting = true;
                        Task.Run(async () => await TryReconnectAsync());
                    }

                    //if (!reconnecting)
                    //{
                    //    reconnecting = true;
                    //    Task.Run(async () => TryReconnectAsync());
                    //}
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_SEND_PARTIAL;
                    //string tmp = Encoding.UTF8.GetString(msg, 0, size);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : ERR_RECV_SEND_PARTIAL " + msg + ", ING CMD = " + currentCommand, Thread.CurrentThread.ManagedThreadId);
                    currentCommand = "";

                    //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_SEND_PARTIAL;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + msg + ")";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE SEND PARTIAL (" + msg + ")";

                    return retval;
                }

                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + msg + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + msg + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                currenStatus = csConnStatus.Disconnected;
                if (currenStatus != backStatus)
                {
                    ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                    e.newstatus = currenStatus;
                    e.oldstatus = backStatus;
                    backStatus = currenStatus;
                    LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                }

                if (!closeFlag && !reconnecting)
                {
                    reconnecting = true;
                    Task.Run(async () => await TryReconnectAsync());
                }

                return retval;
            }
        }

        public csConnStatus GetConnectionStatus()
        {
            return csConnStatus.Connected;
        }

        public async Task<ITNTResponseArgs> Send(byte[] data, int size, int loglevel, int timeout)
        {
            // Convert the string data to byte data using ASCII encoding.  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);
            string className = "LaserSourceController";
            string funcName = "Send";
            string msg = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";

            // Begin sending the data to the remote device.  
            try
            {
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RemoteEndPoint = " + asyncstate.workSocket.RemoteEndPoint.ToString());
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LocalEndPoint = " + asyncstate.workSocket.LocalEndPoint.ToString());

                msg = Encoding.UTF8.GetString(data, 0, size);
                //ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND2 : " + msg, Thread.CurrentThread.ManagedThreadId);
                asyncstate.workSocket.BeginSend(data, 0, size, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
                //asyncstate.workSocket.SendAsync();
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                if (closeFlag == false)
                {
                    if ((asyncstate.workSocket != null) && (asyncstate.workSocket.Connected == true))
                    {
                        CloseClient(0);
                    }
                    //asyncstate.workSocket.c
                    Util.GetPrivateProfileValue("LASER", "SERVERIP", "192.168.3.230", ref value, Constants.PARAMS_INI_FILE);
                    //IP = value;
                    serverIP = value;
                    Util.GetPrivateProfileValue("LASER", "SERVERPORT", "10001", ref value, Constants.PARAMS_INI_FILE);
                    int.TryParse(value, out serverPort);

                    IPAddress ipAddress = IPAddress.Parse(serverIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                    // Create a TCP/IP socket.  
                    asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.  
                    //if (closeFlag == false)
                    asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                }
                else
                {
                    if ((asyncstate.workSocket != null) && (asyncstate.workSocket.Connected == true))
                    {
                        CloseClient(1);
                    }
                }
                return retval;
            }
        }


        void completeSendAsync(object sender, SocketAsyncEventArgs e)
        {
            SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_END;
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = 0;
                try
                {
                    bytesSent = client.EndSend(ar);
                }
                catch(Exception ex)
                {

                }

                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_END;

                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "LaserSourceController", "SendCallback", "RemoteEndPoint = " + client.RemoteEndPoint.ToString());
                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "LaserSourceController", "SendCallback", "LocalEndPoint = " + client.LocalEndPoint.ToString());

                // Signal that all bytes have been sent.  
                //sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SetTcpKeepAlive(Socket socket, uint keepaliveTime, uint keepaliveInterval)
        {
            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            */

            // marshal the equivalent of the native structure into a byte array
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(keepaliveTime)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepaliveTime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)keepaliveInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

            // write SIO_VALS to Socket IOControl
            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        protected async Task<ITNTResponseArgs> ExecuteCommandFuncAsync(string cmd, byte[] msg, int size, int loglevel, int timeout = 1)
        {
            string className = "LaserSourceController";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ExecuteCommandFuncAsync";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            Stopwatch sw = new Stopwatch();
            string sCurrentFunc = "EXECUTE FUNCTION";

            //commandMutex.WaitOne();

            try
            {
                await _commandLock.WaitAsync();
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //if (doingCmdFlag2 == true)
                //{
                //    sw.Start();
                //    while (sw.Elapsed < TimeSpan.FromSeconds(4 * timeout))
                //    {
                //        if (!doingCmdFlag2)
                //            break;

                //        await Task.Delay(50);
                //    }
                //    sw.Stop();

                //    if (doingCmdFlag2)
                //    {
                //        retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                //        string tmp = Encoding.UTF8.GetString(msg, 0, size);
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY - " + tmp, Thread.CurrentThread.ManagedThreadId);
                //        currentCommand = "";
                //        doingCmdFlag2 = false;

                //        //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                //        retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                //        retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                //        retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                //        retval.errorInfo.devErrorInfo.sErrorFunc = cmd;
                //        retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_CAUSE1_COMM + (-retval.execResult).ToString("X2");
                //        retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS BUSY (" + cmd + ")";

                //        //retval.errorInfo.rawErrorCode = (int)LASERCONTROLLERERROR.ERR_COMMAD_BUSY;
                //        //retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                //        //retval.errorInfo.sErrorDevFunc = cmd;
                //        //retval.errorInfo.sErrorDevMsg = "LASER DEVICE IS BUSY (" + cmd + ")";
                //        //retval.errorInfo.sErrorMessage = "LASER DEVICE IS BUSY (" + cmd + ")";
                //        return retval;
                //    }
                //}

                doingCmdFlag2 = true;
                currentCommand = cmd;
                retval = await ExecuteCommandMsgAsync(cmd, msg, size, loglevel, timeout);
                if((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    //await Task.Delay(500);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN CMD 1 - " + cmd, Thread.CurrentThread.ManagedThreadId);
                    retval = await ExecuteCommandMsgAsync(cmd, msg, size, loglevel, timeout);
                    if((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        //await Task.Delay(500);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AGAIN CMD 2 - " + cmd, Thread.CurrentThread.ManagedThreadId);
                        retval = await ExecuteCommandMsgAsync(cmd, msg, size, loglevel, timeout);
                    }
                }
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                currentCommand = "";

                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = cmd;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");


                //retval.errorInfo.sErrorDevMsg = "EXCEPTION = " + ex.HResult.ToString("X2");

                doingCmdFlag2 = false;
            }
            finally
            {
                doingCmdFlag2 = false;
                currentCommand = "";

                _commandLock.Release();
                //commandMutex.ReleaseMutex();  // 동기화 해제
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }

            //doingCmdFlag2 = false;
            //ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        protected async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(string cmd, byte[] msg, int size, int loglevel, int timeout = 2)
        {
            string className = "LaserSourceController";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ExecuteCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //ITNTResponseArgs retval2 = new ITNTResponseArgs();
            int retrycount = 0;
            Stopwatch sw = new Stopwatch();
            string sCurrentFunc = "EXECUTE COMMAND";
            //CommandStack tmpqueue = new CommandStack();

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + cmd, Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    await Task.Delay(50);
                    if (!doingCmdFlag)
                    {
                        lock (LockQueue)
                        {
                            if ((cmdQueue.Count() > 0) && (cmd == cmdQueue.Peek()))
                            {
                                break;
                            }
                        }
                    }
                }
                sw.Stop();
                if (doingCmdFlag)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    //string tmp = Encoding.UTF8.GetString(msg, 0, size);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : DEVICE BUSY " + cmd + ", ING CMD = " +currentCommand, Thread.CurrentThread.ManagedThreadId);
                    currentCommand = "";

                    //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS BUSY2 (" + cmd + ")";

                    //retval.errorInfo.sErrorDevMsg = "DEVICE BUSY 2 : " + cmd;
                    //retval.errorInfo.sErrorCode = sDeviceCode + "01" + "02";

                    //if (!closeFlag && !reconnecting)
                    //{
                    //    reconnecting = true;
                    //    Task.Run(async () => await TryReconnectAsync());
                    //}

                    return retval;
                }

                if(asyncstate.workSocket == null)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    //string tmp = Encoding.UTF8.GetString(msg, 0, size);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : ERR_PORT_NOT_OPENED " + cmd + ", ING CMD = " + currentCommand, Thread.CurrentThread.ManagedThreadId);
                    currentCommand = "";

                    //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS NOT OPENED (" + cmd + ")";

                    //retval.errorInfo.sErrorDevMsg = "DEVICE BUSY 2 : " + cmd;
                    //retval.errorInfo.sErrorCode = sDeviceCode + "01" + "02";

                    currenStatus = csConnStatus.Disconnected;
                    if (currenStatus != backStatus)
                    {
                        ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                        e.newstatus = currenStatus;
                        e.oldstatus = backStatus;
                        backStatus = currenStatus;
                        LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                    }

                    if (!closeFlag && !reconnecting)
                    {
                        reconnecting = true;
                        Task.Run(async () => await TryReconnectAsync());
                    }

                    return retval;
                }

                if (asyncstate.workSocket.Connected == false)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    //string tmp = Encoding.UTF8.GetString(msg, 0, size);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : ERR_PORT_NOT_OPENED2 " + cmd + ", ING CMD = " + currentCommand, Thread.CurrentThread.ManagedThreadId);
                    currentCommand = "";

                    //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS NOT OPENED2 (" + cmd + ")";

                    //retval.errorInfo.sErrorDevMsg = "DEVICE BUSY 2 : " + cmd;
                    //retval.errorInfo.sErrorCode = sDeviceCode + "01" + "02";

                    currenStatus = csConnStatus.Disconnected;
                    if (currenStatus != backStatus)
                    {
                        ConnectionStatusChangedEventArgs e = new ConnectionStatusChangedEventArgs();
                        e.newstatus = currenStatus;
                        e.oldstatus = backStatus;
                        backStatus = currenStatus;
                        LaserConnectionStatusChangedEventFunc?.Invoke(this, e);
                    }

                    if (!closeFlag && !reconnecting)
                    {
                        reconnecting = true;
                        Task.Run(async () => await TryReconnectAsync());
                    }

                    return retval;
                }

#if TEST_DEBUG_LASER
#else
                doingCmdFlag = true;

                lock (LockQueue)
                {
                    if (cmdQueue.Count() > 0)
                    {
                        cmdQueue.Dequeue();
                    }
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    InitializeExecuteCommand();
                    retval = await SendCommandMsgAsync(cmd, msg, size, loglevel, timeout);
                    if (retval.execResult < 0)
                    {
                        //if(retrycount == 2)
                        //{
                        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1} - " + cmd, retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        //    doingCmdFlag = false;
                        //    currentCommand = "";
                        //    return retval;
                        //}
                        //else
                        //{
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                            continue;
                        //}
                    }
                    retval = await RecvResponseMsgAsync(cmd, loglevel, timeout);
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
                        ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
                        if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
                        {
                            await Task.Delay(200);
                        }
                    }
                }
                doingCmdFlag = false;
#endif
                currentCommand = "";
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.execResult = ex.HResult;
                currentCommand = "";
                doingCmdFlag = false;

                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

            }

            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END - " + cmd, Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        protected async Task<ITNTResponseArgs> SendCommandMsgAsync(string cmd, byte[] data, int size, int loglevel, int timeout = 2)
        {
            string className = "LaserSourceController";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string msg = "";
            string sCurrentFunc = "SEND COMMAND";
            //bool bRet = false;
            //SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + cmd, Thread.CurrentThread.ManagedThreadId);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_REQ;

                msg = Encoding.UTF8.GetString(data, 0, size);

                retval = Send(data, size, loglevel);
                if (retval.execResult != 0)
                    return retval;

                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
            }
            catch (Exception ex)
            {
                RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_ERR;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                //retval.execResult = ex.HResult;
                //retval.errorInfo.sErrorCode = sDeviceCode + "05" + (-ex.HResult).ToString("");
                //doingCmdFlag = false;
                return retval;
            }

            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END - " + cmd, Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        protected async Task<ITNTResponseArgs> RecvResponseMsgAsync(string cmd, int loglevel, int timeout = 2)
        {
            string className = "LaserSourceController";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "RecvResponseMsgAsync";// MethodBase.GetCurrentMethod().Name;
            //ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "RECEIVE RESPONSE";

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
                    //retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ACK_TIMEOUT;
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;

                    //retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM +  (-retval.execResult).ToString("X2");
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ACK_TIMEOUT;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ACK_TIMEOUT;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ")" + " : RECEIVE TIMEOUT";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");

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
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;

                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : RECEIVE TIMEOUT";
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");

                    return retval;
                }

                //if (NAKError == 1)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV NAK");
                //    retval.execResult = (int)COMPORTERROR.ERR_RECV_NAK;
                //    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                //    return retval;
                //}

                Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                sw.Stop();

                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }
            //string msg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
            //ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV : {0} / {1}", msg, retval.recvSize), Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        private void InitializeExecuteCommand()
        {
            SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
            RecvFrameData.Initialize();
            RecvFrameLength = 0;
            //NAKError = 0;
        }

        private ErrorInfo2 AnalysisResponseData(string func, string cmd, ITNTResponseArgs args)
        {
            string className = "LaserSourceController";
            string funcName = "AnalysisResponseData";
            ErrorInfo2 errorInfo = new ErrorInfo2();
            //string recvstring = "";

            try
            {
                //retval = (ITNTResponseArgs)args.Clone();
                if (args.recvString.Length > 3)
                {
                    string recvCMD = args.recvString.Substring(0, 3);
                    if (recvCMD == "ERR")
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LASER RECV ERROR (" + cmd + ") = " + args.recvString, Thread.CurrentThread.ManagedThreadId);
                        //retval.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ERROR_RESP;
                        //retval.sErrorCode = sDeviceCode + Constants.ERROR_CAUSE1_DATA + (-retval.rawErrorCode).ToString("X2");
                        //retval.sErrorDevFunc = cmd;
                        //retval.sErrorMessage = "LASER ERROR RESPONSE (" + cmd + ") = " + args.recvString;

                        errorInfo.ex.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ERROR_RESP;
                        errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                        errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                        errorInfo.devErrorInfo.sErrorFunc = func + "(" + cmd + ")";
                        errorInfo.devErrorInfo.sErrorMessage = "LASER ERROR RESPONSE (" + cmd + ") = " + args.recvString;
                        errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.devErrorInfo.execResult).ToString("X2");

                        return retval;
                    }
                }

                if (args.recvString.Length >= cmd.Length)
                {
                    string recvCMD = args.recvString.Substring(0, cmd.Length);
                    if (recvCMD != cmd)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LASER COMMAND MISMATCH (" + cmd + ") != RESPONSE COMMAND (" + recvCMD + ")", Thread.CurrentThread.ManagedThreadId);
                        retval.rawErrorCode = (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH;
                        retval.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH;
                        retval.devErrorInfo.sDeviceName = sDeviceName;
                        retval.devErrorInfo.sDeviceCode = sDeviceCode;
                        retval.devErrorInfo.sErrorFunc = func + "(" + cmd + ")";
                        retval.devErrorInfo.sErrorMessage = "LASER COMMAND MISMATCH (" + cmd + ") != RESPONSE COMMAND (" + recvCMD + ")";
                        retval.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.devErrorInfo.execResult).ToString("X2");

                        //retval.sErrorCode = sDeviceCode + Constants.ERROR_CAUSE1_DATA + (-retval.rawErrorCode).ToString("X2");
                        //retval.sErrorDevFunc = cmd;
                        //retval.sErrorMessage = "LASER COMMAND MISMATCH (" + cmd + ") != RESPONSE COMMAND (" + recvCMD + ")";
                        return retval;
                    }
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LASER RECV DATA INVALID (" + cmd + ")= " + args.recvString, Thread.CurrentThread.ManagedThreadId);

                    retval.rawErrorCode = (int)COMMUNICATIONERROR.ERR_RECV_INVALID_DATA;
                    retval.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_INVALID_DATA;
                    retval.devErrorInfo.sDeviceName = sDeviceName;
                    retval.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.devErrorInfo.sErrorFunc = func + "(" + cmd + ")";
                    retval.devErrorInfo.sErrorMessage = "LASER INVALID RESPONSE (" + cmd + ") = " + args.recvString;
                    retval.sErrorCode = sDeviceCode + Constants.ERROR_COMM + (-retval.devErrorInfo.execResult).ToString("X2");



                    //retval.sErrorCode = sDeviceCode + Constants.ERROR_CAUSE1_DATA + (-retval.rawErrorCode).ToString("X2");
                    //retval.sErrorDevFunc = cmd;
                    //retval.sErrorMessage = "LASER SEND INVALID RESPONSE (" + cmd + ") = " + args.recvString;
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format(cmd + ": EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.rawErrorCode = ex.HResult;
                retval.devErrorInfo.execResult = ex.HResult;
                retval.devErrorInfo.sDeviceName = sDeviceName;
                retval.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.devErrorInfo.sErrorFunc = func + "(" + cmd + ")";
                retval.devErrorInfo.sErrorMessage = "LASER EXCEPTION ERROR (" + cmd + ") = " + ex.Message;
                retval.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.devErrorInfo.execResult).ToString("X2");


                //retval.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.rawErrorCode).ToString("X2");
                //retval.sErrorDevFunc = cmd;
                //retval.sErrorMessage = "LASER EXCEPTION ERROR (" + cmd + ") = " + ex.Message;
            }

            return retval;
        }


        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ITNTResponseArgs> AimingBeamON(int loglevel=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "AimingBeamON";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "ABN";
            int i = 0;
            string recvCMD = "";
            string sCurrentFunc = "AIMING ON";
            //CommandStack queue = new CommandStack();

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                //retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    ////string[] recvMsgs = recvMsg.Split(':');
                    ////if(recvMsgs.Length >= 2)
                    ////{

                    ////}
                    ////else
                    ////{

                    ////}
                    ////string recvERR = recvMsg.Substring(0, 3);
                    ////if()
                    //recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, ("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> AimingBeamOFF(int loglevel=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "AimingBeamOFF";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "ABF";
            int i = 0;
            string sCurrentFunc = "AIMING OFF";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, ("START"), Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                //retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, ("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, ("END"), Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetExternalAimingBeamControll(byte endisable)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetExternalAimingBeamControll";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "DEABC";
            int i = 0;
            string sCurrentFunc = "SET EXTERNAL AIMING CONTROLL";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, ("START"), Thread.CurrentThread.ManagedThreadId);
                if (endisable == 1)
                    exeCMD = "EEABC";

                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }
                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, ("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
//                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, ("END"), Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetExternalControll(byte endisable)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetExternalControll";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "DEC";
            int i = 0;
            string sCurrentFunc = "SET EXTERNAL CONTROLL";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (endisable == 1)
                    exeCMD = "EEC";

                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetGateMode(byte endisable)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetGateMode";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "DGM";
            int i = 0;
            string sCurrentFunc = "SET GATE MODE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (endisable == 1)
                    exeCMD = "EGM";

                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetHWEmissionControll(byte endisable)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetHWEmissionControll";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "DLE";
            int i = 0;
            string sCurrentFunc = "SET HW EMISSION CONTROLL";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (endisable == 1)
                    exeCMD = "ELE";

                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetModulation(byte endisable)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetModulation";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "DMOD";
            int i = 0;
            string sCurrentFunc = "SET MODULATION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (endisable == 1)
                    exeCMD = "EMOD";

                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetPulseMode(byte endisable)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetPulseMode";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "DPM";
            int i = 0;
            string sCurrentFunc = "SET PULSE MODE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (endisable == 1)
                    exeCMD = "EPM";

                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> StartEmission()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //ITNTResponseArgs retval2 = new ITNTResponseArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            string className = "LaserSourceController";
            string funcName = "StartEmission";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "EMON";
            int i = 0;
            string sCurrentFunc = "START EMISSION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.devErrorInfo.execResult;
                        retval.errorInfo = errorInfo;
                    }
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> StopEmission()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "StopEmission";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "EMOFF";
            int i = 0;
            string sCurrentFunc = "STOP EMISSION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadExtendedDeviceStatus()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadExtendedDeviceStatus";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "ESTA";
            int i = 0;
            string sCurrentFunc = "READ EXTENDED DEVICE STATUS";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> LockFrontPannel()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "LockFrontPannel";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "LFP";
            int i = 0;
            string sCurrentFunc = "LOCK FRONT PANNEL";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GetHelp(string command)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo(); 
            string className = "LaserSourceController";
            string funcName = "GetHelp";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "HELP";
            int i = 0;
            string sCurrentFunc = "GET HELP";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                if(command.Length > 0)
                {
                    sendArg.sendBuffer[i++] = (byte)' ';
                    for (int k = 0; k < command.Length; k++)
                        sendArg.sendBuffer[i++] = (byte)command[k];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ResetCritcalError()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ResetCritcalError";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RCE";
            int i = 0;
            string sCurrentFunc = "RESET CRITICAL ERROR";

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadCurrentSetpoint()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadCurrentSetpoint";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RCS";
            int i = 0;
            string sCurrentFunc = "READ CURRENT SET POINT";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadBoardTemperature(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadBoardTemperature";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RBT";
            int i = 0;
            string sCurrentFunc = "READ BOARD TEMPERATURE";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_LASER
#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                //retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }



        public async Task<ITNTResponseArgs> ReadLaserTemperature(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadLaserTemperature";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RCT";
            int i = 0;
            string sCurrentFunc = "READ LASER TEMPERATURE";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_LASER
#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }
                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadErrorCount()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadErrorCount";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "REC";
            int i = 0;
            string sCurrentFunc = "READ ERROR COUNT";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ResetErrors()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ResetErrors";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RERR";
            int i = 0;
            string sCurrentFunc = "RESET ERROR";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadElapsedTime()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadElapsedTime";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RET";
            int i = 0;
            string sCurrentFunc = "READ ELAPSED TIME";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSWVersion()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadSWVersion";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RFV";
            int i = 0;
            string sCurrentFunc = "READ SW VERSION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadModuleErrorCode()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadModuleErrorCode";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RMEC";
            int i = 0;
            string sCurrentFunc = "READ MODULE ERROR CODE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        //public async Task<ITNTResponseArgs> ReadMinimumCurrentSetpoint()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    ITNTSendArgs sendArg = new ITNTSendArgs();
        //    string className = "LaserSourceController";
        //    string funcName = "ReadMinimumCurrentSetpoint";
        //    string recvMsg = "";
        //    //string respVal = "";
        //    string exeCMD = "RNC";
        //    int i = 0;
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //    try
        //    {
        //        for (i = 0; i < exeCMD.Length; i++)
        //        {
        //            sendArg.sendBuffer[i] = (byte)exeCMD[i];
        //        }
        //        //sendArg.sendBuffer[i++] = (byte)' ';
        //        //for (int k = 0; k < value.Length; k++)
        //        //    sendArg.sendBuffer[i++] = (byte)value[k];
        //        sendArg.sendBuffer[i++] = 0x0D;
        //        sendArg.dataSize = i;
        //        currentCommand = exeCMD;

        //        //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
        //        retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //        }
        //        else
        //        {
        //            recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
        //            retval.recvString = recvMsg;
        //            string recvCMD = recvMsg.Substring(0, exeCMD.Length);
        //            if ((recvCMD != "ERR") && (recvCMD != exeCMD))
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
        //                retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //}

        public async Task<ITNTResponseArgs> ReadOuputPower(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadOuputPower";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "ROP";
            int i = 0;
            string sCurrentFunc = "READ OUTPUT POWER";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_LASER
#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPeakPower(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadPeakPower";
            string recvMsg = "";
            string respVal = "";
            string exeCMD = "RPP";
            int i = 0;
            string sCurrentFunc = "READ PEAK POWER";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_LASER
#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                //retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, 3);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPulseRepetitionRate()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadPulseRepetitionRate";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RPRR";
            int i = 0;
            string sCurrentFunc = "READ PULSE REPETITION RATE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadPulseWidth()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadPulseWidth";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RPW";
            int i = 0;
            string sCurrentFunc = "READ PULSE WIDTH";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSerialNumber()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadSerialNumber";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RSN";
            int i = 0;
            string sCurrentFunc = "READ SERIAL NUMBER";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }
        public async Task<ITNTResponseArgs> ReadLaserVersion()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadLaserVersion";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RMN";
            int i = 0;
            string sCurrentFunc = "READ LASER VERSION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }
        public async Task<ITNTResponseArgs> ReadLaserConfiguration()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadLaserConfiguration";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RPSP";
            int i = 0;
            string sCurrentFunc = "READ LASER CONFIGURAION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetDiodeCurrent(string value)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetDiodeCurrent";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "SDC";
            int i = 0;
            string sCurrentFunc = "SET DIODE CURRENT";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }

                if (value.Length > 0)
                {
                    sendArg.sendBuffer[i++] = (byte)' ';
                    for (int k = 0; k < value.Length; k++)
                        sendArg.sendBuffer[i++] = (byte)value[k];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;

                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetPulseRepetitionRate(string value, int loglevel=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetPulseRepetitionRate";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "SPRR";
            int i = 0;
            string sCurrentFunc = "SET PULSE REETITION RATE";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                if (value.Length > 0)
                {
                    sendArg.sendBuffer[i++] = (byte)' ';
                    for (int k = 0; k < value.Length; k++)
                        sendArg.sendBuffer[i++] = (byte)value[k];
                }

                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetPulseWidth(string value)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetPulseWidth";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "SPW";
            int i = 0;
            string sCurrentFunc = "SET PULSE WIDTH";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }

                if (value.Length > 0)
                {
                    sendArg.sendBuffer[i++] = (byte)' ';
                    for (int k = 0; k < value.Length; k++)
                        sendArg.sendBuffer[i++] = (byte)value[k];
                }

                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadDeviceStatus(int loglevel=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ReadDeviceStatus"; // MethodBase.GetCurrentMethod().Name;
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "STA";
            int i = 0;
            string sCurrentFunc = "READ DEVICE STATUS";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

#if TEST_DEBUG_LASER
                string value = "";
                Util.GetPrivateProfileValue("LASERSOURCE", "STATUS", "0", ref value, "Test.ini");
                byte[] tmp = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                retval.recvSize = tmp.Length;
                Array.Copy(tmp, retval.recvBuffer, tmp.Length);
#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                //retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    //retval.errorInfo.sErrorDevice = "LASER";
                    //retval.errorInfo.sErrorDevFunc = "READ DEVICE STATUS";
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadDeviceResetStatus(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadDeviceResetStatus";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RESTA";
            int i = 0;
            string sCurrentFunc = "READ DEVICE RESET STATUS";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_LASER
#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetIP(string value)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SetIP";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "SIP";
            int i = 0;
            string sCurrentFunc = "SET IP";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_LASER
#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }

                if(value.Length > 0)
                {
                    sendArg.sendBuffer[i++] = (byte)' ';
                    for (int k = 0; k < value.Length; k++)
                        sendArg.sendBuffer[i++] = (byte)value[k];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;

                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        //public async Task<ITNTResponseArgs> SelectSequence(string value)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    ITNTSendArgs sendArg = new ITNTSendArgs();
        //    string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = MethodBase.GetCurrentMethod().Name;
        //    string recvMsg = "";
        //    //string respVal = "";
        //    string exeCMD = "SQSEL";
        //    int i = 0;
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //    try
        //    {
        //        for (i = 0; i < exeCMD.Length; i++)
        //        {
        //            sendArg.sendBuffer[i] = (byte)exeCMD[i];
        //        }
        //        sendArg.sendBuffer[i++] = (byte)' ';
        //        for (int k = 0; k < value.Length; k++)
        //            sendArg.sendBuffer[i++] = (byte)value[k];
        //        sendArg.sendBuffer[i++] = 0x0D;
        //        sendArg.dataSize = i;

        //        //sendArg.sendBuffer[i++] = (byte)'R';/* sendArg.dataSize++;*/
        //        //sendArg.sendBuffer[i++] = (byte)'P';/* sendArg.dataSize++;*/
        //        //sendArg.sendBuffer[i++] = (byte)'R';/* sendArg.dataSize++;*/
        //        //sendArg.sendBuffer[i++] = (byte)'R';/* sendArg.dataSize++;*/
        //        //sendArg.sendBuffer[i++] = 0x0D;     /*sendArg.dataSize++;*/

        //        currentCommand = exeCMD;

        //        retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult));
        //        }
        //        else
        //        {
        //            recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
        //            retval.recvString = recvMsg;
        //            string recvCMD = recvMsg.Substring(0, exeCMD.Length);
        //            if ((recvCMD != "ERR") && (recvCMD != exeCMD))
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"));
        //                retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> UnlockFrontPanel()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "UnlockFrontPanel";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "UFP";
            int i = 0;
            string sCurrentFunc = "UNLOCK FRONT PANEL";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }

                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> DisableWaveformPulseMode()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "DisableWaveformPulseMode";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "DWPM";
            int i = 0;
            string sCurrentFunc = "DISABLE WAVEFORM PULSE MODE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }

                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> EnableWaveformPulseMode()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "EnableWaveformPulseMode";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "EWPM";
            int i = 0;
            string sCurrentFunc = "ENABLE WAVEFORM PULSE MODE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ConfigWaveformMode(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ConfigWaveformMode";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "PCFG";
            int i = 0;
            string sCurrentFunc = "CONFIG WAVEFORM MODE";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ProfileList()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ProfileList";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "PRLS";
            int i = 0;
            string sCurrentFunc = "PROFILE LIST";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                //sendArg.sendBuffer[i++] = (byte)' ';
                //for (int k = 0; k < value.Length; k++)
                //    sendArg.sendBuffer[i++] = (byte)value[k];
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SelectProfile(string value)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SelectProfile";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "PRSEL";
            int i = 0;
            string sCurrentFunc = "SELECT PROFILE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }

                if (value.Length > 0)
                {
                    sendArg.sendBuffer[i++] = (byte)' ';
                    for (int k = 0; k < value.Length; k++)
                        sendArg.sendBuffer[i++] = (byte)value[k];
                }

                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SequenceList()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SequenceList";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "SQLS";
            int i = 0;
            string sCurrentFunc = "SEQUENCE LIST";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SelectSequence(string value)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "SelectSequence";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "SQSEL";
            int i = 0;
            string sCurrentFunc = "SELECT SEQUENCE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                if (value.Length > 0)
                {
                    sendArg.sendBuffer[i++] = (byte)' ';
                    for (int k = 0; k < value.Length; k++)
                        sendArg.sendBuffer[i++] = (byte)value[k];
                }

                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, 0);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, exeCMD.Length);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }



        public async Task<ITNTResponseArgs> ReadDeviceStatus4Debug(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadDeviceStatus";
            string recvMsg = "";
            //string respVal = "";
            string exeCMD = "RESTA";
            int i = 0;
            string sCurrentFunc = "READ DEVICE RESET STATUS";
            string value = "";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                Util.GetPrivateProfileValue("STATUS", "USE", "0", ref value, ".\\Parameter\\LaserDebug.ini");
                if (value == "0")
                {
                    retval.recvString = "";
                }
                else
                {
                    Util.GetPrivateProfileValue("STATUS", "VALUE", "0", ref value, ".\\Parameter\\LaserDebug.ini");
                    retval.recvString = value;
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPeakPower4Debug(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            ErrorInfo errorInfo = new ErrorInfo();
            string className = "LaserSourceController";
            string funcName = "ReadPeakPower";
            string recvMsg = "";
            string respVal = "";
            string exeCMD = "RPP";
            int i = 0;
            string sCurrentFunc = "READ PEAK POWER";
            string value = "";
            int min = 0, max = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
#if TEST_DEBUG_LASER

                Util.GetPrivateProfileValue("PEAKPOWER", "USE", "0", ref value, ".\\Parameter\\LaserDebug.ini");
                if (value == "0")
                {
                    retval.recvString = "";
                }
                else
                {
                    Util.GetPrivateProfileValue("PEAKPOWER", "MINVALUE", "0", ref value, ".\\Parameter\\LaserDebug.ini");
                    int.TryParse(value, out min);

                    Util.GetPrivateProfileValue("PEAKPOWER", "MAXVALUE", "0", ref value, ".\\Parameter\\LaserDebug.ini");
                    int.TryParse(value, out max);

                    Random ran = new Random();
                    int ranvalue = ran.Next(min, max + 1);

                    retval.recvString = ranvalue.ToString("D4");
                }

#else
                for (i = 0; i < exeCMD.Length; i++)
                {
                    sendArg.sendBuffer[i] = (byte)exeCMD[i];
                }
                sendArg.sendBuffer[i++] = 0x0D;
                sendArg.dataSize = i;
                currentCommand = exeCMD;
                lock (LockQueue)
                {
                    cmdQueue.Enqueue(exeCMD);
                }
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "QUEUE = " + cmdQueue.Count().ToString(), Thread.CurrentThread.ManagedThreadId);

                //retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel);
                retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, loglevel);
                //retval = await ExecuteCommandFuncAsync(exeCMD, sendArg.sendBuffer, sendArg.dataSize, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - {0:X}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    errorInfo = AnalysisResponseData(sCurrentFunc, exeCMD, retval);
                    if (errorInfo.devErrorInfo.execResult != 0)
                    {
                        retval.execResult = (int)errorInfo.rawErrorCode;
                        retval.errorInfo = errorInfo;
                    }

                    //recvMsg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = recvMsg;
                    //string recvCMD = recvMsg.Substring(0, 3);
                    //if ((recvCMD != "ERR") && (recvCMD != exeCMD))
                    //{
                    //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR - ERR_CMD_MISSMATCH"), Thread.CurrentThread.ManagedThreadId);
                    //    retval.execResult = (int)LASERCONTROLLERERROR.ERR_CMD_MISSMATCH;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                return retval;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        //public async Task<ITNTResponseArgs>ReadPeakPower()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    ITNTSendArgs args = new ITNTSendArgs();
        //    try
        //    {
        //        args.sendBuffer[0] = (byte)'R'; args.dataSize++;
        //        args.sendBuffer[1] = (byte)'P'; args.dataSize++;
        //        args.sendBuffer[2] = (byte)'P'; args.dataSize++;
        //        args.sendBuffer[3] = 0x0D; args.dataSize++;

        //        //retval = ExecuteCommand2(args.sendBuffer, args.dataSize, 0, 1);
        //        retval = await ExecuteCommandMsgAsync(args.sendBuffer, args.dataSize, 0, 1);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return retval;
        //}
    }
}
