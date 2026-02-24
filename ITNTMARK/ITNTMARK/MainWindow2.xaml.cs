using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public partial class MainWindow : Window
    {
        public byte byLaserNumber = 0;
        public byte byMismatchErrorFlag = 0;

        private async Task<ITNTResponseArgs> ExecuteProcessSignal_READY()
        {
            string className = "MainWindow";
            string funcName = "ExecuteProcessSignal_READY";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //int count = 0;
            //int retval = 0;
            string sCurrentFunc = "RECEIVE READY SIGNAL";
            string value = "";
            string tmpNumber = "";
            string log = "";
            byte byReadNumber = 0;
            string sProcedure = "00";

            try
            {
                //currentProcessStatus = 0;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("OPTION", "ISDUALHEAD", "0", ref value, Constants.MARKING_INI_FILE);
                if ((value != "0") && (value != ""))
                {
                    retval = await plcComm.ReadUseLaserNum();
                    if (retval.execResult != 0)
                    {
                        m_bDoingMarkingFlag = false;
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadUseLaserNum) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        return retval;
                    }

                    if (retval.recvString.Length < 8)
                    {
                        log = "PLC DATA LENGTH INVALID (ReadUseLaserNum) : " + retval.recvString.Length + " - " + retval.recvString;
                        //ShowLog(className, funcName, 2, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadUseLaserNum ERROR : PLC STRING LENGTH SHORT = " + retval.recvString.Length.ToString(), Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;

                        retval.errorInfo.sErrorMessage = log;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                        return retval;
                    }

                    tmpNumber = retval.recvString.Substring(4, 4);
                    byte.TryParse(tmpNumber, out byLaserNumber);

                    Util.GetPrivateProfileValue("MARK", "USEHEADNO", "1", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out byReadNumber);

                    if (byReadNumber != byLaserNumber)
                    {
                        log = "MARKING ERROR - MARK HEADER NUMBER IS INVALID. (SETTING = " + byReadNumber.ToString() + ", REAL = " + byLaserNumber.ToString() + ")";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        if (byMismatchErrorFlag == 0)
                        {
                            retval.execResult = -10;
                            retval.errorInfo.sErrorMessage = log;
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 0, 0);

                            //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, log);
                            m_bDoingMarkingFlag = false;
                            byMismatchErrorFlag = 1;
                        }

                        return retval;
                    }
                }
                else
                {
                    byMismatchErrorFlag = 0;
                }

                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_READY_MARK);
                ShowLog(className, funcName, 0, "[0] RECEIVE READY SIGNAL");       ////??????

                ShowLabelData("[0] : STAND BY", lblPLCData);

                ShowErrorMessage("", true);

                m_bDoingMarkingFlag = false;

                //??????
                //await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, null, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 0, 0);

                seqcheckError = 0;
                DriveInfoDeligate();
                //currMarkInfo.Initialize();
                currMarkInfo.currMarkData.isReady = false;

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                //recvArg.rawErrorCode = ex.HResult;          //??????
                //recvArg.sErrorMessage = ex.Message;
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                //recvArg.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                //recvArg.errorInfo.devErrorInfo.sErrorMessage = "OPEN LASER ERROR = " + "CONNECT FAIL";


                ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            return retval;
        }

        private async Task<ITNTResponseArgs> ExecuteProcessSignal_NEXTVIN()
        {
            string className = "MainWindow";
            string funcName = "ExecuteProcessSignal_NEXTVIN";

            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            int count = 0;
            int totcount = 0;
            //int retval = 0;
            string value = "";
            string sCurrentFunc = "RECEIVE START SIGNAL";
            string sProcedure = "01";
            string log = "";
            string tmpNumber = "";
            byte byReadNumber = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ///1-1
                ///

                Util.GetPrivateProfileValue("OPTION", "ISDUALHEAD", "0", ref value, Constants.MARKING_INI_FILE);
                if ((value != "0") && (value != ""))
                {
                    retval = await plcComm.ReadUseLaserNum();
                    if (retval.execResult != 0)
                    {
                        m_bDoingMarkingFlag = false;
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadUseLaserNum) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        return retval;
                    }

                    if (retval.recvString.Length < 8)
                    {
                        log = "PLC DATA LENGTH INVALID (ReadUseLaserNum) : " + retval.recvString.Length + " - " + retval.recvString;
                        //ShowLog(className, funcName, 2, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadUseLaserNum ERROR : PLC STRING LENGTH SHORT = " + retval.recvString.Length.ToString(), Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;

                        retval.errorInfo.sErrorMessage = log;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                        return retval;
                    }

                    tmpNumber = retval.recvString.Substring(4, 4);
                    byte.TryParse(tmpNumber, out byLaserNumber);

                    Util.GetPrivateProfileValue("MARK", "USEHEADNO", "1", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out byReadNumber);

                    if (byReadNumber != byLaserNumber)
                    {
                        log = "MARKING ERROR - MARK HEADER NUMBER IS INVALID. (SETTING = " + byReadNumber.ToString() + ", REAL = " + byLaserNumber.ToString() + ")";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        if (byMismatchErrorFlag == 0)
                        {
                            retval.execResult = -10;
                            retval.errorInfo.sErrorMessage = log;
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 0, 0);

                            //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, log);
                            m_bDoingMarkingFlag = false;
                            byMismatchErrorFlag = 1;
                        }

                        return retval;
                    }
                }
                else
                {
                    byMismatchErrorFlag = 0;
                }

                ShowLabelData("RECEIVE START SIGNAL", lblCheckResult);

                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RECV_NEXTVIN);
                ShowLabelData("[" + sProcedure + "] : " + sCurrentFunc, lblPLCData);
                ShowLog(className, funcName, 0, "[" + sProcedure + "] " + sCurrentFunc);              ///??????

                /////1-2
                if (byMainScreenType == 0)
                {
                    retval = await CheckNextDataCount(sProcedure);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("CANNOT FIND FA DATA 1"), Thread.CurrentThread.ManagedThreadId);
                        ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                        return retval;
                    }

                    //ShowLog(className, funcName, 0, "[1-1] 데이터 검사 시작");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("COMPLETE CHECKING MES DATA"), Thread.CurrentThread.ManagedThreadId);

                    retval = await MakeCurrentMarkData(dgdPlanData);
                }
                else
                    retval = await MakeCurrentMarkData2();

                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);

                    ShowLog(className, funcName, 2, "[1-1] NO MES DATA FOUND", retval.errorInfo.sErrorMessage);
                    //ITNTErrorCode();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, currMarkInfo.currMarkData.mesData.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);

                    return retval;
                }

                //if (currMarkInfo.currMarkData.mesData.userDataType == 3)
                //{
                //    ITNTResponseArgs recv = new ITNTResponseArgs();
                //    recv = await plcComm.SendMatchingResult(PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_OK);
                //    if (recv.execResult != 0)
                //    {
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SIGNAL_PC2PLC_MATCHING_OK ERROR - " + recv.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                //        ShowErrorMessage("SEND MATCHING OK SIGNAL TO PLC ERROR", false);
                //    }
                //    return recvArg;
                //}

                string logstring = "";
                logstring = "MARKING DATA\r\n";
                logstring += "##########################################################\r\n";
                logstring += "    --- DATA INFORMATION ---\r\n";
                logstring += "    TYPE     : " + currMarkInfo.currMarkData.mesData.cartype + "\r\n";
                logstring += "  MES VIN    : " + currMarkInfo.currMarkData.mesData.rawvin + "\r\n";
                logstring += "  MARK VIN   : " + currMarkInfo.currMarkData.mesData.markvin + "\r\n";
                logstring += "    SEQ NO   : " + currMarkInfo.currMarkData.mesData.sequence + "\r\n";

                logstring += "    --- FONT INFORMATION ---\r\n";
                logstring += "    NAME     : " + currMarkInfo.currMarkData.pattern.fontValue.fontName + "\r\n";
                logstring += "    HEIGTH   : " + currMarkInfo.currMarkData.pattern.fontValue.height.ToString() + "\r\n";
                logstring += "    WIDTH    : " + currMarkInfo.currMarkData.pattern.fontValue.width.ToString() + "\r\n";
                logstring += "    PITCH    : " + currMarkInfo.currMarkData.pattern.fontValue.pitch + "\r\n";
                logstring += "    PATTERN  : " + currMarkInfo.currMarkData.pattern.name + "\r\n";
                logstring += "##########################################################";
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, logstring, Thread.CurrentThread.ManagedThreadId);

                logstring = "SEQ = " + currMarkInfo.currMarkData.mesData.sequence.ToString() + ", VIN = " + currMarkInfo.currMarkData.mesData.markvin;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, logstring, Thread.CurrentThread.ManagedThreadId);

                ShowLog(className, funcName, 0, "[1-1] MARKING DATA VIN = " + currMarkInfo.currMarkData.mesData.markvin + ", SEQ = " + currMarkInfo.currMarkData.mesData.sequence);

                //Change Check Flag
                if (byMainScreenType == 0)
                {
                    retval = UpdateNextMarkData(currMarkInfo.currMarkData.mesData);
                    ShowMarkingDataList(true, false);
                    ScrollViewToPoint(dgdPlanData);
                }

                //Show Current data on Top
                //await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2, 1);
                await ShowCurrentSequenceVIN(0, currMarkInfo.currMarkData, (byte)DISPLAY_INFO_COLOR_TYPE.DISP_COLOR_NEXTVIN, 1);

                //Util.GetPrivateProfileValue("COLOR", "CHARACTERLINE", "#FF5072DD", ref value, Constants.PARAMS_INI_FILE);
                //if (value.Length > 8)
                //    lineColor = (Color)ColorConverter.ConvertFromString(value);
                //else
                //    lineColor = (Color)ColorConverter.ConvertFromString("#FF5072DD");

#if MANUAL_MARK
                ImageProcessManager.GetPatternDataManual(currMarkInfo.currMarkData.patternName, currMarkInfo.currMarkData.mesData.rawcartype, ref currMarkInfo.currMarkData.pattern);
#else
                //ImageProcessManager.GetPatternData(currMarkInfo.currMarkData.patternName, ref currMarkInfo.currMarkData.pattern);
#endif

                //Check CAR Type
                //ShowLog(className, funcName, 0, "[1-2] 차종/서열 비교");
                if (byMainScreenType == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK CAR TYPE / SEQUENCE", Thread.CurrentThread.ManagedThreadId);

                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_CARTYPE);
                    //retval = await CheckMarkingData("1-", sCurrentFunc);

                    retval = await CheckCarTypeProcess(currMarkInfo.currMarkData.mesData, sCurrentFunc);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK CAR TYPE / SEQUENCE NG", Thread.CurrentThread.ManagedThreadId);
                        return retval;
                    }
                    ShowLog(className, funcName, 0, "[1-2] COMPARE CAR TYPE OK");

                    //Check Sequence
                    int seqcomptype = (int)Util.GetPrivateProfileValueUINT("OPTION", "SEQCOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                    if (seqcomptype == 2)
                        retval = await CheckSequence2(currMarkInfo.currMarkData.mesData.sequence, sCurrentFunc);
                    //else if (seqcomptype == 1)
                    //    recvArg.execResult = await CheckSequence(currMarkInfo.currMarkData.mesData.sequence);//, selMarkedRow.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString());
                    else if (seqcomptype == 0)
                    {
                        ShowLabelData(currMarkInfo.currMarkData.mesData.sequence, lblPLCSEQValue);
                        ShowLabelData(currMarkInfo.currMarkData.mesData.sequence, lblMESSEQValue);
                        retval.execResult = 0;
                    }

                    if (retval.execResult != 0)
                    {
                        ShowLog(className, funcName, 2, "[1-2] SEQUENCE MATCHING NG", retval.errorInfo.sErrorMessage);
                        await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);
                        //await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        //matchingError = 1;
                        seqcheckError = 1;
                        //ITNTErrorCode();
                        ShowErrorMessage("SEQUENCE MATCHING NG" + retval.errorInfo.sErrorMessage, false);
                        return retval;
                    }
                    else
                        seqcheckError = 0;
                    ShowLog(className, funcName, 0, "[1-2] SEQUENCE MATCHING OK");

                    //Check Double Marking
                    //ShowLog(className, funcName, 0, "[1-3] 이중 각인 체크");
                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_DOUBLE);
#if AGING_TEST_PLC
#else
                    if ((currMarkInfo.currMarkData.mesData.userDataType != 3) && (currMarkInfo.currMarkData.mesData.isInserted == "0"))
                    {
                        retval = await CheckDoubleMarking(currMarkInfo.currMarkData.mesData.rawvin, sCurrentFunc);
                        if (retval.execResult != 0)
                        {
                            m_bDoingMarkingFlag = false;
                            ShowLog(className, funcName, 2, "[1-3] DOUBLE MARKING CHECKING NG", retval.errorInfo.sErrorMessage);
                            await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);
                            //await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                            //ITNTErrorCode();

                            ShowErrorMessage("DOUBLE MARKING CHECKING NG" + retval.errorInfo.sErrorMessage, false);

                            return retval;
                        }
                    }
#endif
                    ShowLog(className, funcName, 0, "[1-3] DOUBLE MARKING CHECKING OK");

                    //Move Robot Signal (matching OK signal to PLC)
                    ShowLog(className, funcName, 0, "[1-4] SEND MATCHING OK SIGNAL");
                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_SEND_MATCHRESULT);
                }
                else
                {
                    ShowLog(className, funcName, 0, "[1-4] SEND READ OK SIGNAL");
                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_SEND_MATCHRESULT);
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendMatchingResult - START"), Thread.CurrentThread.ManagedThreadId);
                retval = await plcComm.SendMatchingResult(PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_OK);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendMatchingResult - ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    //ShowErrorMessage("SEND MATCHING OK TO PLC ERROR", false);
                    //ShowLog(className, funcName, 2, "[1-4] MATCHING OK 신호 전송 실패", "통신 오류");

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendMatchingResult) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                    return retval;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendMatchingResult - END"), Thread.CurrentThread.ManagedThreadId);
#if AGING_TEST
                    //for test
                    Task.Delay(500).Wait();
                    Util.WritePrivateProfileValue("PLC", "SIGNAL", "00FF0002", "TEST.ini");
                    Util.WritePrivateProfileValue("VISION_TEST", "RESULT", "V0O", "TEST.ini");
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NEXT VIN END", Thread.CurrentThread.ManagedThreadId);
                ShowLog(className, funcName, 0, "[1-5] WAIT MARKING SIGNAL");
                //if(byMainScreenType == 0)
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_WAIT_MARKSIGNAL);
                //else
                //    ShowCurrentStateLabel2((byte)STATUS_LABEL_NUM2.STATUS_LABEL_WAIT_MARKSIGNAL);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;

                ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);

                return retval;
            }

            return retval;
        }

        private async Task<ITNTResponseArgs> ExecuteProcessSignal_MARKING(byte orderflag)
        {
            string className = "MainWindow";
            string funcName = "ExecuteProcessSignal_MARKING";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            DistanceData distData = new DistanceData();
            //int retval = 0;
            //int iret = 0;
            int count = 0;
            int totcount = 0;
            Stopwatch sw = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            CheckAreaData chkdata = new CheckAreaData();
            //string stepstring = "";
            string stepstring = "[2";
            string value = "";
            byte byUseVision = 0;
            string log = "";
            string sCurrentFunc = "RECEIVE MARKING SIGNAL";
            string sProcedure = "2";
            //byte byHaedNumber = 0;
            string tmpNumber = "";
            byte byReadNumber = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("OPTION", "ISDUALHEAD", "0", ref value, Constants.MARKING_INI_FILE);
                if ((value != "0") && (value != ""))
                {
                    retval = await plcComm.ReadUseLaserNum();
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadUseLaserNum ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadUseLaserNum) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        return retval;
                    }

                    if (retval.recvString.Length < 8)
                    {
                        log = "PLC DATA LENGTH INVALID (ReadUseLaserNum) : " + retval.recvString.Length + " - " + retval.recvString;
                        //ShowLog(className, funcName, 2, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadUseLaserNum ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;

                        retval.errorInfo.sErrorMessage = log;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        return retval;
                    }

                    tmpNumber = retval.recvString.Substring(4, 4);
                    byte.TryParse(tmpNumber, out byLaserNumber);

                    Util.GetPrivateProfileValue("MARK", "USEHEADNO", "1", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out byReadNumber);

                    if (byReadNumber != byLaserNumber)
                    {
                        log = "MARKING ERROR - MARK HEADER NUMBER IS INVALID. (SETTING = " + byReadNumber.ToString() + ", REAL = " + byLaserNumber.ToString() + ")";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                        if (byMismatchErrorFlag == 0)
                        {
                            retval.execResult = -10;
                            retval.errorInfo.sErrorMessage = log;
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 0, 0);

                            //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, log);
                            m_bDoingMarkingFlag = false;
                            byMismatchErrorFlag = 1;
                        }

                        return retval;
                    }
                }
                else
                {
                    byMismatchErrorFlag = 0;
                    byLaserNumber = 1;
                }

                // data가 없는 경우
                // data count check
                if (currMarkInfo.currMarkData.isReady == false)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "currMarkInfo.isReady = false", Thread.CurrentThread.ManagedThreadId);
                    retval = await LoadMarkDataAgain(sProcedure);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("CANNOT FIND FA DATA 1"), Thread.CurrentThread.ManagedThreadId);
                        ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                        return retval;
                    }

#if MANUAL_MARK
                    ImageProcessManager.GetPatternDataManual(currMarkInfo.currMarkData.patternName, currMarkInfo.currMarkData.mesData.rawcartype, ref currMarkInfo.currMarkData.pattern);
#else
                    //ImageProcessManager.GetPatternData(currMarkInfo.currMarkData.patternName, ref currMarkInfo.currMarkData.pattern);
#endif

                    if (byMainScreenType == 0)
                    {
                        //Check CAR Type
                        ShowLog(className, funcName, 0, stepstring + "-1] COMPARE CAR TYPE");
                        ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_CARTYPE);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK CAR TYPE / SEQUENCE", Thread.CurrentThread.ManagedThreadId);

                        retval = await CheckCarTypeProcess(currMarkInfo.currMarkData.mesData, sCurrentFunc);
                        if (retval.execResult != 0)
                        {
                            m_bDoingMarkingFlag = false;
                            ShowLog(className, funcName, 2, stepstring + "-2] COMPARE CAR TYPE NG", retval.errorInfo.sErrorMessage);
                            //await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);
                            //await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                            //matchingError = 1;
                            //ITNTErrorCode();
                            ShowErrorMessage("COMPARE CAR TYPE NG" + retval.errorInfo.sErrorMessage, false);
                            return retval;
                        }

                        //Check Sequence
                        int seqcomptype = (int)Util.GetPrivateProfileValueUINT("OPTION", "SEQCOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                        if (seqcomptype == 2)
                            retval = await CheckSequence2(currMarkInfo.currMarkData.mesData.sequence, sCurrentFunc);
                        //else if (seqcomptype == 1)
                        //    recvArg = await CheckSequence(currMarkInfo.currMarkData.mesData.sequence);//, selMarkedRow.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString());
                        else if (seqcomptype == 0)
                            retval.execResult = 0;

                        if (retval.execResult != 0)
                        {
                            seqcheckError = 1;
                            //ITNTErrorCode();
                            m_bDoingMarkingFlag = false;

                            //ITNTErrorCode(className, funcName, sProcedure, recvArg.errorInfo);
                            return retval;
                        }
                        else
                            seqcheckError = 0;

                        //Check Double Marking
                        ShowLog(className, funcName, 0, stepstring + "-2] CHECK DOUBLE MARKING");
                        ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_DOUBLE);
#if AGING_TEST_PLC
#else
                        if ((currMarkInfo.currMarkData.mesData.userDataType != 3) && (currMarkInfo.currMarkData.mesData.isInserted == "0"))
                        {
                            retval = await CheckDoubleMarking(currMarkInfo.currMarkData.mesData.rawvin, sCurrentFunc);
                            if (retval.execResult != 0)
                            {
                                m_bDoingMarkingFlag = false;

                                //ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                                return retval;
                            }
                        }
#endif
                    }
                    else
                    {

                    }
                }

                sw2.Start();
                await ShowCurrentSequenceVIN(0, currMarkInfo.currMarkData, (byte)DISPLAY_INFO_COLOR_TYPE.DISP_COLOR_NEXTVIN, 1);

                ///
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RECV_MARKSIGNAL);

                currMarkInfo.currMarkData.markorderFlag = orderflag;

                if (orderflag == 2)
                    stepstring = "[4";
                else
                    stepstring = "[2";
                ShowLog(className, funcName, 0, stepstring + "] RECEIVE MARKING START SIGNAL");

                if (m_bDoingMarkingFlag)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("m_bDoingMarkingFlag = {0} RETURN", m_bDoingMarkingFlag), Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = ErrorCodeConstant.ERROR_PROCESS_ERROR;
                    retval.errorInfo.sErrorMessage = "MARKING COMMAND IS RUNNING ALREADY";
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_YYYY + Constants.ERROR_SIGNAL_DUPLICATE_02;

                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                    sw2.Stop();
                    return retval;
                }

                //ShowLog(className, funcName, 0, "[2-1] 각인 데이터 생성");

                m_bDoingMarkingFlag = true;

                //if (bUseDispalcementSensor)
                //{
                //    distData = await GetDisplacementSensor();
                //    currMarkInfo.distance = (DistanceData)distData.Clone();
                //}

                //send pattern
                //Send Data to micro contoller

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, stepstring + "] READ FONT START", Thread.CurrentThread.ManagedThreadId);
                m_currCMD = (byte)'B';
                retval = await MarkControll.FontFlush();
                if (retval.execResult != 0)
                {
                    //m_bDoingNextVINFlag = false;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND FLUSH SIGNAL TO CONTROLLER ERROR", Thread.CurrentThread.ManagedThreadId);
                    m_bDoingMarkingFlag = false;
                    m_currCMD = 0;

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (FontFlush) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                    sw2.Stop();

                    return retval;
                }

                //Util.GetPrivateProfileValue("OPTION", "ISDUALHEAD", "0", ref value, Constants.MARKING_INI_FILE);
                //if ((value != "0" ) && (value != ""))
                //{
                //    //Util.GetPrivateProfileValue("MARK", "USEHEADNO", "1", ref value, Constants.MARKING_INI_FILE);
                //    //byte.TryParse(value, out byLaserNumber);

                //    retval = await plcComm.ReadUseLaserNum();
                //    if (retval.execResult != 0)
                //    {
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMarkingStatus ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                //        m_bDoingMarkingFlag = false;
                //        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadUseLaserNum) ERROR = " + retval.execResult.ToString();
                //        retval.errorInfo.sErrorFunc = sCurrentFunc;

                //        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                //        sw2.Stop();
                //        return retval;
                //    }

                //    if (retval.recvString.Length < 8)
                //    {
                //        log = "PLC DATA LENGTH INVALID (ReadUseLaserNum) : " + retval.recvString.Length + " - " + retval.recvString;
                //        //ShowLog(className, funcName, 2, log);
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadUseLaserNum ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);
                //        m_bDoingMarkingFlag = false;

                //        retval.errorInfo.sErrorMessage = log;
                //        retval.errorInfo.sErrorFunc = sCurrentFunc;

                //        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                //        sw2.Stop();

                //        return retval;
                //    }

                //    tmpNumber = retval.recvString.Substring(4, 4);
                //    byte.TryParse(tmpNumber, out byLaserNumber);

                //    Util.GetPrivateProfileValue("MARK", "USEHEADNO", "1", ref value, Constants.PARAMS_INI_FILE);
                //    byte.TryParse(value, out byReadNumber);

                //    if (byReadNumber != byLaserNumber)
                //    {
                //        log = "MARKING ERROR - MARK HEADER NUMBER IS INVALID. (SETTING = " + byReadNumber.ToString() + ", REAL = " + byLaserNumber.ToString() + ")";
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //        retval.execResult = -10;
                //        retval.errorInfo.sErrorMessage = log;
                //        retval.errorInfo.sErrorFunc = sCurrentFunc;

                //        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                //        //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, log);
                //        m_bDoingMarkingFlag = false;
                //        sw2.Stop();

                //        return retval;
                //    }
                //}
                //else
                //{
                //    byLaserNumber = 1;
                //}


                if (bHeadType == 0)
                {
                    //for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++) // ??? TM SHIN
                    //{
                    //    List<FontDataClass> fontData = new List<FontDataClass>();
                    //    //ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.currMarkData.pattern.fontValue.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
                    //    currMarkInfo.currMarkData.fontData.Add(fontData);
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowNextMarkData", string.Format("FONT DATA {0}CH, {1}PT", i, fontData.Count), Thread.CurrentThread.ManagedThreadId);
                    //}

                    ShowLog(className, funcName, 0, stepstring + "] SEND MARKING DATA TO CONTROLLER");

                    retval = await SendFontData(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern.name);
                    if (retval.execResult != 0)
                    {
                        //m_bDoingNextVINFlag = false;
                        m_bDoingMarkingFlag = false;
                        m_currCMD = 0;
                        //ShowErrorMessage("SEND VIN TO PLC ERROR", false);
                        //ITNTErrorLog.Instance.Trace(0, "SEND VIN TO PLC ERROR");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND VIN TO CONTROLLER ERROR", Thread.CurrentThread.ManagedThreadId);

                        //recvArg.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (SetPhaseComp) ERROR = " + recvArg.execResult.ToString();
                        //recvArg.errorInfo.sErrorFunc = sErrorFunc;
                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        sw2.Stop();

                        return retval;
                    }

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 MARKING START", Thread.CurrentThread.ManagedThreadId);

                    //[3-2]
                    Util.GetPrivateProfileValue("VISION", "USEVISION", "0", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out byUseVision);
                    if (byUseVision != 0)
                    {
                        //ShowLog(className, funcName, 0, "[3-1] VISION으로 데이터 전송");
                        retval.execResult = await SendData2Vision(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, distData, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                        if (retval.execResult != 0)
                        {
                            await Task.Delay(200);
                            retval.execResult = await SendData2Vision(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, distData, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                            if (retval.execResult != 0)
                            {
                                await Task.Delay(200);
                                retval.execResult = await SendData2Vision(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, distData, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                                if (retval.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND TO VISION FAILURE : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                }
                            }
                        }
                    }
                    //start marking
                    ShowLog(className, funcName, 0, stepstring + "] START MARKING");
                    m_currCMD = (byte)'R';
                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RUN_MARKING);
                    //markTimes = Util.GetPrivateProfileValueUINT("MARK", "MARKTIMES", 1, Constants.PARAMS_INI_FILE);
                    //retval = await MarkControll.StartMark2MarkController((short)markTimes);
                    retval = await MarkControll.RunStart(0);
                    if (retval.execResult != 0)
                    {
                        //m_bDoingNextVINFlag = false;
                        m_bDoingMarkingFlag = false;
                        m_currCMD = 0;
                        //log = "COMMUNICATION TO CONTROLLER (RunStart) ERROR = " + recvArg.execResult.ToString();
                        //ShowLog(className, funcName, 2, log);
                        ////ShowErrorMessage("SEND STARTING SIGNAL TO MARK ERROR", false);
                        ////ITNTErrorLog.Instance.Trace(0, "SEND STARTING SIGNAL TO MARK ERROR");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND STARTING SIGNAL TO MARK ERROR", Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (RunStart) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        sw2.Stop();

                        return retval;
                    }
                    ShowLabelData("MARKING NOW", lblCheckResult, Brushes.Blue);
                    ShowLog(className, funcName, 0, stepstring + "] MARKING NOW");
                    retval = await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_DOING);
                    if (retval.execResult != 0)
                    {
                        ////ShowErrorMessage("SEND MARKING SIGNAL TO PLC ERROR", false);
                        //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "SEND MARKING SIGNAL TO PLC ERROR", recvArg.sErrorMessage);
                        ////ITNTErrorLog.Instance.Trace(0, "SEND MARKING SIGNAL TO PLC ERROR");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMarkingStatus ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;
                        //log = "COMMUNICATION TO PLC (SendMarkingStatus) ERROR = " + recvArg.execResult.ToString();
                        //ShowLog(className, funcName, 2, log);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendMarkingStatus) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        sw2.Stop();

                        return retval;
                    }

                    markRunTimer.Start();
                    if (byUseVision != 0)
                    {
                        //CloseVisionServer();
                        OpenVisionServer();
                    }
                }
                else   // Laser
                {
                    Single pc = Convert.ToSingle(currMarkInfo.currMarkData.pattern.laserValue.sPhaseComp);
                    m_currCMD = (byte)'#';
                    retval = await MarkControll.SetPhaseComp(pc);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SET PHASE COMPENSATION FAILURE", Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;
                        m_currCMD = 0;

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (SetPhaseComp) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        sw2.Stop();

                        return retval;
                    }

                    cycleWatch.Reset();
                    cycleWatch.Start();
                    cycleTimer.Start();
                    bool BFLAG = true;


#if LASER_OFF
#else
                    while (BFLAG)
                    {
                        retval = await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendAirAsync ERROR = " + chkdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            m_bDoingMarkingFlag = false;

                            cycleWatch.Stop();
                            //cycleWatch.Reset();
                            cycleTimer.Stop();

                            await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendAirAsync) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            //retval.errorInfo.sErrorCode = "2";

                            //ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        //#if LASER_YLR

                        chkdata = await Range_Test(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern);
                        if ((chkdata.execResult == (int)ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR) || (chkdata.execResult == (int)ErrorCodeConstant.ERROR_SLOPE_ERROR))
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "User popup" + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                            ITNTErrorCode(className, funcName, sProcedure, chkdata.errorInfo, 0);

                            byte bresult = await ShowWariningDialogAsync(chkdata);
                            if (bresult == 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "User press [RETRY] " + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "USER SELECT RETRY", "");
                                ShowErrorMessage("", true);
                            }
                            else if (bresult == 1)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "User press [CONTINUE] : " + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                                currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                                BFLAG = false;
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "USER SELECT CONTINUE", "");
                                ShowErrorMessage("", true);
                                break;
                            }
                            else if (bresult == 2)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "User cancel Marking because of error : " + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                                //await plcComm.SendMarkingCanceled(1);
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "USER SELECT CANCEL", "");
                                ShowErrorMessage("", true);


                                await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                                await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE, orderflag);

                                ITNTResponseArgs retval2 = new ITNTResponseArgs();
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GO PARKING"), Thread.CurrentThread.ManagedThreadId);
                                m_currCMD = (byte)'K';
                                retval2 = await MarkControll.GoParking((short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.X * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5),
                                                                        (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5),
                                                                        (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5));

                                retval.execResult = chkdata.execResult;
                                retval.errorInfo = (ErrorInfo)chkdata.errorInfo.Clone();
                                sw2.Stop();

                                cycleWatch.Stop();
                                //cycleWatch.Reset();
                                cycleTimer.Stop();

                                return retval;
                            }
                            else
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "User cancel Marking because of error : " + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                                //await plcComm.SendMarkingCanceled(1);
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "USER SELECT CANCEL", "");
                                ShowErrorMessage("", true);

                                await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                                await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE, orderflag);

                                ITNTResponseArgs retval2 = new ITNTResponseArgs();
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GO PARKING"), Thread.CurrentThread.ManagedThreadId);
                                m_currCMD = (byte)'K';
                                retval2 = await MarkControll.GoParking((short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.X * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5),
                                                                        (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5),
                                                                        (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5));

                                retval.execResult = chkdata.execResult;
                                retval.errorInfo = (ErrorInfo)chkdata.errorInfo.Clone();
                                sw2.Stop();

                                cycleWatch.Stop();
                                //cycleWatch.Reset();
                                cycleTimer.Stop();

                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "USER SELECT CANCEL", "");
                                //await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                                //ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                                //ShowErrorMessage("", true);

                                return retval;
                            }
                        }
                        else if (chkdata.execResult != 0)
                        {
                            m_bDoingMarkingFlag = false;
                            await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RangeTest ERROR = " + chkdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            cycleWatch.Stop();
                            //cycleWatch.Reset();
                            cycleTimer.Stop();
                            retval.execResult = chkdata.execResult;

                            retval.errorInfo = (ErrorInfo)chkdata.errorInfo.Clone();

                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                            sw2.Stop();

                            ITNTResponseArgs retval2 = new ITNTResponseArgs();
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GO PARKING"), Thread.CurrentThread.ManagedThreadId);
                            m_currCMD = (byte)'K';
                            retval2 = await MarkControll.GoParking((short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.X * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5),
                                                                    (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5),
                                                                    (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5));

                            //markRunTimer.Stop();
                            return retval;
                        }
                        else
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Normal Height", Thread.CurrentThread.ManagedThreadId);
                            currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                            BFLAG = false;
                            break;
                        }
                    }

                    currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                    markRunTimer.Start();
                    //recvArg = await Start_TEXT(currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.pattern);
                    retval = await Start_TEXT2(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MARKING ERROR - Start_TEXT3. (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;
                        cycleWatch.Stop();
                        //cycleWatch.Reset();
                        cycleTimer.Stop();
                        markRunTimer.Stop();
                        await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        sw2.Stop();

                        return retval;
                    }
                    //ShowLog(className, funcName, 0, stepstring + "-1] MARKING STARTED");

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND COUNT = " +
                        currMarkInfo.senddata.sendDataFire.Count.ToString() + ", " + currMarkInfo.senddata.sendDataClean.Count.ToString(), Thread.CurrentThread.ManagedThreadId);
#endif

                    //ShowLog(className, funcName, 0, "[3-2] 각인 시작 신호 PLC 전송");
                    //ShowCurrentStateLabel(5);

                    retval = await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_DOING, orderflag);
                    if (retval.execResult != 0)
                    {
                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 1);

                        m_bDoingMarkingFlag = false;
                        m_currCMD = 0;
                        cycleWatch.Stop();
                        //cycleWatch.Reset();
                        cycleTimer.Stop();
                        markRunTimer.Stop();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND VIN TO PLC ERROR", Thread.CurrentThread.ManagedThreadId);
                        await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendMarkingStatus) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        sw2.Stop();

                        return retval;
                    }

                    ShowLog(className, funcName, 0, stepstring + "-2] SEND MARKING START SIGNAL TO PLC OK");

                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RUN_MARKING);
                    ShowLog(className, funcName, 0, stepstring + "-3] START MARKING");
                    ShowLabelData("MARKING NOW", lblCheckResult, Brushes.Blue);

                    m_currCMD = (byte)'d';
                    if (orderflag == 2)
                        retval = await MarkControll.dwellTimeSet(currMarkInfo.currMarkData.pattern.headValue.markDelayTime2).ConfigureAwait(false);
                    else
                        retval = await MarkControll.dwellTimeSet(currMarkInfo.currMarkData.pattern.headValue.markDelayTime1).ConfigureAwait(false);

                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("dwellTimeSet ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                        //log = "COMMUNICATION TO CONTROLLER (dwellTimeSet) ERROR = " + recvArg.execResult.ToString();
                        //ShowLog(className, funcName, 2, log);
                        m_bDoingMarkingFlag = false;
                        await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (dwellTimeSet) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        sw2.Stop();

                        return retval;
                    }

                    //[3-2]
                    Util.GetPrivateProfileValue("VISION", "USEVISION", "0", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out byUseVision);
                    if (byUseVision != 0)
                    {
                        //ShowLog(className, funcName, 0, "[3-1] VISION으로 데이터 전송");
                        retval.execResult = await SendData2Vision(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, distData, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                        if (retval.execResult != 0)
                        {
                            await Task.Delay(200);
                            retval.execResult = await SendData2Vision(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, distData, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                            if (retval.execResult != 0)
                            {
                                await Task.Delay(200);
                                retval.execResult = await SendData2Vision(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, distData, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                                if (retval.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND TO VISION FAILURE : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                }
                            }
                        }
                    }

                    retval = await MarkingProcess(orderflag);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MarkingProcess ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;
                        cycleWatch.Stop();
                        //cycleWatch.Reset();
                        cycleTimer.Stop();
                        markRunTimer.Stop();
                        await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                        //ShowLog(className, funcName, 2, stepstring + "-3] MARKING ERROR - MarkingProcess", recvArg.execResult.ToString());
                        //ITNTErrorCode();
                        //recvArg.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (dwellTimeSet) ERROR = " + recvArg.execResult.ToString();
                        //recvArg.errorInfo.sErrorFunc = sErrorFunc;
                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                        if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                        {
                            await ChangeDBProcess4Thread();
                        }
                        sw2.Stop();

                        return retval;
                    }

                    await plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                    ShowLabelData("MARKING COMPLETE", lblCheckResult, Brushes.Blue);

                    cycleWatch.Stop();
                    //cycleWatch.Reset();
                    cycleTimer.Stop();

                    m_bDoingMarkingFlag = false;

                    sw.Stop();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AFTER MARKING : " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);

                    if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                    {
                        await ChangeDBProcess4Thread();
                    }

#if KASK_LASER
                    if (byUseVision != 0)
                    {
                        Thread goLinkPos = new Thread(new ParameterizedThreadStart(threadGoVisionStart));
                        goLinkPos.Start(currMarkInfo.currMarkData);
                    }
#endif
                }

                markRunTimer.Stop();

                //if (vision == null)
                //{
                //    //CloseVisionServer();
                //    OpenVisionServer();
                //}

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("0002 END"), Thread.CurrentThread.ManagedThreadId);
                //m_CurrentMarkNum = (m_CurrentMarkNum + 1) % 4;
                return retval;
            }
            catch (Exception ex)
            {
                //ITNTErrorCode();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                m_bDoingMarkingFlag = false;

                cycleTimer.Stop();
                cycleWatch.Stop();

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                return retval;
            }
        }

        private async Task<ITNTResponseArgs> ExecuteProcessSignal_VISION(byte orderflag)
        {
            string className = "MainWindow";
            string funcName = "ExecuteProcessSignal_VISION";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //int retval = 0;
            string ErrorCode = "";
            string value = "";
            double StartPosU = 0.0d;
            double Step_Length_U = 0.0d;
            //Stopwatch sw1 = new Stopwatch();
            //Stopwatch sw2 = new Stopwatch();
            Stopwatch swLink = new Stopwatch();
            Stopwatch swLink2 = new Stopwatch();
            Stopwatch swLink3 = new Stopwatch();
            Stopwatch swdelay = new Stopwatch();
            Stopwatch swMove = new Stopwatch();
            int useLink = 0;
            string stepstring = "[3";
            int count = 0;
            int totcount = 0;
            Stopwatch sw = new Stopwatch();
            string log = "";
            bool bLinkOK = false;
            string sCurrentFunc = "RECEIVE VISION SIGNAL";
            string sProcedure = "03";
            int timeoutval = 8;
            string tmpNumber = "";
            byte byReadNumber = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("OPTION", "ISDUALHEAD", "0", ref value, Constants.MARKING_INI_FILE);
                if ((value != "0") && (value != ""))
                {
                    retval = await plcComm.ReadUseLaserNum();
                    if (retval.execResult != 0)
                    {
                        m_bDoingMarkingFlag = false;
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadUseLaserNum) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        return retval;
                    }

                    if (retval.recvString.Length < 8)
                    {
                        log = "PLC DATA LENGTH INVALID (ReadUseLaserNum) : " + retval.recvString.Length + " - " + retval.recvString;
                        //ShowLog(className, funcName, 2, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadUseLaserNum ERROR : PLC STRING LENGTH SHORT = " + retval.recvString.Length.ToString(), Thread.CurrentThread.ManagedThreadId);
                        m_bDoingMarkingFlag = false;

                        retval.errorInfo.sErrorMessage = log;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                        return retval;
                    }

                    tmpNumber = retval.recvString.Substring(4, 4);
                    byte.TryParse(tmpNumber, out byLaserNumber);

                    Util.GetPrivateProfileValue("MARK", "USEHEADNO", "1", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out byReadNumber);

                    if (byReadNumber != byLaserNumber)
                    {
                        log = "MARKING ERROR - MARK HEADER NUMBER IS INVALID. (SETTING = " + byReadNumber.ToString() + ", REAL = " + byLaserNumber.ToString() + ")";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        if (byMismatchErrorFlag == 0)
                        {
                            retval.execResult = -10;
                            retval.errorInfo.sErrorMessage = log;
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 0, 0);

                            //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, log);
                            m_bDoingMarkingFlag = false;
                            byMismatchErrorFlag = 1;
                        }

                        return retval;
                    }
                }
                else
                {
                    byMismatchErrorFlag = 0;
                }

                if (currMarkInfo.currMarkData.isReady == false)
                {
                    retval = await LoadMarkDataAgain(sProcedure);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("CANNOT FIND FA DATA 1"), Thread.CurrentThread.ManagedThreadId);
                        ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                        return retval;
                    }
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("0004 START"), Thread.CurrentThread.ManagedThreadId);
                //string patternfile = Constants.PATTERN_PATH + currMarkInfo.currMarkData.pattern.name + ".ini";

                ShowLabelData("START VISION", lblCheckResult, Brushes.Blue);
                currMarkInfo.currMarkData.markorderFlag = orderflag;
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RECV_VISION);

                if (orderflag == 2)
                {
                    ShowLog(className, funcName, 0, "[5] RECEIVE VISION START SIGNAL : " + currMarkInfo.currMarkData.mesData.markvin);
                    stepstring = "[5";
                }
                else
                {
                    ShowLog(className, funcName, 0, "[3] RECEIVE VISION START SIGNAL : " + currMarkInfo.currMarkData.mesData.markvin);
                    stepstring = "[3";
                }

                Util.GetPrivateProfileValue("CONFIG", "INDEPENDENT", "0", ref value, Constants.SCANNER_INI_FILE);
                if (value != "0")
                {
                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RUN_VISION);
                    ShowLog(className, funcName, 0, stepstring + "-2] START VISION");

                    retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                    if (retval.execResult != 0)
                    {
                        await Task.Delay(200);
                        retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                        if (retval.execResult != 0)
                        {
                            await Task.Delay(200);
                            retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                            if (retval.execResult != 0)
                            {
                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }

                                //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO VISION ERROR");
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO VISION ERROR", Thread.CurrentThread.ManagedThreadId);
                                //ShowLog(className, funcName, 2, stepstring + "-3] SEND DATA TO VISION ERROR", recvArg.execResult.ToString());
                                ////ITNTErrorCode();
                                retval.errorInfo.sErrorMessage = "COMMUNICATION TO VISION (SendData2Vision) ERROR = " + retval.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                return retval;
                            }
                        }
                    }
                }
                else
                {
                    Util.GetPrivateProfileValue("CONFIG", "USELINK", "0", ref value, Constants.SCANNER_INI_FILE);
                    int.TryParse(value, out useLink);
                    if (useLink == 0)
                    {
#if KASK_LASER
#else
                        m_currCMD = (byte)'l';
                        retval = await MarkControll.LoadSpeed((byte)'l', currMarkInfo.currMarkData.pattern.scanValue.initSpeed4Scan, currMarkInfo.currMarkData.pattern.scanValue.targetSpeed4Scan, currMarkInfo.currMarkData.pattern.scanValue.accelSpeed4Scan, currMarkInfo.currMarkData.pattern.scanValue.decelSpeed4Scan);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-l ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                            Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                            if (value == "0")
                                await plcComm.SendVisionResult("N");
                            else
                            {
                                await plcComm.SendVisionResult("O");
                                //ShowErrorMessage("COMMUNICATION TO CONTROLLER ERROR", false);
                            }

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed - l) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO CONTROLLER ERROR");
                            return retval;
                        }

                        m_currCMD = (byte)'f';
                        retval = await MarkControll.LoadSpeed((byte)'f', currMarkInfo.currMarkData.pattern.scanValue.initSpeed4ScanFree, currMarkInfo.currMarkData.pattern.scanValue.targetSpeed4ScanFree, currMarkInfo.currMarkData.pattern.scanValue.accelSpeed4ScanFree, currMarkInfo.currMarkData.pattern.scanValue.decelSpeed4ScanFree);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-f ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                            Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                            if (value == "0")
                                await plcComm.SendVisionResult("N");
                            else
                            {
                                await plcComm.SendVisionResult("O");
                                //ShowErrorMessage("COMMUNICATION TO CONTROLLER ERROR", false);
                            }

                            //ShowLog(className, funcName, 2, stepstring + "-3] MARKING PROCESS ERROR - LOAD SPEED FAILURE", recvArg.execResult.ToString());
                            //ITNTErrorCode();
                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed - f) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO CONTROLLER ERROR");
                            return retval;
                        }

                        StartPosU = currMarkInfo.currMarkData.pattern.scanValue.startU;
                        Step_Length_U = currMarkInfo.currMarkData.pattern.scanValue.stepLength_U;
                        StartPosU = StartPosU * Step_Length_U;
                        //double home_u = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, Constants.SCANNER_INI_FILE);
                        short home_u = (short)(Step_Length_U * currMarkInfo.currMarkData.pattern.scanValue.home_U + 0.5);
                        doingCommand = true;
                        //m_currCMD = (byte)'k';
                        //recvArg = await MarkControll.MoveScanProfile((short)StartPosU);
                        m_currCMD = (byte)'h';
                        retval = await MarkControll.MoveScan2Home(home_u);
                        if (retval.execResult != 0)
                        {
                            m_bDoingMarkingFlag = false;
                            //ShowErrorMessage("VISION SCAN ERROR", false);
                            ////ITNTErrorLog.Instance.Trace(0, "VISION SCAN ERROR - MOVE HOME");
                            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCAN ERROR - MOVE HOME", Thread.CurrentThread.ManagedThreadId);

                            //ShowLog(className, funcName, 2, stepstring + "-3] MOVING VISION HEAD TO HOME ERROR", recvArg.execResult.ToString());
                            ////ITNTErrorCode();

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (MoveScan2Home) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        //Stopwatch sw = new Stopwatch();
                        swdelay.Start();
                        while (swdelay.Elapsed < TimeSpan.FromSeconds(6))
                        {
                            if (!doingCommand)
                                break;

                            await Task.Delay(50);
                        }
                        swdelay.Stop();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MOVE HOME : " + swdelay.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
                        swdelay.Reset();
#endif
                        //vision start
                        //string sendmsg = "M5" + "S";
                        retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                        if (retval.execResult != 0)
                        {
                            await Task.Delay(200);
                            retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                            if (retval.execResult != 0)
                            {
                                await Task.Delay(200);
                                retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                                if (retval.execResult != 0)
                                {
                                    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                    if (value == "0")
                                        await plcComm.SendVisionResult("N");
                                    else
                                    {
                                        await plcComm.SendVisionResult("O");
                                        //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                    }

                                    //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO VISION ERROR");
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO VISION ERROR", Thread.CurrentThread.ManagedThreadId);
                                    //ShowLog(className, funcName, 2, stepstring + "-3] SEND DATA TO VISION ERROR", recvArg.execResult.ToString());
                                    ////ITNTErrorCode();
                                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO VISION (SendData2Vision) ERROR = " + retval.execResult.ToString();
                                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                    return retval;
                                }
                            }
                        }

                        Thread.Sleep(1000);

                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE", Thread.CurrentThread.ManagedThreadId);
                        //double ScanLenU = (double)Util.GetPrivateProfileValueDouble("PROFILER", "SCANLEN", 130, patternfile);// LOAD Scan Length U Axis
                        //short scanleng = (short)(StartPosU + ScanLenU * Step_Length_U);
                        timeoutval = (int)Util.GetPrivateProfileValueUINT("PROFILE", "SCANTIMEOUT", 8, Constants.PARAMS_INI_FILE);

                        short scanleng = (short)(StartPosU + currMarkInfo.currMarkData.pattern.scanValue.scanLen * Step_Length_U + 0.5);
                        m_currCMD = (byte)'U';
                        doingCommand = true;

                        retval = await MarkControll.ScanProfile(scanleng, timeoutval);
                        if (retval.execResult != 0)
                        {
                            m_bDoingMarkingFlag = false;
                            m_currCMD = 0;
                            //ShowErrorMessage("VISION SCAN ERROR", false);
                            ////ITNTErrorLog.Instance.Trace(0, "VISION SCAN ERROR");
                            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCAN ERROR", Thread.CurrentThread.ManagedThreadId);

                            //log = "COMMUNICATION TO CONTROLLER (ScanProfile) ERROR = " + recvArg.execResult.ToString();
                            //ShowLog(className, funcName, 2, log);

                            ////ShowLog(className, funcName, 2, stepstring + "-3] MOVING VISION HEAD ERROR - SCAN ERROR", recvArg.execResult.ToString());
                            ////ITNTErrorCode();

                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCAN ERROR", Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (ScanProfile) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        swMove.Start();
                        while (swMove.Elapsed < TimeSpan.FromSeconds(6))
                        {
                            if (!doingCommand)
                                break;

                            await Task.Delay(50);
                        }
                        swMove.Stop();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE : " + swMove.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
                        swMove.Reset();
                        if (doingCommand)
                        {
                            Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                            if (value == "0")
                                await plcComm.SendVisionResult("N");
                            else
                            {
                                await plcComm.SendVisionResult("O");
                                ShowErrorMessage("VISION SCANNER MOVING ERROR", false);
                            }
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCANNER MOVING ERROR - TIMEOUT", Thread.CurrentThread.ManagedThreadId);

                            ShowLog(className, funcName, 2, stepstring + "-3] MOVING VISION HEAD - TIMEOUT", retval.execResult.ToString());
                            //ITNTErrorCode();

                            doingCommand = false;
                        }
                    }
                    else
                    {   // link type Vision Profiler
                        short scanStart = 0;
                        short scanEnd = 0;
#if KASK_LASER
#else
                        StartPosU = (currMarkInfo.currMarkData.pattern.scanValue.startU * currMarkInfo.currMarkData.pattern.scanValue.stepLength_U);

                        ShowLog(className, funcName, 0, stepstring + "-1] READY TO EXECUTE VISION");

                        if (currMarkInfo.currMarkData.pattern.scanValue.reverseScan == 0)
                        {
                            scanStart = (short)(StartPosU + 0.5);
                            scanEnd = (short)(StartPosU + currMarkInfo.currMarkData.pattern.scanValue.scanLen * currMarkInfo.currMarkData.pattern.scanValue.stepLength_U + 0.5);
                        }
                        else
                        {
                            scanEnd = (short)(StartPosU + 0.5);
                            scanStart = (short)(StartPosU + currMarkInfo.currMarkData.pattern.scanValue.scanLen * currMarkInfo.currMarkData.pattern.scanValue.stepLength_U + 0.5);
                        }

                        //1. Move Scan Header
                        m_currCMD = (byte)'M';
                        retval = await MarkControll.GoPoint((short)(currMarkInfo.currMarkData.pattern.scanValue.linkPos * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), 0);
                        if (retval.execResult != 0)
                        {
                            doingCommand = false;
                            m_currCMD = 0;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoParking ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (GoPoint) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        //Set Link 
                        retval = await plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SetLinkAsync) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        swLink.Start();
                        while (swLink.ElapsedMilliseconds < 1000)
                        {
                            await Task.Delay(50);
                        }
                        swLink.Stop();

                        //wait 400ms
                        swLink2.Start();
                        while (swLink2.ElapsedMilliseconds < 1000)        // ?? TM SHIN
                        {
                            //Get Link Status 
                            retval = await plcComm.ReadLinkStatusAsync();
                            if (retval.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadLinkStatusAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                await Task.Delay(200);
                                continue;
                            }

                            if (retval.recvString.Length < 8)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                                await Task.Delay(100);
                                continue;
                            }

                            if (retval.recvString.Substring(4, 4) == PLCControlManager.SIGNAL_PLC2PC_ON)
                            {
                                bLinkOK = true;
                                break;
                            }
                            await Task.Delay(100);
                        }
                        swLink2.Stop();

                        if (retval.execResult != 0)
                        {
                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadLinkStatusAsync) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        if (retval.recvString.Length < 8)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                            retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                            retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + retval.recvString.Length + " - " + retval.recvString;
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                            retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                            retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                            retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                            retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                            retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        if (bLinkOK == false)
                        {
                            //recvArg.sErrorMessage = "SET LINK ON FAIL";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);
                            retval.execResult = ErrorCodeConstant.ERROR_LINKON_ERROR;
                            retval.errorInfo.sErrorMessage = "SET LINK ERROR - SENSOR ERROR";
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_YYYY + Constants.ERROR_LINKON_FAIL;

                            retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                            retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                            retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                            retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                            retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;
                            ///
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        //Go to start position
                        //set speed
                        m_currCMD = (byte)'L';
                        retval = await MarkControll.LoadSpeed(m_currCMD, currMarkInfo.currMarkData.pattern.speedValue.initSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4Fast);
                        if (retval.execResult != 0)
                        {
                            //log = "COMMUNICATION TO CONTROLLER ERROR. (LoadSpeed) : " + recvArg.execResult.ToString();
                            //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                            ////ShowLog(className, funcName, 2, stepstring + "-1] LOAD FAST SPEED ERROR - LoadSpeed ERROR", recvArg.execResult.ToString());
                            ////ITNTErrorCode();

                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-L ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (LoadSpeed - L) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            return retval;
                        }

                        m_currCMD = (byte)'M';
                        retval = await MarkControll.GoPoint((short)scanStart, (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), 0);
                        if (retval.execResult != 0)
                        {
                            //log = "COMMUNICATION TO CONTROLLER ERROR. (GoPoint) : " + recvArg.execResult.ToString();
                            //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                            ////ShowLog(className, funcName, 2, stepstring + "-1] MOVING VISION HEAD (START POSITION) FAILURE", recvArg.execResult.ToString());
                            ////ShowLog(className, funcName, 2, "[3-3] 각인 데이터 생성 오류 - FLUSH 실패", recvArg.execResult.ToString());
                            ////ITNTErrorCode();

                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MoveScanProfile START POS ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (GoPoint - M) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                            return retval;
                        }
#endif

                        retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                        if (retval.execResult != 0)
                        {
                            await Task.Delay(200);
                            retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                            if (retval.execResult != 0)
                            {
                                await Task.Delay(200);
                                retval.execResult = await SendData2Vision(1, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, orderflag, bHeadType, byLaserNumber);
                                if (retval.execResult != 0)
                                {
                                    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                    if (value == "0")
                                        await plcComm.SendVisionResult("N");
                                    else
                                    {
                                        await plcComm.SendVisionResult("O");
                                        //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                    }
                                    //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO VISION ERROR");
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO VISION ERROR", Thread.CurrentThread.ManagedThreadId);

                                    //log = "SEND DATA TO VISION (SendData2Vision) ERROR = " + recvArg.execResult.ToString();
                                    //ShowLog(className, funcName, 2, log);

                                    //ShowLog(className, funcName, 2, stepstring + "-1] SEND DATA TO VISION ERROR", recvArg.execResult.ToString());
                                    //ITNTErrorCode();
                                    retval.errorInfo.sErrorMessage = "COMMUNICATION ERROR TO VISION";
                                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                    return retval;
                                }
                            }
                        }


                        ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RUN_VISION);
                        ShowLog(className, funcName, 0, stepstring + "-2] START VISION");

                        swLink3.Start();
                        while (swLink3.ElapsedMilliseconds < 500)        // ?? TM SHIN
                        {
                            //Get Link Status 
                            retval = await plcComm.ReadLinkStatusAsync();
                            if (retval.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                //log = "COMMUNICATION TO PLC ERROR. (ReadLinkStatusAsync) : " + recvArg.execResult.ToString();
                                //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                ////ShowLog(className, funcName, 2, stepstring + "-1] READ VISION LINK STATUS FAILURE", recvArg.execResult.ToString());
                                ////ITNTErrorCode();
                                retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadLinkStatusAsync) ERROR = " + retval.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            if (retval.recvString.Length < 8)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                                //log = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + recvArg.recvString.Length + " - " + recvArg.recvString;
                                //ShowLog(className, funcName, 2, log);
                                ////ShowLog(className, funcName, 2, stepstring + "-1] VISION LINK DATA LENGTH ERROR : " + recvArg.recvString, recvArg.execResult.ToString());
                                ////ITNTErrorCode();
                                retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + retval.recvString.Length + " - " + retval.recvString;
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            if (retval.recvString.Substring(4, 4) == PLCControlManager.SIGNAL_PLC2PC_ON)
                            {
                                bLinkOK = true;
                                break;
                            }
                            await Task.Delay(100);
                        }
                        swLink3.Stop();

                        if (bLinkOK == false)
                        {
                            StartPosU = (currMarkInfo.currMarkData.pattern.scanValue.startU * currMarkInfo.currMarkData.pattern.scanValue.stepLength_U);

                            ShowLog(className, funcName, 0, stepstring + "-1] READY TO EXECUTE VISION");

                            if (currMarkInfo.currMarkData.pattern.scanValue.reverseScan == 0)
                            {
                                scanStart = (short)(StartPosU + 0.5);
                                scanEnd = (short)(StartPosU + currMarkInfo.currMarkData.pattern.scanValue.scanLen * currMarkInfo.currMarkData.pattern.scanValue.stepLength_U + 0.5);
                            }
                            else
                            {
                                scanEnd = (short)(StartPosU + 0.5);
                                scanStart = (short)(StartPosU + currMarkInfo.currMarkData.pattern.scanValue.scanLen * currMarkInfo.currMarkData.pattern.scanValue.stepLength_U + 0.5);
                            }

                            //1. Move Scan Header
                            m_currCMD = (byte)'M';
                            retval = await MarkControll.GoPoint((short)(currMarkInfo.currMarkData.pattern.scanValue.linkPos * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), 0);
                            if (retval.execResult != 0)
                            {
                                doingCommand = false;
                                m_currCMD = 0;
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoParking ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                //log = "COMMUNICATION TO CONTROLLER ERROR. (GoPoint) : " + recvArg.execResult.ToString();
                                //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                ////ShowLog(className, funcName, 2, stepstring + "-1] MOVING VISION HEAD TO LINK POSITION ERROR", recvArg.execResult.ToString());
                                ////ITNTErrorCode();
                                retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (GoPoint) ERROR = " + retval.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            //Set Link 
                            retval = await plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                            if (retval.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                //log = "COMMUNICATION TO PLC ERROR. (SetLinkAsync) : " + recvArg.execResult.ToString();
                                //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                ////ShowLog(className, funcName, 2, stepstring + "-1] VISION LINK ON FAILURE", recvArg.execResult.ToString());
                                ////ITNTErrorCode();
                                retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SetLinkAsync) ERROR = " + retval.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            swLink.Reset();
                            swLink.Start();
                            while (swLink.ElapsedMilliseconds < 1000)
                            {
                                await Task.Delay(50);
                            }
                            swLink.Stop();

                            //wait 400ms
                            swLink2.Reset();
                            swLink2.Start();
                            while (swLink2.ElapsedMilliseconds < 1000)        // ?? TM SHIN
                            {
                                //Get Link Status 
                                retval = await plcComm.ReadLinkStatusAsync();
                                if (retval.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                    //log = "COMMUNICATION TO PLC ERROR. (ReadLinkStatusAsync) : " + recvArg.execResult.ToString();
                                    //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                    ////ShowLog(className, funcName, 2, stepstring + "-1] READ VISION LINK STATUS FAILURE", recvArg.execResult.ToString());
                                    ////ITNTErrorCode();
                                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadLinkStatusAsync) ERROR = " + retval.execResult.ToString();
                                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                    if (value == "0")
                                        await plcComm.SendVisionResult("N");
                                    else
                                    {
                                        await plcComm.SendVisionResult("O");
                                        //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                    }
                                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                    return retval;
                                }

                                if (retval.recvString.Length < 8)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                                    //log = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + recvArg.recvString.Length + " - " + recvArg.recvString;
                                    //ShowLog(className, funcName, 2, log);
                                    ////ShowLog(className, funcName, 2, stepstring + "-1] VISION LINK DATA LENGTH ERROR : " + recvArg.recvString, recvArg.execResult.ToString());
                                    ////ITNTErrorCode();
                                    ///
                                    retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                                    retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + retval.recvString.Length + " - " + retval.recvString;
                                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                                    //retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + recvArg.recvString.Length + " - " + recvArg.recvString;
                                    //retval.errorInfo.sErrorFunc = sCurrentFunc;
                                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                    return retval;
                                }

                                if (retval.recvString.Substring(4, 4) == PLCControlManager.SIGNAL_PLC2PC_ON)
                                {
                                    bLinkOK = true;
                                    break;
                                }
                                await Task.Delay(100);
                            }
                            swLink2.Stop();

                            if (bLinkOK == false)
                            {
                                //recvArg.sErrorMessage = "SET LINK ON FAIL";
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);
                                retval.execResult = ErrorCodeConstant.ERROR_LINKON_ERROR;
                                retval.errorInfo.sErrorMessage = "SET LINK ERROR - SENSOR ERROR";
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                //log = "SET LINK ON FAIL";
                                //ShowLog(className, funcName, 2, log);
                                ////ShowLog(className, funcName, 2, stepstring + "-1] VISION LINK DATA LENGTH ERROR : " + recvArg.recvString, recvArg.execResult.ToString());
                                ////ITNTErrorCode();
                                ///
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            //Go to start position
                            //set speed
                            m_currCMD = (byte)'L';
                            retval = await MarkControll.LoadSpeed(m_currCMD, currMarkInfo.currMarkData.pattern.speedValue.initSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4Fast);
                            if (retval.execResult != 0)
                            {
                                //log = "COMMUNICATION TO CONTROLLER ERROR. (LoadSpeed) : " + recvArg.execResult.ToString();
                                //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                ////ShowLog(className, funcName, 2, stepstring + "-1] LOAD FAST SPEED ERROR - LoadSpeed ERROR", recvArg.execResult.ToString());
                                ////ITNTErrorCode();

                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-L ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (LoadSpeed - L) ERROR = " + retval.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            m_currCMD = (byte)'M';
                            retval = await MarkControll.GoPoint((short)scanStart, (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), 0);
                            if (retval.execResult != 0)
                            {
                                //log = "COMMUNICATION TO CONTROLLER ERROR. (GoPoint) : " + recvArg.execResult.ToString();
                                //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                ////ShowLog(className, funcName, 2, stepstring + "-1] MOVING VISION HEAD (START POSITION) FAILURE", recvArg.execResult.ToString());
                                ////ShowLog(className, funcName, 2, "[3-3] 각인 데이터 생성 오류 - FLUSH 실패", recvArg.execResult.ToString());
                                ////ITNTErrorCode();

                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MoveScanProfile START POS ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                                retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (GoPoint - M) ERROR = " + retval.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            swLink3.Reset();
                            swLink3.Start();
                            while (swLink3.ElapsedMilliseconds < 500)        // ?? TM SHIN
                            {
                                //Get Link Status 
                                retval = await plcComm.ReadLinkStatusAsync();
                                if (retval.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                    //log = "COMMUNICATION TO PLC ERROR. (ReadLinkStatusAsync) : " + recvArg.execResult.ToString();
                                    //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                    ////ShowLog(className, funcName, 2, stepstring + "-1] READ VISION LINK STATUS FAILURE", recvArg.execResult.ToString());
                                    ////ITNTErrorCode();
                                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadLinkStatusAsync) ERROR = " + retval.execResult.ToString();
                                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                    if (value == "0")
                                        await plcComm.SendVisionResult("N");
                                    else
                                    {
                                        await plcComm.SendVisionResult("O");
                                        //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                    }
                                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                    return retval;
                                }

                                if (retval.recvString.Length < 8)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                                    //log = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + recvArg.recvString.Length + " - " + recvArg.recvString;
                                    //ShowLog(className, funcName, 2, log);
                                    ////ShowLog(className, funcName, 2, stepstring + "-1] VISION LINK DATA LENGTH ERROR : " + recvArg.recvString, recvArg.execResult.ToString());
                                    ////ITNTErrorCode();
                                    //retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + recvArg.recvString.Length + " - " + recvArg.recvString;
                                    //recvArg.errorInfo.sErrorFunc = sCurrentFunc;

                                    retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                                    retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + retval.recvString.Length + " - " + retval.recvString;
                                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                    if (value == "0")
                                        await plcComm.SendVisionResult("N");
                                    else
                                    {
                                        await plcComm.SendVisionResult("O");
                                        //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                    }
                                    ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                    return retval;
                                }

                                if (retval.recvString.Substring(4, 4) == PLCControlManager.SIGNAL_PLC2PC_ON)
                                {
                                    bLinkOK = true;
                                    break;
                                }
                                await Task.Delay(100);
                            }
                            swLink3.Stop();

                            if (bLinkOK == false)
                            {
                                //recvArg.sErrorMessage = "SET LINK ON FAIL";
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : LINK STATUS ERROR"), Thread.CurrentThread.ManagedThreadId);


                                retval.execResult = ErrorCodeConstant.ERROR_LINKON_ERROR;
                                retval.errorInfo.sErrorMessage = "SET LINK ERROR - SENSOR ERROR";
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_YYYY + Constants.ERROR_LINKON_FAIL;

                                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }
                        }

                        //set speed
                        m_currCMD = (byte)'L';
                        retval = await MarkControll.LoadSpeed(m_currCMD, currMarkInfo.currMarkData.pattern.scanValue.initSpeed4Scan, currMarkInfo.currMarkData.pattern.scanValue.targetSpeed4Scan, currMarkInfo.currMarkData.pattern.scanValue.accelSpeed4Scan, currMarkInfo.currMarkData.pattern.scanValue.decelSpeed4Scan);
                        if (retval.execResult != 0)
                        {
                            Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                            if (value == "0")
                                await plcComm.SendVisionResult("N");
                            else
                            {
                                await plcComm.SendVisionResult("O");
                                //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                            }

                            //log = "COMMUNICATION TO CONTROLLER ERROR. (LoadSpeed) : " + recvArg.execResult.ToString();
                            //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                            ////ShowLog(className, funcName, 2, stepstring + "-2] LOAD SCAN SPEED FAILURE", recvArg.execResult.ToString());
                            ////ITNTErrorCode();
                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed - L) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-L ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                            ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                            Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                            if (value == "0")
                                await plcComm.SendVisionResult("N");
                            else
                            {
                                await plcComm.SendVisionResult("O");
                                //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                            }
                            ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                            return retval;
                        }

                        //wait 1 second
                        // ?? TM SHIN
                        int WaitTime = 1000;
                        Util.GetPrivateProfileValue("VISION", "SCANWAITTIME", "1000", ref value, Constants.PARAMS_INI_FILE);
                        int.TryParse(value, out WaitTime);
                        if (WaitTime < 0)
                            WaitTime = 1000;
                        /////////////////////////////////////////////////////////
                        swdelay.Start();
                        //while (swdelay.Elapsed < TimeSpan.FromSeconds(1))
                        while (swdelay.Elapsed < TimeSpan.FromMilliseconds(WaitTime))
                        {
                            await Task.Delay(50);
                        }
                        swdelay.Stop();
                        /////////////////////////////////////////////////////////

                        //scan profiler
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE", Thread.CurrentThread.ManagedThreadId);
                            doingCommand = true;

                            m_currCMD = (byte)'M';
                            retval = await MarkControll.GoPoint((short)scanEnd, (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * currMarkInfo.currMarkData.pattern.headValue.stepLength + 0.5), 0);
                            if (retval.execResult != 0)
                            {
                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }

                                //log = "COMMUNICATION TO CONTROLLER ERROR. (GoPoint) : " + recvArg.execResult.ToString();
                                //ShowLog(className, funcName, 2, log, recvArg.execResult.ToString());
                                ////ShowLog(className, funcName, 2, stepstring + "-2] VISION SCAN FAILURE", recvArg.execResult.ToString());
                                ////ITNTErrorCode();

                                ////iResult.executeresult = retval;
                                ////ShowLog(className, funcName, 2, "[6-2] VISION 헤드 이동 실패", recvArg.execResult.ToString());
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MoveScanProfile START POS ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                                retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (GoPoint) ERROR = " + retval.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoPoint ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }

                            // ?? TM SHIN
                            swMove.Start();
                            while (swMove.Elapsed < TimeSpan.FromSeconds(6))
                            {
                                if (!doingCommand)
                                    break;

                                await Task.Delay(50);
                            }
                            swMove.Stop();

                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE : " + swMove.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);

                            //sw.Reset();
                            if (doingCommand)
                            {
                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("VISION SCANNER MOVING ERROR", false);
                                }
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCANNER MOVING ERROR", Thread.CurrentThread.ManagedThreadId);
                                doingCommand = false;

                                //ShowLog(className, funcName, 2, stepstring + "-2] MOVING VISION HEAD ERROR", recvArg.execResult.ToString());
                                //ITNTErrorCode();

                                retval.errorInfo.sErrorMessage = "VISION SCANNER MOVING ERROR";
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;

                                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                                Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                                if (value == "0")
                                    await plcComm.SendVisionResult("N");
                                else
                                {
                                    await plcComm.SendVisionResult("O");
                                    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                                }
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);

                                return retval;
                            }
                        }

                        Util.GetPrivateProfileValue("OPTION", "VISIONQUICKEND", "0", ref value, Constants.PARAMS_INI_FILE);
                        if (value != "0")
                        {
                            if (this.CheckAccess() == true)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND PLC VISION COMPLETE 1", Thread.CurrentThread.ManagedThreadId);
                                retval = await plcComm.SendScanComplete(1);
                            }
                            else
                            {
                                retval = await this.Dispatcher.Invoke(new Func<Task<ITNTResponseArgs>>(async delegate
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND PLC VISION COMPLETE 2", Thread.CurrentThread.ManagedThreadId);
                                    ITNTResponseArgs ret = new ITNTResponseArgs();
                                    ret = await plcComm.SendScanComplete(1);
                                    return ret;
                                }));
                            }
                        }

                        Thread scannerHomeThread = new Thread(new ParameterizedThreadStart(GoScannerLinkPosition));
                        scannerHomeThread.Start(currMarkInfo.currMarkData.pattern);
                    }
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("0004 END"), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                //ShowLog(className, funcName, 2, stepstring + "] VISION EXCEPTION : " + ex.Message, ex.HResult.ToString());
                ////ITNTErrorCode();

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;

                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                return retval;
            }

            return retval;
        }

        private async Task<ITNTResponseArgs> ExecuteProcessSignal_DATASHIFT()
        {
            string className = "MainWindow";
            string funcName = "ExecuteProcessSignal_DATASHIFT";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            DistanceData distData = new DistanceData();
            //int retval = 0;
            int count = 0;
            int totcount = 0;
            Stopwatch sw = new Stopwatch();
            string sCurrentFunc = "RECEIVE DATA SHIFT SIGNAL";
            string sProcedure = "08";
            //MESReceivedData mesData = new MESReceivedData();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("0008 START"), Thread.CurrentThread.ManagedThreadId);
                if (oldProcessStatus != 0)
                {
                    retval.execResult = -10;
                    //recvArg.sErrorMessage = "FIRST EXECUTE";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "FIRST EXECUTE", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                ShowLog(className, funcName, 0, "[8] RECEIVE DATA SHIFT SIGNAL");              ///??????

                count = 0;
                //count = GetMarkPlanDataCount4Thread(dgdPlanData);
                (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                if (count <= 0)
                {
                    //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("CANNOT FIND FA DATA 5"), Thread.CurrentThread.ManagedThreadId);
                    //ShowErrorMessage("NO MES DATA.", false);
                    m_bDoingMarkingFlag = false;
                    retval.execResult = -1;

                    retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                    return retval;
                }

                retval = await MakeCurrentMarkData(dgdPlanData);
                if (retval.execResult != 0)
                {
                    //if (currMarkInfo.currMarkData.mesData.errorMessage.Length <= 0)
                    //{
                    //    if (recvArg.sErrorMessage.Length > 0)
                    //        currMarkInfo.currMarkData.mesData.errorMessage = recvArg.sErrorMessage;
                    //    else
                    //        currMarkInfo.currMarkData.mesData.errorMessage = "CANNOT FIND FA DATA";
                    //}

                    //ShowLog(className, funcName, 2, "[8-1] CHECK MES DATA ERROR", recvArg.sErrorMessage);
                    ////ITNTErrorCode();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                    return retval;
                }

                //Change Check Flag
                retval = UpdateNextMarkData(currMarkInfo.currMarkData.mesData);
                ShowMarkingDataList(true, false);

                //Show Current data on Top
                //await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2, 1);
                await ShowCurrentSequenceVIN(1, currMarkInfo.currMarkData, (byte)DISPLAY_INFO_COLOR_TYPE.DISP_COLOR_NEXTVIN, 1);

                UpdateCompleteDatabaseThread(dgdPlanData, true, 1);
                //mesData = await GetCurrentMarkData(dgdPlanData);
                await ShowCarTypeSequence(currMarkInfo.currMarkData.mesData);

                oldProcessStatus = 64;
                ShowLog(className, funcName, 0, "[8] DATA SHIFT COMPLETE");              ///??????

                //Util.WritePrivateProfileValue("CURRENT", "VIN", currMarkInfo.currMarkData.mesData.vin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);
                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", currMarkInfo.currMarkData.mesData.sequence.Trim() + "|" + currMarkInfo.currMarkData.mesData.rawvin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("0008 END"), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                //ShowLog(className, funcName, 2, "DATA SHIFT FAILURE - EXCEPTION", ex.Message);
                //ITNTErrorCode();
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;


                ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                return retval;
            }
            return retval;
        }


        ///
        public async Task<byte> ShowWariningDialogAsync(CheckAreaData chkdata)
        {
            //byte retval = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "className", "ShowWariningDialogAsync", "START" + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
            Dispatcher dispatcher = Application.Current.Dispatcher;
            string markbutton = "";
            try
            {
                DispatcherOperation<byte> operation = dispatcher.InvokeAsync(() =>
                {
                    byte ret = 0;
                    ConfirmWindowString msg1 = new ConfirmWindowString();
                    ConfirmWindowString msg2 = new ConfirmWindowString();
                    ConfirmWindowString msg3 = new ConfirmWindowString();
                    ConfirmWindowString msg4 = new ConfirmWindowString();
                    ConfirmWindowString msg5 = new ConfirmWindowString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "className", "ShowWariningDialogAsync", "DispatcherOperation popup" + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);

                    msg1.Message = chkdata.errorInfo.sErrorMessage;
                    msg1.Fontsize = 20;
                    msg1.Foreground = Brushes.Red;

                    msg2.Message = "";
                    msg2.Fontsize = 18;

                    msg3.Message = "PRESS [RETRY] TO START MARKING AGAIN.";
                    msg3.Fontsize = 17;
                    msg3.Foreground = Brushes.Blue;
                    //msg3.HorizontalContentAlignment = HorizontalAlignment.Left;


                    if (chkdata.execResult == (int)ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR)
                        msg4.Message = "";
                    else
                        msg4.Message = "PRESS [CONTINUE] TO IGNORE ERROR AND MARK.";
                    msg4.Fontsize = 17;
                    msg4.Foreground = Brushes.Blue;
                    //msg4.HorizontalContentAlignment = HorizontalAlignment.Left;

                    msg5.Message = "PRESS [CANCEL] TO CANCEL MARKING.";
                    msg5.Fontsize = 17;
                    msg5.Foreground = Brushes.Blue;
                    //msg5.HorizontalContentAlignment = HorizontalAlignment.Left;

                    if (chkdata.execResult == (int)ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR)
                        markbutton = "";
                    else
                        markbutton = "CONTINUE";
                    var dialog = new WarningWindow2("SELECT", msg1, msg3, msg4, msg5, "RETRY", markbutton, "CANCEL", this);
                    bool? result = dialog.ShowDialog();

                    if (result == false)
                        ret = 2;
                    else
                    {
                        ret = (byte)dialog.Result;
                        //if (dialog.Result == WarningWindow2.DialogResultType.Retry)
                        //    ret = 0;
                    }
                    //if (dialog.Result == WarningWindow2.DialogResultType.Retry)
                    //    return (byte)1;
                    //else if (dialog.Result == WarningWindow2.DialogResultType.Continue)
                    //    return (byte)0;
                    //else
                    //    return (byte)2;



                    return ret;

                });
                byte result = await operation.Task;
                //Console.WriteLine(result);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "className", "ShowWariningDialogAsync", "END" + chkdata.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                return result;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "className", "ShowWariningDialogAsync", "EXCEPTION " + ex.Message, Thread.CurrentThread.ManagedThreadId);
                return 2;
            }

        }

        public async void threadGoVisionStart(object obj)
        {
            string className = "MainWindow";
            string funcName = "threadGoVisionStart";

            string value = "";
            int linktype = 0;
            CURRENTMARKDATA markInfo = new CURRENTMARKDATA();
            ITNTResponseArgs recvArg = new ITNTResponseArgs();

            double StartPosU = 0;
            double Step_Length_U = 100;
            Stopwatch swLink = new Stopwatch();
            Stopwatch swdelay = new Stopwatch();
            string log = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                markInfo = (CURRENTMARKDATA)obj;

                Util.GetPrivateProfileValue("CONFIG", "USELINK", "0", ref value, Constants.SCANNER_INI_FILE);
                int.TryParse(value, out linktype);
                if (linktype == 0)
                {
                    m_currCMD = (byte)'l';
                    recvArg = await MarkControll.LoadSpeed((byte)'l', markInfo.pattern.scanValue.initSpeed4Scan, markInfo.pattern.scanValue.targetSpeed4Scan, markInfo.pattern.scanValue.accelSpeed4Scan, markInfo.pattern.scanValue.decelSpeed4Scan);
                    if (recvArg.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-l ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);

                        Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                        if (value == "0")
                            await plcComm.SendVisionResult("N");
                        else
                        {
                            await plcComm.SendVisionResult("O");
                            ShowErrorMessage("COMMUNICATION TO CONTROLLER ERROR", false);
                        }
                        //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO CONTROLLER ERROR");
                        return;
                    }

                    m_currCMD = (byte)'f';
                    recvArg = await MarkControll.LoadSpeed((byte)'f', markInfo.pattern.scanValue.initSpeed4ScanFree, markInfo.pattern.scanValue.targetSpeed4ScanFree, markInfo.pattern.scanValue.accelSpeed4ScanFree, markInfo.pattern.scanValue.decelSpeed4ScanFree);
                    if (recvArg.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-f ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);

                        Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                        if (value == "0")
                            await plcComm.SendVisionResult("N");
                        else
                        {
                            await plcComm.SendVisionResult("O");
                            ShowErrorMessage("COMMUNICATION TO CONTROLLER ERROR", false);
                        }

                        ShowLog(className, funcName, 2, "GO VISION STARTING POINT ERROR - LOAD SPEED FAILURE", recvArg.execResult.ToString());
                        //ITNTErrorCode();

                        //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO CONTROLLER ERROR");
                        return;
                    }

                    StartPosU = markInfo.pattern.scanValue.startU;
                    Step_Length_U = markInfo.pattern.scanValue.stepLength_U;
                    StartPosU = StartPosU * Step_Length_U;
                    //short home_u = (short)(Step_Length_U * pattern.scanValue.home_U + 0.5);
                    //doingCommand = true;
                    //m_currCMD = (byte)'h';
                    //recvArg = await MarkControll.MoveScan2Home(home_u);
                    //if (recvArg.execResult != 0)
                    //{
                    //    m_bDoingMarkingFlag = false;
                    //    ShowErrorMessage("VISION SCAN ERROR", false);
                    //    //ITNTErrorLog.Instance.Trace(0, "VISION SCAN ERROR - MOVE HOME");
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCAN ERROR - MOVE HOME", Thread.CurrentThread.ManagedThreadId);

                    //    ShowLog(className, funcName, 2, stepstring + "-3] MOVING VISION HEAD TO HOME ERROR", recvArg.execResult.ToString());
                    //    //ITNTErrorCode();

                    //    return recvArg;
                    //}

                    ////Stopwatch sw = new Stopwatch();
                    //swdelay.Start();
                    //while (swdelay.Elapsed < TimeSpan.FromSeconds(6))
                    //{
                    //    if (!doingCommand)
                    //        break;

                    //    await Task.Delay(50);
                    //}
                    //swdelay.Stop();
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MOVE HOME : " + swdelay.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
                    //swdelay.Reset();

                    //vision start
                    //string sendmsg = "M5" + "S";
                    //recvArg.execResult = await SendData2Vision(1, markInfo.mesData, markInfo.pattern.name, currMarkInfo.distance, markInfo.multiMarkFlag, 1, bHeadType);
                    //if (recvArg.execResult != 0)
                    //{
                    //    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                    //    if (value == "0")
                    //        await plcComm.SendVisionResult("N");
                    //    else
                    //    {
                    //        await plcComm.SendVisionResult("O");
                    //        ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                    //    }

                    //    //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO VISION ERROR");
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO VISION ERROR", Thread.CurrentThread.ManagedThreadId);
                    //    ShowLog(className, funcName, 2, stepstring + "-3] SEND DATA TO VISION ERROR", recvArg.execResult.ToString());
                    //    //ITNTErrorCode();


                    //    return recvArg;
                    //}

                    //Thread.Sleep(1000);

                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE", Thread.CurrentThread.ManagedThreadId);
                    //short scanleng = (short)(StartPosU + markInfo.pattern.scanValue.scanLen * Step_Length_U + 0.5);
                    //m_currCMD = (byte)'U';
                    //doingCommand = true;

                    //recvArg = await MarkControll.ScanProfile(scanleng);
                    //if (recvArg.execResult != 0)
                    //{
                    //    m_bDoingMarkingFlag = false;
                    //    m_currCMD = 0;
                    //    ShowErrorMessage("VISION SCAN ERROR", false);
                    //    //ITNTErrorLog.Instance.Trace(0, "VISION SCAN ERROR");
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCAN ERROR", Thread.CurrentThread.ManagedThreadId);

                    //    ShowLog(className, funcName, 2, stepstring + "-3] MOVING VISION HEAD ERROR - SCAN ERROR", recvArg.execResult.ToString());
                    //    //ITNTErrorCode();

                    //    return recvArg;
                    //}

                    //swMove.Start();
                    //while (swMove.Elapsed < TimeSpan.FromSeconds(6))
                    //{
                    //    if (!doingCommand)
                    //        break;

                    //    await Task.Delay(50);
                    //}
                    //swMove.Stop();
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE : " + swMove.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
                    //swMove.Reset();
                    //if (doingCommand)
                    //{
                    //    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                    //    if (value == "0")
                    //        await plcComm.SendVisionResult("N");
                    //    else
                    //    {
                    //        await plcComm.SendVisionResult("O");
                    //        ShowErrorMessage("VISION SCANNER MOVING ERROR", false);
                    //    }
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCANNER MOVING ERROR - TIMEOUT", Thread.CurrentThread.ManagedThreadId);

                    //    ShowLog(className, funcName, 2, stepstring + "-3] MOVING VISION HEAD - TIMEOUT", recvArg.execResult.ToString());
                    //    //ITNTErrorCode();

                    //    doingCommand = false;
                    //}
                }
                else
                {   // link type Vision Profiler
                    short scanStart = 0;
                    short scanEnd = 0;
                    StartPosU = (markInfo.pattern.scanValue.startU * markInfo.pattern.scanValue.stepLength_U);

                    ShowLog(className, funcName, 0, "[LINK] READY TO EXECUTE VISION");

                    if (markInfo.pattern.scanValue.reverseScan == 0)
                    {
                        scanStart = (short)(StartPosU + 0.5);
                        scanEnd = (short)(StartPosU + markInfo.pattern.scanValue.scanLen * markInfo.pattern.scanValue.stepLength_U + 0.5);
                    }
                    else
                    {
                        scanEnd = (short)(StartPosU + 0.5);
                        scanStart = (short)(StartPosU + markInfo.pattern.scanValue.scanLen * markInfo.pattern.scanValue.stepLength_U + 0.5);
                    }

                    //1. Move Scan Header
                    m_currCMD = (byte)'M';
                    recvArg = await MarkControll.GoPoint((short)(markInfo.pattern.scanValue.linkPos * markInfo.pattern.headValue.stepLength + 0.5), (short)(markInfo.pattern.headValue.park3DPos.Y * markInfo.pattern.headValue.stepLength + 0.5), (short)(markInfo.pattern.headValue.park3DPos.Z * markInfo.pattern.headValue.stepLength + 0.5), 0);
                    if (recvArg.execResult != 0)
                    {
                        doingCommand = false;
                        m_currCMD = 0;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoParking ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);
                        log = "COMMUNICATION TO CONTROLLER (GoPoint) ERROR = " + recvArg.execResult.ToString();
                        ShowLog(className, funcName, 2, log);

                        //ShowLog(className, funcName, 2, "[LINK] MOVING VISION HEAD TO LINK POSITION ERROR", recvArg.execResult.ToString());
                        //ITNTErrorCode();

                        return;
                    }

                    //Set Link 
                    recvArg = await plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                    if (recvArg.execResult != 0)
                    {
                        log = "COMMUNICATION TO PLC (SetDiodeCurrent) ERROR = " + recvArg.execResult.ToString();
                        ShowLog(className, funcName, 2, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);

                        //ShowLog(className, funcName, 2, "[LINK] VISION LINK ON FAILURE", recvArg.execResult.ToString());
                        //ITNTErrorCode();

                        return;
                    }

                    //wait 400ms
                    swLink.Start();
                    while (swLink.ElapsedMilliseconds < 1000)
                    {
                        await Task.Delay(50);
                    }
                    swLink.Stop();
                    swLink.Reset();

                    swLink.Start();
                    while (swLink.ElapsedMilliseconds < 1000)        // ?? TM SHIN
                    {
                        //Get Link Status 
                        recvArg = await plcComm.ReadLinkStatusAsync();
                        if (recvArg.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);

                            log = "COMMUNICATION TO PLC (ReadLinkStatusAsync) ERROR = " + recvArg.execResult.ToString();
                            ShowLog(className, funcName, 2, log);
                            //ShowLog(className, funcName, 2, "[LINK] READ VISION LINK STATUS FAILURE", recvArg.execResult.ToString());
                            //ITNTErrorCode();

                            return;
                        }

                        if (recvArg.recvString.Length < 8)
                        {
                            log = "PLC DATA LENGTH INVALID (ReadLinkStatusAsync) : " + recvArg.recvString.Length + " - " + recvArg.recvString;
                            ShowLog(className, funcName, 2, log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", recvArg.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                            //ShowLog(className, funcName, 2, "[LINK] VISION LINK DATA LENGTH ERROR : " + recvArg.recvString, recvArg.execResult.ToString());
                            //ITNTErrorCode();

                            return;
                        }

                        if (recvArg.recvString.Substring(4, 4) == PLCControlManager.SIGNAL_PLC2PC_ON)
                        {
                            break;
                        }
                        await Task.Delay(100);
                    }

                    //Go to start position
                    //set speed
                    m_currCMD = (byte)'L';
                    recvArg = await MarkControll.LoadSpeed(m_currCMD, markInfo.pattern.speedValue.initSpeed4Fast, markInfo.pattern.speedValue.targetSpeed4Fast, markInfo.pattern.speedValue.accelSpeed4Fast, markInfo.pattern.speedValue.decelSpeed4Fast);
                    if (recvArg.execResult != 0)
                    {
                        log = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + recvArg.execResult.ToString();
                        ShowLog(className, funcName, 2, log);
                        //ShowLog(className, funcName, 2, "LOAD FAST SPEED ERROR", recvArg.execResult.ToString());
                        //ITNTErrorCode();

                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-l ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }

                    m_currCMD = (byte)'M';
                    recvArg = await MarkControll.GoPoint((short)scanStart, (short)(markInfo.pattern.headValue.park3DPos.Y * markInfo.pattern.headValue.stepLength + 0.5), (short)(markInfo.pattern.headValue.park3DPos.Z * markInfo.pattern.headValue.stepLength + 0.5), 0);
                    if (recvArg.execResult != 0)
                    {
                        log = "COMMUNICATION TO CONTROLLER (GoPoint) ERROR = " + recvArg.execResult.ToString();
                        ShowLog(className, funcName, 2, log);
                        //ShowLog(className, funcName, 2, "[LINK] MOVING VISION HEAD (START POSITION) FAILURE", recvArg.execResult.ToString());
                        //ShowLog(className, funcName, 2, "[3-3] 각인 데이터 생성 오류 - FLUSH 실패", recvArg.execResult.ToString());
                        //ITNTErrorCode();

                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MoveScanProfile START POS ERROR : " + recvArg.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }

                    //recvArg.execResult = await SendData2Vision(1, markInfo.mesData, markInfo.pattern.name, currMarkInfo.distance, markInfo.multiMarkFlag, 1, bHeadType);
                    //if (recvArg.execResult != 0)
                    //{
                    //    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                    //    if (value == "0")
                    //        await plcComm.SendVisionResult("N");
                    //    else
                    //    {
                    //        await plcComm.SendVisionResult("O");
                    //        //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                    //    }
                    //    //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO VISION ERROR");
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO VISION ERROR", Thread.CurrentThread.ManagedThreadId);

                    //    ShowLog(className, funcName, 2, "[LINK] SEND DATA TO VISION FAILURE", recvArg.execResult.ToString());
                    //    //ITNTErrorCode();

                    //    return;
                    //}


                    //ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_RUN_VISION);
                    //ShowLog(className, funcName, 0, "[LINK] START VISION");

                    ////set speed
                    //m_currCMD = (byte)'L';
                    //recvArg = await MarkControll.LoadSpeed(m_currCMD, markInfo.pattern.scanValue.initSpeed4Scan, markInfo.pattern.scanValue.targetSpeed4Scan, markInfo.pattern.scanValue.accelSpeed4Scan, markInfo.pattern.scanValue.decelSpeed4Scan);
                    //if (recvArg.execResult != 0)
                    //{
                    //    Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                    //    if (value == "0")
                    //        await plcComm.SendVisionResult("N");
                    //    else
                    //    {
                    //        await plcComm.SendVisionResult("O");
                    //        //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                    //    }

                    //    ShowLog(className, funcName, 2, "[LINK] LOAD SCAN SPEED FAILURE", recvArg.execResult.ToString());
                    //    //ITNTErrorCode();

                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-l ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);
                    //    return;
                    //}

                    ////wait 1 second
                    //// ?? TM SHIN
                    ///////////////////////////////////////////////////////////
                    //swdelay.Start();
                    //while (swdelay.Elapsed < TimeSpan.FromSeconds(1))
                    //{
                    //    await Task.Delay(50);
                    //}
                    //swdelay.Stop();
                    ///////////////////////////////////////////////////////////

                    ////scan profiler
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE", Thread.CurrentThread.ManagedThreadId);
                    //    doingCommand = true;

                    //    m_currCMD = (byte)'M';
                    //    recvArg = await MarkControll.GoPoint((short)scanEnd, (short)(markInfo.pattern.headValue.park3DPos.Y * markInfo.pattern.headValue.stepLength + 0.5), (short)(markInfo.pattern.headValue.park3DPos.Z * markInfo.pattern.headValue.stepLength + 0.5), 0);
                    //    if (recvArg.execResult != 0)
                    //    {
                    //        Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                    //        if (value == "0")
                    //            await plcComm.SendVisionResult("N");
                    //        else
                    //        {
                    //            await plcComm.SendVisionResult("O");
                    //            //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
                    //        }

                    //        ShowLog(className, funcName, 2, stepstring + "-2] VISION SCAN FAILURE", recvArg.execResult.ToString());
                    //        //ITNTErrorCode();

                    //        //iResult.executeresult = retval;
                    //        //ShowLog(className, funcName, 2, "[6-2] VISION 헤드 이동 실패", recvArg.execResult.ToString());
                    //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MoveScanProfile START POS ERROR : " + recvArg.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    //        return recvArg;
                    //    }

                    //    // ?? TM SHIN
                    //    swMove.Start();
                    //    while (swMove.Elapsed < TimeSpan.FromSeconds(6))
                    //    {
                    //        if (!doingCommand)
                    //            break;

                    //        await Task.Delay(50);
                    //    }
                    //    swMove.Stop();

                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SCANNER MOVE : " + swMove.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);

                    //    //sw.Reset();
                    //    if (doingCommand)
                    //    {
                    //        Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
                    //        if (value == "0")
                    //            await plcComm.SendVisionResult("N");
                    //        else
                    //        {
                    //            await plcComm.SendVisionResult("O");
                    //            //ShowErrorMessage("VISION SCANNER MOVING ERROR", false);
                    //        }
                    //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION SCANNER MOVING ERROR", Thread.CurrentThread.ManagedThreadId);
                    //        doingCommand = false;

                    //        ShowLog(className, funcName, 2, stepstring + "-2] MOVING VISION HEAD ERROR", recvArg.execResult.ToString());
                    //        //ITNTErrorCode();
                    //        recvArg.execResult = -7;

                    //        return recvArg;
                    //    }
                    //}

                    //Thread scannerHomeThread = new Thread(new ParameterizedThreadStart(GoScannerLinkPosition));
                    //scannerHomeThread.Start(markInfo.pattern);
                }


                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }



        private async Task<int> SendData2Vision(byte sendflag, MESReceivedData mesData, string patternName, DistanceData distData, byte multimarkflag, byte sOrder, byte bHeadType, byte byLaserNumber)
        {
            string className = "MainWindow";
            string funcName = "SendData2Vision";
            //
            ITNTSendArgs args = new ITNTSendArgs();
            string sendmsg = "";
            string seq = "";
            string rawtype = "";
            byte byUsePattern = 0;
            //string svin = "";
            //string retstring = "";
            string patName = "";
            //DistanceData distData = new DistanceData();
            //string cmdstring = "";
            string vinstring = "";
            int retval = 0;
            string dist1 = "";
            string dist2 = "";
            string value = "";
            string sMinPower = "00000";
            string sMaxPower = "00000";
            string sAvePower = "00000";
            byte byPowerSend = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                rawtype = mesData.rawcartype.PadRight(4, ' ');
                seq = mesData.sequence.PadRight(4, ' ');
                //svin = mesData.vin;
#if AGING_TEST_DATA
                Util.GetPrivateProfileValue("OPTION", "AGINGTESTVIN", " KMFZSZ7KANU905128 ", ref vinstring, Constants.PARAMS_INI_FILE);
                //vinstring = " KMFZSZ7KANU905128 ";
#else
                vinstring = mesData.markvin;
#endif
                if (vinstring.Length < 19)
                    vinstring = vinstring.PadRight(19, ' ');
                else if (vinstring.Length > 19)
                    vinstring = vinstring.Substring(0, 19);
                //dist1 = distData.sdistance1;
                if (distData.sdistance1.Length < 10)
                    dist1 = distData.sdistance1.PadRight(10, ' ');
                else if (distData.sdistance1.Length > 10)
                    dist1 = distData.sdistance1.Substring(0, 10);
                else
                    dist1 = distData.sdistance1;

                if (distData.sdistance2.Length < 10)
                    dist2 = distData.sdistance2.PadRight(10, ' ');
                else if (distData.sdistance2.Length > 10)
                    dist2 = distData.sdistance2.Substring(0, 10);
                else
                    dist2 = distData.sdistance2;

                if (iLaserPowerList.Count > 0)
                {
                    sMinPower = iLaserPowerList.Min().ToString("D5");
                    sMaxPower = iLaserPowerList.Max().ToString("D5");
                    sAvePower = ((int)iLaserPowerList.Average()).ToString("D5");
                }

                if (bHeadType != 0)
                {
                    byUsePattern = Util.GetPrivateProfileValueByte("OPTION", "USEPATTERN", 0, Constants.PARAMS_INI_FILE);
                    byPowerSend = Util.GetPrivateProfileValueByte("OPTION", "ISSENDLASERPOWER", 0, Constants.PARAMS_INI_FILE);
                    if (byUsePattern != 0)
                    {
                        patName = patternName.PadRight(16, ' ');
                        if (sendflag == 0)
                        {
                            if (byPowerSend != 0)
                                sendmsg = "C5" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + multimarkflag.ToString("D2") + sOrder.ToString("D2") + mesData.bodyno + byLaserNumber.ToString("D1") + sMinPower + sMaxPower + sAvePower;
                            else
                                sendmsg = "C3" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + multimarkflag.ToString("D2") + sOrder.ToString("D2");
                        }
                        else
                        {
                            if (byPowerSend != 0)
                                sendmsg = "C9" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + multimarkflag.ToString("D2") + sOrder.ToString("D2") + mesData.bodyno + byLaserNumber.ToString("D1") + sMinPower + sMaxPower + sAvePower;
                            else
                                sendmsg = "C7" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + multimarkflag.ToString("D2") + sOrder.ToString("D2");
                        }
                    }
                    else
                    {
                        if (sendflag == 0)
                            sendmsg = "C2" + seq + vinstring + rawtype + "1" + dist1 + dist2 + multimarkflag.ToString("D2") + sOrder.ToString("D2");
                        else
                            sendmsg = "C6" + seq + vinstring + rawtype + "1" + dist1 + dist2 + multimarkflag.ToString("D2") + sOrder.ToString("D2");
                    }
                }
                else
                {
                    if (bUseDispalcementSensor)
                    {
                        //distData = await GetDisplacementSensor(0);
                        byUsePattern = Util.GetPrivateProfileValueByte("OPTION", "USEPATTERN", 0, Constants.PARAMS_INI_FILE);
                        if (byUsePattern != 0)
                        {
                            patName = patternName.PadRight(16, ' ');
                            if (sendflag == 0)
                                sendmsg = "M3" + seq + vinstring + rawtype + "1" + distData.sdistance1 + distData.sdistance2 + patName;
                            else
                                sendmsg = "M7" + seq + vinstring + rawtype + "1" + distData.sdistance1 + distData.sdistance2 + patName;
                        }
                        else
                        {
                            if (sendflag == 0)
                                sendmsg = "M2" + seq + vinstring + rawtype + "1" + distData.sdistance1 + distData.sdistance2;
                            else
                                sendmsg = "M6" + seq + vinstring + rawtype + "1" + distData.sdistance1 + distData.sdistance2;
                        }
                    }
                    else
                    {
                        if (sendflag == 0)
                            sendmsg = "M0" + seq + vinstring + rawtype + "1";
                        else
                            sendmsg = "M5" + seq + vinstring + rawtype + "1";
                    }
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 SEND VISION DATA", Thread.CurrentThread.ManagedThreadId);
                args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
                args.sendString = sendmsg;
                args.dataSize = sendmsg.Length;
                Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if ((value == "0") || (value == "2"))
                    retval = await visionServer.SendMessage(args);
                else
                    retval = await visionClient.SendMessage(args);
                return retval;
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        private async Task ShowCarTypeSequence(MESReceivedData mesData)
        {
            ITNTResponseArgs recvArg = new ITNTResponseArgs();
            string sCarTypeRead = "";
            string value = "";
            string plcFrameType = "";
            string plcCaption = "";
            string plcCarType = "";
            string plcSequence = "";

            string mesFrameType = "";
            string mesCaption = "";
            string mesCarType = "";
            string mesSequence = "";

            try
            {
                Util.GetPrivateProfileValue("OPTION", "CARTYPECOMPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "0")
                {
                    //BODY TYPE(MES)
                    mesFrameType = GetFrameType4MES(mesData.rawcartype.Trim(), mesData.rawbodytype.Trim(), mesData.rawtrim.Trim(), "");
                    mesCaption = GetFrameTypeDescription(mesFrameType, 1);
                    mesCarType = mesCaption;
                }
                else if (value == "1")
                {
                    //BODY TYPE(MES)
                    mesFrameType = mesData.rawcartype.Trim();
                    mesCarType = mesFrameType;
                }
                else if (value == "2")         //HMC U51
                {
                    mesFrameType = GetCarTypeFromCarName(mesData.rawcartype.Trim(), "");
                    mesCaption = mesFrameType;
                    mesCarType = mesFrameType;
                }
                else
                {
                    mesFrameType = mesData.rawcartype.Trim();
                    mesCaption = mesData.rawcartype.Trim();
                    mesCarType = mesCaption;
                }

                mesSequence = mesData.sequence;

                recvArg = await plcComm.ReadPLCCarType(1);
                if ((recvArg.execResult == 0) && (recvArg.recvString.Length >= 8))
                {
                    sCarTypeRead = recvArg.recvString.Substring(4, 4);
                    plcFrameType = sCarTypeRead.Trim();

                    if (value == "0")
                    {
                        plcFrameType = plcFrameType.Replace("0", "");
                        plcCaption = GetFrameTypeDescription(plcFrameType);
                        plcCarType = plcCaption;
                    }
                    else if (value == "1")
                    {
                        plcCaption = plcFrameType;
                        plcCarType = plcCaption;
                    }
                    else if (value == "2")         //HMC U51
                    {
                        plcCaption = plcFrameType.Substring(2, 2);
                        plcFrameType = GetCarTypeFromCarName(plcCaption, plcCaption);
                        //plcFrameType = GetCarTypeFromPLC(plcCaption);
                        plcCarType = plcFrameType;
                    }
                    else
                    {
                        plcCaption = GetFrameTypeDescription(plcFrameType, 1);
                        plcCarType = plcCaption;
                    }
                }
                else
                {
                    plcCarType = mesCarType;
                }
                ShowLabelData(plcCarType, lblPLCCARTYPEValue);
                ShowLabelData(mesCarType, lblMESCARTYPEValue);


                recvArg = await plcComm.ReadPLCSequence();
                if ((recvArg.execResult == 0) && (recvArg.recvString.Length >= 8))
                    plcSequence = recvArg.recvString.Substring(4, 4);
                else
                    plcSequence = mesSequence;


                ShowLabelData(plcSequence, lblPLCSEQValue);
                ShowLabelData(mesSequence, lblMESSEQValue);
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onfoff">Link ON or OFF</param>
        /// <param name="offflag">Link ON 시 OFF 실행 여부</param>
        /// <param name="token"></param>
        /// <returns></returns>
        //        private async Task<ITNTResponseArgs>SetLinkAsync(byte onoff, byte offflag=PLCControlManager.SIGNAL_PC2PLC_OFF, int loglevel=0, CancellationToken token=default)
        //        {
        //            string className = "MainWindow";
        //            string funcName = "SetLinkAsync";
        //            Stopwatch sw1 = new Stopwatch();
        //            Stopwatch sw2 = new Stopwatch();
        //            Stopwatch swLink = new Stopwatch();
        //            Stopwatch swLink2 = new Stopwatch();

        //            ITNTResponseArgs retval = new ITNTResponseArgs(128);
        //            bool bOK = false;
        //            string lastStatus = "";

        //            try
        //            {
        //                if (offflag != 0)
        //                {
        //                    retval = await plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_OFF, loglevel, token);
        //                    if (retval.execResult != 0)
        //                    {
        //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //                        return retval;
        //                    }

        //#if TEST_DEBUG_PLC
        //                    Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_LINKSTATUS", "00FF" + "0000", "Test.ini");
        //#endif
        //                    swLink.Start();
        //                    while (swLink.ElapsedMilliseconds < 1000)
        //                    {
        //                        await Task.Delay(50);
        //                    }
        //                    swLink.Stop();

        //                    sw1.Start();
        //                    while (sw1.Elapsed < TimeSpan.FromMilliseconds(1000))
        //                    {
        //                        retval = await plcComm.ReadLinkStatusAsync();
        //                        if (retval.execResult != 0)
        //                        {
        //                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //                            return retval;
        //                        }

        //                        if (retval.recvString.Length < 8)
        //                        {
        //                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);
        //                            retval.execResult = -17;
        //                            return retval;
        //                        }

        //                        if (retval.recvString.Substring(4, 4) == PLCControlManager.SIGNAL_PLC2PC_OFF)
        //                        {
        //                            bOK = true;
        //                            break;
        //                        }

        //                        await Task.Delay(100);
        //                    }
        //                    sw1.Stop();

        //                    if (onoff == PLCControlManager.SIGNAL_PC2PLC_OFF)
        //                    {
        //                        return retval;
        //                    }
        //                }

        //                if (onoff == PLCControlManager.SIGNAL_PC2PLC_ON)
        //                    lastStatus = PLCControlManager.SIGNAL_PLC2PC_ON;
        //                else
        //                    lastStatus = PLCControlManager.SIGNAL_PLC2PC_OFF;

        //                retval = await plcComm.SetLinkAsync(onoff, loglevel, token);
        //                if (retval.execResult != 0)
        //                {
        //                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //                    return retval;
        //                }

        //#if TEST_DEBUG_PLC
        //                Util.WritePrivateProfileValue("PLC", "PLC_ADDRESS_LINKSTATUS", "00FF" + lastStatus, "Test.ini");
        //#endif
        //                swLink2.Start();
        //                while (swLink2.ElapsedMilliseconds < 1000)
        //                {
        //                    await Task.Delay(50);
        //                }
        //                swLink2.Stop();

        //                //sw.Reset();
        //                sw2.Start();
        //                while (sw2.Elapsed < TimeSpan.FromMilliseconds(2000))
        //                {
        //                    retval = await plcComm.ReadLinkStatusAsync();
        //                    if (retval.execResult != 0)
        //                    {
        //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //                        return retval;
        //                    }

        //                    if (retval.recvString.Length < 8)
        //                    {
        //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);
        //                        retval.execResult = -17;
        //                        return retval;
        //                    }

        //                    //value = retval.recvString.Substring(4, 4);
        //                    if (retval.recvString.Substring(4, 4) == lastStatus)
        //                    {
        //                        bOK = true;
        //                        break;
        //                    }

        //                    await Task.Delay(100);
        //                }
        //                sw2.Stop();

        //                if(bOK == false)
        //                {
        //                    retval.execResult = -19;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                retval.execResult = ex.HResult;
        //            }
        //            return retval;
        //        }


        private void ShowLog2(string clsName, string func, byte flag, string log, string error = "", LogLevel level = LogLevel.Info)
        {
            string className = "MainWindow";
            string funcName = "ShowLog";
            string trace = "";
            string logtrace = "";
            DateTime dt = DateTime.Now;
            Color color = new Color();

            try
            {
                trace = dt.ToString("MM-dd HH:mm:ss  ");
                switch (flag)
                {
                    case 0:                 // START
                        //trace += "[NORMAL]  ";
                        trace += log;
                        logtrace = log;
                        break;
                    case 1:                 // END
                        //trace += "SUCCESS";
                        trace += log;
                        logtrace = log;
                        break;
                    case 2:                 // ERROR
                        if (error.Length > 0)
                        {
                            trace += "[ERROR]  " + log + "(" + error + ")";
                            logtrace = "[ERROR]  " + log + "(" + error + ")";
                        }
                        else
                        {
                            trace += "[ERROR]  " + log;
                            logtrace = "[ERROR]  " + log;
                        }
                        break;
                    //case 3:
                    //    break;
                    //case 4:
                    //    break;
                    default:
                        logtrace = log;
                        trace += log;
                        break;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", clsName, func, "SHOWLOG : " + trace, Thread.CurrentThread.ManagedThreadId);

                if (LogListBox.CheckAccess())
                {
                    if (LogListBox.Items.Count > 2000)
                        LogListBox.Items.RemoveAt(0);

                    //LogEntries.Add(new LogEntry { Message = trace, Level = level });
                    if (LogListBox.Items.Count > 0)
                    {
                        LogListBox.SelectedIndex = LogListBox.Items.Count - 1;
                        LogListBox.ScrollIntoView(LogListBox.SelectedItem);
                    }
                }
                else
                {
                    LogListBox.Dispatcher.Invoke(new Action(delegate
                    {
                        if (LogListBox.Items.Count > 2000)
                            LogListBox.Items.RemoveAt(0);

                        //LogEntries.Add(new LogEntry { Message = trace, Level = level });
                        if (LogListBox.Items.Count > 0)
                        {
                            LogListBox.SelectedIndex = LogListBox.Items.Count - 1;
                            LogListBox.ScrollIntoView(LogListBox.SelectedItem);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                //ShowLog(2, cmd, "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowLog(string clsName, string func, byte flag, string log, string error = "")
        {
            string className = "MainWindow";
            string funcName = "ShowLog";
            string trace = "";
            string logtrace = "";
            DateTime dt = DateTime.Now;
            Color color = new Color();

            try
            {
                trace = dt.ToString("MM-dd HH:mm:ss  ");
                switch (flag)
                {
                    case 0:                 // START
                        //trace += "[NORMAL]  ";
                        trace += log;
                        logtrace = log;
                        break;
                    case 1:                 // END
                        //trace += "SUCCESS";
                        trace += log;
                        logtrace = log;
                        break;
                    case 2:                 // ERROR
                        if (error.Length > 0)
                        {
                            trace += "[ERROR]  " + log + "(" + error + ")";
                            logtrace = "[ERROR]  " + log + "(" + error + ")";
                        }
                        else
                        {
                            trace += "[ERROR]  " + log;
                            logtrace = "[ERROR]  " + log;
                        }
                        break;
                    //case 3:
                    //    break;
                    //case 4:
                    //    break;
                    default:
                        logtrace = log;
                        trace += log;
                        break;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", clsName, func, "SHOWLOG : " + trace, Thread.CurrentThread.ManagedThreadId);

                if (lbxLog.CheckAccess())
                {
                    if (lbxLog.Items.Count > 2000)
                        lbxLog.Items.RemoveAt(0);

                    lbxLog.Items.Add(trace);
                    if (lbxLog.Items.Count > 0)
                    {
                        lbxLog.SelectedIndex = lbxLog.Items.Count - 1;
                        lbxLog.ScrollIntoView(lbxLog.SelectedItem);
                    }
                }
                else
                {
                    lbxLog.Dispatcher.Invoke(new Action(delegate
                    {
                        if (lbxLog.Items.Count > 2000)
                            lbxLog.Items.RemoveAt(0);

                        lbxLog.Items.Add(trace);
                        if (lbxLog.Items.Count > 0)
                        {
                            lbxLog.SelectedIndex = lbxLog.Items.Count - 1;
                            lbxLog.ScrollIntoView(lbxLog.SelectedItem);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                //ShowLog(2, cmd, "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public string GetLaserStatus()
        {
            string status = "0";
            if (lblLaserError.CheckAccess() == true)
            {
                status = lblLaserError.Content.ToString();
            }
            else
            {
                status = lblLaserError.Dispatcher.Invoke(new Func<string>(delegate
                {
                    return lblLaserError.Content.ToString();
                }));
            }

            return status;
        }


        public string GetMotorStatus()
        {
            string status = "0";
            if (lblMotorError.CheckAccess() == true)
            {
                status = lblMotorError.Content.ToString();
            }
            else
            {
                status = lblMotorError.Dispatcher.Invoke(new Func<string>(delegate
                {
                    return lblMotorError.Content.ToString();
                }));
            }

            return status;
        }


        private async Task ITNTErrorCode(string className, string funcName, string sProcedure, ErrorInfo errorInfo, byte bSend2PLC = 1, byte showWindow = 0)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string errmsg = "";

            try
            {
                //errmsg = "[ERROR] [" + sProcedure + "] " + errorInfo.sErrorFunc + " : " + errorInfo.sErrorMessage;
                //errmsg = "[ERROR] [" + sProcedure + "] " + errorInfo.sErrorMessage;
                errmsg = "[ERROR] " + errorInfo.sErrorMessage;
                ShowErrorMessage(errmsg, false);

                //if(errorInfo.sErrorDetail1.Length > 0)
                //    errmsg = errmsg + " - " + errorInfo.sErrorDetail1;

                //if (errorInfo.sErrorDetail2.Length > 0)
                //    errmsg = errmsg + " - " + errorInfo.sErrorDetail2;

                //if (errorInfo.sErrorDetail3.Length > 0)
                //    errmsg = errmsg + " - " + errorInfo.sErrorDetail3;

                //if (errorInfo.sErrorDetail4.Length > 0)
                //    errmsg = errmsg + " - " + errorInfo.sErrorDetail4;

                //if (errorInfo.sErrorDetail5.Length > 0)
                //    errmsg = errmsg + " - " + errorInfo.sErrorDetail5;

                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, errmsg, "");              ///??????

                errmsg = "[ERROR] [" + sProcedure + "] " + errorInfo.sErrorMessage;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, errmsg, Thread.CurrentThread.ManagedThreadId);

                //ConfirmWindowString msg1 = new ConfirmWindowString();
                //ConfirmWindowString msg2 = new ConfirmWindowString();
                //ConfirmWindowString msg3 = new ConfirmWindowString();
                //ConfirmWindowString msg4 = new ConfirmWindowString();
                //ConfirmWindowString msg5 = new ConfirmWindowString();

                //msg2.Message = "THERE IS NO MARKING DATA";
                //msg2.Fontsize = 18;
                //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //msg2.Foreground = System.Windows.Media.Brushes.Red;
                //msg2.Background = System.Windows.Media.Brushes.White;

                //msg3.Message = "PLEASE REQUEST DATA TO MES TEAM";
                //msg3.Fontsize = 18;
                //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //msg3.Foreground = System.Windows.Media.Brushes.Blue;
                //msg3.Background = System.Windows.Media.Brushes.White;

                //ShowErrorMessageWindow(msg1, msg2, msg3, msg4, msg5);
                if (bSend2PLC != 0)
                    await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);

                //SaveErrorDB(errorInfo, sProcedure);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ITNTErrorCode", string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<ITNTResponseArgs> EmissionONDelegate()
        {
            string className = "MainWindow";
            string funcName = "EmissionONDelegate";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            Stopwatch sw = new Stopwatch();
            string log = "";
            LASERSTATUS Status = 0;
            bool bEmissionOn = false;
            string sCurrentFunc = "EMISSION ON";
            string sDevice = "PLC";

            try
            {
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                Util.GetPrivateProfileValue("LASER", "HWCONTROL", "0", ref value, Constants.PARAMS_INI_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "HWCONTROL = " + value, Thread.CurrentThread.ManagedThreadId);

                if (value != "0")
                {
                    sDevice = "PLC (SetEmissionOnOff) ERROR = ";
                    retval = await plcComm.SetEmissionOnOff(1);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SETTING EMISSION ON ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO " + sDevice + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }

                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromMilliseconds(1000))
                    {

                        retval = await laserSource.ReadDeviceStatus();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await laserSource.ReadDeviceStatus();
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            {
                                retval = await laserSource.ReadDeviceStatus();
                            }
                        }
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            log = "READ LASER STATUS. (RESULT = " + retval.execResult.ToString() + ")";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            return retval;
                        }

                        string[] st = retval.recvString.Split(':');
                        if (st.Length < 2)
                        {
                            log = "READ LASER STATUS. (STATUS STRING)";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            retval.execResult = -1;
                            return retval;
                        }
                        //2-1. Check Emission status
                        Status = (LASERSTATUS)UInt32.Parse(st[1]);
                        if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                        {
                            bEmissionOn = true;
                            break;
                            //ShowRectangle(EmissionLamp, Brushes.Red);
                            //EmissionLamp.Fill = Brushes.Black;
                        }
                        await Task.Delay(200);
                    }

                    if (bEmissionOn)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Emission ON OK", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = 0;
                        return retval;
                    }
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Emission ON FAIL", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -2;
                        return retval;
                    }
                }
                else
                {
                    sDevice = "LASER (StartEmission) ERROR = ";
                    retval = await laserSource.StartEmission();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await laserSource.StartEmission();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await laserSource.StartEmission();
                    }
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SETTING EMISSION ON ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO " + sDevice + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        private async Task<ITNTResponseArgs> EmissionON()
        {
            string className = "MainWindow";
            string funcName = "EmissionON";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            Stopwatch sw = new Stopwatch();
            string log = "";
            LASERSTATUS Status = 0;
            bool bEmissionOn = false;
            string sCurrentFunc = "EMISSION ON";
            string sDevice = "PLC";

            try
            {
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                Util.GetPrivateProfileValue("LASER", "HWCONTROL", "0", ref value, Constants.PARAMS_INI_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "HWCONTROL = " + value, Thread.CurrentThread.ManagedThreadId);

                if (CheckAccess() == true)
                {
                    retval = await EmissionONDelegate();
                }
                else
                {
                    retval = await Dispatcher.Invoke(new Func<Task<ITNTResponseArgs>>(async delegate
                    {
                        ITNTResponseArgs ret = new ITNTResponseArgs();
                        ret = await EmissionONDelegate();
                        return ret;
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        private async Task<ITNTResponseArgs> EmissionOFFDelegate()
        {
            string className = "MainWindow";
            string funcName = "EmissionOFFDelegate";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            Stopwatch sw = new Stopwatch();
            string log = "";
            LASERSTATUS Status = 0;
            bool bEmissionOff = false;
            string sCurrentFunc = "EMISSION OFF";
            string sDevice = "PLC";

            try
            {
                Util.GetPrivateProfileValue("LASER", "HWCONTROL", "0", ref value, Constants.PARAMS_INI_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "HWCONTROL = " + value, Thread.CurrentThread.ManagedThreadId);

                if (value != "0")
                {
                    sDevice = "PLC (SetEmissionOnOff) ERROR = ";
                    retval = await plcComm.SetEmissionOnOff(0);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SETTING EMISSION OFF ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO " + sDevice + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }

                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromMilliseconds(1000))
                    {
                        retval = await laserSource.ReadDeviceStatus();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await laserSource.ReadDeviceStatus();
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            {
                                retval = await laserSource.ReadDeviceStatus();
                            }
                        }
                        if (retval.execResult != 0)
                        {
                            log = "READ LASER STATUS. (RESULT = " + retval.execResult.ToString() + ")";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (ReadDeviceStatus) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            return retval;
                        }

                        string[] st = retval.recvString.Split(':');
                        if (st.Length < 2)
                        {
                            log = "READ LASER STATUS. (STATUS STRING)";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "READ LASER STATUS. (STATUS STRING LENGTH ERROR) : " + st.Length.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            retval.execResult = -1;
                            return retval;
                        }
                        //2-1. Check emission status
                        Status = (LASERSTATUS)UInt32.Parse(st[1]);
                        if ((Status & LASERSTATUS.EmissionOnOff) == 0)
                        {
                            bEmissionOff = true;
                            break;
                            //ShowRectangle(EmissionLamp, Brushes.Red);
                            //EmissionLamp.Fill = Brushes.Black;
                        }
                        await Task.Delay(200);
                    }

                    if (bEmissionOff)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "EMISSION OFF OK", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = 0;
                        return retval;
                    }
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "EMISSION OFF FAIL", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -2;
                        retval.errorInfo.sErrorMessage = "EMISSION OFF ERROR = EMISSION STATUS IS ON";
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        return retval;
                    }
                }
                else
                {
                    sDevice = "LASER (StartEmission) ERROR = ";
                    retval = await laserSource.StopEmission();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await laserSource.StopEmission();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await laserSource.StopEmission();
                    }
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SETTING EMISSION OFF ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO " + sDevice + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        private async Task<ITNTResponseArgs> EmissionOFF()
        {
            string className = "MainWindow";
            string funcName = "EmissionON";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            Stopwatch sw = new Stopwatch();
            string log = "";
            LASERSTATUS Status = 0;
            bool bEmissionOff = false;

            try
            {
                Util.GetPrivateProfileValue("LASER", "HWCONTROL", "0", ref value, Constants.PARAMS_INI_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "HWCONTROL = " + value, Thread.CurrentThread.ManagedThreadId);

                if (CheckAccess() == true)
                {
                    retval = await EmissionOFFDelegate();
                }
                else
                {
                    retval = await Dispatcher.Invoke(new Func<Task<ITNTResponseArgs>>(async delegate
                    {
                        ITNTResponseArgs ret = new ITNTResponseArgs();
                        ret = await EmissionOFFDelegate();
                        return ret;
                    }));
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        private async Task<ITNTResponseArgs> EmissionONOFFDelegate(byte emission)
        {
            string className = "MainWindow";
            string funcName = "EmissionOFFDelegate";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            Stopwatch sw = new Stopwatch();
            string log = "";
            LASERSTATUS Status = 0;
            bool bEmissionStatus = false;
            string sCurrentFunc = "EMISSION OFF";
            string sDevice = "PLC";
            Func<Task<ITNTResponseArgs>> execfunc = null;

            try
            {
                Util.GetPrivateProfileValue("LASER", "HWCONTROL", "0", ref value, Constants.PARAMS_INI_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "HWCONTROL = " + value, Thread.CurrentThread.ManagedThreadId);

                execfunc = (emission != 0) ? laserSource.StartEmission : laserSource.StopEmission;

                if (value != "0")
                {
                    //sDevice = "PLC (SetEmissionOnOff) ERROR = ";
                    retval = await plcComm.SetEmissionOnOff(emission);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SETTING EMISSION OFF ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO " + sDevice + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }

                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromMilliseconds(1000))
                    {
                        retval = await laserSource.ReadDeviceStatus();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await laserSource.ReadDeviceStatus();
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            {
                                retval = await laserSource.ReadDeviceStatus();
                            }
                        }
                        if (retval.execResult != 0)
                        {
                            log = "READ LASER STATUS. (RESULT = " + retval.execResult.ToString() + ")";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (ReadDeviceStatus) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            return retval;
                        }

                        string[] st = retval.recvString.Split(':');
                        if (st.Length < 2)
                        {
                            log = "READ LASER STATUS. (STATUS STRING)";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                            retval.errorInfo.sErrorMessage = "READ LASER STATUS. (STATUS STRING LENGTH ERROR) : " + st.Length.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            retval.execResult = -1;
                            return retval;
                        }
                        //2-1. Check emission status
                        Status = (LASERSTATUS)UInt32.Parse(st[1]);
                        bool bStatusflag = (emission != 0) ? ((Status & LASERSTATUS.EmissionOnOff) != 0) : ((Status & LASERSTATUS.EmissionOnOff) == 0);
                        if (bStatusflag == true)
                        {
                            bEmissionStatus = true;
                            break;
                            //ShowRectangle(EmissionLamp, Brushes.Red);
                            //EmissionLamp.Fill = Brushes.Black;
                        }
                        await Task.Delay(200);
                    }

                    if (bEmissionStatus)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "EMISSION ON/OFF OK", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = 0;
                        return retval;
                    }
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "EMISSION ON/OFF FAIL", Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -2;
                        retval.errorInfo.sErrorMessage = "EMISSION OFF ERROR = EMISSION STATUS IS ON";
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        return retval;
                    }
                }
                else
                {
                    sDevice = "LASER ERROR = ";
                    retval = await execfunc();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await execfunc();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await execfunc();
                    }
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SETTING EMISSION ON/OFF ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO " + sDevice + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        ////////////////////////////////////////////////////////
        /// <summary>
        /// 
        ///
        /// </summary>
        /// <returns></returns>
        private async Task<ITNTResponseArgs> MakeCurrentMarkData2()//, int bypassmode, byte displayflag)//, bool showFlag = true)
        {
            string className = "MainWindow";
            string funcName = "MakeCurrentMarkData2";

            string ErrorCode = "";
            MESReceivedData mesData = new MESReceivedData();
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string sCurrentFunc = "READ MRRK DATA FROM PLC";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                currMarkInfo.Initialize();

                mesData = await GetCurrentMarkDataFromPLC();
                if (mesData.execResult != 0)
                {
                    retval.execResult = mesData.execResult;
                    retval.errorInfo = (ErrorInfo)mesData.errorInfo.Clone();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetNextMarkDataInfomation ERROR = " + mesData.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                currMarkInfo.currMarkData.mesData = (MESReceivedData)mesData.Clone();

                if (currMarkInfo.currMarkData.mesData.userDataType == 3)
                {
                    currMarkInfo.currMarkData.isReady = true;
                    return retval;
                }

                //currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData);
                //currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.mesData.rawbodytype, currMarkInfo.currMarkData.mesData.rawtrim);
                currMarkInfo.currMarkData.pattern.name = GetPatternName(mesData.rawcartype);
                currMarkInfo.currMarkData.multiMarkFlag = 0;

#if MANUAL_MARK
                ImageProcessManager.GetPatternDataManual(currMarkInfo.currMarkData.pattern.name, currMarkInfo.currMarkData.mesData.rawcartype, ref currMarkInfo.currMarkData.pattern);
#else
                retval = ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
                if (retval.execResult != 0)
                {
                    //ShowLog(className, funcName, 2, "[] ", retval.sErrorMessage);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetPatternValue ERROR = " + retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    //ShowLog(className, funcName, 2, "GetPatternValue ERROR = " + retval.errorInfo.sErrorMessage);
                    return retval;
                }
#endif

                VinNoInfo vininfo = new VinNoInfo();
                vininfo.fontName = currMarkInfo.currMarkData.pattern.fontValue.fontName;
                vininfo.vinNo = currMarkInfo.currMarkData.mesData.markvin;
                vininfo.width = currMarkInfo.currMarkData.pattern.fontValue.width;
                vininfo.height = currMarkInfo.currMarkData.pattern.fontValue.height;
                vininfo.pitch = currMarkInfo.currMarkData.pattern.fontValue.pitch;
                vininfo.thickness = currMarkInfo.currMarkData.pattern.fontValue.thickness;

                retval = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref currMarkInfo.currMarkData.fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref ErrorCode);
                if (retval.execResult != 0)
                {
                    //if (retval.sErrorMessage.Length > 0)
                    //    log = retval.sErrorMessage;
                    //else
                    //    log = "GetFontDataEx ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetFontDataEx ERROR = " + retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                if ((bHeadType == 1) && (currMarkInfo.currMarkData.pattern.laserValue.density == 1))
                    GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);

                currMarkInfo.currMarkData.isReady = true;
                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowNextMarkData", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                //retval.sErrorMessage = " MAKING MARKING DATA EXCEPTION : " + ex.Message;
                return retval;
            }
        }

        private async Task<MESReceivedData> GetCurrentMarkDataFromPLC()
        {
            string className = "MainWindow";
            string funcName = "GetCurrentMarkDataFromPLC";

            MESReceivedData mesData = new MESReceivedData();
            ITNTResponseArgs recvCar = new ITNTResponseArgs(32);
            ITNTResponseArgs recvSeq = new ITNTResponseArgs(32);
            ITNTResponseArgs recvVIN = new ITNTResponseArgs(32);
            string sCurrentFunc = "READ MRRK DATA FROM PLC";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                mesData.userDataType = 0;

                ShowLog(className, funcName, 0, "[1-1] READ CAR TYPE");
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_READ_CARTYPE);
                recvCar = await plcComm.ReadPLCCarType();
                if (recvCar.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC Sequence Error(" + recvCar.execResult.ToString() + ") : " + recvCar.recvString, Thread.CurrentThread.ManagedThreadId);
                    mesData.errorInfo.sErrorMessage = "COOMUNICATION WITH PLC ERROR";
                    mesData.execResult = recvCar.execResult;
                    SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200).Wait();
                    ShowLog(className, funcName, 2, "SEND COMMAND TO PLC (ReadPLCCarType) ERROR : " + recvCar.execResult.ToString());
                    mesData.errorInfo.sErrorFunc = sCurrentFunc;

                    return mesData;
                }

                if (recvCar.recvString.Length < 8)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCCarType Length ERROR " + recvCar.recvString.Length + " - " + recvCar.recvString, Thread.CurrentThread.ManagedThreadId);

                    //mesData.execResult = -7;
                    mesData.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                    mesData.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + recvCar.recvString.Length + " - " + recvCar.recvString;
                    mesData.errorInfo.sErrorFunc = sCurrentFunc;
                    //log = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + retval.recvString.Length + " - " + retval.recvString;
                    //ShowLog(className, funcName, 2, log);
                    return mesData;
                }

                mesData.rawcartype = recvCar.recvString.Substring(4, 4);
                mesData.cartype = GetCarTypeFromNumber(mesData.rawcartype);

                ShowLabelData(mesData.rawcartype, lblPLCCARTYPEValue);
                //currMarkInfo.currMarkData.pattern.name = GetPatternName(mesData.rawcartype);

                //retval = ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
                //if (retval.execResult != 0)
                //{
                //    //retval.execResult = -8;
                //    //recvCar.sErrorMessage = "GET PATTERN VALUE ERROR";
                //    return retval;
                //}

                ShowLog(className, funcName, 0, "[1-2] READ SEQUENCE");

                recvSeq = await plcComm.ReadPLCSequence();
                if (recvSeq.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC Sequence Error(" + recvSeq.execResult.ToString() + ") : " + recvSeq.recvString, Thread.CurrentThread.ManagedThreadId);
                    //retval.errorMessage = "COOMUNICATION WITH PLC ERROR";
                    mesData.execResult = recvSeq.execResult;
                    mesData.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCCarType) ERROR = " + recvSeq.execResult.ToString();
                    mesData.errorInfo.sErrorFunc = sCurrentFunc;

                    //SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200).Wait();

                    //log = "COMMUNICATION TO PLC (ReadPLCSequence) ERROR = " + recvSeq.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    ////ShowLog(className, funcName, 2, "PLC - CONNECTION FAIL", recvSeq.execResult.ToString());
                    ////ITNTErrorCode();

                    return mesData;
                }

                if (recvSeq.recvString.Length < 8)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCSequence Length ERROR " + recvSeq.recvString.Length + " - " + recvSeq.recvString, Thread.CurrentThread.ManagedThreadId);
                    mesData.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                    mesData.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCSequence) : " + recvSeq.recvString.Length + " - " + recvSeq.recvString;
                    mesData.errorInfo.sErrorFunc = sCurrentFunc;
                    //log = "PLC DATA LENGTH INVALID (ReadPLCSequence) : " + recvSeq.recvString.Length + " - " + recvSeq.recvString;
                    //ShowLog(className, funcName, 2, log);
                    return mesData;
                }

                mesData.sequence = recvSeq.recvString.Substring(4, 4);//.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                ShowLabelData(mesData.sequence, lblPLCSEQValue);

                ShowLog(className, funcName, 0, "[1-3] READ VIN");
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_READ_VIN);
                recvVIN = await plcComm.ReadVINAsync();
                if (recvVIN.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC Sequence Error(" + recvVIN.execResult.ToString() + ") : " + recvVIN.recvString, Thread.CurrentThread.ManagedThreadId);
                    mesData.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadVINAsync) ERROR = " + recvVIN.execResult.ToString();
                    mesData.errorInfo.sErrorFunc = sCurrentFunc;
                    mesData.execResult = recvVIN.execResult;
                    ////retval.errorMessage = "COOMUNICATION WITH PLC ERROR";
                    //retval.execResult = -0x12;
                    //SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200).Wait();

                    //log = "COMMUNICATION TO PLC (ReadVINAsync) ERROR = " + recvCar.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    ////ShowLog(className, funcName, 2, "PLC - CONNECTION FAIL", recvVIN.execResult.ToString());
                    ////ITNTErrorCode();

                    return mesData;
                }

                if (recvVIN.recvString.Length >= 23)
                {
                    mesData.rawvin = recvVIN.recvString.Substring(4, 19);
                    mesData.markvin = AddMonthCode(mesData.rawvin);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadVINAsync Length ERROR " + recvVIN.recvString.Length + " - " + recvVIN.recvString, Thread.CurrentThread.ManagedThreadId);
                    mesData.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                    mesData.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadVINAsync) : " + recvVIN.recvString.Length + " - " + recvVIN.recvString;
                    mesData.errorInfo.sErrorFunc = sCurrentFunc;

                    //retval.execResult = -7;
                    //log = "PLC DATA LENGTH INVALID (ReadVINAsync) : " + recvVIN.recvString.Length + " - " + recvVIN.recvString;
                    //ShowLog(className, funcName, 2, log);
                    return mesData;
                }

                DateTime dateValue = DateTime.Now;// Convert.ToDateTime(rowview.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                mesData.productdate = dateValue.ToString("yyyy-MM-dd");


                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_SEND_MATCHRESULT);


                //retval.rawcartype = rowview.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                //retval.bodyno = rowview.Row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                //retval.vin = rowview.Row.ItemArray[Constants.DB_NAME_VIN].ToString();

                ////recv.mesdate = rowview.Row.rowview.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                ////recv.mestime = rowview.Row.rowview.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                //dateValue = Convert.ToDateTime(rowview.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                //DateTime timeValue = Convert.ToDateTime(rowview.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                mesData.mesdate = dateValue.ToString("yyyy-MM-dd");
                mesData.mestime = dateValue.ToString("HH:mm:ss");

                mesData.markdate = dateValue.ToString("yyyy-MM-dd");
                mesData.marktime = dateValue.ToString("HH:mm:ss");

                //retval.lastsequence = rowview.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                //retval.code219 = rowview.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                //retval.idplate = rowview.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                //retval.delete = rowview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                //retval.totalmsg = rowview.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                //retval.rawbodytype = rowview.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                //retval.rawtrim = rowview.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                //retval.region = rowview.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                //retval.bodytype = rowview.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                //retval.cartype = GetCarType(retval.rawcartype.Trim(), "");// rowview.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                //retval.plcvalue = rowview.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                ////recv.markdate = rowview.Row.rowview.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString();
                ////recv.marktime = rowview.Row.rowview.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString();
                //dateValue = Convert.ToDateTime(rowview.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                //timeValue = Convert.ToDateTime(rowview.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());

                mesData.remark = "N";
                mesData.exist = "Y";
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return mesData;
            }
            catch (Exception ex)
            {
                mesData.execResult = ex.HResult;
                mesData.errorInfo.sErrorMessage = "FIND DATA EXCEPTION : " + ex.Message;

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return mesData;
            }
        }

        public string GetCarTypeFromNumber(string rawCarType)
        {
            string value = "";
            string spos = "";
            int pos = 0;
            //string raw = "";
            string key = "";
            string key2 = "";
            string retval = "";
            try
            {
                if (rawCarType.Length <= 0)
                    return retval;

                key = rawCarType.Trim().Substring(0, 1);
                Util.GetPrivateProfileValue("CARTYPE", "USESUBVALCARTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                string[] vals = value.Split('|');
                if ((vals.Length > 0) && (vals.Contains(key) == true))
                {
                    Util.GetPrivateProfileValue("CARTYPE", "SUBVALPOS", "0", ref spos, Constants.PARAMS_INI_FILE);
                    int.TryParse(spos, out pos);
                    if ((pos >= rawCarType.Length) || (pos < 1))
                    {
                        Util.GetPrivateProfileValue("CARTYPE", key, "", ref value, Constants.PARAMS_INI_FILE);
                        if (value.Length <= 0)
                            retval = rawCarType;
                        else
                            retval = value;
                    }
                    else
                    {
                        key2 = key + rawCarType.Trim().Substring(pos, 1);
                        Util.GetPrivateProfileValue("CARTYPE", key2, "", ref value, Constants.PARAMS_INI_FILE);
                        if (value.Length <= 0)
                        {
                            Util.GetPrivateProfileValue("CARTYPE", key, "", ref value, Constants.PARAMS_INI_FILE);
                            if (value.Length <= 0)
                                retval = rawCarType;
                            else
                                retval = value;
                        }
                        else
                            retval = value;
                    }
                }
                else
                {
                    Util.GetPrivateProfileValue("CARTYPE", key, "", ref value, Constants.PARAMS_INI_FILE);
                    if (value.Length <= 0)
                        retval = rawCarType;
                    else
                        retval = value;
                }
            }
            catch (Exception ex)
            {
                if (rawCarType.Length > 0)
                    retval = rawCarType.Trim().Substring(0, 1);
                else
                    retval = rawCarType;
            }
            return retval;
        }


        private async Task<(ITNTResponseArgs, string, string, string, string)> CheckMarkingData(string stepstring, string sCurrentFunc)
        {
            string className = "MainWindow";
            string funcName = "CheckMarkingData";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int seqcomptype = 0;
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK CAR TYPE / SEQUENCE", Thread.CurrentThread.ManagedThreadId);

                //Check CAR Type
                ShowLog(className, funcName, 0, stepstring + "-1] COMPARE CAR TYPE");
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_CARTYPE);

                retval = await CheckCarTypeProcess(currMarkInfo.currMarkData.mesData, sCurrentFunc);
                if (retval.execResult != 0)
                {
                    m_bDoingMarkingFlag = false;
                    ShowLog(className, funcName, 2, stepstring + "-2] COMPARE CAR TYPE NG", retval.errorInfo.sErrorMessage);
                    //await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);
                    //await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                    //matchingError = 1;
                    //ITNTErrorCode();
                    ShowErrorMessage("COMPARE CAR TYPE NG" + retval.errorInfo.sErrorMessage, false);
                    return (retval, "----", "----", "----", "----");
                }

                //Check Sequence
                seqcomptype = (int)Util.GetPrivateProfileValueUINT("OPTION", "SEQCOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                if (seqcomptype == 2)
                    retval = await CheckSequence2(currMarkInfo.currMarkData.mesData.sequence, sCurrentFunc);
                //else if (seqcomptype == 1)
                //    recvArg = await CheckSequence(currMarkInfo.currMarkData.mesData.sequence);//, selMarkedRow.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString());
                else if (seqcomptype == 0)
                    retval.execResult = 0;

                if (retval.execResult != 0)
                {
                    seqcheckError = 1;
                    //ITNTErrorCode();
                    m_bDoingMarkingFlag = false;

                    //ITNTErrorCode(className, funcName, sProcedure, recvArg.errorInfo);
                    return (retval, "----", "----", "----", "----");
                }
                else
                    seqcheckError = 0;

                //Check Double Marking
                ShowLog(className, funcName, 0, stepstring + "-2] CHECK DOUBLE MARKING");
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_DOUBLE);
#if AGING_TEST_PLC
#else
                if ((currMarkInfo.currMarkData.mesData.userDataType != 3) && (currMarkInfo.currMarkData.mesData.isInserted == "0"))
                {
                    retval = await CheckDoubleMarking(currMarkInfo.currMarkData.mesData.rawvin, sCurrentFunc);
                    if (retval.execResult != 0)
                    {
                        m_bDoingMarkingFlag = false;

                        //ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        return (retval, "----", "----", "----", "----");
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                retval.errorInfo.sErrorFunc = "CheckMarkingData";
                return (retval, "----", "----", "----", "----");
            }

            return (retval, "----", "----", "----", "----");
        }

        private async Task<ITNTResponseArgs> CheckCarTypeProcess3(MESReceivedData mesData, string sCurrentFunc)
        {
            string className = "MainWindow";
            string funcName = "CheckCarTypeProcess";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            try
            {
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_CARTYPE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK CAR TYPE / SEQUENCE", Thread.CurrentThread.ManagedThreadId);

                retval = await CheckCarTypeProcess(currMarkInfo.currMarkData.mesData, sCurrentFunc);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK CAR TYPE / SEQUENCE NG", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                ShowLog(className, funcName, 0, "[1-2] COMPARE CAR TYPE OK");

                //Check Sequence
                int seqcomptype = (int)Util.GetPrivateProfileValueUINT("OPTION", "SEQCOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                if (seqcomptype == 2)
                    retval = await CheckSequence2(currMarkInfo.currMarkData.mesData.sequence, sCurrentFunc);
                //else if (seqcomptype == 1)
                //    recvArg.execResult = await CheckSequence(currMarkInfo.currMarkData.mesData.sequence);//, selMarkedRow.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString());
                else if (seqcomptype == 0)
                {
                    ShowLabelData(currMarkInfo.currMarkData.mesData.sequence, lblPLCSEQValue);
                    ShowLabelData(currMarkInfo.currMarkData.mesData.sequence, lblMESSEQValue);
                    retval.execResult = 0;
                }

                if (retval.execResult != 0)
                {
                    ShowLog(className, funcName, 2, "[1-2] SEQUENCE MATCHING NG", retval.errorInfo.sErrorMessage);
                    await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);
                    //await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                    //matchingError = 1;
                    seqcheckError = 1;
                    //ITNTErrorCode();
                    ShowErrorMessage("SEQUENCE MATCHING NG" + retval.errorInfo.sErrorMessage, false);
                    return retval;
                }
                else
                    seqcheckError = 0;
                ShowLog(className, funcName, 0, "[1-2] SEQUENCE MATCHING OK");

                //Check Double Marking
                //ShowLog(className, funcName, 0, "[1-3] 이중 각인 체크");
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMP_DOUBLE);
#if AGING_TEST_PLC
#else
                if ((currMarkInfo.currMarkData.mesData.userDataType != 3) && (currMarkInfo.currMarkData.mesData.isInserted == "0"))
                {
                    retval = await CheckDoubleMarking(currMarkInfo.currMarkData.mesData.rawvin, sCurrentFunc);
                    if (retval.execResult != 0)
                    {
                        m_bDoingMarkingFlag = false;
                        ShowLog(className, funcName, 2, "[1-3] DOUBLE MARKING CHECKING NG", retval.errorInfo.sErrorMessage);
                        await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);
                        //await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        //ITNTErrorCode();

                        ShowErrorMessage("DOUBLE MARKING CHECKING NG" + retval.errorInfo.sErrorMessage, false);

                        return retval;
                    }
                }
#endif
                ShowLog(className, funcName, 0, "[1-3] DOUBLE MARKING CHECKING OK");

                //Move Robot Signal (matching OK signal to PLC)
                ShowLog(className, funcName, 0, "[1-4] SEND MATCHING OK SIGNAL");
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_SEND_MATCHRESULT); return retval;
            }
            catch (Exception ex)
            {

            }
            return retval;
        }
    }
}
