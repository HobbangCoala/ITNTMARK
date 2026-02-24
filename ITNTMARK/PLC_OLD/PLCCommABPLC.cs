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

namespace ITNTMARK
{
    class PLCCommABPLC
    {
        PLC_COMM_TYPE commType = PLC_COMM_TYPE.PLC_COMM_TYPE_TCP;
        PLCABTCP tcpComm;// = new PLCABTCP()

        public PLCCommABPLC(PLCDataArrivedCallbackHandler callback)
        {
            //EventArrivalCallback = callback;
            tcpComm = new PLCABTCP(callback);
        }

        public int OpenPLC(PLC_COMM_TYPE commType)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            int retval = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - " + commType.ToString(), Thread.CurrentThread.ManagedThreadId);

            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                retval = tcpComm.OpenPLC(commType);
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
                retval = tcpComm.ClosePLC(commType);
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

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                retval = await tcpComm.WritePLCAsync(sendArg);
            }
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

        public async Task<ITNTResponseArgs> SendPLCValue2PLC(string plcvalue)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendPLCValue2PLC(plcvalue);
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
        
    }
}
