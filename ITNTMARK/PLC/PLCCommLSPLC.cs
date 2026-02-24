using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ITNTCOMMON;
using ITNTUTIL;
using System.Threading;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public enum CommandType
    {
        Read = 84,  //0x54
        ReadResponse = 85, //0x55
        Write = 88,  //0x58
        WriteResponse = 89 //0x59
    }
    public enum DataType
    {
        Bit = 0x0000,
        Byte = 0x0001,
        Word = 0x0002,
        DWord = 0x0003,
        LWord = 0x0004,
        Continue = 0x0014
    }
    public enum MemoryType
    {
        /// <summary>Internal Contact Area</summary>
        InternalContact = 0,
        /// <summary>Keep contact Area</summary>
        KeepContact = 1,
        /// <summary>System Flag Area</summary>
        SystemFlag = 2,
        /// <summary>Analog Data Register</summary>
        AnalogRegister = 3,
        /// <summary>High speed link/P2P Status Contact</summary>
        HighLink = 4,
        /// <summary>P2P Service Address Area</summary>
        P2PAddress = 5,
        /// <summary>Exclusive flash memory Area</summary>
        FlashMemory = 6

    }

    public static class Data_TypeClass
    {
        public const string Bit = "X";
        public const string Byte = "B";
        public const string Word = "W";
        public const string DWord = "D";
        public const string LWord = "L";
    }

    public static class Memory_TypeClass
    {

        /// <summary>Internal Contact Area</summary>
        public const string InternalContact = "M";
        /// <summary>Keep contact Area</summary>
        public const string KeepContact = "K";
        /// <summary>System Flag Area</summary>
        public const string SystemFlag = "F";
        /// <summary>Analog Data Register</summary>
        public const string AnalogRegister = "U";
        /// <summary>High speed link/P2P Status Contact</summary>
        public const string HighLink = "L";
        /// <summary>P2P Service Address Area</summary>
        public const string P2PAddress = "N";
        /// <summary>Exclusive flash memory Area</summary>
        public const string FlashMemory = "R";
    }

    class PLCCommLSPLC
    {
        //PLCLSTCP3 tcpComm;
        //PLCLSTCP tcpComm;
        //PLCLSTCP2 tcpComm;
        PLC_COMM_TYPE plcCommType = 0;
        PLCLSTCP4 tcpComm;

        //public PLCCommLSPLC()
        //{
        //    tcpComm = new
        //}

        public PLCCommLSPLC(PLCDataArrivedCallbackHandler callback)
        {
            tcpComm = new PLCLSTCP4(callback);//, statusCallback);
            //tcpComm = new PLCLSTCP3(callback, statusCallback);
            //tcpComm = new PLCLSTCP2(callback);
        }

        public PLCCommLSPLC(PLCDataArrivedCallbackHandler callback, PLCConnectionStatusChangedEventHandler statusCallback)
        //public PLCCommLSPLC(PLCDataArrivedCallbackHandler callback)
        {
            tcpComm = new PLCLSTCP4(callback, statusCallback);
            //tcpComm = new PLCLSTCP3(callback, statusCallback);
            //tcpComm = new PLCLSTCP2(callback);
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync(PLC_COMM_TYPE commType)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            string port = "";
            int baudrate = 0;
            int databit = 0;
            try
            {
                Util.GetPrivateProfileValue("PLCCOMM", "PORT", "COM3", ref port, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "BAUDRATE", "38400", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out baudrate);
                Util.GetPrivateProfileValue("PLCCOMM", "DATABIT", "8", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out databit);
                plcCommType = commType;

                if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                {
                    retval.execResult = await tcpComm.OpenPLCAsync(5);
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync(string IP, int wport, PLC_COMM_TYPE commType)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            string port = "";
            int baudrate = 0;
            int databit = 0;
            try
            {
                Util.GetPrivateProfileValue("PLCCOMM", "PORT", "COM3", ref port, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "BAUDRATE", "38400", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out baudrate);
                Util.GetPrivateProfileValue("PLCCOMM", "DATABIT", "8", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out databit);
                plcCommType = commType;

                if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                {
                    retval.execResult = await tcpComm.OpenPLCAsync(IP, wport, 5);
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public ITNTResponseArgs OpenPLC(PLC_COMM_TYPE commType)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            string port = "";
            int baudrate = 0;
            int databit = 0;
            try
            {
                Util.GetPrivateProfileValue("PLCCOMM", "PORT", "COM3", ref port, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "BAUDRATE", "38400", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out baudrate);
                Util.GetPrivateProfileValue("PLCCOMM", "DATABIT", "8", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out databit);
                plcCommType = commType;

                if (commType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                {
                    retval.execResult = tcpComm.OpenPLC(5).Result;
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public int ClosePLC()
        {
            int retval = 0;
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                tcpComm.ClosePLC(1);
            }
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
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCAsync(sendArg);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCAsync(string strAdd)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCAsync(strAdd);
            return retval;
        }

        public ITNTResponseArgs ReadPLC(string strAdd)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = tcpComm.ReadPLC(strAdd);
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
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.WritePLCAsync(sendArg);
            return retval;
        }

        public async Task<ITNTResponseArgs> WritePLCAsync(string sAddress, string sWriteData)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.WritePLCAsync(sAddress, sWriteData);
            return retval;
        }

        public ITNTResponseArgs WritePLC(string sAddress, string sWriteData)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = tcpComm.WritePLC(sAddress, sWriteData);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMatchingResult(byte result)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMatchingResult(result);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendFrameType2PLC(frameType);
            return retval;
        }

        //public async Task<ITNTResponseArgs> SendMarkFinish()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
        //        retval = await tcpComm.SendMarkFinish(0);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendPCError2PLC(string plcvalue, int loglevel, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendPCError2PLC(plcvalue, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadSignalFromPLCAsync(loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCCarType();
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCSequence();
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCChinaFlag()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadPLCChinaFlag();
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadBodyNum(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadBodyNum(token);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadUseLaserNum(CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadUseLaserNum(token);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendSignal(byte signal)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendSignal(signal);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status, byte order=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMarkingStatus(status, order);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result, byte order=0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendVisionResult(result, order);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendAirAsync(byte air)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendAirAsync(air);
            else
                return retval;
            return retval;
        }


        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendErrorInfo(error, address);
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendErrorInfo(error);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendMovingRobot(distance);
            return retval;
        }


        public async Task<ITNTResponseArgs> SendScanComplete(byte scanstatus, int loglevel, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SendScanComplete(scanstatus, loglevel, token);
            return retval;
        }

        public async Task<ITNTResponseArgs> SetCommSettingTCP(string IP, int Port)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SetCommSettingTCP(IP, Port);
            else
                return retval;
            return retval;
        }

        public async Task<ITNTResponseArgs> SetLaserPowerError(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SetLaserPowerError(status, loglevel, token);
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

        public async Task<ITNTResponseArgs> SetCommSettingCOM(string PortNum, int BaudRate)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                return retval;
            return retval;
        }

        public async Task<ITNTResponseArgs> CheckConnection()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = tcpComm.CheckConnection();
            return retval;
        }

        public async Task<ITNTResponseArgs> SetLinkAsync(byte link)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.SetLinkAsync(link);
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadLinkStatusAsync()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            if (plcCommType == PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
                retval = await tcpComm.ReadLinkStatusAsync();
            return retval;
        }


        //public PLCCommLSPLC(string IPAddr, int port, int timeout=3000)
        //{

        //}
    }

    public class LSPLCData
    {
        /// <summary>
        /// 송신프레임
        /// </summary>
        public byte[] TX { get; set; }
        public string TXstring
        {
            get { return TX == null ? "" : BitConverter.ToString(TX); }
        }
        /// <summary>
        /// 수신프레임
        /// </summary>
        public byte[] RX { get; set; }
        public string RXstring
        {
            get { return RX == null ? "" : BitConverter.ToString(RX); }
        }

        public List<AddressData> DataList { get; set; }
        /// <summary>
        /// ACK응답 프레임 여부(데이터 정상 수신) : true;  데이터 비정상 수신 : false;
        /// </summary>
        public string ResponseStatus { get; set; }

        /// <summary>
        /// NAK 프레임 수신시 에러 메시지.
        /// </summary>
        public string NAK_ErrorCotent { get; set; }

        /// <summary>
        /// ACK 응답의 데이터 블럭 수.
        /// </summary>
        public int BlockCount { get; set; }
        /// <summary>
        /// 프레임 에러와 관계없음. 문법오류 및 런타임 Exception 일 경우 Error, 정상 응답인 경우 :OK
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 응답 메세지 타입
        /// </summary>
        public CommandType ResponseType { get; set; }

        public DataType DataType { get; set; }

        /// <summary>
        /// 받아온 응답 값으로 데이터 분서 메서드.
        /// Read,Write 함수의 finally 구문에서 호출 하도록 작성하시오.
        /// </summary>
        //public void MakeData()
        //{
        //    try
        //    {
        //        if (RX == null) return;

        //        NAK_ErrorCotent = string.Empty;

        //        List<AddressData> lstData = new List<AddressData>();

        //        //RX 응답 중 19번째가지는 헤더프레임 정보, 20번째부터 데이터 프레임.
        //        //받은 응답이 없으면, 즉 에러가 발생시 

        //        if (RX?.Length == 0)
        //        {
        //            NAK_ErrorCotent = "서버로 부터 응답을 받지 못했습니다.";
        //            return;
        //        }
        //        if (RX?[20] == (short)CommandType.ReadResponse)
        //        {
        //            ResponseType = CommandType.ReadResponse;
        //        }
        //        if (RX?[20] == (short)CommandType.WriteResponse)
        //        {
        //            ResponseType = CommandType.WriteResponse;
        //        }

        //        byte[] vdataType = new byte[2];
        //        vdataType[0] = RX[22];
        //        vdataType[1] = RX[23];


        //        foreach (DataType item in Enum.GetValues(typeof(DataType)))
        //        {
        //            string vb = BitConverter.ToString(BitConverter.GetBytes((short)item));
        //            string va = BitConverter.ToString(vdataType);
        //            if (vb.Equals(va))
        //            {
        //                DataType = item;
        //                break;
        //            }
        //        }


        //        if (RX?[26] != 0x00 || RX?[27] != 0x00)
        //        {
        //            //에러응답
        //            ResponseStatus = "NAK";
        //            DataList = lstData;
        //            //에러메세지 확인
        //            switch (RX?[28])
        //            {
        //                case 0x12:
        //                    NAK_ErrorCotent = "(0x12)연속읽기인데 바이트 타입이 아닌 경우";
        //                    break;
        //                case 0x11:
        //                    NAK_ErrorCotent = "(0x11)변수명이 4보다 작거나 16보다 큰 경우와 같이 어드레스에 관련된 에러";
        //                    break;
        //                case 0x10:
        //                    NAK_ErrorCotent = "(0x10)없는 디바이스를 요청하는 경우와 같이 디바이스에 관련된 에러";
        //                    break;
        //                case 0x78:
        //                    NAK_ErrorCotent = "(0x78)unknown command";
        //                    break;
        //                case 0x77:
        //                    NAK_ErrorCotent = "(0x77)체크섬 오류";
        //                    break;
        //                case 0x76:
        //                    NAK_ErrorCotent = "(0x76)length 정보 오류";
        //                    break;
        //                case 0x75:
        //                    NAK_ErrorCotent = "(0x75) “LGIS-GLOFA”가 아니거나 “LSIS-XGT”가 아닌 경우";
        //                    break;
        //                case 0x24:
        //                    NAK_ErrorCotent = "(0x24)데이터 타입 에러";
        //                    break;
        //                default:
        //                    NAK_ErrorCotent = "알려지지 않은 에러코드, LS산전 고객센터에 문의 / " + Convert.ToString(RX[28]);
        //                    break;

        //            }
        //        }
        //        else
        //        {
        //            //28번 index 부터 데이터로 정의
        //            int index = 28;

        //            //정상응답
        //            ResponseStatus = "ACK";
        //            byte[] blockCount = new byte[2];  //블럭카운터
        //            byte[] dataByteCount = new byte[2];  //데이터 크기
        //            int unitdatatype = BitConverter.ToInt16(vdataType, 0);
        //            unitdatatype = (unitdatatype == 0x0014) ? 0x0001 : unitdatatype;    //continuous read

        //            byte[] data = new byte[unitdatatype];  //블럭카운터

        //            Array.Copy(RX, index, blockCount, 0, 2);
        //            BlockCount = BitConverter.ToInt16(blockCount, 0);

        //            index = index + 2;

        //            //블럭카운터 만큼의 데이터 갯수가 존재한다.

        //            //Read일 경우 데이터 생성
        //            if (ResponseType == CommandType.ReadResponse)
        //            {
        //                for (int i = 0; i < BlockCount; i++)
        //                {
        //                    Array.Copy(RX, index, dataByteCount, 0, 2);
        //                    int biteSize = BitConverter.ToInt16(dataByteCount, 0); //데이터 크기.

        //                    index = index + 2;
        //                    int continueloop = biteSize / unitdatatype;

        //                    for (int j = 0; j < continueloop; j++)
        //                    {
        //                        Array.Copy(RX, index, data, 0, unitdatatype);

        //                        index = index + unitdatatype;  //다음 인덱스 

        //                        string dataContent = BitConverter.ToString(data).Replace("-", String.Empty);

        //                        AddressData dataValue = new AddressData();
        //                        dataValue.Data = dataContent;
        //                        dataValue.DataByteArray = data;

        //                        lstData.Add(dataValue);
        //                    }
        //                }
        //            }
        //            DataList = lstData;

        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        Message = "Error: " + ex.Message.ToString() + "AAA";
        //    }


        //}
    }

    public class AddressData
    {
        public string Address { get; set; }
        public string Data { get; set; }
        public byte[] DataByteArray { get; set; }
        /// <summary>
        /// 주소 문자열 표현, EX) %DW1100
        /// </summary>
        public string AddressString { get; set; }
        /// <summary>
        /// AddressString 을 바이트 배열로 변환
        /// </summary>
        public byte[] AddressByteArray
        {
            get
            {
                byte[] value = Encoding.ASCII.GetBytes(AddressString);
                return value;
            }
        }
        /// <summary>
        /// AddressByteArray 바이트 배열의 수(2byte)
        /// </summary>
        public byte[] LengthByteArray
        {
            get
            {
                byte[] value = BitConverter.GetBytes((short)AddressByteArray.Length);
                return value;
            }

        }
    }

}
