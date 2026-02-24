using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using HalconDotNet;
using System.Windows;
using System.ComponentModel;
using ITNTUTIL;
using System.Net.Sockets;
using System.Windows.Media.Media3D;
using System.Threading;
using S7.Net;
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTCOMMON
{
    enum ASCII
    {
        SOH = 0x01,   //SOH Start of Header 헤더 시작
        STX = 0x02,   //STX Start of Text   본문 시작, 헤더 종료
        ETX = 0x03,   //ETX End of Text 본문 종료
        EOT = 0x04,   //EOT End of Transmission 전송 종료, 데이터 링크 초기화
        ENQ = 0x05,   //ENQ Enquiry 응답 요구
        ACK = 0x06,   //ACK Acknowledgment  긍정응답
        BEL = 0x07,   //Carrige Return  
        LF = 0x0A,   //Carrige Return  
        CR = 0x0D,   //Carrige Return  
        NAK = 0x15,   //NACK Acknowledgment  
    }

    enum RECVFLAG
    {
        RECVFLAG_IDLE = 0,
        RECVFLAG_RECV_REQ = 1,
        RECVFLAG_RECV_SOH = 2,
        RECVFLAG_RECV_STX = 3,
        RECVFLAG_RECV_ACK = 4,
        RECVFLAG_RECV_NAK = 5,
        RECVFLAG_RECV_ETX = 6,
        RECVFLAG_RECV_END = 7,
        RECVFLAG_RECV_S30 = 8,
        RECVFLAG_RECV_S38 = 9,
        RECVFLAG_RECV_ERR = 0xff,
    }

    enum SENDFLAG
    {
        SENDFLAG_IDLE = 0,
        SENDFLAG_SEND_REQ = 1,
        SENDFLAG_SEND_END = 2,
        SENDFLAG_SEND_ERR = 3,
    }

    enum COMMUNICATIONERROR
    {
        ERR_NO_ERROR = 0,
        ERR_SEND_DATA_FAIL = -0x0001,
        ERR_RECV_ACK_TIMEOUT = -0x0002,
        ERR_RECV_RESPONSE_TIMEOUT = -0x0003,
        ERR_RECV_RESPONSE_TIMEOUT2 = -0x0004,
        ERR_BUFFER_SIZE_ERROR = -0x0005,
        ERR_RECV_BCC_ERROR = -0x0006,
        ERR_CMD_MISSMATCH = -0x0007,
        ERR_PORT_NOT_OPENED = -0x0008,
        ERR_RECV_NAK = -0x0009,
        ERR_COMMAND_BUSY = -0x000A,
        ERR_DOING_COMMAND = -0x000B,
        ERR_NOT_READY = -0x000C,
        ERR_RECV_DATA_NONE = -0x000D,
        ERR_EXECUTE_EXCEPTION = -0x000E,
        ERR_PORT_IS_NULL = -0x000F,
        ERR_RECV_ERROR_RESP = -0x0010,
        ERR_RECV_UNKNOWN_DATA = -0x0011,
        ERR_RECV_INVALID_DATA = -0x0012,
        ERR_RECV_SEND_PARTIAL = -0x0013,
    }

    //enum TCPCOMMERROR
    //{
    //    ERR_NO_ERROR = 0,
    //    ERR_SEND_DATA_FAIL = -0x0001,
    //    ERR_RECV_ACK_TIMEOUT = -0x0002,
    //    ERR_RECV_RESPONSE_TIMEOUT = -0x0003,
    //    ERR_BUFFER_SIZE_ERROR = -0x0004,
    //    ERR_RECV_BCC_ERROR = -0x0005,
    //    ERR_CMD_MISSMATCH = -0x0006,
    //    ERR_PORT_NOT_OPENED = -0x0007,
    //    ERR_RECV_NAK = -0x0008,
    //    ERR_COMMAND_BUSY = -0x0009,
    //    ERR_DOING_COMMAND = -0x000A,
    //    ERR_NOT_READY = -0x000B,
    //    ERR_RECV_DATA_NONE = -0x000C,
    //}

    //enum LASERCONTROLLERERROR
    //{
    //    ERR_NO_ERROR = 0,
    //    ERR_SEND_DATA_FAIL = -0x0001,
    //    ERR_RECV_ACK_TIMEOUT = -0x0002,
    //    ERR_RECV_RESPONSE_TIMEOUT = -0x0003,
    //    ERR_BUFFER_SIZE_ERROR = -0x0004,
    //    ERR_RECV_BCC_ERROR = -0x0005,
    //    ERR_CMD_MISSMATCH = -0x0006,
    //    ERR_PORT_NOT_OPENED = -0x0007,
    //    ERR_PORT_IS_NULL = -0x0008,
    //    //ERR_SEND_FAIL = 0x0009,
    //    ERR_RECV_NAK = -0x0009,
    //    ERR_COMMAD_BUSY = -0x000A,
    //    ERR_RECV_ERROR_RESP = -0x000B,
    //    ERR_RECV_UNKNOWN_DATA = -0x000C,
    //    ERR_RECV_INVALID_DATA = -0x000D,
    //}

    static partial class Constants
    {
        public const string PARAMS_INI_FILE = "Parameter\\Params.ini";
        public const string DATA_CUR_COMPLETE_FILE = "Data\\CurrentComplete.ini";
        public const string DATA_COMPLETE_FILE = "Complete.dat";
        public const string SCANNER_INI_FILE = "Parameter\\ProfileScanner.ini";
        public const string IO_INI_FILE = "Parameter\\IOConfig.ini";
        public const string MARKING_INI_FILE = "Parameter\\MarkHeader.ini";
        public const string PATTERN_PATH = "Parameter\\Pattern\\";
        public const string PARAMS_PATH = "Parameter\\";
        public const string PLCVAL_INI_FILE = "Parameter\\PLCValue.ini";
        public const string FRAMETYPE_INI_FILE = "Parameter\\FrameTypeList.ini";
        public const string DISPLACEMENT_INI_FILE = "Parameter\\Displacement.ini";
        public const string FONT_INI_FILE = "Parameter\\FONT.ini";
        public const string LENZ_INI_FILE = "Parameter\\LenzConfig.ini";
        public const string MESCONF_INI_FILE = "Parameter\\MESConfig.ini";
        //public const string PLC_CONFIG_INI_FILE = "Parameter\\PLCConfig.ini";
        public const string SETTING_INI_FILE = "Parameter\\Settings.ini";
        public const string TOOLTIP_INI_FILE = "Parameter\\ToolTip.ini";


        public const Byte IO_TYPE_INPUT = 1;
        public const Byte IO_TYPE_OUTPUT = 2;
        //public const int MAX_DISPLAY_IO_NUM = 16;
        public const int MAX_DISPLAY_IO_NUM = 8;
        //public const string connstring = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=CarVisionInfo";
#if TEST_DEBUG
        //public const string connstring_plan = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=testworkplandb";
        //public const string connstring_comp = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=testcompletedb";
        //public const string connstring_error = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=errorlistdb";
        public const string connstring = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribedb";
        //public const string connstring_comp = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribedb";
        //public const string connstring_error = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=errorlistdb";
#else
#if DB_DEBUG
        public const string connstring = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribedb_T";
#else
        public const string connstring = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribedb";
        public const string connstring_comp = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribecompletedb";
        public const string connstring_dele = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribedeletedb";
        public const string connstring_error= "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribeerrdb";
#endif
        //public const string connstring_comp = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=scribedb";
        //public const string connstring_error = "server=localhost;uid=root;pwd=1234;persistsecurityinfo=True;database=errorlistdb";
#endif

        public const int LED_MAX_COUNT = 4;
        //public const double PXPERMM = 16.68d;
        public const int MAX_VIN_NO_SIZE = 19;

        //public const int MAX_PLC_IN_SIZE = 32;
        //public const int MAX_PLC_OUT_SIZE = 32;
        public const int MAX_PLC_PORT_SIZE = 32;

        public const Byte SOH = 0x01;
        public const Byte STX = 0x02;
        public const Byte ETX = 0x03;
        public const Byte CRT = 0x0d;

        public const int MAX_THICKNESS_COUNT = 11;
        public const string ERROR_CODE_PREFIX = "PREFIX";

        public const string PRINT_POS_COWL = "2";
        public const string PRINT_POS_FLOOR = "1";

        //public const int DB_NAME_NO = 0;
        //public const int DB_NAME_DATE = 1;
        //public const int DB_NAME_TIME = 2;
        //public const int DB_NAME_CARTYPE = 3;
        //public const int DB_NAME_VINNO = 4;
        //public const int DB_NAME_RESULT = 5;
        //public const int DB_NAME_RESULT2 = 6;
        //public const int DB_NAME_IMAGEPATH = 7;
        //public const int DB_NAME_IMAGEPATH2 = 8;
        //public const int DB_NAME_SEQUENCENO = 9;
        //public const int DB_NAME_RECOGVINVNO = 10;
        //public const int DB_NAME_RECOGVINVNO2 = 11;
        //public const int DB_NAME_CONFIDENCE = 12;
        //public const int DB_NAME_CONFIDENCE2 = 13;
        //public const int DB_NAME_QUALITY = 14;
        //public const int DB_NAME_QUALITY2 = 15;
        //public const int DB_NAME_PEEKTHICKNESS = 16;
        //public const int DB_NAME_PEEKTHICKNESS2 = 17;
        //public const int DB_NAME_BYPASSMODE = 18;
        //public const int DB_NAME_CONFIRMVINNO = 19;
        //public const int DB_NAME_CONFIRMVINNO2 = 20;
        //public const int DB_NAME_RECOG_RESULT = 21;
        //public const int DB_NAME_RECOG_RESULT2 = 22;
        //public const int DB_NAME_WIDTH = 23;
        //public const int DB_NAME_HEIGHT = 24;
        //public const int DB_NAME_PITCH = 25;
        //public const int DB_NAME_THICKNESS = 26;
        //public const int DB_NAME_FONTNAME = 27;
        //public const int DB_NAME_MIS_RECOG = 28;

        //public const int DB_NAME_NO = 0;
        //public const int DB_NAME_SERIAL = 1;
        //public const int DB_NAME_VIN = 2;
        //public const int DB_NAME_TYPE = 3;
        //public const int DB_NAME_BODYNO = 4;
        //public const int DB_NAME_REGION = 5;
        //public const int DB_NAME_PLCDATA = 6;
        //public const int DB_NAME_TIME = 7;
        //public const int DB_NAME_AUTO = 8;
        //public const int DB_NAME_REMARK = 9;
        //public const int DB_NAME_RAWTYPE = 10;
        //public const int DB_NAME_RAWCOUNTRY = 11;

        public const int DIO_OUT_IDX_READY = 0;
        public const int DIO_OUT_IDX_CONFIRMEND = 1;
        public const int DIO_OUT_IDX_INSPECTEND1 = 2;
        public const int DIO_OUT_IDX_INSPECTEND2 = 3;
        public const int DIO_OUT_IDX_INSPECTOK = 4;
        public const int DIO_OUT_IDX_INSPECTNG = 5;

        public const int DIO_IN_IDX_BYPASS = 0;
        public const int DIO_IN_IDX_CHECK_CARTYPE = 1;
        public const int DIO_IN_IDX_START_INSPECT = 2;
        public const int DIO_IN_IDX_START_INSPECT2 = 3;
        public const int DIO_IN_IDX_TYPE_FE = 4;
        public const int DIO_IN_IDX_TYPE_TL = 5;
        public const int DIO_IN_IDX_TYPE_NX = 6;
        public const int DIO_IN_IDX_RESET = 7;

        //public const int CAMERA_WIDTH = 2048;
        //public const int CAMERA_HEIGHT = 400;

        public const int CAMERA_FULL_RESOLUTION_X = 2048;
        public const int CAMERA_FULL_RESOLUTION_Y = 1536;

        public const int BYPASSOOPT_AUTO = 0;
        public const int BYPASSOOPT_BYPASS = 1;
        public const int BYPASSOOPT_MANUAL = 2;



        //=================================================================================
        //PLC
        //Read 옵션 변수 설정
        public const string PLC_READ = "READ";
        //=================================================================================
        //READ에 사용되는 ADDRESS 설정
        public const string PLC_ADDRESS_D100 = "D0100";                  // 미션레일 작업관련 시그널 어드레스
        //READ에 사용되는 VALUE 설정
        public const string SIGNAL_PLC2PC_MARK_READY = "0001";                  //marking start

        public const string PLC_ADDRESS_D300 =                      "D0300";           // 작업관련 시그널 어드레스[상태 및 펄트관련]
            //D0300 = K1(미션레일 셀렉트 스위치)
            //D0300 = K2(엔진레일 셀렉트 스위치)
            //D0300 = K4(혼용사용 셀렉트 스위치)
            //D0301 = K1(미션레일이상)
            //D0302 = K1(엔진레일이상)
            //D0303 = K1(도어열림)
            //D0304 = K1(자동), K2(수동)
            //D0305 = K1(소재부족)-미션레일
            //D0306 = K1(소재부족)-엔진레일
            //D0307 ~ D0309 = 스페어

        //=================================================================================
        public const string PLC_ADDRESS_D400                        = "D0400";         // 작업관련 시그널 어드레스[에러메시지 관련]
        public const string SIGNAL_PLC2PC_NO_ALARM                  = "0000";          // 알람 없음
        public const string SIGNAL_PLC2PC_MISSION_FORWARD_ALM       = "0001";          // 미션레일 전진이상
        public const string SIGNAL_PLC2PC_MISSION_BACKWARD_ALM      = "0002";          // 미션레일 후진이상
        public const string SIGNAL_PLC2PC_ENGINE_FORWARD_ALM        = "0003";          // 엔진레일 전진이상
        public const string SIGNAL_PLC2PC_ENGINE_BACKWARD_ALM       = "0004";          // 엔진레일 후진이상
        public const string SIGNAL_PLC2PC_MISSION_CLAMP_ALM         = "0005";          // 미션레일 클램프이상
        public const string SIGNAL_PLC2PC_ENGINE_CLAMP_ALM          = "0006";          // 엔진레일 클램프이상
        public const string SIGNAL_PLC2PC_MISSION_NOMAT_ALM         = "0007";          // 미션레일 소재없음
        public const string SIGNAL_PLC2PC_ENGINE_NOMAT_ALM          = "0008";          // 엔진레일 소재없음
        public const string SIGNAL_PLC2PC_ALL_NOMAT_ALM             = "0009";          // 전체레일 소재없음
        public const string SIGNAL_PLC2PC_MISSION_FULL_ALM          = "000A";          // 미션레일 만량감지
        public const string SIGNAL_PLC2PC_ENGINE_FULL_ALM           = "000B";          // 엔진레일 만량감지
        public const string SIGNAL_PLC2PC_DOOR_OPEN_ALM             = "000C";          // 도어 열림감지
        public const string SIGNAL_PLC2PC_AIR_ABNORMAL_ALM          = "000D";          // 공압이상 감지
        public const string SIGNAL_PLC2PC_EMERGENCY_STOP_ALM        = "000E";          // 비상정지 감지
        public const string SIGNAL_PLC2PC_ORG_POSITION_ALM          = "000F";          // 원위치 기동 이상 감지

        //=================================================================================
        //Write 옵션 변수 설정
        public const string PLC_WRITE = "WRITE";

        //WRITE에 사용되는 ADDRESS 설정
        public const string PLC_ADDRESS_D200            = "D0200";                 // 작업관련 시그널 어드레스

        //WRITE에 사용되는 VALUE 설정
        public const string SIGNAL_PC2PLC_CLEAR         = "0000";                   // VALUE 클리어
        public const string SIGNAL_PC2PLC_LASER_BUSY    = "0001";                   // 레이저 각인중
        public const string SIGNAL_PC2PLC_LASER_END     = "0002";                   // 레이저 각인완료
        public const string SIGNAL_PC2PLC_PC_ERROR      = "0010";                   // ERROR

        public const string PLC_ADDRESS_D201            = "D0201";                  // 집진기 시그널 어드레스
        public const string SIGNAL_PC2PLC_DUST_OFF      = "0000";                   // 집진기 중지
        public const string SIGNAL_PC2PLC_DUST_ON       = "0001";                   // 집진기 시작
//=================================================================================
        public const string PLC_ADDRESS_D210            = "D0210";                  // 작업관련 시그널 어드레스

        //WRITE에 사용되는 VALUE 설정
        public const string SIGNAL_PC2PLC_SIGNAL_PC2PLC_CYCLE_CLEAR = "0000";       // CYCLE STOP CLEAR
        public const string SIGNAL_PC2PLC_SIGNAL_PC2PLC_CYCLE_STOP = "0001";        // CYCLE STOP CLEAR START
//=================================================================================
       // public const string PLC_ADDRESS_D220 = "D0220";                             // 작업관련 시그널 어드레스

        //WRITE에 사용되는 VALUE 설정
        public const string SIGNAL_PC2PLC_SIGNAL_PC2PLC_LOCK_CLEAR = "0000";        // 도어락 해제
        public const string SIGNAL_PC2PLC_SIGNAL_PC2PLC_LOCK_STOP = "0001";         // 도어락 해제 CLEAR
//=================================================================================
        //읽고쓰는  D영역 길이(16Bit = "FFFF" = 1워드 = 2CHAR<영문>)
        public const string D_LEN_01 = "01";
        public const string D_LEN_20 = "14";


        public const int ERR_PRINTPOS_ERROR = 0x00001001;
        public const int ERR_NOCADFILE_ERROR = 0x00001002;
        public const int ERR_LOADCADFILE_ERROR = 0x00001003;
        public const int ERR_CHANGETEXT_ERROR = 0x00001004;

        public const int DB_NAME_NO = 0;
        public const int DB_NAME_PRODUCTDATE = 1;
        public const int DB_NAME_SEQUENCE = 2;
        public const int DB_NAME_RAWCARTYPE = 3;
        public const int DB_NAME_BODYNO = 4;
        public const int DB_NAME_MARKVIN = 5;
        public const int DB_NAME_MESDATE = 6;
        public const int DB_NAME_MESTIME = 7;
        public const int DB_NAME_LASTSEQ = 8;
        public const int DB_NAME_CODE219 = 9;
        public const int DB_NAME_IDPLATE = 10;
        public const int DB_NAME_DELETE = 11;
        public const int DB_NAME_TOTALMSGE = 12;
        public const int DB_NAME_RAWBODY = 13;
        public const int DB_NAME_RAWTRIM = 14;
        public const int DB_NAME_PLCVALUE = 15;
        public const int DB_NAME_REGION = 16;
        public const int DB_NAME_BODYTYPE = 17;
        public const int DB_NAME_CARTYPE = 18;
        public const int DB_NAME_MARKDATE = 19;
        public const int DB_NAME_MARKTIME = 20;
        public const int DB_NAME_REMARK = 21;
        public const int DB_NAME_ISMARK = 22;
        public const int DB_NAME_COMPLETE = 23;
        public const int DB_NAME_EXIST = 24;
        public const int DB_NAME_CHECKFLAG = 25;
        public const int DB_NAME_VISIONFLAG = 26;
        public const int DB_NAME_RAWVIN = 27;
        public const int DB_NAME_NO2 = 28;
        public const int DB_NAME_ISINSERT = 29;

    }

    public enum csConnStatus
    {
        Closed,
        Listening,
        Connecting,
        Connected,
        Disconnected
    };

    static class ErrorCodeConstant
    {
        public const int ERROR_NOERROR = 0;

        public const int ERROR_DATA_NOT_FOUND = -1;
        public const int ERROR_RECV_DATA_INVALID = -2;
        public const int ERROR_FILE_NOT_FOUND = -3;
        public const int ERROR_PARAM_INVALID = -4;
        public const int ERROR_MATCHING_NG = -5;
        public const int ERROR_DOUBLEMARK_NG = -6;
        public const int ERROR_PROCESS_ERROR = -7;
        public const int ERROR_USER_CANCEL = -8;
        public const int ERROR_PATTERN_NOTFOUND = -9;
        public const int ERROR_Z_HEIGHT_ERROR = -10;
        public const int ERROR_SLOPE_ERROR = -11;
        public const int ERROR_LINKON_ERROR = -12;
        public const int ERROR_EXCEPTION = -13;

    }

    static class ErrorCodeString
    {
        public const string ERROR_NOERROR = "00";

        public const string ERROR_DATA_NOT_FOUND = "01";
        public const string ERROR_RECV_DATA_INVALID = "02";
        public const string ERROR_FILE_NOT_FOUND = "03";
        public const string ERROR_PARAM_INVALID = "04";
        public const string ERROR_MATCHING_NG = "05";
        public const string ERROR_DOUBLEMARK_NG = "06";
        public const string ERROR_PROCESS_ERROR = "07";
        public const string ERROR_USER_CANCEL = "08";
        public const string ERROR_PATTERN_NOTFOUND = "09";
        public const string ERROR_Z_HEIGHT_ERROR = "0A";
        public const string ERROR_SLOPE_ERROR = "0B";
        public const string ERROR_LINKON_ERROR = "0C";
        public const string ERROR_EXCEPTION = "0D";
    }

    static partial class Constants
    {
        public const string ERROR_COMM = "1";
        public const string ERROR_DATA = "2";
        public const string ERROR_PARAM = "3";
        public const string ERROR_XXXX = "4";
        public const string ERROR_YYYY = "5";
        public const string ERROR_EXCEPT = "6";

        ////
        ///
        public const string ERROR_INVALID = "01";
        public const string ERROR_NOT_FOUND = "02";
        public const string ERROR_TYPE_NG = "01";
        public const string ERROR_SEQ_NG = "02";
        public const string ERROR_DOUBLE_MARK = "03";
        public const string ERROR_Z_HEIGHT = "04";
        public const string ERROR_SLOPE_BIG = "05";
        public const string ERROR_NO_PATTERN_FILE = "01";
        public const string ERROR_NO_FONT_FILE = "02";

        public const string ERROR_USER_CANCEL = "03";


        public const string ERROR_SIGNAL_DUPLICATE_01 = "01";
        public const string ERROR_SIGNAL_DUPLICATE_02 = "02";
        public const string ERROR_SIGNAL_DUPLICATE_04 = "04";
        public const string ERROR_LINKON_FAIL = "05";
        public const string ERROR_LINKOFF_FAIL = "06";


        //public const string ERROR_CAUSE1_NONE = "0";
        //public const string ERROR_CAUSE1_SEND = "1";
        //public const string ERROR_CAUSE1_RECV = "2";

        ////public const string ERROR_CAUSE1_COMM = "3";
        //public const string ERROR_CAUSE1_DATA = "3";
        //public const string ERROR_CAUSE2_INVALID_LENGTH = "01";
        //public const string ERROR_CAUSE2_NOTFOUND = "02";
        //public const string ERROR_CAUSE2_PATTERN_NAME_INVALID = "03";
        //public const string ERROR_CAUSE2_MATCHING_NG = "04";
        //public const string ERROR_CAUSE2_DOUBLEMARK_NG = "05";
        //public const string ERROR_CAUSE2_SEQUENCE_NG = "04";

        //public const string ERROR_CAUSE1_PARAM = "6";
        ////public const string ERROR_CAUSE2_PATTERN_LENGTH_INVALID = "01";
        //public const string ERROR_CAUSE2_VIN_EMPTY = "07";



        //public const string ERROR_CAUSE1_SW = "5";
        ////public const string ERROR_CAUSE1_PARAM = "6";


        //public const string ERROR_CAUSE1_FILE = "7";
        //public const string ERROR_CAUSE2_NO_PATTERN = "01";
        //public const string ERROR_CAUSE2_NO_FONT = "02";

        //public const string ERROR_CAUSE2_PLATECHECK_ERROR1 = "03";
        //public const string ERROR_CAUSE2_PLATECHECK_ERROR2 = "03";
        //public const string ERROR_CAUSE2_LINKON_FAIL = "04";

        ////public const string ERROR_CAUSE1_DOWN = "D";
        //public const string ERROR_CAUSE1_EXCEPT = "E";

        //public const string ERROR_CAUSE2_NONE = "00";
        ////public const string ERROR_CAUSE2_LENGTH = "01";
        //public const string ERROR_CAUSE2_INVALID = "02";
        //public const string ERROR_CAUSE2_PROG_NG = "06";
        //public const string ERROR_CAUSE2_CANCEL = "08";
        ////public const string ERROR_CAUSE2_NONE_PATTERN = "09";
        //public const string ERROR_CAUSE2_NONE_FILE = "0A";

        //public const string ERROR_CAUSE2_RECV = "03";
    }

    //MES,USER,DATA,PATTERN,FONT,PARAM,LINK
    //COMM      =   1
    //DATA      =   2
    //PARAM     =   3  (PATTERN, FONT, VIN)
    //XXXXX     =   4  (MATCHING, HEIGHT)
    //YYYYY     =   5 (DUPLICATE SIGNAL, LINK)
    //EXCEPTION =   6
    static class ErrorCodeConstant2
    {
        public const int ERROR_NOERROR = 0;

        public const int ERROR_RECV_DATA_LENGTH_INVALID = -1;   //PLC DATA ERROR   3101
        public const int ERROR_MES_DATA_NOT_FOUND = -2;         // APP DATA MES NOT FOUND               1102
        public const int ERROR_USER_CANCEL_SELECT = -3;         // APP ACT  CANCEL                      1403
        public const int ERROR_TYPE_MATCHING_NG = -4;           // APP ACT  TYPE MATCHING NG            1501
        public const int ERROR_SEQ_MATCHING_NG = -5;            // APP ACT  SEQ MATCHING NG             1502
        public const int ERROR_DOUBLE_MARK_NG = -6;             //APP ACT  DOUBLE MATCHING NG           1503
        public const int ERROR_PROCESS_2ND_SIGNAL_ERROR = -10;  //APP ACT  DUPLICATE MARKING SIGNAL     1407
        public const int ERROR_Z_HEIGHT_ERROR = -12;            // APP ACT Z HEIGHT ERROR               1408
        public const int ERROR_SLOPE_ERROR = -13;               //                                      1409
        public const int ERROR_PARAM_COUNT_INVALID = -11;       //                                      1202
        public const int ERROR_PARAM_LENGTH_INVLAID = -14;      //                                      
        public const int ERROR_PATTERN_NAME_INVLAID = -7;       //                                      1203
        public const int ERROR_PATTERN_FILE_NOT_FOUND = -8;     //                                      1204
        public const int ERROR_FONT_FILE_NOT_FOUND = -9;        //                                      1205
        public const int ERROR_LINKON_ERROR = -15;              

        //public const int ERROR_DATA_NOT_FOUND = -1;
        //public const int ERROR_FILE_NOT_FOUND = -3;
        //public const int ERROR_PARAM_INVALID = -4;
        //public const int ERROR_DOUBLEMARK_NG = -6;
        //public const int ERROR_PATTERN_NOTFOUND = -9;

        public const string ERROR_EXCEPT = "05";
        public const string ERROR_COMM = "01";

    }


    public class DeviceCode
    {
        public const string Device_APP = "1";
        public const string Device_SERVO = "2";
        public const string Device_PLC = "3";
        public const string Device_LASER = "4";
        public const string Device_DISTACE = "5";
    }


    public class DeviceName
    {
        public const string Device_APP = "APP";
        public const string Device_SERVO = "SERVO";
        public const string Device_PLC = "PLC";
        public const string Device_LASER = "LASER";
        public const string Device_DISTACE = "DISPLACEMENT";
    }


    public class DBUpdateEventArgs : EventArgs
    {
        public string RecvMsg { get; set; }
        public int EventType { get; set; }
    }

    //public class ResultUpdateEventArgs : EventArgs
    //{
    //    public Byte EventType { get; set; }
    //    public InspectResultEx Result { get; set; }

    //    public ResultUpdateEventArgs()
    //    {
    //        EventType = 0;
    //        Result = new InspectResultEx();
    //    }
    //}

    //public class ResultUpdateEventArgs : EventArgs
    //{
    //    public Byte EventType { get; set; }
    //    public InspectionResult Result { get; set; }

    //    public ResultUpdateEventArgs()
    //    {
    //        EventType = 0;
    //        Result = new InspectionResult();
    //    }
    //}

    //public class ResultUpdateEventArgs : EventArgs, ICloneable
    //{
    //    public int retvalue;
    //    public byte process;
    //    public long errortype;
    //    public PrcessEventReceivedArgs MarkData;
    //    //public InspectionResult Result { get; set; }

    //    public ResultUpdateEventArgs()
    //    {
    //        retvalue = 0;
    //        process = 0;
    //        errortype = 0;
    //        MarkData = new PrcessEventReceivedArgs();
    //        //Result = new InspectionResult();
    //    }

    //    public object Clone()
    //    {
    //        ResultUpdateEventArgs ret = new ResultUpdateEventArgs();
    //        ret.retvalue = this.retvalue;
    //        ret.process = this.process;
    //        ret.errortype = this.errortype;
    //        ret.MarkData = (PrcessEventReceivedArgs)this.MarkData.Clone();
    //        return ret;
    //    }
    //}

    //public class PrcessEventReceivedArgs : EventArgs, ICloneable
    //{
    //    public int ExceuteResult;
    //    public byte MarkMode;
    //    public byte printPosition;
    //    public string LaserFileName;
    //    public MarkInformation MarkInfo;
    //    public DateTime MarkTime;

    //    public PrcessEventReceivedArgs()
    //    {
    //        ExceuteResult = 0;
    //        MarkMode = 0;
    //        printPosition = 0;
    //        MarkInfo = new MarkInformation();
    //        MarkTime = DateTime.Now;
    //        LaserFileName = "";
    //    }

    //    public object Clone()
    //    {
    //        PrcessEventReceivedArgs ret = new PrcessEventReceivedArgs();
    //        ret.ExceuteResult = this.ExceuteResult;
    //        ret.MarkMode = this.MarkMode;
    //        ret.printPosition = this.printPosition;
    //        ret.LaserFileName = this.LaserFileName;
    //        ret.MarkTime = this.MarkTime;
    //        ret.MarkInfo = (MarkInformation)this.MarkInfo.Clone();
    //        return ret;
    //    }

    //}

    public class DIODataReceiveEventArgs : EventArgs, ICloneable
    {
        public string serial;
        public string model;
        public string rawmodel;
        public string bodyno;
        public string vin;
        public string country;
        public string rawcountry;

        public DIODataReceiveEventArgs()
        {
            serial = "";
            model = "";
            rawmodel = "";
            vin = "";
            country = "";
            bodyno = "";
            rawcountry = "";
        }

        public object Clone()
        {
            DIODataReceiveEventArgs ret = new DIODataReceiveEventArgs();
            ret.serial = this.serial;
            ret.model = this.model;
            ret.rawmodel = this.rawmodel;
            ret.vin = this.vin;
            ret.bodyno = this.bodyno;
            ret.country = this.country;
            ret.rawcountry = this.rawcountry;
            return ret;
        }
    }


    //public class MarkInformation : EventArgs, ICloneable
    //{
    //    public string serial;
    //    public string model;
    //    public string rawmodel;
    //    public string bodyno;
    //    public string vin;
    //    public string country;
    //    public string rawcountry;

    //    public MarkInformation()
    //    {
    //        serial = "";
    //        model = "";
    //        rawmodel = "";
    //        vin = "";
    //        country = "";
    //        bodyno = "";
    //        rawcountry = "";
    //    }

    //    public object Clone()
    //    {
    //        MarkInformation ret = new MarkInformation();
    //        ret.serial = this.serial;
    //        ret.model = this.model;
    //        ret.rawmodel = this.rawmodel;
    //        ret.vin = this.vin;
    //        ret.bodyno = this.bodyno;
    //        ret.country = this.country;
    //        ret.rawcountry = this.rawcountry;
    //        return ret;
    //    }
    //}

    //public class MarkInformation2 : EventArgs, ICloneable
    //{
    //    public string sequnce;
    //    public string cartype;
    //    public string bodyno;
    //    public string vin;
    //    public string country;
    //    public string rawcartype;
    //    public string rawcountry;
    //    public string region;
    //    public string PLCData;

    //    public MarkInformation2()
    //    {
    //        serial = "";
    //        model = "";
    //        rawmodel = "";
    //        vin = "";
    //        country = "";
    //        bodyno = "";
    //        rawcountry = "";
    //        region = "";
    //        PLCData = "";
    //    }

    //    public object Clone()
    //    {
    //        MarkInformation2 ret = new MarkInformation2();
    //        ret.serial = this.serial;
    //        ret.model = this.model;
    //        ret.rawmodel = this.rawmodel;
    //        ret.vin = this.vin;
    //        ret.bodyno = this.bodyno;
    //        ret.country = this.country;
    //        ret.rawcountry = this.rawcountry;
    //        ret.region = region;
    //        ret.PLCData = PLCData;
    //        return ret;
    //    }
    //}

    public class MarkDataRowView : ICloneable
    {
        public string serial { get; set; }
        public string type { get; set; }
        public string rawtype { get; set; }
        public string bodyno { get; set; }
        public string vin { get; set; }
        public string country { get; set; }
        public string rawcountry { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string auto { get; set; }
        public string remark { get; set; }

        public MarkDataRowView()
        {
            serial = "";
            type = "";
            rawtype = "";
            vin = "";
            country = "";
            bodyno = "";
            date = "";
            time = "";
            auto = "";
            remark = "";
            rawcountry = "";
        }

        public object Clone()
        {
            MarkDataRowView ret = new MarkDataRowView();
            ret.serial = this.serial;
            ret.type = this.type;
            ret.rawtype = this.rawtype;
            ret.vin = this.vin;
            ret.country = this.country;
            ret.rawcountry = this.rawcountry;
            ret.bodyno = this.bodyno;
            ret.date = this.date;
            ret.time = this.time;
            ret.auto = this.auto;
            ret.remark = this.remark;
            return ret;
        }
    }

    //public class ServerReceivedMsg
    //{
    //    public string seq;
    //    public string rawtype;
    //    public string bodyno;
    //    public string vin;
    //    public string code;
    //    public string rawcountry;
    //    public string type;
    //    public string country;

    //    public ServerReceivedMsg()
    //    {
    //        seq = "";
    //        rawtype = "";
    //        bodyno = "";
    //        vin = "";
    //        code = "";
    //        rawcountry = "";
    //        type = "";
    //        country = "";
    //    }
    //}

    //public struct _MESReceivedData
    //{
    //    public string factory;
    //    public string productprocess;
    //    public string producttime;
    //    public string sequence;
    //    public string cartype;
    //    public string bodyno;
    //    public string workorder;
    //    public string vin;
    //    public string mestime;
    //    public string lastsequence;
    //    public string code219;
    //    public string idplate;
    //}
    /*
     공장	42	C 	2	0	4 (4공장 2라인)
    공백	@	C 	1	2	 
    현공정 코드	T[121,4]	C 	4	3	T420 (PBS OUT)
    공백	@	C 	1	7	 
    생산기준일	T[112,8]	C 	8	8	20210922 (2021년 9월 22일)
    공백	@	C 	1	16	 
    SEQ	T[17,4]	C 	4	17	1234
    공백	@	C 	1	21	 
    차종(4)+BNO(6)	V[1,10]	C 	10	22	HE  123456
    공백	@	C 	1	32	 
    W/O NAT DEL RSV	O[1,16]	C 	16	33	E2104I006D26A@@@
    공백	@	C 	1	49	 
    차대각자 NO	V[41,17]	C	17	50	 
    공백	@	C 	1	67	 
    SYSTEM DATE	T[18,14]	C 	14	68	20090922235643
    공백	@	C 	1	82	 
    전일 마지막 SEQ	T[301,4]	C 	4	83	 
    공백	@	C 	1	87	 
    219코드	O[235,219]	C 	219	88	 
    공백	@	C 	1	307	 
    Id Plate 구분	X[K102,3]	C	3	308	지역 식별 값,  ex) 100, 210 등
    공백	@	C 	1	310	 
    삭제 Flag 1                           1 = delete data
     */
    public class MESReceivedData : ICloneable
    {
        public int execResult;
        public byte userDataType;   //0 = MES Data, 1 = Select Data, 2 = User Input Data, 3 = Pass Data("    ")
        //public string errorMessage;
        public string factory;
        public string process;
        public string productdate;
        public string sequence;
        public string rawcartype;
        public string bodyno;
        public string workorder;
        public string markvin;
        public string mesdate;
        public string mestime;
        public string lastsequence;
        public string code219;
        public string idplate;
        public string delete;
        public string totalmsg;
        public string rawbodytype;
        public string rawtrim;
        public string region;
        public string bodytype;
        public string cartype;
        public string plcvalue;
        public string markdate;
        public string marktime;
        public string remark;
        public string isMarked;
        public string exist;
        public string rawvin;
        public int no2;
        public string isInserted;

        public ErrorInfo errorInfo = new ErrorInfo();

        public MESReceivedData()
        {
            execResult = 0;
            userDataType = 0;
            //errorMessage = "";
            factory = "";
            process = "";
            productdate = "";
            sequence = "";
            rawcartype = "";
            bodyno = "";
            workorder = "";
            markvin = "";
            mesdate = "";
            mestime = "";
            lastsequence = "";
            code219 = "";
            idplate = "";
            delete = "";
            totalmsg = "";

            rawbodytype = "";
            rawtrim = "";
            region = "";
            bodytype = "";
            cartype = "";
            plcvalue = "";
            markdate = "";
            marktime = "";
            remark = "N";
            isMarked = "N";
            exist = "Y";
            rawvin = "";

            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();

            no2 = 0;
            isInserted = "0";
        }

        public object Clone()
        {
            MESReceivedData ret = new MESReceivedData();
            ret.execResult = execResult;
            ret.userDataType = userDataType;
            //ret.errorMessage = errorMessage;
            ret.factory = factory;
            ret.process = process;
            ret.productdate = productdate;
            ret.sequence = sequence;
            ret.rawcartype = rawcartype;
            ret.bodyno = bodyno;
            ret.workorder = workorder;
            ret.markvin = markvin;
            ret.mesdate = mesdate;
            ret.mestime = mestime;
            ret.lastsequence = lastsequence;
            ret.code219 = code219;
            ret.idplate = idplate;
            ret.delete = delete;
            ret.totalmsg = totalmsg;

            ret.rawbodytype = rawbodytype;
            ret.rawtrim = rawtrim;
            ret.region = region;
            ret.bodytype = bodytype;
            ret.cartype = cartype;
            ret.plcvalue = plcvalue;
            ret.markdate = markdate;
            ret.marktime = marktime;
            ret.remark = remark;
            ret.isMarked = isMarked;
            ret.exist = exist;
            ret.rawvin = rawvin;

            if(errorInfo == null)
                ret.errorInfo = new ErrorInfo();
            else
                ret.errorInfo = (ErrorInfo)errorInfo.Clone();

            ret.no2 = no2;
            ret.isInserted = isInserted;
            return ret;
        }
    }
            
    //publicidplate; class ResultUpdateEventExArgs : EventArgs
    //{
    //    public Byte EventType { get; set; }
    //    public InspectionResultEx Result { get; set; }

    //    public ResultUpdateEventExArgs()
    //    {
    //        EventType = 0;
    //        Result = new InspectionResultEx();
    //    }
    //}

    public class IOOption
    {
        public string Name { get; set; }
        public Byte IsUsed { get; set; }
        public Byte PortNum { get; set; }

        public IOOption()
        {
            Name = "";
            IsUsed = 0;
            PortNum = 0;
        }
    }

    public class CarTypeName
    {
        public string Name { get; set; }
        public Byte PortNum { get; set; }
        public Byte Value { get; set; }

        public CarTypeName()
        {
            Name = "";
            PortNum = 0;
            Value = 0;
        }
    }

    public class HALFINDRESULT
    {
        public double row;
        public double col;
        public double angle;
        public double scale;
        public double score;

        public HALFINDRESULT()
        {
            row = 0.0d;
            col = 0.0d;
            angle = 0.0d;
            scale = 0.0d;
            score = 0.0d;
        }
    }

    public class CROPRESULT
    {
        public Thickness cropRect;
        public Thickness margin;

        public CROPRESULT()
        {
            cropRect = new Thickness(0);
            margin = new Thickness(0);
        }
    }

    public class VinNoInfo : ICloneable
    {
        public string carType;
        public string vinNo;
        public string fontName;
        public double width;
        public double height;
        public double pitch;
        public double thickness;
        public string seqNo;
        public string printPost;
        public int byPassmode;
        public bool[] bSearch = new bool[Constants.MAX_VIN_NO_SIZE];
        //public int order;

        public VinNoInfo()
        {
            carType = "";
            vinNo = "";
            fontName = "";
            width = 0;
            height = 0;
            pitch = 0;
            thickness = 0.0d;
            seqNo = "";
            printPost = "";
            byPassmode = 0;
            //order = 0;
            for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
                bSearch[i] = true;
        }

#region IClonable Members
        public object Clone()
        {
            VinNoInfo retval = new VinNoInfo();
            retval.carType = this.carType;
            retval.vinNo = this.vinNo;
            retval.fontName = this.fontName;

            retval.width = this.width;
            retval.height = this.height;
            retval.pitch = this.pitch;

            retval.thickness = this.thickness;
            retval.seqNo = this.seqNo;
            retval.printPost = this.printPost;

            retval.byPassmode = this.byPassmode;

            for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
                retval.bSearch[i] = this.bSearch[i];

            return retval;
        }
#endregion
    }


    //public class PrcessEventReceivedArgs : EventArgs
    //{
    //    public int EventType { get; set; }
    //    public int InsepctOrder { get; set; }
    //    //public string RecvMsg { get; set; }
    //    public VinNoInfo vinfInfo { get; set; }
    //    public Queue<Byte> RecvData { get; set; }
    //    public int TestMode { get; set; }
    //}


    public class ChangeModelParam2 : ICloneable
    {
        public InspectionSaveDBData inspectData;
        public int vinPos;

        public ChangeModelParam2()
        {
            inspectData = new InspectionSaveDBData();
            vinPos = 0;
        }

        public object Clone()
        {
            ChangeModelParam2 ret = new ChangeModelParam2();

            ret.inspectData = (InspectionSaveDBData)this.inspectData.Clone();
            ret.vinPos = this.vinPos;

            return ret;
        }
    }


    public class InspectionResult : ICloneable
    {
        public VinNoInfo vinInfo;
        public DateTime time;
        public double peakThickness;
        public string errorCode;
        public int executeResult;
        public int inspectOrder;
        public string imagepath;
        public string imagefilename;
        public string imagefiletype;

        public RecogCharacterResult[] recogChar = new RecogCharacterResult[Constants.MAX_VIN_NO_SIZE];

        //public HObject ho_Raw_I_Image;
        //public HObject ho_Raw_P_Image;
        //public HObject ho_I_Frame_Image;
        //public HObject ho_P_Frame_Image;
        //public HObject[] ho_I_Char_Images = new HObject[Constants.MAX_VIN_NO_SIZE];
        //public HObject[] ho_P_Char_Images = new HObject[Constants.MAX_VIN_NO_SIZE];

        public InspectionResult()
        {
            vinInfo = new VinNoInfo();
            time = DateTime.Now;
            peakThickness = 0.0d;
            errorCode = "";
            executeResult = 0;
            inspectOrder = 0;
            imagepath = "";
            imagefilename = "";
            imagefiletype = "";

            //string tmp = "";
            //for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            //{
            //    recogChar[i] = new RecogCharacterResult();
            //    ITNTHOperatorSet.GenEmptyObj(out ho_I_Char_Images[i], out tmp);
            //    ITNTHOperatorSet.GenEmptyObj(out ho_P_Char_Images[i], out tmp);
            //}

            //ITNTHOperatorSet.GenEmptyObj(out ho_Raw_I_Image, out tmp);
            //ITNTHOperatorSet.GenEmptyObj(out ho_Raw_P_Image, out tmp);
            //ITNTHOperatorSet.GenEmptyObj(out ho_I_Frame_Image, out tmp);
            //ITNTHOperatorSet.GenEmptyObj(out ho_P_Frame_Image, out tmp);
        }

        public object Clone()
        {
            InspectionResult ret = new InspectionResult();

            ret.vinInfo = this.vinInfo;
            ret.time = this.time;
            ret.peakThickness = this.peakThickness;
            ret.errorCode = this.errorCode;
            ret.executeResult = this.executeResult;
            ret.inspectOrder = this.inspectOrder;
            ret.imagepath = this.imagepath;
            ret.imagefilename = this.imagefilename;
            ret.imagefiletype = this.imagefiletype;
            //string tmp = "";
            //for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            //{
            //    ret.recogChar[i] = this.recogChar[i];
            //    ret.ho_I_Char_Images[i] = this.ho_I_Char_Images[i];
            //    ret.ho_P_Char_Images[i] = this.ho_P_Char_Images[i];
            //}

            //ret.ho_I_Frame_Image = this.ho_I_Frame_Image;
            //ret.ho_P_Frame_Image = this.ho_P_Frame_Image;
            //ret.ho_Raw_I_Image = this.ho_Raw_I_Image;
            //ret.ho_Raw_P_Image = this.ho_Raw_P_Image;
            return ret;
        }
    }

    //public class InspectionResult4Test : ICloneable
    //{
    //    public VinNoInfo vinInfo;
    //    public DateTime time;
    //    public double peakThickness;
    //    public string errorCode;
    //    public int executeResult;
    //    public int inspectOrder;
    //    public string imagepath;
    //    public string imagefilename;
    //    public string imagefiletype;

    //    public RecogCharacterResult4Test[] recogChar = new RecogCharacterResult4Test[Constants.MAX_VIN_NO_SIZE];

    //    public HObject ho_Raw_I_Image;
    //    public HObject ho_Raw_P_Image;
    //    public HObject ho_I_Frame_Image;
    //    public HObject ho_P_Frame_Image;
    //    public HObject[] ho_I_Char_Images = new HObject[Constants.MAX_VIN_NO_SIZE];
    //    public HObject[] ho_P_Char_Images = new HObject[Constants.MAX_VIN_NO_SIZE];

    //    public InspectionResult4Test()
    //    {
    //        vinInfo = new VinNoInfo();
    //        time = DateTime.Now;
    //        peakThickness = 0.0d;
    //        errorCode = "";
    //        executeResult = 0;
    //        inspectOrder = 0;
    //        imagepath = "";
    //        imagefilename = "";
    //        imagefiletype = "";

    //        string tmp = "";
    //        for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
    //        {
    //            recogChar[i] = new RecogCharacterResult4Test();
    //            ITNTHOperatorSet.GenEmptyObj(out ho_I_Char_Images[i], out tmp);
    //            ITNTHOperatorSet.GenEmptyObj(out ho_P_Char_Images[i], out tmp);
    //        }

    //        ITNTHOperatorSet.GenEmptyObj(out ho_Raw_I_Image, out tmp);
    //        ITNTHOperatorSet.GenEmptyObj(out ho_Raw_P_Image, out tmp);
    //        ITNTHOperatorSet.GenEmptyObj(out ho_I_Frame_Image, out tmp);
    //        ITNTHOperatorSet.GenEmptyObj(out ho_P_Frame_Image, out tmp);
    //    }

    //    public object Clone()
    //    {
    //        InspectionResult4Test ret = new InspectionResult4Test();

    //        ret.vinInfo = this.vinInfo;
    //        ret.time = this.time;
    //        ret.peakThickness = this.peakThickness;
    //        ret.errorCode = this.errorCode;
    //        ret.executeResult = this.executeResult;
    //        ret.inspectOrder = this.inspectOrder;
    //        ret.imagepath = this.imagepath;
    //        ret.imagefilename = this.imagefilename;
    //        ret.imagefiletype = this.imagefiletype;
    //        //string tmp = "";
    //        for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
    //        {
    //            ret.recogChar[i] = this.recogChar[i];
    //            ret.ho_I_Char_Images[i] = this.ho_I_Char_Images[i];
    //            ret.ho_P_Char_Images[i] = this.ho_P_Char_Images[i];
    //        }

    //        ret.ho_I_Frame_Image = this.ho_I_Frame_Image;
    //        ret.ho_P_Frame_Image = this.ho_P_Frame_Image;
    //        ret.ho_Raw_I_Image = this.ho_Raw_I_Image;
    //        ret.ho_Raw_P_Image = this.ho_Raw_P_Image;
    //        return ret;
    //    }
    //}

    public class RecogCharacterResult : ICloneable
    {
        public string errorCode;
        public int executeResult;
        //public bool bRegist;
        public RecogScores[] oneCharResult = new RecogScores[4];

        public RecogCharacterResult()
        {
            errorCode = "";
            executeResult = 0;
            //bRegist = true;
            for (int i = 0; i < 4; i++)
                oneCharResult[i] = new RecogScores();
        }

        public object Clone()
        {
            RecogCharacterResult ret = new RecogCharacterResult();
            ret.errorCode = this.errorCode;
            ret.executeResult = this.executeResult;
            //ret.bRegist = this.bRegist;
            for (int i = 0; i < 4; i++)
                ret.oneCharResult[i] = this.oneCharResult[i];

            return ret;
        }

        public void SwapOneCharResult(int index1, int index2)
        {
            if (index1 == index2)
                return;

            if (index1 >= 3)
                return;

            if (index2 >= 3)
                return;

            RecogScores temp = new RecogScores();
            temp = (RecogScores)oneCharResult[index1].Clone();
            oneCharResult[index1] = (RecogScores)oneCharResult[index2].Clone();
            oneCharResult[index2] = (RecogScores)temp.Clone();
        }
    }


    //public class RecogCharacterResult4Test : ICloneable
    //{
    //    public string errorCode;
    //    public int executeResult;
    //    //public bool bRegist;
    //    public RecogScores4Test[] oneCharResult = new RecogScores4Test[4];

    //    public RecogCharacterResult4Test()
    //    {
    //        errorCode = "";
    //        executeResult = 0;
    //        //bRegist = true;
    //        for (int i = 0; i < 4; i++)
    //            oneCharResult[i] = new RecogScores4Test();
    //    }

    //    public object Clone()
    //    {
    //        RecogCharacterResult4Test ret = new RecogCharacterResult4Test();
    //        ret.errorCode = this.errorCode;
    //        ret.executeResult = this.executeResult;
    //        //ret.bRegist = this.bRegist;
    //        for (int i = 0; i < 4; i++)
    //            ret.oneCharResult[i] = this.oneCharResult[i];

    //        return ret;
    //    }

    //    public void SwapOneCharResult(int index1, int index2)
    //    {
    //        if (index1 == index2)
    //            return;

    //        if (index1 >= 3)
    //            return;

    //        if (index2 >= 3)
    //            return;

    //        RecogScores4Test temp = new RecogScores4Test();
    //        temp = (RecogScores4Test)oneCharResult[index1].Clone();
    //        oneCharResult[index1] = (RecogScores4Test)oneCharResult[index2].Clone();
    //        oneCharResult[index2] = (RecogScores4Test)temp.Clone();
    //    }
    //}

    public class RecogScores : ICloneable
    {
        public char Character;
        public double recogRate_TOT;
        public double recogRate_Conf;
        public double recogRate_CP;
        public double recogRate_CI;
        public double recogRate_GP;
        public double recogRate_GI;
        public double recogRate_RGP;
        public double recogRate_RGI;
        public FindModelResult findPosition;

        public RecogScores()
        {
            Character = (char)' ';
            recogRate_TOT = 0.0d;
            recogRate_Conf = 0.0d;
            recogRate_CP = 0.0d;
            recogRate_CI = 0.0d;
            recogRate_GP = 0.0d;
            recogRate_GI = 0.0d;
            recogRate_RGP = 0.0d;
            recogRate_RGI = 0.0d;
            findPosition = new FindModelResult();
        }

        public object Clone()
        {
            RecogScores ret = new RecogScores();
            ret.Character = this.Character;
            ret.recogRate_TOT = this.recogRate_TOT;
            ret.recogRate_Conf = this.recogRate_Conf;
            ret.recogRate_CP = this.recogRate_CP;
            ret.recogRate_CI = this.recogRate_CI;
            ret.recogRate_GP = this.recogRate_GP;
            ret.recogRate_GI = this.recogRate_GI;
            ret.recogRate_RGP = this.recogRate_RGP;
            ret.recogRate_RGI = this.recogRate_RGI;
            ret.findPosition = (FindModelResult)this.findPosition.Clone();
            return ret;
        }

        public static void SwapOneCharResult(ref RecogScores a, ref RecogScores b)
        {
            RecogScores temp = new RecogScores();
            temp = (RecogScores)a.Clone();
            a = (RecogScores)b.Clone();
            b = (RecogScores)temp.Clone();
        }

    }

    //public class RecogScores4Test : ICloneable
    //{
    //    public char Character;
    //    public double recogRate_TOT;
    //    public double recogRate_Conf;
    //    public double recogRate_CP;
    //    public double recogRate_CI;
    //    public double recogRate_GP;
    //    public double recogRate_GI;
    //    public double recogRate_DP;
    //    public double recogRate_DI;
    //    public FindModelResult findPosition;

    //    public RecogScores4Test()
    //    {
    //        Character = (char)' ';
    //        recogRate_TOT = 0.0d;
    //        recogRate_Conf = 0.0d;
    //        recogRate_CP = 0.0d;
    //        recogRate_CI = 0.0d;
    //        recogRate_GP = 0.0d;
    //        recogRate_GI = 0.0d;
    //        recogRate_DP = 0.0d;
    //        recogRate_DI = 0.0d;
    //        findPosition = new FindModelResult();
    //    }

    //    public object Clone()
    //    {
    //        RecogScores4Test ret = new RecogScores4Test();
    //        ret.Character = this.Character;
    //        ret.recogRate_TOT = this.recogRate_TOT;
    //        ret.recogRate_Conf = this.recogRate_Conf;
    //        ret.recogRate_CP = this.recogRate_CP;
    //        ret.recogRate_CI = this.recogRate_CI;
    //        ret.recogRate_GP = this.recogRate_GP;
    //        ret.recogRate_GI = this.recogRate_GI;
    //        ret.recogRate_DP = this.recogRate_DP;
    //        ret.recogRate_DI = this.recogRate_DI;
    //        ret.findPosition = (FindModelResult)this.findPosition.Clone();
    //        return ret;
    //    }

    //    public static void SwapOneCharResult(ref RecogScores4Test a, ref RecogScores4Test b)
    //    {
    //        RecogScores4Test temp = new RecogScores4Test();
    //        temp = (RecogScores4Test)a.Clone();
    //        a = (RecogScores4Test)b.Clone();
    //        b = (RecogScores4Test)temp.Clone();
    //    }
    //}

    //public class CharacterResult
    //{
    //    public char Character;
    //    public double recogRate_TOT;
    //    public double recogRate_CP;
    //    public double recogRate_CI;
    //    public double recogRate_GP;
    //    public double recogRate_GI;
    //    public FindModelResult findPosition;

    //    public CharacterResult()
    //    {

    //    }

    //    public object Clone()
    //    {
    //        CharacterResult ret = new CharacterResult();
    //        return ret;
    //    }

    //}

    public class FindModelResult : ICloneable
    {
        public double row;
        public double col;
        public double angle;
        public double scale;
        public double score;

        public FindModelResult()
        {
            row = 0.0d;
            col = 0.0d;
            angle = 0.0d;
            scale = 0.0d;
            score = 0.0d;
        }

        public object Clone()
        {
            FindModelResult ret = new FindModelResult();
            ret.row = this.row;
            ret.col = this.col;
            ret.angle = this.angle;
            ret.scale = this.scale;
            ret.score = this.score;

            return ret;
        }
    }

    public class InspectionSaveDBData : ICloneable
    {
        public InspectionResult inspectResult;
        public string recogResult;
        public string recogVinNo;
        public string confirmResult;
        public string confirmVinNo;
        public string fileName;
        public double[] Confidence = new double[Constants.MAX_VIN_NO_SIZE];
        public double[] Quality = new double[Constants.MAX_VIN_NO_SIZE];
        public string DBChangeType;

        public InspectionSaveDBData()
        {
            inspectResult = new InspectionResult();
            recogResult = "";
            recogVinNo = "";
            confirmResult = "";
            confirmVinNo = "";
            fileName = "";
            DBChangeType = "0";

            for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            {
                Confidence[i] = 0.0d;
                Quality[i] = 0.0d;
            }
        }

        public object Clone()
        {
            InspectionSaveDBData ret = new InspectionSaveDBData();

            ret.inspectResult = (InspectionResult)this.inspectResult.Clone();
            ret.recogResult = this.recogResult;
            ret.recogVinNo = this.recogVinNo;
            ret.confirmResult = this.confirmResult;
            ret.confirmVinNo = this.confirmVinNo;
            ret.fileName = this.fileName;
            ret.DBChangeType = this.DBChangeType;

            for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            {
                ret.Confidence[i] = this.Confidence[i];
                ret.Quality[i] = this.Quality[i];
            }

            return ret;
        }
    }


    public class RecogCharacterResultEx : ICloneable
    {
        public string errorCode;
        public int executeResult;
        public bool bRegist;
        public List<RecogScores> scores = new List<RecogScores>();

        public RecogCharacterResultEx()
        {
            errorCode = "";
            executeResult = 0;
            bRegist = true;
            scores = new List<RecogScores>();
            scores.Clear();
        }

        public object Clone()
        {
            RecogCharacterResultEx ret = new RecogCharacterResultEx();
            ret.errorCode = this.errorCode;
            ret.executeResult = this.executeResult;
            ret.bRegist = this.bRegist;
            for (int i = 0; i < scores.Count; i++)
                ret.scores.Add(this.scores[i]);

            return ret;
        }

        //public void SwapOneCharResult(int index1, int index2)
        //{
        //    if (index1 == index2)
        //        return;

        //    if (index1 >= 3)
        //        return;

        //    if (index2 >= 3)
        //        return;

        //    RecogScores temp = new RecogScores();
        //    temp = (RecogScores)scores[index1].Clone();
        //    scores[index1] = (RecogScores)scores[index2].Clone();
        //    scores[index2] = (RecogScores)temp.Clone();
        //}

    }

    public class InspectionResultEx : ICloneable
    {
        public VinNoInfo vinInfo;
        public DateTime time;
        public double peakThickness;
        public string errorCode;
        public int executeResult;
        public int inspectOrder;
        public string imageFileName;
        public RecogCharacterResultEx[] recogChar = new RecogCharacterResultEx[Constants.MAX_VIN_NO_SIZE];

        //public HObject ho_I_Frame_Image;
        //public HObject ho_P_Frame_Image;

        //public HObject[] ho_I_Char_Images = new HObject[Constants.MAX_VIN_NO_SIZE];
        //public HObject[] ho_P_Char_Images = new HObject[Constants.MAX_VIN_NO_SIZE];

        public InspectionResultEx()
        {
            vinInfo = new VinNoInfo();
            time = DateTime.Now;
            peakThickness = 0.0d;
            errorCode = "";
            executeResult = 0;
            inspectOrder = 0;
            imageFileName = "";
            //string tmp = "";
            //for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            //{
            //    recogChar[i] = new RecogCharacterResultEx();
            //    ITNTHOperatorSet.GenEmptyObj(out ho_I_Char_Images[i], out tmp);
            //    ITNTHOperatorSet.GenEmptyObj(out ho_P_Char_Images[i], out tmp);
            //}

            //ITNTHOperatorSet.GenEmptyObj(out ho_I_Frame_Image, out tmp);
            //ITNTHOperatorSet.GenEmptyObj(out ho_P_Frame_Image, out tmp);
        }

        public object Clone()
        {
            InspectionResultEx ret = new InspectionResultEx();

            ret.vinInfo = this.vinInfo;
            ret.time = this.time;
            ret.peakThickness = this.peakThickness;
            ret.errorCode = this.errorCode;
            ret.executeResult = this.executeResult;
            ret.inspectOrder = this.inspectOrder;
            ret.imageFileName = this.imageFileName;
            //string tmp = "";
            //for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            //{
            //    ret.recogChar[i] = this.recogChar[i];
            //    ret.ho_I_Char_Images[i] = this.ho_I_Char_Images[i];
            //    ret.ho_P_Char_Images[i] = this.ho_P_Char_Images[i];
            //}

            //ret.ho_I_Frame_Image = this.ho_I_Frame_Image;
            //ret.ho_P_Frame_Image = this.ho_P_Frame_Image;

            return ret;
        }
    }


    public class InspectionSaveDBDataEx : ICloneable
    {
        public InspectionResultEx inspectResult;
        public string recogResult;
        public string recogVinNo;
        public string confirmResult;
        public string confirmVinNo;
        public string fileName;
        public double[] Confidence = new double[Constants.MAX_VIN_NO_SIZE];
        public double[] Quality = new double[Constants.MAX_VIN_NO_SIZE];
        public string DBChangeType;

        public InspectionSaveDBDataEx()
        {
            inspectResult = new InspectionResultEx();
            recogResult = "";
            recogVinNo = "";
            confirmResult = "";
            confirmVinNo = "";
            fileName = "";
            DBChangeType = "0";

            for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            {
                Confidence[i] = 0.0d;
                Quality[i] = 0.0d;
            }
        }

        public object Clone()
        {
            InspectionSaveDBDataEx ret = new InspectionSaveDBDataEx();

            ret.inspectResult = (InspectionResultEx)this.inspectResult.Clone();
            ret.recogResult = this.recogResult;
            ret.recogVinNo = this.recogVinNo;
            ret.confirmResult = this.confirmResult;
            ret.confirmVinNo = this.confirmVinNo;
            ret.fileName = this.fileName;
            ret.DBChangeType = this.DBChangeType;

            for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            {
                ret.Confidence[i] = this.Confidence[i];
                ret.Quality[i] = this.Quality[i];
            }

            return ret;
        }
    }

    public class InspectCharacterParam
    {
        //public HObject ho_In_I_Image;
        //public HObject ho_In_P_Image;
        //public int position;
        //public char character;
        //public double percent_GP;
        //public double percent_GI;
        //public double[] score = new double[HalconControl.useChar.Length];
        //public string printpost;
        //public int checkoption;
        //public bool changeLetter;
        //public double changeMinScore;
        //public double spaceMinScore;
        //public List<char> skipLetters;
        //public HTuple[] h_ModelID_C = new HTuple[HalconControl.useChar.Length];
        //public bool[] b_Model_C = new bool[HalconControl.useChar.Length];
        //public HTuple[] h_ModelID_GP = new HTuple[HalconControl.useChar.Length];
        //public bool[] b_Model_GP = new bool[HalconControl.useChar.Length];
        //public HTuple[] h_ModelID_GI = new HTuple[HalconControl.useChar.Length];
        //public bool[] b_Model_GI = new bool[HalconControl.useChar.Length];
        //public HTuple[] h_ModelID_G = new HTuple[HalconControl.useChar.Length];
        //public bool[] b_Model_G = new bool[HalconControl.useChar.Length];

        //public InspectCharacterParam()
        //{
        //    string error;
        //    ITNTHOperatorSet.GenEmptyObj(out ho_In_I_Image, out error);
        //    ITNTHOperatorSet.GenEmptyObj(out ho_In_P_Image, out error);
        //    position = 0;
        //    character = (char)' ';
        //    percent_GP = 0.6d;
        //    percent_GI = 0.4d;
        //    printpost = "1";
        //    checkoption = 0;
        //    changeLetter = false;
        //    changeMinScore = 0.0d;
        //    spaceMinScore = 0.0d;
        //    skipLetters = new List<char>();
        //    for (int i = 0; i < HalconControl.useChar.Length; i++)
        //    {
        //        score[i] = 0.0d;
        //        h_ModelID_C[i] = new HTuple();
        //        h_ModelID_GP[i] = new HTuple();
        //        h_ModelID_GI[i] = new HTuple();
        //        h_ModelID_G[i] = new HTuple();
        //        b_Model_C[i] = false;
        //        b_Model_GP[i] = false;
        //        b_Model_GI[i] = false;
        //        b_Model_G[i] = false;
        //    }
        //}
    }

    public class byPassOption
    {
        public string cartype;
        public int option;
        public int contError;

        public byPassOption()
        {
            cartype = "";
            option = 0;
            contError = 0;
        }
    }

    public class OptionDataBase
    {
        public string ImageSavePath1;
        public string ImageSavePath2;

        public string DataDeleteOpt;
        public string DataDeleteInterval;

        public string MarkCommIP;
        public string VisionIP;
        public string MarkCommPort;

        public string PLCCommType;
        public string PLCCommTCPPort;
        public string PLCCommSerilaPort;

        public string[] RecogRate = new string[Constants.MAX_VIN_NO_SIZE];
        public string[] QualityRate = new string[Constants.MAX_VIN_NO_SIZE];

        public OptionDataBase()
        {
            //optBypassOptCount = 0;

            ImageSavePath1 = "D:\\VISION";
            ImageSavePath2 = "D:\\VISION2";

            DataDeleteOpt = "";
            DataDeleteInterval = "";

            MarkCommIP = "";
            VisionIP = "";
            MarkCommPort = "";

            PLCCommType = "";
            PLCCommTCPPort = "";
            PLCCommSerilaPort = "";

            for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
            {
                RecogRate[i] = "";
                QualityRate[i] = "";
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public static bool operator ==(OptionDataBase a, OptionDataBase b)
        {
            try
            {
                if (!string.Equals(a.ImageSavePath1, b.ImageSavePath1))
                    return false;

                if (a.ImageSavePath2 != b.ImageSavePath2)
                    return false;

                if (a.DataDeleteOpt != b.DataDeleteOpt)
                    return false;

                if (a.MarkCommIP != b.MarkCommIP)
                    return false;

                if (a.VisionIP != b.VisionIP)
                    return false;

                if (a.MarkCommPort != b.MarkCommPort)
                    return false;

                if (a.PLCCommType != b.PLCCommType)
                    return false;

                if (a.PLCCommTCPPort != b.PLCCommTCPPort)
                    return false;

                if (a.PLCCommSerilaPort != b.PLCCommSerilaPort)
                    return false;

                for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
                {
                    if (a.RecogRate[i] != b.RecogRate[i])
                        return false;
                    if (a.QualityRate[i] != b.QualityRate[i])
                        return false;
                }

                if (a.DataDeleteInterval != b.DataDeleteInterval)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "OptionDataBase", "==", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1:X}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return false;
            }
        }

        public static bool operator !=(OptionDataBase a, OptionDataBase b)
        {
            try
            {
                if (a.ImageSavePath1 != b.ImageSavePath1)
                    return true;

                if (a.ImageSavePath2 != b.ImageSavePath2)
                    return true;

                if (a.DataDeleteOpt != b.DataDeleteOpt)
                    return true;

                if (a.MarkCommIP != b.MarkCommIP)
                    return true;

                if (a.VisionIP != b.VisionIP)
                    return true;

                if (a.MarkCommPort != b.MarkCommPort)
                    return true;

                if (a.PLCCommType != b.PLCCommType)
                    return true;

                if (a.PLCCommTCPPort != b.PLCCommTCPPort)
                    return true;

                if (a.PLCCommSerilaPort != b.PLCCommSerilaPort)
                    return true;

                for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
                {
                    if (a.RecogRate[i] != b.RecogRate[i])
                        return true;

                    if (a.QualityRate[i] != b.QualityRate[i])
                        return true;
                }

                if (a.DataDeleteInterval != b.DataDeleteInterval)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "OptionDataBase", "operator !=", string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return true;
            }
        }
    }

    public class OptionData3
    {
        public OptionDataBase option;
        public int optBypassOptCount;
        public byPassOption[] byPassOption = new byPassOption[8];
        public int bypassType;
        public int bypassCount;
        public int autoClearHour;
        public int autoClearMin;

        public OptionData3()
        {
            option = new OptionDataBase();
            for (int i = 0; i < 8; i++)
                byPassOption[i] = new byPassOption();

            optBypassOptCount = 0;

            bypassType = 0;
            bypassCount = 0;

            autoClearHour = 0;
            autoClearMin = 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public static bool operator ==(OptionData3 a, OptionData3 b)
        {
            try
            {
                if (a.option != b.option)
                    return false;

                if (a.optBypassOptCount != b.optBypassOptCount)
                    return false;

                for (int i = 0; i < a.optBypassOptCount; i++)
                {
                    if (a.byPassOption[i].cartype != b.byPassOption[i].cartype)
                        return false;

                    if (a.byPassOption[i].contError != b.byPassOption[i].contError)
                        return false;

                    if (a.byPassOption[i].option != b.byPassOption[i].option)
                        return false;
                }

                if (a.bypassType != b.bypassType)
                    return false;

                if (a.bypassCount != b.bypassCount)
                    return false;

                if (a.autoClearHour != b.autoClearHour)
                    return false;

                if (a.autoClearMin != b.autoClearMin)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "OptionData3", "operator ==", string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return false;
            }

        }

        public static bool operator !=(OptionData3 a, OptionData3 b)
        {
            try
            {
                if (a.option != b.option)
                    return true;

                if (a.optBypassOptCount != b.optBypassOptCount)
                    return true;

                for (int i = 0; i < a.optBypassOptCount; i++)
                {
                    if (a.byPassOption[i].cartype != b.byPassOption[i].cartype)
                        return true;

                    if (a.byPassOption[i].contError != b.byPassOption[i].contError)
                        return true;

                    if (a.byPassOption[i].option != b.byPassOption[i].option)
                        return true;
                }

                if (a.bypassType != b.bypassType)
                    return true;

                if (a.bypassCount != b.bypassCount)
                    return true;

                if (a.autoClearHour != b.autoClearHour)
                    return true;

                if (a.autoClearMin != b.autoClearMin)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "OptionData3", "operator !=", string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return true;
            }
        }
    }

    //public struct FontData
    //{
    //    //public double X;
    //    //public double Y;
    //    //public double Z;
    //    public Vector3D vector3d;
    //    public int Flag;

    //    public FontData(Vector3D vector3d, int f)
    //    {
    //        this.vector3d = vector3d;
    //        Flag = f;
    //    }
    //}

    public class FontDataClass : ICloneable
    {
        public Vector3D vector3d;
        public int Flag;

        public FontDataClass()
        {
            vector3d = new Vector3D();
            Flag = 0;
        }

        public FontDataClass(Vector3D vector3d, int f)
        {
            this.vector3d = vector3d;
            Flag = f;
        }

        public FontDataClass(double x, double y, double z, int f)
        {
            this.vector3d.X = x;
            this.vector3d.Y = y;
            this.vector3d.Z = z;
            Flag = f;
        }

        public object Clone()
        {
            FontDataClass ret = new FontDataClass();
            ret.vector3d = vector3d;
            ret.Flag = Flag;
            return ret;
        }

        public void Clean()
        {
            vector3d = new Vector3D();
            Flag = 0;
        }
    }


    //public struct FontData
    //{
    //    public double X;
    //    public double Y;
    //    public double Z;
    //    public int Flag;

    //    public FontData(double x, double y, double z, int f)
    //    {
    //        X = x;
    //        Y = y;
    //        Z = z;
    //        Flag = f;
    //    }
    //}

    //public struct FontDataLaser
    //{
    //    public double X;
    //    public double Y;
    //    public double Z;
    //    //public double C;
    //    public int Flag;
    //}


    public class FontData4Send : ICloneable
    {
        public byte cN, fN;
        public UInt16 mX, mY, mZ, mI, mC;
        public byte mF;

        public FontData4Send()
        {
            cN = fN = 0;
            mX = mY = mZ = mC = mI = 0;
            mF = 0xff;
        }

        public object Clone()
        {
            FontData4Send ret = new FontData4Send();
            ret.cN = cN;
            ret.fN = fN;
            ret.mX = mX;
            ret.mY = mY;
            ret.mZ = mZ;
            ret.mF = mF;
            ret.mI = mI;
            return ret;
        }
    }



    public class RemarkData : ICloneable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string date;
        string time;
        string serial;
        string type;
        string bodyno;
        string vin;
        string country;

        public RemarkData()
        {
            date = "";
            time = "";
            serial = "";
            type = "";
            bodyno = "";
            vin = "";
            country = "";
        }

        public object Clone()
        {
            RemarkData ret = new RemarkData();
            ret.date = this.date;
            ret.time = this.time;
            ret.serial = this.serial;
            ret.type = this.type;
            ret.bodyno = this.bodyno;
            ret.vin = this.vin;
            ret.country = this.country;

            return ret;
        }

        protected void Notify(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }

    public class ServerReceivedEventArgs : ICloneable
    {
        public string recvMsg;
        public byte[] recvBuffer;
        public int recvSize;

        public ServerReceivedEventArgs()
        {
            recvMsg = "";
            recvBuffer = new byte[1024];
            recvSize = 1024;
        }

        public ServerReceivedEventArgs(int size)
        {
            recvMsg = "";
            if(size > 0)
            {
                recvBuffer = new byte[size];
                recvSize = size;
            }
            else
            {
                recvBuffer = new byte[1024];
                recvSize = 1024;
            }
        }

        public object Clone()
        {
            ServerReceivedEventArgs ret = new ServerReceivedEventArgs(recvSize);
            ret.recvMsg = this.recvMsg;
            Array.Copy(recvBuffer, ret.recvBuffer, recvSize);
            ret.recvSize = recvSize;
            return ret;
        }
    }

    public class DeviceStatusChangedEventArgs : ICloneable
    {
        public int exeResult;
        public csConnStatus oldstatus;
        public csConnStatus newstatus;

        public DeviceStatusChangedEventArgs()
        {
            exeResult = 0;
            oldstatus = csConnStatus.Closed;
            newstatus = csConnStatus.Closed;
        }

        public object Clone()
        {
            DeviceStatusChangedEventArgs ret = new DeviceStatusChangedEventArgs();
            ret.exeResult = exeResult;
            ret.oldstatus = this.oldstatus;
            ret.newstatus = this.newstatus;

            return ret;
        }
    }



    public class ServerStatusChangedEventArgs : ICloneable
    {
        public csConnStatus oldstatus;
        public csConnStatus newstatus;

        public ServerStatusChangedEventArgs()
        {
            oldstatus = csConnStatus.Closed;
            newstatus = csConnStatus.Closed;
        }

        public object Clone()
        {
            ServerStatusChangedEventArgs ret = new ServerStatusChangedEventArgs();
            ret.oldstatus = this.oldstatus;
            ret.newstatus = this.newstatus;

            return ret;
        }
    }


    public class ConnectionStatusChangedEventArgs : ICloneable
    {
        public csConnStatus oldstatus;
        public csConnStatus newstatus;

        public ConnectionStatusChangedEventArgs()
        {
            oldstatus = csConnStatus.Closed;
            newstatus = csConnStatus.Closed;
        }

        public object Clone()
        {
            ConnectionStatusChangedEventArgs ret = new ConnectionStatusChangedEventArgs();
            ret.oldstatus = this.oldstatus;
            ret.newstatus = this.newstatus;

            return ret;
        }
    }


    public class MESClientReceivedEventArgs : ICloneable
    {
        public string recvMsg;
        public byte[] recvBuffer;
        public int recvSize;
        public int blockCount;
        public int recordSize;
        public int serial;
        public int itotalCount;

        public MESClientReceivedEventArgs()
        {
            recvMsg = "";
            recvBuffer = new byte[1024*8];
            recvSize = 1024;
            blockCount = 0;
            recordSize = 0;
            serial = 0;
            itotalCount = 0;
        }

        public MESClientReceivedEventArgs(int size)
        {
            recvMsg = "";
            if (size > 0)
            {
                recvBuffer = new byte[size];
                recvSize = size;
            }
            else
            {
                recvBuffer = new byte[1024];
                recvSize = 1024;
            }
            blockCount = 0;
            recordSize = 0;
            serial = 0;
        }

        public object Clone()
        {
            MESClientReceivedEventArgs ret = new MESClientReceivedEventArgs(recvSize);
            ret.recvMsg = this.recvMsg;
            Array.Copy(recvBuffer, ret.recvBuffer, recvSize);
            ret.recvSize = recvSize;
            ret.blockCount = blockCount;
            ret.recordSize = recordSize;
            ret.serial = serial;
            return ret;
        }
    }


    //public class ServerStatusChangedEventArgs : ICloneable
    //{
    //    public csConnStatus oldstatus;
    //    public csConnStatus newstatus;

    //    public ServerStatusChangedEventArgs()
    //    {
    //        oldstatus = csConnStatus.Closed;
    //        newstatus = csConnStatus.Closed;
    //    }

    //    public object Clone()
    //    {
    //        ServerStatusChangedEventArgs ret = new ServerStatusChangedEventArgs();
    //        ret.oldstatus = this.oldstatus;
    //        ret.newstatus = this.newstatus;

    //        return ret;
    //    }
    //}

    public class LaserParams : ICloneable
    {
        public int nPenNo;                     // Pen’s NO. (0-255)
        public int nMarkLoop;              //mark times
        public double dMarkSpeed;       //speed of marking mm/s
        public double dPowerRatio;      // power ratio of laser (0-100%)
        public double dCurrent;         //current of laser (A)
        public int nFreq;                  // frequency of laser HZ
        public int nQPulseWidth;           //width of Q pulse (us)
        public int nStartTC;               // Start delay (us)
        public int nLaserOffTC;            //delay before laser off (us)
        public int nEndTC;                 // marking end delay (us)
        public int nPolyTC;                //delay for corner (us)
        public double dJumpSpeed;       //speed of jump without laser (mm/s)
        public int nJumpPosTC;             //delay about jump position (us)
        public int nJumpDistTC;            //delay about the jump Measure (us))
        public double dEndComp;         // compensate for end (mm)
        public double dAccDist;         // Measure of speed up ( mm)
        public double dPointTime;       //delay for point mark (ms)
        public bool bPulsePointMode;   //pulse for point mark mode
        public int nPulseNum;              //the number of pulse
        public double dFlySpeed;        //speed of production line

        public LaserParams()
        {
            nPenNo = 0;                     // Pen’s NO. (0-255)
            nMarkLoop = 0;              //mark times
            dMarkSpeed = 0.0d;       //speed of marking mm/s
            dPowerRatio = 0.0d;      // power ratio of laser (0-100%)
            dCurrent = 0.0d;         //current of laser (A)
            nFreq = 0;                  // frequency of laser HZ
            nQPulseWidth = 0;           //width of Q pulse (us)
            nStartTC = 0;               // Start delay (us)
            nLaserOffTC = 0;            //delay before laser off (us)
            nEndTC = 0;                 // marking end delay (us)
            nPolyTC = 0;                //delay for corner (us)
            dJumpSpeed = 0.0d;       //speed of jump without laser (mm/s)
            nJumpPosTC = 0;             //delay about jump position (us)
            nJumpDistTC = 0;            //delay about the jump Measure (us))
            dEndComp = 0.0d;         // compensate for end (mm)
            dAccDist = 0.0d;         // Measure of speed up ( mm)
            dPointTime = 0.0d;       //delay for pomark (ms)
            bPulsePointMode = false;   //pulse for pomark mode
            nPulseNum = 0;              //the number of pulse
            dFlySpeed = 0.0d;        //speed of production line
        }

        public object Clone()
        {
            LaserParams ret = new LaserParams();
            ret.nPenNo = this.nPenNo;
            ret.nMarkLoop= this.nMarkLoop;
            ret.dMarkSpeed= this.dMarkSpeed;
            ret.dPowerRatio= this.dPowerRatio;
            ret.dCurrent = this.dCurrent;
            ret.nFreq = this.nFreq;
            ret.nQPulseWidth = this.nQPulseWidth;
            ret.nStartTC = this.nStartTC;
            ret.nLaserOffTC = this.nLaserOffTC;
            ret.nEndTC = this.nEndTC;
            ret.nPolyTC = this.nPolyTC;
            ret.dJumpSpeed = this.dJumpSpeed;
            ret.nJumpPosTC = this.nJumpPosTC;
            ret.nJumpDistTC = this.nJumpDistTC;
            ret.dEndComp = this.dEndComp;
            ret.dAccDist = this.dAccDist;
            ret.dPointTime = this.dPointTime;
            ret.bPulsePointMode = this.bPulsePointMode;
            ret.nPulseNum = this.nPulseNum;
            ret.dFlySpeed = this.dFlySpeed;
            return ret;
        }
    }

    public class AddRemarkDataEventArgs : ICloneable
    {
        public List<MarkDataRowView> markList;

        public AddRemarkDataEventArgs()
        {
            markList = new List<MarkDataRowView>();
        }

        public object Clone()
        {
            AddRemarkDataEventArgs ret = new AddRemarkDataEventArgs();
            ret.markList = new List<MarkDataRowView>(this.markList);

            return ret;
        }
    }

    public class PLCChangedEvnetArgs : ICloneable
    {
        public string receiveMsg;

        public PLCChangedEvnetArgs()
        {
            receiveMsg = "";
        }

        public object Clone()
        {
            PLCChangedEvnetArgs ret = new PLCChangedEvnetArgs();
            ret.receiveMsg = this.receiveMsg;
            return ret;
        }

    }

    public class ITNTResponseArgs : ICloneable
    {
        public byte msgtype;        //0=byte,1=string
        public int execResult;
        public int recvType;
        int bufSize;
        public string recvString;
        public byte[] recvBuffer;
        public byte[] recvBuffHex;
        public int recvSize;
        public byte exeCmd;
        public byte stsCmd;
        //public int rawErrorCode;
        //public string sErrorCode;
        //public string sErrorMessage;
        public ErrorInfo errorInfo = new ErrorInfo();

        public ITNTResponseArgs()
        {
            execResult = 0;
            recvType = 0;
            recvString = "";
            recvBuffer = new byte[1024];
            recvBuffHex = new byte[1024];
            bufSize = 1024;
            recvSize = 0;
            exeCmd = 0;
            stsCmd = 0;
            //rawErrorCode = 0;
            //sErrorCode = "";
            //sErrorMessage = "";
            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();
        }

        public ITNTResponseArgs(int bufsize)
        {
            execResult = 0;
            recvType = 0;
            recvString = "";
            recvBuffer = new byte[bufsize];
            recvBuffHex = new byte[bufsize];
            this.bufSize = bufsize;
            recvSize = 0;
            exeCmd = 0;
            stsCmd = 0;
            //rawErrorCode = 0;
            //sErrorCode = "";
            //sErrorMessage = "";
            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();
        }

        public object Clone()
        {
            ITNTResponseArgs ret = new ITNTResponseArgs(this.bufSize);
            ret.execResult = execResult;
            ret.recvType = recvType;
            ret.bufSize = bufSize;
            ret.recvString = recvString;
            if (recvBuffer == null)
                recvBuffer = new byte[this.bufSize];
            Array.Copy(recvBuffer, ret.recvBuffer, bufSize);
            if (recvBuffHex == null)
                recvBuffHex = new byte[this.bufSize];
            Array.Copy(recvBuffHex, ret.recvBuffHex, bufSize);
            ret.recvSize = recvSize;
            ret.exeCmd = exeCmd;
            ret.stsCmd = stsCmd;
            //ret.rawErrorCode = rawErrorCode;
            //ret.sErrorCode = sErrorCode;
            //ret.sErrorMessage = sErrorMessage;

            if(errorInfo == null)
                ret.errorInfo = new ErrorInfo();
            else
                ret.errorInfo = (ErrorInfo)errorInfo.Clone();

            return ret;
        }

        public void Initialize()
        {
            execResult = 0;
            recvType = 0;
            recvString = "";
            if (this.bufSize <= 0)
                this.bufSize = 1024;
            recvBuffer = new byte[this.bufSize];
            recvBuffHex = new byte[this.bufSize];
            recvSize = 0;
            exeCmd = 0;
            stsCmd = 0;

            //rawErrorCode = 0;
            //sErrorCode = "";
            //sErrorMessage = "";
            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();
        }
    }

    public class ITNTSendArgs : ICloneable
    {
        public byte msgType;        //0=byte, 1=string
        public int sendType;
        int bufSize;
        public string sendString;
        public byte[] sendBuffer;
        public int dataSize;
        public int Address;
        public string AddrString;
        public int loglevel;
        public int timeout;
        public int plcDBNum;
        public ITNTSendArgs()
        {
            msgType = 0;
            sendType = 0;
            sendString = "";
            sendBuffer = new byte[1024];
            bufSize = 1024;
            dataSize = 0;
            Address = 0;
            AddrString = "0";
            loglevel = 0;
            timeout = 2;
            plcDBNum = 0;
        }

        public ITNTSendArgs(int bufsize)
        {
            msgType = 0;
            sendType = 0;
            sendString = "";
            sendBuffer = new byte[bufsize];
            dataSize = 0;
            Address = 0;
            AddrString = "0";
            this.bufSize = bufsize;
            loglevel = 0;
            timeout = 2;
            plcDBNum = 0;
        }

        public object Clone()
        {
            ITNTSendArgs ret = new ITNTSendArgs(this.bufSize);
            ret.msgType = msgType;
            ret.sendType = sendType;
            ret.bufSize = bufSize;
            ret.sendString = sendString;
            Array.Copy(sendBuffer, ret.sendBuffer, bufSize);
            ret.dataSize = dataSize;
            ret.Address = Address;
            ret.AddrString = AddrString;
            ret.loglevel = loglevel;
            ret.timeout = timeout;
            ret.plcDBNum = plcDBNum;
            return ret;
        }
    }

    public class PLCDataArrivedEventArgs : ICloneable
    {
        public int recvType;
        public string recvData;
        public int recvSize;

        public PLCDataArrivedEventArgs()
        {
            recvType = 0;
            recvData = "";
            recvSize = 0;
        }

        public object Clone()
        {
            PLCDataArrivedEventArgs ret = new PLCDataArrivedEventArgs();
            ret.recvType = recvType;
            ret.recvData = recvData;
            ret.recvSize = recvSize;
            return ret;
        }
    }

    //public class FontMark : ICloneable
    //{
    //    public string fontName;
    //    public double width;
    //    public double height;
    //    public double pitch;
    //    public double thickness;

    //    public FontMark()
    //    {
    //        fontName = "";
    //        width = 0.0d;
    //        height = 0.0d;
    //        pitch = 0.0d;
    //        thickness = 0.0d;
    //    }

    //    public object Clone()
    //    {
    //        FontMark ret = new FontMark();
    //        ret.fontName = fontName;
    //        ret.width = width;
    //        ret.height = height;
    //        ret.pitch = pitch;
    //        ret.thickness = thickness;
    //        return ret;
    //    }

    //}

    public class MarkControllerRecievedEvnetArgs : ICloneable
    {
        public byte execmd;
        public byte stscmd;
        public string receiveMsg;
        public byte[] receiveBuffer;
        public int receiveSize;

        public MarkControllerRecievedEvnetArgs()
        {
            execmd = 0;
            stscmd = 0;
            receiveMsg = "";
            receiveBuffer = new byte[1024];
            receiveSize = 0;
        }

        public object Clone()
        {
            MarkControllerRecievedEvnetArgs ret = new MarkControllerRecievedEvnetArgs();
            ret.execmd = execmd;
            ret.stscmd = stscmd;
            ret.receiveMsg = this.receiveMsg;
            Array.Copy(receiveBuffer, ret.receiveBuffer, receiveSize);
            ret.receiveSize = receiveSize;
            return ret;
        }

    }


    public class LPMControllerRecievedEvnetArgs : ICloneable
    {
        public char execmd;
        public int value;
        public readonly byte[] Data;


        public LPMControllerRecievedEvnetArgs()
        {
            execmd = ' ';
            value = 0;
            Data = new byte[256];
        }

        public LPMControllerRecievedEvnetArgs(int size)
        {
            execmd = ' ';
            value = 0;
            Data = new byte[size];
        }

        public object Clone()
        {
            LPMControllerRecievedEvnetArgs ret = new LPMControllerRecievedEvnetArgs();
            ret.execmd = execmd;
            ret.value = value;
            Array.Copy(Data, ret.Data, ret.Data.Length);
            return ret;
        }
    }


    public class LaserSourceControllerEvnetArgs : ICloneable
    {
        public int execResult;
        public string laserStatus;
        public string avgPower;
        public string peakPower;
        public string temperature;
        public string laserError;

        //    if ( OutStr.Trim() != "Low" ) ControlWindow.AvgPowerTxt.Text = OutStr;
        //    if ( PeakStr.Trim() != "Low" ) ControlWindow.PeakPowerTxt.Text = PeakStr;
        //    ControlWindow.TemperatureTxt.Text = TempStr;
        //    ControlWindow.LaserErrorTxt.Text = Status.ToString("X");
        //    ControlWindow.LaserErrorTxt.BorderBrush = brushesBorder;
        //    ControlWindow.ProfileTxt.Text = profID[1];
        //public byte execmd;
        //public byte stscmd;
        //public string receiveMsg;
        //public byte[] receiveBuffer;
        //public int receiveSize;

        public LaserSourceControllerEvnetArgs()
        {
            execResult = 0;
            laserStatus = "";
            avgPower = "";
            peakPower = "";
            temperature = "";
            laserError = "";
        }

        public void Initialize()
        {
            execResult = 0;
            laserStatus = "";
            avgPower = "";
            peakPower = "";
            temperature = "";
            laserError = "";
        }

        public object Clone()
        {
            LaserSourceControllerEvnetArgs ret = new LaserSourceControllerEvnetArgs();

            ret.execResult = execResult;
            ret.laserStatus = laserStatus;
            ret.avgPower = avgPower;
            ret.peakPower = peakPower;
            ret.temperature = temperature;
            ret.laserError = laserError;
            return ret;
        }
    }

    public class LaserControllerStatusEvnetArgs : ICloneable
    {
        public int execResult;
        public byte datatype;
        public string recvdata1;
        public string recvdata2;

        public LaserControllerStatusEvnetArgs()
        {
            execResult = 0;
            datatype = 0;
            recvdata1 = "";
            recvdata2 = "";
        }

        public void Initialize()
        {
            execResult = 0;
            datatype = 0;
            recvdata1 = "";
            recvdata2 = "";
        }

        public object Clone()
        {
            LaserControllerStatusEvnetArgs ret = new LaserControllerStatusEvnetArgs();

            ret.execResult = execResult;
            ret.datatype = datatype;
            ret.recvdata1 = recvdata1;
            ret.recvdata2 = recvdata2;
            return ret;
        }
    }


    public class CURRENTMARKDATA : ICloneable
    {
        public bool isReady;
        public MESReceivedData mesData;
        public PatternValueEx pattern;

        public double fontSizeX;
        public double fontSizeY;
        public double shiftValue;
        public string printpost;

        public List<List<FontDataClass>> fontData;
        public FontDataClass[,,] fontDot;
        public byte multiMarkFlag;
        public byte markorderFlag;
        public string sMonthCode;

        public CURRENTMARKDATA()
        {
            isReady = false;
            mesData = new MESReceivedData();
            pattern = new PatternValueEx();

            fontSizeX = 0.0d;
            fontSizeY = 0.0d;
            shiftValue = 0;
            printpost = "";

            fontData = new List<List<FontDataClass>>();
            multiMarkFlag = 0;
            markorderFlag = 0;
            sMonthCode = "";
        }

        public void Initialize()
        {
            isReady = false;
            mesData = new MESReceivedData();
            pattern = new PatternValueEx();

            fontSizeX = 0.0d;
            fontSizeY = 0.0d;
            shiftValue = 0;
            printpost = "";

            fontData = new List<List<FontDataClass>>();
            multiMarkFlag = 0;
            markorderFlag = 0;

            sMonthCode = "";
        }

        public object Clone()
        {
            CURRENTMARKDATA ret = new CURRENTMARKDATA();
            ret.isReady = isReady;
            ret.mesData = (MESReceivedData)mesData.Clone();
            ret.pattern = (PatternValueEx)pattern.Clone();

            ret.fontSizeX = fontSizeX;
            ret.fontSizeY = fontSizeY;
            ret.shiftValue = shiftValue;
            ret.printpost = printpost;

            ret.fontData = fontData.ToList();

            ret.multiMarkFlag = multiMarkFlag;
            ret.markorderFlag = markorderFlag;

            int ileng = fontDot.GetLength(0);
            int jleng = fontDot.GetLength(1);
            int kleng = fontDot.GetLength(2);

            for (int i = 0; i < ileng; i++)
            {
                for (int j = 0; j < jleng; j++)
                {
                    for (int k = 0; k < kleng; k++)
                    {
                        ret.fontDot[i, j, k] = new FontDataClass();
                        ret.fontDot[i, j, k] = (FontDataClass)fontDot[i, j, k].Clone();
                    }
                }
            }
            ret.sMonthCode = sMonthCode;
            return ret;
        }
    }



    public class MarkVINInformEx : ICloneable
    {
        //public bool isReady;
        //public MESReceivedData mesData;
        //public PatternValueEx pattern;

        //public double fontSizeX;
        //public double fontSizeY;
        //public double shiftValue;
        //public string printpost;

        //public List<List<FontDataClass>> fontData;

        public CURRENTMARKDATA currMarkData;

        public DistanceData distance;

        public CheckAreaData checkdata;
        public MarkSendDataLaser senddata;
        ////public FontDataClass[,,] fontDot;
        //public FontDataClass[,,] fontDot;

        public MarkVINInformEx()
        {
            currMarkData = new CURRENTMARKDATA();

            //isReady = false;
            //mesData = new MESReceivedData();
            //pattern = new PatternValueEx();
            //fontSizeX = 0.0d;
            //fontSizeY = 0.0d;
            //shiftValue = 0;
            //printpost = "";
            //fontData = new List<List<FontDataClass>>();

            distance = new DistanceData();
            checkdata = new CheckAreaData();
            senddata = new MarkSendDataLaser();
        }

        public void Initialize()
        {
            currMarkData = new CURRENTMARKDATA();

            //isReady = false;
            //mesData = new MESReceivedData();
            //pattern = new PatternValueEx();
            //fontSizeX = 0.0d;
            //fontSizeY = 0.0d;
            //shiftValue = 0;
            //printpost = "";
            //fontData = new List<List<FontDataClass>>();

            distance = new DistanceData();
            checkdata.Clear();
            senddata.Clear();
        }

        public object Clone()
        {
            MarkVINInformEx ret = new MarkVINInformEx();

            ret.currMarkData = (CURRENTMARKDATA)currMarkData.Clone();
            //ret.isReady = isReady;
            //ret.mesData = (MESReceivedData)mesData.Clone();
            //ret.pattern = (PatternValueEx)pattern.Clone();
            //ret.fontData = fontData.ToList();
            //ret.fontSizeX = fontSizeX;
            //ret.fontSizeY = fontSizeY;
            //ret.shiftValue = shiftValue;
            //ret.printpost = printpost;

            ret.distance = (DistanceData)distance.Clone();
            ret.checkdata = (CheckAreaData)checkdata.Clone();
            ret.senddata = (MarkSendDataLaser)senddata.Clone();

            return ret;
        }
    }

    public class StateObject
    {
        // Client socket.  
        public Socket workSocket;
        // Size of receive buffer.  
        public int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer;
        // Received data string.  
        //public StringBuilder sb;

        public StateObject(int size = 0)
        {
            workSocket = null;
            if (size <= 0)
                BufferSize = 1024;
            else
                BufferSize = size;
            buffer = new byte[BufferSize];
            //sb = new StringBuilder();
        }

        //public struct m_font  // TEXT
        //{
        //    public byte cN, fN;
        //    public UInt16 mX, mY, mZ;
        //    public byte mF;
        //}

        public class M_FONT
        {
            public byte cN, fN;
            public UInt16 mX, mY, mZ;
            public byte mF;
            public M_FONT()
            {
                cN = fN = 0;
                mX = mY = mZ = 0;
                mF = 0;
            }
        }
    }

    public struct MPOINT // TEXT
    {
        public Double X;
        public Double Y;
        public Double Z;
        //public Double S;
    }



    public class FontValue : ICloneable
    {
        public string fontName;
        public double width;
        public double height;
        public double pitch;
        public double thickness;
        public double rotateAngle;
        public short strikeCount;

        public FontValue()
        {
            fontName = "";
            width = 0;
            height = 0;
            pitch = 0;
            thickness = 0;
            rotateAngle = 0;
            strikeCount = 0;
        }

        public object Clone()
        {
            FontValue ret = new FontValue();
            ret.fontName = fontName;
            ret.width = width;
            ret.height = height;
            ret.pitch = pitch;
            ret.thickness = thickness;
            ret.rotateAngle = rotateAngle;
            ret.strikeCount = strikeCount;

            return ret;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(FontValue a, FontValue b)
        {
            if (a.fontName != b.fontName)
                return false;
            if (a.width != b.width)
                return false;
            if (a.height != b.height)
                return false;
            if (a.pitch != b.pitch)
                return false;
            if (a.thickness != b.thickness)
                return false;
            if (a.rotateAngle != b.rotateAngle)
                return false;
            if (a.strikeCount != b.strikeCount)
                return false;

            return true;
        }

        public static bool operator !=(FontValue a, FontValue b)
        {
            if (a.fontName != b.fontName)
                return true;
            if (a.width != b.width)
                return true;
            if (a.height != b.height)
                return true;
            if (a.pitch != b.pitch)
                return true;
            if (a.thickness != b.thickness)
                return true;
            if (a.rotateAngle != b.rotateAngle)
                return true;
            if (a.strikeCount != b.strikeCount)
                return true;

            return false;
        }
    }


    public class HeadValue : ICloneable
    {
        public short max_X;
        public short max_Y;
        public short max_Z;
        public short opmode;
        public short stepLength;
        public double angleDegree;
        public byte sensorPosition;
        public byte spatterType;

        public Vector3D park3DPos;
        public Vector3D home3DPos;

        public double rasterSP;
        public double rasterEP;

        public double distance0Position;
        public short markDelayTime1;
        public short markDelayTime2;

        public byte bySkipPlateCheck;

        public double slope;
        public double slope4Manual;

        public HeadValue()
        {
            max_X = 0;
            max_Y = 0;
            max_Z = 0;
            opmode = 0;
            stepLength = 0;
            angleDegree = 0;
            sensorPosition = 0;
            spatterType = 0;

            park3DPos = new Vector3D();
            home3DPos = new Vector3D();

            rasterSP = 0;
            rasterEP = 0;

            distance0Position = 0.0;

            markDelayTime1 = 0;
            markDelayTime2 = 0;
            bySkipPlateCheck = 0;

            slope = 0;
            slope4Manual = 0;
        }

        public object Clone()
        {
            HeadValue ret = new HeadValue();
            ret.max_X = max_X;
            ret.max_Y = max_Y;
            ret.max_Z = max_Z;
            ret.opmode = opmode;
            ret.stepLength = stepLength;
            ret.angleDegree = angleDegree;
            ret.sensorPosition = sensorPosition;
            ret.spatterType = spatterType;

            ret.park3DPos = park3DPos;
            ret.home3DPos = home3DPos;

            ret.rasterSP = rasterSP;
            ret.rasterEP = rasterEP;

            ret.distance0Position = distance0Position;

            ret.markDelayTime1 = markDelayTime1;
            ret.markDelayTime2 = markDelayTime2;

            ret.bySkipPlateCheck = bySkipPlateCheck;

            ret.slope = slope;
            ret.slope4Manual = slope4Manual;

            return ret;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(HeadValue a, HeadValue b)
        {
            if (a.max_X != b.max_X)
                return false;
            if (a.max_Y != b.max_Y)
                return false;
            if (a.max_Z != b.max_Z)
                return false;
            if (a.opmode != b.opmode)
                return false;
            if (a.stepLength != b.stepLength)
                return false;
            if (a.angleDegree != b.angleDegree)
                return false;
            if(a.sensorPosition != b.sensorPosition)
                return false;
            if (a.spatterType != b.spatterType)
                return false;

            if (a.park3DPos != b.park3DPos)
                return false;
            if (a.home3DPos != b.home3DPos)
                return false;
            if (a.rasterSP != b.rasterSP)
                return false;
            if (a.rasterEP != b.rasterEP)
                return false;
            if (a.distance0Position != b.distance0Position)
                return false;

            if (a.markDelayTime1 != b.markDelayTime1)
                return false;
            if (a.markDelayTime2 != b.markDelayTime2)
                return false;

            if (a.bySkipPlateCheck != b.bySkipPlateCheck)
                return false;

            if (a.slope != b.slope)
                return false;
            if (a.slope4Manual != b.slope4Manual)
                return false;

            return true;
        }

        public static bool operator !=(HeadValue a, HeadValue b)
        {
            if (a.max_X != b.max_X)
                return true;
            if (a.max_Y != b.max_Y)
                return true;
            if (a.max_Z != b.max_Z)
                return true;
            if (a.opmode != b.opmode)
                return true;
            if (a.stepLength != b.stepLength)
                return true;
            if (a.angleDegree != b.angleDegree)
                return true;
            if (a.sensorPosition != b.sensorPosition)
                return true;
            if (a.spatterType != b.spatterType)
                return true;

            if (a.park3DPos != b.park3DPos)
                return true;
            if (a.home3DPos != b.home3DPos)
                return true;
            if (a.rasterSP != b.rasterSP)
                return true;
            if (a.rasterEP != b.rasterEP)
                return true;
            if (a.distance0Position != b.distance0Position)
                return true;

            if (a.markDelayTime1 != b.markDelayTime1)
                return true;
            if (a.markDelayTime2 != b.markDelayTime2)
                return true;

            if (a.bySkipPlateCheck != b.bySkipPlateCheck)
                return true;

            if (a.slope != b.slope)
                return true;
            if (a.slope4Manual != b.slope4Manual)
                return true;

            return false;
        }
    }


    public class SpeedValue : ICloneable
    {
        //Fast Moving
        public short initSpeed4Fast;
        public short targetSpeed4Fast;
        public short accelSpeed4Fast;
        public short decelSpeed4Fast;
        //Marking Speed (Raster)
        public short initSpeed4MarkR;
        public short targetSpeed4MarkR;
        public short accelSpeed4MarkR;
        public short decelSpeed4MarkR;
        //Marking Speed (Vector)
        public short initSpeed4MarkV;
        public short targetSpeed4MarkV;
        public short accelSpeed4MarkV;
        public short decelSpeed4MarkV;
        //Move Home 
        public short initSpeed4Home;
        public short targetSpeed4Home;
        public short accelSpeed4Home;
        public short decelSpeed4Home;
        //Measure Sensor Check
        public short initSpeed4Measure;
        public short targetSpeed4Measure;
        public short accelSpeed4Measure;
        public short decelSpeed4Measure;

        public short initSpeed4Clean;
        public short targetSpeed4Clean;
        public short accelSpeed4Clean;
        public short decelSpeed4Clean;
        //Solenoid
        public short solOnTime;
        public short solOffTime;
        
        //dwellTime
        public short dwellTime;

        public SpeedValue()
        {
            initSpeed4Fast = 0;
            targetSpeed4Fast = 0;
            accelSpeed4Fast = 0;
            decelSpeed4Fast = 0;

            initSpeed4MarkR = 0;
            targetSpeed4MarkR = 0;
            accelSpeed4MarkR = 0;
            decelSpeed4MarkR = 0;

            initSpeed4MarkV = 0;
            targetSpeed4MarkV = 0;
            accelSpeed4MarkV = 0;
            decelSpeed4MarkV = 0;

            initSpeed4Home = 0;
            targetSpeed4Home = 0;
            accelSpeed4Home = 0;
            decelSpeed4Home = 0;

            initSpeed4Measure = 0;
            targetSpeed4Measure = 0;
            accelSpeed4Measure = 0;
            decelSpeed4Measure = 0;

            initSpeed4Clean = 0;
            targetSpeed4Clean = 0;
            accelSpeed4Clean = 0;
            decelSpeed4Clean = 0;

            solOnTime = 0;
            solOffTime = 0;

            dwellTime = 0;
        }

        public object Clone()
        {
            SpeedValue ret = new SpeedValue();

            ret.initSpeed4Fast = initSpeed4Fast;
            ret.targetSpeed4Fast = targetSpeed4Fast;
            ret.accelSpeed4Fast = accelSpeed4Fast;
            ret.decelSpeed4Fast = decelSpeed4Fast;

            ret.initSpeed4MarkR = initSpeed4MarkR;
            ret.targetSpeed4MarkR = targetSpeed4MarkR;
            ret.accelSpeed4MarkR = accelSpeed4MarkR;
            ret.decelSpeed4MarkR = decelSpeed4MarkR;

            ret.initSpeed4MarkV = initSpeed4MarkV;
            ret.targetSpeed4MarkV = targetSpeed4MarkV;
            ret.accelSpeed4MarkV = accelSpeed4MarkV;
            ret.decelSpeed4MarkV = decelSpeed4MarkV;

            ret.initSpeed4Home = initSpeed4Home;
            ret.targetSpeed4Home = targetSpeed4Home;
            ret.accelSpeed4Home = accelSpeed4Home;
            ret.decelSpeed4Home = decelSpeed4Home;

            ret.initSpeed4Measure = initSpeed4Measure;
            ret.targetSpeed4Measure = targetSpeed4Measure;
            ret.accelSpeed4Measure = accelSpeed4Measure;
            ret.decelSpeed4Measure = decelSpeed4Measure;

            ret.initSpeed4Clean = initSpeed4Clean;
            ret.targetSpeed4Clean = targetSpeed4Clean;
            ret.accelSpeed4Clean = accelSpeed4Clean;
            ret.decelSpeed4Clean = decelSpeed4Clean;

            ret.solOnTime = solOnTime;
            ret.solOffTime = solOffTime;

            ret.dwellTime = dwellTime;

            return ret;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(SpeedValue a, SpeedValue b)
        {
            if (a.initSpeed4Fast != b.initSpeed4Fast)
                return false;
            if (a.targetSpeed4Fast != b.targetSpeed4Fast)
                return false;
            if (a.accelSpeed4Fast != b.accelSpeed4Fast)
                return false;
            if (a.decelSpeed4Fast != b.decelSpeed4Fast)
                return false;
            if (a.initSpeed4MarkR != b.initSpeed4MarkR)
                return false;
            if (a.targetSpeed4MarkR != b.targetSpeed4MarkR)
                return false;
            if (a.accelSpeed4MarkR != b.accelSpeed4MarkR)
                return false;
            if (a.decelSpeed4MarkR != b.decelSpeed4MarkR)
                return false;
            if (a.initSpeed4MarkV != b.initSpeed4MarkV)
                return false;
            if (a.targetSpeed4MarkV != b.targetSpeed4MarkV)
                return false;
            if (a.accelSpeed4MarkV != b.accelSpeed4MarkV)
                return false;
            if (a.decelSpeed4MarkV != b.decelSpeed4MarkV)
                return false;
            if (a.initSpeed4Home != b.initSpeed4Home)
                return false;
            if (a.targetSpeed4Home != b.targetSpeed4Home)
                return false;
            if (a.accelSpeed4Home != b.accelSpeed4Home)
                return false;
            if (a.decelSpeed4Home != b.decelSpeed4Home)
                return false;
            if (a.initSpeed4Measure != b.initSpeed4Measure)
                return false;
            if (a.targetSpeed4Measure != b.targetSpeed4Measure)
                return false;
            if (a.accelSpeed4Measure != b.accelSpeed4Measure)
                return false;
            if (a.decelSpeed4Measure != b.decelSpeed4Measure)
                return false;
            if (a.initSpeed4Clean != b.initSpeed4Clean)
                return false;
            if (a.targetSpeed4Clean != b.targetSpeed4Clean)
                return false;
            if (a.accelSpeed4Clean != b.accelSpeed4Clean)
                return false;
            if (a.decelSpeed4Clean != b.decelSpeed4Clean)
                return false;
            if (a.solOnTime != b.solOnTime)
                return false;
            if (a.solOffTime != b.solOffTime)
                return false;
            if (a.dwellTime != b.dwellTime)
                return false;

            return true;
        }

        public static bool operator !=(SpeedValue a, SpeedValue b)
        {
            if (a.initSpeed4Fast != b.initSpeed4Fast)
                return true;
            if (a.targetSpeed4Fast != b.targetSpeed4Fast)
                return true;
            if (a.accelSpeed4Fast != b.accelSpeed4Fast)
                return true;
            if (a.decelSpeed4Fast != b.decelSpeed4Fast)
                return true;
            if (a.initSpeed4MarkR != b.initSpeed4MarkR)
                return true;
            if (a.targetSpeed4MarkR != b.targetSpeed4MarkR)
                return true;
            if (a.accelSpeed4MarkR != b.accelSpeed4MarkR)
                return true;
            if (a.decelSpeed4MarkR != b.decelSpeed4MarkR)
                return true;
            if (a.initSpeed4MarkV != b.initSpeed4MarkV)
                return true;
            if (a.targetSpeed4MarkV != b.targetSpeed4MarkV)
                return true;
            if (a.accelSpeed4MarkV != b.accelSpeed4MarkV)
                return true;
            if (a.decelSpeed4MarkV != b.decelSpeed4MarkV)
                return true;
            if (a.initSpeed4Home != b.initSpeed4Home)
                return true;
            if (a.targetSpeed4Home != b.targetSpeed4Home)
                return true;
            if (a.accelSpeed4Home != b.accelSpeed4Home)
                return true;
            if (a.decelSpeed4Home != b.decelSpeed4Home)
                return true;
            if (a.initSpeed4Measure != b.initSpeed4Measure)
                return true;
            if (a.targetSpeed4Measure != b.targetSpeed4Measure)
                return true;
            if (a.accelSpeed4Measure != b.accelSpeed4Measure)
                return true;
            if (a.decelSpeed4Measure != b.decelSpeed4Measure)
                return true;
            if (a.initSpeed4Clean != b.initSpeed4Clean)
                return true;
            if (a.targetSpeed4Clean != b.targetSpeed4Clean)
                return true;
            if (a.accelSpeed4Clean != b.accelSpeed4Clean)
                return true;
            if (a.decelSpeed4Clean != b.decelSpeed4Clean)
                return true;
            if (a.solOnTime != b.solOnTime)
                return true;
            if (a.solOffTime != b.solOffTime)
                return true;
            if (a.dwellTime != b.dwellTime)
                return true;

            return false;
        }
    }

    public class PositionValue : ICloneable
    {
        public Vector3D center3DPos;
        public double checkDistanceHeight;
        public double teachingZHeight;
        public byte plateMode;
        //public double cleaningHeight;

        public PositionValue()
        {
            center3DPos = new Vector3D();
            checkDistanceHeight = 0.0;
            teachingZHeight = 0.0;
            plateMode = 0;
            //cleaningHeight = 0;
        }

        public object Clone()
        {
            PositionValue ret = new PositionValue();

            ret.center3DPos = center3DPos;
            ret.checkDistanceHeight = checkDistanceHeight;
            ret.teachingZHeight = teachingZHeight;
            ret.plateMode = plateMode;
            //ret.cleaningHeight = cleaningHeight;
            return ret;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(PositionValue a, PositionValue b)
        {
            if (a.center3DPos != b.center3DPos)
                return false;
            if (a.checkDistanceHeight != b.checkDistanceHeight)
                return false;
            if (a.teachingZHeight != b.teachingZHeight)
                return false;
            if (a.plateMode != b.plateMode)
                return false;
            //if(a.cleaningHeight != b.cleaningHeight)
            //    return false;

            return true;
        }

        public static bool operator !=(PositionValue a, PositionValue b)
        {
            if (a.center3DPos != b.center3DPos)
                return true;
            if (a.checkDistanceHeight != b.checkDistanceHeight)
                return true;
            if (a.teachingZHeight != b.teachingZHeight)
                return true;
            if (a.plateMode != b.plateMode)
                return true;
            //if (a.cleaningHeight != b.cleaningHeight)
            //    return true;

            return false;
        }
    }

    public class LaserValue : ICloneable
    {
        public short waveformNum;
        public short waveformClean;
        //public Single phaseComp;
        public string sPhaseComp;
        public double density;
        public double cleanPosition;
        public double cleanDelta;
        public int charClean;
        public int combineFireClean;
        public char charFull;
        public short useCleaning;
        public string markPower;
        public string markWidth;
        public string cleanPower;
        public string cleanWidth;
        public string platePower;
        public string plateWidth;
        public string spotPower;
        public string spotWidth;

        public LaserValue()
        {
            waveformNum = 0;
            waveformClean = 0;
            //phaseComp = 0;
            sPhaseComp = "0";
            density = 0;
            cleanPosition = 0;
            cleanDelta = 0;
            charClean = 0;
            combineFireClean = 0;
            charFull = ':';
            useCleaning = 0;

            markPower = "";
            markWidth = "";
            cleanPower = "";
            cleanWidth = "";
            platePower = "";
            plateWidth = "";
            spotPower = "";
            spotWidth = "";
        }

        public object Clone()
        {
            LaserValue ret = new LaserValue();
            ret.waveformNum = waveformNum;
            ret.waveformClean = waveformClean;
            //ret.phaseComp = phaseComp;
            ret.sPhaseComp = sPhaseComp;
            ret.cleanPosition = cleanPosition;
            ret.density = density;
            ret.cleanDelta = cleanDelta;
            ret.charClean = charClean;
            ret.combineFireClean = combineFireClean;
            ret.charFull = charFull;
            ret.useCleaning = useCleaning;

            ret.markPower = markPower;
            ret.markWidth = markWidth;
            ret.cleanPower = cleanPower;
            ret.cleanWidth = cleanWidth;
            ret.platePower = platePower;
            ret.plateWidth = plateWidth;
            ret.spotPower = spotPower;
            ret.spotWidth = spotWidth;

            return ret;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(LaserValue a, LaserValue b)
        {
            if (a.waveformNum != b.waveformNum)
                return false;
            if (a.waveformClean != b.waveformClean)
                return false;
            //if (a.phaseComp != b.phaseComp)
            //    return false;
            if (a.sPhaseComp != b.sPhaseComp)
                return false;
            if (a.cleanPosition != b.cleanPosition)
                return false;
            if (a.density != b.density)
                return false;
            if (a.cleanDelta != b.cleanDelta)
                return false;

            if (a.charClean != b.charClean)
                return false;
            if (a.combineFireClean != b.combineFireClean)
                return false;
            if (a.charFull != b.charFull)
                return false;


            if (a.useCleaning != b.useCleaning)
                return false;
            return true;
        }

        public static bool operator !=(LaserValue a, LaserValue b)
        {
            if (a.waveformNum != b.waveformNum)
                return true;
            if (a.waveformClean != b.waveformClean)
                return true;
            //if (a.phaseComp != b.phaseComp)
            //    return true;
            if (a.sPhaseComp != b.sPhaseComp)
                return true;
            if (a.cleanPosition != b.cleanPosition)
                return true;
            if (a.density != b.density)
                return true;
            if (a.cleanDelta != b.cleanDelta)
                return true;

            if (a.charClean != b.charClean)
                return true;
            if (a.combineFireClean != b.combineFireClean)
                return true;
            if (a.charFull != b.charFull)
                return true;


            if (a.useCleaning != b.useCleaning)
                return true;
            return false;
        }
    }


    public class ScanValue : ICloneable
    {
        //Scanner
        public double startU;
        public double scanLen;
        public double parkingU;
        public short max_U;
        public double home_U;
        public double linkPos;

        //step length
        public short stepLength_U;

        //scan 
        public short initSpeed4Scan;
        public short targetSpeed4Scan;
        public short accelSpeed4Scan;
        public short decelSpeed4Scan;

        //scan free 
        public short initSpeed4ScanFree;
        public short targetSpeed4ScanFree;
        public short accelSpeed4ScanFree;
        public short decelSpeed4ScanFree;
        public byte reverseScan;

        public ScanValue()
        {
            startU = 0;
            scanLen = 0;
            parkingU = 0;
            max_U = 0;
            home_U = 0;
            stepLength_U = 0;
            linkPos = 0;

            initSpeed4Scan = 27;
            targetSpeed4Scan = 37;
            accelSpeed4Scan = 10;
            decelSpeed4Scan = 10;

            initSpeed4ScanFree = 0;
            targetSpeed4ScanFree = 0;
            accelSpeed4ScanFree = 0;
            decelSpeed4ScanFree = 0;

            reverseScan = 0;
        }

        public object Clone()
        {
            ScanValue ret = new ScanValue();
            ret.startU = startU;
            ret.scanLen = scanLen;
            ret.parkingU = parkingU;
            ret.max_U = max_U;
            ret.home_U = home_U;
            ret.stepLength_U = stepLength_U;
            ret.linkPos = linkPos;

            ret.initSpeed4Scan = initSpeed4Scan;
            ret.targetSpeed4Scan = targetSpeed4Scan;
            ret.accelSpeed4Scan = accelSpeed4Scan;
            ret.decelSpeed4Scan = decelSpeed4Scan;

            ret.initSpeed4ScanFree = initSpeed4ScanFree;
            ret.targetSpeed4ScanFree = targetSpeed4ScanFree;
            ret.accelSpeed4ScanFree = accelSpeed4ScanFree;
            ret.decelSpeed4ScanFree = decelSpeed4ScanFree;
            ret.reverseScan = reverseScan;
            return ret;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(ScanValue a, ScanValue b)
        {
            if (a.startU != b.startU)
                return false;
            if (a.scanLen != b.scanLen)
                return false;
            if (a.parkingU != b.parkingU)
                return false;
            if (a.max_U != b.max_U)
                return false;
            if (a.home_U != b.home_U)
                return false;
            if (a.stepLength_U != b.stepLength_U)
                return false;
            if(a.linkPos != b.linkPos)
                return false;

            if (a.initSpeed4Scan != b.initSpeed4Scan)
                return false;
            if (a.targetSpeed4Scan != b.targetSpeed4Scan)
                return false;
            if (a.accelSpeed4Scan != b.accelSpeed4Scan)
                return false;
            if (a.decelSpeed4Scan != b.decelSpeed4Scan)
                return false;

            if (a.initSpeed4ScanFree != b.initSpeed4ScanFree)
                return false;
            if (a.targetSpeed4ScanFree != b.targetSpeed4ScanFree)
                return false;
            if (a.accelSpeed4ScanFree != b.accelSpeed4ScanFree)
                return false;
            if (a.decelSpeed4ScanFree != b.decelSpeed4ScanFree)
                return false;
            if (a.reverseScan != b.reverseScan)
                return false;
            return true;
        }

        public static bool operator !=(ScanValue a, ScanValue b)
        {
            if (a.startU != b.startU)
                return true;
            if (a.scanLen != b.scanLen)
                return true;
            if (a.parkingU != b.parkingU)
                return true;
            if (a.max_U != b.max_U)
                return true;
            if (a.home_U != b.home_U)
                return true;
            if (a.stepLength_U != b.stepLength_U)
                return true;
            if (a.linkPos != b.linkPos)
                return true;

            if (a.initSpeed4Scan != b.initSpeed4Scan)
                return true;
            if (a.targetSpeed4Scan != b.targetSpeed4Scan)
                return true;
            if (a.accelSpeed4Scan != b.accelSpeed4Scan)
                return true;
            if (a.decelSpeed4Scan != b.decelSpeed4Scan)
                return true;

            if (a.initSpeed4ScanFree != b.initSpeed4ScanFree)
                return true;
            if (a.targetSpeed4ScanFree != b.targetSpeed4ScanFree)
                return true;
            if (a.accelSpeed4ScanFree != b.accelSpeed4ScanFree)
                return true;
            if (a.decelSpeed4ScanFree != b.decelSpeed4ScanFree)
                return true;
            if (a.reverseScan != b.reverseScan)
                return true;
            return false;
        }
    }

    public class PatternValueEx : ICloneable
    {
        public string name;

        public FontValue fontValue;
        public HeadValue headValue;
        public SpeedValue speedValue;
        public PositionValue positionValue;
        public LaserValue laserValue;
        public ScanValue scanValue;

        public PatternValueEx()
        {
            name = "";
            fontValue = new FontValue();
            headValue = new HeadValue();
            speedValue = new SpeedValue();
            positionValue = new PositionValue();
            laserValue = new LaserValue();
            scanValue = new ScanValue();
        }

        public object Clone()
        {
            PatternValueEx ret = new PatternValueEx();
            ret.name = name;
            ret.fontValue = (FontValue)fontValue.Clone();
            ret.headValue = (HeadValue)headValue.Clone();
            ret.speedValue = (SpeedValue)speedValue.Clone();
            ret.positionValue = (PositionValue)positionValue.Clone();
            ret.laserValue = (LaserValue)laserValue.Clone();
            ret.scanValue = (ScanValue)scanValue.Clone();

            return ret;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(PatternValueEx a, PatternValueEx b)
        {
            if (a.name != b.name)
                return false;
            if (a.fontValue != b.fontValue)
                return false;
            if (a.headValue != b.headValue)
                return false;
            if (a.speedValue != b.speedValue)
                return false;
            if (a.positionValue != b.positionValue)
                return false;
            if (a.laserValue != b.laserValue)
                return false;
            if (a.scanValue != b.scanValue)
                return false;

            return true;
        }

        public static bool operator !=(PatternValueEx a, PatternValueEx b)
        {
            if (a.name != b.name)
                return true;
            if (a.fontValue != b.fontValue)
                return true;
            if (a.headValue != b.headValue)
                return true;
            if (a.speedValue != b.speedValue)
                return true;
            if (a.positionValue != b.positionValue)
                return true;
            if (a.laserValue != b.laserValue)
                return true;
            if (a.scanValue != b.scanValue)
                return true;

            return false;
        }
    }


    public class distanceSensorData :ICloneable
    {
        public int execResult;
        //public string sMeasure;
        public double rawdistance;
        public double sensorshift;
        public double sensoroffset;
        //public string sMeasure2;
        //public double sensorshift2;
        //public double sensoroffset2;
        public ErrorInfo errorInfo = new ErrorInfo();

        public distanceSensorData()
        {
            execResult = 0;
            //sMeasure = "";
            rawdistance = 0;
            sensorshift = 0.0d;
            sensoroffset = 0.0d;
            //sMeasure2 = "";
            //sensorshift2 = 0.0d;
            //sensoroffset2 = 0.0d;
            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();
        }

        public object Clone()
        {
            distanceSensorData ret = new distanceSensorData();
            ret.execResult = execResult;
            //ret.sMeasure = sMeasure;
            ret.rawdistance = rawdistance;
            ret.sensorshift = sensoroffset;
            ret.sensoroffset = sensoroffset;
            //ret.sMeasure2 = sMeasure2;
            //ret.sensorshift2 = sensorshift2;
            //ret.sensoroffset2 = sensoroffset2;
            if (errorInfo == null)
                ret.errorInfo = new ErrorInfo();
            else
                ret.errorInfo = (ErrorInfo)errorInfo.Clone();

            return ret;
        }

        public void Clear()
        {
            this.execResult = 0;
            //this.sMeasure = "";
            this.rawdistance = 0;
            this.sensorshift = 0.0d;
            this.sensoroffset = 0.0d;
            //sMeasure2 = "";
            //sensorshift2 = 0.0d;
            //sensoroffset2 = 0.0d;
            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();
        }
    }

    public class MeasureHeightData : ICloneable
    {
        public int execResult;
        public double HeightLU;
        public double HeightLD;
        public double HeightRU;
        public double HeightRD;
        public double HeightCT;
        public double HeightCD;

        public MeasureHeightData()
        {
            execResult = 0;
            HeightLU = 0.0d;
            HeightLD = 0.0d;
            HeightRU = 0.0d;
            HeightRD = 0.0d;
            HeightCT = 0.0d;
            HeightCD = 0.0d;
        }

        public object Clone()
        {
            MeasureHeightData ret = new MeasureHeightData();
            ret.execResult = execResult;
            ret.HeightLU = HeightLU;
            ret.HeightLD = HeightLD;
            ret.HeightRU = HeightRU;
            ret.HeightRD = HeightRD;
            return ret;
        }
    }

    public class CheckAreaData : ICloneable
    {
        public int execResult;
        public bool bReady;
        public double PlaneCenterZ;
        public bool ErrorDistanceSensor;
        public Vector3D NormalDir;
        public bool TwoLineDisplay;
        //public int rawErrorCode;
        //public string sErrorMessage;
        public Vector3D centerPoint;
        public Vector3D centerPointBLU;
        public double[] checkdistance = new double[10];
        public ErrorInfo errorInfo = new ErrorInfo();

        public CheckAreaData()
        {
            execResult = 0;
            bReady = false;
            PlaneCenterZ = 0;

            ErrorDistanceSensor = false;
            NormalDir = new Vector3D();

            TwoLineDisplay = false;
            //rawErrorCode = 0;
            //sErrorMessage = "";
            centerPoint = new Vector3D();
            centerPointBLU = new Vector3D();

            checkdistance = new double[10];
            for (int i = 0; i < checkdistance.Length; i++)
                checkdistance[i] = 0;

            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();
        }

        public void Clear()
        {
            execResult = 0;
            bReady = false;
            PlaneCenterZ = 0;

            ErrorDistanceSensor = false;
            NormalDir = new Vector3D();

            TwoLineDisplay = false;
            //rawErrorCode = 0;
            //sErrorMessage = "";
            centerPoint = new Vector3D();
            centerPointBLU = new Vector3D();

            for (int i = 0; i < checkdistance.Length; i++)
                checkdistance[i] = 0;

            if (errorInfo == null)
                errorInfo = new ErrorInfo();
            else
                errorInfo.Clear();
        }

        public object Clone()
        {
            CheckAreaData ret = new CheckAreaData();
            ret.execResult = execResult;
            ret.bReady = bReady;
            ret.PlaneCenterZ = PlaneCenterZ;

            ret.ErrorDistanceSensor = ErrorDistanceSensor;
            ret.NormalDir = NormalDir;

            ret.TwoLineDisplay = TwoLineDisplay;
            //ret.rawErrorCode = rawErrorCode;
            //ret.sErrorMessage = sErrorMessage;

            ret.centerPoint = centerPoint;
            ret.centerPointBLU = centerPointBLU;

            for (int i = 0; i < checkdistance.Length; i++)
                ret.checkdistance[i] = checkdistance[i];

            if (errorInfo == null)
                ret.errorInfo = new ErrorInfo();
            else
                ret.errorInfo = (ErrorInfo)errorInfo.Clone();

            return ret;
        }
    }


    public class MarkSendDataLaser : ICloneable
    {
        public int execResult;
        public bool bReady;

        //public short SendDataIndex;
        //public short SendDataCount;
        public bool CleanFireFlag;
        //public double PlaneCenterZ;
        //public MPOINT normalDir;
        //public FontDataLaser normalDir;
        //public FontDataClass fontdata;
        //public bool ErrorDistanceSensor;
        //public List<Vector3D> planePoints;
        //public Vector3D NormalDir;
        public List<string> sendDataFire;
        public List<string> sendDataClean;
        //public bool TwoLineDisplay;

        public MarkSendDataLaser()
        {
            execResult = -1;
            bReady = false;
            //SendDataIndex = 0;
            //SendDataCount = 0;
            //EndOfSend = false;
            //CleanFireFlag = false;
            //PlaneCenterZ = 0;
            //fontdata = new FontDataClass();

            //ErrorDistanceSensor = false;
            //planePoints = new List<Vector3D>();
            //NormalDir = new Vector3D();

            sendDataFire = new List<string>();
            sendDataClean = new List<string>();

            //TwoLineDisplay = false;
        }

        public void Clear()
        {
            execResult = -1;
            bReady = false;
            //SendDataIndex = 0;
            //SendDataCount = 0;
            //EndOfSend = false;
            CleanFireFlag = false;
            //PlaneCenterZ = 0;
            //fontdata = new FontDataClass();

            //ErrorDistanceSensor = false;
            //planePoints = new List<Vector3D>();
            //NormalDir = new Vector3D();

            sendDataFire = new List<string>();
            sendDataClean = new List<string>();

            //TwoLineDisplay = false;
        }

        public object Clone()
        {
            MarkSendDataLaser ret = new MarkSendDataLaser();
            ret.execResult = execResult;
            ret.bReady = bReady;
            //ret.SendDataIndex = SendDataIndex;
            //ret.SendDataCount = SendDataCount;
            //ret.EndOfSend = EndOfSend;
            ret.CleanFireFlag = CleanFireFlag;
            //ret.PlaneCenterZ = PlaneCenterZ;
            //ret.fontdata = fontdata;

            //ret.ErrorDistanceSensor = ErrorDistanceSensor;
            //ret.planePoints = planePoints.ToList();
            //ret.NormalDir = NormalDir;
            ret.sendDataFire = sendDataFire.ToList();
            ret.sendDataClean = sendDataClean.ToList();

            //ret.TwoLineDisplay = TwoLineDisplay;

            return ret;
        }
    }

    public class DistanceData : ICloneable
    {
        public int execResult;
        public double distance1;
        public double distance2;
        public string sdistance1;
        public string sdistance2;

        public DistanceData()
        {
            execResult = 0;

            distance1 = 0;
            distance2 = 0;
            sdistance1 = "";
            sdistance2 = "";
        }

        public void Initialize()
        {
            execResult = 0;

            distance1 = 0;
            distance2 = 0;
            sdistance1 = "";
            sdistance2 = "";
        }

        public object Clone()
        {
            DistanceData ret = new DistanceData();

            ret.execResult = execResult;

            ret.distance1 = distance1;
            ret.distance2 = distance2;
            ret.sdistance1 = sdistance1;
            ret.sdistance2 = sdistance2;

            return ret;
        }
    }

    public class ErrorInfo : ICloneable
    {
        public long rawErrorCode;
        public string sErrorCode;
        public string sErrorFunc;
        public string sErrorMessage;
        public string sErrorDetail1;
        public string sErrorDetail2;
        public string sErrorDetail3;
        public string sErrorDetail4;
        public string sErrorDetail5;
        public DeviceErrorInfo devErrorInfo;

        public ErrorInfo()
        {
            rawErrorCode = 0;
            sErrorCode = "";
            sErrorFunc = "";
            //sErrorDevice = "";
            //sErrorDevFunc = "";
            //sErrorDevMsg = "";
            sErrorMessage = "";
            sErrorDetail1 = "";
            sErrorDetail2 = "";
            sErrorDetail3 = "";
            sErrorDetail4 = "";
            sErrorDetail5 = "";
            devErrorInfo = new DeviceErrorInfo();
        }

        public object Clone()
        {
            ErrorInfo ret = new ErrorInfo();

            ret.rawErrorCode = rawErrorCode;
            ret.sErrorCode = sErrorCode;
            ret.sErrorFunc = sErrorFunc;
            //ret.sErrorDevice = sErrorDevice;
            //ret.sErrorDevFunc = sErrorDevFunc;
            //ret.sErrorDevMsg = sErrorDevMsg;
            ret.sErrorMessage = sErrorMessage;
            ret.sErrorDetail1 = sErrorDetail1;
            ret.sErrorDetail2 = sErrorDetail2;
            ret.sErrorDetail3 = sErrorDetail3;
            ret.sErrorDetail4 = sErrorDetail4;
            ret.sErrorDetail5 = sErrorDetail5;
            if(devErrorInfo!= null )
                ret.devErrorInfo = devErrorInfo;
            else
                ret.devErrorInfo = new DeviceErrorInfo();

            return ret;
        }

        public void Clear()
        {
            rawErrorCode = 0;
            sErrorCode = "";
            sErrorFunc = "";
            sErrorMessage = "";
            sErrorDetail1 = "";
            sErrorDetail2 = "";
            sErrorDetail3 = "";
            sErrorDetail4 = "";
            sErrorDetail5 = "";

            if (devErrorInfo != null)
                devErrorInfo.Clear();
            else
                devErrorInfo = new DeviceErrorInfo();
        }
    }

    public class DeviceErrorInfo : ICloneable
    {
        public int execResult;
        //public long rawErrorCode;
        public string sDeviceCode;
        public string sDeviceName;
        //public string sErrorCode;
        public string sErrorFunc;
        public string sErrorMessage;

        public DeviceErrorInfo()
        {
            execResult = 0;
            //rawErrorCode = 0;
            sDeviceCode = "";
            sDeviceName = "";
            //sErrorCode = "";
            sErrorFunc = "";
            sErrorMessage = "";
        }

        public object Clone()
        {
            DeviceErrorInfo ret = new DeviceErrorInfo();

            ret.execResult = execResult;
            //ret.rawErrorCode = rawErrorCode;
            ret.sDeviceCode = sDeviceCode;
            ret.sDeviceName = sDeviceName;
            //ret.sErrorCode = sErrorCode;
            ret.sErrorFunc = sErrorFunc;
            ret.sErrorMessage = sErrorMessage;

            return ret;
        }

        public void Clear()
        {
            execResult = 0;
            //rawErrorCode = 0;
            sDeviceCode = "";
            sDeviceName = "";
            //sErrorCode = "";
            sErrorFunc = "";
            sErrorMessage = "";
        }
    }

    public class CarTypeOption : ICloneable
    {
        public int carTypeFlag;
        public byte useMultiData;
        public byte useDataCount;
        public byte carTypePos1;
        public byte carTypePos2;
        public byte carTypePos3;
        public byte carTypePos4;

        public CarTypeOption()
        {
            carTypeFlag = 0;
            useMultiData = 0;
            useDataCount = 0;
            carTypePos1 = 0;
            carTypePos2 = 0;
            carTypePos3 = 0;
            carTypePos4 = 0;
        }

        public object Clone()
        {
            CarTypeOption ret = new CarTypeOption();

            ret.carTypeFlag = carTypeFlag;
            ret.useMultiData = useMultiData;
            ret.useDataCount = useDataCount;
            ret.carTypePos1 = carTypePos1;
            ret.carTypePos2 = carTypePos2;
            ret.carTypePos3 = carTypePos3;
            ret.carTypePos4 = carTypePos4;

            return ret;
        }

        public void Clear()
        {
            carTypeFlag = 0;
            useMultiData = 0;
            useDataCount = 0;
            carTypePos1 = 0;
            carTypePos2 = 0;
            carTypePos3 = 0;
            carTypePos4 = 0;
        }
    }

    public class ConnectedClient
    {
        public TcpClient socket { get; set; }
        public Int64 Id { get; set; }
        public byte[] buffer;
        public NetworkStream stream;
        public RingBuffer RecvBuff;
        public int size;

        //public int sp;
        //public int ep;
        //public CircularBuffer<byte> cp;
        //public ReceivePacket Receive { get; set; }

        public ConnectedClient()
        {
            buffer = new byte[128];
            //sp = ep = 0;
            Id = 0;
            RecvBuff = new RingBuffer(1024);
            size = 128;
            socket = new TcpClient();
        }

        public ConnectedClient(int size)
        {
            buffer = new byte[size];
            //sp = ep = 0;
            Id = 0;
            RecvBuff = new RingBuffer(size);
            this.size = size;
            socket = new TcpClient();
        }

        //public ConnectedClient(TcpClient socket, int id)
        //{
        //    //Receive = new ReceivePacket(/*socket*/, id);
        //    ////Receive = new ReceivePacket(socket);
        //    //Receive.StartReceiving();
        //    this.socket = socket;
        //    Id = id;
        //    buffer = new byte[1024];
        //    sp = ep = 0;
        //}
    }
    //public class MyListBoxItem
    //{
    //    public MyListBoxItem(Color c, string m)
    //    {
    //        ItemColor = c;
    //        Message = m;
    //    }
    //    public Color ItemColor { get; set; }
    //    public string Message { get; set; }
    //}
}