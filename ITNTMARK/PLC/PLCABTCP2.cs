using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvancedHMIDrivers;
using MfgControl.AdvancedHMI.Drivers.Common;
using System.Reflection;
using ITNTUTIL;
using ITNTCOMMON;
using System.Timers;
using System.Threading;
using System.Windows.Controls;
using System.Data;
//using S7.Net.Types;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{

    internal class PLCABTCP2
    {
        //public event PLCControllerStatusEventHandler PLCControllerStatusEventFunc;

        //READ STATUS
        public string PLC_ADDRESS_SIGNAL = "0";
        public string PLC_ADDRESS_CARTYPE = "1";
        public string PLC_ADDRESS_LINKSTATUS = "2";
        public string PLC_ADDRESS_AUTOMANUAL = "3";
        public string PLC_ADDRESS_FRAMETYPE = "4";
        public string PLC_ADDRESS_PCERROR = "5";
        public string PLC_ADDRESS_SEQUENCE = "6";
        public string PLC_ADDRESS_BODYNUM = "6";
        public string PLC_ADDRESS_CHINA = "14";                 //0 = Normal, 1 = China
        public string PLC_ADDRESS_RESERV02 = "15";
        public string PLC_ADDRESS_EMISSIONSTATUS = "EMISSIONSTATUS";

        //WRITE STATUS
        public string PLC_ADDRESS_MATCHRESULT = "16";
        public string PLC_ADDRESS_MARKSTATUS = "17";
        public string PLC_ADDRESS_VISIONRESULT = "18";
        public string PLC_ADDRESS_MARKSTATUS_2 = "19";
        public string PLC_ADDRESS_VISIONRESULT_2 = "20";
        public string PLC_ADDRESS_SETLINK = "21";
        public string PLC_ADDRESS_SETAIR = "22";
        public string PLC_ADDRESS_SCANCOMPLETE = "23";
        public string PLC_ADDRESS_MESCOUNTERROR = "MESCOUNTERROR";
        public string PLC_ADDRESS_REQMOVEROBOT = "REQMOVEROBOT";
        public string PLC_ADDRESS_SETERRORCODE = "SETERRORCODE";      ///WARNING - NOT USED. CHECK SENDERROR functions.
        public string PLC_ADDRESS_PCEMISSION = "PCEMISSION";
        public string PLC_ADDRESS_LASERPOWEROFF = "FROM_PC_LASER_POWER_OFF";
        public string PLC_ADDRESS_LASERPOWERLOW = "LASER_POWER_LOW";
        public string PLC_ADDRESS_HEARTBEAT = "PC_HEART_BIT";
        public string PLC_ADDRESS_MARKCANCEL = "PC_MARK_CANCEL";


        public const int SIGNAL_PLC2PC_NORMAL = 0;
        public const int SIGNAL_PLC2PC_NEXTVIN = 1;                // 각인준비 OK _ 넥스트 빈
        public const int SIGNAL_PLC2PC_MARK_1 = 2;                // 각인 시작
        public const int SIGNAL_PLC2PC_VISION_1 = 4;                // 각인 완료
        public const int SIGNAL_PLC2PC_NOFRAME = 8;                // 비상 정지
        public const int SIGNAL_PLC2PC_EMERGENCY_STOP = 16;               // 비상 정지
        public const int SIGNAL_PLC2PC_MARK_2 = 32;               // 각인 시작
        public const int SIGNAL_PLC2PC_VISION_2 = 64;               // 각인 완료


        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const int SIGNAL_PC2PLC_READY = 0;                   // 운전준비
        public const int SIGNAL_PC2PLC_MATCHING_OK = 1;             // 각인 시작
        public const int SIGNAL_PC2PLC_MATCHING_NG = 2;             // NG
        public const int SIGNAL_PC2PLC_PC_ERROR = 4;                // PC TOTAL ERROR

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 차종(타입)데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PC --> PLC간 차종(타입)데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const byte PLC_MARK_STATUS_IDLE = 0;
        public const byte PLC_MARK_STATUS_DOING = 1;
        public const byte PLC_MARK_STATUS_COMPLETE = 2;


        EthernetIPforCLXCom ABPLCControl = new EthernetIPforCLXCom();
        private PLCDataArrivedCallbackHandler EventArrivalCallback;
        private PLCConnectionStatusChangedEventHandler ConnectCallback;


        System.Timers.Timer readPLCTimer = new System.Timers.Timer();
        bool bOpend = false;
        bool bTimerReadPLC = false;
        private int errorCount = 0;
        string statusAddr = "";

        private readonly object cmdLock = new object();

        bool DoingPLCStatusThread = false;
        Thread _plcStatusThread = null;
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        csConnStatus connState = csConnStatus.Closed;

        public PLCABTCP2(PLCDataArrivedCallbackHandler callback, PLCConnectionStatusChangedEventHandler connectFunc)
        {
            //callbackHandler = callback;
            ConnectCallback = connectFunc;
            EventArrivalCallback = callback;
        }

        public void LoadOption()
        {
            Util.GetPrivateProfileValue("ADDRESS", "SIGNAL", "SIGNAL", ref PLC_ADDRESS_SIGNAL, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "CARTYPE", "CARTYPE", ref PLC_ADDRESS_CARTYPE, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "LINKSTATUS", "LINKSTATUS", ref PLC_ADDRESS_LINKSTATUS, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "AUTOMANUAL", "AUTOMANUAL", ref PLC_ADDRESS_AUTOMANUAL, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "FRAMETYPE", "FRAMETYPE", ref PLC_ADDRESS_FRAMETYPE, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "PCERROR", "PCERROR", ref PLC_ADDRESS_PCERROR, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "SEQUENCE", "SEQUENCE", ref PLC_ADDRESS_SEQUENCE, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "BODYNUM", "BODYNUM", ref PLC_ADDRESS_BODYNUM, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "CHINA", "CHINA", ref PLC_ADDRESS_CHINA, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "RESERVED02", "RESERVED02", ref PLC_ADDRESS_RESERV02, Constants.PLCVAL_INI_FILE);

            //WRITE
            Util.GetPrivateProfileValue("ADDRESS", "MATCHRESULT", "MATCHRESULT", ref PLC_ADDRESS_MATCHRESULT, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "MARKSTATUS", "MARKSTATUS", ref PLC_ADDRESS_MARKSTATUS, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "VISIONRESULT", "VISIONRESULT", ref PLC_ADDRESS_VISIONRESULT, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "MARKSTATUS2", "MARKSTATUS2", ref PLC_ADDRESS_MARKSTATUS_2, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "VISIONRESULT2", "VISIONRESULT2", ref PLC_ADDRESS_VISIONRESULT_2, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "SETLINK", "SETLINK", ref PLC_ADDRESS_SETLINK, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "SETAIR", "SETAIR", ref PLC_ADDRESS_SETAIR, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "SCANCOMPLETE", "SCANCOMPLETE", ref PLC_ADDRESS_SCANCOMPLETE, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "MESCOUNTERROR", "MESCOUNTERROR", ref PLC_ADDRESS_MESCOUNTERROR, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "REQMOVEROBOT", "REQMOVEROBOT", ref PLC_ADDRESS_REQMOVEROBOT, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "SETERRORCODE", "SETERRORCODE", ref PLC_ADDRESS_SETERRORCODE, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "LASERPOWEROFF", "FROM_PC_LASER_POWER_OFF", ref PLC_ADDRESS_LASERPOWEROFF, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "LASERPOWERLOW", "LASER_POWER_LOW", ref PLC_ADDRESS_LASERPOWERLOW, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "HEARTBEAT", "PC_HEART_BIT", ref PLC_ADDRESS_HEARTBEAT, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue("ADDRESS", "MARKCANCEL", "MARKCANCEL", ref PLC_ADDRESS_MARKCANCEL, Constants.PLCVAL_INI_FILE);
        }

        public ITNTResponseArgs OpenPLC()
        {
            string className = "PLCABTCP2";
            string funcName = "OpenPLC";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string IPAddr = "", Port = "", Slot = "";
            //ITNTResponseArgs args = new ITNTResponseArgs();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            string sCurrentFunc = "OPEN PLC";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                lock (cmdLock)
                {
                    LoadOption();

                    Util.GetPrivateProfileValue("PLCCOMM", "ServerIP", "192.168.1.200", ref IPAddr, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "ListenPort", "44818", ref Port, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "ProcessorSlot", "0", ref Slot, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D0100", "D100", ref statusAddr, Constants.PARAMS_INI_FILE);

                    ABPLCControl = new EthernetIPforCLXCom();
                    ABPLCControl.BeginInit();
                    ABPLCControl.IPAddress = IPAddr;
                    ABPLCControl.Port = Convert.ToInt32(Port);
                    ABPLCControl.ProcessorSlot = Convert.ToInt32(Slot);
                    ABPLCControl.Timeout = 1000;

                    //ABPLCControl.PollRateOverride = 250;
                    //ABPLCControl.DataReceived += DataReceived;
                    //ABPLCControl.ComError += OnErrorReceived;
                    //subscribeID = ABPLCControl.Subscribe(tabName, 0, 200, DataReceived);
                    //ABPLCControl.SubscriptionDataReceived += DataReceived;
                    ABPLCControl.EndInit();

                    //tokenSource//     
                    retval = ReadSignalFromPLC4Test().Result;
                    if (retval.execResult == 0)
                    {
                        readPLCTimer.Interval = 1000;
                        readPLCTimer.Enabled = false;
                        readPLCTimer.AutoReset = false;
                        //readPLCTimer.Elapsed += new ElapsedEventHandler(TimerCallbackFunc);
                        readPLCTimer.Enabled = true;

                        bTimerReadPLC = true;
                        //StartReadPLC();

                        DoingPLCStatusThread = true;
                        _plcStatusThread = new Thread(PLCStatusThread);
                        _plcStatusThread.Start();
                        bOpend = true;
                        statusArg.exeResult = 0;
                        statusArg.oldstatus = connState;
                        statusArg.newstatus = csConnStatus.Connected;
                        if(statusArg.newstatus != connState)
                        {
                            connState = statusArg.newstatus;
                            ConnectCallback?.Invoke(statusArg);
                        }
                    }
                    else
                    {
                        StopABReadPLC();
                        //ABPLCControl.Unsubscribe(subscribeID);
                        ABPLCControl.CloseConnection();
                        bOpend = false;

                        DoingPLCStatusThread = true;
                        _plcStatusThread = new Thread(PLCStatusThread);
                        _plcStatusThread.Start();

                        //retval.execResult = args.execResult;

                        statusArg.newstatus = csConnStatus.Disconnected;
                        if (statusArg.newstatus != connState)
                        {
                            statusArg.oldstatus = connState;
                            connState = statusArg.newstatus;
                            ConnectCallback?.Invoke(statusArg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string IPAddr = "", Port = "", Slot = "";
            //ITNTResponseArgs args = new ITNTResponseArgs();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            string sCurrentFunc = "OPEN PLC";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                lock (cmdLock)
                {
                    LoadOption();

                    Util.GetPrivateProfileValue("PLCCOMM", "ServerIP", "192.168.1.200", ref IPAddr, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "ListenPort", "44818", ref Port, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "ProcessorSlot", "0", ref Slot, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D0100", "D100", ref statusAddr, Constants.PARAMS_INI_FILE);

                    ABPLCControl = new EthernetIPforCLXCom();
                    ABPLCControl.BeginInit();
                    ABPLCControl.IPAddress = IPAddr;
                    ABPLCControl.Port = Convert.ToInt32(Port);
                    ABPLCControl.ProcessorSlot = Convert.ToInt32(Slot);
                    ABPLCControl.Timeout = 1000;

                    //ABPLCControl.PollRateOverride = 250;
                    //ABPLCControl.DataReceived += DataReceived;
                    //ABPLCControl.ComError += OnErrorReceived;
                    //subscribeID = ABPLCControl.Subscribe(tabName, 0, 200, DataReceived);
                    //ABPLCControl.SubscriptionDataReceived += DataReceived;
                    ABPLCControl.EndInit();

                    //tokenSource//     
                    retval = ReadSignalFromPLC4Test().Result;
                    if (retval.execResult == 0)
                    {
                        readPLCTimer.Interval = 1000;
                        readPLCTimer.Enabled = false;
                        readPLCTimer.AutoReset = false;
                        //readPLCTimer.Elapsed += new ElapsedEventHandler(TimerCallbackFunc);
                        readPLCTimer.Enabled = true;

                        bTimerReadPLC = true;
                        //StartReadPLC();

                        DoingPLCStatusThread = true;
                        _plcStatusThread = new Thread(PLCStatusThread);
                        _plcStatusThread.Start();
                        bOpend = true;
                        statusArg.exeResult = 0;
                        statusArg.oldstatus = connState;
                        statusArg.newstatus = csConnStatus.Connected;
                        if(statusArg.newstatus != connState)
                        {
                            connState = statusArg.newstatus;
                            ConnectCallback?.Invoke(statusArg);
                        }
                    }
                    else
                    {
                        StopABReadPLC();
                        //ABPLCControl.Unsubscribe(subscribeID);
                        ABPLCControl.CloseConnection();
                        bOpend = false;

                        DoingPLCStatusThread = true;
                        _plcStatusThread = new Thread(PLCStatusThread);
                        _plcStatusThread.Start();

                        //retval = args.execResult;

                        statusArg.newstatus = csConnStatus.Disconnected;
                        if (statusArg.newstatus != connState)
                        {
                            statusArg.oldstatus = connState;
                            connState = statusArg.newstatus;
                            ConnectCallback?.Invoke(statusArg);
                        }

                        retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                        retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " ERROR = " + "CONNECT FAIL";

                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                return retval;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public int ClosePLC()
        {
            int retval = 0;
            string className = "PLCABTCP2";
            string funcName = "ClosePLC";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            try
            {
                lock (cmdLock)
                {
                    StopABReadPLC();
                    //ABPLCControl.Unsubscribe(subscribeID);
                    ABPLCControl.CloseConnection();
                    bOpend = false;
                }

                statusArg.newstatus = csConnStatus.Disconnected;
                if (statusArg.newstatus != connState)
                {
                    statusArg.oldstatus = connState;
                    connState = statusArg.newstatus;
                    ConnectCallback?.Invoke(statusArg);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private int StopABReadPLC()
        {
            int retval = 0;
            string className = "PLCABTCP2";
            string funcName = "StopABReadPLC";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                if (readPLCTimer != null)
                {
                    //readPLCTimer.Elapsed -= new ElapsedEventHandler(TimerCallbackFunc);
                    readPLCTimer.Enabled = false;
                    readPLCTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCControl", "StopABReadPLC", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        void TimerCallbackFunc(object sender, ElapsedEventArgs e)
        {
            string className = "PLCABTCP2";
            string funcName = "TimerCallbackFunc";
            string rmsg = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            int leng = 0;
            try
            {
                if (bTimerReadPLC)
                {
                    retval = ReadPLC("VISION_COM", /*ref rmsg, ref leng, */3);
                    if (retval.execResult == 0)
                    {
                        if ("N" == rmsg)
                            WritePLC("VISION_COM", "O", 3);
                        else
                        {
                            if (errorCount > 10)
                                errorCount = 0;
                            else
                                errorCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(3, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            finally
            {
                if (bTimerReadPLC)
                    readPLCTimer.Start();
            }
        }


        //        public int ReadPLC(int address, int count, ref T buff)
        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            retval.msgtype = sendArg.msgType;
            if (sendArg.msgType == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    retval = ReadPLC(sendArg.AddrString, /*retval.recvBuffer, ref retval.recvSize,*/ sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    retval = ReadPLC(sendArg.AddrString, /*ref retval.recvString, ref retval.recvSize, */sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCAsync2(ITNTSendArgs sendArg, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] temp;
            string sCurrentFunc = "READ PLC 2";
            try
            {
                retval.msgtype = sendArg.msgType;
                for (int i = 0; i < 3; i++)
                {
                    retval = ReadPLC(sendArg.AddrString, /*ref retval.recvString, ref retval.recvSize, */sendArg.loglevel);
                    if (retval.execResult == 0)
                    {
                        temp = Encoding.UTF8.GetBytes(retval.recvString);
                        if(temp.Length > retval.recvBuffer.Length)
                            Array.Copy(temp, retval.recvBuffer, retval.recvBuffer.Length);
                        else
                            Array.Copy(temp, retval.recvBuffer, temp.Length);
                        break;
                    }
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCAsync4Test(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //byte[] temp;
            string sCurrentFunc = "READ PLC 4";
            try
            {
                retval.msgtype = sendArg.msgType;
                //for (int i = 0; i < 3; i++)
                {
                    //retval = ReadPLC4Test(sendArg.AddrString, ref retval.recvString, ref retval.recvSize, sendArg.loglevel);
                    retval = ReadPLC4Test(sendArg.AddrString, sendArg.loglevel);
                    //if (retval.execResult == 0)
                    //{
                    //    temp = Encoding.UTF8.GetBytes(retval.recvString);
                    //    Array.Copy(temp, retval.recvBuffer, temp.Length);
                    //    //break;
                    //}
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            retval.msgtype = sendArg.msgType;
            if (sendArg.msgType == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    retval = WritePLC(sendArg.AddrString, sendArg.dataSize, sendArg.sendBuffer, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    retval = WritePLC(sendArg.AddrString, sendArg.sendString, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync2(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "WRITE PLC 2";
            try
            {
                retval.msgtype = sendArg.msgType;
                for (int i = 0; i < 3; i++)
                {
                    retval = WritePLC(sendArg.AddrString, sendArg.sendString, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + sendArg.AddrString + ") EXCEPTION ERROR = " + ex.Message;
                //retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
            }

            return retval;
        }

        //public async Task<ITNTResponseArgs> WritePLCAsync2(ITNTSendArgs sendArg)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    try
        //    {
        //        retval.msgtype = sendArg.msgType;
        //        for (int i = 0; i < 3; i++)
        //        {
        //            retval.execResult = WritePLC(sendArg.AddrString, sendArg.sendString, sendArg.loglevel);
        //            if (retval.execResult == 0)
        //                break;
        //            await Task.Delay(100);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //    }

        //    return retval;
        //}

        //private ITNTResponseArgs ReadPLC(string addr, ref string msg, ref int leng, int level, CancellationToken token = default)
        private ITNTResponseArgs ReadPLC(string addr, /*ref string msg, ref int leng, */int level, CancellationToken token = default)
        {
            string className = "PLCABTCP2";
            string funcName = "ReadPLC";
            //ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            string sCurrentFunc = "READ PLC";

            try
            {
                if ((ABPLCControl != null) && bOpend)
                {
                    lock (cmdLock)
                    {
                        value = ABPLCControl.Read(addr);
                        retval.recvString = value.PadLeft(8, '0');
                        retval.recvSize = retval.recvString.Length;

                        byte[] recvBuff = Encoding.UTF8.GetBytes(value);
                        if(recvBuff.Length > 0)
                        {
                            if(recvBuff.Length < retval.recvBuffer.Length)
                                Array.Copy(recvBuff, retval.recvBuffer, recvBuff.Length);
                            else
                                Array.Copy(recvBuff, retval.recvBuffer, retval.recvBuffer.Length);
                        }

                        //value = value.PadLeft(8, '0');
                        ////value = "00FF" + value;
                        //byte[] recvBuff = Encoding.UTF8.GetBytes(value);
                        //if (recvBuff.Length > 0)
                        //    Array.Copy(recvBuff, msg, recvBuff.Length);
                        //leng = recvBuff.Length;

                        //value = ABPLCControl.Read(addr);
                        //retval.recvString = value.PadLeft(8, '0');
                        ////msg = "00FF" + value;
                        //retval.recvSize = retval.recvString.Length;
                        ////ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "READ DATA : " + msg, Thread.CurrentThread.ManagedThreadId);
                    }
                }
                else
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") ERROR = PORT NOT OPENED";
                }
            }
            catch (Exception ex)
            {
                ClosePLC();
                statusArg.newstatus = csConnStatus.Disconnected;
                if (statusArg.newstatus != connState)
                {
                    statusArg.oldstatus = connState;
                    connState = statusArg.newstatus;
                    ConnectCallback?.Invoke(statusArg);
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                return retval;
            }

            //ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        //private ITNTResponseArgs ReadPLC(string addr, byte[] msg, ref int leng, int level, CancellationToken token = default)
        //private ITNTResponseArgs ReadPLC(string addr, /*byte[] msg, ref int leng, */int level, CancellationToken token = default)
        //{
        //    string className = "PLCABTCP2";
        //    string funcName = "ReadPLC";
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    string value = "";
        //    DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
        //    string sCurrentFunc = "READ PLC";
        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //        if ((ABPLCControl != null) && bOpend)
        //        {
        //            lock (cmdLock)
        //            {
        //                value = ABPLCControl.Read(addr);
        //                value = value.PadLeft(8, '0');
        //                //value = "00FF" + value;
        //                byte[] recvBuff = Encoding.UTF8.GetBytes(value);
        //                if (recvBuff.Length > 0)
        //                    Array.Copy(recvBuff, msg, recvBuff.Length);
        //                leng = recvBuff.Length;
        //            }
        //        }
        //        else
        //        {
        //            retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
        //            retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
        //            retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
        //            retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
        //            retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_CAUSE1_DOWN + (-retval.execResult).ToString("X2");
        //            retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") ERROR = PORT NOT OPENED";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ClosePLC();
        //        //ABPLCControl.CloseConnection();
        //        //bOpend = false;

        //        statusArg.newstatus = csConnStatus.Disconnected;
        //        if (statusArg.newstatus != connState)
        //        {
        //            statusArg.oldstatus = connState;
        //            connState = statusArg.newstatus;
        //            ConnectCallback?.Invoke(statusArg);
        //        }

        //        retval.execResult = ex.HResult;
        //        retval.errorInfo.devErrorInfo.execResult = ex.HResult;
        //        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
        //        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
        //        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
        //        retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") EXCEPTION ERROR = " + ex.Message;
        //        retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }

        //    ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //    return retval;
        //}

        private ITNTResponseArgs ReadPLC4Test(string addr, /*ref string msg, ref int leng, */int level, CancellationToken token = default)
        {
            string className = "PLCABTCP2";
            string funcName = "ReadPLC4Test";

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            string sCurrentFunc = "READ PLC 2";

            try
            {
                if (ABPLCControl != null)
                {
                    lock (cmdLock)
                    {
                        value = ABPLCControl.Read(addr);
                        retval.recvString = value.PadLeft(8, '0');
                        retval.recvSize = retval.recvString.Length;
                        //msg = value.PadLeft(8, '0');
                        ////msg = "00FF" + value;
                        //leng = msg.Length;
                        ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "READ DATA : " + retval.recvString, Thread.CurrentThread.ManagedThreadId);
                    }
                }
                else
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") ERROR = PORT NOT OPENED";
                }
            }
            catch (Exception ex)
            {
                ClosePLC();

                statusArg.newstatus = csConnStatus.Disconnected;
                if (statusArg.newstatus != connState)
                {
                    statusArg.oldstatus = connState;
                    connState = statusArg.newstatus;
                    ConnectCallback?.Invoke(statusArg);
                }
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private ITNTResponseArgs WritePLC(string addr, int count, byte[] buffer, int level, CancellationToken token = default)
        {
            string className = "PLCABTCP2";
            string funcName = "WritePLC";

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTResponseArgs retval = new ITNTResponseArgs();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            string sCurrentFunc = "WRITE PLC";

            try
            {
                if ((ABPLCControl != null) && bOpend)
                {
                    byte[] buf = new byte[count];
                    Array.Copy(buffer, buf, count);
                    lock (cmdLock)
                    {
                        ABPLCControl.Write(addr, buf);
                    }
                }
                else
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") ERROR = PORT NOT OPENED";
                }
            }
            catch (Exception ex)
            {
                ClosePLC();
                statusArg.newstatus = csConnStatus.Disconnected;
                if (statusArg.newstatus != connState)
                {
                    statusArg.oldstatus = connState;
                    connState = statusArg.newstatus;
                    ConnectCallback?.Invoke(statusArg);
                }

                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private ITNTResponseArgs WritePLC(string addr, string msg, int level, CancellationToken token = default)
        {
            string className = "PLCABTCP2";
            string funcName = "WritePLC";

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START (" + addr + ", " + msg + ")", Thread.CurrentThread.ManagedThreadId);
            ITNTResponseArgs retval = new ITNTResponseArgs();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            string sCurrentFunc = "WRITE PLC";

            try
            {
                if ((ABPLCControl != null) && bOpend)
                {
                    lock (cmdLock)
                    {
                        ABPLCControl.Write(addr, msg);
                    }
                }
                else
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") ERROR = PORT NOT OPENED";
                }
            }
            catch (Exception ex)
            {
                ClosePLC();
                statusArg.newstatus = csConnStatus.Disconnected;
                if (statusArg.newstatus != connState)
                {
                    statusArg.oldstatus = connState;
                    connState = statusArg.newstatus;
                    ConnectCallback?.Invoke(statusArg);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + addr + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async void PLCStatusThread(object obj)
        {
            string className = "PLCABTCP2";
            string funcName = "PLCStatusThread";

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            ITNTResponseArgs retval4status = new ITNTResponseArgs(128);
            ITNTResponseArgs retval4auto = new ITNTResponseArgs(128);
            ITNTSendArgs sndval4status = new ITNTSendArgs();
            string msg = "";
            CancellationTokenSource tokenSource = (CancellationTokenSource)obj;

            string IPAddr = "";
            string Port = "";
            string Slot = "";
            string statusAddr = "";
            ITNTResponseArgs args = new ITNTResponseArgs();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            string value = "";
            byte isUseHeartbeat = 0;
            Stopwatch sw = new Stopwatch();

            try
            {
                Util.GetPrivateProfileValue("PLCCOMM", "ServerIP", "192.168.1.200", ref IPAddr, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "ListenPort", "44818", ref Port, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "ProcessorSlot", "0", ref Slot, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D0100", "D100", ref statusAddr, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "USEHEARTBEAT", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out isUseHeartbeat);

                while (DoingPLCStatusThread)
                {
                    sw.Reset();
                    sw.Start();
                    if (!DoingPLCStatusThread)
                    {
                        sw.Stop();
                        break;
                    }
                    await Task.Delay(200);
                    if (!DoingPLCStatusThread)
                    {
                        sw.Stop();
                        break;
                    }

                    if ((ABPLCControl == null) || (bOpend == false))
                    {
                        if (ABPLCControl == null)
                        {
                            ABPLCControl = new EthernetIPforCLXCom();
                        }
                        ABPLCControl.BeginInit();
                        ABPLCControl.IPAddress = IPAddr;
                        ABPLCControl.Port = Convert.ToInt32(Port);
                        ABPLCControl.ProcessorSlot = Convert.ToInt32(Slot);
                        //ABPLCControl.Timeout = 1000;
                        ABPLCControl.EndInit();

                        lock (cmdLock)
                        {
                            args = ReadSignalFromPLC4Test().Result;
                            if (args.execResult == 0)
                            {
                                readPLCTimer.Interval = 1000;
                                readPLCTimer.Enabled = false;
                                readPLCTimer.AutoReset = false;
                                readPLCTimer.Enabled = true;

                                bTimerReadPLC = true;
                                //StartReadPLC();
                                bOpend = true;

                                statusArg.newstatus = csConnStatus.Connected;
                                if (statusArg.newstatus != connState)
                                {
                                    statusArg.oldstatus = connState;
                                    connState = statusArg.newstatus;
                                    ConnectCallback?.Invoke(statusArg);
                                }
                            }
                            else
                            {
                                //StopABReadPLC();
                                ////ABPLCControl.Unsubscribe(subscribeID);
                                //ABPLCControl.CloseConnection();
                                bOpend = false;

                                statusArg.newstatus = csConnStatus.Disconnected;
                                if (statusArg.newstatus != connState)
                                {
                                    statusArg.oldstatus = connState;
                                    connState = statusArg.newstatus;
                                    ConnectCallback?.Invoke(statusArg);
                                }
                                sw.Stop();
                                continue;
                            }
                        }
                        if (!DoingPLCStatusThread)
                        {
                            sw.Stop();
                            break;
                        }
                        await Task.Delay(1000);
                        if (!DoingPLCStatusThread)
                        {
                            sw.Stop();
                            break;
                        }
                    }
                    retval4status.Initialize();

                    retval4status = await ReadSignalFromPLC();
                    if (!DoingPLCStatusThread)
                        tokenSource.Cancel();
                    if (retval4status.execResult == 0)
                    {
                        retval4status.recvType = 1;
                        OnPLCStatusDataArrivedCallbackFunc(retval4status);
                    }
                    //ReadPLC(statusAddr, ref msg, 3);

                    //await SendHeartBeat();

                    await Task.Delay(200);
                    if (!DoingPLCStatusThread)
                    {
                        sw.Stop();
                        break;
                    }
                    sw.Stop();
                    if(isUseHeartbeat != 0)
                    {
                        await SendHeartBeat(1, 1);
                        if (sw.ElapsedMilliseconds > 5 * 1000)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READING TIME = " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
                        }
                    }

                    if (!DoingPLCStatusThread)
                        break;
                    //await Task.Delay(200);
                    //if (!DoingPLCStatusThread)
                    //    break;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                _plcStatusThread = null;
            }
            catch (Exception ex)
            {
                sw.Stop();
            }

        }


        protected virtual void OnPLCStatusDataArrivedCallbackFunc(ITNTResponseArgs e)
        {
            //PLCDataArrivedEventHandler handler = PLCDataArrivedEventFunc;
            //if (handler != null)
            //    handler(this, e);
            EventArrivalCallback?.Invoke(e);
        }






        /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ITNTResponseArgs> SendPLCSignalAsync(byte signal, CancellationToken token = default)
        {
            string className = "PLCABTCP2";
            string funcName = "SendPLCSignalAsync";
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs();
            string value = "";

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            sendArg.AddrString = PLC_ADDRESS_SIGNAL;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            //string value = "";
            value = string.Format("{0:D4}", signal);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            //string val = result.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D200", "D200", ref value, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = signal.ToString();
            sendArg.sendBuffer[0] = signal;
            //sendArg.AddrString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMatchingResult(byte result, CancellationToken token = default)
        {
            string className = "PLCABTCP2";
            string funcName = "SendMatchingResult";
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs();
            string value = "";

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            sendArg.AddrString = PLC_ADDRESS_MATCHRESULT;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            //string value = "";
            value = string.Format("{0:D4}", result);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            string val = result.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D200", "D200", ref value, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            //sendArg.AddrString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType, CancellationToken token=default)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);

            sendArg.AddrString = PLC_ADDRESS_SEQUENCE;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", frameType);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D800", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            string val = frameType.PadLeft(4, '0');
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);
            val = "0004";
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D800", "D800", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
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

        public async Task<ITNTResponseArgs> SendPCError2PLC(string plcvalue, CancellationToken token = default)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);

            sendArg.AddrString = PLC_ADDRESS_SETERRORCODE;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", plcvalue);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D210", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            string val = plcvalue.PadLeft(4, '0');
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D210", "D210", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel = 2)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);

            sendArg.AddrString = PLC_ADDRESS_SIGNAL;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
            //value = Encoding.UTF8.GetBytes(value);
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length / 2;
#else

            sendArg.msgType = 0;
            //sendArg.sendString = val;
            sendArg.loglevel = 3;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadSignalFromPLC(CancellationToken token = default)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            string value = "";
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D100", "D100", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);

            sendArg.AddrString = PLC_ADDRESS_SIGNAL;
            //int.TryParse(PLC_ADDRESS_SIGNAL, out sendArg.Address);

#if TEST_DEBUG_PLC
            //value = Encoding.UTF8.GetBytes(value);            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");

            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length;
#else
            sendArg.msgType = 0;
            //sendArg.sendString = val;
            sendArg.loglevel = 3;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSignalFromPLC4Test(CancellationToken token = default)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            string value = "";
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D100", "D100", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);

            sendArg.AddrString = PLC_ADDRESS_SIGNAL;
            //int.TryParse(PLC_ADDRESS_SIGNAL, out sendArg.Address);

#if TEST_DEBUG_PLC
            //value = Encoding.UTF8.GetBytes(value);            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");

            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length / 2;
#else
            sendArg.msgType = 0;
            //sendArg.sendString = val;
            sendArg.loglevel = 3;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync4Test(sendArg, token);
#endif
            return retval;
        }

        //        public async Task<ITNTResponseArgs> ReadSignalFromPLC()
        //        {
        //            byte[] recv = new byte[32];
        //            ITNTResponseArgs retval = new ITNTResponseArgs(64);
        //            ITNTSendArgs sendArg = new ITNTSendArgs(64);
        //#if TEST_DEBUG_PLC
        //            string value = "";
        //            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
        //            //value = Encoding.UTF8.GetBytes(value);
        //            retval.recvString = value;
        //            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
        //            retval.recvSize = value.Length / 2;
        //#else
        //            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D100", "D100", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
        //            sendArg.msgType = 0;
        //            //sendArg.sendString = val;
        //            sendArg.loglevel = 3;
        //            sendArg.timeout = 1;

        //            retval = await ReadPLCAsync2(sendArg);
        //#endif
        //            return retval;
        //        }

        public async Task<ITNTResponseArgs> ReadPLCCarType(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_CARTYPE;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);


#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CAR", "00000008", ref value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_CAR", "PLC_CAR", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            //sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            //byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_SEQUENCE;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQ", "00000008", ref value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_SEQ", "PLC_SEQ", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            //sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg, token);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadLinkStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_LINKSTATUS;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);


#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_LINKSTATUS", "00FF0001", ref value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_CAR", "PLC_CAR", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            //sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg, token);
#endif
            return retval;
        }




        public async Task<ITNTResponseArgs> SendSignal(byte signal, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "00FF" + signal.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = signal.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D200", "D200", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_MARKSTATUS;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "00FF" + status.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = status.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D230_MARK", "D230", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_VISIONRESULT;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

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
            string val = "";
            if (result == "O")
                val = 1.ToString("D4");
            else if (result == "S")
                val = 4.ToString("D4");
            else            //"N"
                val = 2.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(value);
            //Array.Copy(temp, recv, temp.Length);

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D240_VISION", "D240", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_SETERRORCODE;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            value = error.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = error.ToString("D4");

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D240_VISION", "D240", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            //retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_SETERRORCODE;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            value = error.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = error.ToString("D4");

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D240_VISION", "D240", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            //retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "";
            value = distance.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = distance.ToString("D4");

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D250", "D250", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendScanComplete(CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_SCANCOMPLETE;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            value = 1.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D250", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = 1.ToString("D4");

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D250_SCAN_COMP", "D250", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendCountWanring(byte status, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "";
            value = 1.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D250", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = status.ToString("D4");

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D0260", "D260", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendAirAsync(byte air, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_SETAIR;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            value = 1.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D250", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = air.ToString("D4");

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D250_SCAN_COMP", "D250", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendLinkAsync(byte link, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_SETLINK;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "";
            value = 1.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D250", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = link.ToString("D4");

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D250_SCAN_COMP", "D250", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SetEmissionOnOff(byte emission, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_PCEMISSION;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "00FF" + emission.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = emission.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D230_MARK", "D230", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);

#endif
            return retval;
        }

        
        public async Task<ITNTResponseArgs> ReadEmssionStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_EMISSIONSTATUS;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);


#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CAR", "00000008", ref value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_CAR", "PLC_CAR", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            //sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg, token);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> SetLaserPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_LASERPOWEROFF;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "00FF" + status.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = status.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D230_MARK", "D230", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendLaserLowPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_LASERPOWERLOW;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "00FF" + status.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = status.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D230_MARK", "D230", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }
        public async Task<ITNTResponseArgs> SendHeartBeat(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            sendArg.AddrString = PLC_ADDRESS_HEARTBEAT;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "00FF" + status.ToString("D4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D230", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = status.ToString("D4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            //Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D230_MARK", "D230", ref sendArg.AddrString, Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = loglevel;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingCanceled(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);

            sendArg.AddrString = PLC_ADDRESS_MARKCANCEL;
            //int.TryParse(sendArg.AddrString, out sendArg.Address);

#if TEST_DEBUG_PLC
            string value = "00FF" + status.ToString("D4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MARKCANCEL", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string val = status.ToString("D4");

            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = loglevel;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg, token);
#endif
            return retval;
        }




    }
}
