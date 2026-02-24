using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Reflection;
//using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Windows.Media;
//using Imaging;
//using Media3D;
//using System.Windows.Shapes;
//using System.Windows.Threading;
using ITNTCOMMM;
using ITNTCOMMON;
using ITNTUTIL;
using System.Diagnostics;
using System.Windows.Media.Animation;
//using System.Drawing;
//using System.Windows.Shapes;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public partial class MainWindow : Window
    {

        static public int iPLCStatus = 0;

        double dLaserAveragePower = 0;
        int iLaserPeakPower = 0;
        byte byLaserConnStatus = 0; // 0 = disconnected, 1 = connected

        public async Task OnPLCDataReceivedCallBakHanlder(ITNTResponseArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnPLCDataReceivedCallBakHanlder";

            ITNTResponseArgs recvArg = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            string recvSignal = "";
            string value = "";
            MESReceivedData mesData = new MESReceivedData();
            DistanceData distData = new DistanceData();
            string type = "";

            try
            {
                //if (iPLCStatus != e.execResult)
                //{
                //    if (e.execResult != 0)
                //    {
                //        ShowLabelData("연결 실패", lblPLCConnectStatus, backbrush:Brushes.Red);
                //        ShowLog(className, funcName, 2, "PLC 연결 오류", iPLCStatus.ToString());
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PLC 연결 오류 : " + iPLCStatus.ToString(), Thread.CurrentThread.ManagedThreadId);

                //        return;
                //    }
                //    else
                //    {
                //        if(("연결 실패" == GetLabelData(lblPLCConnectStatus)) || ("" == GetLabelData(lblPLCConnectStatus)))
                //            ShowLabelData("연결 성공", lblPLCConnectStatus);
                //        //iPLCStatus = e.execResult;
                //        ShowLog(className, funcName, 2, "PLC 연결 성공", iPLCStatus.ToString());
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PLC 연결 성공", Thread.CurrentThread.ManagedThreadId);
                //    }

                //    iPLCStatus = e.execResult;
                //}

                if (e.recvSize <= 0)
                    return;

                if (e.recvType == 1)             //D100 Data
                {
                    Util.GetPrivateProfileValue("PLCCOMM", "AVAILABLESIGNAL", "0000|0001|0002|0003|0004|0008|0010|0020|0040|0080", ref value, Constants.PARAMS_INI_FILE);
                    string[] availableSignal = value.Split('|');
                    recvSignal = Encoding.UTF8.GetString(e.recvBuffer, 0, e.recvSize);//.recvString.Substring(4, 4);
                    if(recvSignal.Length < 8)
                    {
                        return;
                    }

                    recvSignal = recvSignal.Substring(4, 4);
                    //if ((recvSignal == recvStatus) && (seqcheckError == 0))
                    if (recvSignal == recvStatus)
                    {
                        return;
                    }

                    //ShowLabelData(recvSignal, lblPLCConnectStatus, backbrush: Brushes.Green);

                    if (bControllerInitFlag == 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONTROLLER NOT INITIALIZED", Thread.CurrentThread.ManagedThreadId);
                        return;
                    }

                    if ((Array.IndexOf(availableSignal, recvSignal) < 0))
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("recvSignal{0} NOT Available", recvSignal), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }

                    if (recvSignal != "0008")
                    {
                        recvStatus = recvSignal;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV DATA = {0}", recvSignal), Thread.CurrentThread.ManagedThreadId);
                    }
                    else
                    {
                        Util.GetPrivateProfileValue("OPTION", "USE08SIGNAL", "0", ref value, Constants.PARAMS_INI_FILE);
                        if (value != "0")
                            recvStatus = recvSignal;
                        else
                            return;
                    }

                    if (recvSignal == "0000")           //IDEL or Normal
                    {
                        recvArg = await ExecuteProcessSignal_READY();
                    }
                    else if (recvSignal == "0001")      //Next VIN
                    {
                        recvArg = await ExecuteProcessSignal_NEXTVIN();
                    }
                    else if (recvSignal == "0002")     //robot move ok
                    {
                        recvArg = await ExecuteProcessSignal_MARKING(1);
                    }
                    else if (recvSignal == "0004")           //robot move to vision position
                    {
                        recvArg = await ExecuteProcessSignal_VISION(1);
                    }
                    else if (recvSignal == "0008")          //Data Shift
                    {
                        recvArg = await ExecuteProcessSignal_DATASHIFT();
                    }
                    else if (recvSignal == "0032")     //robot move ok
                    {
                        recvArg = await ExecuteProcessSignal_MARKING(2);
                    }
                    else if (recvSignal == "0064")           //robot move to vision position
                    {
                        recvArg = await ExecuteProcessSignal_VISION(2);
                    }
                    else if (recvSignal == "0016")          //EMERGENCY STOP
                    {
                        //currentProcessStatus = 16;
                    }
                }
                else if (e.recvType == 2)
                {
                    //autoFlag = Encoding.UTF8.GetString(e.recvBuffer);//.recvString.Substring(4, 4);
                    //if (autoFlag.Length < 8)
                    //    return;

                    //autoFlag = autoFlag.Substring(4, 4);
                    //if (autoFlag == autoBack)
                    //{
                    //    return;
                    //}
                    //autoBack = autoFlag;
                    //if (autoFlag == "0001")
                    //    m_autoExecuteFlag = 1;          //auto
                    //else
                    //    m_autoExecuteFlag = 0;          //manual
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public async Task OnPLCStatusChangedHandler(ConnectionStatusChangedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnPLCStatusChangedHandler";
            SolidColorBrush brush;

            try
            {
                if ((e.newstatus == csConnStatus.Closed) ||
                    (e.newstatus == csConnStatus.Disconnected))
                {
                    ShowLabelData("CONNECTION FAIL", lblPLCConnectStatus, backbrush: Brushes.Red);
                }
                else if (e.newstatus == csConnStatus.Connected)
                {
                    ShowLabelData("CONNECTION SUCCESS", lblPLCConnectStatus, backbrush: Brushes.Green);
                }
                else if (e.newstatus == csConnStatus.Connecting)
                {
                    brush = new SolidColorBrush(Color.FromArgb(255, (byte)225, (byte)225, (byte)0));
                    ShowLabelData("CONNECTING", lblPLCConnectStatus, backbrush: brush);
                }
                else
                {
                    ShowLabelData("CONNECTION FAIL", lblPLCConnectStatus, backbrush: Brushes.Red);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("STATUS = {0}", e.newstatus), Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLabelData("CONNECTION FAIL", lblPLCConnectStatus, backbrush: Brushes.Red);
                //return ex.HResult;
            }
        }

        public void OnPLCStatusChangedEventHandler(DeviceStatusChangedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnPLCStatusChangedEventHandler";
            SolidColorBrush brush;

            try
            {
                if ((e.newstatus == csConnStatus.Closed) ||
                    (e.newstatus == csConnStatus.Disconnected))
                {
                    ShowLabelData("DISCONNECTED", lblPLCConnectStatus, backbrush: Brushes.Red);
                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_END, "PLC CONNECTION DISCONNECTED", "");
                }
                else if (e.newstatus == csConnStatus.Connected)
                {
                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "PLC CONNECTED", "");
                    ShowLabelData("CONNECTED", lblPLCConnectStatus, backbrush: Brushes.Green);
                }
                else if (e.newstatus == csConnStatus.Connecting)
                {
                    brush = new SolidColorBrush(Color.FromArgb(255, (byte)225, (byte)225, (byte)0));
                    ShowLabelData("CONNECTED", lblPLCConnectStatus, backbrush: brush);
                }
                else
                {
                    ShowLabelData("DISCONNECTED", lblPLCConnectStatus, backbrush: Brushes.Red);
                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_END, "PLC CONNECTION DISCONNECTED", "");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("STATUS = {0}", e.newstatus), Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLabelData("DISCONNECTED", lblPLCConnectStatus, backbrush: Brushes.Red);
                //return ex.HResult;
            }
        }

        public async void OnDisplaceStatusChangedEventHandler(DeviceStatusChangedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnPLCStatusChangedEventHandler";
            //SolidColorBrush brush;

            try
            {
                DisplaceStatusChangedFunction(e);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //ShowLabelData("DISCONNECTED", lblDisplaceConnectStatus, backbrush: Brushes.Red);
                //return ex.HResult;
            }
        }


        public async Task DisplaceStatusChangedFunction(DeviceStatusChangedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "DisplaceStatusChangedFunction";
            SolidColorBrush brush;

            try
            {
                if ((e.newstatus == csConnStatus.Closed) ||
                    (e.newstatus == csConnStatus.Disconnected))
                {
                    ShowLabelData("DISCONNECTED", lblDisplaceConnectStatus, backbrush: Brushes.Red);
                    ShowTextBlock(ConnectionStatusText, "DISCONNECTED", Brushes.White, Brushes.Red);
                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_END, "DISPLACE SENSOR IS DISCONNECTED", "");
                }
                else if (e.newstatus == csConnStatus.Connected)
                {
                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "DISPLACE SENSOR IS CONNECTED", "");
                    ShowLabelData("CONNECTED", lblDisplaceConnectStatus, backbrush: Brushes.Green);
                    ShowTextBlock(ConnectionStatusText, "CONNECTED", Brushes.White, Brushes.Green);
                }
                else if (e.newstatus == csConnStatus.Connecting)
                {
                    brush = new SolidColorBrush(Color.FromArgb(255, (byte)225, (byte)225, (byte)0));
                    ShowLabelData("CONNECTED", lblDisplaceConnectStatus, backbrush: brush);
                    ShowTextBlock(ConnectionStatusText, "CONNECTING", Brushes.White, Brushes.Yellow);
                }
                else
                {
                    ShowLabelData("DISCONNECTED", lblDisplaceConnectStatus, backbrush: Brushes.Red);
                    ShowTextBlock(ConnectionStatusText, "DISCONNECTED", Brushes.Black, Brushes.Gray);
                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_END, "DISPLACE SENSOR IS DISCONNECTED", "");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("STATUS = {0}", e.newstatus), Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLabelData("DISCONNECTED", lblDisplaceConnectStatus, backbrush: Brushes.Red);
                //return ex.HResult;
            }
        }

        private async Task OnDIODataArrivalEvent(uint uData)
        {
            ITNTResponseArgs recvArg = new ITNTResponseArgs(128);
            string className = "MainWindow";
            string funcName = "OnDIODataArrivalEvent";
            string msg = "";
            //int retval = 0;
            string log = "";
            string sCurrentFunc = "DIO DATA RECEIVE";

            try
            {
                msg = uData.ToString("X8");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV: " + msg, Thread.CurrentThread.ManagedThreadId);


                if (DIOControl.GetBitStatus(uData, DIO_IN_BUZZSTOP) == 1)
                {
                    DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 0);
                    m_BuzzerStop = true;
                    //ITNTJobLog.Instance.Trace(0, "Buzzer Stop..");
                }

                if (DIOControl.GetBitStatus(uData, DIO_IN_EMERGENCY) == 1)
                {
                    DIOControl.DIOWriteOutportBit(DIO_OUT_PRINTSTART, 0);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_ABORT, 1);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 1);

                    DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);

                    m_bDoingMarkingFlag = false;
                    m_bEmergencyFlag = true;
                    ShowErrorMessage("EMERGENCY STOP !!!", false);
                    //ITNTJobLog.Instance.Trace(0, "stop signal of emergency is received.!!");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECEIVE EMERGENCY STOP !!!", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (DIOControl.GetBitStatus(uData, DIO_IN_ERRRESET) == 1)
                {
                    ShowErrorMessage("", true);

                    if (m_bDoingMarkingFlag)
                    {
                        return;
                    }
                    else
                    {
                        DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 0);

                        DIOControl.DIOWriteOutportBit(DIO_OUT_CLAMP_SOL, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_UNCLAMP_SOL, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_ABORT, 0);
                        //m_Ready = false;
                        m_BuzzerStop = false;
                        m_bEmergencyFlag = false;

                        if (m_ManualClamp == true)
                            ShowCurrentStateLabelManual(1);
                        else
                            ShowCurrentStateLabelManual(0);
                        //ITNTJobLog.Instance.Trace(0, "Error Reset..");
                        return;
                    }
                }

                if (DIOControl.GetBitStatus(uData, DIO_IN_AIRPRESSURE) == 0)
                {

                    DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);

                    if (m_BuzzerStop == true)
                    {
                        DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 0);
                    }
                    else
                    {
                        DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 1);
                    }

                    ShowErrorMessage("공압 연결이 제대로 되지않았습니다 !!!", false);
                    //ITNTJobLog.Instance.Trace(0, "Air Pressure is abnormal");
                    return;
                }

                if ((DIOControl.GetBitStatus(uData, DIO_IN_UNCLAMP) == 1) || (DIOControl.GetBitStatus(uData, DIO_IN_M_UNCLAMP) == 1))
                {

                    if (m_bDoingMarkingFlag)
                    {
                        ShowErrorMessage("현재 마킹 중입니다 !!!", false);
                        return;
                    }

                    DIOControl.DIOWriteOutportBit(DIO_OUT_CLAMP_SOL, 0);
                    await Task.Delay(500);

                    DIOControl.DIOWriteOutportBit(DIO_OUT_UNCLAMP_SOL, 1);
                    await Task.Delay(500);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_UNCLAMP_SOL, 0);

                    TimeSpan maxDuration;
                    uint status = 0;

                    maxDuration = (UNCLAMP_TIMEOUT > 0) ? TimeSpan.FromSeconds(UNCLAMP_TIMEOUT) : TimeSpan.FromSeconds(1);

                    Stopwatch sw = Stopwatch.StartNew();

                    sw.Start();
                    while (sw.Elapsed < maxDuration)
                    {
                        await Task.Delay(10);
                        DIOControl.DIOReadInportBit((int)DIO_IN_SENS_UNCLAMP, ref status);
                        if (status == 1)
                            break;
                    }
                    sw.Stop();

                    if (status == 1)
                    {
                        ShowCurrentStateLabelManual(5);

                        m_ManualClamp = false;
                        m_ManualComplete = false;
                        //ITNTJobLog.Instance.Trace(0, "UnClamped...");
                    }
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "UNCLAMP_RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                        ShowErrorMessage("언클램프가 동작하지 않습니다 !!!", false);

                        DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);

                        if (m_BuzzerStop == true)
                        {
                            DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 0);
                        }
                        else
                        {
                            DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 1);
                        }
                    }

                    return;
                }

                if ((DIOControl.GetBitStatus(uData, DIO_IN_CLAMP) == 1) || (DIOControl.GetBitStatus(uData, DIO_IN_M_CLAMP) == 1))
                {
                    if (m_bEmergencyFlag)
                    {
                        ShowErrorMessage("비상정지가 해제되지 않아 클램프가 불가합니다 !!!", false);
                        return;
                    }

                    if (m_bDoingMarkingFlag)
                    {
                        ShowErrorMessage("현재 마킹 중입니다 !!!", false);
                        return;
                    }
#if DEBUG_SEQ
#else
                    DIOControl.DIOWriteOutportBit(DIO_OUT_UNCLAMP_SOL, 0);
                    await Task.Delay(500);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_CLAMP_SOL, 1);
                    await Task.Delay(500);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_CLAMP_SOL, 0);

                    TimeSpan maxDuration;
                    uint status = 0;

                    maxDuration = (CLAMP_TIMEOUT > 0) ? TimeSpan.FromSeconds(CLAMP_TIMEOUT) : TimeSpan.FromSeconds(1);

                    Stopwatch sw = Stopwatch.StartNew();

                    sw.Start();
                    while (sw.Elapsed < maxDuration)
                    {
                        await Task.Delay(10);
                        DIOControl.DIOReadInportBit((int)DIO_IN_SENS_CLAMP, ref status);
                        if (status == 1)
                            break;
                    }
                    sw.Stop();

                    if (status == 1)
                    {
                        ShowCurrentStateLabelManual(1);

                        m_ManualClamp = true;
                        m_ManualComplete = false;
                        //ITNTJobLog.Instance.Trace(0, "Clamped...");
                    }
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CLAMP_RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                        ShowErrorMessage("클램프가 동작하지 않습니다 !!!", false);

                        DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);

                        if (m_BuzzerStop == true)
                        {
                            DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 0);
                        }
                        else
                        {
                            DIOControl.DIOWriteOutportBit(DIO_OUT_BUZZER, 1);
                        }
                    }
                    //#else
                    //                ShowCurrentStateLabelManual(1);

                    //                m_ManualClamp = true;
                    //                m_ManualComplete = false;
                    //                //ITNTJobLog.Instance.Trace(0, "Clamped...");
#endif
                    return;
                }

                if (DIOControl.GetBitStatus(uData, DIO_IN_NEXT_VIN) == 1)
                {
                    if (m_bEmergencyFlag)
                    {
                        ShowErrorMessage("비상정지가 해제되지 않아 클램프가 불가합니다 !!!", false);
                        return;
                    }

                    if (m_bDoingMarkingFlag)
                    {
                        ShowErrorMessage("현재 마킹 중입니다 !!!", false);
                        return;
                    }

                    NextVINMarkPonit();
                    return;
                }

                if ((DIOControl.GetBitStatus(uData, DIO_IN_MARKSTART) == 1) || (DIOControl.GetBitStatus(uData, DIO_IN_M_MARKSTART) == 1))
                {
                    //if (!m_Ready)
                    //{
                    //    ShowErrorMessage("Cycle not Ready !!!", false);
                    //    return;
                    //}
                    uint status = 0;

                    if (m_bDoingMarkingFlag)
                        return;

                    if (m_ManualClamp == false)
                    {
                        ShowErrorMessage("클램프를 먼저 해야합니다 !!!", false);
                        return;
                    }
                    else
                        ShowErrorMessage("", true);

                    DIOControl.DIOReadInportBit((int)DIO_IN_SENS_CLAMP, ref status);
                    if (status == 0)
                    {
                        ShowErrorMessage("클램프 센서가 체크되지 않았습니다. !!!", false);
                        return;
                    }
                    else
                        ShowErrorMessage("", true);

                    string ErrorCode = "";
                    currMarkInfo.Initialize();
                    GetMarkDataInfomation(dgdPlanData, ref currMarkInfo.currMarkData.mesData);
                    //currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData);
                    currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.mesData.rawbodytype, currMarkInfo.currMarkData.mesData.rawtrim);
#if MANUAL_MARK
                ImageProcessManager.GetPatternDataManual(currMarkInfo.currMarkData.patternName, currMarkInfo.currMarkData.mesData.rawcartype, ref currMarkInfo.currMarkData.pattern);
#else
                    ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
#endif
                    //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
                    List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
                    //PatternValueEx pattern = new PatternValueEx();
                    ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
                    VinNoInfo vininfo = new VinNoInfo();
                    vininfo.fontName = currMarkInfo.currMarkData.pattern.fontValue.fontName;
                    vininfo.vinNo = currMarkInfo.currMarkData.mesData.markvin;
                    vininfo.width = currMarkInfo.currMarkData.pattern.fontValue.width;
                    vininfo.height = currMarkInfo.currMarkData.pattern.fontValue.height;
                    vininfo.pitch = currMarkInfo.currMarkData.pattern.fontValue.pitch;
                    vininfo.thickness = currMarkInfo.currMarkData.pattern.fontValue.thickness;

                    //byte fontdirection = 0; string value = "";
                    //Util.GetPrivateProfileValue("OPTION", "FONTDIRECTION", "0", ref value, Constants.PARAMS_INI_FILE);
                    //byte.TryParse(value, out fontdirection);

                    recvArg = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref ErrorCode);
                    if (recvArg.execResult != 0)
                    {
                        //if (recvArg.sErrorMessage.Length > 0)
                        //    log = recvArg.sErrorMessage;
                        //else
                        //    log = "GET FONT DATA ERROR : " + recvArg.execResult.ToString();
                        //ShowLog(className, funcName, 2, log);
                        //recvArg.errorInfo.
                        ITNTErrorCode(className, funcName, sCurrentFunc, recvArg.errorInfo);
                        return;
                    }
                    //for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++)
                    //{
                    //    List<FontDataClass> FontDataClass = new List<FontDataClass>();
                    //    ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.currMarkData.pattern.fontValue.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
                    //    currMarkInfo.currMarkData.fontData.Add(fontData);
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "", string.Format("FONT DATA {0}CH, {1}PT", i, fontData.Count));
                    //}
                    GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);

                    currMarkInfo.currMarkData.isReady = true;
                    //await ShowCurrentMarkingInformation(-1, currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.mesData.sequence, currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.pattern, 2);
                    await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2, 1);

                    //Check Plan Data List
                    ShowCurrentStateLabelManual(2);
                    //Doing Mark Flag On
                    m_bDoingMarkingFlag = true;
                    int count = 0;
                    int totcount = 0;
                    //lock (getCountLock)
                    {
                        //count = GetMarkPlanDataCount(dgdPlanData);
                        (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                    }

                    if (count <= 0)
                    {
                        //ITNTJobLog.Instance.Trace(0, "THERE IS NO MES DATA !!!");
                        //ITNTErrorLog.Instance.Trace(0, "THERE IS NO MES DATA !!!");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "", "THERE IS NO MES DATA !!!", Thread.CurrentThread.ManagedThreadId);
                        ShowErrorMessage("THERE IS NO MES DATA !!!", false);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);
                        return;
                    }

                    //Check Double Marking
                    recvArg = await CheckDoubleMarking(currMarkInfo.currMarkData.mesData.rawvin, sCurrentFunc);
                    if (recvArg.execResult > 0)
                    {
                        m_bDoingMarkingFlag = false;
                        //ShowErrorMessage("이중각인 데이터가 감지되었습니다 !!!", false);

                        //ShowLog(className, funcName, 2, "이중각인 감지", recvArg.sErrorMessage);
                        return;
                    }
                    else if (recvArg.execResult != 0)
                    {
                        m_bDoingMarkingFlag = false;
                        return;
                    }

                    //Display Current Mark Data
                    //currMarkInfo.Initialize();
                    //GetMarkDataInfomation(ref currMarkInfo.currMarkData.mesData);
                    //currMarkInfo.currMarkData.patternName = GetPatternName(currMarkInfo.currMarkData.mesData);
                    //ImageProcessManager.GetPatternData(currMarkInfo.currMarkData.patternName, ref currMarkInfo.currMarkData.pattern);

                    ////string ErrorCode = "";

                    //for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++)
                    //{
                    //    List<FontDataClass> FontDataClass = new List<FontDataClass>();
                    //    ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.currMarkData.pattern.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
                    //    currMarkInfo.currMarkData.fontData.Add(fontData);
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("FONT DATA {0}CH, {1}PT", i, fontData.Count));
                    //}
                    //currMarkInfo.isReady = true;
                    //ShowCurrentMarkingInformation(currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.mesData.sequence, currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.pattern);

                    //send pattern
                    //Send Data to micro contoller
                    ShowCurrentStateLabelManual(3);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);
                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 1);
#if DEBUG_SEQ
#else
                    m_currCMD = (byte)'B';
                    recvArg = await MarkControll.FontFlush();
                    if (recvArg.execResult != 0)
                    {
                        //m_bDoingNextVINFlag = false;
                        m_bDoingMarkingFlag = false;
                        ShowErrorMessage("마킹기로 FLUSH 명령 전송 오류 발생", false);
                        //ITNTErrorLog.Instance.Trace(0, "마킹기로 FLUSH 명령 전송 오류 발생");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "마킹기로 FLUSH 명령 전송 오류 발생", Thread.CurrentThread.ManagedThreadId);

                        DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);
                        return;
                    }

                    recvArg = await SendFontData(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern.name);
                    if (recvArg.execResult != 0)
                    {
                        //m_bDoingNextVINFlag = false;
                        m_bDoingMarkingFlag = false;
                        ShowErrorMessage("마킹기로 VIN 전송 오류 발생", false);
                        //ITNTErrorLog.Instance.Trace(0, "마킹기로 VIN 전송 오류 발생");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "마킹기로 VIN 전송 오류 발생", Thread.CurrentThread.ManagedThreadId);

                        DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);
                        return;
                    }

                    recvArg.execResult = await MarkControll.StartMark2MarkController(0);
                    if (recvArg.execResult != 0)
                    {
                        ShowErrorMessage("마킹기로 각인시작 명령 전송 오류 발생", false);
                        //ITNTErrorLog.Instance.Trace(0, "마킹기로 각인시작 명령 전송 오류 발생");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "마킹기로 각인시작 명령 전송 오류 발생", Thread.CurrentThread.ManagedThreadId);

                        DIOControl.DIOWriteOutportBit(DIO_OUT_RED, 1);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                        DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 0);
                        return;
                    }

                    //ShowCurrentStateLabel(4);
                    ShowCurrentStateLabelManual(4);
#endif
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        
        public async void OnLaserConnectionStatusChangedEventReceivedFunc(object sender, ConnectionStatusChangedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnLaserConnectionStatusChangedEventReceivedFunc";
            //Brush brushesBorder = Brushes.Black;
            //LASERSTATUS Status = 0;
            //UInt32 ists = 0;
            //ITNTResponseArgs retval = new ITNTResponseArgs();
            SolidColorBrush brush;

            try
            {
                if((e.newstatus == csConnStatus.Closed) || 
                    (e.newstatus == csConnStatus.Disconnected))
                {
                    ShowLabelData("DISCONNECTED", lblLaserConnectStatus, backbrush: Brushes.Red);
                    if (byLaserConnStatus != 0)
                    {
                        await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON); // PLC에 레이저 파워 OFF 상태로 셋팅
                    }
                    byLaserConnStatus = 0;
                    laserStatusBack = "00000";
                }
                else if(e.newstatus == csConnStatus.Connected)
                {
                    ShowLabelData("CONNECTED", lblLaserConnectStatus, backbrush: Brushes.Green);
                    byLaserConnStatus = 1;
                }
                else if(e.newstatus == csConnStatus.Connecting)
                {
                    brush = new SolidColorBrush(Color.FromArgb(255, (byte)225, (byte)225, (byte)0));
                    ShowLabelData("CONNECTING", lblLaserConnectStatus, backbrush: brush);
                }
                else
                {
                    if (byLaserConnStatus != 0)
                    {
                        await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON); // PLC에 레이저 파워 OFF 상태로 셋팅
                    }
                    byLaserConnStatus = 0;
                    laserStatusBack = "00000";
                    ShowLabelData("DISCONNECTED", lblLaserConnectStatus, backbrush: Brushes.Red);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("STATUS = {0}", e.newstatus), Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLabelData("DISCONNECTED", lblLaserConnectStatus, backbrush: Brushes.Red);
                //return ex.HResult;
                if (byLaserConnStatus != 0)
                {
                    await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON); // PLC에 레이저 파워 OFF 상태로 셋팅
                }
                byLaserConnStatus = 0;
                laserStatusBack = "00000";
            }
        }


        public async void OnLaserControllerStatusChangedEventReceivedFunc(object sender, LaserControllerStatusEvnetArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnLaserControllerStatusChangedEventReceivedFunc";

            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                //Thread laserInfoThread = new Thread(new ParameterizedThreadStart(LaserControllerStatusChangedEventThread));
                //laserInfoThread.Start(e);
                LaserControllerStatusChangedEventThread(e);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //return ex.HResult;
            }
        }

        public async Task LaserControllerStatusChangedEventThread(object obj)
        {
            string className = "MainWindow";
            string funcName = "OnLaserControllerStatusChangedEventReceivedFunc";
            System.Windows.Media.Brush brushesBorder = System.Windows.Media.Brushes.Black;
            //LASERSTATUS Status = 0;
            UInt32 ists = 0;
            uint uErrorsts = 0;
            //ITNTResponseArgs retval = new ITNTResponseArgs();
            LaserControllerStatusEvnetArgs e = (LaserControllerStatusEvnetArgs)obj;

            try
            {
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (e.datatype == 0)
                {
                    if (e.recvdata1 == laserStatusBack)
                        return;

                    //ShowLabelData(e.recvdata1, lblLaserStatusValue);

                    UInt32.TryParse(e.recvdata1, out ists);

                    uErrorsts = ists & (UInt32)LASERSTATUS.StatusError;
                    if (uErrorsts != 0)
                    {
                        ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Red, null, brushesBorder);

                        if (uErrorsts == uLaserStatusErrorBack)
                        {
                            laserStatusBack = e.recvdata1;
                            return;
                        }

                        ShowLog(className, funcName, 0, laserStatusBack + " -> " + e.recvdata1);
                        uLaserStatusErrorBack = uErrorsts;
                        //ErrorLaserSource = true;
                        //MarkControll.SetLaserErrorStatus(true);
                        ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);

                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : " + ists.ToString("X8"), "");

                        if ((ists & (UInt32)LASERSTATUS.CommandBufferOverload) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Command Buffer Overload (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.OverHeat) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Over Heat (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.HighBackReflectionLevel) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Back Reflection Level " + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.PulseTooLong) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Pulse Too Long (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) != 0)
                        {
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Out Of Range (" + ists.ToString("X8") + ")", "");
                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON, 1);
                        }
                        else
                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF, 1);

                        if ((ists & (UInt32)LASERSTATUS.HighPulseEnergy) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Pulse Energy (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.FiberInterlockActiveOK) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Internal Power Supply Failure (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.DutyCycleTooHigh) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Duty Cycle Too High (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.LowTemerature) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Low Temerature (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyFailure) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Failure (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.GNDLeakge) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : GND Leakge (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.AimingBeamAlarm) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Aiming Beam Alarm (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.CriticalError) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Critical Error (" + ists.ToString("X8") + ")", "");

                        if ((ists & (UInt32)LASERSTATUS.HighAveragePower) != 0)
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Average Power (" + ists.ToString("X8") + ")", "");
                        laserStatusBack = e.recvdata1;
                    }
                    else
                    {
                        //ErrorLaserSource = false;
                        //MarkControll.SetLaserErrorStatus(false);

                        ShowLabelData("NORMAL", lblLaserStatus, Brushes.Black, Brushes.Green);//, brushesBorder);
                        ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Black, null, brushesBorder);
                        if ((uLaserStatusErrorBack != 0) || (laserStatusBack == "00000"))
                        {
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER NORMAL : " + ists.ToString("X8"), "");
                            if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) == 0)
                                await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        }

                        laserStatusBack = e.recvdata1;
                        if (uErrorsts != uLaserStatusErrorBack)
                            uLaserStatusErrorBack = uErrorsts;
                    }

                    //Emi
                    if ((ists & (UInt32)LASERSTATUS.EmissionOnOff) != 0)
                        ShowRectangle(Brushes.Red, EmissionLamp);
                    else
                        ShowRectangle(Brushes.Black, EmissionLamp);

                    if ((ists & (UInt32)LASERSTATUS.AimingBeamOnOff) != 0)
                        ShowRectangle(Brushes.Red, AimingLamp);
                    else
                        ShowRectangle(Brushes.Black, AimingLamp);
                    laserStatusBack = e.recvdata1;
                }


                if (e.datatype == 1)
                {
                    if (e.recvdata1 != laserPowerBack1)
                    {
                        double dtmp = 0.0;
                        double.TryParse(e.recvdata1, out dtmp);
                        if (dLaserAveragePower < dtmp)
                        {
                            dLaserAveragePower = dtmp;
                            ShowLabelData(e.recvdata1, lblAvgPowerValue);
                        }
                    }

                    if (e.recvdata2 != laserPowerBack2)
                    {
                        int dtmp = 0;
                        int.TryParse(e.recvdata2, out dtmp);
                        //if (dLaserPeakPower < dtmp)
                        if (dtmp > 0)
                        {
                            iLaserPeakPower = dtmp;
                            ShowLabelData(e.recvdata2, lblPeakPowerValue);
                        }
                    }
                }

                if ((e.datatype == 2) & (e.recvdata1 != laserTempBack))
                    ShowLabelData(e.recvdata1, lblTemperatureValue);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //return ex.HResult;
            }
        }

//        public async Task LaserControllerStatusChangedEventThread(object obj)
//        {
//            string className = "MainWindow";
//            string funcName = "OnLaserControllerStatusChangedEventReceivedFunc";
//            Brush brushesBorder = Brushes.Black;
//            //LASERSTATUS Status = 0;
//            UInt32 ists = 0;
//            uint uErrorsts = 0;
//            //ITNTResponseArgs retval = new ITNTResponseArgs();
//            LaserControllerStatusEvnetArgs e = (LaserControllerStatusEvnetArgs)obj;
//            bool bError = false;

//            try
//            {
//                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
//                if (e.datatype == 0)
//                {
//                    if (e.recvdata1 == laserStatusBack)
//                        return;

//                    //ShowLabelData(e.recvdata1, lblLaserStatusValue);

//                    UInt32.TryParse(e.recvdata1, out ists);

//                    uErrorsts = ists & (UInt32)LASERSTATUS.StatusError;
//                    if (uErrorsts != 0)
//                    {
//                        ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Red, null, brushesBorder);

//                        if (uErrorsts == uLaserStatusErrorBack)
//                        {
//                            laserStatusBack = e.recvdata1;
//                            return;
//                        }

//                        ShowLog(className, funcName, 0, laserStatusBack + " -> " + e.recvdata1);
//                        uLaserStatusErrorBack = uErrorsts;
//                        //ErrorLaserSource = true;
//                        //MarkControll.SetLaserErrorStatus(true);
//                        ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
//                        if (_laserStatusStoryboard.GetCurrentState(this) != ClockState.Active)
//                            _laserStatusStoryboard.Begin();

//                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : " + ists.ToString("X8"), "");

//                        if ((ists & (UInt32)LASERSTATUS.CommandBufferOverload) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Command Buffer Overload (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.OverHeat) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Over Heat (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.HighBackReflectionLevel) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Back Reflection Level " + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.PulseTooLong) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Pulse Too Long (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }
//#if LASER_YLR
//#else
//                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Out Of Range (" + ists.ToString("X8") + ")", "");
//                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON);
//                        }
//                        else
//                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);

//                        if ((ists & (UInt32)LASERSTATUS.GNDLeakge) != 0)
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : GND Leakge (" + ists.ToString("X8") + ")", "");
//#endif
//                        if ((ists & (UInt32)LASERSTATUS.HighPulseEnergy) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Pulse Energy (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.FiberInterlockActiveOK) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Internal Power Supply Failure (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.DutyCycleTooHigh) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Duty Cycle Too High (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.LowTemerature) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Low Temerature (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyFailure) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Failure (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }


//                        if ((ists & (UInt32)LASERSTATUS.AimingBeamAlarm) != 0)
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Aiming Beam Alarm (" + ists.ToString("X8") + ")", "");

//                        if ((ists & (UInt32)LASERSTATUS.CriticalError) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Critical Error (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }

//                        if ((ists & (UInt32)LASERSTATUS.HighAveragePower) != 0)
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Average Power (" + ists.ToString("X8") + ")", "");
//                            bError = true;
//                        }
//                        if (bError)
//                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON);
//                        laserStatusBack = e.recvdata1;
//                    }
//                    else
//                    {
//                        //ErrorLaserSource = false;
//                        //MarkControll.SetLaserErrorStatus(false);

//                        ShowLabelData("NORMAL", lblLaserStatus, Brushes.Black, Brushes.Green);//, brushesBorder);
//                        if (_laserStatusStoryboard.GetCurrentState(this) == ClockState.Active)
//                            _laserStatusStoryboard.Stop();

//                        ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Black, null, brushesBorder);
//                        if ((uLaserStatusErrorBack != 0) || (laserStatusBack == "00000"))
//                        {
//                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER NORMAL : " + ists.ToString("X8"), "");
//#if LASER_YLR
//                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);
//#else
//                            if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) == 0)
//                                await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);
//#endif
//                        }

//                        laserStatusBack = e.recvdata1;
//                        if (uErrorsts != uLaserStatusErrorBack)
//                            uLaserStatusErrorBack = uErrorsts;
//                    }

//                    //Emi
//                    if ((ists & (UInt32)LASERSTATUS.EmissionOnOff) != 0)
//                        ShowRectangle(Brushes.Red, EmissionLamp);
//                    else
//                    {
//                        StopLabelBlinking();
//                    }

//                    if ((ists & (UInt32)LASERSTATUS.AimingBeamOnOff) != 0)
//                        ShowRectangle(Brushes.Red, AimingLamp);
//                    else
//                        ShowRectangle(Brushes.Black, AimingLamp);
//                    laserStatusBack = e.recvdata1;
//                }


//                if (e.datatype == 1)
//                {
//                    if (e.recvdata1 != laserPowerBack1)
//                    {
//                        double dtmp = 0.0;
//                        double.TryParse(e.recvdata1, out dtmp);
//                        if (dLaserAveragePower < dtmp)
//                        {
//                            dLaserAveragePower = dtmp;
//                            ShowLabelData(e.recvdata1, lblAvgPowerValue);
//                        }
//                    }

//                    int itmp = 0;
//                    int.TryParse(e.recvdata2, out itmp);
//                    if ((byLaserStartFlag != 0) && (itmp > 0))
//                        iLaserPowerList.Add(itmp);
//                    if (e.recvdata2 != laserPowerBack2)
//                    {
//                        //if (dLaserPeakPower < dtmp)
//                        if (itmp > 0)
//                        {
//                            iLaserPeakPower = itmp;
//                            ShowLabelData(e.recvdata2, lblPeakPowerValue);
//                        }
//                    }
//                }

//                if ((e.datatype == 2) & (e.recvdata1 != laserTempBack))
//                    ShowLabelData(e.recvdata1, lblTemperatureValue);
//            }
//            catch (Exception ex)
//            {
//                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
//                //return ex.HResult;
//            }
//        }

        //        public async void OnLaserControllerStatusChangedEventReceivedFunc(object sender, LaserControllerStatusEvnetArgs e)
        //        {
        //            string className = "MainWindow";
        //            string funcName = "OnLaserControllerStatusChangedEventReceivedFunc";
        //            Brush brushesBorder = Brushes.Black;
        //            //LASERSTATUS Status = 0;
        //            UInt32 ists = 0;
        //            uint uErrorsts = 0;
        //            //ITNTResponseArgs retval = new ITNTResponseArgs();
        //            bool bError = false;
        //            string status = "";

        //            try
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //                if (e.datatype == 0)
        //                {
        //                    if (e.recvdata1 == laserStatusBack)
        //                    {
        //                        //uErrorsts = ists & (UInt32)LASERSTATUS.StatusError;
        //                        //if (uErrorsts != 0)
        //                        //{
        //                        //    status = GetLabelData(lblLaserStatus);
        //                        //    if (status == "ERROR")
        //                        //        return;
        //                        //}
        //                        //else
        //                        //{
        //                        //    status = GetLabelData(lblLaserStatus);
        //                        //    if (status == "NORMAL")
        //                        //        return;
        //                        //}
        //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "e.recvdata1 == laserStatusBack", Thread.CurrentThread.ManagedThreadId);
        //                        return;
        //                    }

        //                    //ShowLabelData(e.recvdata1, lblLaserStatusValue);

        //                    UInt32.TryParse(e.recvdata1, out ists);

        //                    uErrorsts = ists & (UInt32)LASERSTATUS.StatusError;
        //                    if (uErrorsts != 0)
        //                    {
        //                        //status = GetLabelData(lblLastErrorValue);
        //                        //if (status != ists.ToString("X8"))
        //                            ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Red, null, brushesBorder);

        //                        //status = GetLabelData(lblLaserStatus);
        //                        //if (status != "ERROR")
        //                            //ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
        //                        if (uErrorsts == uLaserStatusErrorBack)
        //                        {
        //                            laserStatusBack = e.recvdata1;
        //                            return;
        //                        }

        //                        ShowLog(className, funcName, 0, laserStatusBack + " -> " + e.recvdata1);
        //                        ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
        //                        uLaserStatusErrorBack = uErrorsts;
        //                        //ErrorLaserSource = true;
        //                        //MarkControll.SetLaserErrorStatus(true);
        //                        //ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
        //                        if (_laserStatusStoryboard.GetCurrentState(this) != ClockState.Active)
        //                            _laserStatusStoryboard.Begin();

        //                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : " + ists.ToString("X8"), "");

        //                        if ((ists & (UInt32)LASERSTATUS.CommandBufferOverload) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Command Buffer Overload (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.OverHeat) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Over Heat (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.HighBackReflectionLevel) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Back Reflection Level " + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.PulseTooLong) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Pulse Too Long (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }
        //#if LASER_YLR
        //#else
        //                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Out Of Range (" + ists.ToString("X8") + ")", "");
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON);
        //                        }
        //                        else
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);

        //                        if ((ists & (UInt32)LASERSTATUS.GNDLeakge) != 0)
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : GND Leakge (" + ists.ToString("X8") + ")", "");
        //#endif
        //                        if ((ists & (UInt32)LASERSTATUS.HighPulseEnergy) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Pulse Energy (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.FiberInterlockActiveOK) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Internal Power Supply Failure (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.DutyCycleTooHigh) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Duty Cycle Too High (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.LowTemerature) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Low Temerature (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyFailure) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Failure (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }


        //                        if ((ists & (UInt32)LASERSTATUS.AimingBeamAlarm) != 0)
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Aiming Beam Alarm (" + ists.ToString("X8") + ")", "");

        //                        if ((ists & (UInt32)LASERSTATUS.CriticalError) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Critical Error (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.HighAveragePower) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Average Power (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }
        //                        if (bError)
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON);
        //                        laserStatusBack = e.recvdata1;
        //                    }
        //                    else
        //                    {
        //                        //ErrorLaserSource = false;
        //                        //MarkControll.SetLaserErrorStatus(false);
        //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "no error", Thread.CurrentThread.ManagedThreadId);

        //                        ShowLabelData("NORMAL", lblLaserStatus, Brushes.Black, Brushes.Green);//, brushesBorder);
        //                        if (_laserStatusStoryboard.GetCurrentState(this) == ClockState.Active)
        //                            _laserStatusStoryboard.Stop();

        //                        ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Black, null, brushesBorder);
        //                        if ((uLaserStatusErrorBack != 0) || (laserStatusBack == "00000"))
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER NORMAL : " + ists.ToString("X8"), "");
        //#if LASER_YLR
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);
        //#else
        //                            if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) == 0)
        //                                await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);
        //#endif
        //                        }

        //                        laserStatusBack = e.recvdata1;
        //                        if (uErrorsts != uLaserStatusErrorBack)
        //                            uLaserStatusErrorBack = uErrorsts;
        //                    }

        //                    //Emi
        //                    if ((ists & (UInt32)LASERSTATUS.EmissionOnOff) != 0)
        //                        ShowRectangle(Brushes.Red, EmissionLamp);
        //                    else
        //                    {
        //                        StopLabelBlinking();
        //                    }

        //                    if ((ists & (UInt32)LASERSTATUS.AimingBeamOnOff) != 0)
        //                        ShowRectangle(Brushes.Red, AimingLamp);
        //                    else
        //                        ShowRectangle(Brushes.Black, AimingLamp);
        //                    laserStatusBack = e.recvdata1;
        //                }


        //                if (e.datatype == 1)
        //                {
        //                    if (e.recvdata1 != laserPowerBack1)
        //                    {
        //                        double dtmp = 0.0;
        //                        double.TryParse(e.recvdata1, out dtmp);
        //                        if (dLaserAveragePower < dtmp)
        //                        {
        //                            dLaserAveragePower = dtmp;
        //                            ShowLabelData(e.recvdata1, lblAvgPowerValue);
        //                        }
        //                    }

        //                    int itmp = 0;
        //                    int.TryParse(e.recvdata2, out itmp);
        //                    if ((byLaserStartFlag != 0) && (itmp > 0))
        //                        iLaserPowerList.Add(itmp);
        //                    if (e.recvdata2 != laserPowerBack2)
        //                    {
        //                        //if (dLaserPeakPower < dtmp)
        //                        if (itmp > 0)
        //                        {
        //                            iLaserPeakPower = itmp;
        //                            ShowLabelData(e.recvdata2, lblPeakPowerValue);
        //                        }
        //                    }
        //                }

        //                if ((e.datatype == 2) & (e.recvdata1 != laserTempBack))
        //                    ShowLabelData(e.recvdata1, lblTemperatureValue);
        //            }
        //            catch (Exception ex)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //                //return ex.HResult;
        //            }
        //        }

        //        public async Task LaserControllerStatusChangedEventThread(object obj)
        //        {
        //            string className = "MainWindow";
        //            string funcName = "OnLaserControllerStatusChangedEventReceivedFunc";
        //            Brush brushesBorder = Brushes.Black;
        //            //LASERSTATUS Status = 0;
        //            UInt32 ists = 0;
        //            uint uErrorsts = 0;
        //            //ITNTResponseArgs retval = new ITNTResponseArgs();
        //            LaserControllerStatusEvnetArgs e = (LaserControllerStatusEvnetArgs)obj;
        //            bool bError = false;
        //            string status = "";

        //            try
        //            {
        //                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //                if (e.datatype == 0)
        //                {
        //                    if (e.recvdata1 == laserStatusBack)
        //                    {
        //                        uErrorsts = ists & (UInt32)LASERSTATUS.StatusError;
        //                        if (uErrorsts != 0)
        //                        {
        //                            status = GetLabelData(lblLaserStatus);
        //                            if (status == "ERROR")
        //                                return;
        //                        }
        //                        else
        //                        {
        //                            status = GetLabelData(lblLaserStatus);
        //                            if (status == "NORMAL")
        //                                return;
        //                        }
        //                    }

        //                    //ShowLabelData(e.recvdata1, lblLaserStatusValue);

        //                    UInt32.TryParse(e.recvdata1, out ists);

        //                    uErrorsts = ists & (UInt32)LASERSTATUS.StatusError;
        //                    if (uErrorsts != 0)
        //                    {
        //                        status = GetLabelData(lblLastErrorValue);
        //                        if(status != ists.ToString("X8"))
        //                            ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Red, null, brushesBorder);

        //                        status = GetLabelData(lblLaserStatus);
        //                        if(status != "ERROR")
        //                            ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
        //                        if (uErrorsts == uLaserStatusErrorBack)
        //                        {
        //                            laserStatusBack = e.recvdata1;
        //                            return;
        //                        }

        //                        ShowLog(className, funcName, 0, laserStatusBack + " -> " + e.recvdata1);
        //                        uLaserStatusErrorBack = uErrorsts;
        //                        //ErrorLaserSource = true;
        //                        //MarkControll.SetLaserErrorStatus(true);
        //                        //ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
        //                        if (_laserStatusStoryboard.GetCurrentState(this) != ClockState.Active)
        //                            _laserStatusStoryboard.Begin();

        //                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : " + ists.ToString("X8"), "");

        //                        if ((ists & (UInt32)LASERSTATUS.CommandBufferOverload) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Command Buffer Overload (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.OverHeat) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Over Heat (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.HighBackReflectionLevel) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Back Reflection Level " + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.PulseTooLong) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Pulse Too Long (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }
        //#if LASER_YLR
        //#else
        //                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Out Of Range (" + ists.ToString("X8") + ")", "");
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON);
        //                        }
        //                        else
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);

        //                        if ((ists & (UInt32)LASERSTATUS.GNDLeakge) != 0)
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : GND Leakge (" + ists.ToString("X8") + ")", "");
        //#endif
        //                        if ((ists & (UInt32)LASERSTATUS.HighPulseEnergy) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Pulse Energy (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.FiberInterlockActiveOK) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Internal Power Supply Failure (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.DutyCycleTooHigh) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Duty Cycle Too High (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.LowTemerature) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Low Temerature (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.PowerSupplyFailure) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Power Supply Failure (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }


        //                        if ((ists & (UInt32)LASERSTATUS.AimingBeamAlarm) != 0)
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Aiming Beam Alarm (" + ists.ToString("X8") + ")", "");

        //                        if ((ists & (UInt32)LASERSTATUS.CriticalError) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : Critical Error (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }

        //                        if ((ists & (UInt32)LASERSTATUS.HighAveragePower) != 0)
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER ERROR : High Average Power (" + ists.ToString("X8") + ")", "");
        //                            bError = true;
        //                        }
        //                        if(bError)
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON);
        //                        laserStatusBack = e.recvdata1;
        //                    }
        //                    else
        //                    {
        //                        //ErrorLaserSource = false;
        //                        //MarkControll.SetLaserErrorStatus(false);

        //                        ShowLabelData("NORMAL", lblLaserStatus, Brushes.Black, Brushes.Green);//, brushesBorder);
        //                        if(_laserStatusStoryboard.GetCurrentState(this) == ClockState.Active)
        //                            _laserStatusStoryboard.Stop();

        //                        ShowLabelData(ists.ToString("X8"), lblLastErrorValue, Brushes.Black, null, brushesBorder);
        //                        if((uLaserStatusErrorBack != 0) || (laserStatusBack == "00000"))
        //                        {
        //                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER NORMAL : " + ists.ToString("X8"), "");
        //#if LASER_YLR
        //                            await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);
        //#else
        //                            if ((ists & (UInt32)LASERSTATUS.PowerSupplyOutOfRangeOK) == 0)
        //                                await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_OFF);
        //#endif
        //                        }

        //                        laserStatusBack = e.recvdata1;
        //                        if (uErrorsts != uLaserStatusErrorBack)
        //                            uLaserStatusErrorBack = uErrorsts;
        //                    }

        //                    //Emi
        //                    if ((ists & (UInt32)LASERSTATUS.EmissionOnOff) != 0)
        //                        ShowRectangle(Brushes.Red, EmissionLamp);
        //                    else
        //                    {
        //                        StopLabelBlinking();
        //                    }

        //                    if ((ists & (UInt32)LASERSTATUS.AimingBeamOnOff) != 0)
        //                        ShowRectangle(Brushes.Red, AimingLamp);
        //                    else
        //                        ShowRectangle(Brushes.Black, AimingLamp);
        //                    laserStatusBack = e.recvdata1;
        //                }


        //                if (e.datatype == 1)
        //                {
        //                    if (e.recvdata1 != laserPowerBack1)
        //                    {
        //                        double dtmp = 0.0;
        //                        double.TryParse(e.recvdata1, out dtmp);
        //                        if (dLaserAveragePower < dtmp)
        //                        {
        //                            dLaserAveragePower = dtmp;
        //                            ShowLabelData(e.recvdata1, lblAvgPowerValue);
        //                        }
        //                    }

        //                    int itmp = 0;
        //                    int.TryParse(e.recvdata2, out itmp);
        //                    if ((byLaserStartFlag != 0) && (itmp > 0))
        //                        iLaserPowerList.Add(itmp);
        //                    if (e.recvdata2 != laserPowerBack2)
        //                    {
        //                        //if (dLaserPeakPower < dtmp)
        //                        if (itmp > 0)
        //                        {
        //                            iLaserPeakPower = itmp;
        //                            ShowLabelData(e.recvdata2, lblPeakPowerValue);
        //                        }
        //                    }
        //                }

        //                if ((e.datatype == 2) & (e.recvdata1 != laserTempBack))
        //                    ShowLabelData(e.recvdata1, lblTemperatureValue);
        //            }
        //            catch (Exception ex)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //                //return ex.HResult;
        //            }
        //        }

        private void StartLabelBlinking()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _flashStoryboard.Begin();
                ShowLabelData("LASER ON", EmissionLabel);
            }));
        }

        private void StopLabelBlinking()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _flashStoryboard.Stop();
                ShowLabelData("LASER OFF", EmissionLabel);
            }));
        }

        private async void MyDeley(long milliseconds)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            while (sw.ElapsedMilliseconds < milliseconds)
            {
                await Task.Delay(50);
            }
            sw.Stop();
        }


        public void ShowSelectedPlanData(DataGrid grid, DataRowView rowview)
        {
            string className = "MainWindow";
            string funcName = "ShowSelectedPlanData";

            int count = 0;
            bool bfind = false;
            int index = 0;
            //string value = "";
            DataRowView dataview = null;

            try
            {
                if (grid.CheckAccess())
                {
                    foreach (DataRowView row in grid.Items)
                    {
                        if (row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString() == rowview.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString())
                        {
                            bfind = true;
                            dataview = row;
                            break;
                        }
                        index++;
                    }
                    if (bfind && (dataview != null))
                    {
                        if (grid.Items.Count > count + index)
                        {
                            grid.SelectedItem = dataview;
                            grid.UpdateLayout();
                            grid.ScrollIntoView(dataview, null);
                        }
                        else
                        {
                            grid.SelectedItem = dataview;
                            grid.UpdateLayout();
                            grid.ScrollIntoView(grid.Items[grid.Items.Count - 1], null);
                        }
                    }
                }
                else
                {
                    foreach (DataRowView row in grid.Items)
                    {
                        if (row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString() == rowview.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString())
                        {
                            bfind = true;
                            break;
                        }
                        index++;
                    }
                    if (bfind)
                    {
                        if (grid.Items.Count > count + index)
                        {
                            grid.SelectedIndex = index;
                            grid.UpdateLayout();
                            grid.ScrollIntoView(grid.Items[index + count]);
                        }
                        else
                        {
                            grid.SelectedIndex = index;
                            grid.UpdateLayout();
                            grid.ScrollIntoView(grid.Items[grid.Items.Count]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}/{0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public string AddMonthCode(string rawvin)
        {
            string retval = "";
            string modivin = "";
            string svinyear = "";
            string syear = "";
            string spcyear = "";
            string smonth = "";
            string value = "";

            try
            {
                Util.GetPrivateProfileValue("CONFIG", "USEMONTHCODE", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "0")
                {
                    retval = rawvin;
                    return retval;
                }

                //" MALJC81DMPM012465*"
                modivin = rawvin;
                if (modivin.Length < 19)
                    modivin = rawvin.PadRight(19, ' ');
                svinyear = modivin.Substring(10, 1);
                Util.GetPrivateProfileValue("YEAR", svinyear, "25", ref syear, Constants.PARAMS_INI_FILE);

                spcyear = DateTime.Now.ToString("yy");

                if (spcyear != syear)
                    smonth = "01";
                else
                    smonth = DateTime.Now.ToString("MM");

                Util.GetPrivateProfileValue("MONTH", smonth, "A", ref value, Constants.PARAMS_INI_FILE);

                retval = modivin;
                if ((modivin.Substring(modivin.Length - 1, 1) == "*") && (modivin.Substring(0, 1) != "*"))
                {
                    retval = modivin.Substring(1, 17) + "*" + value;
                }
            }
            catch (Exception ex)
            {
                retval = rawvin;
            }
            return retval;
        }


        private void ShowTextBlock(TextBlock block, string message, Brush fore=null, Brush back = null)
        {
            if (block == null)
                return;

            if (block.CheckAccess() == true)
            {
                if(fore != null)
                    block.Foreground = fore;
                if(back != null)
                    block.Background = back;
                block.Text = message;
            }
            else
            {
                block.Dispatcher.Invoke(new Action(() =>
                {
                    if (fore != null)
                        block.Foreground = fore;
                    if (back != null)
                        block.Background = back;
                    block.Text = message;
                }
                ));
            }
        }
    }
}
