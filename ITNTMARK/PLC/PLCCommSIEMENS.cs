using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITNTCOMMON;
using ITNTMARK.PLC;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014


namespace ITNTMARK
{
    class PLCCommSIEMENS
    {
        PLCSIEMENSTCP tcpComm;
        PLC_COMM_TYPE plcCommType = 0;

        public PLCCommSIEMENS(PLCDataArrivedCallbackHandler callback, PLCConnectionStatusChangedEventHandler statusCallback)
        {
            tcpComm = new PLCSIEMENSTCP(callback, statusCallback);
        }

        public ITNTResponseArgs OpenPLC(PLC_COMM_TYPE commType)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            plcCommType = commType;
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = tcpComm.OpenPLC(2);
            return retval;
        }

        public int ClosePLC(PLC_COMM_TYPE type)
        {
            int retval = 0;
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                tcpComm.ClosePLC(1);
            return retval;
        }


        public async Task<ITNTResponseArgs> OpenPLCAsync(PLC_COMM_TYPE commType, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            plcCommType = commType;
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.OpenPLCAsync(2, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync(string IP, int port, string rackno, string slotno, PLC_COMM_TYPE commType, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            plcCommType = commType;
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.OpenPLCAsync(IP, port, rackno, slotno, 2, token);
            return retval;
        }

        //public int ReadPLC<T>(int address, int count, ref T buff)
        //{
        //    int retval = 0;
        //    return retval;
        //}

        //public int WritePLC<T>(int address, int count, T buff)
        //{
        //    int retval = 0;


        //    return retval;
        //}

        //public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    return retval;
        //}

        //public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCAsync(sendArg, token);
            return retval;
        }

        //public async Task<ITNTResponseArgs> ReadPLCAsync(string strAdd, int loglevel = 0, CancellationToken token = default)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //        retval = await tcpComm.ReadPLCAsync(strAdd, loglevel, token);
        //    return retval;
        //}

        //public ITNTResponseArgs ReadPLC(string strAdd, int loglevel= 0)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //        retval = tcpComm.ReadPLC(strAdd, loglevel);
        //    return retval;
        //}

        //public int ReadPLC(int address, int count, ref PLCReadDataArgs buff)
        //{
        //    int retval = 0;
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.WritePLCAsync(sendArg, token);
            return retval;
        }

        //public async Task<ITNTResponseArgs> WritePLCAsync(string sAddress, string sWriteData, int loglevel = 0, CancellationToken token = default)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //        retval = await tcpComm.WritePLCAsync(sAddress, sWriteData, loglevel, token);
        //    return retval;
        //}

        //public ITNTResponseArgs WritePLC(string sAddress, string sWriteData)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //        retval = tcpComm.WritePLC(sAddress, sWriteData);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendMatchingResult(byte result, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMatchingResult(result, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendFrameType2PLC(frameType, loglevel, token);
            return retval;
        }

        //public async Task<ITNTResponseArgs> SendMarkFinish()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //        retval = await tcpComm.SendMarkFinish(0);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendPCError2PLC(string plcvalue, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendPCError2PLC(plcvalue, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadSignalFromPLCAsync(loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType(int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCCarType(loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCSequence(loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCChinaFlag(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCChinaFlag(loglevel, token);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendSignal(byte signal, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendSignal(signal, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status, byte order=1, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMarkingStatus(status, order, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result, byte order=0, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendVisionResult(result, order, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address, int loglevel=0, CancellationToken token= default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendErrorInfo(error, address, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendErrorInfo(error, loglevel, token);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMovingRobot(distance,loglevel, token);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendScanComplete(byte scanstatus, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendScanComplete(scanstatus, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendCountWanring(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendCountWanring(status, loglevel, token);
            return retval;
        }




//////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ITNTResponseArgs> SetCommSettingTCP(string IP, int Port, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SetCommSettingTCP(IP, Port, loglevel, token);
            else
                return retval;
            return retval;
        }

        public ITNTResponseArgs GetCommSettingTCP(ref string IP, ref int Port)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = tcpComm.GetCommSettingTCP(ref IP, ref Port);
            else
                return retval;
            return retval;
        }

        public async Task<ITNTResponseArgs> SetCommSettingCOM(string PortNum, int BaudRate, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                return retval;
            return retval;
        }

        public async Task<ITNTResponseArgs> CheckConnection(int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = tcpComm.CheckConnection(loglevel);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetLinkAsync(byte link, int loglevel=0, CancellationToken token= default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SetLinkAsync(link, loglevel, token);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendAirAsync(byte air, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendAirAsync(air, loglevel, token);
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadLinkStatusAsync(int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadLinkStatusAsync(loglevel, token);
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadVINAsync(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadVINAsync(token);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendPLCSignalAsync(byte signal, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendPLCSignalAsync(signal, loglevel, token);
            return retval;
        }

    }
}
