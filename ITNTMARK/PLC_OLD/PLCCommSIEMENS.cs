using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTCOMMON;

namespace ITNTMARK
{
    class PLCCommSIEMENS
    {
        public PLCCommSIEMENS(PLCDataArrivedCallbackHandler callback)
        {

        }

        public int OpenPLC(PLC_COMM_TYPE commType)
        {
            int retval = 0;
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
            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            return retval;
        }


    }
}
