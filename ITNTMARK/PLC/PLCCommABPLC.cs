using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTCOMMON;
using AdvancedHMIDrivers;
using MfgControl.AdvancedHMI.Drivers.Common;
using System.Reflection;
using ITNTUTIL;
using System.Timers;
using System.Threading;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    class PLCCommABPLC
    {
        PLC_COMM_TYPE commType = PLC_COMM_TYPE.PLC_COMM_TYPE_TCP;
        //PLCABTCP2 tcpComm;// = new PLCABTCP()
        PLCABTCP7 tcpComm;// = new PLCABTCP()

        public PLCCommABPLC(PLCDataArrivedCallbackHandler callback, PLCConnectionStatusChangedEventHandler connectFunc)
        {
            //EventArrivalCallback = callback;
            tcpComm = new PLCABTCP7(callback, connectFunc);
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync(PLC_COMM_TYPE commType)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + commType.ToString(), Thread.CurrentThread.ManagedThreadId);

            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                retval = await tcpComm.OpenPLCAsync();
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public ITNTResponseArgs OpenPLC(PLC_COMM_TYPE commType)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + commType.ToString(), Thread.CurrentThread.ManagedThreadId);

            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                retval = tcpComm.OpenPLC();
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
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                retval = tcpComm.ClosePLC();
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                retval = await tcpComm.ReadPLCAsync(sendArg);
            }

            return retval;
        }

        //public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //    {
        //        retval = await tcpComm.WritePLCAsync(sendArg);
        //    }
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> WritePLCAsync2(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                retval = await tcpComm.WritePLCAsync2(sendArg);
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadSignalFromPLCAsync();
            return retval;
        }


       
        public async Task<ITNTResponseArgs> SendPLCSignalAsync(byte signal, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendPLCSignalAsync(signal, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMatchingResult(byte result)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMatchingResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendFrameType2PLC(frameType);
            return retval;
        }

        //public async Task<ITNTResponseArgs> SendMarkFinish()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //        retval = await melseqSerial.SendMarkFinish(0);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendPCError2PLC(string plcvalue)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendPCError2PLC(plcvalue);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCCarType();
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCSequence();
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadLinkStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadLinkStatusAsync();
            return retval;
        }

        

        public async Task<ITNTResponseArgs> SendSignal(byte signal)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendSignal(signal);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMarkingStatus(status);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendErrorInfo(error, address);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendErrorInfo(error);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMovingRobot(distance);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendScanComplete()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendScanComplete();
            return retval;
        }

        public async Task<ITNTResponseArgs> SendCountWanring(byte status)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendCountWanring(status);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendAirAsync(byte air, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendAirAsync(air, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendLinkAsync(byte link, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendLinkAsync(link, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetEmissionOnOff(byte emission, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SetEmissionOnOff(emission);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadEmssionStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadEmssionStatusAsync();
            return retval;
        }

        public async Task<ITNTResponseArgs> SetLaserPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SetLaserPowerError(status, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendLaserLowPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendLaserLowPowerError(status, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingCanceled(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMarkingCanceled(status, loglevel, token);
            return retval;
        }

    }
}
