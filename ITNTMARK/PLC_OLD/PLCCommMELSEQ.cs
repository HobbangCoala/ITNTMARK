using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTUTIL;
using ITNTCOMMON;

namespace ITNTMARK
{
    class PLCCommMELSEQ
    {
        PLC_COMM_TYPE plcCommType = 0;
        PLCMELSEQSerial melseqSerial;// = new PLCMELSEQSerial();
        //PLCDataArrivedCallbackHandler callbackHandler;

        public PLCCommMELSEQ(PLCDataArrivedCallbackHandler callback)
        {
            //callbackHandler = callback;
            melseqSerial = new PLCMELSEQSerial(callback);
        }

        public async Task<int> OpenPLCAsync(PLC_COMM_TYPE commType)
        {
            int retval = 0;
            string value = "";
            string port = "";
            int baudrate = 0;
            int databit = 0;
            try
            {
                Util.GetPrivateProfileValue("PLCCOMM", "PORT", "COM3", ref port, ITNTCOMMON.Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "BAUDRATE", "38400", ref value, ITNTCOMMON.Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out baudrate);
                Util.GetPrivateProfileValue("PLCCOMM", "DATABIT", "8", ref value, ITNTCOMMON.Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out databit);
                plcCommType = commType;

                if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                {
                    retval = await melseqSerial.OpenPLCAsync(port, baudrate, databit);
                }
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
            }
            return retval;
        }

        public int ClosePLC(PLC_COMM_TYPE commType)
        {
            int retval = 0;
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

        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.ReadPLCAsync(sendArg);
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
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.WritePLCAsync(sendArg);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMatchingResult(byte result)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.SendMatchingResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.SendFrameType2PLC(frameType);
            return retval;
        }

        //public async Task<ITNTResponseArgs> SendMarkFinish()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
        //        retval = await melseqSerial.SendMarkFinish(0);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendPLCValue2PLC(string plcvalue)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.SendPLCValue2PLC(plcvalue);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.ReadPLCCarType();
            return retval;
        }

        public async Task<ITNTResponseArgs> SendSignal(byte signal)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.SendSignal(signal);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.SendMarkingStatus(status);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.SendVisionResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
                retval = await melseqSerial.SendErrorInfo(error, address);
            return retval;
        }

        //public int WritePLC(int address, int count, PLCWriteDataArgs arg)
        //{
        //    int retval = 0;
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
        //    {
        //        retval = melseqSerial.WritePLC(address, count, arg);
        //    }


        //    return retval;
        //}
    }
}
