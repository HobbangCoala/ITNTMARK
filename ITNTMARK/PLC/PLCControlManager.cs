using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITNTCOMMON;
using ITNTUTIL;

#pragma warning disable 0642
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public delegate Task PLCDataArrivedCallbackHandler(ITNTResponseArgs e);

    public enum PLC_DEVICE_TYPE
    {
        PLC_DEVICE_TYPE_NONE = 0,
        PLC_DEVICE_TYPE_MELSEC = 1,
        PLC_DEVICE_TYPE_SIMENS = 2,
        PLC_DEVICE_TYPE_AB = 3,
        PLC_DEVICE_TYPE_LS = 4,
    }

    public enum PLC_COMM_TYPE
    {
        PLC_COMM_TYPE_NONE = 0,
        PLC_COMM_TYPE_RS232 = 1,
        PLC_COMM_TYPE_TCP = 2,
        PLC_COMM_TYPE_API = 3,
    }

    public class PLCControlManager
    {
        //public event PLCDataArrivedEventHandler dataArrivedEvent = null;

        public PLC_DEVICE_TYPE plcType = PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_NONE;
        public PLC_COMM_TYPE plcCommType = PLC_COMM_TYPE.PLC_COMM_TYPE_NONE;

        PLCCommMELSEQ melseqPLCComm;// = new PLCCommMELSEQ();
        PLCCommABPLC abPLCComm;// = new PLCCommABPLC();
        PLCCommSIEMENS siemensComm;// = new PLCCommSIEMENS();
        PLCCommLSPLC lsPLCComm;// = new PLCCommABPLC();
        int PLC_ERR_UNSUPPORTED_PLC = -20;

        bool bPLCOpened = false;
        PLCDataArrivedCallbackHandler callbackHndler;

        public const byte SIGNAL_PC2PLC_OFF = 0;
        public const byte SIGNAL_PC2PLC_ON = 1;

        public const string SIGNAL_PLC2PC_OFF = "0000";
        public const string SIGNAL_PLC2PC_ON = "0001";
        //public const byte PLC_MARK_STATUS_COMPLETE = 2;

        public PLCControlManager(PLCDataArrivedCallbackHandler callback, PLCConnectionStatusChangedEventHandler statusCallback)
        //public PLCControlManager(PLCDataArrivedCallbackHandler callback)
        { 
            string value = "";
            Util.GetPrivateProfileValue("PLCCOMM", "PLCTYPE", "1", ref value, Constants.PARAMS_INI_FILE);
            int type = 0;
            Int32.TryParse(value, out type);
            plcType = (PLC_DEVICE_TYPE)type;

            Util.GetPrivateProfileValue("PLCCOMM", "COMMTYPE", "1", ref value, Constants.PARAMS_INI_FILE);
            Int32.TryParse(value, out type);
            plcCommType = (PLC_COMM_TYPE)type;

            melseqPLCComm = new PLCCommMELSEQ(callback);
            abPLCComm = new PLCCommABPLC(callback, statusCallback);
            siemensComm = new PLCCommSIEMENS(callback, statusCallback);
            //lsPLCComm = new PLCCommLSPLC(callback);
            lsPLCComm = new PLCCommLSPLC(callback, statusCallback);
            callbackHndler = callback;
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync(CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (bPLCOpened)
            {
                retval.execResult = 0;
                return retval;
            }

            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.OpenPLCAsync(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.OpenPLCAsync(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.OpenPLCAsync(plcCommType, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.OpenPLCAsync(plcCommType);
            //            else
            //                retval = 0;
            //retval = await melseqPLCComm.OpenPLCAsync(plcCommType);
            if (retval.execResult == 0)
                bPLCOpened = true;
            return retval;
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync(string IP, int port, string rackno, string slotno, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (bPLCOpened)
            {
                retval.execResult = 0;
                return retval;
            }

            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ; //retval = abPLCComm.OpenPLC(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ; //retval = await melseqPLCComm.OpenPLCAsync(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.OpenPLCAsync(IP, port, rackno, slotno, plcCommType, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.OpenPLCAsync(IP, port, plcCommType);
            //            else
            //                retval = 0;
            //retval = await melseqPLCComm.OpenPLCAsync(plcCommType);
            if (retval.execResult == 0)
                bPLCOpened = true;
            return retval;
        }

        public ITNTResponseArgs OpenPLC()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (bPLCOpened)
            {
                retval.execResult = 0;
                return retval;
            }

            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = abPLCComm.OpenPLC(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = melseqPLCComm.OpenPLC(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = siemensComm.OpenPLC(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = lsPLCComm.OpenPLC(plcCommType);
            //            else
            //                retval = 0;
            //retval = await melseqPLCComm.OpenPLCAsync(plcCommType);
            if (retval.execResult == 0)
                bPLCOpened = true;
            return retval;
        }

        public int ClosePLC()
        {
            int retval = 0;
            if (!bPLCOpened)
                return 0;

            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = abPLCComm.ClosePLC(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = melseqPLCComm.ClosePLC(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = siemensComm.ClosePLC(plcCommType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = lsPLCComm.ClosePLC();
//            else
//                retval = melseqPLCComm.ClosePLC(plcCommType);
            if (retval == 0)
                bPLCOpened = false;
            return retval;

        }

        //public int ReadPLC<T>(int address, int count, ref T buff)
        //{
        //    int retval = 0;

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        retval = abPLCComm.ReadPLC(address, count, ref buff);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        retval = melseqPLCComm.ReadPLC(address, count, ref buff);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = siemensComm.ReadPLC(address, count, ref buff);
        //    else
        //        retval = melseqPLCComm.ReadPLC(address, count, ref buff);
        //    return retval;
        //}

        //public int WritePLC<T>(int address, int count, T buff)
        //{
        //    int retval = 0;

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        retval = abPLCComm.WritePLC(address, count, buff);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        retval = melseqPLCComm.WritePLC(address, count, buff);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = siemensComm.WritePLC(address, count, buff);
        //    else
        //        retval = melseqPLCComm.WritePLC(address, count, buff);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.ReadPLCAsync(sendArg);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.ReadPLCAsync(sendArg);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.ReadPLCAsync(sendArg, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadPLCAsync(sendArg);
            //else
            //    retval = await melseqPLCComm.ReadPLCAsync(sendArg);
            return retval;
        }

        //public async Task<ITNTResponseArgs> ReadPLCAsync(string strAdd, int loglevel = 0, CancellationToken token = default)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        ;// retval = await abPLCComm.ReadPLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        ;// retval = await melseqPLCComm.ReadPLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = await siemensComm.ReadPLCAsync(strAdd, loglevel, token);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
        //        retval = await lsPLCComm.ReadPLCAsync(strAdd);
        //    //else
        //    //    retval = await melseqPLCComm.ReadPLCAsync(sendArg);
        //    return retval;
        //}

        //public ITNTResponseArgs ReadPLC(string strAdd, int loglevel = 0)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        ;// retval = await abPLCComm.ReadPLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        ;// retval = await melseqPLCComm.ReadPLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = siemensComm.ReadPLC(strAdd, loglevel);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
        //        retval = lsPLCComm.ReadPLC(strAdd);
        //    //else
        //    //    retval = await melseqPLCComm.ReadPLCAsync(sendArg);
        //    return retval;
        //}

        //public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg, CancellationToken token = default)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        retval = await abPLCComm.WritePLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        retval = await melseqPLCComm.WritePLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = await siemensComm.WritePLCAsync(sendArg, token);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
        //        retval = await lsPLCComm.WritePLCAsync(sendArg);
        //    //else
        //    //    retval = await melseqPLCComm.WritePLCAsync(sendArg);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> WritePLCAsync2(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.WritePLCAsync2(sendArg);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.WritePLCAsync(sendArg);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.WritePLCAsync(sendArg, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.WritePLCAsync(sendArg);
            //else
            //    retval = await melseqPLCComm.WritePLCAsync(sendArg);
            return retval;
        }

        //public async Task<ITNTResponseArgs> WritePLCAsync(string sAddress, string sWriteData, int loglevel = 0, CancellationToken token = default)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        ;// retval = await abPLCComm.WritePLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        ;// retval = await melseqPLCComm.WritePLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = await siemensComm.WritePLCAsync(sAddress, sWriteData, loglevel, token);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
        //        retval = await lsPLCComm.WritePLCAsync(sAddress, sWriteData);
        //    //else
        //    //    retval = await melseqPLCComm.WritePLCAsync(sendArg);
        //    return retval;
        //}

        //public ITNTResponseArgs WritePLC(string sAddress, string sWriteData)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        ;// retval = await abPLCComm.WritePLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        ;// retval = await melseqPLCComm.WritePLCAsync(sendArg);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = siemensComm.WritePLC(sAddress, sWriteData);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
        //        retval = lsPLCComm.WritePLC(sAddress, sWriteData);
        //    //else
        //    //    retval = await melseqPLCComm.WritePLCAsync(sendArg);
        //    return retval;
        //}
        
        public async Task<ITNTResponseArgs> SendPLCSignalAsync(byte signal, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendPLCSignalAsync(signal, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.SendPLCSignalAsync(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendPLCSignalAsync(signal, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                ;// retval = await lsPLCComm.SendPLCSignalAsync(signal);
            //else
            //    retval = await melseqPLCComm.SendMatchingResult(result);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMatchingResult(byte result, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendMatchingResult(result);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendMatchingResult(result);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendMatchingResult(result, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendMatchingResult(result);
            //else
            //    retval = await melseqPLCComm.SendMatchingResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendFrameType2PLC(frameType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendFrameType2PLC(frameType);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendFrameType2PLC(frameType, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendFrameType2PLC(frameType);
            //else
            //    retval = await melseqPLCComm.SendFrameType2PLC(frameType);
            return retval;
        }

        //public async Task<ITNTResponseArgs> SendMarkFinish()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        ;
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        retval = await melseqPLCComm.SendMarkFinish();
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        ;
        //    else
        //        retval = await melseqPLCComm.SendMarkFinish();
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendSignal(byte signal, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendSignal(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendSignal(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendSignal(signal, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendSignal(signal);
            //else
            //    retval = await melseqPLCComm.SendSignal(signal);
            return retval;
        }

        //public async Task<ITNTResponseArgs> CheckMatchingData(string carType)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        ;
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        retval = await melseqPLCComm.SendFrameType2PLC(carType);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        ;
        //    else
        //        retval = await melseqPLCComm.SendCarType2PLC(carType);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendPCError2PLC(string value, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendPCError2PLC(value);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendPCError2PLC(value);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendPCError2PLC(value, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendPCError2PLC(value, loglevel, token);
            //else
            //    retval = await melseqPLCComm.SendPLCValue2PLC(value);
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.ReadSignalFromPLCAsync(loglevel);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.ReadSignalFromPLCAsync(loglevel);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.ReadSignalFromPLCAsync(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadSignalFromPLCAsync(loglevel);
            //else
            //    retval.execResult = PLC_ERR_UNSUPPORTED_PLC;
            //retval = await melseqPLCComm.ReadPLCCarType();
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadPLCCarType(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.ReadPLCCarType();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.ReadPLCCarType();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.ReadPLCCarType(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadPLCCarType();
            //else
            //    retval.execResult = PLC_ERR_UNSUPPORTED_PLC;
            //retval = await melseqPLCComm.ReadPLCCarType();
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.ReadPLCSequence();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.ReadPLCSequence();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.ReadPLCSequence(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadPLCSequence();
            //else
            //    retval.execResult = PLC_ERR_UNSUPPORTED_PLC;
                //retval = await melseqPLCComm.ReadPLCCarType();
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadPLCChinaFlag(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ; //retval = await abPLCComm.ReadPLCSequence();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.ReadPLCSequence();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.ReadPLCChinaFlag(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadPLCChinaFlag();
            //else
            //    retval.execResult = PLC_ERR_UNSUPPORTED_PLC;
            //retval = await melseqPLCComm.ReadPLCCarType();
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadBodyNum(CancellationToken token = default)
        {

            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ;// retval = await abPLCComm.SendLaserPowerError(status, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.SendSignal(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                ;// retval = await siemensComm.SendSignal(signal, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadBodyNum(token);
            //else
            //    retval = await melseqPLCComm.SendSignal(signal);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadUseLaserNum(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ;// retval = await abPLCComm.SendLaserPowerError(status, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.SendSignal(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                ;// retval = await siemensComm.SendSignal(signal, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadUseLaserNum(token);
            //else
            //    retval = await melseqPLCComm.SendSignal(signal);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status, byte order=1, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendMarkingStatus(status);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendMarkingStatus(status);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendMarkingStatus(status, order, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendMarkingStatus(status, order);
            //else
            //    retval.execResult = PLC_ERR_UNSUPPORTED_PLC;
            //retval = await melseqPLCComm.SendMarkingStatus(status);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result, byte order=0, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendVisionResult(result);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendVisionResult(result);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendVisionResult(result, order, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendVisionResult(result, order);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendErrorInfo(error, address);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendErrorInfo(error, address);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendErrorInfo(error, address, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendErrorInfo(error, address);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendErrorInfo(error);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                retval = await melseqPLCComm.SendErrorInfo(error);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendErrorInfo(error, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendErrorInfo(error);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendMovingRobot(distance);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.SendMovingRobot(distance);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendMovingRobot(distance, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendMovingRobot(distance);
            //else
            //    retval = await melseqPLCComm.SendMatchingResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendScanComplete(byte scanstatus, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendScanComplete(scanstatus, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendScanComplete(scanstatus, loglevel, token);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }


        public async Task<ITNTResponseArgs> SetLinkAsync(byte link, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendLinkAsync(link, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SetLinkAsync(link, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SetLinkAsync(link);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadLinkStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.ReadLinkStatusAsync(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.ReadLinkStatusAsync(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.ReadLinkStatusAsync();
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendAirAsync(byte air, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendAirAsync(air, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendAirAsync(air, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SendAirAsync(air);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetEmissionOnOff(byte emission, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SetEmissionOnOff(emission);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.SendSignal(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                ;// retval = await siemensComm.SendSignal(signal, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                ;// retval = await lsPLCComm.SendSignal(signal);
            //else
            //    retval = await melseqPLCComm.SendSignal(signal);
            return retval;
        }


        public async Task<ITNTResponseArgs> ReadEmssionStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.ReadEmssionStatusAsync(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                ;// retval = await siemensComm.ReadLinkStatusAsync(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                ;// retval = await lsPLCComm.ReadLinkStatusAsync();
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadVINAsync(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ;//retval = await abPLCComm.ReadPLCAsync(sendArg);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.ReadPLCAsync(sendArg);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.ReadVINAsync(token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                ;//retval = await lsPLCComm.ReadPLCAsync(sendArg);
            //else
            //    retval = await melseqPLCComm.ReadPLCAsync(sendArg);
            return retval;
        }


        public async Task<ITNTResponseArgs> SetLaserPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SetLaserPowerError(status, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.SendSignal(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                ;// retval = await siemensComm.SendSignal(signal, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SetLaserPowerError(status, loglevel, token);
            //else
            //    retval = await melseqPLCComm.SendSignal(signal);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendLaserLowPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendLaserLowPowerError(status, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;// retval = await melseqPLCComm.SendSignal(signal);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                ;// retval = await siemensComm.SendSignal(signal, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                ;// retval = await lsPLCComm.SendSignal(signal);
            //else
            //    retval = await melseqPLCComm.SendSignal(signal);
            return retval;
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Commnication Setting Functions

        public async Task<ITNTResponseArgs> SetCommSettingTCP(string IP, int Port, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ; //retval = await abPLCComm.SetCommSettingTCP();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SetCommSettingTCP(IP, Port, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SetCommSettingTCP(IP, Port);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public ITNTResponseArgs GetCommSettingTCP(ref string IP, ref int Port)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ; //retval = await abPLCComm.SetCommSettingTCP();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = siemensComm.GetCommSettingTCP(ref IP, ref Port);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = lsPLCComm.GetCommSettingTCP(ref IP, ref Port);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendCountWanring(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendCountWanring(status);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SendCountWanring(status, loglevel, token);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetCommSettingCOM(string PortNum, int BaudRate, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ; //retval = await abPLCComm.SetCommSettingCOM();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.SetCommSettingCOM(PortNum, BaudRate, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.SetCommSettingCOM(PortNum, BaudRate);
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> CheckConnection(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                ; //retval = await abPLCComm.SetCommSettingTCP();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                retval = await siemensComm.CheckConnection(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                retval = await lsPLCComm.CheckConnection();
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingCanceled(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                retval = await abPLCComm.SendMarkingCanceled(status, loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                ;//retval = await melseqPLCComm.SendScanComplete();
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                ;// retval = await siemensComm.CheckConnection(loglevel, token);
            else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                ;// retval = await lsPLCComm.CheckConnection();
            //else
            //    retval = await melseqPLCComm.SendVisionResult(result);
            return retval;
        }

        //public int WritePLC(int address, int count, PLCWriteDataArgs args)
        //{
        //    int retval = 0;

        //    if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
        //        retval = abPLCComm.WritePLC(address, count, args);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
        //        retval = melseqPLCComm.WritePLC(address, count, args);
        //    else if (plcType == PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
        //        retval = siemensComm.WritePLC(address, count, args);
        //    else
        //        retval = melseqPLCComm.WritePLC(address, count, args);
        //    return retval;
        //}

        //void OnPLDataReceivedEvent(PLCDataArrivedEventArgs e)
        //{
        //    PLCDataArrivedEventHandler handler = dataArrivedEvent;
        //    if (handler != null)
        //        handler(this, e);
        //}

    }
}
