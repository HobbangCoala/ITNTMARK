using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using S7.Net;
using S7.Net.Types;
using System.Drawing;
using System.Windows.Controls.Primitives;


#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK.PLC
{

    //struct _PLC_MEMORY_
    //{
    //    int address;
    //    short size;
    //    short type;
    //};

    internal class PLCSIEMENSTCP
    {
        PLCDataArrivedCallbackHandler callbackHandler;
        PLCConnectionStatusChangedEventHandler ConnectCallback;

        //---------------------------------------------------------------------------------------------------'
        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PLC --> PC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------
        public const string SIGNAL_CLEAR = "0";

        //---------------------------------------------------------------------------------------------------
        // System에서 사용될 PLC --> PC간 데이터 전송에 사용되는 D 영역 설정
        //---------------------------------------------------------------------------------------------------

        //READ STATUS
        public string PLC_ADDRESS_SIGNAL            = "0";
        public string PLC_ADDRESS_CARTYPE           = "1";
        public string PLC_ADDRESS_LINKSTATUS        = "2";
        public string PLC_ADDRESS_AUTOMANUAL        = "3";
        public string PLC_ADDRESS_FRAMETYPE         = "4";
        public string PLC_ADDRESS_PCERROR           = "5";
        public string PLC_ADDRESS_SEQUENCE          = "6";
        public string PLC_ADDRESS_BODYNUM           = "6";
        public string PLC_ADDRESS_CHINA             = "14";                 //0 = Normal, 1 = China
        public string PLC_ADDRESS_RESERV02          = "15";
        public string PLC_ADDRESS_VIN = "4";

        //WRITE STATUS
        public string PLC_ADDRESS_MATCHRESULT       = "16";
        public string PLC_ADDRESS_MARKSTATUS        = "17";
        public string PLC_ADDRESS_VISIONRESULT      = "18";
        public string PLC_ADDRESS_MARKSTATUS_2      = "19";
        public string PLC_ADDRESS_VISIONRESULT_2    = "20";
        public string PLC_ADDRESS_SETLINK           = "21";
        public string PLC_ADDRESS_SETAIR            = "22";
        public string PLC_ADDRESS_SCANCOMPLETE      = "23";
        public string PLC_ADDRESS_MESCOUNTERROR     = "24";
        public string PLC_ADDRESS_REQMOVEROBOT      = "25";
        public string PLC_ADDRESS_SETERRORCODE      = "26";


        public const int SIGNAL_PLC2PC_NORMAL           = 0;
        public const int SIGNAL_PLC2PC_NEXTVIN          = 1;                // 각인준비 OK _ 넥스트 빈
        public const int SIGNAL_PLC2PC_MARK_1           = 2;                // 각인 시작
        public const int SIGNAL_PLC2PC_VISION_1         = 4;                // 각인 완료
        public const int SIGNAL_PLC2PC_NOFRAME          = 8;                // 비상 정지
        public const int SIGNAL_PLC2PC_EMERGENCY_STOP   = 16;               // 비상 정지
        public const int SIGNAL_PLC2PC_MARK_2           = 32;               // 각인 시작
        public const int SIGNAL_PLC2PC_VISION_2         = 64;               // 각인 완료

        //READ STATUS
        public int PLC_LENGTH_SIGNAL = 0;
        public int PLC_LENGTH_CARTYPE = 1;
        public int PLC_LENGTH_LINKSTATUS = 2;
        public int PLC_LENGTH_AUTOMANUAL = 3;
        public int PLC_LENGTH_FRAMETYPE = 4;
        public int PLC_LENGTH_PCERROR = 5;
        public int PLC_LENGTH_SEQUENCE = 6;
        public int PLC_LENGTH_BODYNUM = 6;
        public int PLC_LENGTH_CHINA = 14;
        public int PLC_LENGTH_RESERV02 = 15;
        public int PLC_LENGTH_VIN = 19;

        //WRITE STATUS
        public int PLC_LENGTH_MATCHRESULT = 16;
        public int PLC_LENGTH_MARKSTATUS = 17;
        public int PLC_LENGTH_VISIONRESULT = 18;
        public int PLC_LENGTH_MARKSTATUS_2 = 19;
        public int PLC_LENGTH_VISIONRESULT_2 = 20;
        public int PLC_LENGTH_SETLINK = 21;
        public int PLC_LENGTH_SETAIR = 22;
        public int PLC_LENGTH_SCANCOMPLETE = 23;
        public int PLC_LENGTH_MESCOUNTERROR = 24;
        public int PLC_LENGTH_REQMOVEROBOT = 25;
        public int PLC_LENGTH_SETERRORCODE = 26;

        //READ STATUS
        public int PLC_DBNUM_SIGNAL = 1;
        public int PLC_DBNUM_CARTYPE = 1;
        public int PLC_DBNUM_LINKSTATUS = 1;
        public int PLC_DBNUM_AUTOMANUAL = 1;
        public int PLC_DBNUM_FRAMETYPE = 1;
        public int PLC_DBNUM_PCERROR = 1;
        public int PLC_DBNUM_SEQUENCE = 1;
        public int PLC_DBNUM_BODYNUM = 1;
        public int PLC_DBNUM_CHINA = 1;
        public int PLC_DBNUM_RESERV02 = 1;
        public int PLC_DBNUM_VIN = 100;

        //WRITE STATUS
        public int PLC_DBNUM_MATCHRESULT = 1;
        public int PLC_DBNUM_MARKSTATUS = 1;
        public int PLC_DBNUM_VISIONRESULT = 1;
        public int PLC_DBNUM_MARKSTATUS_2 = 1;
        public int PLC_DBNUM_VISIONRESULT_2 = 1;
        public int PLC_DBNUM_SETLINK = 1;
        public int PLC_DBNUM_SETAIR = 1;
        public int PLC_DBNUM_SCANCOMPLETE = 1;
        public int PLC_DBNUM_MESCOUNTERROR = 1;
        public int PLC_DBNUM_REQMOVEROBOT = 1;
        public int PLC_DBNUM_SETERRORCODE = 1;
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

        bool isConnected = false;
        //private Socket workSocket;
        //RingBuffer rb = new RingBuffer(1204);
        //private const string _companyID = "LSIS-XGT";
        private readonly object bufferLock = new object();
        private readonly object cmdLock = new object();

        bool DoingPLCStatusThread = false;
        Thread _plcStatusThread = null;

        bool DoingPLCConnectionThread = false;
        Thread _plcConnectionThread = null;

        public const int PLC_MODE_WRITE = 1;
        public const int PLC_MODE_READ = 0;


        byte[] RecvFrameData = new byte[2048];
        int RecvFrameLength = 0;
        byte commError = 0;
        //bool doingCommand = false;

        const int RETURN_SIZE = 8;
        const int DATA_SIZE = 1;
        const int STRING_SIZE = 2;

        protected RingBuffer recvBuffer;
        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        bool doingCmdFlag = false;

        Plc siemensPLC = null;

        byte[] HeaderData = new byte[6] { (byte)'0', (byte)'0', (byte)'F', (byte)'F', (byte)'0', (byte)'0' };
        string sHeaderData = "00FF00";
        string sHeaderData4Digits = "00FF";
        csConnStatus connStatus = csConnStatus.Closed;
        //byte[] HeaderData = new byte[4] { (byte)'0', (byte)'0', (byte)'F', (byte)'F' };

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public PLCSIEMENSTCP(PLCDataArrivedCallbackHandler callback, PLCConnectionStatusChangedEventHandler statusCallback)
        {
            callbackHandler = callback;
            ConnectCallback = statusCallback;
            recvBuffer = new RingBuffer(4096);
            LoadOption();
        }

        private void LoadOption()
        {
            string value = "";
            string datasize = "1";

            try
            {
                datasize = DATA_SIZE.ToString();

                //READ
                Util.GetPrivateProfileValue("ADDRESS", "SIGNAL", "0", ref PLC_ADDRESS_SIGNAL, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "CARTYPE", "1", ref PLC_ADDRESS_CARTYPE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "LINKSTATUS", "2", ref PLC_ADDRESS_LINKSTATUS, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "AUTOMANUAL", "3", ref PLC_ADDRESS_AUTOMANUAL, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "FRAMETYPE", "4", ref PLC_ADDRESS_FRAMETYPE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "PCERROR", "5", ref PLC_ADDRESS_PCERROR, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SEQUENCE", "6", ref PLC_ADDRESS_SEQUENCE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "BODYNUM", "6", ref PLC_ADDRESS_BODYNUM, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "CHINA", "14", ref PLC_ADDRESS_CHINA, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "RESERVED02", "15", ref PLC_ADDRESS_RESERV02, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "VIN", "4", ref PLC_ADDRESS_VIN, Constants.PLCVAL_INI_FILE);

                //WRITE
                Util.GetPrivateProfileValue("ADDRESS", "MATCHRESULT", "16", ref PLC_ADDRESS_MATCHRESULT, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "MARKSTATUS", "17", ref PLC_ADDRESS_MARKSTATUS, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "VISIONRESULT", "18", ref PLC_ADDRESS_VISIONRESULT, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "MARKSTATUS2", "19", ref PLC_ADDRESS_MARKSTATUS_2, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "VISIONRESULT2", "20", ref PLC_ADDRESS_VISIONRESULT_2, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SETLINK", "21", ref PLC_ADDRESS_SETLINK, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SETAIR", "22", ref PLC_ADDRESS_SETAIR, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SCANCOMPLETE", "23", ref PLC_ADDRESS_SCANCOMPLETE, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "MESCOUNTERROR", "24", ref PLC_ADDRESS_MESCOUNTERROR, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "REQMOVEROBOT", "25", ref PLC_ADDRESS_REQMOVEROBOT, Constants.PLCVAL_INI_FILE);
                Util.GetPrivateProfileValue("ADDRESS", "SETERRORCODE", "26", ref PLC_ADDRESS_SETERRORCODE, Constants.PLCVAL_INI_FILE);

                //READ
                Util.GetPrivateProfileValue("LENGTH", "SIGNAL", datasize, ref value, Constants.PLCVAL_INI_FILE);         int.TryParse(value, out PLC_LENGTH_SIGNAL);
                Util.GetPrivateProfileValue("LENGTH", "CARTYPE", datasize, ref value, Constants.PLCVAL_INI_FILE);        int.TryParse(value, out PLC_LENGTH_CARTYPE);
                Util.GetPrivateProfileValue("LENGTH", "LINKSTATUS", datasize, ref value, Constants.PLCVAL_INI_FILE);     int.TryParse(value, out PLC_LENGTH_LINKSTATUS);
                Util.GetPrivateProfileValue("LENGTH", "AUTOMANUAL", datasize, ref value, Constants.PLCVAL_INI_FILE);     int.TryParse(value, out PLC_LENGTH_AUTOMANUAL);
                Util.GetPrivateProfileValue("LENGTH", "FRAMETYPE", datasize, ref value, Constants.PLCVAL_INI_FILE);      int.TryParse(value, out PLC_LENGTH_FRAMETYPE);
                Util.GetPrivateProfileValue("LENGTH", "PCERROR", datasize, ref value, Constants.PLCVAL_INI_FILE);        int.TryParse(value, out PLC_LENGTH_PCERROR);
                Util.GetPrivateProfileValue("LENGTH", "SEQUENCE", datasize, ref value, Constants.PLCVAL_INI_FILE);       int.TryParse(value, out PLC_LENGTH_SEQUENCE);
                Util.GetPrivateProfileValue("LENGTH", "BODYNUM", datasize, ref value, Constants.PLCVAL_INI_FILE);        int.TryParse(value, out PLC_LENGTH_BODYNUM);
                Util.GetPrivateProfileValue("LENGTH", "CHINA", datasize, ref value, Constants.PLCVAL_INI_FILE);          int.TryParse(value, out PLC_LENGTH_CHINA);
                Util.GetPrivateProfileValue("LENGTH", "RESERVED02", datasize, ref value, Constants.PLCVAL_INI_FILE);     int.TryParse(value, out PLC_LENGTH_RESERV02);
                Util.GetPrivateProfileValue("LENGTH", "VIN", datasize, ref value, Constants.PLCVAL_INI_FILE);            int.TryParse(value, out PLC_LENGTH_VIN);

                //WRITE
                Util.GetPrivateProfileValue("LENGTH", "MATCHRESULT", datasize, ref value, Constants.PLCVAL_INI_FILE);    int.TryParse(value, out PLC_LENGTH_MATCHRESULT);
                Util.GetPrivateProfileValue("LENGTH", "MARKSTATUS", datasize, ref value, Constants.PLCVAL_INI_FILE);     int.TryParse(value, out PLC_LENGTH_MARKSTATUS);
                Util.GetPrivateProfileValue("LENGTH", "VISIONRESULT", datasize, ref value, Constants.PLCVAL_INI_FILE);   int.TryParse(value, out PLC_LENGTH_VISIONRESULT);
                Util.GetPrivateProfileValue("LENGTH", "MARKSTATUS2", datasize, ref value, Constants.PLCVAL_INI_FILE);    int.TryParse(value, out PLC_LENGTH_MARKSTATUS_2);
                Util.GetPrivateProfileValue("LENGTH", "VISIONRESULT2", datasize, ref value, Constants.PLCVAL_INI_FILE);  int.TryParse(value, out PLC_LENGTH_VISIONRESULT_2);
                Util.GetPrivateProfileValue("LENGTH", "SETLINK", datasize, ref value, Constants.PLCVAL_INI_FILE);        int.TryParse(value, out PLC_LENGTH_SETLINK);
                Util.GetPrivateProfileValue("LENGTH", "SETAIR", datasize, ref value, Constants.PLCVAL_INI_FILE);         int.TryParse(value, out PLC_LENGTH_SETAIR);
                Util.GetPrivateProfileValue("LENGTH", "SCANCOMPLETE", datasize, ref value, Constants.PLCVAL_INI_FILE);   int.TryParse(value, out PLC_LENGTH_SCANCOMPLETE);
                Util.GetPrivateProfileValue("LENGTH", "MESCOUNTERROR", datasize, ref value, Constants.PLCVAL_INI_FILE);  int.TryParse(value, out PLC_LENGTH_MESCOUNTERROR);
                Util.GetPrivateProfileValue("LENGTH", "REQMOVEROBOT", datasize, ref value, Constants.PLCVAL_INI_FILE);   int.TryParse(value, out PLC_LENGTH_REQMOVEROBOT);
                Util.GetPrivateProfileValue("LENGTH", "SETERRORCODE", datasize, ref value, Constants.PLCVAL_INI_FILE);   int.TryParse(value, out PLC_LENGTH_SETERRORCODE);

                //READ
                Util.GetPrivateProfileValue("DBNUM", "SIGNAL", "1", ref value, Constants.PLCVAL_INI_FILE);          int.TryParse(value, out PLC_DBNUM_SIGNAL);
                Util.GetPrivateProfileValue("DBNUM", "CARTYPE", "1", ref value, Constants.PLCVAL_INI_FILE);         int.TryParse(value, out PLC_DBNUM_CARTYPE);
                Util.GetPrivateProfileValue("DBNUM", "LINKSTATUS", "1", ref value, Constants.PLCVAL_INI_FILE);      int.TryParse(value, out PLC_DBNUM_LINKSTATUS);
                Util.GetPrivateProfileValue("DBNUM", "AUTOMANUAL", "1", ref value, Constants.PLCVAL_INI_FILE);      int.TryParse(value, out PLC_DBNUM_AUTOMANUAL);
                Util.GetPrivateProfileValue("DBNUM", "FRAMETYPE", "1", ref value, Constants.PLCVAL_INI_FILE);       int.TryParse(value, out PLC_DBNUM_FRAMETYPE);
                Util.GetPrivateProfileValue("DBNUM", "PCERROR", "1", ref value, Constants.PLCVAL_INI_FILE);         int.TryParse(value, out PLC_DBNUM_PCERROR);
                Util.GetPrivateProfileValue("DBNUM", "SEQUENCE", "1", ref value, Constants.PLCVAL_INI_FILE);        int.TryParse(value, out PLC_DBNUM_SEQUENCE);
                Util.GetPrivateProfileValue("DBNUM", "BODYNUM", "1", ref value, Constants.PLCVAL_INI_FILE);         int.TryParse(value, out PLC_DBNUM_BODYNUM);
                Util.GetPrivateProfileValue("DBNUM", "CHINA", "1", ref value, Constants.PLCVAL_INI_FILE);           int.TryParse(value, out PLC_DBNUM_CHINA);
                Util.GetPrivateProfileValue("DBNUM", "RESERVED02", "1", ref value, Constants.PLCVAL_INI_FILE);      int.TryParse(value, out PLC_DBNUM_RESERV02);
                Util.GetPrivateProfileValue("DBNUM", "VIN", "1", ref value, Constants.PLCVAL_INI_FILE);             int.TryParse(value, out PLC_DBNUM_VIN);

                //WRITE
                Util.GetPrivateProfileValue("DBNUM", "MATCHRESULT", "1", ref value, Constants.PLCVAL_INI_FILE);     int.TryParse(value, out PLC_DBNUM_MATCHRESULT);
                Util.GetPrivateProfileValue("DBNUM", "MARKSTATUS", "1", ref value, Constants.PLCVAL_INI_FILE);      int.TryParse(value, out PLC_DBNUM_MARKSTATUS);
                Util.GetPrivateProfileValue("DBNUM", "VISIONRESULT", "1", ref value, Constants.PLCVAL_INI_FILE);    int.TryParse(value, out PLC_DBNUM_VISIONRESULT);
                Util.GetPrivateProfileValue("DBNUM", "MARKSTATUS2", "1", ref value, Constants.PLCVAL_INI_FILE);     int.TryParse(value, out PLC_DBNUM_MARKSTATUS_2);
                Util.GetPrivateProfileValue("DBNUM", "VISIONRESULT2", "1", ref value, Constants.PLCVAL_INI_FILE);   int.TryParse(value, out PLC_DBNUM_VISIONRESULT_2);
                Util.GetPrivateProfileValue("DBNUM", "SETLINK", "1", ref value, Constants.PLCVAL_INI_FILE);         int.TryParse(value, out PLC_DBNUM_SETLINK);
                Util.GetPrivateProfileValue("DBNUM", "SETAIR", "1", ref value, Constants.PLCVAL_INI_FILE);          int.TryParse(value, out PLC_DBNUM_SETAIR);
                Util.GetPrivateProfileValue("DBNUM", "SCANCOMPLETE", "1", ref value, Constants.PLCVAL_INI_FILE);    int.TryParse(value, out PLC_DBNUM_SCANCOMPLETE);
                Util.GetPrivateProfileValue("DBNUM", "MESCOUNTERROR", "1", ref value, Constants.PLCVAL_INI_FILE);   int.TryParse(value, out PLC_DBNUM_MESCOUNTERROR);
                Util.GetPrivateProfileValue("DBNUM", "REQMOVEROBOT", "1", ref value, Constants.PLCVAL_INI_FILE);    int.TryParse(value, out PLC_DBNUM_REQMOVEROBOT);
                Util.GetPrivateProfileValue("DBNUM", "SETERRORCODE", "1", ref value, Constants.PLCVAL_INI_FILE);    int.TryParse(value, out PLC_DBNUM_SETERRORCODE);
            }
            catch (Exception ex)
            {

            }
        }

        public async Task<ITNTResponseArgs> OpenPLCAsync(short timeout, CancellationToken token=default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "OpenPLCAsync";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sIPAddr = "", sPort = "", sSlot = "", sRack = "";
            int iPort = 0;
            short wSlot = 0, wRack = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //StateObject state = new StateObject();
            Stopwatch sw = new Stopwatch();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

            try
            {
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

#if TEST_DEBUG_PLC
                await Task.Delay(500);

                DoingPLCStatusThread = true;
                _plcStatusThread = new Thread(PLCStatusThread);
                _plcStatusThread.Start();

                isConnected = true;

                retval.execResult = 0;
#else
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref sIPAddr, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref sPort, Constants.PARAMS_INI_FILE);

                Util.GetPrivateProfileValue("PLCCOMM", "RACKNO", "0", ref sRack, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SLOTNO", "2", ref sSlot, Constants.PARAMS_INI_FILE);

                int.TryParse(sPort, out iPort);
                short.TryParse(sRack, out wRack);
                short.TryParse(sSlot, out wSlot);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + sIPAddr + ", PORT = " + sPort, Thread.CurrentThread.ManagedThreadId);

                if (siemensPLC == null)
                {
                    //siemensPLC = new Plc(CpuType.S7200, sIPAddr, iPort, wRack, wSlot);
                    siemensPLC = new Plc(CpuType.S7300, sIPAddr, wRack, wSlot);
                }

                if (siemensPLC.IsConnected == true)
                {
                    statusArg.newstatus = csConnStatus.Connected;
                    if(statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }

                    retval.execResult = 0;
                    return retval;
                }

                try
                {
                    await siemensPLC.OpenAsync(token);
                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                    {
                        if (siemensPLC.IsConnected == true)
                            break;

                        await Task.Delay(50);
                    }
                    sw.Stop();

                    if (siemensPLC.IsConnected == true)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTED", Thread.CurrentThread.ManagedThreadId);

                        statusArg.newstatus = csConnStatus.Connected;
                        if (statusArg.newstatus != connStatus)
                        {
                            statusArg.oldstatus = connStatus;
                            connStatus = statusArg.newstatus;
                            ConnectCallback?.Invoke(statusArg);
                        }

                        isConnected = true;
                        DoingPLCStatusThread = true;
                        _plcStatusThread = new Thread(PLCStatusThread);
                        _plcStatusThread.Start();

                        return retval;
                    }
                    else
                    {
                        isConnected = false;
                        //if(DoingPLCConnectionThread == false)
                        //{
                        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                        DoingPLCConnectionThread = true;
                        _plcConnectionThread = new Thread(PLCConnectionThread);
                        _plcConnectionThread.Start();
                        //}
                        statusArg.newstatus = csConnStatus.Disconnected;
                        if (statusArg.newstatus != connStatus)
                        {
                            statusArg.oldstatus = connStatus;
                            connStatus = statusArg.newstatus;
                            ConnectCallback?.Invoke(statusArg);
                        }
                        retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                        return retval;
                    }
                }
                catch(Exception ex)
                {
                    siemensPLC.Close();

                    isConnected = false;
                    //if (DoingPLCConnectionThread == false)
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    DoingPLCConnectionThread = true;
                    _plcConnectionThread = new Thread(PLCConnectionThread);
                    _plcConnectionThread.Start();
                    //}
                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }

                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    return retval;
                }
#endif
            }
            catch (Exception ex)
            {
                if (siemensPLC == null)
                    siemensPLC = new Plc(CpuType.S7300, sIPAddr, wRack, wSlot);
                siemensPLC.Close();

                isConnected = false;
                //if (DoingPLCConnectionThread == false)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                //    DoingPLCConnectionThread = true;
                //    _plcConnectionThread = new Thread(PLCConnectionThread);
                //    _plcConnectionThread.Start();
                //}
                statusArg.newstatus = csConnStatus.Disconnected;
                if (statusArg.newstatus != connStatus)
                {
                    statusArg.oldstatus = connStatus;
                    connStatus = statusArg.newstatus;
                    ConnectCallback?.Invoke(statusArg);
                }

                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> OpenPLCAsync(string IP, int port, string rackno, string slotno, short timeout, CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "OpenPLCAsync";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //string sIPAddr = "", sPort = "", sSlot = "", sRack = "";
            //int iPort = 0;
            short wSlot = 0, wRack = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //StateObject state = new StateObject();
            Stopwatch sw = new Stopwatch();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

            try
            {
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

#if TEST_DEBUG_PLC
                await Task.Delay(500);

                DoingPLCStatusThread = true;
                _plcStatusThread = new Thread(PLCStatusThread);
                _plcStatusThread.Start();

                isConnected = true;

                retval.execResult = 0;
#else
                //Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref sIPAddr, Constants.PARAMS_INI_FILE);
                //Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref sPort, Constants.PARAMS_INI_FILE);

                //Util.GetPrivateProfileValue("PLCCOMM", "RACKNO", "192.168.1.2", ref sRack, Constants.PARAMS_INI_FILE);
                //Util.GetPrivateProfileValue("PLCCOMM", "SLOTNO", "2", ref sSlot, Constants.PARAMS_INI_FILE);

                //int.TryParse(sPort, out iPort);
                short.TryParse(rackno, out wRack);
                short.TryParse(slotno, out wSlot);

                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + sIPAddr + ", PORT = " + sPort, Thread.CurrentThread.ManagedThreadId);

                if (siemensPLC == null)
                {
                    //siemensPLC = new Plc(CpuType.S7200, sIPAddr, iPort, wRack, wSlot);
                    siemensPLC = new Plc(CpuType.S7300, IP, wRack, wSlot);
                }

                if (siemensPLC.IsConnected == true)
                {
                    statusArg.newstatus = csConnStatus.Connected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }
                    return retval;
                }

                await siemensPLC.OpenAsync(token);
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (siemensPLC.IsConnected == true)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();

                if (siemensPLC.IsConnected == true)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    statusArg.newstatus = csConnStatus.Connected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }

                    isConnected = true;
                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return retval;
                }
                else
                {
                    isConnected = false;
                    //if(DoingPLCConnectionThread == false)
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    DoingPLCConnectionThread = true;
                    _plcConnectionThread = new Thread(PLCConnectionThread);
                    _plcConnectionThread.Start();
                    //}
                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }


                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    return retval;
                }
#endif
            }
            catch (Exception ex)
            {
                if (siemensPLC == null)
                    siemensPLC = new Plc(CpuType.S7300, IP, wRack, wSlot);
                siemensPLC.Close();

                isConnected = false;
                //if (DoingPLCConnectionThread == false)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                //    DoingPLCConnectionThread = true;
                //    _plcConnectionThread = new Thread(PLCConnectionThread);
                //    _plcConnectionThread.Start();
                //}

                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }



        public ITNTResponseArgs OpenPLC(short timeout)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "OpenPLC";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sIPAddr = "", sPort = "", sSlot = "", sRack = "";
            int iPort = 0;
            short wSlot = 0, wRack = 0;
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

            //StateObject state = new StateObject();
            //Stopwatch sw = new Stopwatch();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

#if TEST_DEBUG_PLC
                Thread.Sleep(500);

                DoingPLCStatusThread = true;
                _plcStatusThread = new Thread(PLCStatusThread);
                _plcStatusThread.Start();
                isConnected = true;

                retval.execResult = 0;
#else
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref sIPAddr, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref sPort, Constants.PARAMS_INI_FILE);

                Util.GetPrivateProfileValue("PLCCOMM", "RACKNO", "0", ref sRack, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SLOTNO", "2", ref sSlot, Constants.PARAMS_INI_FILE);

                int.TryParse(sPort, out iPort);
                short.TryParse(sRack, out wRack);
                short.TryParse(sSlot, out wSlot);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + sIPAddr + ", PORT = " + sPort, Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RACK = " + sRack + ", SLOT = " + sSlot, Thread.CurrentThread.ManagedThreadId);

                if (siemensPLC == null)
                {
                    //siemensPLC = new Plc(CpuType.S7200, sIPAddr, iPort, wRack, wSlot);
                    siemensPLC = new Plc(CpuType.S7300, sIPAddr, wRack, wSlot);
                }

                siemensPLC.ReadTimeout = 1;
                if (siemensPLC.IsConnected == true)
                {
                    siemensPLC.Close();
                    isConnected = true;
                    //return 0;
                }

                siemensPLC.Open();
                //sw.Start();

                if (siemensPLC.IsConnected == true)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTED", Thread.CurrentThread.ManagedThreadId);

                    statusArg.newstatus = csConnStatus.Connected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }

                    isConnected = true;
                    DoingPLCStatusThread = true;
                    _plcStatusThread = new Thread(PLCStatusThread);
                    _plcStatusThread.Start();

                    return retval;
                }
                else
                {
                    isConnected = false;

                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }

                    //if (DoingPLCConnectionThread == false)
                    //{
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    DoingPLCConnectionThread = true;
                    _plcConnectionThread = new Thread(PLCConnectionThread);
                    _plcConnectionThread.Start();
                    //}
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                    return retval;
                }
#endif
            }
            catch (Exception ex)
            {
                if(siemensPLC == null)
                    siemensPLC = new Plc(CpuType.S7300, sIPAddr, wRack, wSlot);
                siemensPLC.Close();

                isConnected = false;
                //if (DoingPLCConnectionThread == false)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                //    DoingPLCConnectionThread = true;
                //    _plcConnectionThread = new Thread(PLCConnectionThread);
                //    _plcConnectionThread.Start();
                //}

                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }

        public void ClosePLC(byte threadflag)
        {
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            if (threadflag != 0)
                DoingPLCStatusThread = false;
#if TEST_DEBUG_PLC
#else
            if (isConnected == true)
            {
                //if ((GetWorkSocket() != null) && (GetWorkSocket().Connected))
                //{
                //    GetWorkSocket().Disconnect(false);
                //    GetWorkSocket().Close();
                //    SetWorkSocket(null);
                //}
                siemensPLC.Close();
            }
#endif
            isConnected = false;

            statusArg.newstatus = csConnStatus.Disconnected;
            if (statusArg.newstatus != connStatus)
            {
                statusArg.oldstatus = connStatus;
                connStatus = statusArg.newstatus;
                ConnectCallback?.Invoke(statusArg);
            }
        }


        public async Task<ITNTResponseArgs> ExecuteCommandFuncAsync(byte RWFlag, ITNTSendArgs sArg, int timeout = 2, CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ExecuteCommandMsgAsync";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                if (siemensPLC == null)
                {
                    retval.errorInfo.rawErrorCode = -1;
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "SIMENS PLC OBJECT IS NULL";
                    return retval;
                }

                if (siemensPLC.IsConnected == false)
                {
                    retval.errorInfo.rawErrorCode = -2;
                    retval.execResult = -2;
                    retval.errorInfo.sErrorMessage = "SIMENS PLC DOES NOT CONNECTED";
                    return retval;
                }

                retval = await ExecuteCommandMsgAsync(RWFlag, sArg, timeout, token);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = await ExecuteCommandMsgAsync(RWFlag, sArg, timeout, token);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = await ExecuteCommandMsgAsync(RWFlag, sArg, timeout, token);
                }

                if((retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR) && (retval.execResult != (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY))
                {
                    siemensPLC.Close();
                    isConnected = false;

                    statusArg.newstatus = csConnStatus.Disconnected;
                    if (statusArg.newstatus != connStatus)
                    {
                        statusArg.oldstatus = connStatus;
                        connStatus = statusArg.newstatus;
                        ConnectCallback?.Invoke(statusArg);
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public ITNTResponseArgs ExecuteCommandFunc(byte RWFlag, ITNTSendArgs sArg, int timeout = 2)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ExecuteCommandMsgAsync";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, timeout);
                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                {
                    retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, timeout);
                    if (retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY)
                        retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, timeout);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(byte RWFlag, ITNTSendArgs sArg, int timeout = 2, CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ExecuteCommandMsgAsync";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            Stopwatch sw = new Stopwatch();
            //Word wdata = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(sArg.loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (!doingCmdFlag)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();
                if (doingCmdFlag)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                doingCmdFlag = true;
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    if(RWFlag == PLC_MODE_WRITE)
                    {
                        byte[] senddata = new byte[sArg.dataSize];
                        Array.Copy(sArg.sendBuffer, senddata, sArg.dataSize);
                        try
                        {
                            await siemensPLC.WriteBytesAsync(S7.Net.DataType.DataBlock, sArg.plcDBNum, sArg.Address, senddata, token);
                            //await siemensPLC.WriteAsync(S7.Net.DataType.DataBlock, 1, sArg.Address, senddata);
                            break;
                        }
                        catch(Exception ex)
                        {
                            retval.execResult = ex.HResult;
                            retval.errorInfo.sErrorMessage = ex.Message;
                        }
                    }
                    else
                    {
                        try
                        {
                            byte[] recvdata = await siemensPLC.ReadBytesAsync(S7.Net.DataType.DataBlock, sArg.plcDBNum, sArg.Address, sArg.dataSize, token);
                            if (recvdata.Length >= sArg.dataSize)
                            {
                                Array.Reverse(recvdata);
                                Array.Copy(recvdata, retval.recvBuffer, sArg.dataSize);
                                retval.recvSize = sArg.dataSize;
                                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                                break;
                            }
                            else
                            {
                                Array.Reverse(recvdata);
                                if (recvdata.Length > 0)
                                {
                                    Array.Copy(recvdata, retval.recvBuffer, recvdata.Length);
                                    retval.recvSize = recvdata.Length;
                                    retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                                    break;
                                }
                                //else
                                //{
                                //    continue;
                                //}
                            }
                        }
                        catch (Exception ex)
                        {
                            retval.execResult = ex.HResult;
                            retval.errorInfo.sErrorMessage = ex.Message;
                        }
                    }
                }
                doingCmdFlag = false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCmdFlag = false;
            }

            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
            ITNTTraceLog.Instance.Trace(sArg.loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public ITNTResponseArgs ExecuteCommandMsg(byte RWFlag, ITNTSendArgs sArg, int timeout = 2)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ExecuteCommandMsgAsync";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START");

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            Stopwatch sw = new Stopwatch();

            try
            {
                ITNTTraceLog.Instance.Trace(sArg.loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (!doingCmdFlag)
                        break;

                    Thread.Sleep(50);
                }
                sw.Stop();
                if (doingCmdFlag)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY");
                    return retval;
                }

                doingCmdFlag = true;
                for (retrycount = 0; retrycount < 3; retrycount++)
                {
                    if (RWFlag == PLC_MODE_WRITE)
                    {
                        byte[] senddata = new byte[sArg.dataSize];
                        Array.Copy(sArg.sendBuffer, senddata, sArg.dataSize);
                        try
                        {
                            siemensPLC.WriteBytes(S7.Net.DataType.DataBlock, sArg.plcDBNum, sArg.Address, senddata);
                            //await simensPLC.WriteAsync(S7.Net.DataType.DataBlock, 1, sArg.Address, senddata);
                            break;
                        }
                        catch (Exception ex)
                        {
                            retval.execResult = ex.HResult;
                            retval.errorInfo.sErrorMessage = ex.Message;
                        }
                    }
                    else
                    {
                        byte[] recvdata = siemensPLC.ReadBytes(S7.Net.DataType.DataBlock, sArg.plcDBNum, sArg.Address, sArg.dataSize);
                        if (recvdata.Length > sArg.dataSize)
                        {
                            Array.Copy(recvdata, retval.recvBuffer, sArg.dataSize);
                            retval.recvSize = sArg.dataSize;
                            retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                            break;
                        }
                        else
                        {
                            if (recvdata.Length > 0)
                            {
                                Array.Copy(recvdata, retval.recvBuffer, recvdata.Length);
                                retval.recvSize = recvdata.Length;
                                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                                break;
                            }
                            //else
                            //{
                            //    continue;
                            //}
                        }
                    }
                }
                doingCmdFlag = false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCmdFlag = false;
            }

            ITNTTraceLog.Instance.Trace(sArg.loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END");
            return retval;
        }


        private async void PLCStatusThread()
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "PLCStatusThread";
            ITNTResponseArgs retval4status = new ITNTResponseArgs(128);
            ITNTResponseArgs retval4PLC = new ITNTResponseArgs(128);
            string sIPAddr = "";
            string sPort = "";
            string sRack = "";
            string sSlot = "";
            int iPort = 0;
            short wRack = 0;
            short wSlot = 0;
            Stopwatch sw = new Stopwatch();
            DeviceStatusChangedEventArgs statusArg = new DeviceStatusChangedEventArgs();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                while (DoingPLCStatusThread)
                {
                    if (!DoingPLCStatusThread)
                        break;

                    //if ((isConnected == false) || (siemensPLC.IsConnected == false))
                    if (isConnected == false)
                    {
                        Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "192.168.1.2", ref sIPAddr, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "2004", ref sPort, Constants.PARAMS_INI_FILE);

                        Util.GetPrivateProfileValue("PLCCOMM", "RACKNO", "0", ref sRack, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("PLCCOMM", "SLOTNO", "2", ref sSlot, Constants.PARAMS_INI_FILE);

                        int.TryParse(sPort, out iPort);
                        short.TryParse(sRack, out wRack);
                        short.TryParse(sSlot, out wSlot);

                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + sIPAddr + ", PORT = " + sPort, Thread.CurrentThread.ManagedThreadId);

                        if (siemensPLC == null)
                        {
                            //siemensPLC = new Plc(CpuType.S7200, sIPAddr, iPort, wRack, wSlot);
                            siemensPLC = new Plc(CpuType.S7300, sIPAddr, wRack, wSlot);
                        }

                        if (siemensPLC.IsConnected != true)
                        {
                            try
                            {
                                await siemensPLC.OpenAsync();
                                sw.Start();
                                while (sw.Elapsed < TimeSpan.FromSeconds(1))
                                {
                                    if (siemensPLC.IsConnected == true)
                                        break;

                                    await Task.Delay(50);
                                }
                                sw.Stop();
                            }
                            catch (Exception ex)
                            {
                                statusArg.newstatus = csConnStatus.Disconnected;
                                if (statusArg.newstatus != connStatus)
                                {
                                    statusArg.oldstatus = connStatus;
                                    connStatus = statusArg.newstatus;
                                    ConnectCallback?.Invoke(statusArg);
                                }
                            }

                            if (siemensPLC.IsConnected == true)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTED", Thread.CurrentThread.ManagedThreadId);
                                isConnected = true;
                                //DoingPLCStatusThread = true;
                                //_plcStatusThread = new Thread(PLCStatusThread);
                                //_plcStatusThread.Start();
                                statusArg.newstatus = csConnStatus.Connected;
                                if (statusArg.newstatus != connStatus)
                                {
                                    statusArg.oldstatus = connStatus;
                                    connStatus = statusArg.newstatus;
                                    ConnectCallback?.Invoke(statusArg);
                                }
                            }
                            else
                            {
                                isConnected = false;
                                statusArg.newstatus = csConnStatus.Disconnected;
                                if (statusArg.newstatus != connStatus)
                                {
                                    statusArg.oldstatus = connStatus;
                                    connStatus = statusArg.newstatus;
                                    ConnectCallback?.Invoke(statusArg);
                                }

                                await Task.Delay(1000);
                                continue;
                            }
                        }
                    }

                    retval4status.Initialize();
                    retval4status = await ReadSignalFromPLCAsync(2);
                    retval4status.recvType = 1;
                    OnPLCStatusDataArrivedCallbackFunc(retval4status);
                    //if (retval4status.execResult == 0)
                    //{
                    //    retval4status.recvType = 1;
                    //    OnPLCStatusDataArrivedCallbackFunc(retval4status);
                    //}
                    //else
                    //{

                    //}

                    if (!DoingPLCStatusThread)
                        break;
                    await Task.Delay(200);
                    if (!DoingPLCStatusThread)
                        break;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                _plcStatusThread = null;
            }
            catch (Exception ex)
            {
            }
        }

        private async void PLCConnectionThread()
        {
            string className = "PLCSIEMENSETCP";
            string funcName = "PLCConnectionThread";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //ITNTResponseArgs retval4status = new ITNTResponseArgs(128);
            //ITNTResponseArgs retval4PLC = new ITNTResponseArgs(128);
            ITNTResponseArgs retval = new ITNTResponseArgs();

            while (DoingPLCConnectionThread)
            {
                if (!DoingPLCConnectionThread)
                    break;

                //retval = OpenPLC(2);
                retval = OpenPLC(2);
                if(retval.execResult == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTION SUCCESS", Thread.CurrentThread.ManagedThreadId);
                    break;
                }

                //retval4status.Initialize();
                //retval4status = await ReadSignalFromPLCAsync(2);
                //retval4status.recvType = 1;
                //OnPLCStatusDataArrivedCallbackFunc(retval4status);

                if (!DoingPLCConnectionThread)
                    break;
                await Task.Delay(1000);
                if (!DoingPLCConnectionThread)
                    break;
            }
            DoingPLCConnectionThread = false;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            _plcConnectionThread = null;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public async Task<ITNTResponseArgs> ReadPLCAsync(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ReadPLCAsync";
            byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            int i = 0;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //string addr = "";
            try
            {
                //sendArg.loglevel = loglevel;
                //sendArg.dataSize = DATA_SIZE;

#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sendArg, 1, token);
                if (retval.execResult == 0)
                {
                    //retval.recvSize = RETURN_SIZE;
                    //retval.recvSize = sendArg.dataSize;
                    retval.msgtype = 0;
                    //                    retval.recvString=

                    retval.recvSize = RETURN_SIZE;
                    byte[] tmp = new byte[retval.recvSize];
                    if (retval.recvSize >= 2)
                        Array.Copy(retval.recvBuffer, 0, tmp, 0, 2);
                    else
                        Array.Copy(retval.recvBuffer, 0, tmp, 0, retval.recvSize);

                    Array.Reverse(tmp);
                    stmp = "";
                    for (i =0; i < 2; i++)
                    {
                        stmp += retval.recvBuffer[i].ToString("X2");
                    }

                    //stmp = retval.recvBuffer[0].ToString("X2");
                    retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.errorInfo.rawErrorCode = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = ex.Message;
            }

            return retval;
        }

//        public async Task<ITNTResponseArgs> ReadPLCAsync(string strAdd, int loglevel, CancellationToken token = default)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//            ITNTSendArgs sArg = new ITNTSendArgs();
//            byte[] tmpData = new byte[RETURN_SIZE];
//            int tmpSize = 0;

//            try
//            {
//                sArg.loglevel = loglevel;
//                sArg.AddrString = strAdd;
//                sArg.dataSize = DATA_SIZE;

//#if TEST_DEBUG_PLC
//                await Task.Delay(500);
//#else

//                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
//                if (retval.execResult == 0)
//                {
//                    retval.recvSize = RETURN_SIZE;
//                    if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
//                        tmpSize = HeaderData.Length;
//                    else
//                        tmpSize = RETURN_SIZE - HeaderData.Length;

//                    Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
//                    Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
//                    Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
//                    retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
//                }
//#endif
//            }
//            catch (Exception ex)
//            {
//                retval.execResult = ex.HResult;
//            }

//            return retval;
//        }

//        public ITNTResponseArgs ReadPLC(string strAdd, int loglevel)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//            ITNTSendArgs sArg = new ITNTSendArgs();
//            //string addr = "";
//            try
//            {
//                int.TryParse(PLC_ADDRESS_SIGNAL, out sArg.Address);

//                sArg.dataSize = DATA_SIZE;
//                sArg.loglevel = loglevel;


//#if TEST_DEBUG_PLC
//                Thread.Sleep(500);
//#else

//                sArg.AddrString = strAdd;

//                retval = ExecuteCommandFunc(PLC_MODE_READ, sArg, 1);
//                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_READ, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
//                //retval = ExecuteCommandMsg(PLC_MODE_READ, sArg, 2);
//                //if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
//                //{
//                //    retval = ExecuteCommandMsg(PLC_MODE_READ, sArg, 2);
//                //    if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
//                //        retval = ExecuteCommandMsg(PLC_MODE_READ, sArg, 2);
//                //}
//#endif
//            }
//            catch (Exception ex)
//            {
//                retval.execResult = ex.HResult;
//            }

//            return retval;
//        }

        public async Task<ITNTResponseArgs> WritePLCAsync(ITNTSendArgs sendArg, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //ITNTSendArgs sArg = new ITNTSendArgs();


            try
            {
                //sArg.loglevel = loglevel;
                if(sendArg.sendType == 1)
                {
                    short shtmp = 0;
                    short.TryParse(sendArg.sendString, System.Globalization.NumberStyles.HexNumber, null, out shtmp);

                    byte[] tmp = BitConverter.GetBytes(shtmp);
                    Array.Copy(tmp, 0, sendArg.sendBuffer, 0, tmp.Length);
                }

                sendArg.dataSize = DATA_SIZE;

#if TEST_DEBUG_PLC
                await Task.Delay(500);
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sendArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

//        public async Task<ITNTResponseArgs> WritePLCAsync(string sAddress, string sWriteData, int loglevel, CancellationToken token = default)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//            ITNTSendArgs sArg = new ITNTSendArgs();
//            //string addr = "";
//            try
//            {
//#if TEST_DEBUG_PLC
//                await Task.Delay(500);
//#else

//                //if (sendArg.Address > 0)
//                //    addr = string.Format("{0}", sendArg.Address);
//                //else
//                //    addr = sendArg.AddrString;
//                sArg.AddrString = sAddress;
//                sArg.sendString = sWriteData;
//                sArg.dataSize = sWriteData.Length;

//                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 0);

//                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
//                //retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
//                //if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
//                //{
//                //    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
//                //    if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
//                //        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0);
//                //}
//#endif
//            }
//            catch (Exception ex)
//            {
//                retval.execResult = ex.HResult;
//            }

//            return retval;
//        }

//        public ITNTResponseArgs WritePLC(string sAddress, string sWriteData)
//        {
//            ITNTResponseArgs retval = new ITNTResponseArgs();
//            ITNTSendArgs sArg = new ITNTSendArgs();
//            //string addr = "";
//            try
//            {
//#if TEST_DEBUG_PLC
//                Thread.Sleep(500);
//#else

//                //if (sendArg.Address > 0)
//                //    addr = string.Format("{0}", sendArg.Address);
//                //else
//                //    addr = sendArg.AddrString;
//                sArg.AddrString = sAddress;
//                sArg.sendString = sWriteData;
//                sArg.dataSize = sWriteData.Length;

//                retval = ExecuteCommandFunc(PLC_MODE_WRITE, sArg, 0);

//                ////retval.execResult = ExecuteCommandMsg(PLC_MODE_WRITE, addr, sendArg.sendBuffer, sendArg.dataSize, ref retval.recvBuffer, ref retval.recvSize, sendArg.loglevel);
//                //retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, 0);
//                //if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
//                //{
//                //    retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, 0);
//                //    if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
//                //        retval = ExecuteCommandMsg(PLC_MODE_WRITE, sArg, 0);
//                //}
//#endif
//            }
//            catch (Exception ex)
//            {
//                retval.execResult = ex.HResult;
//            }

//            return retval;
//        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        protected virtual void OnPLCStatusDataArrivedCallbackFunc(ITNTResponseArgs e)
        {
            //PLCDataArrivedEventHandler handler = PLCDataArrivedEventFunc;
            //if (handler != null)
            //    handler(this, e);
            callbackHandler?.Invoke(e);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// 
        /// 
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<ITNTResponseArgs> ReadSignalFromPLCAsync(int loglevel, CancellationToken token=default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_SIGNAL;
                int.TryParse(PLC_ADDRESS_SIGNAL, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_SIGNAL;
                sArg.plcDBNum = PLC_DBNUM_SIGNAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SIGNAL", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if(retval.execResult == 0)
                {
                    //if ((RETURN_SIZE - HeaderData.Length) >= STRING_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);

                    retval.recvSize = RETURN_SIZE;
                    stmp = retval.recvBuffer[0].ToString("X2");
                    retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if(retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType(int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_CARTYPE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_CARTYPE;
                sArg.plcDBNum = PLC_DBNUM_CARTYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CARTYPE", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    stmp = retval.recvBuffer[1].ToString("X2");
                    stmp += retval.recvBuffer[0].ToString("X2");
                    retval.recvString = sHeaderData4Digits + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCCarType4(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_CARTYPE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_CARTYPE;
                sArg.plcDBNum = PLC_DBNUM_CARTYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CARTYPE", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    short sdata = BitConverter.ToInt16(retval.recvBuffer, 0);
                    stmp = sdata.ToString("X4");
                    retval.recvString = "00FF" + stmp;
                    byte[] retdata = new byte[retval.recvString.Length + 1];
                    Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, retdata, 0);
                    if (retval.recvString.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retval.recvString.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadLinkStatusAsync(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_LINKSTATUS;
                int.TryParse(sArg.AddrString, out sArg.Address);
                
                sArg.dataSize = PLC_LENGTH_LINKSTATUS;
                sArg.plcDBNum = PLC_DBNUM_LINKSTATUS;
                //int.TryParse(sArg.AddrString, out sArg.Address);

#if TEST_DEBUG_PLC
                string value = "00FF0000";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_LINKSTATUS", value, ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    stmp = retval.recvBuffer[0].ToString("X2");
                    retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadAutoSignalFromPLC(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_AUTOMANUAL;
                int.TryParse(PLC_ADDRESS_AUTOMANUAL, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_AUTOMANUAL;
                sArg.plcDBNum = PLC_DBNUM_AUTOMANUAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_AUTOMANUAL", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    stmp = retval.recvBuffer[0].ToString("X2");
                    retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCFrameType(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_FRAMETYPE;
                int.TryParse(PLC_ADDRESS_FRAMETYPE, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_FRAMETYPE;
                sArg.plcDBNum = PLC_DBNUM_FRAMETYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_FRAMETYPE", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    stmp = retval.recvBuffer[0].ToString("X2");
                    retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCPCError(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            try
            {
                sArg.AddrString = PLC_ADDRESS_PCERROR;
                int.TryParse(PLC_ADDRESS_PCERROR, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_PCERROR;
                sArg.plcDBNum = PLC_DBNUM_PCERROR;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_PCERROR", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    stmp = retval.recvBuffer[0].ToString("X2");
                    retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence(int loglevel = 0, CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ReadPLCSequence";
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            byte bytmp = 0;
            short shtmp = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_SEQUENCE;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_SEQUENCE;
                sArg.plcDBNum = PLC_DBNUM_SEQUENCE;
                //sArg.dataSize = 4;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQUENCE", "", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;

                ////retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                ////retval.recvSize = value.Length;

                ////Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);
                //retval.recvSize = RETURN_SIZE;

                //retval.recvBuffer[0] = 0x00;
                //retval.recvBuffer[1] = 0x00;
                //retval.recvBuffer[2] = 0x82;
                //retval.recvBuffer[3] = 0x30;

                //for (int i = 0; i < 4; i++)
                //{
                //    if (i > retval.recvBuffer.Length)
                //        continue;
                //    bytmp = retval.recvBuffer[i];
                //    stmp = bytmp.ToString("X2");
                //    retval.recvString += stmp;
                //}

#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvString = "00FF";
                    retval.recvSize = RETURN_SIZE;

                    string temp = retval.recvBuffer[0].ToString("X2") + retval.recvBuffer[1].ToString("X2") + retval.recvBuffer[2].ToString("X2") + retval.recvBuffer[3].ToString("X2");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "BEFORE : " + temp, Thread.CurrentThread.ManagedThreadId);
                    Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);
                    
                    temp = retval.recvBuffer[0].ToString("X2") + retval.recvBuffer[1].ToString("X2") + retval.recvBuffer[2].ToString("X2") + retval.recvBuffer[3].ToString("X2");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AFTER : " + temp, Thread.CurrentThread.ManagedThreadId);

                    for (int i = 0; i < 4; i++)
                    {
                        if (i > retval.recvBuffer.Length)
                            continue;
                        bytmp = retval.recvBuffer[i];
                        stmp = bytmp.ToString("X2");
                        retval.recvString += stmp;
                    }

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "STRING : " + retval.recvString, Thread.CurrentThread.ManagedThreadId);

                    //shtmp = BitConverter.ToInt16(retval.recvBuffer, 0);
                    //retval.recvString = "00FF" + shtmp.ToString("D4");



                    //stmp = retval.recvBuffer[0].ToString("X2");
                    //retval.recvString = sHeaderData + stmp;
                    //byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    //if (retval.recvString.Length < RETURN_SIZE)

                    //Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCSequence4(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            byte bytmp = 0;
            byte[] bytmps;
            short shtmp = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_SEQUENCE;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_SEQUENCE;
                sArg.plcDBNum = PLC_DBNUM_SEQUENCE;
                //sArg.dataSize = 4;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                retval.recvSize = RETURN_SIZE;
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_SEQUENCE", "", ref value, "TEST.ini");
                retval.recvString = value;
                bytmps = new byte[value.Length+1];
                Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, bytmps, 0);
                Array.Copy(bytmps, 0, retval.recvBuffer, 0, RETURN_SIZE);
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = value.Length;

                //Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);

                //retval.recvBuffer[0] = 0x00;
                //retval.recvBuffer[1] = 0x00;
                //retval.recvBuffer[2] = 0x82;
                //retval.recvBuffer[3] = 0x30;

                //for (int i = 0; i < 4; i++)
                //{
                //    if (i > retval.recvBuffer.Length)
                //        continue;
                //    bytmp = retval.recvBuffer[i];
                //    stmp = bytmp.ToString("X2");
                //    retval.recvString += stmp;
                //}
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    short sdata = BitConverter.ToInt16(retval.recvBuffer, 0);
                    stmp = sdata.ToString("X4");
                    retval.recvString = "00FF" + stmp;
                    byte[] retdata = new byte[retval.recvString.Length + 1];
                    Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, retdata, 0);
                    if (retval.recvString.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retval.recvString.Length);

                    //retval.recvString = "";
                    //retval.recvSize = RETURN_SIZE;

                    //shtmp = BitConverter.ToInt16(retval.recvBuffer, 0);
                    //stmp = shtmp.ToString("X4");

                    //retval.recvString = "00FF" + stmp;

                    ////byte[] bytmp;
                    //Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, bytmps, 0);

                    //temp = retval.recvBuffer[0].ToString("X2") + retval.recvBuffer[1].ToString("X2") + retval.recvBuffer[2].ToString("X2") + retval.recvBuffer[3].ToString("X2");
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCSIEMENSTCP", "ReadAutoSignalFromPLC", "AFTER : " + temp, Thread.CurrentThread.ManagedThreadId);

                    //for (int i = 0; i < 4; i++)
                    //{
                    //    if (i > retval.recvBuffer.Length)
                    //        continue;
                    //    bytmp = retval.recvBuffer[i];
                    //    stmp = bytmp.ToString("X2");
                    //    retval.recvString += stmp;
                    //}

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCSIEMENSTCP", "ReadAutoSignalFromPLC", "STRING : " + retval.recvString, Thread.CurrentThread.ManagedThreadId);
                }
#endif
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "PLCSIEMENSTCP", "ReadAutoSignalFromPLC", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCChinaFlag(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            short shtmp = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_CHINA;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_CHINA;
                sArg.plcDBNum = PLC_DBNUM_CHINA;
                //sArg.dataSize = 1;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_CHINA", "00000008", ref value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    shtmp = BitConverter.ToInt16(retval.recvBuffer, 0);
                    retval.recvString = "00FF" + shtmp.ToString("D4");


                    //stmp = retval.recvBuffer[0].ToString("X2");
                    //retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= RETURN_SIZE)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);

                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadPLCBodyNumber(int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(16);
            ITNTSendArgs sArg = new ITNTSendArgs(16);
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";
            byte bytmp = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_BODYNUM;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_BODYNUM;
                sArg.plcDBNum = PLC_DBNUM_BODYNUM;
                //sArg.dataSize = 4;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_BODYNUM", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 2, token);
                if (retval.execResult == 0)
                {
                    retval.recvSize = RETURN_SIZE;
                    Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);

                    for (int i = 0; i < retval.recvSize; i++)
                    {
                        if (i > retval.recvBuffer.Length)
                            continue;
                        bytmp = retval.recvBuffer[i];
                        stmp = bytmp.ToString("X2");
                        retval.recvString += stmp;
                    }

                    //retval.recvSize = RETURN_SIZE;
                    ////stmp = retval.recvBuffer[0].ToString("X2");
                    //stmp = Encoding.UTF8.GetString(retval.recvBuffer, 0, 4);
                    //retval.recvString = "00FF" + stmp;
                    //byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    //if (retdata.Length >= RETURN_SIZE)
                    //    Array.Copy(retdata, 0, retval.recvBuffer, 0, RETURN_SIZE);
                    //else
                    //    Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ReadVINAsync(CancellationToken token = default)
        {
            string className = "PLCSIEMENSTCP";
            string funcName = "ReadPLCAsync";
            byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;
            string stmp = "";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sArg = new ITNTSendArgs();
            //string addr = "";
            try
            {
                sArg.AddrString = PLC_ADDRESS_VIN;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_VIN;
                sArg.plcDBNum = PLC_DBNUM_VIN;

#if TEST_DEBUG_PLC
                string value = "";
                Util.GetPrivateProfileValue("PLC", "PLC_ADDRESS_VIN", "00FF0000", ref value, "TEST.ini");
                //value = Encoding.UTF8.GetBytes(value);
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(value);
                retval.recvSize = value.Length;
                await Task.Delay(500);
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_READ, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    retval.msgtype = 0;

                    if (retval.recvSize >= sArg.dataSize)
                    {
                        Array.Reverse(retval.recvBuffer, 0, sArg.dataSize);
                        //Array.Copy(recvdata, retval.recvBuffer, sArg.dataSize);
                        retval.recvSize = sArg.dataSize;
                        retval.recvString = "00FF" + Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    }
                    else
                    {
                        Array.Reverse(retval.recvBuffer, 0, retval.recvSize);
                        if (retval.recvSize > 0)
                        {
                            Array.Copy(retval.recvBuffer, retval.recvBuffer, retval.recvSize);
                            //retval.recvSize = retval.recvSize;
                            retval.recvString = "00FF" + Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                        }
                        //else
                        //{
                        //    continue;
                        //}
                    }


                    //retval.recvString.Reverse();
                    //stmp = retval.recvString;
                    //retval.recvString = "00FF" + stmp;
                    retval.recvSize += 4;

                    //stmp = retval.recvBuffer[0].ToString("X2");
                    //retval.recvString = sHeaderData + stmp;
                    byte[] retdata = Encoding.UTF8.GetBytes(retval.recvString);
                    if (retdata.Length >= retval.recvSize)
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retval.recvSize);
                    else
                        Array.Copy(retdata, 0, retval.recvBuffer, 0, retdata.Length);
                }
#endif
            }
            catch (Exception ex)
            {
                retval.errorInfo.rawErrorCode = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = ex.Message;
            }

            return retval;
        }

        /// ////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="result"></param>
        /// <param name="loglevel"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<ITNTResponseArgs> SendMatchingResult(byte result, int loglevel=0, CancellationToken token = default)
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_MATCHRESULT;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (result == SIGNAL_PC2PLC_MATCHING_OK)
                    sArg.sendBuffer[0] = 1;
                else
                    sArg.sendBuffer[0] = 2;

                sArg.dataSize = PLC_LENGTH_MATCHRESULT;
                sArg.plcDBNum = PLC_DBNUM_MATCHRESULT;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "";
                value = string.Format("{0:D4}", result);
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MATCHRESULT", value, "TEST.ini");
                retval.recvString = value;
                retval.recvSize = value.Length;
#else

                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = 0;

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //        tmpSize = HeaderData.Length;
                //    else
                //        tmpSize = RETURN_SIZE - HeaderData.Length;

                //    Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendFrameType2PLC(string frameType, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            byte[] tmpData = new byte[RETURN_SIZE];
            int tmpSize = 0;

            try
            {
                sArg.AddrString = PLC_ADDRESS_FRAMETYPE;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.dataSize = PLC_LENGTH_FRAMETYPE;
                sArg.plcDBNum = PLC_DBNUM_FRAMETYPE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = 0;

#if TEST_DEBUG_PLC
                string value = "";
                value = string.Format("{0:D4}", frameType);
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_FRAMETYPE", value, "TEST.ini");
                retval.recvString = value;
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
                if (retval.execResult == 0)
                {
                    //retval.recvSize = RETURN_SIZE;
                    //if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                    //    tmpSize = HeaderData.Length;
                    //else
                    //    tmpSize = RETURN_SIZE - HeaderData.Length;

                    //Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                    //Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                    //Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                }

                ////string val = frameType.PadLeft(4, '0');
                //byte[] temp = Encoding.UTF8.GetBytes(frameType);
                //Array.Copy(temp, sArg.sendBuffer, temp.Length);
                //sArg.sendString = frameType;
                //sArg.dataSize = temp.Length;

                //retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2, token);

                ////retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                ////if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                ////{
                ////    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                ////    if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                ////        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 0, 2);
                ////}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendPCError2PLC(string plcvalue, int loglevel, CancellationToken token = default)
        {
            //byte[] recv = new byte[32];
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] tmpData = new byte[RETURN_SIZE];
            //int tmpSize = 0;

            try
            {
#if TEST_DEBUG_PLC
            string value = "";
            value = string.Format("{0:D4}", plcvalue);
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D210", value, "TEST.ini");
            retval.recvString = value;
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_PCERROR;
                byte[] temp = Encoding.UTF8.GetBytes(plcvalue);
                Array.Copy(temp, sArg.sendBuffer, temp.Length);
                sArg.sendString = plcvalue;
                sArg.dataSize = PLC_LENGTH_PCERROR;
                sArg.plcDBNum = PLC_DBNUM_PCERROR;
                //sArg.dataSize = temp.Length;

                //int.TryParse(sArg.AddrString, out sArg.Address);
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = 0;

                retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 1, token);
                //if (retval.execResult == 0)
                //{
                //    retval.recvSize = RETURN_SIZE;
                //    if ((RETURN_SIZE - HeaderData.Length) >= DATA_SIZE)
                //        tmpSize = HeaderData.Length;
                //    else
                //        tmpSize = RETURN_SIZE - HeaderData.Length;

                //    Array.Copy(retval.recvBuffer, 0, tmpData, HeaderData.Length, tmpSize);
                //    Array.Copy(HeaderData, 0, tmpData, 0, HeaderData.Length);
                //    Array.Copy(tmpData, 0, retval.recvBuffer, 0, retval.recvSize);
                //    retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendSignal(byte signal, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);

            try
            {
#if TEST_DEBUG_PLC
            string value = "00FF" + signal.ToString("X4");
            Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
            retval.recvString = value;
            retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
            retval.recvSize = value.Length;
#else
                sArg.AddrString = PLC_ADDRESS_SIGNAL;
                int.TryParse(sArg.AddrString, out sArg.Address);
                sArg.sendBuffer[0] = signal;
                sArg.dataSize = PLC_LENGTH_SIGNAL;
                sArg.plcDBNum = PLC_DBNUM_SIGNAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 2, token);
                //retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                //if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                //{
                //    retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                //    if (retval.execResult == (int)TCPCOMMERROR.ERR_COMMAND_BUSY)
                //        retval = await ExecuteCommandMsgAsync(PLC_MODE_WRITE, sArg, 3, 2);
                //}
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus(byte status, byte order, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                if (order == 2)
                {
                    sArg.AddrString = PLC_ADDRESS_MARKSTATUS_2;
                    sArg.dataSize = PLC_LENGTH_MARKSTATUS_2;
                    sArg.plcDBNum = PLC_DBNUM_MARKSTATUS_2;
                }
                else
                {
                    sArg.AddrString = PLC_ADDRESS_MARKSTATUS;
                    sArg.dataSize = PLC_LENGTH_MARKSTATUS;
                    sArg.plcDBNum = PLC_DBNUM_MARKSTATUS;
                }

                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (status == PLC_MARK_STATUS_DOING)
                    sArg.sendBuffer[0] = 1;
                else if (status == PLC_MARK_STATUS_COMPLETE)
                    sArg.sendBuffer[0] = 2;
                else
                    sArg.sendBuffer[0] = 0;

                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + status.ToString("X4");
                if (order == 2)
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MARKSTATUS_2", value, "TEST.ini");
                else
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MARKSTATUS", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult(string result, byte order=0, int loglevel = 0,  CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];
            try
            {
                if (order == 2)
                {
                    sArg.AddrString = PLC_ADDRESS_VISIONRESULT_2;
                    sArg.dataSize = PLC_LENGTH_VISIONRESULT_2;
                    sArg.plcDBNum = PLC_DBNUM_VISIONRESULT_2;
                }
                else
                {
                    sArg.AddrString = PLC_ADDRESS_VISIONRESULT;
                    sArg.dataSize = PLC_LENGTH_VISIONRESULT;
                    sArg.plcDBNum = PLC_DBNUM_VISIONRESULT;
                }

                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (result == "O")
                    sArg.sendBuffer[0] = 1;
                else
                    sArg.sendBuffer[0] = 2;
                sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF";
                if (result == "O")
                    value += 1.ToString("X4");
                else
                    value += 2.ToString("X4");
                if (result == "O")
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_VISIONRESULT_2", value, "TEST.ini");
                else
                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_VISIONRESULT", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMarkingStatus_2nd(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_MARKSTATUS_2;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (status == PLC_MARK_STATUS_DOING)
                    sArg.sendBuffer[0] = 1;
                else if (status == PLC_MARK_STATUS_COMPLETE)
                    sArg.sendBuffer[0] = 2;
                else
                    sArg.sendBuffer[0] = 0;

                sArg.dataSize = PLC_LENGTH_MARKSTATUS_2;
                sArg.plcDBNum = PLC_DBNUM_MARKSTATUS_2;
                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + status.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_MARKSTATUS_2", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendVisionResult_2nd(string result, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs(256);
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_VISIONRESULT_2;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                if (result == "O")
                    sArg.sendBuffer[0] = 1;
                else
                    sArg.sendBuffer[0] = 2;
                sArg.dataSize = PLC_LENGTH_VISIONRESULT_2;
                sArg.plcDBNum = PLC_DBNUM_VISIONRESULT_2;
                //sArg.dataSize = DATA_SIZE;
                sArg.sendString = Encoding.UTF8.GetString(sArg.sendBuffer, 0, sArg.dataSize);
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF";
                if (result == "O")
                    value += 1.ToString("X4");
                else
                    value += 2.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_VISIONRESULT_2", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, string address, int loglevel=0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            //byte[] recv = new byte[256];
            //string value = "";
            try
            {
                sArg.AddrString = address;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[1] = error;

                sArg.dataSize = PLC_LENGTH_SETERRORCODE;
                sArg.plcDBNum = PLC_DBNUM_SETERRORCODE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                //value = error.ToString("X4");
                //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                //retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = 4;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendErrorInfo(byte error, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            //byte[] recv = new byte[256];
            //string value = "";
            try
            {
                sArg.AddrString = PLC_ADDRESS_SETERRORCODE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[1] = error;
                sArg.dataSize = PLC_LENGTH_SETERRORCODE;
                sArg.plcDBNum = PLC_DBNUM_SETERRORCODE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                //value = error.ToString("X4");
                //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                //retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = 4;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendMovingRobot(byte distance, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
                sArg.AddrString = PLC_ADDRESS_REQMOVEROBOT;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = distance;
                sArg.dataSize = PLC_LENGTH_REQMOVEROBOT;
                sArg.plcDBNum = PLC_DBNUM_REQMOVEROBOT;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = distance.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendScanComplete(byte scanstatus, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
                sArg.AddrString = PLC_ADDRESS_SCANCOMPLETE;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = scanstatus;
                sArg.dataSize = PLC_LENGTH_SCANCOMPLETE;
                sArg.plcDBNum = PLC_DBNUM_SCANCOMPLETE;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                //string value = error.ToString("X4");
                //Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D200", value, "TEST.ini");
                //retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = 4;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SetLinkAsync(byte link, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_SETLINK;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = link;
                sArg.dataSize = PLC_LENGTH_SETLINK;
                sArg.plcDBNum = PLC_DBNUM_SETLINK;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + link.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SendCountWanring(byte status, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            ITNTSendArgs sArg = new ITNTSendArgs(64);
            byte[] recv = new byte[64];

            try
            {
                sArg.AddrString = PLC_ADDRESS_SETLINK;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = status;
                sArg.dataSize = PLC_LENGTH_SETLINK;
                sArg.plcDBNum = PLC_DBNUM_SETLINK;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + status.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else

                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif

            }
            catch(Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = ex.Message;
                retval.errorInfo.rawErrorCode = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> SendAirAsync(byte air, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_SETAIR;
                int.TryParse(sArg.AddrString, out sArg.Address);

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = air;
                sArg.dataSize = PLC_LENGTH_SETAIR;
                sArg.plcDBNum = PLC_DBNUM_SETAIR;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + air.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> SendPLCSignalAsync(byte signal, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(256);
            ITNTSendArgs sArg = new ITNTSendArgs();
            //byte[] recv = new byte[256];
            try
            {
                sArg.AddrString = PLC_ADDRESS_SIGNAL;
                int.TryParse(sArg.AddrString, out sArg.Address);

                sArg.dataSize = PLC_LENGTH_SIGNAL;
                sArg.plcDBNum = PLC_DBNUM_SIGNAL;
                //sArg.dataSize = DATA_SIZE;
                sArg.loglevel = loglevel;

                //sArg.sendBuffer[0] = 0;
                sArg.sendBuffer[0] = signal;
                //sArg.dataSize = PLC_LENGTH_SETAIR;
                //sArg.plcDBNum = PLC_DBNUM_SETAIR;
                //sArg.dataSize = DATA_SIZE;
                //sArg.loglevel = loglevel;

#if TEST_DEBUG_PLC
                string value = "00FF" + signal.ToString("X4");
                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_D610_LINK_COMMAND", value, "TEST.ini");
                retval.recvString = value;
                retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                retval.recvSize = value.Length;
#else
                retval = await ExecuteCommandFuncAsync(PLC_MODE_WRITE, sArg, 1, token);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        ///////////////////

        public async Task<ITNTResponseArgs> SetCommSettingTCP(string IP, int Port, int loglevel = 0, CancellationToken token = default)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);

            try
            {
                if (IP.Length > 0)
                    Util.WritePrivateProfileValue("PLCCOMM", "SERVERIP", IP, Constants.PARAMS_INI_FILE);
                if (Port > 0)
                    Util.WritePrivateProfileValue("PLCCOMM", "SERVERPORT", Port.ToString(), Constants.PARAMS_INI_FILE);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public ITNTResponseArgs GetCommSettingTCP(ref string IP, ref int Port)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            string value = "";

            try
            {
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERIP", "", ref IP, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "", ref value, Constants.PARAMS_INI_FILE);
                int.TryParse(value, out Port);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public ITNTResponseArgs CheckConnection(int loglevel = 0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            ITNTSendArgs sArg = new ITNTSendArgs(32);
            string value = "";

            try
            {
                bool conn = isConnected;


                //if (GetWorkSocket() == null)
                //{
                //    retval.recvString = "SOCKET NULL";
                //    retval.execResult = -1;
                //    return retval;
                //}

                //if (GetWorkSocket().Connected == false)
                //{
                //    retval.recvString = "CONNECTION FALSE";
                //    retval.execResult = -2;
                //    return retval;
                //}

                retval.recvString = "CONNECTION GOOD";
                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.recvString = "CONNECTION EXCEPTION";
            }
            return retval;
        }


    }
}
