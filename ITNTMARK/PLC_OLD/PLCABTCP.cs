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

namespace ITNTMARK
{
    class PLCABTCP
    {
        EthernetIPforCLXCom ABPLCControl = new EthernetIPforCLXCom();
        private PLCDataArrivedCallbackHandler EventArrivalCallback;

        System.Timers.Timer readPLCTimer = new System.Timers.Timer();
        bool bOpend = false;
        bool bTimerReadPLC = false;
        private int errorCount = 0;
        string statusAddr = "";

        private readonly object cmdLock = new object();

        bool DoingPLCStatusThread = false;
        Thread _plcStatusThread = null;

        public PLCABTCP(PLCDataArrivedCallbackHandler callback)
        {
            //callbackHandler = callback;
            EventArrivalCallback = callback;
        }

        public int OpenPLC(PLC_COMM_TYPE commType)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            int retval = 0;
            string IPAddr = "", Port = "", Slot = "";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + commType.ToString(), Thread.CurrentThread.ManagedThreadId);

            try
            {
                lock(cmdLock)
                {
                    Util.GetPrivateProfileValue("PLCCOMM", "ServerIP", "192.168.1.200", ref IPAddr, ITNTCOMMON.Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "ListenPort", "44818", ref Port, ITNTCOMMON.Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "ProcessorSlot", "0", ref Slot, ITNTCOMMON.Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D0100", "D100", ref statusAddr, ITNTCOMMON.Constants.PARAMS_INI_FILE);

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
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public int ClosePLC(PLC_COMM_TYPE commType)
        {
            int retval = 0;
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                lock(cmdLock)
                {
                    StopABReadPLC();
                    //ABPLCControl.Unsubscribe(subscribeID);
                    ABPLCControl.CloseConnection();
                    bOpend = false;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private int StopABReadPLC()
        {
            int retval = 0;
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCControl", "StopABReadPLC", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        void TimerCallbackFunc(object sender, ElapsedEventArgs e)
        {
            string className = "PLCControl";
            string funcName = "TimerCallbackFunc";
            string rmsg = "";
            int retval = 0;
            int leng = 0;
            try
            {
                if (bTimerReadPLC)
                {
                    retval = ReadPLC("VISION_COM", ref rmsg, ref leng, 3);
                    if (retval == 0)
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
                ITNTTraceLog.Instance.Trace(3, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
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
                for(int i = 0; i < 3; i++)
                {
                    retval.execResult = ReadPLC(sendArg.AddrString, retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    retval.execResult = ReadPLC(sendArg.AddrString, ref retval.recvString, ref retval.recvSize, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCAsync2(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] temp;
            try
            {
                retval.msgtype = sendArg.msgType;
                for (int i = 0; i < 3; i++)
                {
                    retval.execResult = ReadPLC(sendArg.AddrString, ref retval.recvString, ref retval.recvSize, sendArg.loglevel);
                    if (retval.execResult == 0)
                    {
                        temp = Encoding.UTF8.GetBytes(retval.recvString);
                        Array.Copy(temp, retval.recvBuffer, temp.Length);
                        break;
                    }
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            retval.msgtype = sendArg.msgType;
            if (sendArg.msgType == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    retval.execResult = WritePLC(sendArg.AddrString, sendArg.dataSize, sendArg.sendBuffer, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    retval.execResult = WritePLC(sendArg.AddrString, sendArg.sendString, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync2(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                retval.msgtype = sendArg.msgType;
                for (int i = 0; i < 3; i++)
                {
                    retval.execResult = WritePLC(sendArg.AddrString, sendArg.sendString, sendArg.loglevel);
                    if (retval.execResult == 0)
                        break;
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        private int ReadPLC(string addr, ref string msg, ref int leng, int level)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;
            string value = "";
            try
            {
                if ((ABPLCControl != null) || bOpend)
                {
                    lock(cmdLock)
                    {
                        value = ABPLCControl.Read(addr);
                        msg = "00FF" + value;
                        leng = msg.Length;
                        ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "READ DATA : " + msg, Thread.CurrentThread.ManagedThreadId);
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private int ReadPLC(string addr, byte[] msg, ref int leng, int level)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;
            string value = "";
            try
            {
                if ((ABPLCControl != null) || bOpend)
                {
                    lock(cmdLock)
                    {
                        value = ABPLCControl.Read(addr);
                        value = "00FF" + value;
                        byte[] recvBuff = Encoding.UTF8.GetBytes(value);
                        if (recvBuff.Length > 0)
                            Array.Copy(recvBuff, msg, recvBuff.Length);
                        leng = recvBuff.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private int WritePLC(string addr, int count, byte[] buffer, int level)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;

            try
            {
                if ((ABPLCControl != null) || bOpend)
                {
                    byte[] buf = new byte[count];
                    Array.Copy(buffer, buf, count);
                    lock (cmdLock)
                    {
                        ABPLCControl.Write(addr, buf);
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private int WritePLC(string addr, string msg, int level)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "START (" + addr + ", " + msg + ")", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;

            try
            {
                if ((ABPLCControl != null) || bOpend)
                {
                    lock(cmdLock)
                    {
                        ABPLCControl.Write(addr, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async void PLCStatusThread()
        {
            string className = "PLCControl";
            string funcName = "PLCStatusThread";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            ITNTResponseArgs retval4status = new ITNTResponseArgs(128);
            ITNTResponseArgs retval4auto = new ITNTResponseArgs(128);
            ITNTSendArgs sndval4status = new ITNTSendArgs();
            string msg = "";

            while (DoingPLCStatusThread)
            {
                if (!DoingPLCStatusThread)
                    break;
                await Task.Delay(200);
                if (!DoingPLCStatusThread)
                    break;

                retval4status.Initialize();
                retval4status = await ReadSignalFromPLC();
                if (retval4status.execResult == 0)
                {
                    retval4status.recvType = 1;
                    OnPLCStatusDataArrivedCallbackFunc(retval4status);
                }
                //ReadPLC(statusAddr, ref msg, 3);

                if (!DoingPLCStatusThread)
                    break;
                await Task.Delay(200);
                if (!DoingPLCStatusThread)
                    break;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            _plcStatusThread = null;
        }

        protected virtual void OnPLCStatusDataArrivedCallbackFunc(ITNTResponseArgs e)
        {
            //PLCDataArrivedEventHandler handler = PLCDataArrivedEventFunc;
            //if (handler != null)
            //    handler(this, e);
            EventArrivalCallback?.Invoke(e);
        }

        public async Task<ITNTResponseArgs> SendMatchingResult(byte result)
        {
            string className = "PLCControl";
            string funcName = "PLCStatusThread";
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", result);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = 4;
#else
            string val = result.ToString("X4");
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);
            string value = "";
            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D200", "D200", ref value, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            ITNTSendArgs sendArg = new ITNTSendArgs();
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.AddrString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
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
            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D800", "D800", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
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

        public async Task<ITNTResponseArgs> SendPLCValue2PLC(string plcvalue)
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
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

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D210", "D210", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSignalFromPLC()
        {
            byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
            //value = Encoding.UTF8.GetBytes(value);
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(value);
            retval.recvSize = value.Length / 2;
#else
            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D100", "D100", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 0;
            //sendArg.sendString = val;
            sendArg.loglevel = 3;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg);
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
            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_CAR", "PLC_CAR", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            //sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "";
            Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQ", "00000008", ref value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_SEQ", "PLC_SEQ", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            //sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await ReadPLCAsync2(sendArg);
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
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D200", "D200", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
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
            //byte[] temp = Encoding.UTF8.GetBytes(val);
            //Array.Copy(temp, recv, temp.Length);

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D230_MARK", "D230", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
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
            string val = "";
            if (result == "O")
                val = 1.ToString("X4");
            else if (result == "S")
                val = 4.ToString("X4");
            else            //"N"
                val = 2.ToString("X4");
            //byte[] temp = Encoding.UTF8.GetBytes(value);
            //Array.Copy(temp, recv, temp.Length);

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D240_VISION", "D240", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = val;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sendArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

#if TEST_DEBUG_PLC
            string value = "";
            value = error.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = 4;
#else
            string value = "";
            value = error.ToString("X4");

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D240_VISION", "D240", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance)
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
            value = distance.ToString("X4");

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D250", "D250", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendScanComplete()
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
            value = 1.ToString("X4");

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D250_SCAN_COMP", "D250", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SendCountWanring(byte status)
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
            value = status.ToString("X4");

            Util.GetPrivateProfileValue("PLCCOMM", "PLC_ADDRESS_D0260", "D260", ref sendArg.AddrString, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            sendArg.msgType = 1;
            sendArg.sendString = value;
            sendArg.loglevel = 0;
            sendArg.timeout = 1;

            retval = await WritePLCAsync2(sendArg);
#endif
            return retval;
        }

    }
}
