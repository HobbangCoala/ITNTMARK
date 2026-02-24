using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
//using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
//using System.Windows.Controls.Primitives;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014


namespace ITNTMARK
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    enum DIO_INPUT
    {
        INPUT_OPTION_00 = 0x00,
        INPUT_OPTION_01 = 0x01,
        INPUT_OPTION_02 = 0x02,
        INPUT_OPTION_03 = 0x03,
        INPUT_OPTION_04 = 0x04,
        INPUT_OPTION_05 = 0x05,
        INPUT_OPTION_06 = 0x06,
        INPUT_OPTION_07 = 0x07,
        INPUT_OPTION_08 = 0x08,
        INPUT_OPTION_09 = 0x09,
        INPUT_OPTION_10 = 0x0A,
        INPUT_OPTION_11 = 0x0B,
        INPUT_OPTION_12 = 0x0C,
        INPUT_OPTION_13 = 0x0D,
        INPUT_OPTION_14 = 0x0E,
        INPUT_OPTION_15 = 0x0F,
        INPUT_OPTION_16 = 0x10,
        INPUT_OPTION_17 = 0x11,
        INPUT_OPTION_18 = 0x12,
        INPUT_OPTION_19 = 0x13,
        INPUT_OPTION_20 = 0x14,
        INPUT_OPTION_21 = 0x15,
        INPUT_OPTION_22 = 0x16,
        INPUT_OPTION_23 = 0x17,
        INPUT_OPTION_24 = 0x18,
        INPUT_OPTION_25 = 0x19,
        INPUT_OPTION_26 = 0x1A,
        INPUT_OPTION_27 = 0x1B,
        INPUT_OPTION_28 = 0x1C,
        INPUT_OPTION_29 = 0x1D,
        INPUT_OPTION_30 = 0x1E,
        INPUT_OPTION_31 = 0x1F,
    }

    enum DISPLAY_INFO_COLOR_TYPE
    {
        DISP_COLOR_IDEL = 0,
        DISP_COLOR_ = 1,
        DISP_COLOR_NEXTVIN = 2,
        DISP_COLOR_MARKING = 3,
        DISP_COLOR_COMPLETE = 4,
    }

    public class MarkedVINInfo
    {
        public string seq;
        public string vin;
        public string rawcartype;

        public MarkedVINInfo()
        {
            seq = "";
            vin = "";
            rawcartype = "";
        }
    }

    enum mesUpdateStatus
    {
        MES_UPDATE_STATUS_IDLE = 0,
        MES_UPDATE_STATUS_RECEIVING = 1,
        MES_UPDATE_STATUS_SAVING_FILE = 2,
        MES_UPDATE_STATUS_RECV_COMPLETE = 3,
        MES_UPDATE_STATUS_INSERTING = 4,
        MES_UPDATE_STATUS_INST_COMPLETE = 5,
    }

    public delegate Task<ITNTResponseArgs> VisionDataArrivedEventHandler(Object sender, ITNTResponseArgs e);
    //public delegate void PLCStatusChangedEventHandler(Object sender, PLCChangedEvnetArgs e);
    public delegate void PLCErrorReceivedEventHandler(Object sender, PLCChangedEvnetArgs e);
    public delegate void StartSignalReceivedEventHandler(Object sender, PLCChangedEvnetArgs e);
    public delegate int ServerReceivedEventHandler(object sender, ServerReceivedEventArgs e);
    public delegate void ServerStatusChangedEventHandler(object sender, ServerStatusChangedEventArgs e);
    public delegate void MarkControllerStatusDataEventHandler(Object sender, MarkControllerRecievedEvnetArgs e);
    public delegate Task<int> MESClientReceivedEventHandler(object sender, MESClientReceivedEventArgs e);
    public delegate int MESClientStatusChangedEventHandler(object sender, ServerStatusChangedEventArgs e);
    public delegate void LaserSourceControllerEventHandler(object sender, LaserSourceControllerEvnetArgs e);
    public delegate void LaserControllerStatusEventHandler(object sender, LaserControllerStatusEvnetArgs e);
    public delegate void LaserConnectionStatusChangedEventHandler(object sender, ConnectionStatusChangedEventArgs e);
    //public delegate void PLCConnectionStatusChangedEventHandler(object sender, ConnectionStatusChangedEventArgs e);
    public delegate void PLCConnectionStatusChangedEventHandler(DeviceStatusChangedEventArgs e);
    public delegate void LPMControllerDataReceivedEventHandler(Object sender, LPMControllerRecievedEvnetArgs e);
    //public delegate void PLCControllerStatusEventHandler(object sender, DeviceStatusChangedEventArgs e);
    //public delegate void DisplaceConnectionStatusChangedEventHandler(DeviceStatusChangedEventArgs e);
    //PLCConnectionStatusChangedEventHandler ConnectCallback;
    public delegate void ClientDataArrivalHandler(string msg);
    public delegate void ClientConnectionHandler(DeviceStatusChangedEventArgs status);
    public delegate void ConnectionChangedEventHandler(DeviceStatusChangedEventArgs e);

    public partial class MainWindow : Window
    {
        static string recvStatus = "";
        byte[] recvMarkBack = new byte[32];

        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        MESServer mesServer = new MESServer();
        //MESOEMDll oemDllServer = new MESOEMDll();
        MESClient3 mesClient = new MESClient3();
        MESFTB mesFTB = new MESFTB();
        GMESClass mesGMES = new GMESClass();

        public VisionServer2 visionServer = new VisionServer2();
        public ITNTClientAsync2 visionClient = null;

        public PLCControlManager plcComm;// = new PLCControlManager();

        // bool m_bCheckVisionOption = false;
        //bool m_bCheckDoubleMarkOption = false;

        //bool //m_bDoingNextVINFlag = false;       //
        bool m_bDoingMarkingFlag = false;
        bool m_bEmergencyFlag = false;
        //bool m_bDataShiftFlag = false;

        //MarkVINInform currMark = new MarkVINInform();
        MarkVINInformEx currMarkInfo = new MarkVINInformEx();
        //MarkVINInformLaser currMarkInfoLaser = new MarkVINInformLaser();

        //public MarkController MarkControll = new MarkController();
        public MarkController MarkControll = new MarkController();
        public LPMControll lpmControll = new LPMControll();
        MESReceivedData m_currentMarkData = new MESReceivedData();

        //int m_CurrentMarkNum = 0;               //현재 각인할 Frame이 팔레트에서 몇번째에 있는 Frame인지를 설정 (0 ~ 3만 가능)

        Line charline = new Line();
        byte m_currCMD = 0;

        DispatcherTimer markRunTimer = new DispatcherTimer();

        public Byte[] InSignal = new byte[Constants.MAX_PLC_PORT_SIZE];// { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public Byte[] InSignalB = new byte[Constants.MAX_PLC_PORT_SIZE];// { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public Byte[] OutSignal = new byte[Constants.MAX_PLC_PORT_SIZE];// { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public Byte[] OutSignalB = new byte[Constants.MAX_PLC_PORT_SIZE];// { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public static uint DIOINValue = 0;
        public static uint DIOOUTValue = 0;
        public static uint DIOINValueB = 0;
        public static uint DIOOUTValueB = 0;

        public Thread DIOReadINPORTThread = null;
        public Thread DIOReadOUTPORTThread = null;
        private bool bReadINPORTThread = false;
        private bool bReadOUTPORTThread = false;
        private bool bSupendOUTPORTThread = true;

        //public IOOption[] outOption = new IOOption[Constants.MAX_PLC_PORT_SIZE];
        //IOOption[] inOption = new IOOption[Constants.MAX_PLC_PORT_SIZE];
        uint DIO_IN_BUZZSTOP = 0;
        uint DIO_IN_ABORT = 0;
        uint DIO_IN_EMERGENCY = 0;
        uint DIO_IN_AIRPRESSURE = 0;
        uint DIO_IN_CYCLEREADY = 0;
        uint DIO_IN_UNCLAMP = 0;
        uint DIO_IN_CLAMP = 0;
        uint DIO_IN_MARKSTART = 0;
        uint DIO_IN_ERRRESET = 0;
        uint DIO_IN_MARKING = 0;
        uint DIO_IN_MARKCOMPLETE = 0;
        uint DIO_IN_M_UNCLAMP = 0;
        uint DIO_IN_M_CLAMP = 0;
        uint DIO_IN_M_MARKSTART = 0;
        uint DIO_IN_SENS_UNCLAMP = 0;
        uint DIO_IN_SENS_CLAMP = 0;
        uint DIO_IN_NEXT_VIN = 0;

        int DIO_OUT_PRINTSTART = 0;
        int DIO_OUT_ABORT = 0;
        int DIO_OUT_RED = 0;
        int DIO_OUT_LAMPYELLOW = 0;
        int DIO_OUT_LAMPGREEN = 0;
        int DIO_OUT_BUZZER = 0;
        int DIO_OUT_UNCLAMP_SOL = 0;
        int DIO_OUT_CLAMP_SOL = 0;

        //Cylinder Sensor Timeout Variables
        uint CLAMP_TIMEOUT = 0;
        uint UNCLAMP_TIMEOUT = 0;

        bool m_BuzzerStop = false;
        bool m_Ready = false;

        bool m_ManualClamp = false;
        bool m_ManualComplete = false;
        int m_autoExecuteFlag = 0;      //수동/자동 Status Flag
        const int STATUS_LABEL_COUNT = 14;

        enum STATUS_LABEL_NUM
        {
            STATUS_LABEL_READY_MARK = 0,
            STATUS_LABEL_RECV_NEXTVIN = 1,
            STATUS_LABEL_COMP_CARTYPE = 2,
            STATUS_LABEL_READ_CARTYPE = 2,
            STATUS_LABEL_COMP_DOUBLE = 3,
            STATUS_LABEL_READ_VIN = 3,
            STATUS_LABEL_SEND_MATCHRESULT = 4,
            STATUS_LABEL_WAIT_MARKSIGNAL = 5,
            STATUS_LABEL_RECV_MARKSIGNAL = 6,
            STATUS_LABEL_RUN_MARKING = 7,
            STATUS_LABEL_FINISH_MARKING = 8,
            STATUS_LABEL_SEND_MARKCOMPLETE = 9,
            STATUS_LABEL_WAIT_VISION = 10,
            STATUS_LABEL_COMPLETE_JOB = 10,
            STATUS_LABEL_RECV_VISION = 11,
            STATUS_LABEL_RUN_VISION = 12,
            STATUS_LABEL_FINISH_VISION = 13,
            //STATUS_LABEL_SAVE_FINISHDATA    = 14,
            //STATUS_LABEL_FINISH_JOB = 12,
        }

        bool visionFlag = false;
        //int visionFinish = 0;
        int currentProcessStatus = 0;
        int oldProcessStatus = -1;

        bool doingCommand = false;
        //MarkedVINInfo markedInfo = new MarkedVINInfo();
        //int CurrentStateLabel = 0;
        //bool markCompleteFlag = false;

        public string masterpw;
        public string operatorpw;
        public string userpw;

        public string masterID;
        public string operatorID;
        public string userID;

        public byte PasswordFlag;
        bool chkSeqFlag = false;
        bool chkSeqBack = false;

        bool showBlink = false;
        int isShowWarningBlink = 0;

        public byte currentWindow = 0;

        public byte seqcheckError = 0;

        bool recvSensorData = false;

        DistanceSensor2 distanceSensor = null;
        bool bUseDispalcementSensor = false;

        public byte fwVersionFlag = 0;
        public string fwVersion = "";
        //string DBDisplayCommand = "SELECT * from plantable ORDER BY SEQUENCE ASC, DATE(PRODUCTDATE) ASC";

        Thread mesDataSaveThread;

        System.Windows.Point currentPoint = new System.Windows.Point();
        public bool homeError = false;
        //Mutex markMutex = new Mutex(true);

        public string tableName = "plantable";
        //bool bDBChanged = false;
        bool bcloseThread = false;
        public int mesDBUpdateFlag = -1;

        Color lineColor;

        bool bLaserOpen = false;

        Thread dataCountWarningThread = null;
        bool showBlinkCount = false;
        bool showWarningFlag = false;
        bool changeDBProcessFlag = false;
        public LaserSourceController laserSource = new LaserSourceController();
        //public LaserSourceController2 laserSource = new LaserSourceController2();

        double AngleOfSensor = 0;
        public byte bHeadType = 0;   //0 = Scribe, 1 = Laser

        //Line charline = new Line();
        //Ellipse Dotline = new Ellipse();

        DispatcherTimer cycleTimer = new DispatcherTimer();
        Stopwatch cycleWatch = new Stopwatch();
        byte datashift = 0;

        static string laserStatusBack = "00000";
        static string laserPowerBack1 = "00000";
        static string laserPowerBack2 = "00000";
        static string laserTempBack = "00000";

        uint uLaserStatusErrorBack = 0;

        static byte bControllerInitFlag = 0;


        //LaserSourceStatusWindow laserStatuswindow = new LaserSourceStatusWindow();
        struct WARNINGBLINKPARAM
        {
            public System.Windows.Controls.Control ctrl;
            public Color backc1;
            public Color backc2;
            public short CycleTime_ms;
        }

        //public byte motorErrorFlag = 0;
        //public byte laserErrorFlag = 0;

        DataGrid CompleteDataGrid = new DataGrid();
        DataGrid PlanDataGrid = new DataGrid();

        byte byMainScreenType = 0;
        //Grid PlanGrid = new Grid();
        //Grid CompGrid = new Grid();
        //Grid CompGrid2 = new Grid();

        private Storyboard _flashStoryboard;
        private Storyboard _laserStatusStoryboard;
        //private Storyboard blinkingStoryboard;

        double averageDetectPower = 0;

#nullable enable
        private CancellationTokenSource? _cts;
#nullable disable

        private bool _isBusy = false;

        //private ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();

        //private ObservableCollection<LogEntry> LogEntries = new ObservableCollection<LogEntry>();
        /// ////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////
        public MainWindow()
        {
            string className = "MainWindow";
            string funcName = "MainWindow";
            string value = "";
            Stopwatch sw = new Stopwatch();
            string ColorString = "";
            int retval = 0;
            int LoadVISION = 0;
            ITNTResponseArgs retArg = new ITNTResponseArgs();

            try
            {
                InitializeComponent();

                _flashStoryboard = (Storyboard)this.Resources["BackgroundColorStoryboard"];
                //_laserStatusStoryboard = (Storyboard)this.Resources["BackgroundColorStoryboard2"];
                //motorErrorFlag = 0;
                //laserErrorFlag = 0;
                //LogListBox.ItemsSource = _logItems;
                //LogListBox.ItemsSource = LogEntries;

                lblLaserError.Content = "0";
                lblMotorError.Content = "0";


                currentProcessStatus = 0;
                oldProcessStatus = -1;

                ShowLog(className, funcName, 0, "---  PROGRAM START  ---");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                //SetNextDataGrid();
                //SetCompletetDataGrid();

                retval = Util.GetPrivateProfileValue("SYSTEM", "Background", "#FFFFFFFF", ref ColorString, Constants.PARAMS_INI_FILE);
                Color color = (Color)ColorConverter.ConvertFromString(ColorString);
                this.Background = new SolidColorBrush(color);

                cycleTimer.Tick += cycleTimerHandler;
                cycleTimer.IsEnabled = false;
                cycleTimer.Stop();

                Util.GetPrivateProfileValue("OPTION", "MAINSCREENTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out byMainScreenType);
                if (byMainScreenType != 0)
                {
                    CompleteDataGrid = dgdCompleteData2;
                    PlanDataGrid = dgdPlanData;
                    gridCompGrid2.Visibility = Visibility.Visible;
                    gridCompGrid.Visibility = Visibility.Collapsed;
                    gridPlanGrid.Visibility = Visibility.Collapsed;

                    lblSeqStatusE02.HorizontalContentAlignment = HorizontalAlignment.Center;
                    lblSeqStatusE02.Content = "CHECK" + Environment.NewLine + "MODEL";
                    lblSeqStatusE03.Content = "CHECK VIN";

                    stpMainMESInfo.Visibility = Visibility.Collapsed;
                    stpMainResultInfo.Visibility = Visibility.Collapsed;

                    SetCompletetDataGrid(CompleteDataGrid);
                }
                else
                {
                    CompleteDataGrid = dgdCompleteData;
                    PlanDataGrid = dgdPlanData;
                    gridCompGrid2.Visibility = Visibility.Collapsed;
                    gridCompGrid.Visibility = Visibility.Visible;
                    gridPlanGrid.Visibility = Visibility.Visible;

                    stpMainMESInfo.Visibility = Visibility.Visible;
                    stpMainResultInfo.Visibility = Visibility.Visible;

                    SetNextDataGrid(PlanDataGrid);
                    SetCompletetDataGrid(CompleteDataGrid);
                }

                CheckUpgradeDatabase();

                DriveInfoDeligate();

                LoadVISION = (int)Util.GetPrivateProfileValueUINT("VISION", "LOADVISION", 0, Constants.PARAMS_INI_FILE);
                if (LoadVISION != 0)
                {
                    try
                    {
                        Process[] array = Process.GetProcessesByName("AOIVISION");
                        if (array.Length > 0)
                        {
                            for (int ii = 0; ii < array.Length; ii++)
                            {
                                array[ii].Kill();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MainWindow", string.Format("KILL VISION EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                    }
                }

                Util.GetPrivateProfileValue("COLOR", "CHARACTERLINE", "#FF5072DD", ref value, Constants.PARAMS_INI_FILE);
                if (value.Length > 8)
                    lineColor = (Color)ColorConverter.ConvertFromString(value);
                else
                    lineColor = (Color)ColorConverter.ConvertFromString("#FF5072DD");

                //if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                //    ChangeDBProcess().Wait();

                ShowMarkingDataList(true, true);
                DataRowView row = GetLastMarkedData(dgdPlanData);
                ShowMarkingDataList(true, false);
                ScrollViewToPoint(dgdPlanData);
                //dgdPlanData.ScrollIntoView(row);

                showWarningFlag = true;
                showBlinkCount = false;
                WARNINGBLINKPARAM param;
                param.ctrl = lblPlanDataWarning;
                param.backc1 = Color.FromRgb(255, 255, 255);
                param.backc2 = Color.FromRgb(255, 0, 0);
                param.CycleTime_ms = 1000;
                //showBlinkCount = true;

                currentPoint = new System.Windows.Point(0, 0);

                Util.GetPrivateProfileValue("OPTION", "SHOWSIDEDATA", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "0")
                {
                    DisplacementGrid.Visibility = Visibility.Hidden;
                    //MarkCountGrid.Visibility = Visibility.Hidden;
                    CarInformGrid.Visibility = Visibility.Hidden;

                    //DispalcementBorder.Visibility = Visibility.Hidden;
                    //ClearCountBorder.Visibility = Visibility.Hidden;
                    //CarInformBorder.Visibility = Visibility.Hidden;
                }

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                showToolTip();
                LoadOption();
                int count = 0;
                int totcount = 0;
                //count = GetMarkPlanDataCount2(dgdPlanData);
                (count, totcount) = GetCount4MarkPlanData(dgdPlanData);

#if MANUAL_MARK
                stpAutoStatus.Visibility = Visibility.Collapsed;
                stpManualStatus.Visibility = Visibility.Visible;
                AutoMarkMenu.Visibility = Visibility.Collapsed;
                ManualMarkMenu.Visibility = Visibility.Visible;
                gridDisplayStatus.Visibility = Visibility.Collapsed;
#else
                AutoMarkMenu.Visibility = Visibility.Visible;
                ManualMarkMenu.Visibility = Visibility.Collapsed;
#endif
                recvSensorData = false;

                DateTime dt = DateTime.Now;
                Util.GetPrivateProfileValue("MES", "DOWNLOADTIME", dt.ToString("yyyy-MM-dd - HH:mm:ss"), ref value, Constants.PARAMS_INI_FILE);
                ShowWorkPlanCount(lblWorkPlanDataCount, count);
                ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                ShowMESReceivedTime(lblVINLastUpdateDate, value, false);
                //ShowWorkPlanCountNTime(lblWorkPlanDataCount, lblVINLastUpdateDate, count, value, false);

                Task.Run(OpenMarkController);

                if (bHeadType != 0)
                {
                    Task.Run(OpenDistanceSensor);
                    Task.Run(OpenLaserSource);
                    Task.Run(OpenLPMController);
                }
                else
                {
                    Util.GetPrivateProfileValue("CONFIG", "USE", "0", ref value, Constants.DISPLACEMENT_INI_FILE);
                    if (value != "0")
                    {
                        string val = "";
                        Util.GetPrivateProfileValue("DISPLAY", "ONMAIN", "0", ref val, Constants.DISPLACEMENT_INI_FILE);

                        if (val != "0")
                        {
                            bUseDispalcementSensor = true;
                            lblShowSensor1.Visibility = Visibility.Visible;
                        }

                        //OpenDistanceSensor();
                        Task.Run(OpenDistanceSensor);
                    }
                }

                Util.GetPrivateProfileValue("MES", "USEMES", "1", ref value, Constants.PARAMS_INI_FILE);
                if (value != "0")
                {
                    retval = OpenMESServer();
                    if (retval != 0)
                    {
                        ShowLog(className, funcName, 2, "MES CONNECTION FAIL", retval.ToString());
                    }
                }

                OpenVisionServer();

                //plcComm = new PLCControlManager(OnPLCDataReceivedCallBakHanlder);
                //plcComm = new PLCControlManager(OnPLCDataReceivedCallBakHanlder, OnPLCStatusChangedEventHandler);
                Task.Run(OpenPLCAsync);

                for (int i = 0; i < Constants.MAX_PLC_PORT_SIZE; i++)
                {
                    //inOption[i] = new IOOption();
                    InSignal[i] = 0;
                    InSignalB[i] = 0;

                    //outOption[i] = new IOOption();
                    OutSignal[i] = 0;
                    OutSignalB[i] = 0;
                }

                LoadDIOOption();
                OpenCommunicationToDIO();

                //useVISION = (int)Util.GetPrivateProfileValueUINT("VISION", "LOADVISION", 0, Constants.PARAMS_INI_FILE);
                if (LoadVISION != 0)
                {
                    Thread.Sleep(1000);

                    string visionPath = "C:\\ITNT\\ITNTVISION";
                    FileInfo vinfo = new FileInfo(visionPath + "\\AOIVISION.exe");
                    if (vinfo.Exists)
                    {
                        Util.GetPrivateProfileValue("VISION", "EXEPATH", "C:\\ITNT\\ITNTVISION", ref visionPath, Constants.PARAMS_INI_FILE);
                        ProcessStartInfo procInfo = new ProcessStartInfo();
                        procInfo.UseShellExecute = true;
                        procInfo.FileName = visionPath + "\\AOIVISION.exe";
                        procInfo.WorkingDirectory = Environment.CurrentDirectory;
                        procInfo.Verb = "runas";

                        Process.Start(procInfo);
                    }
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NO VISION FILE", Thread.CurrentThread.ManagedThreadId);
                    }
                }

                isShowWarningBlink = (int)Util.GetPrivateProfileValueUINT("OPTION", "SHOWWARNING", 0, Constants.PARAMS_INI_FILE);

                if ((chbCheckSeq.IsChecked == false) && (isShowWarningBlink != 0))
                {
                    showBlink = true;
                    lblSeqCheckWarning.Visibility = Visibility.Visible;
                    SoftBlink(lblSeqCheckWarning, Color.FromRgb(255, 255, 255), Color.FromRgb(255, 0, 0), 1000);
                }

                markRunTimer.Tick += new EventHandler(markRunTimerTick);
                int timeout = (int)Util.GetPrivateProfileValueUINT("MARK", "TIMEOUTVALUE", 120, Constants.PARAMS_INI_FILE);
                markRunTimer.Interval = new TimeSpan(0, 0, timeout);

                //string AngleDegree = "";
                ////Util.GetPrivateProfileValue("SENSOR", "ANGLEDEGREE", "23.0", ref AngleDegree, Constants.PARAMS_INI_FILE);    // load  sensor angle
                //Util.GetPrivateProfileValue("SENSOR", "ANGLEDEGREE", "23.0", ref AngleDegree, Constants.MARKING_INI_FILE);    // load  sensor angle
                //double.TryParse(AngleDegree, out AngleOfSensor);
                AngleOfSensor = (double)Util.GetPrivateProfileValueDouble("SENSOR", "ANGLEDEGREE", 23.0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");

                ShowErrorMessage("", true);

                GetPasswords();
                CheckPlanDataCountWarning(count, lblPlanDataWarning);

#if LPM_DIRECTUSE
                // ###
                //
                // Serial port configuration for LPM module in Laser Head
                //
                var serialPortLPM = new SerialPortInput();

                // Listen to Serial Port events

                serialPortLPM.ConnectionStatusChanged += delegate (object sender, SerialPortLib.ConnectionStatusChangedEventArgs args)
                {
                    Console.WriteLine("LPM Connected = {0}", args.Connected);
                };

                serialPortLPM.MessageReceived += delegate (object sender, MessageReceivedEventArgs args)
                {
                    Console.WriteLine("Received message from LPM : {0}", BitConverter.ToString(args.Data));
                    double slope = 0d;
                    double intercept = 0;
                    string value = "";
                    Util.GetPrivateProfileValue("LENZ", "SLOPE", "0.0604", ref value, Constants.LENZ_INI_FILE);
                    double.TryParse(value, out slope);

                    Util.GetPrivateProfileValue("LENZ", "INTER", "3.2690", ref value, Constants.LENZ_INI_FILE);
                    double.TryParse(value, out intercept);


                    var lpmDada = BitConverter.ToString(args.Data).Split('-');
                    int idx = 0;
                    while (idx < lpmDada.Length)
                    {
                        var cmd = Convert.ToInt32(lpmDada[idx++], 16);
                        switch ((char)cmd)
                        {
                            case 'M':
                                var hex3 = HexAscii2Bin(lpmDada[idx++]) * 256 + HexAscii2Bin(lpmDada[idx++]) * 16 + HexAscii2Bin(lpmDada[idx++]);
                                //if (Mode_File.LenzCalibFlag) Mode_File.LenzCalib.Add((double)hex3);

                                var x = slope * (double)hex3 + intercept;       // ADC => %
                                var w = 68.24 * x - 181.1;                                          // %   => watt
                                ShowLabelData(w.ToString("F0"), lblreadlPowerValue);
                                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                                //{
                                //    EndPowerTxt.Text = x.ToString("F2");
                                //    EndPowerWattTxt.Text = w.ToString("F0");
                                //    ControlWindow.txt_log.AppendText(DateTime.Now + " EndPoint LASER POWER " + hex3.ToString() + " / " + x.ToString("F2") + "% / " + w.ToString("F2") + "Watts" + Environment.NewLine);
                                //}));
                                idx += 2;
                                break;
                            case 'P':
                                break;
                            case 'Z':
                                ShowLabelData("Ready", lblreadlPowerValue);
                                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                                //{
                                //    EndPowerTxt.Text = "Ready";
                                //    EndPowerWattTxt.Text = "Ready";
                                //}));
                                idx += 2;
                                break;
                            default:
                                break;
                        }
                    }
                };

                // Set port options
                serialPortLPM.SetPort("COM2", 115200);

                // Connect the serial port
                serialPortLPM.Connect();

                while (!serialPortLPM.Connect()) ;
                // Send a message
                var message = System.Text.Encoding.UTF8.GetBytes("Z");
                serialPortLPM.SendMessage(message);

                int HexAscii2Bin(string hex)
                {
                    var t1 = (hex[0] >= 'A') ? hex[0] - 'A' + 10 : hex[0] - '0';
                    var t2 = (hex[1] >= 'A') ? hex[1] - 'A' + 10 : hex[1] - '0';
                    var t3 = t1 * 16 + t2;
                    return (t3 >= (int)'A') ? t3 - (int)'A' + 10 : t3 - (int)'0';
                }
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        void SetNextDataGrid(DataGrid grid)
        {
            string value = "";
            string header = "";
            int count = 0;
            string name = "";
            int colsize = 0;
            string binding = "";
            int totcount = 0;
            string format = "";

            try
            {
                totcount = grid.Columns.Count;
                Util.GetPrivateProfileValue("SETTING", "COUNT", "4", ref value, "./Parameter/NextDataGrid.ini");
                int.TryParse(value, out count);

                for(int i = 1; i < count + 1; i++)
                {
                    Util.GetPrivateProfileValue(i.ToString(), "NAME", "SEQUENCE", ref name, "./Parameter/NextDataGrid.ini");
                    Util.GetPrivateProfileValue(i.ToString(), "SIZE", "4", ref value, "./Parameter/NextDataGrid.ini");
                    int.TryParse(value, out colsize);

                    Util.GetPrivateProfileValue(i.ToString(), "BIND", "SEQUENCE", ref binding, "./Parameter/NextDataGrid.ini");
                    Util.GetPrivateProfileValue(i.ToString(), "FORMAT", "", ref format, "./Parameter/CompleteDataGrid.ini");

                    // 특정 인덱스의 컬럼을 가져옵니다.
                    var column = grid.Columns[i]; // 또는 원하는 인덱스

                    // 1. 헤더 이름 변경
                    column.Header = name;

                    // 비율 기반 너비
                    column.Width = new DataGridLength(colsize, DataGridLengthUnitType.Star);

                    // 3. 바인딩 변경 (텍스트 컬럼일 때만 가능)
                    if (column is DataGridTextColumn textColumn)
                    {
                        if (format.Length > 0)
                        {
                            textColumn.Binding = new Binding(binding)
                            {
                                StringFormat = format,
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                            };
                        }
                        else
                        {
                            textColumn.Binding = new Binding(binding)
                            {
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                            };
                        }
                    }

                    column.Visibility = Visibility.Visible;
                }

                for(int k = count + 1; k < totcount; k++)
                {
                    var column = grid.Columns[k];
                    column.Visibility = Visibility.Collapsed;
                }
            }
            catch(Exception ex)
            {

            }
        }

        void SetCompletetDataGrid(DataGrid grid)
        {
            string value = "";
            string header = "";
            int count = 0;
            string name = "";
            int colsize = 0;
            string binding = "";
            int totcount = 0;
            string format = "";

            try
            {
                totcount = grid.Columns.Count;

                Util.GetPrivateProfileValue("SETTING", "COUNT", "4", ref value, "./Parameter/CompleteDataGrid.ini");
                int.TryParse(value, out count);

                for (int i = 0; i < count; i++)
                {
                    Util.GetPrivateProfileValue(i.ToString(), "NAME", "SEQUENCE", ref name, "./Parameter/CompleteDataGrid.ini");
                    Util.GetPrivateProfileValue(i.ToString(), "SIZE", "4", ref value, "./Parameter/CompleteDataGrid.ini");
                    int.TryParse(value, out colsize);

                    Util.GetPrivateProfileValue(i.ToString(), "BIND", "SEQUENCE", ref binding, "./Parameter/CompleteDataGrid.ini");
                    Util.GetPrivateProfileValue(i.ToString(), "FORMAT", "", ref format, "./Parameter/CompleteDataGrid.ini");
                    //Util.GetPrivateProfileValue(i.ToString(), "NAME", "SEQUENCE", ref name, "./Parameter/NextDataGrid");

                    // 특정 인덱스의 컬럼을 가져옵니다.
                    var column = grid.Columns[i]; // 또는 원하는 인덱스

                    // 1. 헤더 이름 변경
                    column.Header = name;

                    // 비율 기반 너비
                    column.Width = new DataGridLength(colsize, DataGridLengthUnitType.Star);

                    // 3. 바인딩 변경 (텍스트 컬럼일 때만 가능)
                    if (column is DataGridTextColumn textColumn)
                    {
                        if (format.Length > 0)
                        {
                            textColumn.Binding = new Binding(binding)
                            {
                                StringFormat = format,
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                            };
                        }
                        else
                        {
                            textColumn.Binding = new Binding(binding)
                            {
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                            };
                        }
                    }

                    column.Visibility = Visibility.Visible;
                }

                for (int k = count; k < totcount; k++)
                {
                    var column = grid.Columns[k];
                    column.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {

            }
        }

        void GetPasswords()
        {
            //StringBuilder sb = new StringBuilder();
            //string curDir = AppDomain.CurrentDomain.BaseDirectory;
            //int returnVal = 0;
            //string iniFileName = curDir + "VisionConfig.ini";
            //sb.Capacity = 32;
            Util.GetPrivateProfileValue("OPTION", "Master", "777777", ref masterpw, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("OPTION", "Operator", "333333", ref operatorpw, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("OPTION", "User", "111111", ref userpw, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("OPTION", "MasterID", "admin", ref masterID, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("OPTION", "OperatorID", "operator", ref operatorID, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("OPTION", "UserID", "user", ref userID, Constants.PARAMS_INI_FILE);
        }


        private (int, int) GetCount4MarkPlanData(DataGrid grid)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            int remaincount = 0;
            int totalCount = 0;
            bool bstart = false;
            //string cmdstring = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                //if(grid.CheckAccess() == true)
                //{
                //    cmdstring = "";
                //}
                //else
                //{
                //    grid.Dispatcher.BeginInvoke(new Action(delegate
                //    {
                //    }));
                //}


                if (grid.CheckAccess())
                {
                    totalCount = grid.Items.Count;
                    foreach (DataRowView row in grid.Items)
                    {
                        if (bstart == true)
                        {
                            if ((row[Constants.DB_NAME_DELETE].ToString() != "DLT") &&
                                (row[Constants.DB_NAME_ISMARK].ToString() != "Y") &&
                                (row[Constants.DB_NAME_COMPLETE].ToString() != "Y") &&
                                (row[Constants.DB_NAME_EXIST].ToString() != "N"))
                                remaincount++;
                        }
                        else
                        {
                            if (row[Constants.DB_NAME_CHECKFLAG].ToString() != "0")
                                bstart = true;
                        }
                        //totalCount++;
                    }
                }
                else
                {
                    grid.Dispatcher.Invoke(new Action(delegate
                    {
                        totalCount = grid.Items.Count;
                        foreach (DataRowView row in grid.Items)
                        {
                            if (bstart == true)
                            {
                                if ((row[Constants.DB_NAME_DELETE].ToString() != "DLT") &&
                                    (row[Constants.DB_NAME_ISMARK].ToString() != "Y") &&
                                    (row[Constants.DB_NAME_COMPLETE].ToString() != "Y") &&
                                    (row[Constants.DB_NAME_EXIST].ToString() != "N"))
                                    remaincount++;
                            }
                            else
                            {
                                if (row[Constants.DB_NAME_CHECKFLAG].ToString() != "0")
                                    bstart = true;
                            }
                            //totalCount++;
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return (0,0);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return (remaincount, totalCount);
        }


        private void LoadDIOOption()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string value = "";
            try
            {
                Util.GetPrivateProfileValue("INPUT", "BUZZSTOP", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_BUZZSTOP);

                Util.GetPrivateProfileValue("INPUT", "ABORT", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_ABORT);

                Util.GetPrivateProfileValue("INPUT", "EMERGENCY", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_EMERGENCY);

                Util.GetPrivateProfileValue("INPUT", "AIRPRESSURE", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_AIRPRESSURE);

                Util.GetPrivateProfileValue("INPUT", "PANELUNCLAMP", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_UNCLAMP);

                Util.GetPrivateProfileValue("INPUT", "PANELCLAMP", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_CLAMP);

                Util.GetPrivateProfileValue("INPUT", "PANELMARKSTART", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_MARKSTART);

                Util.GetPrivateProfileValue("INPUT", "ERRRESET", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_ERRRESET);

                Util.GetPrivateProfileValue("INPUT", "PBUNCLAMP", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_M_UNCLAMP);

                Util.GetPrivateProfileValue("INPUT", "PBCLAMP", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_M_CLAMP);

                Util.GetPrivateProfileValue("INPUT", "PBMARKSTART", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_M_MARKSTART);

                Util.GetPrivateProfileValue("INPUT", "SENSUNCLAMP", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_SENS_UNCLAMP);

                Util.GetPrivateProfileValue("INPUT", "SENSCLAMP", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_SENS_CLAMP);

                Util.GetPrivateProfileValue("INPUT", "PANELNEXTVIN", "0", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out DIO_IN_NEXT_VIN);


                //Output Assign
                Util.GetPrivateProfileValue("OUTPUT", "MARKSTOP", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_ABORT);

                Util.GetPrivateProfileValue("OUTPUT", "MARKSTART", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_PRINTSTART);

                Util.GetPrivateProfileValue("OUTPUT", "TWLAMPGRN", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_LAMPGREEN);

                Util.GetPrivateProfileValue("OUTPUT", "TWLAMPYEL", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_LAMPYELLOW);

                Util.GetPrivateProfileValue("OUTPUT", "TWLAMPRED", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_RED);

                Util.GetPrivateProfileValue("OUTPUT", "TWBUZZER", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_BUZZER);

                Util.GetPrivateProfileValue("OUTPUT", "PBCLAMPSOL", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_CLAMP_SOL);

                Util.GetPrivateProfileValue("OUTPUT", "PBUNCLAMPSOL", "0", ref value, Constants.IO_INI_FILE);
                int.TryParse(value, out DIO_OUT_UNCLAMP_SOL);
                //Cylinder timeout values
                Util.GetPrivateProfileValue("TIMEOUT", "CLAMP", "1", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out CLAMP_TIMEOUT);

                Util.GetPrivateProfileValue("TIMEOUT", "UNCLAMP", "1", ref value, Constants.IO_INI_FILE);
                uint.TryParse(value, out CLAMP_TIMEOUT);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private int OpenCommunicationToDIO()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;
            string value = "";
            Util.GetPrivateProfileValue("DIO", "USEDIO", "0", ref value, Constants.PARAMS_INI_FILE);
            if (value == "0")
                return 0;

            string errorcode = "";
            retval = DIOControl.OpenDevice(this, out errorcode);
            if (retval == 0)
            {
                //imgMainConnToPLC.Source = Util.GetImageSource(".\\images\\connect.jpg");
                if (!bReadINPORTThread)
                {
                    bReadINPORTThread = true;
                    DIOReadINPORTThread = new Thread(new ThreadStart(DIOReadINPORTThreadFunc));
                    DIOReadINPORTThread.Start();
                }

#if MANUAL_MARK
                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
#endif
                //DIOControl.SetCallBackFunction(DIODataArrival);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DIO Open Success", Thread.CurrentThread.ManagedThreadId);
            }
            else
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenDevice ERROR({0})", retval), Thread.CurrentThread.ManagedThreadId);

            return retval;
        }

        private void CloseCommunicationToDIO()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string value = "";
            Util.GetPrivateProfileValue("DIO", "USEDIO", "0", ref value, Constants.PARAMS_INI_FILE);
            if (value == "0")
                return;

            DIOControl.CloseDevice();
        }

        private async void DIOReadINPORTThreadFunc()
        {
            int i;
            uint readData = 0;
            uint readDataB = 0;
            while (bReadINPORTThread)
            {
                for (i = 0; i < Constants.MAX_PLC_PORT_SIZE; i++)
                {
                    DIOControl.DIOReadInportDWORD(0, ref readData);
                    if (readData != readDataB)
                    {
                        readDataB = readData;
                        await OnDIODataArrivalEvent(readData);
                    }
                }
                await Task.Delay(50);
            }
        }

        private void ShowColor()
        {
            //for (int i = 1; i < dgdPlanData.Rows.Count; i++)
            //{
            //    if (i % 2 != 0)
            //    {
            //        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
            //    }
            //    else
            //    {
            //        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.White;
            //    }
            //}
        }

        private async void TestThread(object obj)
        {
            Util.WritePrivateProfileValue("PLC", "SIGNAL", "00FF0001", "TEST.ini");
        }

        private void RunningTimer_Tick(object sender, EventArgs e)
        {

        }

        private void LoadOption()
        {
            string value = "";
            string def = "";
            //int ivalue = 0;

            ////ivalue = (int)Util.GetPrivateProfileValueUINT("OPTION", "MarkingCount", 0, Constants.PARAMS_INI_FILE);
            ////value = ivalue.ToString();
            //Util.GetPrivateProfileValue("OPTION", "MarkingCount", "1", ref value, Constants.PARAMS_INI_FILE);
            //ShowLabelData(value, lblMarkingCount);
            //Util.GetPrivateProfileValue("OPTION", "MarkingClearTime", "", ref value, Constants.PARAMS_INI_FILE);
            //ShowLabelData(value, lblPINChangeTime);

            Util.GetPrivateProfileValue("OPTION", "MarkingCount", "1", ref value, Constants.PARAMS_INI_FILE);
            ShowLabelData(value, lblMarkingCount);

            def = DateTime.Now.ToString("yyyy/MM/dd");
            Util.GetPrivateProfileValue("OPTION", "MarkingClearDate", def, ref value, Constants.PARAMS_INI_FILE);
            ShowLabelData(value, lblPINChangeDate);

            def = DateTime.Now.ToString("HH:mm:ss");
            Util.GetPrivateProfileValue("OPTION", "MarkingClearTime", def, ref value, Constants.PARAMS_INI_FILE);
            ShowLabelData(value, lblPINChangeTime);
        }


        //private string GetPatternName(MESReceivedData data)
        private string GetPatternName(string rawcartype, string rawbodytype = "", string rawtrim = "")
        {
            string retval = "";
            string mesFrameType = "";
            int seqcomptype = 0;
            string value = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "GetPatternName", "START", Thread.CurrentThread.ManagedThreadId);
                seqcomptype = (int)Util.GetPrivateProfileValueUINT("OPTION", "PATTERNTYPE", 0, Constants.PARAMS_INI_FILE);
                if (seqcomptype == 1)
                {
                    retval = GetPatternNameFromNumber(rawcartype);
                    //if (rawcartype.Trim().Length > 0)
                    //{
                    //    value = rawcartype.Trim().Substring(0, 1);
                    //    retval = GetPatternName2(value);//, data);
                    //}
                    //else
                    //{
                    //    retval = "";
                    //}
                    ////retval = GetPatternName2(value, data);

                    ////value = data.rawcartype.Trim().Substring(0, 1);
                    ////retval = GetPatternName2(value, data);
                }
                else if (seqcomptype == 2)
                {
                    retval = GetPatternName2(rawcartype);
                }
                else
                {
                    //mesFrameType = GetFrameType4MES(data.rawcartype.Trim(), data.rawbodytype.Trim(), data.rawtrim.Trim(), "");
                    mesFrameType = GetFrameType4MES(rawcartype.Trim(), rawbodytype.Trim(), rawtrim.Trim(), "");

                    Util.GetPrivateProfileValue("PATTERNNAME", mesFrameType, "", ref retval, Constants.PARAMS_INI_FILE);
                    if (retval.Length <= 0)
                        retval = "Pattern_" + rawcartype.Trim();
                    //if (retval.Length <= 0)
                    //    retval = "Pattern_" + data.rawcartype.Trim();
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "GetPatternName", "pattern Name = " + retval, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "GetPatternName", string.Format("EXCEPTION1 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return retval;
        }


        private string GetPatternName2(string rawtype)//, MESReceivedData data)
        {
            string retval = "";
            Util.GetPrivateProfileValue("PATTERNNAME", rawtype, "", ref retval, Constants.PARAMS_INI_FILE);
            if (retval.Length <= 0)
                retval = "Pattern_" + rawtype;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "GetPatternName", "pattern Name = " + retval, Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public string GetPatternNameFromNumber(string rawCarType)
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
                    return ("Pattern_" + rawCarType);

                key = rawCarType.Trim().Substring(0, 1);
                Util.GetPrivateProfileValue("PATTERNNAME", "USESUBVALCARTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                string[] vals = value.Split('|');
                if ((vals.Length > 0) && (vals.Contains(key) == true))
                {
                    Util.GetPrivateProfileValue("PATTERNNAME", "SUBVALPOS", "0", ref spos, Constants.PARAMS_INI_FILE);
                    int.TryParse(spos, out pos);
                    if ((pos >= rawCarType.Length) || (pos < 1))
                    {
                        Util.GetPrivateProfileValue("PATTERNNAME", key, "", ref value, Constants.PARAMS_INI_FILE);
                        if (value.Length <= 0)
                            retval = "Pattern_" + key;
                        else
                            retval = value;
                    }
                    else
                    {
                        key2 = key + rawCarType.Trim().Substring(pos, 1);
                        Util.GetPrivateProfileValue("PATTERNNAME", key2, "", ref value, Constants.PARAMS_INI_FILE);
                        if (value.Length <= 0)
                        {
                            Util.GetPrivateProfileValue("PATTERNNAME", key, "", ref value, Constants.PARAMS_INI_FILE);
                            if (value.Length <= 0)
                                retval = "Pattern_" + key;
                            else
                                retval = value;
                        }
                        else
                            retval = value;
                    }
                }
                else
                {
                    Util.GetPrivateProfileValue("PATTERNNAME", key, "", ref value, Constants.PARAMS_INI_FILE);
                    if (value.Length <= 0)
                        retval = "Pattern_" + key;
                    else
                        retval = value;
                }
            }
            catch (Exception ex)
            {
                if (rawCarType.Length > 0)
                    retval = "Pattern_" +rawCarType.Trim().Substring(0, 1);
                else
                    retval = rawCarType;
            }
            return retval;
        }

        //public string GetPatternNameKIA(string rawcartype)
        //{
        //    string value = "";
        //    string spos = "";
        //    int pos = 0;
        //    //string raw = "";
        //    string key = "";
        //    string key2 = "";
        //    string retval = "";

        //    try
        //    {
        //        if (rawcartype.Trim().Length <= 0)
        //        {
        //            retval = "Pattern_" + rawcartype;
        //            return retval;
        //        }

        //        key = rawcartype.Trim().Substring(0, 1);

        //        Util.GetPrivateProfileValue("PATTERNNAME", "USESUBVALCARTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
        //        string[] vals = value.Split('|');
        //        if ((vals.Length > 0) && (vals.Contains(key) == true))
        //        {
        //            Util.GetPrivateProfileValue("PATTERNNAME", "SUBVALPOS", "0", ref spos, Constants.PARAMS_INI_FILE);
        //            int.TryParse(spos, out pos);
        //            if ((pos >= rawcartype.Length) || (pos < 1))
        //            {
        //                Util.GetPrivateProfileValue("PATTERNNAME", key, "", ref value, Constants.PARAMS_INI_FILE);
        //                if (value.Length <= 0)
        //                    retval = "Pattern_" + rawcartype;
        //                else
        //                    retval = value;
        //            }
        //            else
        //            {
        //                key2 = key + rawcartype.Trim().Substring(pos, 1);
        //                Util.GetPrivateProfileValue("PATTERNNAME", key2, "", ref value, Constants.PARAMS_INI_FILE);
        //                if (value.Length <= 0)
        //                {
        //                    Util.GetPrivateProfileValue("PATTERNNAME", key, "", ref value, Constants.PARAMS_INI_FILE);
        //                    if (value.Length <= 0)
        //                        retval = "Pattern_" + rawcartype;
        //                    else
        //                        retval = value;
        //                }
        //                else
        //                    retval = value;
        //            }
        //        }
        //        else
        //        {
        //            retval = GetPatternName2(key);//, data);

        //            //Util.GetPrivateProfileValue("PATTERNNAME", key, "", ref value, Constants.PARAMS_INI_FILE);
        //            //if (value.Length <= 0)
        //            //    value = "Pattern_" + rawcartype;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if (rawcartype.Trim().Length > 0)
        //        {
        //            value = rawcartype.Trim().Substring(0, 1);
        //            retval = "Pattern_" + value;
        //        }
        //        else
        //        {
        //            value = rawcartype;
        //            retval = "Pattern_" + value;
        //        }
        //    }
        //    return retval;
        //}

        private void markRunTimerTick(object sender, EventArgs e)
        {
            ErrorInfo errInfo = new ErrorInfo();
            string sCurrentFunc = "MARKING TIMER";
            string sProcedure = "2";
            try
            {
                m_bDoingMarkingFlag = false;
                ////m_bDoingNextVINFlag = false;
                ////SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                //plcComm.SendErrorInfo((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR);
                //////ITNTErrorLog.Instance.Trace(0, "각인 실패 (타임아웃 발생)");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "markRunTimerTick", "MARKING FAIL (MARKING TIMEOUT)", Thread.CurrentThread.ManagedThreadId);
                ////ShowErrorMessage("Marking Failure (Timeout Error)", false);
                //ShowErrorMessage("MARK FAIL : MARKING TIMEOUT", false);
                markRunTimer.Stop();

                errInfo.sErrorMessage = "MARKING FAIL = MARKING TIMEOUT";
                errInfo.sErrorFunc = sCurrentFunc;

                ITNTErrorCode("MainWindow", "markRunTimerTick", sProcedure, errInfo);
            }
            catch (Exception ex)
            {

            }

            //ShowCurrentStateLabel(0);

            //for test
#if AGING_TEST
            ShowCurrentStateLabel(0);
            Task.Delay(500).Wait();
            Util.WritePrivateProfileValue("PLC", "SIGNAL", "00FF0000", "TEST.ini");
            Task.Delay(500).Wait();
            Util.WritePrivateProfileValue("PLC", "SIGNAL", "00FF0001", "TEST.ini");
#endif
        }

        private void ClearCurrentMarkingInformation()
        {
            if (CheckAccess())
            {
                lblCurrentSeq.Content = "";
                lblCurrentType.Content = "";
                cvsshowChar00.Children.Clear();
                cvsshowChar01.Children.Clear();
                cvsshowChar02.Children.Clear();
                cvsshowChar03.Children.Clear();
                cvsshowChar04.Children.Clear();
                cvsshowChar05.Children.Clear();
                cvsshowChar06.Children.Clear();
                cvsshowChar07.Children.Clear();
                cvsshowChar08.Children.Clear();
                cvsshowChar09.Children.Clear();
                cvsshowChar10.Children.Clear();
                cvsshowChar11.Children.Clear();
                cvsshowChar12.Children.Clear();
                cvsshowChar13.Children.Clear();
                cvsshowChar14.Children.Clear();
                cvsshowChar15.Children.Clear();
                cvsshowChar16.Children.Clear();
                cvsshowChar17.Children.Clear();
                cvsshowChar18.Children.Clear();
            }
            else
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    lblCurrentSeq.Content = "";
                    lblCurrentType.Content = "";
                    cvsshowChar00.Children.Clear();
                    cvsshowChar01.Children.Clear();
                    cvsshowChar02.Children.Clear();
                    cvsshowChar03.Children.Clear();
                    cvsshowChar04.Children.Clear();
                    cvsshowChar05.Children.Clear();
                    cvsshowChar06.Children.Clear();
                    cvsshowChar07.Children.Clear();
                    cvsshowChar08.Children.Clear();
                    cvsshowChar09.Children.Clear();
                    cvsshowChar10.Children.Clear();
                    cvsshowChar11.Children.Clear();
                    cvsshowChar12.Children.Clear();
                    cvsshowChar13.Children.Clear();
                    cvsshowChar14.Children.Clear();
                    cvsshowChar15.Children.Clear();
                    cvsshowChar16.Children.Clear();
                    cvsshowChar17.Children.Clear();
                    cvsshowChar18.Children.Clear();
                }));
            }
        }

        private void ShowIdleStatus()
        {
            //int charNum = 0;
            string ctrlName = "";
            Canvas[] cvsshowChar = new Canvas[19];
            //Brush brush;

            string colorstring = "#FFE1E1E1";
            Color color = (Color)ColorConverter.ConvertFromString(colorstring);
            //brush = new SolidColorBrush(color);
            if (CheckAccess())
            {
                lblCurrentSeq.Background = new SolidColorBrush(color);
                lblCurrentType.Background = new SolidColorBrush(color);

                for (int i = 0; i < 19; i++)
                {
                    ctrlName = string.Format("cvsshowChar{0:D2}", i);
                    cvsshowChar[i] = (Canvas)FindName(ctrlName);
                    if (cvsshowChar[i] == null)
                        continue;

                    cvsshowChar[i].Background = new SolidColorBrush(color);
                }
            }
            else
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    lblCurrentSeq.Background = new SolidColorBrush(color);
                    lblCurrentType.Background = new SolidColorBrush(color);

                    for (int i = 0; i < 19; i++)
                    {
                        ctrlName = string.Format("cvsshowChar{0:D2}", i);
                        cvsshowChar[i] = (Canvas)FindName(ctrlName);
                        if (cvsshowChar[i] == null)
                            continue;

                        cvsshowChar[i].Background = new SolidColorBrush(color);
                    }
                }));
            }
        }

        private async void GoScannerLinkPosition(object obj)
        {
            string className = "MainWindow";
            string funcName = "GoScannerLinkPosition";
            ITNTResponseArgs recvArg = new ITNTResponseArgs();
            PatternValueEx pattern = (PatternValueEx)obj;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                //set speed
                m_currCMD = (byte)'L';
                recvArg = await MarkControll.LoadSpeed(m_currCMD, currMarkInfo.currMarkData.pattern.speedValue.initSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4Fast);
                if (recvArg.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-l ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                m_currCMD = (byte)'M';
                recvArg = await MarkControll.GoPoint((short)(pattern.scanValue.linkPos * pattern.headValue.stepLength + 0.5), (short)(pattern.headValue.park3DPos.Y * pattern.headValue.stepLength + 0.5), (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5), 0);
                if (recvArg.execResult != 0)
                {
                    doingCommand = false;
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GO Link POS ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //Set Link OFF
                recvArg = await plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                if (recvArg.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //wait 400ms
                Stopwatch swLink = new Stopwatch();
                swLink.Start();
                while (swLink.ElapsedMilliseconds < 800)
                {
                    await Task.Delay(50);
                }

                //Get Link Status 
                recvArg = await plcComm.ReadLinkStatusAsync();
                if (recvArg.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : {0}", recvArg.execResult), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (recvArg.recvString.Length < 8)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GetLinkAsync ERROR : PLC STRING LENGTH SHORT {0}", recvArg.recvString.Length), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (recvArg.recvString.Substring(4, 4) == "0002")
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LinkAsync : " + recvArg.recvString, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {

            }
        }

        private void UpdateCompleteDatabaseThread(DataGrid grid, bool showflag, byte completeFlag)
        {
            string className = "MainWIndow";
            string funcName = "UpdateCompleteDatabaseThread";

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dtable = new DataTable();

            DataRowView row = null;
            object obj = new object();
            int count = 0;
            int totcount = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                row = GetNextMarkPointData();
                if (completeFlag == 0)
                {
                    if ((currMarkInfo.currMarkData.mesData.userDataType != 2) && (currMarkInfo.currMarkData.mesData.userDataType != 3))
                        dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET ISMARK='Y', COMPLETE='Y' WHERE RAWVIN='" + currMarkInfo.currMarkData.mesData.rawvin + "' AND SEQUENCE='" + currMarkInfo.currMarkData.mesData.sequence + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dtable, ref obj);
                }
                //else
                //    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET ISMARK='Y', COMPLETE='Y' WHERE VIN='" + currMarkInfo.currMarkData.mesData.vin + "' AND SEQUENCE='" + currMarkInfo.currMarkData.mesData.sequence + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dtable, ref obj);

                if (showflag)
                {
                    ShowPlanDataList4Thread(grid);//, dtable);
                    //count = GetMarkPlanDataCount(dgdPlanData);
                    (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                    ShowWorkPlanCount4Thread(lblWorkPlanDataCount, count);
                    ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                    CheckPlanDataCountWarning(count, lblPlanDataWarning);
                    ScrollViewToPoint(dgdPlanData);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private int CheckDisplacement(MarkVINInformEx minfo)
        {
            int retval = 0;
            //string cartype = minfo.mesData.rawcartype.Trim().Substring(0, 1);
            string cartype = minfo.currMarkData.mesData.cartype.Trim();
            string value = "";
            double leftdisplacement = 0;
            double rightdisplacement = 0;
            //Util.GetPrivateProfileValue("AVAILABLE", cartype, "1|8", ref value, Constants.DISPLACEMENT_INI_FILE);
            Util.GetPrivateProfileValue("AVAILABLE", cartype, "6|6", ref value, Constants.DISPLACEMENT_INI_FILE);
            string[] vals = value.Split('|');
            if (vals.Length >= 2)
            {
                double.TryParse(vals[0], out leftdisplacement);
                double.TryParse(vals[1], out rightdisplacement);
            }

            if ((minfo.distance.distance1 >= leftdisplacement) && (minfo.distance.distance2 <= rightdisplacement))
                return 0;
            else
            {
                retval = (int)(minfo.distance.distance1 - leftdisplacement);
            }
            return retval;
        }


        private async Task<MESReceivedData> GetCurrentMarkData(DataGrid grid)
        {
            string className = "MainWindow";
            string funcName = "GetCurrentMarkData";

            //DataRow row = null;
            MESReceivedData retval = new MESReceivedData();
            bool bfind = false;
            string value = "";
            int checkvalue = 0;
            string sCurrentFunc = "GET CURRENT MARK DATA";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                retval.userDataType = 0;

                foreach (DataRowView row in grid.Items)
                {
                    value = row.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString();
                    Int32.TryParse(value, out checkvalue);
                    if (checkvalue != 0)
                    {
                        bfind = true;

                        DateTime dateValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                        retval.productdate = dateValue.ToString("yyyy-MM-dd");

                        retval.sequence = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                        retval.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                        retval.bodyno = row.Row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                        retval.rawvin = GetRawVIN(row.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                        //retval.vin = row.Row.ItemArray[Constants.DB_NAME_VIN].ToString();
                        retval.markvin = AddMonthCode(row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString());

                        //recv.mesdate = row.Row.row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                        //recv.mestime = row.Row.row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                        dateValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                        DateTime timeValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                        retval.mesdate = dateValue.ToString("yyyy-MM-dd");
                        retval.mestime = timeValue.ToString("HH:mm:ss");

                        retval.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                        retval.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                        retval.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                        retval.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                        retval.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                        retval.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                        retval.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                        retval.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                        retval.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                        retval.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                        retval.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                        //recv.markdate = row.Row.row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString();
                        //recv.marktime = row.Row.row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString();
                        dateValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                        timeValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                        retval.markdate = dateValue.ToString("yyyy-MM-dd");
                        retval.marktime = timeValue.ToString("HH:mm:ss");

                        retval.remark = row.Row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                        retval.exist = row.Row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                        retval.isInserted = row.Row.ItemArray[Constants.DB_NAME_ISINSERT].ToString(); 
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                        return retval;
                    }
                }

                if (bfind == false)
                {
                    ////retval.errorMessage = "FIND DATA FAIL : ROW NULL";
                    //retval.errorInfo.sErrorMessage = "NO SET DATA FOUND";
                    //retval.errorInfo.sErrorFunc = sCurrentFunc;
                    //retval.execResult = -0x14;
                    ////ShowLog(className, funcName, 2, "다음 데이터 찾기 실패", "ROW NULL");

                    retval.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_NOT_FOUND;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CANNOT FIND DATA - row is null", Thread.CurrentThread.ManagedThreadId);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                //retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                //retval.errorInfo.sErrorFunc = sCurrentFunc;

                //retval.execResult = ex.HResult;
                //retval.errorInfo.sErrorFunc = sCurrentFunc;
                //retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                //retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_DATA + Constants.ERROR_CAUSE2_NOTFOUND;

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");


                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;


                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        //private async Task<int> ShowNextMarkData(DataGrid grid, int bypassmode, byte displayflag, bool showFlag = true)
        /// <summary>
        /// 
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="selecttype">0=NEXT, 1=CURRENT</param>
        /// <returns></returns>
        private async Task<ITNTResponseArgs> MakeCurrentMarkData(DataGrid grid, byte selecttype = 0)//, int bypassmode, byte displayflag)//, bool showFlag = true)
        {
            string className = "MainWindow";
            string funcName = "MakeCurrentMarkData";

            string ErrorCode = "";
            MESReceivedData mesData = new MESReceivedData();
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            byte useChinaOption = 0;
            string sCurrentFunc = "MAKE CURRENT MARK DATA";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                currMarkInfo.Initialize();

                if (selecttype != 0)
                {
                    mesData = await GetCurrentMarkData(grid);
                }
                else
                {
                    mesData = await GetNextMarkDataInfomation(grid);//, 1);
                }
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
                currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.mesData.rawbodytype, currMarkInfo.currMarkData.mesData.rawtrim);
                if (currMarkInfo.currMarkData.pattern.name.Length <= 0)
                {
                    retval.execResult = ErrorCodeConstant.ERROR_FILE_NOT_FOUND;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = "PATTERN NAME IS INVALID";
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_PARAM + Constants.ERROR_INVALID;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }

                if (useChinaOption != 0)
                {
                    retval = await plcComm.ReadPLCChinaFlag();
                    if (retval.execResult != 0)
                    {
                        //retval.sErrorMessage = "PLC 중국 옵션 읽기 실패 : 통신 실패";
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCChinaFlag ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCChinaFlag) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        return retval;
                    }

                    if (retval.recvString.Length < 8)
                    {
                        //retval.sErrorMessage = "PLC 중국 옵션 읽기 실패 : 데이터 길이 이상";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCChinaFlag LENGTH = " + retval.recvString.Length.ToString(), Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + retval.recvString.Length + " - " + retval.recvString;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        return retval;
                    }

                    //if (showFlag)
                    //{
                    //    ShowMarkingDataList(true, false);
                    //}
                    string chinaflag = retval.recvString.Substring(4, 4);
                    byte.TryParse(chinaflag, out currMarkInfo.currMarkData.multiMarkFlag);
                }
                else
                    currMarkInfo.currMarkData.multiMarkFlag = 0;

#if MANUAL_MARK
                ImageProcessManager.GetPatternDataManual(currMarkInfo.currMarkData.pattern.name, currMarkInfo.currMarkData.mesData.rawcartype, ref currMarkInfo.currMarkData.pattern);
#else
                retval = ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
                if (retval.execResult != 0)
                {
                    //ShowLog(className, funcName, 2, "[] ", retval.sErrorMessage);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetPatternValue ERROR = " + retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
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
                    return retval;
                }

                if ((bHeadType == 1) && (currMarkInfo.currMarkData.pattern.laserValue.density == 1))
                    GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);

                currMarkInfo.currMarkData.isReady = true;
                retval.execResult = 0;
                //if (showFlag)
                //    await ShowCurrentMarkingInformation2(bypassmode, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, displayflag, 1);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowNextMarkData", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;

                return retval;
            }
        }

        //        private async Task<ITNTResponseArgs> ShowNextMarkData(DataGrid grid, int bypassmode, byte displayflag, bool showFlag = true)
        //        {
        //            string ErrorCode = "";
        //            //int retval = 0;
        //            MESReceivedData retval = new MESReceivedData();

        //            try
        //            {
        //                currMarkInfo.Initialize();
        //                retval = await GetNextMarkDataInfomation(grid);//, 1);
        //                //retval = GetNextMarkDataInfomation2(grid, ref currMarkInfo.currMarkData.mesData, 1);
        //                if (retval.execResult != 0)
        //                    return retval.execResult;

        //                currMarkInfo.currMarkData.mesData = (MESReceivedData)retval.Clone();
        //                if (currMarkInfo.currMarkData.mesData.userDataType == 3)
        //                {
        //                    currMarkInfo.currMarkData.isReady = true;
        //                    return 0;
        //                }

        //                currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData);

        //                if (showFlag)
        //                {
        //                    ShowMarkingDataList(true, false);
        //                }

        //#if MANUAL_MARK
        //                    ImageProcessManager.GetPatternDataManual(currMarkInfo.currMarkData.pattern.name, currMarkInfo.currMarkData.mesData.rawcartype, ref currMarkInfo.currMarkData.pattern);
        //#else
        //                retval.execResult = ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
        //                if (retval.execResult != 0)
        //                {
        //                    return -1;
        //                }

        //#endif
        //                //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
        //                //List<List<FontDataClass>> revData = new List<List<FontDataClass>>();
        //                //PatternValueEx pattern = new PatternValueEx();
        //                //ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
        //                VinNoInfo vininfo = new VinNoInfo();
        //                vininfo.fontName = currMarkInfo.currMarkData.pattern.fontValue.fontName;
        //                vininfo.vinNo = currMarkInfo.currMarkData.mesData.vin;
        //                vininfo.width = currMarkInfo.currMarkData.pattern.fontValue.width;
        //                vininfo.height = currMarkInfo.currMarkData.pattern.fontValue.height;
        //                vininfo.pitch = currMarkInfo.currMarkData.pattern.fontValue.pitch;
        //                vininfo.thickness = currMarkInfo.currMarkData.pattern.fontValue.thickness;

        //                //byte fontdirection = 0; string value = "";
        //                //Util.GetPrivateProfileValue("OPTION", "FONTDIRECTION", "0", ref value, Constants.PARAMS_INI_FILE);
        //                //byte.TryParse(value, out fontdirection);

        //                retval.execResult = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref currMarkInfo.currMarkData.fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref ErrorCode);
        //                if (retval.execResult != 0)
        //                {
        //                    return -1;
        //                }

        //                //for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++)
        //                //{
        //                //    List<FontDataClass> FontDataClass = new List<FontDataClass>();
        //                //    ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.currMarkData.pattern.fontValue.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
        //                //    currMarkInfo.currMarkData.fontData.Add(fontData);
        //                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowNextMarkData", string.Format("FONT DATA {0}CH, {1}PT", i, fontData.Count));
        //                //}

        //                if ((bHeadType == 1) && (currMarkInfo.currMarkData.pattern.laserValue.density == 1))
        //                    GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);

        //                currMarkInfo.currMarkData.isReady = true;
        //                if (showFlag)
        //                    //await ShowCurrentMarkingInformation(bypassmode, currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.mesData.sequence, currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.pattern, displayflag);
        //                    await ShowCurrentMarkingInformation2(bypassmode, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, displayflag, 1);
        //                return 0;
        //            }
        //            catch (Exception ex)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowNextMarkData", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //                return ex.HResult;
        //            }
        //        }

        private int ShowMultiDataWarning(int count)
        {
            int retval = 0;
            bool ret = false;
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                //msg1.Message = "Select [NO] to reset point.";
                //msg1.Fontsize = 18;
                //msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg1.VerticalContentAlignment = VerticalAlignment.Center;
                //msg1.Foreground = Brushes.Blue;
                //msg1.Background = Brushes.White;

                msg2.Message = "There are " + count + " data with the same sequence.";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                msg3.Message = "Please delete unused data.";
                msg3.Fontsize = 18;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Red;
                msg3.Background = Brushes.White;

                //msg4.Message = "Select [NO] to reset point.";
                //msg4.Fontsize = 18;
                //msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg4.VerticalContentAlignment = VerticalAlignment.Center;
                //msg4.Foreground = Brushes.Blue;
                //msg4.Background = Brushes.White;

                if (CheckAccess())
                {
                    ConfirmWindow warning = new ConfirmWindow("Confirm Window", msg1, msg2, msg3, msg4, "OK", "", this);
                    warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    ret = warning.ShowDialog().Value;
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        ConfirmWindow warning = new ConfirmWindow("Confirm Window", msg1, msg2, msg3, msg4, "OK", "", this);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        ret = warning.ShowDialog().Value;
                    }));
                }

                if (ret == true)
                    return 1;
                else
                    return 0;
                //return retval;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
            //return retval;
        }

        //        private int GetNextMarkDataInfomation(DataGrid grid, ref MESReceivedData recvMsg, byte setFlag)
        //        {
        //            string className = "MainWindow";
        //            string funcName = "GetNextMarkDataInfomation";
        //            int retval = 0;
        //            //string value = "";
        //            int carcomptype = 0;
        //            try
        //            {
        //                //Util.GetPrivateProfileValue("OPTION", "CARTYPECOMPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
        //                carcomptype = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - COMMTYPE = " + carcomptype.ToString(), Thread.CurrentThread.ManagedThreadId);
        //#if AGING_TEST_DATA
        //                retval = GetNextMarkDataInfomation_AgingTest(dgdPlanData, ref recvMsg, setFlag);

        //#else
        //                if (carcomptype == 1)
        //                    retval = GetNextMarkDataInfomation_Sub1(dgdPlanData);//, ref recvMsg, setFlag);
        //                else if (carcomptype == 2)
        //                    retval = GetNextMarkDataInfomation_Sub2(dgdPlanData, ref recvMsg, setFlag);
        //                else
        //                    retval = GetNextMarkDataInfomation_Sub0(dgdPlanData, ref recvMsg, setFlag);
        //#endif
        //                return retval;
        //            }
        //            catch (Exception ex)
        //            {
        //                //ITNTJobLog.Instance.Trace(0, "GET DATA EXCEPTION");
        //                //ITNTErrorLog.Instance.Trace(0, "GET DATA EXCEPTION");
        //                recvMsg.errorMessage = "GET DATA EXCEPTION";
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //                return ex.HResult;
        //            }
        //        }

        private async Task<MESReceivedData> GetNextMarkDataInfomation(DataGrid grid)
        {
            string className = "MainWindow";
            string funcName = "GetNextMarkDataInfomation";
            MESReceivedData mesData = new MESReceivedData();
            int nextdatatype = 0;
            string sCurrentFunc = "GET NEXT MARK DATA";

            try
            {
                //Util.GetPrivateProfileValue("OPTION", "CARTYPECOMPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                nextdatatype = (int)Util.GetPrivateProfileValueUINT("OPTION", "NEXTDATATYPE", 0, Constants.PARAMS_INI_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START - COMMTYPE = " + nextdatatype.ToString(), Thread.CurrentThread.ManagedThreadId);
#if AGING_TEST_DATA
                mesData = GetNextMarkDataInfomation_AgingTest(dgdPlanData, ref recvMsg, setFlag);
#else
                if (nextdatatype == 1)          // Read plc sequence and find data
                    mesData = await GetNextMarkDataInfomation_Sub1(dgdPlanData);//, ref recvMsg, setFlag);
                else if (nextdatatype == 2)    // Next data followed checked data
                    mesData = await GetNextMarkDataInfomation_Sub2(dgdPlanData);//, ref recvMsg, setFlag);
                else                           // current checked data
                    mesData = await GetNextMarkDataInfomation_Sub0(dgdPlanData);//, ref recvMsg, setFlag);
#endif
                return mesData;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                mesData.execResult = ex.HResult;
                mesData.errorInfo.sErrorFunc = sCurrentFunc;
                mesData.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                mesData.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-mesData.execResult).ToString("X2");

                mesData.errorInfo.devErrorInfo.execResult = mesData.execResult;
                mesData.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                mesData.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                mesData.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                mesData.errorInfo.devErrorInfo.sErrorMessage = mesData.errorInfo.sErrorMessage;

                return mesData;
            }
        }

        /// <summary>
        /// SEQUENCE를 PLC로부터 읽은 후 해당 SEQEUNCE를 찾아서 리턴
        /// 조지아
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        private async Task<MESReceivedData> GetNextMarkDataInfomation_Sub1(DataGrid grid)//, ref MESReceivedData recvMsg, byte setFlag)
        {
            string className = "MainWindow";
            string funcName = "GetNextMarkDataInfomation_1";
            MESReceivedData retval = new MESReceivedData();
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            ITNTResponseArgs recvarg2 = new ITNTResponseArgs();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbMainDataTable = new DataTable();
            DataTable dbTable = new DataTable();
            object obj = new object();
            string vin = "";
            string seq = "";
            string cartype = "";
            //int retval = 0;
            DataRow row = null;
            string sCurrentFunc = "GET NEXT MARK DATA 1";

#if AGING_TEST_PLC
            bool bfind = false;
            string nextseq = "";
            string nexttype = "";
#endif
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                retval.userDataType = 0;
                // 1. Read Next Sequence
                recvarg = await plcComm.ReadPLCSequence();
                if (recvarg.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC Sequence Error(" + recvarg.execResult.ToString() + ") : " + recvarg.recvString, Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = recvarg.execResult;

                    retval.errorInfo = (ErrorInfo)recvarg.errorInfo.Clone();
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCSequence) ERROR = " + recvarg.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PLC SEQ : " + recvarg.recvString, Thread.CurrentThread.ManagedThreadId);

#if AGING_TEST_PLC
                foreach (DataRowView rv in grid.Items)
                {
                    if (rv.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString() != "0")
                    {
                        bfind = true;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "FIND DATA", Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    if (bfind)
                    {
                        nextseq = rv.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                        if (nextseq.Length >= 4)
                            recvarg.recvString = "00FF" + nextseq;
                        else
                        {
                            recvarg.recvString = "00FF" + nextseq;
                            recvarg.recvString.PadRight(4, '0');
                        }
                        nexttype = rv.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                        break;
                    }
                }
                if (bfind == false)
                {
                    DataRowView rv2 = grid.Items[0] as DataRowView;
                    nextseq = rv2.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                    if (nextseq.Length >= 4)
                        recvarg.recvString = "00FF" + nextseq;
                    else
                    {
                        recvarg.recvString = "00FF" + nextseq;
                        recvarg.recvString.PadRight(4, '0');
                    }
                    nexttype = rv2.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC STRING - TEST(" + recvarg.recvString.Length.ToString() + ") : " + recvarg.recvString, Thread.CurrentThread.ManagedThreadId);
#endif
                if (recvarg.recvString.Length < 8)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCSequence Length ERROR " + recvarg.recvString.Length + " - " + recvarg.recvString, Thread.CurrentThread.ManagedThreadId);
                    //retval.execResult = recvarg.execResult;
                    retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                    //retval.errorInfo = (ErrorInfo)recvarg.errorInfo.Clone();

                    retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCSequence) : " + recvarg.recvString.Length + " - " + recvarg.recvString;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }

                seq = recvarg.recvString.Substring(4, 4);

                // 2. Find Next Sequence from DB 
#if AGING_TEST_PLC
                dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where SEQUENCE='" + seq + "'", CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
#else
                //dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where SEQUENCE='" + seq + "' AND ISMARK='N'", CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where SEQUENCE='" + seq + "'", CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
#endif
                DataRow[] rows = dbMainDataTable.Select();

                // 2.1 There is No Data
                if (rows.Length <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "There is no data from db - sequence", Thread.CurrentThread.ManagedThreadId);

                    retval.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                    retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_NOT_FOUND;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }
                // 2.2 Only One Sequence
                else if (rows.Length == 1)
                {
                    row = rows[0];
                    retval.execResult = 0;
                    retval.userDataType = 0;
                    //recvarg.execResult = 0;
                }
                //2.3 Duplicated Sequence Case
                else //if (rows.Length > 1)
                {
                    //2.3.1 Read Car Type
                    recvarg2 = await plcComm.ReadPLCCarType();
                    if (recvarg2.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC Car Type Error(" + recvarg2.execResult.ToString() + ") : " + recvarg2.recvString, Thread.CurrentThread.ManagedThreadId);

                        retval.execResult = recvarg2.execResult;
                        retval.errorInfo = (ErrorInfo)recvarg2.errorInfo.Clone();
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCCarType) ERROR = " + recvarg2.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }

                    //recvarg2.recvString = Encoding.UTF8.GetString(recvarg2.recvBuffer);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadPLCCarType 1 {0}", recvarg2.recvString), Thread.CurrentThread.ManagedThreadId);
#if AGING_TEST_PLC
                    if (nexttype.Length >= 4)
                        recvarg2.recvString = "00FF" + nexttype;
                    else
                    {
                        recvarg2.recvString = "00FF" + nexttype;
                        recvarg2.recvString.PadRight(4, '0');
                    }
#endif
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadPLCCarType 2 {0}", recvarg2.recvString), Thread.CurrentThread.ManagedThreadId);
                    if (recvarg2.recvString.Length < 8)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC Car Type is invalid.(" + recvarg2.recvString.Length.ToString() + ") : " + recvarg2.recvString, Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                        //retval.errorInfo = (ErrorInfo)recvarg.errorInfo.Clone();
                        retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + recvarg2.recvString.Length + " - " + recvarg2.recvString;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                        retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                        return retval;
                    }

                    cartype = recvarg2.recvString.Substring(4, 4);
                    cartype = cartype.Trim();
                    //2.1.2 Find Sequnece and Car Type from DB
                    dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where SEQUENCE='" + seq + "' AND RAWCARTYPE='" + cartype + "'"/* AND ISMARK='N'"*/, CommandMode.Reader, CommandTypeEnum.Text, ref dbTable, ref obj);
                    DataRow[] rows2 = dbTable.Select();
                    //Only one Data (Sequence + Car Type)
                    if (rows2.Length == 1)
                    {
                        row = rows2[0];
                        retval.execResult = 0;
                        retval.userDataType = 0;
                        //recvarg.execResult = 0;
                        //recvMsg
                    }
                    //Duplicated Data or No Data
                    else
                    {
                        DataTable dt = new DataTable();
                        //int selResult = 0;
                        //DataRow row2 = new DataRow();
                        //string cmd = "SELECT * from " + tableName + " where SEQUENCE='" + seq + "' AND ISMARK='N'";
                        string cmd = "SELECT * from " + tableName + " where SEQUENCE='" + seq + "'";
                        dbwrap.ExecuteCommand(Constants.connstring, cmd, CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);
                        if (CheckAccess())
                        {
                            retval = SelectMarkData(dt);
                        }
                        else
                        {
                            retval = (MESReceivedData)Dispatcher.Invoke(new Func<MESReceivedData>(delegate
                            {
                                MESReceivedData data = new MESReceivedData();
                                data = SelectMarkData(dt);
                                return data;
                            }));
                        }

                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                        if (retval.execResult != 0)
                        {
                            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "THERE IS NO DATA TO BE MARKDED - ", Thread.CurrentThread.ManagedThreadId);
                            //retval.execResult = recvarg.execResult;
                            //retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + recvarg.recvString.Length + " - " + recvarg.recvString;
                            //retval.errorInfo.sErrorFunc = sCurrentFunc;

                            //ShowLog(className, funcName, 2, " - CONNECTION FAIL", recvarg.execResult.ToString());
                            //ITNTErrorCode();
                            return retval;
                        }

                        return retval;
                    }
                }

                if (row != null)
                {
                    DateTime dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                    retval.productdate = dateValue.ToString("yyyy-MM-dd");

                    retval.sequence = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                    retval.rawcartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    retval.bodyno = row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                    retval.rawvin = GetRawVIN(row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                    //retval.vin = row.ItemArray[Constants.DB_NAME_VIN].ToString();
                    retval.markvin = AddMonthCode(retval.rawvin);

                    //recv.mesdate = row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                    //recv.mestime = row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                    DateTime timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                    retval.mesdate = dateValue.ToString("yyyy-MM-dd");
                    retval.mestime = timeValue.ToString("HH:mm:ss");

                    retval.lastsequence = row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                    retval.code219 = row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                    retval.idplate = row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                    retval.delete = row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                    retval.totalmsg = row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                    retval.rawbodytype = row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                    retval.rawtrim = row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                    retval.region = row.ItemArray[Constants.DB_NAME_REGION].ToString();
                    retval.bodytype = row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                    retval.cartype = row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                    retval.plcvalue = row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                    timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                    retval.markdate = dateValue.ToString("yyyy-MM-dd");
                    retval.marktime = timeValue.ToString("HH:mm:ss");

                    retval.remark = row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                    retval.exist = row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                    retval.isInserted = row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED1 END", Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "row is invlaid", Thread.CurrentThread.ManagedThreadId);

                    retval.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_NOT_FOUND;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }

                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.userDataType = 0;

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }
        }

        private async Task<MESReceivedData> GetNextMarkDataInfomation_AgingTest(DataGrid grid)//, ref MESReceivedData recvMsg, byte setFlag)
        {
            string className = "MainWindow";
            string funcName = "GetNextMarkDataInfomation_AgingTest";
            MESReceivedData mesData = new MESReceivedData();
            DataRow row = null;
            bool bfind = false;
            try
            {
                mesData.userDataType = 0;

#if AGING_TEST_PLC
                foreach (DataRowView rv in grid.Items)
                {
                    if (rv.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString() != "0")
                    {
                        bfind = true;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "FIND DATA", Thread.CurrentThread.ManagedThreadId);
                        row = rv.Row;
                        break;
                    }
                }
                if (bfind == false)
                {
                    if(grid.Items.Count <= 0)
                    {
                        //ITNTJobLog.Instance.Trace(0, "CANNOT FIND DATA");
                        //ITNTErrorLog.Instance.Trace(0, "CANNOT FIND DATA");

                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NO MES DATA FOUND.", Thread.CurrentThread.ManagedThreadId);
                        mesData.errorInfo.sErrorMessage = "CANNOT FIND DATA";
                        //ShowErrorMessage("CANNOT FIND DATA", false);
                        mesData.execResult = -0x13;
                        return mesData;
                    }

                    DataRowView rv2 = grid.Items[0] as DataRowView;
                    row = rv2.Row;
                }
#endif

                mesData.execResult = 0;
                mesData.userDataType = 0;

                if (row != null)
                {
                    DateTime dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                    mesData.productdate = dateValue.ToString("yyyy-MM-dd");

                    mesData.sequence = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                    mesData.rawcartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    mesData.bodyno = row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                    mesData.rawvin = GetRawVIN(row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                    mesData.markvin = AddMonthCode(mesData.rawvin);

                    //recv.mesdate = row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                    //recv.mestime = row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                    DateTime timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                    mesData.mesdate = dateValue.ToString("yyyy-MM-dd");
                    mesData.mestime = timeValue.ToString("HH:mm:ss");

                    mesData.lastsequence = row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                    mesData.code219 = row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                    mesData.idplate = row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                    mesData.delete = row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                    mesData.totalmsg = row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                    mesData.rawbodytype = row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                    mesData.rawtrim = row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                    mesData.region = row.ItemArray[Constants.DB_NAME_REGION].ToString();
                    mesData.bodytype = row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                    mesData.cartype = row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                    mesData.plcvalue = row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                    timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                    mesData.markdate = dateValue.ToString("yyyy-MM-dd");
                    mesData.marktime = timeValue.ToString("HH:mm:ss");

                    mesData.remark = row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                    mesData.exist = row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                    mesData.isInserted = row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                    //recvMsg = (MESReceivedData)ret.Clone();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED1 END", Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    //ITNTErrorLog.Instance.Trace(0, "USER CANCEL SELECTION DATA");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "row is invlaid", Thread.CurrentThread.ManagedThreadId);
                    mesData.errorInfo.sErrorMessage = "USER CANCEL SELECTION DATA";
                    mesData.execResult = -0x19;
                    return mesData;
                }

                return mesData;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                mesData.userDataType = 0;
                mesData.execResult = ex.HResult;
                return mesData;
            }
        }

        /// <summary>
        /// 현재 CHECKFLAG = 1 인 데이터를 찾아서 리턴
        /// 42라인
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        private async Task<MESReceivedData> GetNextMarkDataInfomation_Sub0(DataGrid grid)//, ref MESReceivedData recvMsg, byte setFlag)
        {
            string className = "MainWindow";
            string funcName = "GetNextMarkDataInfomation_Sub0";
            DataRow row = null;
            MESReceivedData retval = new MESReceivedData();
            //ITNTResponseArgs recvarg = new ITNTResponseArgs();
            ITNTResponseArgs recvarg2 = new ITNTResponseArgs();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbMainDataTable = new DataTable();
            DataTable dbTable = new DataTable();
            object obj = new object();
            string seq = "";
            //int retval = 0;
            string cartype = "";
            string sCurrentFunc = "GET NEXT MARK DATA 0";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                retval.userDataType = 0;

                // 1. Chek Flag = 1 찾기
                dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where CHECKFLAG=1", CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                DataRow[] rows = dbMainDataTable.Select();

                // 1.1
                if (rows.Length > 1)
                {
                    //ShowLog(className, funcName, 0, " 중복");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK FLAG DUPLICATE - " + rows.Length.ToString(), Thread.CurrentThread.ManagedThreadId);

                    seq = rows[0].ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                    recvarg2 = await plcComm.ReadPLCCarType();
                    if (recvarg2.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO PLC (ReadPLCCarType) ERROR = " + recvarg2.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo = (ErrorInfo)recvarg2.errorInfo.Clone();
                        retval.execResult = recvarg2.execResult;
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCCarType) ERROR = " + recvarg2.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }

                    //recvarg2.recvString = Encoding.UTF8.GetString(recvarg2.recvBuffer, 0, recvarg2.recvSize);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadPLCCarType {0}", recvarg2.recvString), Thread.CurrentThread.ManagedThreadId);
                    if (recvarg2.recvString.Length < 8)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC Car Type is invalid.(" + recvarg2.recvString.Length.ToString() + ") : " + recvarg2.recvString, Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = recvarg2.execResult;
                        retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + recvarg2.recvString.Length + " - " + recvarg2.recvString;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;// + (-retval.execResult).ToString("X2");

                        retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                        return retval;
                    }

                    cartype = recvarg2.recvString.Substring(4, 4);
                    cartype = cartype.Trim();

                    dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where SEQUENCE='" + seq + "' AND RAWCARTYPE='" + cartype + "' AND ISMARK='N'", CommandMode.Reader, CommandTypeEnum.Text, ref dbTable, ref obj);
                    DataRow[] rows2 = dbTable.Select();
                    row = rows2[0];
                    //recvarg.execResult = 0;
                }
                else if (rows.Length == 1)
                {
                    row = rows[0];
                    //recvarg.execResult = 0;
                }
                else
                {
                    foreach (DataRowView rowview in grid.Items)
                    {
                        if ((rowview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() != "DLT") &&
                            (rowview.Row.ItemArray[Constants.DB_NAME_ISMARK].ToString() != "Y") &&
                            (rowview.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() != "Y") &&
                            (rowview.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() != "N"))
                        {
                            row = rowview.Row;
                            //recvarg.execResult = 0;
                            break;
                        }
                    }
                }

                if (row != null)
                {
                    //vin = row.ItemArray[Constants.DB_NAME_VIN].ToString();
                    //seq = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();

                    DateTime dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                    retval.productdate = dateValue.ToString("yyyy-MM-dd");

                    retval.sequence = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                    retval.rawcartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    retval.bodyno = row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                    retval.rawvin = GetRawVIN(row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                    //retval.rawvin = row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                    retval.markvin = AddMonthCode(retval.rawvin);

                    //recv.mesdate = row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                    //recv.mestime = row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                    DateTime timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                    retval.mesdate = dateValue.ToString("yyyy-MM-dd");
                    retval.mestime = timeValue.ToString("HH:mm:ss");

                    retval.lastsequence = row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                    retval.code219 = row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                    retval.idplate = row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                    retval.delete = row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                    retval.totalmsg = row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                    retval.rawbodytype = row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                    retval.rawtrim = row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                    retval.region = row.ItemArray[Constants.DB_NAME_REGION].ToString();
                    retval.bodytype = row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                    retval.cartype = row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                    retval.plcvalue = row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                    //recv.markdate = row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString();
                    //recv.marktime = row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                    timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                    retval.markdate = dateValue.ToString("yyyy-MM-dd");
                    retval.marktime = timeValue.ToString("HH:mm:ss");

                    retval.remark = row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                    retval.exist = row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                    retval.isInserted = row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                    //recvMsg = (MESReceivedData)ret.Clone();

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED1 END", Thread.CurrentThread.ManagedThreadId);

                    //if (setFlag != 0)
                    //{
                    //    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=0 WHERE CHECKFLAG=1", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    //    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE VIN='" + ret.vin + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    //    //dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE VIN='" + vin + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    //    Util.WritePrivateProfileValue("CURRENT", "SEQVIN", ret.sequence.Trim() + "|" + ret.vin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                    //}
                }
                else
                {
                    //ITNTJobLog.Instance.Trace(0, "CANNOT FIND DATA");
                    //ITNTErrorLog.Instance.Trace(0, "CANNOT FIND DATA");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CANNOT FIND DATA - row is null", Thread.CurrentThread.ManagedThreadId);
                    //retval.errorMessage = "CANNOT FIND DATA";
                    retval.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                    retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_NOT_FOUND;// + (-retval.execResult).ToString("X2");

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;
                    //retval.execResult = -0x14;
                    return retval;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }

            return retval;
        }

        /// <summary>
        /// CHECKFLAG = Flag가 1인 데이터를 찾은 후 그 다음 데이터를 리턴
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        private async Task<MESReceivedData> GetNextMarkDataInfomation_Sub2(DataGrid grid)//, ref MESReceivedData recvMsg, byte setFlag)
        {
            string className = "MainWindow";
            string funcName = "GetNextMarkDataInfomation_Sub2";

            DataRow row = null;
            MESReceivedData retval = new MESReceivedData();
            bool bfind = false;
            string value = "";
            int checkvalue = 0;
            string sCurrentFunc = "GET NEXT MARK DATA 2";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                retval.userDataType = 0;

                foreach (DataRowView rowview in grid.Items)
                {
                    if (bfind == false)
                    {
                        value = rowview.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString();
                        Int32.TryParse(value, out checkvalue);
                        if (checkvalue != 0)
                        {
                            bfind = true;
                            continue;
                        }
                    }
                    else
                    {
                        ////if ((tempview.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() == "N") &&
                        ////    (tempview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() == "N") &&
                        ////    (tempview.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() == "Y"))
                        //if ((rowview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() != "DLT") &&
                        //    (rowview.Row.ItemArray[Constants.DB_NAME_ISMARK].ToString() != "Y") &&
                        //    (rowview.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() != "Y") &&
                        //    (rowview.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() != "N"))
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEQ = " + rowview.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString(), Thread.CurrentThread.ManagedThreadId);
                            row = rowview.Row;
                            break;
                        }
                    }
                }

                if (row != null)
                {
                    //vin = row.ItemArray[Constants.DB_NAME_VIN].ToString();
                    //seq = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();

                    DateTime dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                    retval.productdate = dateValue.ToString("yyyy-MM-dd");

                    retval.sequence = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                    retval.rawcartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    retval.bodyno = row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                    retval.rawvin = GetRawVIN(row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                    //retval.rawvin = row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                    retval.markvin = AddMonthCode(retval.rawvin);

                    //recv.mesdate = row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                    //recv.mestime = row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                    DateTime timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                    retval.mesdate = dateValue.ToString("yyyy-MM-dd");
                    retval.mestime = timeValue.ToString("HH:mm:ss");

                    retval.lastsequence = row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                    retval.code219 = row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                    retval.idplate = row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                    retval.delete = row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                    retval.totalmsg = row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                    retval.rawbodytype = row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                    retval.rawtrim = row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                    retval.region = row.ItemArray[Constants.DB_NAME_REGION].ToString();
                    retval.bodytype = row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                    retval.cartype = row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                    retval.plcvalue = row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                    //recv.markdate = row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString();
                    //recv.marktime = row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                    timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                    retval.markdate = dateValue.ToString("yyyy-MM-dd");
                    retval.marktime = timeValue.ToString("HH:mm:ss");

                    retval.remark = row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                    retval.exist = row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                    retval.isInserted = row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                    //recvMsg = (MESReceivedData)ret.Clone();

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED1 END", Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CANNOT FIND DATA - row is null", Thread.CurrentThread.ManagedThreadId);

                    retval.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_NOT_FOUND;

                    retval.errorInfo.devErrorInfo.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = "NO MARKING DATA FOUND";

                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }
        }


        private MESReceivedData SelectMarkData(DataTable dt)
        {
            string className = "MainWindow";
            string funcName = "SelectMarkData";
            //DataRow row = null;
            MESReceivedData retval = new MESReceivedData();
            string sCurrentFunc = "SELECT MARK DATA";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                SelectSeqDataWindow window = new SelectSeqDataWindow(dt);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == true)
                {
                    retval = (MESReceivedData)window.selectedData.Clone();
                    retval.execResult = 0;
                }
                else
                {
                    //ITNTErrorLog.Instance.Trace(0, "USER CANCEL SELECTION DATA");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "USER CANCEL SELECTION DATA1", Thread.CurrentThread.ManagedThreadId);
                    //retval.errorInfo.sErrorFunc = sCurrentFunc;
                    //retval.errorInfo.sErrorMessage = "USER CANCEL SELECTION DATA";
                    //retval.execResult = -0x19;
                    retval.markvin = "";
                    retval.rawvin = "";

                    retval.execResult = ErrorCodeConstant.ERROR_USER_CANCEL;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = "USER CANCEL SELECTION DATA";
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_USER_CANCEL;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.execResult = ex.HResult;
                //retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                //retval.errorInfo.sErrorFunc = sCurrentFunc;

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }
        }


        private async Task<ITNTResponseArgs> CheckCarTypeProcess(MESReceivedData mesdata, string sProcess)
        {
            string className = "MainWindow";
            string funcName = "CheckCarTypeProcess";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            //int retval = 0;
            int cartypeOption = 0;
            string plcFrameType = "";
            string plcCaption = "";
            string mesFrameType = "";
            string mesCaption = "";
            string scartyperead = "";
            string value = "";
            string sCurrentFunc = "CHECK CAR TYPE";
            string sProcedure = "1";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (mesdata.userDataType != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SKIP FLAG SET", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = 0;
                    return retval;
                }

                cartypeOption = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                if (cartypeOption == 5) //HMI 3
                {
                    retval = await plcComm.ReadBodyNum();
                    if (retval.execResult != 0)
                    {
                        //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadBodyNum ERROR = " + retval.execResult, Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadBodyNum) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    //recvArg.recvString = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadBodyNum {0}", retval.recvString), Thread.CurrentThread.ManagedThreadId);

                    mesCaption = mesdata.bodyno;
                    plcCaption = retval.recvString;

                    mesFrameType = mesCaption;
                    plcFrameType = plcCaption;
                }
                else
                {
#if AGING_TEST_PLC
#else
                    retval = await plcComm.ReadPLCCarType();
                    if (retval.execResult != 0)
                    {
                        //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCCarType ERROR = " + retval.execResult, Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCCarType) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    //recvArg.recvString = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadPLCCarType {0}", retval.recvString), Thread.CurrentThread.ManagedThreadId);
                    if (retval.recvString.Length < 8)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("COMMUNICATION ERROR TO PLC - SHORT LENGTH {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                        retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                        retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + retval.recvString.Length + " - " + retval.recvString;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                        retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    scartyperead = retval.recvString.Substring(4, 4);
                    plcFrameType = scartyperead.Trim();
#endif

                    //cartypeOption = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                    if (cartypeOption == 0)     //HMC U42
                    {
                        //BODY TYPE(MES)
                        mesFrameType = GetFrameType4MES(mesdata.rawcartype.Trim(), mesdata.rawbodytype.Trim(), mesdata.rawtrim.Trim(), "");
                        mesCaption = GetFrameTypeDescription(mesFrameType, 1);
                        plcFrameType = plcFrameType.Replace("0", "");
                        plcCaption = GetFrameTypeDescription(plcFrameType);

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else if (cartypeOption == 1)    //KIA
                    {
                        //plcCaption = GetCarTypeDescription(plcFrameType);
                        //ShowLabelData(plcCaption, lblPLCCARTYPEValue);

                        //BODY TYPE(MES)
                        mesFrameType = mesdata.rawcartype.Trim();
                        //mesCaption = GetCarTypeDescription(plcFrameType);
                        //ShowLabelData(mesCaption, lblMESCARTYPEValue);

                        mesCaption = mesFrameType;
                        plcCaption = plcFrameType;

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else if (cartypeOption == 2)         //HMC U51
                    {
                        mesFrameType = GetCarTypeFromCarName(mesdata.rawcartype.Trim(), "");

                        //plcCaption = plcFrameType.Substring(2,2);
                        //plcFrameType = GetCarTypeFromPLC(plcCaption);
#if AGING_TEST_PLC
                        plcFrameType = GetCarTypeFromPLC(mesFrameType);
                        plcFrameType = "00" + plcFrameType;
#endif

                        plcCaption = plcFrameType.Substring(2, 2);
                        plcFrameType = GetCarTypeFromCarName(plcCaption, plcCaption);
                        //plcFrameType = GetCarTypeFromPLC(plcCaption);

                        mesCaption = mesFrameType;
                        plcCaption = plcFrameType;

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else
                    {
                        //recvArg.recvString = recvArg.recvString.Substring(4, 4);
                        //plcFrameType = recvArg.recvString.Trim();
                        plcCaption = GetFrameTypeDescription(plcFrameType, 1);

                        plcFrameType = scartyperead.Trim();

                        mesFrameType = mesdata.rawcartype.Trim();
                        mesCaption = mesdata.rawcartype.Trim();

#if AGING_TEST_PLC
                        plcCaption = mesCaption;
#endif
                        plcFrameType = plcCaption;
                    }
                }
                ShowLabelData(plcCaption, lblPLCCARTYPEValue);
                ShowLabelData(mesCaption, lblMESCARTYPEValue);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[2] : RECEIVE CAR TYPE FROM MES - " + mesCaption, Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[2] : RECEIVE CAR TYPE FROM PLC - " + plcCaption, Thread.CurrentThread.ManagedThreadId);

                if (plcFrameType != mesFrameType)
                {
                    string log = "";
                    log = "PLC : " + plcCaption + ", MES : " + mesCaption;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("MATCHING ERROR - " + log), Thread.CurrentThread.ManagedThreadId);

                    ShowLabelData("CAR TYPE MATCHING NG", lblCheckResult, Brushes.Red);
                    retval.execResult = ErrorCodeConstant.ERROR_MATCHING_NG;
                    //recvArg.sErrorMessage = "MATCHING NG - " + log;

                    retval.errorInfo.sErrorMessage = "MATCHING NG - " + log;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_TYPE_NG;
                    await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 0);

                    ShowMatchingErrorMessage(plcFrameType, mesFrameType);
                    //SaveErrorDB(retval.errorInfo, sCurrentFunc);

                    retval.recvString = plcCaption;
                    return retval;
                }
                ShowLabelData("CAR TYPE MATCHING OK", lblCheckResult, Brushes.Blue);

                if (cartypeOption == 0)     //HMC U42
                {
                    retval = await plcComm.SendFrameType2PLC(plcFrameType);
                    if (retval.execResult != 0)
                    {
                        //ShowErrorMessage("SEND CAR TYPE TO PLC ERROR", false);
                        ////ITNTErrorLog.Instance.Trace(0, "PLC로 차종 정보 신호 전송 ERROR 발생");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CheckCarType", "SendFrameType2PLC ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendFrameType2PLC) ERROR = " + retval.execResult.ToString();
                        //recvArg.errorInfo.sErrorFunc = sErrorFunc;
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        async Task<ITNTResponseArgs> OpenLaserSource()
        {
            string className = "MainWindow";
            string funcName = "OpenLaserSource";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //ITNTResponseArgs retval2 = new ITNTResponseArgs();
            string statusstring = "";
            SolidColorBrush brush;
            string log = "";
            string value = "";
            string sCurrentFunc = "CONNECTION TO LASER CONTROLLER";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(className, funcName, 0, sCurrentFunc + " START");

                retval = await laserSource.StartClient(3);
                if (retval.execResult == 0)
                {
#if TEST_DEBUG_LASER
                    laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
#else
#if LASER_YLR_PULSEMODE
                    retval = await laserSource.EnableWaveformPulseMode();          // Enable Waveform Pulse Mode only YLR
                    retval = await laserSource.SetPulseRepetitionRate("1.75");
#else
#endif
                    //laserSource.LaserSourceControllerEventFunc += OnLaserSourceDataReceivedEventFunc;
                    laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
                    //laserSource.ConnectionStatusChangedEventFunc += OnLaserConnectionStatusChangedEventReceivedFunc;
                    laserSource.LaserConnectionStatusChangedEventFunc += OnLaserConnectionStatusChangedEventReceivedFunc;
#endif
                    //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO PLC");
                    statusstring = "CONNECTED";
                    brush = Brushes.Green;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LASER CONNECTION SUCCESS", Thread.CurrentThread.ManagedThreadId);
                    ShowLog(className, funcName, 0, sCurrentFunc + " SUCCESS");
                    ShowLabelData(statusstring, lblLaserConnectStatus, backbrush: brush);
                    byLaserConnStatus = 1;
                    retval = await InitializeLaserController();
                    if (retval.execResult != 0)
                    {
                        //brush = Brushes.Red;
                        ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                        return retval;
                    }
                }
                else
                {
                    //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO PLC");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LASER CONNECTION FAIL", Thread.CurrentThread.ManagedThreadId);
                    statusstring = "DISCONNECTED";
                    brush = Brushes.Red;
                    ShowLabelData(statusstring, lblLaserConnectStatus, backbrush: brush);
                    if (byLaserConnStatus != 0)
                    {
                        await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON); // PLC에 레이저 파워 OFF 상태로 셋팅
                    }
                    byLaserConnStatus = 0;

                    retval.errorInfo.sErrorMessage = sCurrentFunc + " FAIL = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);

                    //laserSource.LaserSourceControllerEventFunc += OnLaserSourceDataReceivedEventFunc;
                    laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
                    //laserSource.ConnectionStatusChangedEventFunc += OnLaserConnectionStatusChangedEventReceivedFunc;
                    laserSource.LaserConnectionStatusChangedEventFunc += OnLaserConnectionStatusChangedEventReceivedFunc;

                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                statusstring = "DISCONNECTED";
                brush = Brushes.Red;
                ShowLabelData(statusstring, lblLaserConnectStatus, backbrush: brush);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                if (byLaserConnStatus != 0)
                {
                    byLaserConnStatus = 0;
                    await plcComm.SetLaserPowerError(PLCControlManager.SIGNAL_PC2PLC_ON); // PLC에 레이저 파워 OFF 상태로 셋팅
                }

                //laserSource.LaserSourceControllerEventFunc += OnLaserSourceDataReceivedEventFunc;
                laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
                //laserSource.ConnectionStatusChangedEventFunc += OnLaserConnectionStatusChangedEventReceivedFunc;
                laserSource.LaserConnectionStatusChangedEventFunc += OnLaserConnectionStatusChangedEventReceivedFunc;

            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        async Task<ITNTResponseArgs> InitializeLaserController()
        {
            string className = "MainWindow";
            string funcName = "OpenLaserSource";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "INITIALIZE LASER CONTROLLER";
            ErrorInfo errInfo = new ErrorInfo();
            string log = "";
            string value = "";
            string sProcedure = "0";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(className, funcName, 0, sCurrentFunc + " START");
#if TEST_DEBUG_LASER
#else
                retval = await laserSource.ReadDeviceStatus();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.ReadDeviceStatus();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.ReadDeviceStatus();
                }

                if (retval.execResult != 0)
                {
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (ReadDeviceStatus) ERROR = " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    //ITNTErrorCode(className, funcName, sErrorFunc, errInfo);
                    return retval;
                }

                string[] st = retval.recvString.Split(':');
                if (st.Length < 2)
                {
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (ReadDeviceStatus) ERROR = " + retval.execResult.ToString();
                    //log = sErrorFunc + "(ReadDeviceStatus) ERROR = LASER STATUS STRING LENGTH ERROR";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                    errInfo.sErrorMessage = log;
                    errInfo.sErrorFunc = sCurrentFunc;

                    retval.execResult = -5;
                    retval.errorInfo = (ErrorInfo)errInfo.Clone();
                    //ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);

                    return retval;
                }
                //2-1. Check emission status
                LASERSTATUS Status = (LASERSTATUS)UInt32.Parse(st[1]);
                if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                {
                    //ShowLog("MARKING - STOP EMISSION");
                    //retval = await laserSource.StopEmission();
                    //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //{
                    //    retval = await laserSource.StopEmission();
                    //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //        retval = await laserSource.StopEmission();
                    //}

                    retval = await EmissionOFF();
                    if (retval.execResult != 0)
                    {
                        //log = sErrorFunc + "(EmissionOFF) ERROR = " + retval.execResult.ToString();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        errInfo.sErrorMessage = log;
                        errInfo.sErrorFunc = sCurrentFunc;
                        //ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
                        return retval;
                        //await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        //return retval2.execResult;
                    }
                }

                //Util.GetPrivateProfileValue("LASER", "HWCONTROL", "0", ref value, Constants.PARAMS_INI_FILE);
                //if (value != "0")
                //{
                //    retval = await laserSource.SetHWEmissionControll(1);
                //    if (retval.execResult != 0)
                //    {
                //        //log = sErrorFunc + "(SetHWEmissionControll1) ERROR = " + retval.execResult.ToString();
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //        errInfo.sErrorMessage = log;
                //        errInfo.sErrorFunc = sCurrentFunc;
                //        //ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
                //        return retval;
                //        //log = "LASER INITIALIZE ERROR - HW EMISSION FAIL. (RESULT = " + retval2.execResult.ToString() + ")";
                //        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //        //await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                //        //return retval2.execResult;
                //    }
                //}
                //else
                {
                    retval = await laserSource.SetHWEmissionControll(0);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await laserSource.SetHWEmissionControll(0);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await laserSource.SetHWEmissionControll(0);
                    }
                    if (retval.execResult != 0)
                    {
                        //log = sErrorFunc + "(SetHWEmissionControll0) ERROR = " + retval.execResult.ToString();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        errInfo.sErrorMessage = log;
                        errInfo.sErrorFunc = sCurrentFunc;
                        //ITNTErrorCode(className, funcName, sErrorFunc, errInfo);
                        return retval;
                        //log = "LASER INITIALIZE ERROR - SW EMISSION FAIL. (RESULT = " + retval2.execResult.ToString() + ")";
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        //await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        //return retval2.execResult;
                    }
                }
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                ShowLog(className, funcName, 0, sCurrentFunc + " END");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
            }

            return retval;
        }

        int OpenMESServer()
        {
            string className = "MainWindow";
            string funcName = "OpenMESServer";
            int retval = 0;
            string ServerPort = "";
            string value = "";
            SolidColorBrush brush = new SolidColorBrush();
            string status = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(className, funcName, 0, "CONNECTION TO MES START", "");
                Util.GetPrivateProfileValue("MES", "MESTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "5")
                {
                    //oemDllServer.OpenMESCommunicationAsync();
                    //oemDllServer.StartTimer();
                    //MESServer.statusEvent += OnMESSeverStatusChangedEventHandler;

                    if (mesClient == null)
                        mesClient = new MESClient3();
                    mesClient.receivedEvent += OnReceiveMESClientDataHandler;
                    mesClient.ClientStatusChangedEvent += OnMESStatusChangedHandler;
                    retval = mesClient.StartMES().Result;
                }
                else if (value == "6")
                {
                    if (mesFTB == null)
                        mesFTB = new MESFTB();

                    mesClient.receivedEvent += OnReceiveMESClientDataHandler;
                    mesClient.ClientStatusChangedEvent += OnMESStatusChangedHandler;
                    retval = mesFTB.StartTimer();
                }
                else if (value == "101")      //GMES
                {
                    if (mesGMES == null)
                        mesGMES = new GMESClass();
                    mesGMES.receivedEvent += OnReceiveGMESDataHandler;
                    mesGMES.statusChangedEvent += OnMESStatusChangedHandler;
                    retval = mesGMES.StartGMES().Result;
                }
                else
                {
                    Util.GetPrivateProfileValue("MES", "SERVERPORT", "0", ref ServerPort, Constants.PARAMS_INI_FILE);
                    if (mesServer == null)
                        mesServer = new MESServer();
                    mesServer.receivedEvent += OnReceiveMESServerDataHandler;
                    retval = mesServer.StartServer(IPAddress.Any, Convert.ToInt32(ServerPort)).Result;
                    //MESServer.statusEvent += OnMESSeverStatusChangedEventHandler;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OpenMESServer", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }

            if (retval != 0)
            {
                //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO MES");
                brush = Brushes.Red;
                status = "DISCONNECTED";
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OpenMESServer", "FAIL TO CONNECTION TO MES", Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO  MES");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OpenMESServer", "SUCCESS TO CONNECTION TO MES", Thread.CurrentThread.ManagedThreadId);
                //status = "연결 성공";
                //brush = Brushes.Green;
                status = "WAITING";
                brush = new SolidColorBrush(Color.FromArgb(255, (byte)225, (byte)225, (byte)0));
            }

            ShowLabelData(status, lblMESConnectStatus, backbrush:brush);
            //ShowDeviceStatus(lblMainStatusMESName, brush);

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OpenMESServer", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        //public async Task<ITNTResponseArgs> SendFontDataLaser(string vin, string patternName)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    return retval;
        //}


        public async Task<ITNTResponseArgs> SendFontData(string vin, string patternName)
        {
            string className = "MainWindow";
            string funcName = "SendFontData";

            string value = "";
            double stepLength, Step_W, Step_H;
            short i, j;
            double fontSizeX = 0.0d;
            double fontSizeY = 0.0d;
            ushort usX, usY;
            string log = "";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            Stopwatch sw = new Stopwatch();
            PatternValueEx pattern = new PatternValueEx();
            System.Windows.Point SP = new System.Windows.Point();
            string sCurrentFunc = "SEND FONT DATA TO CONTROLLER";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", "START - " + vin, Thread.CurrentThread.ManagedThreadId);

                //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                //if(value != "0")
                //{
                //    retval = await SendFontDataLaser(vin, patternName);
                //    return retval;
                //}

                //if(bHeadType != 0)
                //{
                //    retval = await SendFontDataLaser(vin, patternName);
                //    return retval;
                //}

                sw.Start();
                Util.GetPrivateProfileValue("MARK", "STEP_LENGTH", "50", ref value, Constants.MARKING_INI_FILE);
                double.TryParse(value, out stepLength);
                ImageProcessManager.GetPatternValue(patternName, bHeadType, ref pattern);

                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4MarkV, pattern.speedValue.targetSpeed4MarkV, pattern.speedValue.accelSpeed4MarkV, pattern.speedValue.decelSpeed4MarkV);
                if (retval.execResult != 0)
                {
                    //log = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("LoadSpeed ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed-L) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                m_currCMD = (byte)'F';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4Home, pattern.speedValue.targetSpeed4Home, pattern.speedValue.accelSpeed4Home, pattern.speedValue.decelSpeed4Home);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("LoadSpeed ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    //log = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed-F) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                //m_currCMD = (byte)'L';
                //retval = await MarkControll.LoadSpeed((byte)'L', pattern.initSpeed4Load, pattern.targetSpeed4Load, pattern.accelSpeed4Load, pattern.decelSpeed4Load);
                //if (retval.execResult != 0)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("LoadSpeed ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                //    return retval;
                //}

                //m_currCMD = (byte)'L';
                //retval = await MarkControll.LoadSpeed((byte)'L', pattern.initSpeed4Load, pattern.targetSpeed4Load, pattern.accelSpeed4Load, pattern.decelSpeed4Load);
                //if (retval.execResult != 0)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("LoadSpeed ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                //    return retval;
                //}







                m_currCMD = (byte)'S';
                retval = await MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime).ConfigureAwait(false);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("SolOnOffTime ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    //log = "COMMUNICATION TO CONTROLLER (SolOnOffTime) ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (SolOnOffTime) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                if (fwVersionFlag >= 1)
                {
                    m_currCMD = (byte)'d';
                    retval = await MarkControll.dwellTimeSet(pattern.speedValue.dwellTime).ConfigureAwait(false);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("dwellTimeSet ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                        //log = "COMMUNICATION TO CONTROLLER (dwellTimeSet) ERROR = " + retval.execResult.ToString();
                        //ShowLog(className, funcName, 2, log);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (dwellTimeSet) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }
                }

                m_currCMD = (byte)'N';
                retval = await MarkControll.StrikeNo(pattern.fontValue.strikeCount).ConfigureAwait(false); // Marking couter
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("StrikeNo ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    //log = "COMMUNICATION TO CONTROLLER (StrikeNo) ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (StrikeNo) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                //switch (pattern.rotateAngle)
                //{
                //    case 0:
                //        SP.X = pattern.startX * stepLength;
                //        break;

                //    case 180:
                //        SP.X = pattern.startX - pattern.pitch * (vin.Length - 1) + pattern.width * stepLength;
                //        break;
                //    default:
                //        SP.X = pattern.startX * stepLength;
                //        break;
                //}
                SP.X = pattern.positionValue.center3DPos.X * stepLength;
                SP.Y = pattern.positionValue.center3DPos.Y * stepLength;

                System.Windows.Point TP = new System.Windows.Point();
                TP.X = (pattern.fontValue.pitch * (vin.Length - 1) + pattern.fontValue.width) * stepLength / 2.0;
                TP.Y = pattern.fontValue.height * stepLength / 2.0;
                TP.X += SP.X; TP.Y += SP.Y;

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("TP.X = " + TP.X.ToString() + "TP.Y = " + TP.Y.ToString()), Thread.CurrentThread.ManagedThreadId);

                //Point CP = new Point();
                //CP.X = pattern.startX * stepLength;
                //CP.Y = pattern.startY * stepLength;

                List<System.Windows.Point> changedPoint = new List<System.Windows.Point>();
                //FontDataClass movedData = new FontDataClass();
                string error = "";

                ImageProcessManager.GetStartPointLinear(vin.Length, TP, SP, pattern.fontValue.pitch * stepLength, pattern.fontValue.rotateAngle, ref changedPoint);
                List<List<FontDataClass>> revData = new List<List<FontDataClass>>();
                //PatternValueEx pattern = new PatternValueEx();
                ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
                VinNoInfo vininfo = new VinNoInfo();
                vininfo.fontName = currMarkInfo.currMarkData.pattern.fontValue.fontName;
                vininfo.vinNo = currMarkInfo.currMarkData.mesData.markvin;
                vininfo.width = currMarkInfo.currMarkData.pattern.fontValue.width;
                vininfo.height = currMarkInfo.currMarkData.pattern.fontValue.height;
                vininfo.pitch = currMarkInfo.currMarkData.pattern.fontValue.pitch;
                vininfo.thickness = currMarkInfo.currMarkData.pattern.fontValue.thickness;

                //byte fontdirection = 0;
                //Util.GetPrivateProfileValue("OPTION", "FONTDIRECTION", "0", ref value, Constants.PARAMS_INI_FILE);
                //byte.TryParse(value, out fontdirection);

                retval = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref currMarkInfo.currMarkData.fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref error);
                if (retval.execResult != 0)
                {
                    //if (retval.sErrorMessage.Length > 0)
                    //    log = retval.sErrorMessage;
                    //else
                    //    log = "GET FONT DATA ERROR : " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    return retval;
                }

                for (i = 0; i < changedPoint.Count; i++)
                {
                    if (vin[i] == ' ')
                        continue;

                    List<FontDataClass> fdatas = new List<FontDataClass>();
                    fdatas = currMarkInfo.currMarkData.fontData[i].ToList();
                    //ImageProcessManager.GetOneCharacterPrintData((char)vin[i], pattern.fontValue.fontName, ref fdatas, out fontSizeX, out fontSizeY, out error);

                    Step_W = pattern.fontValue.width / (fontSizeX - 1) * stepLength;
                    Step_H = pattern.fontValue.height / (fontSizeY - 1) * stepLength;

                    for (j = 0; j < fdatas.Count; j++)
                    {
                        System.Windows.Point RotatePt = new System.Windows.Point();
                        FontDataClass fontValue = new FontDataClass();
                        fontValue.Flag = fdatas[j].Flag;
                        fontValue.vector3d.X = changedPoint[i].X + fdatas[j].vector3d.X * Step_W;
                        fontValue.vector3d.Y = changedPoint[i].Y + fdatas[j].vector3d.Y * Step_H;
                        if (pattern.fontValue.fontName == "5X7")
                            fontValue.vector3d.Y = changedPoint[i].Y + (fdatas[j].vector3d.Y - 3) * Step_H;
                        else if (pattern.fontValue.fontName == "11X16")
                            fontValue.vector3d.Y = changedPoint[i].Y + (fdatas[j].vector3d.Y - 5) * Step_H;

                        RotatePt = ImageProcessManager.Rotate_Point(fontValue.vector3d.X, fontValue.vector3d.Y, changedPoint[i].X, changedPoint[i].Y, pattern.fontValue.rotateAngle);
                        usX = Convert.ToUInt16(RotatePt.X + 0.5);
                        usY = Convert.ToUInt16(RotatePt.Y + 0.5);
                        string ms = i.ToString("X4") + j.ToString("X4") + usX.ToString("X4") + usY.ToString("X4") + fontValue.Flag.ToString("X4");

                        //if (fontValue.Flag != 0.0)
                        //{
                        //    if (fontValue.X > gMaxX)
                        //        gMaxX = (short)(fontValue.X + 0.5);

                        //    if (fontValue.X < gMinX)
                        //        gMinX = (short)(fontValue.X + 0.5);

                        //    if (fontValue.Y > gMaxY)
                        //        gMaxY = (short)(fontValue.Y + 0.5);

                        //    if (fontValue.Y < gMinY)
                        //        gMinY = (short)(fontValue.Y + 0.5);

                        //    AREA_Test = true;
                        //}

                        //idx++;
                        //string ms = i.ToString("X4") + j.ToString("X4") +((short)(fontValue.X + 0.5d)).ToString("X4") + ((short)(fontValue.Y + 0.5d)).ToString("X4") + fontValue.Flag.ToString("X4");
                        //savefile2(ms);

                        m_currCMD = (byte)'D';
                        retval = await MarkControll.LoadFontData(i, j, (short)usX, (short)usY, (short)fontValue.Flag).ConfigureAwait(false);
                        if (retval.execResult != 0)
                        {
                            //log = "COMMUNICATION TO CONTROLLER (LoadFontData) ERROR = " + retval.execResult.ToString();
                            //ShowLog(className, funcName, 2, log);

                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadFontData) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            return retval;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
            }
            sw.Stop();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SendFontData2", string.Format("END - {0}", sw.ElapsedMilliseconds), Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<distanceSensorData> ReadDisplacementSensor(byte count, int loglevel = 0)
        {
            string className = "MainWindow";
            string funcName = "ReadDisplacementSensor";

            ITNTResponseArgs recvArg = new ITNTResponseArgs();

            string sdistance1 = "";
            string sdistance2 = "";

            double distance1 = 0.0d;
            double distance2 = 0.0d;
            double distanceN = 0.0d;
            double distanceZ = 0.0d;
            double distanceH = 0.0d;
            double shiftaverage = 0.0d;
            double offsetaverage = 0.0d;
            int exeSuccessCount = 0;
            int errcode = 0;

            string sensor = "";
            distanceSensorData retval = new distanceSensorData();
            int num = 0;
            string sCurrentFunc = "READ DISPLACEMENT SENSOR";

            try
            {
                if (count <= 0)
                {
                    count = 1;
                    //ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ SENSOR ERROR - COUNT = 0", Thread.CurrentThread.ManagedThreadId);
                    //retval.execResult = ErrorCodeConstant.ERROR_PARAM_INVALID;
                    //retval.errorInfo.sErrorMessage = "READ COUNT IS 0";
                    //retval.errorInfo.sErrorFunc = sCurrentFunc;
                    //retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_PARAM + Constants.ERROR_INVALID;

                    //retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    //retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    //retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    //retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    //retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    //return retval;
                }

                for (int i = 0; i < count; i++)
                {
                    num++;
                    recvArg = await distanceSensor.ReadSensor(3, loglevel);//, _cts.Token);
                    if ((recvArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        recvArg = await distanceSensor.ReadSensor(3, loglevel);//, _cts.Token);
                        if ((recvArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            recvArg = await distanceSensor.ReadSensor(3, loglevel);//, _cts.Token);
                    }

                    if (recvArg.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISTANCE SENSOR ERROR " + recvArg.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        errcode = -1;
                        distanceZ = 0;
                        distanceH = 0;

                        retval.execResult = recvArg.execResult;

                        retval.errorInfo = (ErrorInfo)recvArg.errorInfo.Clone();
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO SENSOR (ReadSensor) ERROR = " + recvArg.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        return retval;
                    }
                    else
                    {
                        sensor = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                        //sensor = sensor.ToString();
                        string[] vals = sensor.Split(',');
                        if (vals.Length <= 1)
                        {
                            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISTANCE DATA ERROR", Thread.CurrentThread.ManagedThreadId);
                            errcode = -2;
                            distanceZ = 0;
                            distanceH = 0;

                            //retval.errorInfo.sErrorFunc = sCurrentFunc;
                            //retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_CAUSE1_PARAM + Constants.ERROR_CAUSE2_INVALID;
                            //ShowLog(className, funcName, 2, "READ SENSOR ERROR", "READ LASER DISTANCE DATA ERROR");

                            retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                            retval.errorInfo.sErrorMessage = "READ DISTANCE SENSOR DATA LENGTH ERROR : " + vals.Length.ToString();// + recvArg.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;
                            retval.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                            retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                            retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                            retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                            retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                            retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                            return retval;
                        }
                        else
                        {
                            if (vals.Length >= 2)
                            {
                                exeSuccessCount++;

                                sdistance1 = vals[1];
                                if (sdistance1.Length > 10)
                                    sdistance1 = sdistance1.Substring(0, 10);
                                double.TryParse(sdistance1, out distance1);
                                distance1 = distance1 / 100.0d;                 // Change to mm
                                if (distance1 != 1000000.0)
                                {
                                    distanceN = distance1;
                                    distanceZ = Math.Cos(Math.PI / 180.0 * AngleOfSensor) * distanceN;
                                    distanceH = Math.Sin(Math.PI / 180.0 * AngleOfSensor) * distanceN;
                                }

                                if (vals.Length >= 3)
                                {
                                    sdistance2 = vals[2];
                                    if (sdistance2.Length > 10)
                                        sdistance2 = sdistance2.Substring(0, 10);
                                    double.TryParse(sdistance2, out distance2);
                                    distance2 = distance2 / 100.0d;             // Change to mm
                                    if (distance2 != 1000000.0)
                                    {
                                        distanceN = distance2;
                                        distanceZ = Math.Cos(Math.PI / 180.0 * AngleOfSensor) * distanceN;
                                        distanceH = Math.Sin(Math.PI / 180.0 * AngleOfSensor) * distanceN;
                                    }
                                }
                            }
                            else
                            {
                                errcode = -3;
                                distanceZ = 0;
                                distanceH = 0;
                                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISTANCE DATA ERROR 2", Thread.CurrentThread.ManagedThreadId);

                                retval.errorInfo.sErrorMessage = "READ DISTANCE SENSOR DATA LENGTH ERROR 2 : " + vals.Length.ToString();// + recvArg.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;

                                retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                                //retval.errorInfo.sErrorMessage = "READ DISTANCE SENSOR DATA LENGTH ERROR : " + vals.Length.ToString();// + recvArg.execResult.ToString();
                                retval.errorInfo.sErrorFunc = sCurrentFunc;
                                retval.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_SERVO;
                                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_SERVO;
                                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                                return retval;
                                //ShowLog(className, funcName, 2, "READ SENSOR ERROR", "READ LASER DISTANCE DATA ERROR 2");
                            }
                        }
                    }
                    shiftaverage += (double)distanceH;
                    shiftaverage = shiftaverage / num;
                    offsetaverage += (double)distanceZ;
                    offsetaverage = offsetaverage / num;
                }

                if (exeSuccessCount != 0)
                {
                    retval.execResult = 0;
                    retval.sensoroffset = (double)offsetaverage;
                    retval.sensorshift = (double)shiftaverage;
                    retval.rawdistance = distanceN;
                }
                else
                {
                    //retval.execResult = errcode;

                    retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                    retval.errorInfo.sErrorMessage = "READ DISTANCE SENSOR DATA LENGTH ERROR";// + recvArg.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                }
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                //retval.errorInfo.sErrorFunc = sCurrentFunc;


                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }
            return retval;
        }


        private void SaveLPMData(double lpmdata)
        {
            string className = "MainWindow";
            string funcName = "SaveLPMData";
            string depthFileName = "";
            string depthAllfn = "";
            string depthFilePath = "";
            DateTime dt = DateTime.Now;

            try
            {
                depthFileName = ".\\LPMDATA\\" + dt.ToString("yyyy") + dt.ToString("MM") + "\\" + "DEPTH_TODAY." + dt.ToString("dd");
                depthFilePath = ".\\LPMDATA\\";

                depthAllfn = ".\\LPMDATA\\DEPTH_HISTORY.ALL";

                DirectoryInfo di = new DirectoryInfo(depthFilePath);
                if (di.Exists == false)
                    Directory.CreateDirectory(depthFilePath);

                FileInfo fd = new FileInfo(depthFileName);
                if (fd.Exists == false)
                {
                    using (StreamWriter swd = fd.CreateText())
                    {
                        swd.WriteLine(lpmdata.ToString("F3"));
                    }
                }
                else
                {
                    using (StreamWriter swd = fd.AppendText())
                    {
                        swd.WriteLine(lpmdata.ToString("F3"));
                    }
                }

                FileInfo fa = new FileInfo(depthAllfn);
                if (fa.Exists == false)
                {

                }

                //    depthAllfn = ".\\TEMP\\" + irData.inspectResult.vininfo.carType + "_DEPTH_HISTORY.ALL";

                ////FileInfo fd = new FileInfo(depthFileName);
                //if (!fd.Exists)
                //{
                //    // Averaging Today --> ????_DEPTH_HISTORY.ALL
                //    DirectoryInfo folder = new DirectoryInfo(".\\TEMP");
                //    if (folder.Exists)
                //    {
                //        FileInfo[] files = folder.GetFiles(irData.inspectResult.vininfo.carType + "_DEPTH_TODAY.*");
                //        foreach (FileInfo file in files)
                //        {
                //            using (StreamReader sr = new StreamReader(file.FullName))
                //            {
                //                var ai = 0;
                //                double sumOfDepthA = 0.0;
                //                while (!sr.EndOfStream)
                //                {
                //                    var oneLine = sr.ReadLine();
                //                    var oneLines = oneLine.Trim().Split('|');
                //                    var depthLine = double.Parse(oneLines[0]);
                //                    if (double.IsNaN(depthLine))
                //                        continue;
                //                    sumOfDepthA += depthLine; ai++;
                //                }

                //                if (ai >= 1)
                //                {
                //                    sumOfDepthA /= (double)ai;

                //                    FileInfo faw = new FileInfo(depthAllfn);
                //                    if (!faw.Exists)
                //                    {
                //                        using (StreamWriter swa = faw.CreateText())
                //                        {
                //                            swa.WriteLine(sumOfDepthA.ToString("F3") + "|" + ai.ToString() + "|" + DateTime.Now.ToString());
                //                        }
                //                    }
                //                    else
                //                    {
                //                        using (StreamWriter swa = faw.AppendText())
                //                        {
                //                            swa.WriteLine(sumOfDepthA.ToString("F3") + "|" + ai.ToString() + "|" + DateTime.Now.ToString());
                //                        }
                //                    }
                //                }
                //            }
                //            file.Delete();
                //        }
                //    }

                //    using (StreamWriter swd = fd.CreateText())
                //    {
                //        swd.WriteLine(irData.inspectResult.maxdepth.ToString("F2") + "|" + irData.inspectResult.avgwidth.ToString("F2") + "|" + DateTime.Now.ToString());
                //    }
                //}
                //else
                //{
                //    using (StreamWriter swd = fd.AppendText())
                //    {
                //        swd.WriteLine(irData.inspectResult.maxdepth.ToString("F2") + "|" + irData.inspectResult.avgwidth.ToString("F2") + "|" + DateTime.Now.ToString());
                //    }
                //}

                ////
                ////  Dot Image Display & Draw Depth Graph
                //cvsShowImage.Children.Clear();
                //ViewerDepthImage.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;

                //var imgFileName = ".\\TEMP\\" + "CS_DEPTH_VIEW.tiff";

                //FileInfo fi = new FileInfo(imgFileName);
                //if (fi.Exists)
                //{
                //    // Open a Stream and decode a TIFF image.
                //    Stream imageStreamSource = new FileStream(imgFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                //    var decoder = new TiffBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                //    BitmapSource bitmapSource = decoder.Frames[0];
                //    cvsShowImage.Height = bitmapSource.PixelHeight;
                //    cvsShowImage.Width = bitmapSource.PixelWidth;

                //    depthImage.ImageSource = decoder.Frames[0];

                //    for (int i1 = 0; i1 < mi2.measureInfo.Length; i1++)
                //    {
                //        var count11 = mi2.measureInfo[i1].oneLineFixedValue.Count;
                //        if (count11 == 0) continue;

                //        Line[] line1 = new Line[count11];

                //        // Selected Dot Line
                //        for (int j = 0; j < count11; j++)
                //        {
                //            line1[j] = new Line();
                //            line1[j].X1 = mi2.measureInfo[i1].oneLineFixedValue[j].point.col1;
                //            line1[j].X2 = mi2.measureInfo[i1].oneLineFixedValue[j].point.col2;
                //            line1[j].Y1 = mi2.measureInfo[i1].oneLineFixedValue[j].point.row1;
                //            line1[j].Y2 = mi2.measureInfo[i1].oneLineFixedValue[j].point.row2;
                //            line1[j].Stroke = Brushes.Red;
                //            line1[j].StrokeThickness = 1.0;
                //            cvsShowImage.Children.Add(line1[j]);

                //            TextBlock textBlock = new TextBlock();
                //            textBlock.Text = " " + mi2.measureInfo[i1].depth.ToString("F2") + " / " + mi2.measureInfo[i1].width.ToString("F2") + " ";
                //            textBlock.Foreground = new SolidColorBrush(Colors.LightGreen);
                //            textBlock.Background = new SolidColorBrush(Colors.Black);
                //            Canvas.SetLeft(textBlock, irData.inspectResult.recogChar[i1].PoriginPos.X + 55.0);
                //            Canvas.SetTop(textBlock, irData.inspectResult.recogChar[i1].PoriginPos.Y);
                //            cvsShowImage.Children.Add(textBlock);
                //        }

                //        // Depth Line graph
                //        for (int j = 0; j < count11; j++)
                //        {
                //            var valcount = mi2.measureInfo[i1].oneLineFixedValue[j].grayLevel.Count;
                //            if (valcount < 20) continue;
                //            for (var V = 0; V < valcount - 1; V++)
                //            {
                //                line1[j] = new Line();
                //                line1[j].X1 = (mi2.measureInfo[i1].oneLineFixedValue[j].grayLevel[V].index - mi2.measureInfo[i1].oneLineFixedValue[j].grayLevel[0].index) + irData.inspectResult.recogChar[i1].PoriginPos.X + 60.0;
                //                line1[j].X2 = (mi2.measureInfo[i1].oneLineFixedValue[j].grayLevel[V + 1].index - mi2.measureInfo[i1].oneLineFixedValue[j].grayLevel[0].index) + irData.inspectResult.recogChar[i1].PoriginPos.X + 60.0;
                //                line1[j].Y1 = mi2.measureInfo[i1].oneLineFixedValue[j].grayLevel[V].grayvalue - mi2.measureInfo[i1].oneLineValue[j].grayLevel[0].grayvalue;
                //                line1[j].Y2 = mi2.measureInfo[i1].oneLineFixedValue[j].grayLevel[V + 1].grayvalue - mi2.measureInfo[i1].oneLineValue[j].grayLevel[0].grayvalue;
                //                line1[j].Y1 /= 10.0; line1[j].Y1 = 110.0 - line1[j].Y1;
                //                line1[j].Y2 /= 10.0; line1[j].Y2 = 110.0 - line1[j].Y2;

                //                line1[j].Stroke = Brushes.Red;
                //                line1[j].StrokeThickness = 0.5;
                //                cvsShowImage.Children.Add(line1[j]);
                //            }
                //        }
                //    }
                //    imageStreamSource.Close();
                //}

                //// fa + fd read & display $$$$$

                //const double Yorigin = 300.0;

                //const double line1Xoffset = 60.0;
                //const double line1Yoffset = 60.0;

                //int di = 0;
                //double sumOfDepth = 0.0;

                //FileInfo fa = new FileInfo(depthAllfn);
                //if (fa.Exists)
                //{
                //    using (StreamReader sra = new StreamReader(depthAllfn))
                //    {

                //        int i = 0;

                //        double depthLine = 0.0;

                //        string[] oneLines = null;

                //        List<PointF> lines = new List<PointF>();
                //        while (!sra.EndOfStream)
                //        {
                //            var oneLine = sra.ReadLine();
                //            oneLines = oneLine.Trim().Split('|');
                //            depthLine = double.Parse(oneLines[0]);
                //            if (double.IsNaN(depthLine))
                //                continue;
                //            sumOfDepth += depthLine;

                //            var point1 = new PointF();

                //            point1.X = (float)line1Xoffset + di++;
                //            point1.Y = (float)(line1Yoffset + depthLine * 200.0 + 0.5);
                //            point1.Y = (float)Yorigin - point1.Y;

                //            lines.Add(point1);
                //        }

                //        // History's Depth graph
                //        if (lines.Count > 1)
                //        {
                //            for (i = 0; i < lines.Count - 1; i++)
                //            {
                //                var line = new Line();
                //                line.X1 = lines[i].X; line.X2 = lines[i + 1].X;
                //                line.Y1 = lines[i].Y; line.Y2 = lines[i + 1].Y;
                //                line.Stroke = Brushes.Blue;
                //                line.StrokeThickness = 1.0;
                //                cvsShowImage.Children.Add(line);
                //            }
                //        }
                //    }
                //}


                //using (StreamReader sr = new StreamReader(depthFileName))
                //{
                //    int i = 0;

                //    double depthLine = 0.0;

                //    string[] oneLines = null;

                //    List<PointF> lines = new List<PointF>();
                //    while (!sr.EndOfStream)
                //    {
                //        var oneLine = sr.ReadLine();
                //        oneLines = oneLine.Trim().Split('|');
                //        depthLine = double.Parse(oneLines[0]);
                //        if (double.IsNaN(depthLine))
                //            continue;
                //        sumOfDepth += depthLine;

                //        var point1 = new PointF();

                //        point1.X = (float)line1Xoffset + di++;
                //        point1.Y = (float)(line1Yoffset + depthLine * 200.0 + 0.5);
                //        point1.Y = (float)Yorigin - point1.Y;

                //        lines.Add(point1);
                //    }

                //    if (di >= 1) sumOfDepth /= (double)di;

                //    // Today's Depth graph
                //    if (lines.Count > 1)
                //    {
                //        for (i = 0; i < lines.Count - 1; i++)
                //        {
                //            var line = new Line();
                //            line.X1 = lines[i].X; line.X2 = lines[i + 1].X;
                //            line.Y1 = lines[i].Y; line.Y2 = lines[i + 1].Y;
                //            line.Stroke = Brushes.Green;
                //            line.StrokeThickness = 0.5;
                //            cvsShowImage.Children.Add(line);
                //        }
                //    }
                //    // X-axis line
                //    var axis = new Line();
                //    axis.X1 = line1Xoffset - 1; axis.X2 = line1Xoffset + di;
                //    axis.Y1 = Yorigin + 1 - line1Yoffset; axis.Y2 = Yorigin + 1 - line1Yoffset;
                //    axis.Stroke = Brushes.Black;
                //    axis.StrokeThickness = 1.0;
                //    cvsShowImage.Children.Add(axis);

                //    // Y-axis line
                //    axis = new Line();
                //    axis.X1 = axis.X2 = line1Xoffset - 1;
                //    axis.Y1 = Yorigin + 1 - line1Yoffset; axis.Y2 = axis.Y1 - 100.0;
                //    axis.Stroke = Brushes.Black;
                //    axis.StrokeThickness = 1.0;
                //    cvsShowImage.Children.Add(axis);

                //    TextBlock textBlock = new TextBlock();
                //    textBlock.Text = "0.5mm";
                //    textBlock.Foreground = new SolidColorBrush(Colors.Green);
                //    //textBlock.Background = new SolidColorBrush(Colors.Black);
                //    Canvas.SetLeft(textBlock, axis.X2 - 15.0);
                //    Canvas.SetTop(textBlock, axis.Y2 - 17.0);
                //    cvsShowImage.Children.Add(textBlock);

                //    textBlock = new TextBlock();
                //    textBlock.Text = depthLine.ToString("F2") + " Avg. " + sumOfDepth.ToString("F3");
                //    textBlock.Foreground = new SolidColorBrush(Colors.LightGreen);
                //    textBlock.Background = new SolidColorBrush(Colors.Black);
                //    Canvas.SetLeft(textBlock, lines[i].X + 2.0);
                //    Canvas.SetTop(textBlock, lines[i].Y);
                //    cvsShowImage.Children.Add(textBlock);

                //    // Display filename
                //    textBlock = new TextBlock();
                //    var afName = depthAllfn.Trim().Split('\\');
                //    textBlock.Text = afName[2];
                //    textBlock.Foreground = new SolidColorBrush(Colors.Blue);
                //    textBlock.Background = new SolidColorBrush(Colors.LightGray);
                //    Canvas.SetLeft(textBlock, line1Xoffset);
                //    Canvas.SetTop(textBlock, line1Yoffset);
                //    cvsShowImage.Children.Add(textBlock);

                //    textBlock = new TextBlock();
                //    var dfName = depthFileName.Trim().Split('\\');
                //    textBlock.Text = dfName[2];
                //    textBlock.Foreground = new SolidColorBrush(Colors.DarkGreen);
                //    textBlock.Background = new SolidColorBrush(Colors.LightGray);
                //    Canvas.SetLeft(textBlock, line1Xoffset);
                //    Canvas.SetTop(textBlock, line1Yoffset + 20.0);
                //    cvsShowImage.Children.Add(textBlock);
                //}
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
            }
        }


        public void PlateColoring(byte[,] heightColor)
        {
            int BitMap_WIDTH = heightColor.GetUpperBound(1);
            int BitMap_HEIGHT = heightColor.GetUpperBound(0);

            byte[,,] pixelArrayB = new byte[BitMap_HEIGHT, BitMap_WIDTH, 4];

            for (int y = 0; y < BitMap_HEIGHT; y++)
            {
                for (int x = 0; x < BitMap_WIDTH; x++)
                {
                    var rgb = GetColorMap(heightColor[y, x], false);
                    pixelArrayB[y, x, 0] = rgb.Item3;
                    pixelArrayB[y, x, 1] = rgb.Item2;
                    pixelArrayB[y, x, 2] = rgb.Item1;
                    pixelArrayB[y, x, 3] = 255;
                }
            }

            byte[] byteArrayB = new byte[BitMap_HEIGHT * BitMap_WIDTH * 4];
            int index = 0;
            for (int row = 0; row < BitMap_HEIGHT; row++)
            {
                for (int col = 0; col < BitMap_WIDTH; col++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        byteArrayB[index++] = pixelArrayB[row, col, i];
                    }
                }
            }

            WriteableBitmap writeableBitmap = new WriteableBitmap
            (
                BitMap_WIDTH,
                BitMap_HEIGHT,
                20,//96,
                20,//96,
                PixelFormats.Bgra32,
                null
            );

            Int32Rect rectangleB = new Int32Rect(0, 0, BitMap_WIDTH, BitMap_HEIGHT);
            int strideB = 4 * BitMap_WIDTH;
            writeableBitmap.WritePixels(rectangleB, byteArrayB, strideB, 0);

            System.Windows.Controls.Image imageB = new System.Windows.Controls.Image();
            imageB.Stretch = Stretch.None;
            imageB.Margin = new Thickness(0);

            PlateColor.Children.Add(imageB);
            imageB.Source = writeableBitmap;
        }


        public (byte r, byte g, byte b) GetColorMap(byte i, bool grayColor)
        {
            double red = 0, green = 0, blue = 0;    // ( 0.0 ~ 1.0 )
            try
            {
                red = green = blue = 0.0;
                if (i < 32)
                {
                    blue = 8.0 * (double)i / 255.0;
                }
                else if (i < 96)
                {
                    green = 1.0 + 4.0 * ((double)i - 95.0) / 255.0;
                    blue = 1.0;
                }
                else if (i < 160)
                {
                    red = 1.0 + 4.0 * ((double)i - 159.0) / 255.0;
                    blue = 4.0 * (159.0 - (double)i) / 255.0;
                    green = 1.0;
                }
                else if (i < 224)
                {
                    green = 4.0 * (223.0 - (double)i) / 255.0;
                    red = 1.0;
                }
                else
                {
                    red = 8.0 * (255.0 - (double)i) / 255.0;
                }

                // New Color [0,255]:
                red = red * 255.0 + 0.5;
                green = green * 255.0 + 0.5;
                blue = blue * 255.0 + 0.5;

                if (grayColor == true)  //gray
                {
                    red = green = blue = i;
                }

                return ((byte)red, (byte)green, (byte)blue);
            }
            catch (Exception ex)
            {
                return ((byte)red, (byte)green, (byte)blue);
            }
        }

        const int RANGE_COUNT = 7;


        public async Task<distanceSensorData> GetMeasureLength(Vector3D vp3, int pos, byte count)
        {
            string className = "MainWindow";
            string funcName = "GetMeasureLength";

            distanceSensorData sensorData = new distanceSensorData();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "MEASURE LENGTH";

            try
            {
                m_currCMD = (byte)'M';
                retval = await MarkControll.GoPoint((int)(vp3.X + 0.5), (int)(vp3.Y + 0.5), (int)(vp3.Z + 0.5), pos);
                if (retval.execResult != 0)
                {
                    sensorData.execResult = retval.execResult;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GoPoint ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                    sensorData.errorInfo = (ErrorInfo)retval.errorInfo.Clone();
                    sensorData.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (GoPoint) ERROR = " + retval.execResult.ToString();
                    sensorData.errorInfo.sErrorFunc = sCurrentFunc;
                    return sensorData;
                }

                sensorData = await ReadDisplacementSensor(count, 0);
                if (sensorData.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadDisplacementSensor : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    //log = "READ DISTANCE SENSOR ERROR : " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);

                    //sensorData.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (GoPoint) ERROR = " + retval.execResult.ToString();
                    //sensorData.errorInfo.sErrorFunc = sErrorFunc;

                    return sensorData;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //log = "READ DISTANCE SENSOR " + string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message);
                //ShowLog(className, funcName, 2, log);
                sensorData.execResult = ex.HResult;
                sensorData.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                sensorData.errorInfo.sErrorFunc = sCurrentFunc;
            }
            return sensorData;
        }


        public async Task<ITNTResponseArgs> PrepareLaserSource(string orderstring)
        {
            string className = "MainWindow";
            string funcName = "MarkingProcess";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string log = "";
            LASERSTATUS Status = 0;
            Stopwatch sw = new Stopwatch();
            bool bEmissionOK = false;
            string sCurrentFunc = "PREPARE LASER SOURCE";

            try
            {
                //1. Aiming Beam OFF
                retval = await laserSource.AimingBeamOFF();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamOFF();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.AimingBeamOFF();
                }
                //AimingLamp.Fill = Brushes.Black;
                if (retval.execResult != 0)
                {
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamOFF) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                ShowRectangle(Brushes.Black, EmissionLamp);
                StopLabelBlinking();

                //2.read laser status
                //ShowLog("MARKING - READ LASER STATUS");


                //retval = await laserSource.ReadDeviceStatus();
                //if (retval.execResult == 0)
                //{
                //    string[] st = retval.recvString.Split(':');
                //    if (st.Length >= 2)
                //    {
                //        //2-1. Check emission satus
                //        Status = (LASERSTATUS)UInt32.Parse(st[1]);
                //        if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                //        {
                //            //ShowLog("MARKING - STOP EMISSION");
                //            retval = await laserSource.StopEmission();
                //            if (retval.execResult != 0)
                //            {
                //                log = "MARKING ERROR - STOP EMISSION. (" + retval.execResult.ToString() + ")";
                //                //ShowLog(log);
                //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //                return retval;
                //            }
                //            //EmissionLamp.Fill = Brushes.Black;
                //            ShowRectangle(Brushes.Black, EmissionLamp);
                //        }
                //    }
                //    else
                //    {
                //        log = "MARKING ERROR - READ LASER STATUS. (STATUS STRING)";
                //        //ShowLog(log);
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //        return retval;
                //    }
                //}
                //else
                //{
                //    log = "MARKING ERROR - READ LASER STATUS. (" + retval.execResult.ToString() + ")";
                //    //ShowLog(log);
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //    return retval;
                //}

                //3. load waveform profile number
                //ShowLog("MARKING - SELECT PROFILE");
#if LASER_YLR_PULSEMODE
                retval = await laserSource.SelectProfile(currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString());
                if (retval.execResult == 0)
                {
                    string[] prsel = retval.recvString.Split('[', ']');
                    if (prsel.Length >= 2)
                    {
                        if (prsel[0] == "PRSEL: ")
                        {
                            string[] sel = prsel[1].Split(':');
                            if (currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString() != sel[0])
                            {
                                log = "MARKING ERROR - SELECT PROFILE. (PROFILE SETTING ERROR)";
                                //ShowLog(log);
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                                return retval;
                            }
                        }
                        else
                        {
                            log = "MARKING ERROR - SELECT PROFILE. (PROFILE SETTING RESPONSE ERROR)";
                            //ShowLog(log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            return retval;
                        }
                    }
                    else
                    {
                        log = "MARKING ERROR - SELECT PROFILE. (PROFILE STRING)";
                        //ShowLog(log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        return retval;
                    }
                }
                else
                {
                    log = "MARKING ERROR - SELECT PROFILE. (" + retval.execResult.ToString() + ")";
                    //ShowLog(log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                //4. get waveform mode
                //ShowLog("MARKING - CONFIG WAVEFORM MODE");
                retval = await laserSource.ConfigWaveformMode(0);
                if (retval.execResult == 0)
                {
                    string[] pcfg = retval.recvString.Split('[', ']');
                    if (pcfg.Length < 2)
                    {
                        log = "MARKING ERROR - CONFIG WAVEFORM MODE. (PROFILE STRING)";
                        //ShowLog(log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        return retval;
                    }
                }
                else
                {
                    log = "MARKING ERROR - CONFIG WAVEFORM MODE. (" + retval.execResult.ToString() + ")";
                    //ShowLog(log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

#else
                retval = await laserSource.SetPulseRepetitionRate("1.00");
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.SetPulseRepetitionRate("1.00");
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.SetPulseRepetitionRate("1.00");
                }
                if (retval.execResult != 0)
                {
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetPulseRepetitionRate) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                retval = await laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.markPower);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.markPower);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.markPower);
                }
                if (retval.execResult != 0)
                {
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetDiodeCurrent) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                //ShowLabelData(currMarkInfo.currMarkData.pattern.laserValue.markPower, lblPowerValue);

                retval = await laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.markWidth);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.markWidth);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.markWidth);
                }
                if (retval.execResult != 0)
                {
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetPulseWidth) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                //ShowLabelData(currMarkInfo.currMarkData.pattern.laserValue.markPower, lblPowerValue);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }
        }


        private void savefile2(string data)
        {
            using (StreamWriter w = File.AppendText("..\\myFile2.txt"))
            {
                w.WriteLine(data);
            }
            //StreamWriter writer_;
            //String strFilePath = "..\\test.txt";
            //writer_ = File.CreateText(strFilePath);
            //writer_.WriteLine(data);
            //writer_.Close();
        }

        void CloseMESServer()
        {
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseMESServer", "START", Thread.CurrentThread.ManagedThreadId);
            string value = "";
            try
            {
                Util.GetPrivateProfileValue("MES", "MESTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "5")
                {
                    if (mesClient != null)
                        mesClient.CloseMES(1);
                    //oemDllServer.CloseMESCommunication();
                }
                else if (value == "101")      //GMES
                {
                    if (mesGMES != null)
                        mesGMES.CloseMES();
                }
                else
                {
                    if (mesServer != null)
                        mesServer.ServerClose();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseMESServer", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseMESServer", "END", Thread.CurrentThread.ManagedThreadId);
        }

        private async Task<ITNTResponseArgs> OpenPLCAsync()
        {
            string className = "MainWindow";
            string funcName = "OpenPLCAsync";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            SolidColorBrush brush = new SolidColorBrush();
            string status = "";
            string sCurrentFunc = "CONNECTION TO PLC";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(className, funcName, 0, sCurrentFunc + " START", "");
                if (plcComm == null)
                    plcComm = new PLCControlManager(OnPLCDataReceivedCallBakHanlder, OnPLCStatusChangedEventHandler);
                //plcComm = new PLCControlManager(OnPLCDataReceivedCallBakHanlder, OnPLCStatusChangedHandler);

                retval = await plcComm.OpenPLCAsync();
                if (retval.execResult != 0)
                {
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("[ERROR_PLC_PORT_Open] : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                    //ITNTErrorLog.Instance.Trace(0, string.Format("[ERROR_PLC_PORT_Open] : {0}", retval));
                    //ShowErrorMessage("PLC Open Error. Check Please", false);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PCL OPEN FAIL - " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    brush = Brushes.Red;
                    //ShowLog(className, funcName, 2, "PLC CONNECTION FAIL", retval.execResult.ToString());
                    status = "DISCONNECTED";
                    ShowLabelData(status, lblPLCConnectStatus, backbrush: brush);
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = sCurrentFunc + " FAIL = " + retval.execResult.ToString();
                    ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
                    return retval;
                }
                else
                {
                    //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO PLC");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PCL OPEN SUCCESS", Thread.CurrentThread.ManagedThreadId);
                    brush = Brushes.Green;
                    status = "CONNECTED";
                    ShowLabelData(status, lblPLCConnectStatus, backbrush: brush);
                    ShowLog(className, funcName, 0, sCurrentFunc + " SUCCESS", "");
                }
                //if(retval == 0)
                //    plcComm.dataArrivedEvent += OnPLCDataArrivedEventHandler;
                //if (retArg.execResult != 0)
                //    ShowLog(className, funcName, 2, "PLC 연결 실패", retArg.execResult.ToString());
                //else
                //    ShowLog(className, funcName, 0, "PLC 연결 성공", "");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                brush = Brushes.Red;
                status = "DISCONNECTED";
                ShowLabelData(status, lblPLCConnectStatus, backbrush: brush);

                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);
            }

            //if (retval.execResult != 0)
            //{
            //    //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO PLC");
            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PCL OPEN FAIL", Thread.CurrentThread.ManagedThreadId);
            //    brush = Brushes.Red;
            //    ShowLog(className, funcName, 2, "PLC CONNECTION FAIL", retval.execResult.ToString());
            //    status = "DISCONNECTED";
            //}
            //else
            //{
            //    //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO PLC");
            //    brush = Brushes.Green;
            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PCL OPEN SUCCESS", Thread.CurrentThread.ManagedThreadId);
            //    status = "CONNECTED";
            //    ShowLog(className, funcName, 0, "PLC CONNECTION SUCCESS", "");
            //}
            //ShowDeviceStatus(lblMainStatusPLCName, brush);

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OpenPLC", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private ITNTResponseArgs OpenPLC()
        {
            string className = "MainWindow";
            string funcName = "OpenPLC";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            SolidColorBrush brush = new SolidColorBrush();
            string status = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (plcComm == null)
                    plcComm = new PLCControlManager(OnPLCDataReceivedCallBakHanlder, OnPLCStatusChangedEventHandler);
                //plcComm = new PLCControlManager(OnPLCDataReceivedCallBakHanlder, OnPLCStatusChangedHandler);
                retval = plcComm.OpenPLC();
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("[ERROR_PLC_PORT_Open] : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                    //ITNTErrorLog.Instance.Trace(0, string.Format("[ERROR_PLC_PORT_Open] : {0}", retval));
                    ShowErrorMessage("PLC Serial Port Open Error. Check Please", false);
                }
                //if(retval == 0)
                //    plcComm.dataArrivedEvent += OnPLCDataArrivedEventHandler;

                //retval = plcComm.ReadLinkStatusAsync().Result;
                //if (retval.recvString.Substring(4, 4) == "0001")
                //    retval = plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_OFF).Result;
                //plcComm..PLCControllerStatusEventFunc += OnPLCStatusChangedEventReceivedFunc;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }

            if (retval.execResult != 0)
            {
                //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO PLC");
                status = "DISCONNECTED";
                brush = Brushes.Red;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PCL OPEN FAIL", Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO PLC");
                status = "CONNECTED";
                brush = Brushes.Green;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PCL OPEN SUCCESS", Thread.CurrentThread.ManagedThreadId);
            }
            //ShowDeviceStatus(lblMainStatusPLCName, brush);
            ShowLabelData(status, lblPLCConnectStatus, backbrush: brush);

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        void ClosePLC()
        {
            try
            {
                if (plcComm != null)
                    plcComm.ClosePLC();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ClosePLC", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<int> OpenMarkController()
        {
            string className = "MainWindow";
            string funcName = "OpenMarkController";
            int retval = 0;
            SolidColorBrush brush = new SolidColorBrush();
            string status = "";
            //string ver = "";
            //string headType = "";
            string sCurrentFunc = "CONNECTION TO CONTROLLER";
            ErrorInfo errInfo = new ErrorInfo();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, sCurrentFunc + " START", retval.ToString());

                if (MarkControll == null)
                    MarkControll = new MarkController();
                //retval = await MarkControll.OpenMarkControllerAsync();
                retval = MarkControll.OpenMarkController();
                if (retval != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("[ERROR_MARK_PORT_Open] : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                    //ITNTErrorLog.Instance.Trace(0, string.Format("[ERROR_MARK_PORT_Open] : {0}", retval));
                    //ShowErrorMessage("MARK Serial Port Open Error. Check Please", false);
                    //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO MARKING CONTROLLER");
                    brush = Brushes.Red;
                    status = "DISCONNECTED";
                    ShowLabelData(status, lblControllerConnectStatus, backbrush: brush);
                    errInfo.sErrorFunc = sCurrentFunc;
                    errInfo.sErrorMessage = sCurrentFunc + " FAIL = " + retval.ToString();
                    ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
                    //ShowLog(className, funcName, 2, "Controller CONNECTION FAIL", retval.ToString());
                }
                else
                {
                    fwVersion = await MarkControll.GetFWVersion();
                    if ((fwVersion.Length > 0) && (fwVersion.CompareTo("101") >= 0))
                        fwVersionFlag = 1;
                    else
                        fwVersionFlag = 0;

                    //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref headType, Constants.PARAMS_INI_FILE);
                    if (bHeadType == 0)
                        retval = InitializeController().Result.execResult;
                    else
                        retval = InitializeControllerLaser().Result.execResult;

                    if (retval != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("[InitializeController] : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                        //ITNTErrorLog.Instance.Trace(0, string.Format("[ERROR_MARK_PORT_Open] : {0}", retval));
                        //ShowErrorMessage("MARK Initialize Error. Check Please", false);
                        //ITNTJobLog.Instance.Trace(0, "FAIL TO INITIALIZE TO MARKING CONTROLLER");
                        brush = Brushes.Red;
                        //ShowLog(className, funcName, 2, "Controller CONNECTION FAIL", "INITIALIZE FAIL");
                        status = "DISCONNECTED";
                        ShowLabelData(status, lblControllerConnectStatus, backbrush: brush);
                        errInfo.sErrorFunc = "INITIALIZE CONTROLLER";
                        errInfo.sErrorMessage = errInfo.sErrorFunc + " FAIL = " + retval.ToString();
                        ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
                    }
                    else
                    {
                        bControllerInitFlag = 1;
                        //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO MARKING CONTROLLER");
                        if (bHeadType == 0)
                            MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
                        else
                            MarkControll.markCommLaser.MarkControllerDataArrivedEventFunc += OnLaserMarkControllerEventFunc;

                        brush = Brushes.Green;
                        ShowLog(className, funcName, 0, "Controller CONNECTION SUCCESS", "");
                        status = "CONNECTED";
                        ShowLabelData(status, lblControllerConnectStatus, backbrush: brush);
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO MARKING CONTROLLER");
                brush = Brushes.Red;
                //lblMainStatusMarkName.Background = Brushes.Red;
                retval = ex.HResult;
                //ShowLog(className, funcName, 2, "Controller CONNECTION FAIL", retval.ToString());
                status = "DISCONNECTED";
                ShowLabelData(status, lblControllerConnectStatus, backbrush: brush);
                errInfo.sErrorFunc = sCurrentFunc;
                errInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
            }

            //ShowDeviceStatus(lblMainStatusMarkName, brush);
            //ShowLabelData(status, lblControllerConnectStatus, backbrush: brush);

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        //public int OpenDistanceSensor()
        //{
        //    int retval = 0;
        //    distanceSensor.StartClient();
        //    return retval;
        //}

        public void CloseDistanceSensor()
        {
            if(distanceSensor != null)
                distanceSensor.CloseClient(1);

            //if (distanceSensor != null)
            //    distanceSensor.CloseClient(1);
        }

        private async Task<int> OpenLPMController()
        {
            string className = "MainWindow";
            string funcName = "OpenLPMController";
            int retval = 0;
            SolidColorBrush brush = new SolidColorBrush();
            string status = "";
            string sCurrentFunc = "CONNECTION TO LPM";
            ErrorInfo errInfo = new ErrorInfo();
            try
            {
                //ShowLog(className, funcName, 0, "Controller CONNECTION START", "");
                ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, sCurrentFunc + " START");

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (lpmControll == null)
                    lpmControll = new LPMControll();
                retval = lpmControll.OpenDevice();
                if (retval != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("[ERROR_LPM_PORT_Open] : {0}", retval), Thread.CurrentThread.ManagedThreadId);
                    brush = Brushes.Red;
                    status = "DISCONNECTED";
                    errInfo.sErrorMessage = sCurrentFunc + " FAIL - " + retval.ToString();
                    errInfo.sErrorFunc = sCurrentFunc;
                    //ITNTErrorCode(className, funcName, sErrorFunc, errInfo);
                }
                else
                {
                    lpmControll.LPMControllerDataReceivedEventFunc += OnLPMControllerEventFunc;
                    brush = Brushes.Green;
                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, sCurrentFunc + " SUCCESS", "");
                    status = "CONNECTED";
                    var message = System.Text.Encoding.UTF8.GetBytes("Z");
                    lpmControll.SendMessage(message);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                brush = Brushes.Red;
                lblMainStatusMarkName.Background = Brushes.Red;
                retval = ex.HResult;
                //ShowLog(className, funcName, 2, "LPM  FAIL", retval.ToString());
                status = "DISCONNECTED";
                errInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                errInfo.sErrorFunc = sCurrentFunc;
                //ITNTErrorCode(className, funcName, sErrorFunc, errInfo);
            }

            //ShowDeviceStatus(lblMainStatusMarkName, brush);
            //ShowLabelData(status, lblLPMConnectStatus, backbrush: brush);

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private void CloseLPM()
        {
            string className = "MainWindow";
            string funcName = "CloseLPM";
            string value = "";

            try
            {
                lpmControll.LPMControllerDataReceivedEventFunc -= OnLPMControllerEventFunc;
                lpmControll.CloseDevice();
            }
            catch (Exception ex)
            {

            }
        }
        //private void ShowDeviceStatus(Label label, SolidColorBrush brush)
        //{
        //    try
        //    {
        //        if ((label == null) || (brush == null))
        //            return;

        //        if (label.CheckAccess())
        //        {
        //            label.Background = brush;
        //        }
        //        else
        //        {
        //            label.Dispatcher.Invoke(new Action(delegate
        //            {
        //                label.Background = brush;
        //            }));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}


        //private void ShowLabel()
        //{

        //}
        private void OnConnectionStatusChanged(bool connected)
        {
            string status = connected ? "CONNECTED" : "DISCONNECTED";
            Brush brush = connected ? Brushes.Green : Brushes.Red;
            Dispatcher.Invoke(() =>
            {
                ShowLabelData(status, lblDisplaceConnectStatus, backbrush: brush);

                //lblStatus.Content = 
                //lblStatus.Foreground = connected ? Brushes.Green : Brushes.Red;
            });
        }

        public async Task<int> OpenDistanceSensor()
        {
            string className = "MainWindow";
            string funcName = "OpenDistanceSensor";
            int retval = 0;
            SolidColorBrush brush = new SolidColorBrush();
            string status = "";
            ErrorInfo errInfo = new ErrorInfo();
            string sCurrentFunc = "CONNECTION TO DISTANCE SENSOR";

            try
            {
                //ShowLog(className, funcName, 0, "OPEN DISTANCE SENSOR START");
                ShowLog(className, funcName, 0, sCurrentFunc + " START");

                if (distanceSensor == null)
                    distanceSensor = new DistanceSensor2(OnDisplaceStatusChangedEventHandler);
                    //distanceSensor = new DistanceSensor(OnDisplaceStatusChangedEventHandler);
                //distanceSensor = new DisplacementSensor(OnDisplaceStatusChangedEventHandler);
                //distanceSensor = new DistanceSensor(OnDisplaceStatusChangedEventHandler);
                retval = await distanceSensor.StartClientSync(5);
                if (retval != 0)
                {
                    //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO PLC");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DISTANCE SENSOR OPEN FAIL", Thread.CurrentThread.ManagedThreadId);
                    brush = Brushes.Red;
                    //ShowLog(className, funcName, 2, "DISPLACE SENSOR CONNECTION FAIL", retval.ToString());
                    status = "DISCONNECTED";
                    errInfo.sErrorMessage = sCurrentFunc + " FAIL = " + retval.ToString();
                    errInfo.sErrorFunc = sCurrentFunc;
                    ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
                }
                else
                {
                    //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO PLC");
                    brush = Brushes.Green;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DISTANCE SENSOR OPEN SUCCESS", Thread.CurrentThread.ManagedThreadId);
                    status = "CONNECTED";
                    ShowLog(className, funcName, 0, sCurrentFunc + " SUCCESS", "");
                }
                //ShowDeviceStatus(lblMainStatusPLCName, brush);
                ShowLabelData(status, lblDisplaceConnectStatus, backbrush: brush);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                brush = Brushes.Red;
                status = "DISCONNECTED";
                ShowLabelData(status, lblDisplaceConnectStatus, backbrush: brush);
                retval = ex.HResult;
                //errInfo.sErrorDevMsg = "EXCEPTION = " + ex.Message;
                errInfo.sErrorFunc = sCurrentFunc;
                ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
            }
            //distanceSensor.StartClient();
            return retval;
        }

        //public async Task<int> OpenDistanceSensor2()
        //{
        //    string className = "MainWindow";
        //    string funcName = "OpenDistanceSensor";
        //    int retval = 0;
        //    SolidColorBrush brush = new SolidColorBrush();
        //    string status = "";
        //    ErrorInfo errInfo = new ErrorInfo();
        //    string sCurrentFunc = "CONNECTION TO DISTANCE SENSOR";

        //    try
        //    {
        //        //ShowLog(className, funcName, 0, "OPEN DISTANCE SENSOR START");
        //        ShowLog(className, funcName, 0, sCurrentFunc + " START");

        //        _cts = new CancellationTokenSource();
        //        if (distanceSensor == null)
        //            //distanceSensor = new DisplacementSensor(OnDisplaceStatusChangedEventHandler);
        //            distanceSensor = new DistanceSensorAI2(this, OnDisplaceStatusChangedEventHandler);

        //        distanceSensor.ConnectionStatusChanged += TcpClient_ConnectionStatusChanged;
        //        distanceSensor.LogMessageReceived += TcpClient_LogMessageReceived;

        //        //distanceSensor = new DistanceSensor(OnDisplaceStatusChangedEventHandler);
        //        await distanceSensor.ConnectAsync(_cts.Token);

        //        //distanceSensor5.ConnectionStatusChanged += DistanceSensor5_ConnectionStatusChanged;
        //        //distanceSensor5.LogMessageReceived += DistanceSensor5_LogMessageReceived;


        //        //_tcpClient.ConnectionStatusChanged += TcpClient_ConnectionStatusChanged;
        //        //_tcpClient.LogMessageReceived += TcpClient_LogMessageReceived;

        //        //if (retval != 0)
        //        //{
        //        //    //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO PLC");
        //        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DISTANCE SENSOR OPEN FAIL", Thread.CurrentThread.ManagedThreadId);
        //        //    brush = Brushes.Red;
        //        //    //ShowLog(className, funcName, 2, "DISPLACE SENSOR CONNECTION FAIL", retval.ToString());
        //        //    status = "DISCONNECTED";
        //        //    errInfo.sErrorMessage = sCurrentFunc + " FAIL = " + retval.ToString();
        //        //    errInfo.sErrorFunc = sCurrentFunc;
        //        //    ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
        //        //}
        //        //else
        //        //{
        //        //    //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO PLC");
        //        //    brush = Brushes.Green;
        //        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DISTANCE SENSOR OPEN SUCCESS", Thread.CurrentThread.ManagedThreadId);
        //        //    status = "CONNECTED";
        //        //    ShowLog(className, funcName, 0, sCurrentFunc + " SUCCESS", "");
        //        //}
        //        //ShowDeviceStatus(lblMainStatusPLCName, brush);
        //        //ShowLabelData(status, lblDisplaceConnectStatus, backbrush: brush);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        brush = Brushes.Red;
        //        status = "DISCONNECTED";
        //        ShowLabelData(status, lblDisplaceConnectStatus, backbrush: brush);
        //        retval = ex.HResult;
        //        //errInfo.sErrorDevMsg = "EXCEPTION = " + ex.Message;
        //        errInfo.sErrorFunc = sCurrentFunc;
        //        ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
        //    }
        //    //distanceSensor.StartClient();
        //    return retval;
        //}


        //public async Task<int> OpenDistanceSensor3()
        //{
        //    string className = "MainWindow";
        //    string funcName = "OpenDistanceSensor";
        //    int retval = 0;
        //    SolidColorBrush brush = new SolidColorBrush();
        //    string status = "";
        //    ErrorInfo errInfo = new ErrorInfo();
        //    string sCurrentFunc = "CONNECTION TO DISTANCE SENSOR";

        //    try
        //    {
        //        //ShowLog(className, funcName, 0, "OPEN DISTANCE SENSOR START");
        //        ShowLog(className, funcName, 0, sCurrentFunc + " START");

        //        if (_tcpClient == null)
        //            //distanceSensor = new DisplacementSensor(OnDisplaceStatusChangedEventHandler);
        //            _tcpClient = new DistanceSensorAI2(OnDisplaceStatusChangedEventHandler);

        //        _tcpClient.ConnectionStatusChanged += TcpClient_ConnectionStatusChanged;
        //        _tcpClient.LogMessageReceived += TcpClient_LogMessageReceived;
        //        //distanceSensor = new DistanceSensor(OnDisplaceStatusChangedEventHandler);
        //        retval = await distanceSensor.StartClientSync(5);
        //        if (retval != 0)
        //        {
        //            //ITNTJobLog.Instance.Trace(0, "FAIL TO CONNECTION TO PLC");
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DISTANCE SENSOR OPEN FAIL", Thread.CurrentThread.ManagedThreadId);
        //            brush = Brushes.Red;
        //            //ShowLog(className, funcName, 2, "DISPLACE SENSOR CONNECTION FAIL", retval.ToString());
        //            status = "DISCONNECTED";
        //            errInfo.sErrorMessage = sCurrentFunc + " FAIL = " + retval.ToString();
        //            errInfo.sErrorFunc = sCurrentFunc;
        //            ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
        //        }
        //        else
        //        {
        //            //ITNTJobLog.Instance.Trace(0, "SUCCESS TO CONNECTION TO PLC");
        //            brush = Brushes.Green;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DISTANCE SENSOR OPEN SUCCESS", Thread.CurrentThread.ManagedThreadId);
        //            status = "CONNECTED";
        //            ShowLog(className, funcName, 0, sCurrentFunc + " SUCCESS", "");
        //        }
        //        //ShowDeviceStatus(lblMainStatusPLCName, brush);
        //        ShowLabelData(status, lblDisplaceConnectStatus, backbrush: brush);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        brush = Brushes.Red;
        //        status = "DISCONNECTED";
        //        ShowLabelData(status, lblDisplaceConnectStatus, backbrush: brush);
        //        retval = ex.HResult;
        //        //errInfo.sErrorDevMsg = "EXCEPTION = " + ex.Message;
        //        errInfo.sErrorFunc = sCurrentFunc;
        //        ITNTErrorCode(className, funcName, sCurrentFunc, errInfo);
        //    }
        //    //distanceSensor.StartClient();
        //    return retval;
        //}

        private void TcpClient_ConnectionStatusChanged(object sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionStatusText.Text = status;
                switch (status)
                {
                    case "CONNECTED":
                        ConnectionStatusText.Foreground = Brushes.Green;
                        lblDisplaceConnectStatus.Background = Brushes.Green;
                        break;
                    case "CONNECTING":
                    //case "연결 실패, 재시도 중...":
                    //case "통신 오류 발생, 재접속 시도 중...":
                        ConnectionStatusText.Foreground = Brushes.Orange;
                        lblDisplaceConnectStatus.Background = Brushes.Orange;
                        break;
                    case "DISCONNECTED":
                        ConnectionStatusText.Foreground = Brushes.Red;
                        lblDisplaceConnectStatus.Background = Brushes.Red;
                        break;
                    default:
                        ConnectionStatusText.Foreground = Brushes.Black;
                        lblDisplaceConnectStatus.Background = Brushes.Red;
                        break;
                }
            });
        }

        //private void TcpClient_LogMessageReceived(object? sender, LogEventArgs e)
        //{
        //    Dispatcher.Invoke(() =>
        //    {
        //        var time = DateTime.Now.ToString("HH:mm:ss");
        //        LogEntries.Add(new LogItem { Message = $"[{time}] [{e.Level}] {e.Message}", Level = e.Level });

        //        if (LogListBox.Items.Count > 0)
        //        {
        //            LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
        //        }
        //    });
        //}


        void CloseMarkController()
        {
            string className = "MainWindow";
            string funcName = "CloseMarkController";
            string sCurrentFunc = "CLOSE MARK CONTROLLER";

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                ShowLog(className, funcName, 0, sCurrentFunc + " START");
                if (MarkControll != null)
                    MarkControll.CloseMarkController();
                ShowLog(className, funcName, 0, sCurrentFunc + " END");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        int OpenVisionServer()
        {
            string className = "MainWindow";
            string funcName = "OpenVisionServer";

            string value = "";
            string serverIP = "", serverPort = "";
            string visionIP = "";
            int retval = 0;
            int useVision = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "START");
                //ShowLog(className, funcName, 0, "CONNECTION TO VISION PC START", "");

                useVision = (int)Util.GetPrivateProfileValueUINT("VISION", "USEVISION", 1, Constants.PARAMS_INI_FILE);
                if (useVision == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT USE VISION", Thread.CurrentThread.ManagedThreadId);
                    return 0;
                }

                Util.GetPrivateProfileValue("VISION", "SERVERPORT", "0", ref serverPort, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("VISION", "SERVERIP", "192.168.0.37", ref serverIP, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);

                if((value == "0") || (value == "2"))
                {
                    if (visionServer == null)
                        visionServer = new VisionServer2();
                    visionServer.StartServer(IPAddress.Any, Convert.ToInt32(serverPort));
                }
                else
                {
                    if (visionClient == null)
                        visionClient = new ITNTClientAsync2(ClientDataArrival, ClientConnectionStatus);
                    visionClient.StartClient();
                }

                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "END");
                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
                return ex.HResult;
            }
        }

        void ClientDataArrival(string msg)
        {
            AnalysisReceivedTcpMsg(msg);
        }

        void ClientConnectionStatus(DeviceStatusChangedEventArgs status)
        {
            //csConnStatus ClientStatus;
            //if (imgMainConnToMarking.CheckAccess() == false)
            //{
            //    imgMainConnToMarking.Dispatcher.Invoke(new System.Action(delegate
            //    {
            //        if (markCommType == 0)
            //            ClientStatus = client.GetConnectionStatus();
            //        else
            //            ClientStatus = asyncClient.GetConnectionStatus();

            //        if (ClientStatus == csConnStatus.Connected)
            //        {
            //            //ShowLog("MainWindow", "ClientConnectionStatus", 0, "MES CONNECTION SUCCESS", "");
            //            //lblMarkConn.Background = brushConnect;
            //            imgMainConnToMarking.Source = Util.GetImageSource("./images/connect.png");
            //            ShowLabelData(lblMarkConn, "MARK", backbrush: brushConnect);
            //        }
            //        else
            //        {
            //            //ShowLog("MainWindow", "ClientConnectionStatus", 0, "MES CONNECTION FAIL", "");
            //            //lblMarkConn.Background = Brushes.Red;
            //            imgMainConnToMarking.Source = Util.GetImageSource("./images/disconnect.png");
            //            ShowLabelData(lblMarkConn, "MARK", backbrush: Brushes.Red);
            //        }
            //    }));
            //}
            //else
            //{
            //    if (markCommType == 0)
            //        ClientStatus = client.GetConnectionStatus();
            //    else
            //        ClientStatus = asyncClient.GetConnectionStatus();

            //    if (ClientStatus == csConnStatus.Connected)
            //    {
            //        //ShowLog("MainWindow", "ClientConnectionStatus", 0, "MES CONNECTION SUCCESS", "");
            //        //lblMarkConn.Background = brushConnect;
            //        imgMainConnToMarking.Source = Util.GetImageSource("./images/connect.png");
            //    }
            //    else
            //    {
            //        //ShowLog("MainWindow", "ClientConnectionStatus", 0, "MES CONNECTION FAIL", "");
            //        //lblMarkConn.Background = Brushes.Red;
            //        imgMainConnToMarking.Source = Util.GetImageSource("./images/disconnect.png");
            //    }
            //}

            //if (lblMarkConn.CheckAccess() == false)
            //{
            //    lblMarkConn.Dispatcher.Invoke(new System.Action(delegate
            //    {
            //        if (markCommType == 0)
            //            ClientStatus = client.GetConnectionStatus();
            //        else
            //            ClientStatus = asyncClient.GetConnectionStatus();

            //        if (ClientStatus == csConnStatus.Connected)
            //            lblMarkConn.Background = brushConnect;// Brushes.Green;
            //        else
            //            lblMarkConn.Background = Brushes.Red;
            //    }));
            //}
            //else
            //{
            //    if (markCommType == 0)
            //        ClientStatus = client.GetConnectionStatus();
            //    else
            //        ClientStatus = asyncClient.GetConnectionStatus();

            //    if (ClientStatus == csConnStatus.Connected)
            //        lblMarkConn.Background = brushConnect;// Brushes.Green;
            //    else
            //        lblMarkConn.Background = Brushes.Red;
            //}
        }


        private async void AnalysisReceivedTcpMsg(string msg)
        {
            string className = "MainWindow";
            string funcName = "AnalysisReceivedTcpMsg";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            MESReceivedData receivedMsg = new MESReceivedData();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                retval = await AnalyzeVisionReceivedData(msg);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AnalyzeReceivedServerData return Error " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //SaveMESReceivedData(msg, receivedMsg);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnReceiveMESServerDataHandler", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                return;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return;
        }


        int OpenVisionServer2()
        {
            string className = "MainWindow";
            string funcName = "OpenVisionServer";

            int retval = 0;
            string ServerPort = "";
            int useVision = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                useVision = (int)Util.GetPrivateProfileValueUINT("VISION", "USEVISION", 1, Constants.PARAMS_INI_FILE);
                if (useVision == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT USE VISION", Thread.CurrentThread.ManagedThreadId);
                    return 0;
                }

                //Util.GetPrivateProfileValue("VISION", "SERVERPORT", "0", ref ServerPort, Constants.PARAMS_INI_FILE);
                //if (vision == null)
                //    vision = new VisionServer2();
                //vision.DataArrivedEventFunc += OnReceiveVisionServerDataHandler;
                ////MESServer.statusEvent += OnMESSeverStatusChangedEventHandler;
                //retval = vision.StartServer(IPAddress.Any, Convert.ToInt32(ServerPort)).Result;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        void CloseVisionServer()
        {
            int useVision = 0;
            string value = "";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseVisionServer", "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                useVision = (int)Util.GetPrivateProfileValueUINT("VISION", "USEVISION", 1, Constants.PARAMS_INI_FILE);
                if (useVision == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OpenVisionServer", "NOT USE VISION", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if ((value == "0") || (value == "2"))
                {
                    if (visionServer != null)
                        visionServer.ServerClose();
                }
                else
                {
                    if (visionClient != null)
                        visionClient.CloseClient();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseVisionServer", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseVisionServer", "END", Thread.CurrentThread.ManagedThreadId);
        }

        void CloseVisionServer2()
        {
            int useVision = 0;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseVisionServer", "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                useVision = (int)Util.GetPrivateProfileValueUINT("VISION", "USEVISION", 1, Constants.PARAMS_INI_FILE);
                if (useVision == 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OpenVisionServer", "NOT USE VISION", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //if (vision != null)
                //{
                //    vision.DataArrivedEventFunc -= OnReceiveVisionServerDataHandler;
                //    vision.ServerClose();
                //}
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseVisionServer", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CloseVisionServer", "END", Thread.CurrentThread.ManagedThreadId);
        }

        private async Task<ITNTResponseArgs> OnReceiveVisionServerDataHandler(object sender, ITNTResponseArgs arg)
        {
            string className = "MainWindow";
            string funcName = "OnReceiveVisionServerDataHandler";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            string msg = arg.recvString;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            MESReceivedData receivedMsg = new MESReceivedData();

            try
            {
                retval = await AnalyzeVisionReceivedData(msg);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AnalyzeReceivedServerData return Error " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                //SaveMESReceivedData(msg, receivedMsg);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnReceiveMESServerDataHandler", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        private int UpdateSelectedPlanData(MESReceivedData msg)
        {
            string className = "MainWindow";
            string funcName = "UpdateSelectedPlanData";
            string value = "";
            //int palette = 0;
            DataTable dbMainDataTable = new DataTable();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            string commandstring = "";
            string dbdisplay = "";

            try
            {
                //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock", Thread.CurrentThread.ManagedThreadId);

                //Util.GetPrivateProfileValue("OPTION", "PALETTE", "0", ref value, Constants.PARAMS_INI_FILE);
                //int.TryParse(value, out palette);

                //lock (DBLock)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED", Thread.CurrentThread.ManagedThreadId);

                    //db.Open(Constants.connstring);
                    Util.GetPrivateProfileValue("OPTION", "DELETEAFTERCOMPLETE", "0", ref value, Constants.PARAMS_INI_FILE);
                    if (value == "1")
                        commandstring = "DELETE FROM " + tableName + " WHERE RAWVIN ='" + msg.rawvin + "' AND SEQUENCE = '" + msg.sequence + "'";
                    else
                        commandstring = "UPDATE " + tableName + " SET ISMARK='Y' WHERE RAWVIN='" + msg.rawvin + "' AND SEQUENCE='" + msg.sequence + "'";

                    dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    dbdisplay = MakeDBDisplayText(tableName);
                    dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);

                if (dgdPlanData.CheckAccess())
                {
                    dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                    dgdPlanData.Items.Refresh();
                    if (dgdPlanData.Items.Count > 0)
                    {
                        //dgdPlanData.SelectedIndex = 0;
                        dgdPlanData.UpdateLayout();
                    }
                }
                else
                {
                    dgdPlanData.Dispatcher.Invoke(new Action(delegate
                    {
                        dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                        dgdPlanData.Items.Refresh();
                        if (dgdPlanData.Items.Count > 0)
                        {
                            //dgdPlanData.SelectedIndex = 0;
                            dgdPlanData.UpdateLayout();
                        }
                    }));
                }
                int count = 0;
                int totcount = 0;
                (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                ShowWorkPlanCount(lblWorkPlanDataCount, count);
                ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                CheckPlanDataCountWarning(count, lblPlanDataWarning);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            return 0;
        }


        private int DeleteMarkedPlanData(DataGrid grid)
        {
            DataTable dbMainDataTable = new DataTable();
            //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();
            string dbdisplay = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "DeleteMarkedPlanData", "DBLock", Thread.CurrentThread.ManagedThreadId);
                //lock (DBLock)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "DeleteMarkedPlanData", "LOCKED", Thread.CurrentThread.ManagedThreadId);

                    dbdisplay = MakeDBDisplayText(tableName);

                    dbwrap.ExecuteCommand(Constants.connstring, "DELETE FROM " + tableName + " WHERE RAWVIN='" + currMarkInfo.currMarkData.mesData.rawvin + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    dbwrap.ExecuteCommand(Constants.connstring, "DELETE FROM " + tableName + " WHERE ISMARK='Y'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "DeleteMarkedPlanData", "LOCK END", Thread.CurrentThread.ManagedThreadId);

                if (dgdPlanData.CheckAccess())
                {
                    dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                    dgdPlanData.Items.Refresh();
                    if (dgdPlanData.Items.Count > 0)
                    {
                        //dgdPlanData.SelectedIndex = 0;
                        dgdPlanData.UpdateLayout();
                    }
                }
                else
                {
                    dgdPlanData.Dispatcher.Invoke(new Action(delegate
                    {
                        dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                        dgdPlanData.Items.Refresh();
                        if (dgdPlanData.Items.Count > 0)
                        {
                            //dgdPlanData.SelectedIndex = 0;
                            dgdPlanData.UpdateLayout();
                        }
                    }));
                }

                int count = 0;
                int totcount = 0;
                //lock (getCountLock)
                {
                    //count = GetMarkPlanDataCount(dgdPlanData);
                    (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                }
                ShowWorkPlanCount4Thread(lblWorkPlanDataCount, count);
                ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                CheckPlanDataCountWarning(count, lblPlanDataWarning);

                //ShowWorkPlanCountNTime(lblWorkPlanDataCount, lblVINLastUpdateDate, count, "", false);
            }
            catch (Exception ex)
            {
                //ErrorCode = string.Format("000E{0:X8}", Math.Abs(ex.HResult));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "DeleteMarkedPlanData", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            return 0;
        }

        private int CheckMarkControllerSensor(byte[] sensor, int length)
        {
            int retval = 0;

            return retval;
        }


        private async void OnMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnMarkControllerEventFunc";

            string param1 = "";
            string param2 = "";
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            byte[] sensor = new byte[8];
            int retval = 0;
            DataRowView row = null;
            //short Length;
            //short steplength;
            string value = "";
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            byte currCMD = 0;
            //string headType = "";

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                //lock(eventLock)
                {
                    i = 6;
                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    Array.Copy(e.receiveBuffer, i, sensor, 0, 4);
                    retval = CheckMarkControllerSensor(sensor, 4);

                    //if (currentWindow != 0)
                    //    return;
                    currCMD = e.execmd;

                    switch (e.stscmd)
                    {
                        case 0x30:      //stand by
                            ITNTTraceLog.Instance.TraceHex(1, "MainWindow::OnMarkControllerEventFunc()  RECV MARK :  ", e.receiveSize, e.receiveBuffer);

                            if (param1.Length > 0)
                                chindex = Convert.ToInt32(param1, 16);
                            if (param2.Length > 0)
                                ptindex = Convert.ToInt32(param2, 16);
                            //if (param3.Length > 0)
                            //    drindex = Convert.ToInt32(param3, 16);

                            if ((currCMD == 'R') && (m_currCMD == 'R'))
                            {
                                if (!m_bDoingMarkingFlag)
                                    m_bDoingMarkingFlag = true;

                                if (this.CheckAccess())
                                    ShowMarkingOneLine(chindex, ptindex);
                                else
                                {
                                    this.Dispatcher.Invoke(new Action(delegate
                                    {
                                        ShowMarkingOneLine(chindex, ptindex);
                                    }));
                                }
                            }
                            //Task.Delay(100);
                            break;

                        case 0x31:      //running
                            break;

                        case 0x32:      //run ok
                            break;

                        //case 0x33:      //home ok
                        //    break;
                        //case 0x34:      //jog ok
                        //    break;
                        //case 0x35:      //test ok
                        //    break;
                        //case 0x36:      //go ok
                        //    break;
                        case 0x37:      //cold boot
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COLD BOOT", Thread.CurrentThread.ManagedThreadId);

                            //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref headType, Constants.PARAMS_INI_FILE);
                            bControllerInitFlag = 0;
                            if (bHeadType == 0)
                                retval = InitializeController().Result.execResult;
                            else
                                retval = InitializeControllerLaser().Result.execResult;

                            //retval = InitializeController().Result.execResult;
                            if (retval == 0)
                            {
                                doingCommand = true;
                                Stopwatch sw = new Stopwatch();
                                sw.Start();
                                while (sw.Elapsed < TimeSpan.FromSeconds(6))
                                {
                                    if (!doingCommand)
                                        break;

                                    await Task.Delay(50);
                                }
                                bControllerInitFlag = 1;
                            }
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COLD BOOT END", Thread.CurrentThread.ManagedThreadId);
                            break;

                        case 0x38:
                            //count = 0;
                            double dvalue = 0.0d;
                            double.TryParse(param1, out dvalue);
                            currentPoint.X = dvalue;
                            double.TryParse(param1, out dvalue);
                            currentPoint.Y = dvalue;

                            doingCommand = false;
                            if ((currCMD == 'U') && (m_currCMD == 'U'))
                            {
                                //ITNTJobLog.Instance.Trace(0, "[4] : RECEIVE SCAN COMPLETE");
                                Util.GetPrivateProfileValue("OPTION", "VISIONQUICKEND", "0", ref value, Constants.PARAMS_INI_FILE);
                                if(value != "0")
                                    recvarg = await plcComm.SendScanComplete(1);
                            }

                            if (((currCMD == 'R') && (m_currCMD == 'R')) ||
                                (currCMD == '@'))
                            {
                                //ITNTJobLog.Instance.Trace(0, "[4] : RECEIVE MARKING COMPLETE");

#if MANUAL_MARK
                                //ShowCurrentStateLabel(5);
                                ShowCurrentStateLabelManual(4);
                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
#else
                                ShowLog(className, funcName, 0, "[2-3] MARKING COMPLETE");

                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_MARKING);
#endif
                                ShowLabelData("[4] : MARKING COMPLETE", lblPLCData);
                                ShowLabelData("MARKING COMPLETE", lblCheckResult, Brushes.Blue);

                                //
                                currMarkInfo.currMarkData.mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
                                currMarkInfo.currMarkData.mesData.marktime = DateTime.Now.ToString("HH:mm:ss");

                                SaveMarkResultData(currMarkInfo.currMarkData.mesData, 0, 0, currMarkInfo.currMarkData.multiMarkFlag, currMarkInfo.currMarkData.markorderFlag, currMarkInfo.checkdata, 0);
                                WriteCompleteData(currMarkInfo.currMarkData.mesData, 0);

                                //Util.GetPrivateProfileValue("OPTION", "UPDATEFLAG", "0", ref value, Constants.PARAMS_INI_FILE);
                                ////if(value == "0")
                                //{
                                //    row = GetNextMarkPointData();
                                //    UpdatePlanDatabase(dgdPlanData, row);
                                //}

                                UpdateCompleteDatabaseThread(dgdPlanData, true, 0);

                                //DeletePlanData();
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_SEND_MARKCOMPLETE);
                                recvarg = await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE);
                                if (recvarg.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMarkingStatus ERROR - " + recvarg.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                    ShowErrorMessage("SEND COMPLETE SIGNAL TO PLC ERROR", false);
                                }

                                ShowLog(className, funcName, 0, "[3] JOB COMPLETE");

                                //retval = ImageProcessManager.GetFontData(vininfo, ref MyData, out fontSizeX, out fontSizeY, out ErrorCode);
                                //if (retval != 0)
                                //    return;


                                //await ShowCurrentMarkingInformation(-1, currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.mesData.sequence, currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.pattern, 1);
                                await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, null, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2, 1);

                                //markCompleteFlag = true;
                                //Length = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, Constants.MARKING_INI_FILE);
                                //steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

                                //Length = (short)(Length * steplength);
                                //recvarg = await MarkControll.GoHome(0, Length);

                                m_bDoingMarkingFlag = false;
                                //m_bDoingNextVINFlag = false;

                                //Util.WritePrivateProfileValue("CURRENT", "VIN", currMarkInfo.currMarkData.mesData.vin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);
                                //Util.WritePrivateProfileValue("CURRENT", "INDEX", m_CurrentMarkNum.ToString(), Constants.DATA_CUR_COMPLETE_FILE);
                                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", currMarkInfo.currMarkData.mesData.sequence.Trim() + "|" + currMarkInfo.currMarkData.mesData.rawvin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);

                                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.currMarkData.mesData.sequence + "-" + currMarkInfo.currMarkData.mesData.vin);
                                ////ITNTJobLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.currMarkData.mesData.sequence + "-" + currMarkInfo.currMarkData.mesData.vin);

                                //currMarkInfo.Initialize();
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[4] : MARKING COMPLETE", Thread.CurrentThread.ManagedThreadId);
                                //ITNTJobLog.Instance.Trace(0, "[4] : MARKING COMPLETE");
                                markRunTimer.Stop();
                                //ShowCurrentStateLabel(7);
                                m_currCMD = 0;
                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_COMPLETE_JOB);

                                if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                                {
                                    await ChangeDBProcess4Thread();
                                }

                                //if(mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE)
                                //{
                                //    Thread ccr2workThread = new Thread(new ParameterizedThreadStart(CCR2WORK2));
                                //    ccr2workThread.Start(dgdPlanData);
                                //}

                                int ivalue = 0;
                                ivalue = (int)Util.GetPrivateProfileValueUINT("OPTION", "MarkingCount", 0, Constants.PARAMS_INI_FILE);
                                ivalue++;
                                value = ivalue.ToString();
                                ShowLabelData(value, lblMarkingCount);
                                Util.WritePrivateProfileValue("OPTION", "MarkingCount", value, Constants.PARAMS_INI_FILE);

                                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_WAIT_VISION);
                            }
                            else
                            {

                            }
                            break;

                        case 0x39:      //emergency
                            break;

                        default:
                            break;
                    }
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void ColdBootThreadFunc(object obj)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                bControllerInitFlag = 0;

                if (bHeadType == 0)
                    retval = await InitializeController();
                else
                    retval = await InitializeControllerLaser();

                //retval = InitializeController().Result.execResult;
                if (retval.execResult == 0)
                {
                    //doingCommand = true;
                    //Stopwatch sw = new Stopwatch();
                    //sw.Start();
                    //while (sw.Elapsed < TimeSpan.FromSeconds(6))
                    //{
                    //    if (!doingCommand)
                    //        break;

                    //    await Task.Delay(50);
                    //}
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ColdBootThreadFunc", "BOOT SUCCESS", Thread.CurrentThread.ManagedThreadId);
                    bControllerInitFlag = 1;
                }
                else
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ColdBootThreadFunc", "BOOT ERROR", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ColdBootThreadFunc", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void OnLPMControllerEventFunc(object sender, LPMControllerRecievedEvnetArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnLPMControllerEventFunc";
            string param1 = "";
            string param2 = "";
            string param3 = "";
            string param4 = "";
            int iparam1 = 0, iparam2 = 0, iparam3 = 0, iparam4 = 0;
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            //byte[] sensor = new byte[8];
            int retval = 0;
            DataRowView row = null;
            string value = "";
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            byte currCMD = 0;
            string log = "";
            double power = 0;
            int slope = 0;
            int intercept = 0;

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                Util.GetPrivateProfileValue("LENZ", "SLOPE", "0.1", ref value, Constants.LENZ_INI_FILE);
                int.TryParse(value, out slope);

                Util.GetPrivateProfileValue("LENZ", "INTER", "0.0", ref value, Constants.LENZ_INI_FILE);
                int.TryParse(value, out intercept);

                switch (e.execmd)
                {
                    case 'M':
                        if (byLaserStartFlag != 0)
                        {
                            iLPMPowerList.Add(e.value);
                        }

                        //s_peakPower = GetLabelData(lblPeakPowerValue);
                        //double.TryParse(s_peakPower, out d_peakPower);
                        var x = slope * (double)e.value + intercept;       // ADC => %
                        var w = 68.24 * x - 181.1;                         // %   => watt
                        power = w;

                        var x1 = slope * (double)e.value + intercept + iLaserPeakPower * 0.98;     // ADC => %
                        var x2 = slope * (double)e.value + intercept + iLaserPeakPower * 0.99;     // ADC => %
                        var x3 = iLaserPeakPower * 0.99;
                        //if (d_peakPower > 0)
                        if ((x1 > iLaserPeakPower * 0.98) && (x1 < iLaserPeakPower))
                            power = x1;
                        else if ((x2 > iLaserPeakPower * 0.98) && (x2 < iLaserPeakPower))
                            power = x2;
                        else
                            power = x3;

                        if (power < iLaserPeakPower * 0.975)
                        {
                            await plcComm.SendLaserLowPowerError(1);
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "LASER POWER WEAK WANRING : " + iLaserPeakPower.ToString("F0") + ", " + power.ToString("F3"));
                            ShowLabelData("LASER POWER WARNING", lblPowerWarning);
                            if (lblPowerWarning.Visibility != Visibility.Visible)
                                lblPowerWarning.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            await plcComm.SendLaserLowPowerError(0);
                            if (lblPowerWarning.Visibility == Visibility.Visible)
                                lblPowerWarning.Visibility = Visibility.Collapsed;
                        }
                        ShowLabelData(power.ToString("F0"), lblreadlPowerValue);
                        break;

                    case 'Z':
                        ShowLabelData("Ready", lblreadlPowerValue);

                        break;
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private async void OnLaserMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        //{
        //    string className = "MainWindow";
        //    string funcName = "OnLaserMarkControllerEventFunc";
        //    string param1 = "";
        //    string param2 = "";
        //    string param3 = "";
        //    string param4 = "";
        //    int iparam1 = 0, iparam2 = 0, iparam3 = 0, iparam4 = 0;
        //    int i = 0;
        //    int chindex = 0;
        //    int ptindex = 0;
        //    //byte[] sensor = new byte[8];
        //    int retval = 0;
        //    DataRowView row = null;
        //    string value = "";
        //    ITNTResponseArgs recvarg = new ITNTResponseArgs();
        //    byte currCMD = 0;
        //    string log = "";
        //    int basesize = 0;

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //        //OnLaserMarkControllerEventTask(sender, e);

        //        Task task = Task.Run(() => OnLaserMarkControllerEventTask(sender, e));
        //        //await task;

        //        ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private async void OnLaserMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        {
            string className = "MainWindow";
            string funcName = "OnLaserMarkControllerEventFunc";
            string param1 = "";
            string param2 = "";
            string param3 = "";
            string param4 = "";
            int iparam1 = 0, iparam2 = 0, iparam3 = 0, iparam4 = 0;
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            //byte[] sensor = new byte[8];
            int retval = 0;
            DataRowView row = null;
            string value = "";
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            byte currCMD = 0;
            string log = "";
            int basesize = 0;

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                i = 6;
                basesize = 10;
                if (e.receiveSize >= basesize)
                {
                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    try
                    {
                        iparam1 = Convert.ToInt32(param1, 16);
                    }
                    catch (Exception ex)
                    {
                        int.TryParse(param1, System.Globalization.NumberStyles.HexNumber, null, out iparam1);
                    }
                }
                if (e.receiveSize >= basesize + 4)
                {
                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    try
                    {
                        iparam2 = Convert.ToInt32(param2, 16);
                    }
                    catch (Exception ex)
                    {
                        int.TryParse(param2, System.Globalization.NumberStyles.HexNumber, null, out iparam2);
                    }
                }
                if (e.receiveSize >= basesize + 8)
                {
                    param3 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    try
                    {
                        iparam3 = Convert.ToInt32(param3, 16);
                    }
                    catch (Exception ex)
                    {
                        int.TryParse(param3, System.Globalization.NumberStyles.HexNumber, null, out iparam3);
                    }
                    //param3Flag = true;
                }
                if (e.receiveSize >= basesize + 12)
                {
                    param4 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    try
                    {
                        iparam4 = Convert.ToInt32(param4, 16);
                    }
                    catch (Exception ex)
                    {
                        int.TryParse(param4, System.Globalization.NumberStyles.HexNumber, null, out iparam4);
                    }
                    //param4Flag = true;
                }

                currCMD = e.execmd;

                switch (e.stscmd)
                {
                    case 0x30:      //stand by
                        //ITNTTraceLog.Instance.TraceHex(1, className + "::" + funcName + "()  RECV MARK :  ", e.receiveSize, e.receiveBuffer);
                        chindex = (iparam4 >> 8);
                        ptindex = iparam4 & 0xff;

                        if (chindex == (byte)'J')
                        {
                            chindex = 0;
                        }

                        //if (((currCMD == 'R') && (m_currCMD == 'R')) || ((currCMD == '@') && (m_currCMD == '@')))
                        if ((m_currCMD == 'R') || (m_currCMD == '@'))
                        {
                            if (!m_bDoingMarkingFlag)
                                m_bDoingMarkingFlag = true;


                            if (this.CheckAccess())
                            {
                                if (currMarkInfo.senddata.CleanFireFlag == true && currMarkInfo.checkdata.TwoLineDisplay == true)
                                    ShowMarkingOneLine(chindex, ptindex - 1);
                                ShowMarkingOneLine(chindex, ptindex);
                            }
                            else
                            {
                                this.Dispatcher.Invoke(new Action(delegate
                                {
                                    if (currMarkInfo.senddata.CleanFireFlag == true && currMarkInfo.checkdata.TwoLineDisplay == true)
                                        ShowMarkingOneLine(chindex, ptindex - 1);
                                    ShowMarkingOneLine(chindex, ptindex);
                                }));
                            }

                            //if (this.CheckAccess())
                            //    ShowLaserMarkingOneLine(chindex, ptindex);
                            //else
                            //{
                            //    this.Dispatcher.Invoke(new Action(delegate
                            //    {
                            //        ShowLaserMarkingOneLine(chindex, ptindex);
                            //    }));
                            //}
                        }
                        //Task.Delay(100);
                        break;

                    case 0x31:      //running
                        if ((currCMD == (byte)'@') ||
                            (currCMD == (byte)'H') ||
                            (currCMD == (byte)'J') ||
                            (currCMD == (byte)'M') ||
                            (currCMD == (byte)'K'))
                        {
                            if ((iparam4 & 0x8000) != 0)
                            {
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "MARKING", "LASER ERROR!!!!");
                                //laserErrorFlag = 1;
                                //ShowLabelData("1", lblLaserError, Brushes.White, Brushes.Red);
                                ////lblLaserError.Background = Brushes.Red;
                                ////lblLaserError.Content = "1";
                                //MarkControll.SetLaserErrorStatus(true);

                                //ShowButtonColor(btnLaserReset, null, Brushes.Red);
                            }
                            else
                            {
                                //laserErrorFlag = 0;
                                //lblLaserError.Background = Brushes.Green;
                                //lblLaserError.Content = "0";
                                //ShowLabelData("0", lblLaserError, Brushes.White, Brushes.Green);
                                //MarkControll.SetLaserErrorStatus(false);

                                //ShowButtonColor(btnLaserReset, null, Brushes.LightGray);
                            }

                            if ((iparam4 & 0x07) != 0)
                            {
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "MARKING", "MOTOR ERROR!!!!");
                                //motorErrorFlag = 1;
                                //lblMotorError.Background = Brushes.Red;
                                //lblMotorError.Content = "1";

                                //ShowLabelData("1", lblMotorError, Brushes.White, Brushes.Red);
                                //MarkControll.SetMotorErrorStatus(true);
                                //ShowLog((byte)LOGTYPE.LOG_SUCCESS, "MARKING", "MOTOR ERROR!!!!");
                                if (this.CheckAccess() == true)
                                {
                                    recvarg = await EmissionOFF();
                                    //recvarg = await laserSource.StopEmission();
                                    //if ((recvarg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvarg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    //{
                                    //    recvarg = await laserSource.StopEmission();
                                    //    if ((recvarg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvarg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    //        recvarg = await laserSource.StopEmission();
                                    //}
                                }
                                else
                                {
                                    recvarg = await this.Dispatcher.Invoke(new Func<Task<ITNTResponseArgs>>(async delegate
                                    {
                                        ITNTResponseArgs ret = new ITNTResponseArgs();

                                        ret = await EmissionOFF();
                                        //ret = await laserSource.StopEmission();
                                        //if ((ret.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (ret.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                        //{
                                        //    ret = await laserSource.StopEmission();
                                        //    if ((ret.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (ret.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                        //        ret = await laserSource.StopEmission();
                                        //}
                                        return ret;
                                    }));
                                }
                            }
                            else
                            {
                                //motorErrorFlag = 0;
                                //lblMotorError.Background = Brushes.Green;
                                //lblMotorError.Content = "0";
                                //ShowLabelData("0", lblMotorError, Brushes.White, Brushes.Green);
                                //MarkControll.SetMotorErrorStatus(false);
                            }
                        }
                        break;

                    case 0x32:      //run ok
                        break;

                    //case 0x33:      //home ok
                    //    break;
                    //case 0x34:      //jog ok
                    //    break;
                    //case 0x35:      //test ok
                    //    break;
                    //case 0x36:      //go ok
                    //    break;
                    case 0x37:      //cold boot
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnLaserMarkControllerEventFunc", "COLD BOOT", Thread.CurrentThread.ManagedThreadId);

                        Thread coldBootThread = new Thread(new ParameterizedThreadStart(ColdBootThreadFunc));
                        coldBootThread.Start(0);

                        ////Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref headType, Constants.PARAMS_INI_FILE);
                        //Thread.Sleep(2000);
                        //bControllerInitFlag = 0;

                        //if (bHeadType == 0)
                        //    retval = InitializeController().Result.execResult;
                        //else
                        //    retval = InitializeControllerLaser().Result.execResult;

                        ////retval = InitializeController().Result.execResult;
                        //if (retval == 0)
                        //{
                        //    doingCommand = true;
                        //    Stopwatch sw = new Stopwatch();
                        //    sw.Start();
                        //    while (sw.Elapsed < TimeSpan.FromSeconds(6))
                        //    {
                        //        if (!doingCommand)
                        //            break;

                        //        await Task.Delay(50);
                        //    }
                        //    bControllerInitFlag = 1;
                        //}
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnLaserMarkControllerEventFunc", "COLD BOOT END", Thread.CurrentThread.ManagedThreadId);
                        break;

                    case 0x38:
                        //count = 0;
                        double dvalue = 0.0d;
                        double.TryParse(param1, out dvalue);
                        currentPoint.X = dvalue;
                        double.TryParse(param1, out dvalue);
                        currentPoint.Y = dvalue;

                        doingCommand = false;
                        if ((currCMD == 'U') && (m_currCMD == 'U'))
                        {
                            //ITNTJobLog.Instance.Trace(0, "[4] : RECEIVE SCAN COMPLETE");
                            Util.GetPrivateProfileValue("OPTION", "VISIONQUICKEND", "0", ref value, Constants.PARAMS_INI_FILE);
                            if (value != "0")
                                recvarg = await plcComm.SendScanComplete(1);
                        }

                        if ((iparam4 & 0x8000) != 0)
                        {
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "MARKING", "LASER ERROR!!!!");//, LogLevel.Error);
                            //laserErrorFlag = 1;
                            //lblLaserError.Background = Brushes.Red;
                            //lblLaserError.Content = "1";
                            //ShowLabelData("1", lblLaserError, Brushes.White, Brushes.Red);
                            //MarkControll.SetLaserErrorStatus(true);

                            //ShowButtonColor(btnLaserReset, null, Brushes.Red);
                        }
                        else
                        {
                            //laserErrorFlag = 0;
                            //lblLaserError.Background = Brushes.Green;
                            //lblLaserError.Content = "0";
                            //ShowLabelData("0", lblLaserError, Brushes.White, Brushes.Green);
                            //MarkControll.SetLaserErrorStatus(false);
                            //ShowButtonColor(btnLaserReset, null, Brushes.LightGray);
                        }

                        if ((iparam4 & 0x07) != 0)
                        {
                            ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "MARKING", "MOTOR ERROR!!!!");
                            //motorErrorFlag = 1;
                            //lblMotorError.Background = Brushes.Red;
                            //lblMotorError.Content = "1";
                            //ShowLabelData("1", lblMotorError, Brushes.White, Brushes.Red);
                            //MarkControll.SetMotorErrorStatus(true);
                            if (this.CheckAccess() == true)
                            {
                                recvarg = await EmissionOFF();
                                //recvarg = await laserSource.StopEmission();
                                //if ((recvarg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvarg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                //{
                                //    recvarg = await laserSource.StopEmission();
                                //    if ((recvarg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvarg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                //        recvarg = await laserSource.StopEmission();
                                //}
                            }
                            else
                            {
                                recvarg = await this.Dispatcher.Invoke(new Func<Task<ITNTResponseArgs>>(async delegate
                                {
                                    ITNTResponseArgs ret = new ITNTResponseArgs();

                                    ret = await EmissionOFF();
                                    //ret = await laserSource.StopEmission();
                                    //if ((ret.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (ret.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    //{
                                    //    ret = await laserSource.StopEmission();
                                    //    if ((ret.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (ret.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    //        ret = await laserSource.StopEmission();
                                    //}
                                    return ret;
                                }));
                            }
                        }
                        else
                        {
                            //motorErrorFlag = 0;
                            //lblMotorError.Background = Brushes.Green;
                            //lblMotorError.Content = "0";
                            //ShowLabelData("0", lblMotorError, Brushes.White, Brushes.Green);
                            //MarkControll.SetMotorErrorStatus(false);
                        }
                        break;

                    case (byte)ASCII.ACK:
                        if (e.receiveSize < 10)
                            break;

                        if (m_currCMD == '@')
                        {
                            string m_font = "";


                            if ((iparam4 & 0x8000) != 0)
                            {
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "MARKING", "LASER ERROR!!!!");
                                //laserErrorFlag = 1;
                                //lblLaserError.Background = Brushes.Red;
                                //lblLaserError.Content = "1";
                                //ShowButtonColor(btnLaserReset, null, Brushes.Red);
                                //ShowLabelData("1", lblLaserError, Brushes.White, Brushes.Red);
                                //MarkControll.SetLaserErrorStatus(true);
                            }
                            else
                            {
                                //laserErrorFlag = 0;
                                //ShowButtonColor(btnLaserReset, null, Brushes.LightGray);
                                //lblLaserError.Background = Brushes.Green;
                                //lblLaserError.Content = "0";
                                //ShowLabelData("0", lblLaserError, Brushes.White, Brushes.Green);
                                //MarkControll.SetLaserErrorStatus(false);
                            }

                            if ((iparam4 & 0x07) != 0)
                            {
                                //motorErrorFlag = 1;
                                ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "MARKING", "MOTOR ERROR!!!!");
                                //lblMotorError.Background = Brushes.Red;
                                //lblMotorError.Content = "1";
                                //ShowLabelData("1", lblMotorError, Brushes.White, Brushes.Red);
                                //MarkControll.SetMotorErrorStatus(true);

                                if (this.CheckAccess() == true)
                                {
                                    recvarg = await EmissionOFF();
                                    //recvarg = await laserSource.StopEmission();
                                    //if ((recvarg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvarg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    //{
                                    //    recvarg = await laserSource.StopEmission();
                                    //    if ((recvarg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (recvarg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    //        recvarg = await laserSource.StopEmission();
                                    //}
                                }
                                else
                                {
                                    recvarg = await this.Dispatcher.Invoke(new Func<Task<ITNTResponseArgs>>(async delegate
                                    {
                                        ITNTResponseArgs ret = new ITNTResponseArgs();

                                        recvarg = await EmissionOFF();
                                        //ret = await laserSource.StopEmission();
                                        //if ((ret.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (ret.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                        //{
                                        //    ret = await laserSource.StopEmission();
                                        //    if ((ret.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (ret.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                        //        ret = await laserSource.StopEmission();
                                        //}
                                        return ret;
                                    }));
                                }
                            }
                            else
                            {
                                //motorErrorFlag = 1;
                                //lblMotorError.Background = Brushes.Green;
                                //lblMotorError.Content = "0";
                                //ShowLabelData("0", lblMotorError, Brushes.White, Brushes.Green);
                                //MarkControll.SetMotorErrorStatus(false);
                            }
                        }
                        break;
                    case 0x39:      //emergency
                        break;

                    default:
                        break;
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<int> ChangeDBProcess(/*string vin, string seq*/)
        {
            int retval = 0;
            string value = "";
            //string oldtbName = "";
            Stopwatch sw = new Stopwatch();
            string newtbName = "";
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbMainDataTable = new DataTable();
            object obj = new object();
            int count = 0;
            int totcount = 0;
            string dbdisplay = "";

            //Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref tableName, Constants.PARAMS_INI_FILE);
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ChangeDBProcess", "START", Thread.CurrentThread.ManagedThreadId);
                changeDBProcessFlag = true;

                sw.Start();
                if (tableName == "plantable")
                    newtbName = "plantable2";
                else
                    newtbName = "plantable";

                dbwrap.ExecuteCommand(Constants.connstring, "SELECT COUNT(*) FROM " + newtbName, CommandMode.Scalar, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                int newcount = (int)(long)obj;
                if (newcount <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ChangeDBProcess", "NEW TABLE DATA NONE", Thread.CurrentThread.ManagedThreadId);
                    changeDBProcessFlag = false;
                    return -1;
                }

                tableName = newtbName;

                dbdisplay = MakeDBDisplayText(tableName);

                //Util.GetPrivateProfileValue("OPTION", "SHOWDBTEXT", "0", ref value, Constants.PARAMS_INI_FILE);
                //if (value == "1")
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY SEQUENCE ASC, DATE(PRODUCTDATE) ASC";
                //else if (value == "2")
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY NO ASC";
                //else
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";

                ShowMarkingDataList(true, false);
                GetLastMarkedData(dgdPlanData);
                ShowMarkingDataList(true, false);

                //int count = GetMarkPlanDataCount2(dgdPlanData);
                (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                string datetime = DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss");
                ShowWorkPlanCount(lblWorkPlanDataCount, count);
                ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                CheckPlanDataCountWarning(count, lblPlanDataWarning);
                ScrollViewToPoint(dgdPlanData);

                //ShowWorkPlanCountNTime(lblWorkPlanDataCount, lblVINLastUpdateDate, count, datetime, false);
                //DBClear(oldtbName);

                mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_IDLE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);
                Util.WritePrivateProfileValue("OPTION", "TABLENAME", tableName, Constants.PARAMS_INI_FILE);
                sw.Stop();

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ChangeDBProcess", "END - TIME : " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                changeDBProcessFlag = false;
                retval = ex.HResult;
            }
            changeDBProcessFlag = false;
            return retval;
        }

        delegate void delfuncShowMarkingOneLine(int xcharIndex, int fontIndex);

        public void DelegateShowMarkingOneLine(int xcharIndex, int fontIndex)
        {
            if (this.CheckAccess())
            {
                ShowMarkingOneLine(xcharIndex, fontIndex);
            }
            else
            {
                this.Dispatcher.Invoke(new delfuncShowMarkingOneLine(ShowMarkingOneLine), xcharIndex, fontIndex);
            }
        }

        private void ShowMarkingOneLine(int xcharIndex, int fontIndex)//, Canvas showcanvas)//, int sensor)
        {
            Brush brush;

            try
            {
                if (!m_bDoingMarkingFlag)
                {
                    return;
                }

                if(bHeadType != 0)
                {
                    ShowMarkingOneLineLaser(xcharIndex, fontIndex);
                    return;
                }
                brush = new SolidColorBrush(lineColor);

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowMarkingOneLine", "START", Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowMarkingOneLine", string.Format("INDEX : CH = {0}, FT = {1}", xcharIndex, fontIndex), Thread.CurrentThread.ManagedThreadId);

                List<FontDataClass> fdata = new List<FontDataClass>();
                fdata = currMarkInfo.currMarkData.fontData[xcharIndex];
                FontDataClass font = fdata[fontIndex];
                Canvas showcanvas = new Canvas();
                string name = string.Format("cvsshowChar{0:D2}", xcharIndex);
                showcanvas = (Canvas)FindName(name);
                if (showcanvas == null)
                    return;

                double canvaswidth = showcanvas.Width;
                double canvasheight = showcanvas.Height;
                double OriginX = 1.5d * Util.PXPERMM;
                double OriginY = 2.5d * Util.PXPERMM;
                double orgWidth = (currMarkInfo.currMarkData.pattern.fontValue.width) * Util.PXPERMM + OriginX * 2;
                double orgHeight = Util.PXPERMM * currMarkInfo.currMarkData.pattern.fontValue.height + OriginY * 2;
                /***********************************
                1 inch  25.4mm
                1 inch  72 pt
                1 inch  96 px        dpi
                1 mm    2.83465 pt
                1 mm    3.7795 px    dpi/ 25.4
                ***********************************/
                double CharHeight = currMarkInfo.currMarkData.pattern.fontValue.height * Util.PXPERMM;
                double CharWidth = currMarkInfo.currMarkData.pattern.fontValue.width * Util.PXPERMM;
                double CharThick = currMarkInfo.currMarkData.pattern.fontValue.thickness * Util.PXPERMM * ((canvaswidth / orgWidth) + 0.2);

                double heightthRatio = canvasheight / orgHeight;
                double widthRatio = canvaswidth / orgWidth;

                if (font.Flag == 1)
                {
                    //(showcanvas.Parent as Canvas).Children.Clear();
                    charline = new System.Windows.Shapes.Line();
                    charline.Stroke = brush;
                    charline.StrokeThickness = CharThick;
                    charline.StrokeStartLineCap = PenLineCap.Round;
                    charline.StrokeEndLineCap = PenLineCap.Round;
                    charline.StrokeLineJoin = PenLineJoin.Round;

                    charline.X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                    charline.Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;
                }
                else if (font.Flag == 2)
                {
                    if (charline != null)
                    {
                        charline.X2 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                        charline.Y2 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;

                        showcanvas.Children.Add(charline);
                    }
                    //(showcanvas.Parent as Canvas).Children.Clear();
                    charline = new System.Windows.Shapes.Line();
                    charline.Stroke = brush;
                    charline.StrokeThickness = CharThick;
                    charline.StrokeStartLineCap = PenLineCap.Round;
                    charline.StrokeEndLineCap = PenLineCap.Round;
                    charline.StrokeLineJoin = PenLineJoin.Round;

                    charline.X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                    charline.Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;
                }
                else if (font.Flag == 4)
                {
                    if (charline != null)
                    {
                        charline.X2 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                        charline.Y2 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;
                        showcanvas.Children.Add(charline);
                    }
                }
                else
                {
                }
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowMarkingOneLine", "END", Thread.CurrentThread.ManagedThreadId);
                return;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowMarkingOneLine", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        public void ShowLaserMarkingOneLine(int xcharIndex, int fontIndex)
        {
            string className = "MainWindow";
            string funcName = "ShowLaserMarkingOneLine";

            Canvas showcanvas = new Canvas();
            string name = "";
            double canvaswidth = 0;
            double canvasheight = 0;
            double OriginX = 0;
            double OriginY = 0;
            double orgWidth = 0;
            double orgHeight = 0;
            double CharHeight = 0;
            double CharWidth = 0;
            double CharThick = 0;
            double heightRatio = 0;
            double widthRatio = 0;
            int Dotsize = 5;
            List<FontDataClass> fdata = new List<FontDataClass>();
            FontDataClass font;
            double left, right;//, top, bottom;
            Canvas[] showcanvas1 = new Canvas[Constants.MAX_VIN_NO_SIZE];

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                showcanvas = new Canvas();
                name = string.Format("cvsshowChar{0:D2}", xcharIndex);
                showcanvas = (Canvas)FindName(name);
                if (showcanvas == null)
                    return;

                for (int i = 0; i < Constants.MAX_VIN_NO_SIZE; i++)
                    showcanvas1[i] = new Canvas();

                canvaswidth = showcanvas.Width;
                canvasheight = showcanvas.Height;
                OriginX = 1.5d * Util.PXPERMM;// 1.5D
                OriginY = 2.5d * Util.PXPERMM;//2.5D
                orgWidth = (currMarkInfo.currMarkData.pattern.fontValue.width) * Util.PXPERMM + OriginX * 2;
                orgHeight = Util.PXPERMM * currMarkInfo.currMarkData.pattern.fontValue.height + OriginY * 2;

                CharHeight = currMarkInfo.currMarkData.pattern.fontValue.height * Util.PXPERMM;
                CharWidth = currMarkInfo.currMarkData.pattern.fontValue.width * Util.PXPERMM;
                CharThick = currMarkInfo.currMarkData.pattern.fontValue.thickness * Util.PXPERMM * canvaswidth / orgWidth;
                heightRatio = canvasheight / orgHeight;
                widthRatio = canvaswidth / orgWidth;
                Dotsize = 5;
                //Debug.WriteLine(String.Format("EVENT-({0},{1})", xcharIndex, fontIndex));

                switch (currMarkInfo.currMarkData.pattern.laserValue.density)
                {
                    case 0:
                        fdata = currMarkInfo.currMarkData.fontData[xcharIndex].ToList();
                        font = (FontDataClass)fdata[fontIndex].Clone();

                        if (font.Flag == 1 || font.Flag == 2 || font.Flag == 3 || font.Flag == 4 || font.Flag == 5)
                        {
                            Ellipse dotline = new Ellipse();

                            dotline = new Ellipse();
                            dotline.Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.Black;
                            dotline.StrokeThickness = CharThick;
                            dotline.Height = (double)Dotsize;
                            dotline.Width = (double)Dotsize;
                            dotline.Fill = Brushes.Red;
                            dotline.Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio - (double)Dotsize / 2.0, 0, 0);
                            //dotline.Margin = new Thickness(left, right, 0, 0);

                            Canvas.SetZIndex(dotline, (int)(CharThick + 0.5));
                            showcanvas.Children.Add(dotline);
                            ////showcanvas.Children.Add(img);

                            //Dotline = new Ellipse
                            //{
                            //    Stroke = Brushes.Red,
                            //    StrokeThickness = CharThick,
                            //    Height = (double)Dotsize,
                            //    Width = (double)Dotsize,
                            //    Fill = Brushes.Red,
                            //    Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio - (double)Dotsize / 2.0, 0, 0)
                            //};
                            //Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                            //////showcanvas.Children.Add(img);
                            //showcanvas.Children.Add(Dotline);
                            //////showcanvas.Children.Remove(img);
                        }
                        break;

                    case 1:
                        for (int v = 0; v < currMarkInfo.currMarkData.mesData.markvin.Length; v++)
                        {
                            string names = string.Format("cvsshowChar{0:D2}", v);
                            if (showcanvas1.Length <= v)
                                continue;
                            showcanvas1[v] = (Canvas)FindName(names);
                            if (showcanvas1[v] == null)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:00}:{1}()  {2}", className, funcName, string.Format("CANVAS IS NULL ({0})", v), Thread.CurrentThread.ManagedThreadId);
                                //Debug.WriteLine(string.Format("CANVAS IS NULL ({0})", v));
                                continue;
                            }
                            //img.Source = new BitmapImage(new Uri(@"C:\ITNTLASER\ITNTMARK_CONFIG\laser_beam.png"));
                            //img.Height = 10.0; img.Width = 10.0;

                            for (int x = 0; x < currMarkInfo.currMarkData.fontDot.GetLength(1); x++)
                            {
                                FontDataClass fontdot = (FontDataClass)currMarkInfo.currMarkData.fontDot[v, x, fontIndex].Clone();
                                if (fontdot.Flag != 0)
                                {
                                    //Debug.WriteLine(String.Format("MARK-({0}:{1},{2})", fontIndex, fontdot.vector3d.X, fontdot.vector3d.Y));
                                    ////Canvas.SetLeft(img, ((OriginX + (font.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - img.Height / 2));
                                    ////Canvas.SetTop(img, (OriginY + (font.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - img.Width / 2);
                                    left = (OriginX + (fontdot.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0;
                                    right = (OriginY + (fontdot.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio - (double)Dotsize / 2.0;
                                    //top = 0;
                                    //bottom = 0;
                                    //Debug.WriteLine(String.Format("DISP-({3}/{0}:{1},{2}:{4}.{5})", fontIndex, fontdot.vector3d.X, fontdot.vector3d.Y, xcharIndex, left, right));
                                    //Dotline = new Ellipse
                                    //{
                                    //    Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen,
                                    //    StrokeThickness = CharThick,
                                    //    Height = (double)Dotsize,
                                    //    Width = (double)Dotsize,
                                    //    Fill = Brushes.Red,
                                    //    Margin = new Thickness(left, right, 0, 0)
                                    //};

                                    Ellipse dotline = new Ellipse();
                                    dotline = new Ellipse();
                                    dotline.Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen;
                                    dotline.StrokeThickness = CharThick;
                                    dotline.Height = (double)Dotsize;
                                    dotline.Width = (double)Dotsize;
                                    dotline.Fill = Brushes.Red;
                                    dotline.Margin = new Thickness(left, right, 0, 0);

                                    Canvas.SetZIndex(dotline, (int)(CharThick + 0.5));
                                    ////showcanvas.Children.Add(img);
                                    showcanvas1[v].Children.Add(dotline);


                                    //Dotline = new Ellipse();
                                    //Dotline.Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen;
                                    //Dotline.StrokeThickness = CharThick;
                                    //Dotline.Height = (double)Dotsize;
                                    //Dotline.Width = (double)Dotsize;
                                    //Dotline.Fill = Brushes.Red;
                                    //Dotline.Margin = new Thickness(left, right, 0, 0);

                                    //Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                                    //////showcanvas.Children.Add(img);
                                    //showcanvas1.Children.Add(Dotline);
                                    //////showcanvas.Children.Remove(img);
                                }
                            }
                        }
                        break;

                    default:
                        fdata = currMarkInfo.currMarkData.fontData[xcharIndex].ToList();
                        font = (FontDataClass)fdata[fontIndex].Clone();
                        if (font.Flag == 1)
                        {
                            // (showcanvas.Parent as Canvas).Children.Clear();
                            charline = new System.Windows.Shapes.Line
                            {
                                Stroke = Brushes.Red,
                                StrokeThickness = CharThick,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round,
                                X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio,
                                Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio
                            };
                            charline.X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                            charline.Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio;
                        }
                        else if (font.Flag == 2 || font.Flag == 3 || font.Flag == 5)
                        {
                            if (charline != null)
                            {
                                charline.X2 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                                charline.Y2 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio;

                                showcanvas.Children.Add(charline);

                                charline = new System.Windows.Shapes.Line
                                {
                                    Stroke = Brushes.Red,
                                    StrokeThickness = CharThick,
                                    StrokeStartLineCap = PenLineCap.Round,
                                    StrokeEndLineCap = PenLineCap.Round,
                                    StrokeLineJoin = PenLineJoin.Round,
                                    X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio,
                                    Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio,
                                };
                                charline.X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                                charline.Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio;
                            }
                        }
                        else if (font.Flag == 4)
                        {
                            if (charline != null)
                            {
                                charline.X2 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                                charline.Y2 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio;
                                showcanvas.Children.Add(charline);
                            }
                        }
                        else
                        {

                        }
                        break;
                }
                return;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        public void ShowMarkingOneLineLaser(int xcharIndex, int fontIndex)
        {
            string className = "MainWinow";
            string funcName = "ShowMarkingOneLineLaser";

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                Canvas showcanvas = new Canvas();

                if(xcharIndex < 0 || fontIndex < 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("INVALID INDEX - CH : {0}/{1}", xcharIndex, fontIndex), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                string name = string.Format("cvsshowChar{0:D2}", xcharIndex);
                showcanvas = (Canvas)FindName(name);
                if (showcanvas == null)
                    return;

                double canvaswidth = showcanvas.ActualWidth;
                double canvasheight = showcanvas.ActualHeight;
                double OriginX = 1.5d * Util.PXPERMM;// 1.5D
                double OriginY = 2.5d * Util.PXPERMM;//2.5D
                double orgWidth = (currMarkInfo.currMarkData.pattern.fontValue.width) * Util.PXPERMM + OriginX * 2;
                double orgHeight = Util.PXPERMM * currMarkInfo.currMarkData.pattern.fontValue.height + OriginY * 2;

                /***********************************
                1 inch  25.4mm
                1 inch  72 pt
                1 inch  96 px        dpi
                1 mm    2.83465 pt
                1 mm    3.7795 px    dpi/ 25.4
                ***********************************/
                double CharHeight = currMarkInfo.currMarkData.pattern.fontValue.height * Util.PXPERMM;
                double CharWidth = currMarkInfo.currMarkData.pattern.fontValue.width * Util.PXPERMM;
                double CharThick = currMarkInfo.currMarkData.pattern.fontValue.thickness * Util.PXPERMM * canvaswidth / orgWidth;
                double heightthRatio = canvasheight / orgHeight;
                double widthRatio = canvaswidth / orgWidth;
                int Dotsize = 5;
                //Image img = new Image();

                List<FontDataClass> fdata = new List<FontDataClass>();
                FontDataClass font;

                switch (currMarkInfo.currMarkData.pattern.laserValue.density)
                {
                    case 0://only dot
                        fdata = currMarkInfo.currMarkData.fontData[xcharIndex];
                        font = fdata[fontIndex];

                        if (font.Flag == 1 || font.Flag == 2 || font.Flag == 3 || font.Flag == 4 || font.Flag == 5)
                        {
                            //img.Source = new BitmapImage(new Uri(@"C:\ITNTLASER\ITNTMARK_CONFIG\laser_beam.png"));
                            //img.Height = 10.0;
                            //img.Width = 10.0;
                            ////Canvas.SetLeft(img, ((OriginX + (font.X * CharWidth) / currMarkInfoLaser.fontSizeX) * widthRatio - img.Height / 2));
                            ////Canvas.SetTop(img, (OriginY + (font.Y * CharHeight) / currMarkInfoLaser.fontSizeY) * heightthRatio - img.Width / 2);
                            ///
                            Ellipse dotline = new Ellipse();
                            //Dotline = new Ellipse
                            //{
                            //    Stroke = Brushes.Red,
                            //    StrokeThickness = CharThick,
                            //    Height = (double)Dotsize,
                            //    Width = (double)Dotsize,
                            //    Fill = Brushes.Red,
                            //    Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0)
                            //};

                            dotline.Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen;
                            //dotline.Stroke = Brushes.Red;
                            dotline.StrokeThickness = CharThick;
                            dotline.Height = (double)Dotsize;
                            dotline.Width = (double)Dotsize;
                            dotline.Fill = Brushes.Red;
                            dotline.Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0);

                            Canvas.SetZIndex(dotline, (int)(CharThick + 0.5));
                            ////showcanvas.Children.Add(img);
                            showcanvas.Children.Add(dotline);
                            ////showcanvas.Children.Remove(img);


                            //Dotline = new Ellipse
                            //{
                            //    Stroke = Brushes.Red,
                            //    StrokeThickness = CharThick,
                            //    Height = (double)Dotsize,
                            //    Width = (double)Dotsize,
                            //    Fill = Brushes.Red,
                            //    Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0)
                            //};
                            //Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                            //////showcanvas.Children.Add(img);
                            //showcanvas.Children.Add(Dotline);
                            //////showcanvas.Children.Remove(img);
                        }
                        break;

                    case 1: //dot with one line
                        for (int v = 0; v < currMarkInfo.currMarkData.mesData.markvin.Length; v++)
                        {
                            Canvas showcanvas1 = new Canvas();
                            string names = string.Format("cvsshowChar{0:D2}", v);
                            showcanvas1 = (Canvas)FindName(names);
                            if (showcanvas1 == null)
                                return;
                            //img.Source = new BitmapImage(new Uri(@"C:\ITNTLASER\ITNTMARK_CONFIG\laser_beam.png"));
                            //img.Height = 10.0; img.Width = 10.0;

                            for (int x = 0; x < currMarkInfo.currMarkData.fontDot.GetLength(1); x++)
                            {
                                if (fontIndex > currMarkInfo.currMarkData.fontDot.GetLength(2))
                                {
                                    continue;
                                }
                                FontDataClass fontdot = currMarkInfo.currMarkData.fontDot[v, x, fontIndex];
                                if (fontdot.Flag != 0)
                                {
                                    ////Canvas.SetLeft(img, ((OriginX + (font.X * CharWidth) / currMarkInfoLaser.fontSizeX) * widthRatio - img.Height / 2));
                                    ////Canvas.SetTop(img, (OriginY + (font.Y * CharHeight) / currMarkInfoLaser.fontSizeY) * heightthRatio - img.Width / 2);
                                    ///

                                    Ellipse dotline = new Ellipse();
                                    //dotline.Stroke = Brushes.Red;
                                    //dotline.Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen;
                                    dotline.Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightBlue;
                                    dotline.StrokeThickness = CharThick;
                                    dotline.Height = (double)Dotsize;
                                    dotline.Width = (double)Dotsize;
                                    dotline.Fill = Brushes.Red;
                                    dotline.Margin = new Thickness((OriginX + (fontdot.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (fontdot.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0);
                                    Canvas.SetZIndex(dotline, (int)(CharThick + 0.5));
                                    showcanvas1.Children.Add(dotline);

                                    //dotline.Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0);


                                    //Dotline = new Ellipse
                                    //{
                                    //    Stroke = Brushes.Red,
                                    //    StrokeThickness = CharThick,
                                    //    Height = (double)Dotsize,
                                    //    Width = (double)Dotsize,
                                    //    Fill = Brushes.Red,
                                    //    Margin = new Thickness((OriginX + (fontdot.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (fontdot.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0)
                                    //};
                                    //Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                                    //////showcanvas.Children.Add(img);
                                    //showcanvas1.Children.Add(Dotline);
                                    //////showcanvas.Children.Remove(img);
                                }
                            }
                        }
                        break;

                    default:
                        fdata = currMarkInfo.currMarkData.fontData[xcharIndex];
                        font = fdata[fontIndex];
                        if (font.Flag == 1)
                        {
                            // (showcanvas.Parent as Canvas).Children.Clear();
                            charline = new System.Windows.Shapes.Line
                            {
                                Stroke = Brushes.Red,
                                StrokeThickness = CharThick,
                                StrokeStartLineCap = PenLineCap.Round,
                                StrokeEndLineCap = PenLineCap.Round,
                                StrokeLineJoin = PenLineJoin.Round,
                                X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio,
                                Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio
                            };
                            charline.X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                            charline.Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;
                        }
                        else if (font.Flag == 2 || font.Flag == 3 || font.Flag == 5)
                        {
                            if (charline != null)
                            {
                                charline.X2 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                                charline.Y2 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;

                                showcanvas.Children.Add(charline);

                                charline = new System.Windows.Shapes.Line
                                {

                                    Stroke = Brushes.Red,
                                    StrokeThickness = CharThick,
                                    StrokeStartLineCap = PenLineCap.Round,
                                    StrokeEndLineCap = PenLineCap.Round,
                                    StrokeLineJoin = PenLineJoin.Round,
                                    X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio,
                                    Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio,

                                };
                                charline.X1 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                                charline.Y1 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;
                            }
                        }
                        else if (font.Flag == 4)
                        {
                            if (charline != null)
                            {
                                charline.X2 = (OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio;
                                charline.Y2 = (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio;
                                showcanvas.Children.Add(charline);
                            }
                        }

                        else
                        {

                        }
                        break;

                }
                return;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:00}:{1}()  {2}", "MainWindow", "ShowMarkingOneLineLaser", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }


        public int GetVinCharacterFontDot(string vin, List<List<FontDataClass>> fontdata, double fontsizeX, double fontsizeY, double shiftVal, string fontName)
        {
            string className = "MainWindow";
            string funcName = "GetVinCharacterFontDot";
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int a, b, c;
            FontDataClass fd = new FontDataClass();

            try
            {
                currMarkInfo.currMarkData.fontDot = new FontDataClass[vin.Length, (int)fontsizeX, (int)fontsizeY];
                for (int k = 0; k < vin.Length; k++)
                    for (int j = 0; j < fontsizeX; j++)
                        for (int l = 0; l < fontsizeY; l++)
                            currMarkInfo.currMarkData.fontDot[k, j, l] = new FontDataClass();

                for (int vi = 0; vi < vin.Length; vi++)
                {
                    List<FontDataClass> linefont = new List<FontDataClass>();
                    linefont = fontdata[vi].ToList();

                    if (linefont.Count() > 0)
                    {
                        for (int i = 0; i < linefont.Count(); i++)
                        {
                            //FontDataClass fd = new FontDataClass();
                            fd = (FontDataClass)linefont[i].Clone();
                            //point = pointList[i].Split(',');
                            if (fd.Flag >= 0)
                            {
                                a = vi;
                                b = (int)Math.Round(fd.vector3d.X);
                                c = (int)(fontsizeY - 1.0 + shiftVal - Math.Round(fd.vector3d.Y));
                                //c = (int)Math.Round(fd.vector3d.Y);
                                //fd.vector3d.Y = ((double)fontsizeY - 1.0 + (double)shiftVal - fd.vector3d.Y);
                                //fd.vector3d.Z = 0;
                                currMarkInfo.currMarkData.fontDot[a, b, c] = (FontDataClass)fd.Clone();
                                //Debug.WriteLine(String.Format("DOT-({0},{1},{2}):{3}/{4}", a, b, c, fd.vector3d.X, fd.vector3d.Y));
                            }
                        }
                    }
                }
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }


        //private bool isCheckckbVisionErrorType()
        //{
        //    bool bret = false;
        //    if(ckbVisionErrorType.CheckAccess())
        //    {
        //        if (ckbVisionErrorType.IsChecked == true)
        //            bret = true;
        //        else
        //            bret = false;
        //    }
        //    else
        //    {

        //        bret = Dispatcher.Invoke(new Func<bool>(delegate
        //        {
        //            if (ckbVisionErrorType.IsChecked == true)
        //            {
        //                return true;
        //            }
        //            else
        //                return false;
        //        }));
        //    }
        //    return bret;
        //}

        private async Task<ITNTResponseArgs> AnalyzeVisionReceivedData(string msg)
        {
            string className = "MainWindow";
            string funcName = "AnalyzeVisionReceivedData";

            int idx = 0;
            int length = 0;
            string cmd = "";
            string subcmd = "";
            //ITNTSendArgs arg = new ITNTSendArgs();
            ITNTSendArgs args = new ITNTSendArgs();
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            //string sendMsg = "";
            string ErrorCode = "";
            //DataRowView row = null;
            string value = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if(msg.Length < 3)
                {
                    retval = await plcComm.SendVisionResult("N", currMarkInfo.currMarkData.markorderFlag);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV MESSAGE LENGTH ERROR : " + msg.Length.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                idx = 0;
                length = 1;
                cmd = msg.Substring(idx++, length);
                subcmd = msg.Substring(idx++, length);
                switch (cmd)
                {
                    case "R":
                        if (!currMarkInfo.currMarkData.isReady)
                        {
                            currMarkInfo.Initialize();
                            GetMarkDataInfomation(dgdPlanData, ref currMarkInfo.currMarkData.mesData);
                            //currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData);
                            currMarkInfo.currMarkData.pattern.name = GetPatternName(currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.mesData.rawbodytype, currMarkInfo.currMarkData.mesData.rawtrim);

#if MANUAL_MARK
                            ImageProcessManager.GetPatternDataManual(currMarkInfo.currMarkData.patternName, currMarkInfo.currMarkData.mesData.rawcartype, ref currMarkInfo.currMarkData.pattern);
#else
                            ImageProcessManager.GetPatternValue(currMarkInfo.currMarkData.pattern.name, bHeadType, ref currMarkInfo.currMarkData.pattern);
#endif
                            //await ShowCurrentMarkingInformation(-1, currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.mesData.sequence, currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.pattern, 1);
                            await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, null, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 1, 1);

                            //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
                            List<List<FontDataClass>> revData = new List<List<FontDataClass>>();
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

                            retval = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref currMarkInfo.currMarkData.fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref ErrorCode);
                            if (retval.execResult != 0)
                            {

                            }
                            //for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++)
                            //{
                            //    List<FontDataClass> FontDataClass = new List<FontDataClass>();
                            //    ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.currMarkData.pattern.fontValue.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
                            //    currMarkInfo.currMarkData.fontData.Add(fontData);
                            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "Anal", string.Format("FONT DATA {0}CH, {1}PT", i, fontData.Count));
                            //}
                            currMarkInfo.currMarkData.isReady = true;
                        }

                        if (subcmd == "0")
                        {
                            string seq = currMarkInfo.currMarkData.mesData.sequence.PadRight(4, ' ');
                            string rawtype = currMarkInfo.currMarkData.mesData.rawcartype.PadRight(4, ' ');
                            string sendmsg = "M0" + seq + currMarkInfo.currMarkData.mesData.markvin + rawtype + "1";
                            args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
                            args.sendString = sendmsg;
                            args.dataSize = sendmsg.Length;
                            Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                            if ((value == "0") || (value == "2"))
                                await visionServer.SendMessage(args);
                            else
                                await visionClient.SendMessage(args);
                        }
                        break;

                    case "V":
                    case "B":
                        if (subcmd == "0")
                        {
                            length = 1;
                            string result = msg.Substring(idx++, length);
                            //string value = "";
                            Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);

                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV VISION RESULT = {0}", result), Thread.CurrentThread.ManagedThreadId);

                            if (value == "0")
                            {
                                if (result == "O")
                                {
                                    ShowLabelData("VISION OK", lblCheckResult, Brushes.Blue);

                                    if (currMarkInfo.currMarkData.markorderFlag == 2)
                                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "[5-3] VISION FINISH : OK");
                                    else
                                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "[3-3] VISION FINISH : OK");
                                }
                                else
                                {
                                    ShowLabelData("VISION NG", lblCheckResult, Brushes.Red);

                                    if (currMarkInfo.currMarkData.markorderFlag == 2)
                                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "[5-3] VISION FINISH : NG");
                                    else
                                        ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "[3-3] VISION FINISH : NG");
                                }

                                retval = await plcComm.SendVisionResult(result, currMarkInfo.currMarkData.markorderFlag);
                            }
                            else
                            {
                                if (currMarkInfo.currMarkData.markorderFlag == 2)
                                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "[5-3] VISION BYPASS");
                                else
                                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "[3-3] VISION BYPASS");

                                ShowLabelData("VISION BYPASS", lblCheckResult, Brushes.Blue);
                                retval = await plcComm.SendVisionResult("O", currMarkInfo.currMarkData.markorderFlag);
                            }

                            if (retval.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendVisionResult - ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                                ShowErrorMessage("SEND COMPLETE SIGNAL TO PLC ERROR", false);
                                if (currMarkInfo.currMarkData.markorderFlag == 1)
                                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "[5-3] VISION FINISH     PLC : " + result);
                                else
                                    ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "[3-3] VISION FINISH");
                            }

                            ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION);
                            await Task.Delay(300);

#if AGING_TEST
                            //for test
                            Util.WritePrivateProfileValue("VISION_TEST", "RESULT", "", "TEST.ini");
                            Task.Delay(300).Wait();
                            ShowCurrentStateLabel(9);
                            Task.Delay(300).Wait();
                            ShowCurrentStateLabel(0);
                            Task.Delay(500).Wait();
                            Util.WritePrivateProfileValue("PLC", "SIGNAL", "00FF0000", "TEST.ini");
                            Task.Delay(500).Wait();
                            Util.WritePrivateProfileValue("PLC", "SIGNAL", "00FF0001", "TEST.ini");
#endif
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                retval = await plcComm.SendVisionResult("N", currMarkInfo.currMarkData.markorderFlag);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private int OnMESStatusChangedHandler(object sender, ServerStatusChangedEventArgs e)
        {
            int retval = 0;
            SolidColorBrush brush = new SolidColorBrush();
            string status = "";
            try
            {
                if (e.newstatus != e.oldstatus)
                {
                    if (e.newstatus == csConnStatus.Closed)
                    {
                        status = "DISCONNECTED";
                        brush = Brushes.Red;
                    }
                    else if (e.newstatus == csConnStatus.Disconnected)
                    {
                        status = "DISCONNECTED";
                        brush = new SolidColorBrush(Color.FromArgb(255, (byte)225, (byte)225, (byte)0));
                    }
                    //brush = Brushes.Yellow;
                    else if ((e.newstatus == csConnStatus.Connected) || (e.newstatus == csConnStatus.Connecting))
                    {
                        status = "CONNECTED";
                        brush = Brushes.Green;
                    }
                    else
                    {
                        status = "DISCONNECTED";
                        brush = Brushes.Red;
                    }
                    //ShowDeviceStatus(lblMainStatusMESName, brush);
                    ShowLabelData(status, lblMESConnectStatus, backbrush: brush);
                }
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
            }

            return retval;
        }


        private int OnReceiveMESServerDataHandler(object sender, ServerReceivedEventArgs arg)
        {
            string className = "MainWindow";
            string funcName = "OnReceiveMESServerDataHandler";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            string msg = arg.recvMsg;
            int retval = 0;
            MESReceivedData receivedMsg = new MESReceivedData();

            try
            {
                retval = AnalyzeMESReceivedData(msg, ref receivedMsg);
                if (retval != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AnalyzeReceivedServerData return Error " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                retval = SaveMESReceivedData(msg, receivedMsg);
                if (retval == 0)
                {
                    mesServer.SendCommand(arg);
                    ShowMarkingDataList4Thread(true, false);
#if MANUAL_MARK
                    GetCurrentMarkPointData(1);
#endif
                    int count = 0;
                    int totcount = 0;
                    //lock (getCountLock)
                    {
                        //count = GetMarkPlanDataCount4Thread(dgdPlanData);
                        (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                    }

                    string datetime = DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss");
                    ShowWorkPlanCount4Thread(lblWorkPlanDataCount, count);
                    ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                    ShowMESReceivedTime4Thread(lblVINLastUpdateDate, datetime, true);
                    CheckPlanDataCountWarning(count, lblPlanDataWarning);

                    //ShowWorkPlanCountNTime4Thread(lblWorkPlanDataCount, lblVINLastUpdateDate, count, datetime, true);
                }
                else
                {
                    ITNTResponseArgs recvArg = new ITNTResponseArgs();
                    recvArg.errorInfo.sErrorMessage = "MES DATA INVALID";
                    //ITNTErrorCode();
                }
                    //ShowErrorMessage4Thread("MES DATA INVALID", false);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnReceiveMESServerDataHandler", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async Task<int> OnReceiveMESClientDataHandler(object sender, MESClientReceivedEventArgs arg)
        {
            string className = "MainWindow";
            string funcName = "OnReceiveMESClientDataHandler";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;
            ITNTDBWrapper dbwarp = new ITNTDBWrapper();
            MESReceivedData recvData = new MESReceivedData();

            try
            {
                //retval = AnalyzeMESReceivedData(arg.recvMsg, ref recvData);
                //if (retval != 0)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AnalyzeReceivedServerData return Error " + retval.ToString());
                //    return -1;
                //}

                ////mesDataSaveThread = new Thread(MESDataThreadFunc);
                ////mesDataSaveThread.Start();
                //mesDataSaveThread = new Thread(new ParameterizedThreadStart(CCR2WORK2));
                //mesDataSaveThread.Start(dgdPlanData);

                //MESUPDATETHREADArgs e = new MESUPDATETHREADArgs();
                //e.dg = dgdPlanData;
                //e.recvClient = (MESClientReceivedEventArgs)arg.Clone();
                //mesDataSaveThread = new Thread(new ParameterizedThreadStart(CCR2WORK3));
                //mesDataSaveThread.Start(e);

                //mesDataSaveThread = new Thread(new ThreadStart(CCR2WORK3));
                //mesDataSaveThread.Start();
                mesDataSaveThread = new Thread(new ParameterizedThreadStart(CCR2WORK3));
                mesDataSaveThread.Start(1);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnReceiveMESServerDataHandler", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async Task<int> OnReceiveGMESDataHandler(object sender, MESClientReceivedEventArgs arg)
        {
            string className = "MainWindow";
            string funcName = "OnReceiveMESClientDataHandler";
            int retval = 0;
            ITNTDBWrapper dbwarp = new ITNTDBWrapper();
            MESReceivedData recvData = new MESReceivedData();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                mesDataSaveThread = new Thread(new ParameterizedThreadStart(CCR2WORK4GMES));
                mesDataSaveThread.Start(1);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnReceiveMESServerDataHandler", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            return retval;
        }

        //private void ShowWorkPlanCount()
        //{
        //    try
        //    {
        //        lock (PlanDBLock)
        //        {
        //            if (lblWorkPlanDataCount.CheckAccess())
        //            {
        //                lblWorkPlanDataCount.Content = dgdPlanData.Items.Count.ToString();
        //            }
        //            else
        //            {
        //                lblWorkPlanDataCount.Dispatcher.Invoke(new Action(delegate
        //                {
        //                    lblWorkPlanDataCount.Content = dgdPlanData.Items.Count.ToString();
        //                }));
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        private async Task<DataRow> GetCurrentMarkPointData(byte setFlag)
        {
            string className = "MainWindow";
            string funcName = "GetCurrentMarkPointData";

            DataRow row = null;
            DataTable dbMainDataTable = new DataTable();
            //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            ITNTSendArgs sendarg = new ITNTSendArgs();
            string value = "";
            DataRowView rowview;
            string vin = "";
            string seq = "";
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            int iret = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                //int comptype = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                //if (comptype == 1)
                //{
                //    recvarg = await plcComm.ReadPLCSequence();
                //    if (recvarg.execResult == 0)
                //    {
                //        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock1");
                //        if (recvarg.recvString.Length < 8)
                //        {
                //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC STRING(" + recvarg.recvString.Length.ToString() + ") : " + recvarg.recvString, Thread.CurrentThread.ManagedThreadId);
                //            return row;
                //        }

                //        value = recvarg.recvString.Substring(4, 4);
                //        //lock (DBLock)
                //        {
                //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED1", Thread.CurrentThread.ManagedThreadId);
                //            iret = dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=0 WHERE CHECKFLAG=1", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                //            iret = dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where SEQUENCE='" + value + "'", CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                //            //db.Open(Constants.connstring);

                //            //db.CommandText = "UPDATE plantable SET CHECKFLAG=0 WHERE CHECKFLAG=1";
                //            //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);

                //            //db.CommandText = "SELECT * from plantable where SEQUENCE='" + value + "'";
                //            //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);

                //            DataRow[] rows = dbMainDataTable.Select();
                //            if (rows.Length > 0)
                //            {
                //                row = rows[0];
                //                vin = row.ItemArray[Constants.DB_NAME_VIN].ToString();

                //                dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE VIN='" + vin + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                //                //db.CommandText = "UPDATE plantable SET CHECKFLAG=1 WHERE VIN='" + vin + "'";
                //                //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                //                //db.Close();

                //                ShowMarkingDataList(true, false);
                //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED1 END", Thread.CurrentThread.ManagedThreadId);
                //                return row;
                //            }
                //            else
                //            {
                //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "There is no data", Thread.CurrentThread.ManagedThreadId);
                //                //db.Close();
                //                return row;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Read PLC ERROR : " + recvarg.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                //        return row;
                //    }
                //}

                //lock (DBLock)
                {
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED11");
                    dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " where CHECKFLAG=1", CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                    //db.Open(Constants.connstring);
                    //db.CommandText = "SELECT * from plantable where CHECKFLAG=1";
                    //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
                    DataRow[] rows = dbMainDataTable.Select();
                    if (rows.Length > 0)
                    {
                        row = rows[0];
                        //db.Close();
                        return row;
                    }
                    //db.Close();
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END11", Thread.CurrentThread.ManagedThreadId);

                //DataRowView rowview;
                //string vin = "";
                foreach (object t in dgdPlanData.Items)
                {
                    rowview = t as DataRowView;
                    if ((rowview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() != "DLT") &&
                        (rowview.Row.ItemArray[Constants.DB_NAME_ISMARK].ToString() != "Y") &&
                        (rowview.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() != "Y") &&
                        (rowview.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() != "N"))
                    {
                        row = rowview.Row;
                        vin = rowview.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                        seq = rowview.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                        if (setFlag != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock", Thread.CurrentThread.ManagedThreadId);
                            //lock (DBLock)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED", Thread.CurrentThread.ManagedThreadId);
                                dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE VIN='" + vin + "' AND SEQUENCE='" + seq + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                                //db.Open(Constants.connstring);
                                //db.CommandText = "UPDATE plantable SET CHECKFLAG=1 WHERE VIN='" + vin + "'";
                                //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                                //db.Close();
                            }
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                        }
                        break;
                    }
                }

                return row;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return row;
            }
        }


        private DataRowView GetNextMarkPointData()
        {
            string value = "";
            int checkvalue = 0;
            DataRowView rowview = null;
            //DataRowView tempview = null;
            bool bFind = false;
            string className = "MainWindow";
            string funcName = "GetNextMarkPointData";

            try
            {
                foreach (DataRowView rv in dgdPlanData.Items)
                {
                    if (bFind == false)
                    {
                        value = rv.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString();
                        Int32.TryParse(value, out checkvalue);
                        if (checkvalue != 0)
                        {
                            bFind = true;
                        }
                    }
                    else
                    {
                        //if ((tempview.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() == "N") &&
                        //    (tempview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() == "N") &&
                        //    (tempview.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() == "Y"))
                        if ((rv.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() != "DLT") &&
                            (rv.Row.ItemArray[Constants.DB_NAME_ISMARK].ToString() != "Y") &&
                            (rv.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() != "Y") &&
                            (rv.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() != "N"))
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEQ = " + rv.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString(), Thread.CurrentThread.ManagedThreadId);
                            return rv;
                        }
                    }
                }

                return rowview;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 1: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return rowview;
            }

            //try
            //{
            //    foreach (object t in dgdPlanData.ItemsSource)
            //    {
            //        tempview = t as DataRowView;
            //        if (tempview != null)
            //        {
            //            if (bFind == false)
            //            {
            //                value = tempview.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString();
            //                Int32.TryParse(value, out checkvalue);
            //                if (checkvalue != 0)
            //                {
            //                    bFind = true;
            //                }
            //            }
            //            else
            //            {
            //                //if ((tempview.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() == "N") &&
            //                //    (tempview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() == "N") &&
            //                //    (tempview.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() == "Y"))
            //                if ((tempview.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() != "DLT") &&
            //                    (tempview.Row.ItemArray[Constants.DB_NAME_ISMARK].ToString() != "Y") &&
            //                    (tempview.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() != "Y") &&
            //                    (tempview.Row.ItemArray[Constants.DB_NAME_EXIST].ToString() != "N"))
            //                {
            //                    return tempview;
            //                }
            //            }
            //        }
            //    }

            //    return rowview;
            //}
            //catch (Exception ex)
            //{
            //    return rowview;
            //}
        }


        private DataRowView GetLastMarkedData(DataGrid grid)
        {
            DataRowView rowview = null;
            DataRowView rowview1 = null;
            DataRowView rowvin = null;
            //bool bFind = false;
            bool bFindVIN = false;
            string className = "MainWindow";
            string funcName = "GetLastMarkedData";
            DataTable dbMainDataTable = new DataTable();
            string vin = "";
            string seq = "";
            string vinseq = "";
            string[] vals;
            int i = 0;
            string value = "";
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("CURRENT", "SEQVIN", "", ref vinseq, Constants.DATA_CUR_COMPLETE_FILE);
                vals = vinseq.Split('|');
                if (vals.Length >= 2)
                {
                    vin = vals[1];
                    //if (vals[1].Length == 17)
                    //    vin = " " + vals[1] + " ";
                    //else if (vals[1].Length == 18)
                    //    vin = vals[1] + " ";
                    seq = vals[0];
                }

                if (grid.Items.Count <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NO DATA", Thread.CurrentThread.ManagedThreadId);
                    return rowview;
                }

                foreach (DataRowView rv in grid.Items)
                {
                    if (i == 0)
                        rowview1 = rv;
                    i++;

                    value = rv.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString();
                    if (value != "0")
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK - SEQ = " + rv.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString(), Thread.CurrentThread.ManagedThreadId);
                        return rv;
                        //bFind = true;
                    }

                    if ((rv.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString() == seq) && ((rv.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString()).Contains(vin) == true))
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN SEQ = " + rv.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString(), Thread.CurrentThread.ManagedThreadId);
                        bFindVIN = true;
                        rowvin = rv;
                        //return rv;
                    }
                }

                //if (bFind == false)
                {
                    if (bFindVIN)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END 2 SEQ = " + rowvin.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString(), Thread.CurrentThread.ManagedThreadId);
                        rowview = rowvin;
                        //rowvin.Row.ItemArray[Constants.DB_NAME_CHECKFLAG] = "1";
                        //return rowvin;
                    }
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END 3 SEQ = " + rowview1.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString(), Thread.CurrentThread.ManagedThreadId);
                        rowview = rowview1;
                        //return rowview1;
                    }
                    rowview.Row.ItemArray[Constants.DB_NAME_CHECKFLAG] = "1";
                    vin = rowview.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                    object obj = new object();
                    //string commandstring = "SELECT * from " + ((MainWindow)System.Windows.Application.Current.MainWindow).tableName + " WHERE VIN LIKE '%" + tbxVin.Text + "%' ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
                    //string commandstring = "UPDATE " +tableName + " SET CHECKFLAG=1 WHERE RAWVIN LIKE '%" + vin + "%' ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
                    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE RAWVIN='" + vin + "' AND SEQUENCE='" + seq + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    //dbwrap.ExecuteCommand();
                }

                return rowview;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 1: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return rowview;
            }
        }

        /// <summary>
        /// HMC UL#42
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="retMsg"></param>
        /// <returns></returns>
        private int AnalyzeMESReceivedData_Alogorithm00(string msg, ref MESReceivedData retMsg)
        {
            int idx = 0;
            int length = 0;
            string value = "";
            string tmpVIN = "";
            int retval = -1;
            DateTime dt;
            try
            {
                retMsg.totalmsg = msg;

                //factory
                length = 2;
                if (msg.Length >= (length + idx))
                    retMsg.factory = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //process
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.process = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //product date
                length = 8;
                if (msg.Length >= (length + idx))
                    retMsg.productdate = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //sequence
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.sequence = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //delete
                length = 3;
                if (msg.Length >= (length + idx))
                    retMsg.delete = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //raw car type
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.rawcartype = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                retval--;

                //body no
                length = 6;
                if (msg.Length >= (length + idx))
                    retMsg.bodyno = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //work order
                length = 16;
                if (msg.Length >= (length + idx))
                    retMsg.workorder = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //vin
                length = 17;
                if (msg.Length >= (length + idx))
                    tmpVIN = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //mes date
                length = 8;
                if (msg.Length >= (length + idx))
                    retMsg.mesdate = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                retval--;

                //mes time
                length = 6;
                if (msg.Length >= (length + idx))
                    retMsg.mestime = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //last sequence
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.lastsequence = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //code 219
                length = 219;
                //string code219 = "";
                if (msg.Length >= (length + idx))
                    retMsg.code219 = msg.Substring(idx, length);
                else
                    return retval;
                idx += length;
                idx++;  //space
                retval--;

                //id plate
                length = 3;
                if (msg.Length >= (length + idx))
                    retMsg.idplate = msg.Substring(idx, length);
                else
                    return -13;

                retMsg.rawvin = tmpVIN;
                Util.GetPrivateProfileValue("VINTYPE", "ADDASTREK", "", ref value, Constants.PARAMS_INI_FILE);
                if (value.Contains(retMsg.idplate.Trim()))
                    retMsg.markvin = "*" + tmpVIN + "*";
                else
                    retMsg.markvin = " " + tmpVIN + " ";
                idx += length;
                idx++;  //space

                //body type = 219code s[4]
                retMsg.rawbodytype = retMsg.code219.Substring(3, 1);

                //trim type = 219code s[8]
                retMsg.rawtrim = retMsg.code219.Substring(7, 1);

                //retMsg.region;
                retMsg.bodytype = GetBodyType(retMsg.rawcartype, retMsg.rawbodytype, retMsg.rawtrim, "", 1);
                retMsg.plcvalue = GetPLCValue(retMsg.rawcartype, retMsg.rawbodytype, retMsg.rawtrim, "", 1);

                dt = DateTime.Now;
                retMsg.markdate = dt.ToString("yyyy-MM-dd");    //dt.ToString("yyyy-MM-dd");
                retMsg.marktime = dt.ToString("HH:mm:ss");      //dt.ToString("HH:mm:ss");
                retMsg.remark = "";
                retMsg.exist = "Y";
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return 0;
        }


        /// <summary>
        /// For KaGA
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="retMsg"></param>
        /// <returns></returns>
        private int AnalyzeMESReceivedData_Alogorithm05(string msg, ref MESReceivedData retMsg)
        {
            /*
             *              sSeq = Mid(sBuf, 1, 4)
                            sVin = Mid(sBuf, 19, 19)
                            sCar = Mid(sBuf, 15, 4)
            *
            */

            //<2109303738EY  116031  L 311131513151315131123112    DL3 4LHDAT2WD       NNNN                      5XXG14J27NG116031    NNN.AFOLDN22                   >
            int idx = 0;
            int length = 0;
            string value = "";
            string tmpVIN = "";
            int retval = -1;
            System.DateTime dt;
            try
            {
                retMsg.totalmsg = msg;

                //order date
                length = 6;
                if (msg.Length >= (length + idx))
                    retMsg.productdate = "20" + msg.Substring(idx, length);
                idx += length;

                ////Commit (Body Plan)
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.sequence = msg.Substring(idx, length);
                idx += length;

                ////body type
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.bodytype = msg.Substring(idx, length);
                retMsg.bodytype.Trim();
                idx += length;

                ////bodyno
                length = 6;
                if (msg.Length >= (length + idx))
                    retMsg.bodyno = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 16;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                ////raw car type
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.rawcartype = msg.Substring(idx, length);
                idx += length;

                length = 8;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.cartype = msg.Substring(idx, length);
                idx += length;

                length = 41;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                ////vin
                length = 19;
                if (msg.Length >= (length + idx))
                    retMsg.rawvin = msg.Substring(idx, length);
                idx += length;

                retMsg.markvin = AddMonthCode(retMsg.rawvin);

                dt = System.DateTime.Now;
                retMsg.markdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.marktime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.mesdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.mestime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.remark = "";
                retMsg.exist = "Y";
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return 0;
        }

        /// <summary>
        /// KaGA
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="retMsg"></param>
        /// <returns></returns>
        private int AnalyzeMESReceivedData_Alogorithm01(string msg, int orderLength, ref MESReceivedData retMsg)
        {
            //<9855HY  0796814111 5XYRG4LC1NG079681    >
            int idx = 0;
            int length = 0;
            string value = "";
            System.DateTime dt;
            string tmpstring = "";

            try
            {
                tmpstring = msg.Trim();
                if ((orderLength != 0) && (tmpstring.Length <= orderLength))
                {
                    return -2;
                }

                retMsg.totalmsg = msg;

                //order date
                length = 8;
                if (msg.Length >= (length + idx))
                    retMsg.productdate = msg.Substring(idx, length);
                idx += length;
                idx += 6;

                ////Commit (Body Plan)
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.sequence = msg.Substring(idx, length);
                idx += length;

                ////body type
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.bodytype = msg.Substring(idx, length);
                retMsg.bodytype.Trim();
                idx += length;

                ////bodyno
                length = 6;
                if (msg.Length >= (length + idx))
                    retMsg.bodyno = msg.Substring(idx, length);
                idx += length;

                //length = 4;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;

                //length = 16;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;

                ////raw car type
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.rawcartype = msg.Substring(idx, length).Trim();
                idx += length;

                //value = retMsg.rawcartype.Trim().Substring(0, 1);
                retMsg.cartype = GetCarTypeFromNumber(retMsg.rawcartype);

                //length = 8;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;

                //length = 4;
                //if (msg.Length >= (length + idx))
                //    retMsg.cartype = msg.Substring(idx, length);
                //idx += length;

                //length = 41;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;

                ////vin
                length = 19;
                if (msg.Length >= (length + idx))
                    retMsg.rawvin = msg.Substring(idx, length);
                idx += length;

                retMsg.markvin = AddMonthCode(retMsg.rawvin);

                dt = System.DateTime.Now;
                retMsg.productdate = dt.ToString("yyyy-MM-dd");
                retMsg.markdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.marktime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.mesdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.mestime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.remark = "";
                retMsg.exist = "Y";
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return 0;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="orderLength"></param>
        /// <param name="retMsg"></param>
        /// <returns></returns>
        private int AnalyzeMESReceivedData_Alogorithm02(string msg, int orderLength, ref MESReceivedData retMsg)
        {
            /**     sSeq = Mid(sRecord, 3, 4)
                    sVin = Mid(sRecord, 7, 19)
                    sCar = Mid(sRecord, 26, 1)
                    If Mid(sRecord, 28, 1) = "C" Then
                        sArea = Mid(sRecord, 28, 1)
                    Else
                        sArea = ""
                    End If
            **/

            //<  1341 KMTFB41DDRU041341 S X                                  >
            //<  2031*KMTG3G1AXRU142031*I C                                  >

            int idx = 0;
            int length = 0;
            string value = "";
            //string tmpVIN = "";
            int retval = -1;
            DateTime dt;
            try
            {
                if ((orderLength != 0) && (msg.Length <= orderLength))
                {
                    return -2;
                }

                retMsg.totalmsg = msg;

                //order date
                length = 8;
                if (msg.Length >= (length + idx))
                    retMsg.productdate = msg.Substring(idx, length);
                idx += length;
                idx += 6;

                //
                length = 2;
                //if (msg.Length >= (length + idx))
                //    retMsg.productdate = "20" + msg.Substring(idx, length);
                idx += length;

                ////Sequence - Commit (Body Plan)
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.sequence = msg.Substring(idx, length);
                else
                    return retval;
                retval--;
                idx += length;

                ////vin
                length = 19;
                if (msg.Length >= (length + idx))
                    retMsg.rawvin = msg.Substring(idx, length);
                else
                    return retval;
                retval--;
                idx += length;

                //////raw car type
                length = 2;
                if (msg.Length >= (length + idx))
                {
                    retMsg.rawcartype = msg.Substring(idx, length).Trim();
                    //value = retMsg.rawcartype.Trim().Substring(0, 1);
                    retMsg.cartype = GetCarTypeFromNumber(retMsg.rawcartype);
                }
                else
                    return retval;
                retval--;
                idx += length;

                //////region
                length = 1;
                if (msg.Length >= (length + idx))
                {
                    retMsg.region = msg.Substring(idx, length);
                    retMsg.idplate = retMsg.region;
                }
                else
                    return retval;
                retval--;
                idx += length;

                retMsg.markvin = AddMonthCode(retMsg.markvin);
                ///
                dt = System.DateTime.Now;
                retMsg.productdate = dt.ToString("yyyy-MM-dd");
                retMsg.markdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.marktime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.mesdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.mestime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.remark = "";
                retMsg.exist = "Y";
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return 0;
        }

        private int AnalyzeMESReceivedData_Alogorithm_HMI_12(string msg, int orderLength, ref MESReceivedData retMsg)
        {
            /**
                sSeq = Mid(sBuf, 3, 4)
                sVin = Mid(sBuf, 7, 19)
                sCar = Mid(sBuf, 26, 2)
            **/

            //AL0015MALJC81DMPM012465* DNX
            //AL0016MALJC81DMPM012466* DNX

            int idx = 0;
            int length = 0;
            string value = "";
            //string tmpVIN = "";
            int retval = -1;
            DateTime dt;
            string modivin = "";
            //string rawvin = "";
            string vinyear = "";
            string syear = "";
            //char[] vinchar = new char[19];
            //int iyear = 0;
            string pcyear = "";
            string smonth = "";

            try
            {
                if ((orderLength != 0) && (msg.Length <= orderLength))
                {
                    return -2;
                }

                retMsg.totalmsg = msg;

                //order date
                length = 8;
                if (msg.Length >= (length + idx))
                    retMsg.productdate = msg.Substring(idx, length);
                idx += length;
                idx += 6;

                //
                length = 2;
                //if (msg.Length >= (length + idx))
                //    retMsg.productdate = "20" + msg.Substring(idx, length);
                idx += length;

                ////Sequence - Commit (Body Plan)
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.sequence = msg.Substring(idx, length);
                else
                    return retval;
                retval--;
                idx += length;

                ////vin
                length = 19;
                if (msg.Length >= (length + idx))
                    retMsg.rawvin = msg.Substring(idx, length);
                //retMsg.vin = msg.Substring(idx, length);
                else
                    return retval;
                retval--;
                idx += length;

                //////raw car type
                length = 2;
                if (msg.Length >= (length + idx))
                {
                    retMsg.rawcartype = msg.Substring(idx, length);
                    value = retMsg.rawcartype.Trim().Substring(0, 2);
                    retMsg.cartype = GetCarTypeFromCarName(value, retMsg.rawcartype);
                }
                else
                    return retval;
                retval--;
                idx += length;

                retMsg.markvin = AddMonthCode(retMsg.rawvin);

                ////" MALJC81DMPM012465*"
                //modivin = retMsg.rawvin;
                //if (modivin.Length < 19)
                //    modivin = retMsg.rawvin.PadRight(19, ' ');
                //vinyear = modivin.Substring(10, 1);
                //Util.GetPrivateProfileValue("YEAR", vinyear, "25", ref syear, Constants.PARAMS_INI_FILE);

                //pcyear = DateTime.Now.ToString("yy");

                //if (pcyear != syear)
                //    smonth = "01";
                //else
                //    smonth = DateTime.Now.ToString("MM");

                //Util.GetPrivateProfileValue("MONTH", smonth, "A", ref value, Constants.PARAMS_INI_FILE);

                //retMsg.vin = modivin;
                //if ((modivin.Substring(modivin.Length - 1, 1) == "*") && (modivin.Substring(0, 1) != "*"))
                //{
                //    retMsg.vin = modivin.Substring(1, 17) + "*" + value;
                //}

                ////////region
                //length = 1;
                //if (msg.Length >= (length + idx))
                //{
                //    retMsg.region = msg.Substring(idx, length);
                //    retMsg.idplate = retMsg.region;
                //}
                //else
                //    return retval;
                //retval--;
                //idx += length;

                ///
                dt = System.DateTime.Now;
                retMsg.productdate = dt.ToString("yyyy-MM-dd");
                retMsg.markdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.marktime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.mesdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.mestime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.remark = "";
                retMsg.exist = "Y";
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return 0;
        }

        private int AnalyzeMESReceivedData_Alogorithm_HMI_3(string msg, int orderLength, ref MESReceivedData retMsg, int no2)
        {
            /**
                sSeq = Mid(sBuf, 3, 4)
                sVin = Mid(sBuf, 7, 19)
                sCar = Mid(sBuf, 26, 2)
                sbODY = Mid(sBuf, 28, 10)
            **/

            //AL0015MALJC81DMPM012465* DNX
            //AL0016MALJC81DMPM012466* DNX

            int idx = 0;
            int length = 0;
            string value = "";
            //string tmpVIN = "";
            int retval = -1;
            DateTime dt;
            string modivin = "";
            //string rawvin = "";
            string vinyear = "";
            string syear = "";
            //char[] vinchar = new char[19];
            //int iyear = 0;
            string pcyear = "";
            string smonth = "";

            try
            {
                if ((orderLength != 0) && (msg.Length <= orderLength))
                {
                    return -2;
                }

                retMsg.totalmsg = msg;

                //order date
                length = orderLength;
                if (msg.Length >= (length + idx))
                    retMsg.productdate = msg.Substring(idx, length);
                idx += length;

                //
                length = 2;
                //if (msg.Length >= (length + idx))
                //    retMsg.productdate = "20" + msg.Substring(idx, length);
                idx += length;

                ////Sequence - Commit (Body Plan)
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.sequence = msg.Substring(idx, length);
                else
                    return retval;
                retval--;
                idx += length;

                ////vin
                length = 19;
                if (msg.Length >= (length + idx))
                    retMsg.rawvin = msg.Substring(idx, length);
                //retMsg.vin = msg.Substring(idx, length);
                else
                    return retval;
                retval--;
                idx += length;

                //////raw car type
                length = 2;
                if (msg.Length >= (length + idx))
                {
                    retMsg.rawcartype = msg.Substring(idx, length);
                    value = retMsg.rawcartype.Trim().Substring(0, 2);
                    retMsg.cartype = GetCarTypeFromCarName(value, retMsg.rawcartype);
                }
                else
                    return retval;
                retval--;
                idx += length;

                //////BODY NUMBER
                length = 10;
                if (msg.Length >= (length + idx))
                {
                    retMsg.bodyno = msg.Substring(idx, length);
                }
                else
                    return retval;
                retval--;
                idx += length;

                retMsg.markvin = AddMonthCode(retMsg.rawvin);

                ////" MALJC81DMPM012465*"
                //modivin = retMsg.rawvin;
                //if (modivin.Length < 19)
                //    modivin = retMsg.rawvin.PadRight(19, ' ');
                //vinyear = modivin.Substring(10, 1);
                //Util.GetPrivateProfileValue("YEAR", vinyear, "25", ref syear, Constants.PARAMS_INI_FILE);

                //pcyear = DateTime.Now.ToString("yy");

                //if (pcyear != syear)
                //    smonth = "01";
                //else
                //    smonth = DateTime.Now.ToString("MM");

                //Util.GetPrivateProfileValue("MONTH", smonth, "A", ref value, Constants.PARAMS_INI_FILE);

                //retMsg.vin = modivin;
                //if ((modivin.Substring(modivin.Length - 1, 1) == "*") && (modivin.Substring(0, 1) != "*"))
                //{
                //    retMsg.vin = modivin.Substring(1, 17) + "*" + value;
                //}

                ////////region
                //length = 1;
                //if (msg.Length >= (length + idx))
                //{
                //    retMsg.region = msg.Substring(idx, length);
                //    retMsg.idplate = retMsg.region;
                //}
                //else
                //    return retval;
                //retval--;
                //idx += length;

                ///
                dt = System.DateTime.Now;
                retMsg.productdate = dt.ToString("yyyy-MM-dd");
                retMsg.markdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.marktime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.mesdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.mestime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.remark = "";
                retMsg.exist = "Y";
                retMsg.no2 = no2 * 10;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return 0;
        }

        private int AnalyzeMESReceivedData(string msg, ref MESReceivedData receivedMsg)
        {
            string className = "MainWindow";
            string funcName = "AnalyzeMESReceivedData";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int exeResult = 0;
            MESReceivedData retval = new MESReceivedData();
            int mesAlgoType = 0;

            try
            {
                mesAlgoType = (int)Util.GetPrivateProfileValueUINT("MES", "ALGORITHMTYPE", 0, Constants.PARAMS_INI_FILE);
                if (mesAlgoType == 0)
                    exeResult = AnalyzeMESReceivedData_Alogorithm00(msg, ref receivedMsg);
                else if (mesAlgoType == 1)
                    exeResult = AnalyzeMESReceivedData_Alogorithm01(msg, 0, ref receivedMsg);
                else if (mesAlgoType == 2)
                    exeResult = AnalyzeMESReceivedData_Alogorithm02(msg, 0, ref receivedMsg);
                else
                    exeResult = AnalyzeMESReceivedData_Alogorithm00(msg, ref receivedMsg);
                //else
                //    exeResult = AnalyzeMESReceivedData_Alogorithm01(msg, 0, ref receivedMsg);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 1: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return exeResult;
        }

        private int SaveMESReceivedData(string msg, MESReceivedData args)
        {
            string className = "MainWindow";
            string funcName = "SaveMESReceivedData";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;
            DateTime dt = DateTime.Now;
            string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();

            try
            {
                DataTable dbMainDataTable = new DataTable();
                ////ITNTDBManage db = new ITNTDBManage();

                string searchstring = string.Format("select * from " + tableName + " where PRODUCTDATE='" + args.productdate + "' AND SEQUENCE='" + args.sequence + "'");
                //string searchstring = string.Format("select * from plantable where VIN='" + args.vin + "'");

                string insertstring = "INSERT INTO " + tableName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                  args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.rawvin + "','" + args.mesdate + "','" + args.mestime + "','" +
                  args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
                  args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" + args.isMarked + "','N','" + args.exist + "',0)";

                string updatestring = "UPDATE " + tableName + " set PRODUCTDATE='" + args.productdate + "', SEQUENCE='" + args.sequence + "', RAWCARTYPE='" + args.rawcartype +
                    "', BODYNO='" + args.bodyno + "', MESDATE='" + args.mesdate + "', MESTIME='" + args.mestime + "', LASTSEQ='" + args.lastsequence +
                    "', CODE219='" + args.code219 + "', IDPLATE='" + args.idplate + "', DELETEFLAG='" + args.delete + "', TOTALMSG='" + args.totalmsg + "', RAWBODY='" + args.rawbodytype +
                    "', RAWTRIM='" + args.rawtrim + "', PLCVALUE='" + args.plcvalue + "', REGION='" + args.region + "', BODYTYPE='" + args.bodytype + "', CARTYPE='" + args.cartype +
                    "', MARKDATE='" + args.markdate + "', MARKTIME='" + args.marktime + "', REMARK='" + args.remark + "', ISMARK='" + args.isMarked + "', COMPLETE='N', EXIST='" + args.exist + "', CHECKFLAG=0 " +
                    "where PRODUCTDATE='" + args.productdate + "' AND SEQUENCE='" + args.sequence + "'";

                //string updatestring = "UPDATE plantable set PRODUCTDATE='" + args.productdate + "', SEQUENCE='" + args.sequence + "', RAWCARTYPE='" + args.rawcartype +
                //    "', BODYNO='" + args.bodyno + "', VIN='" + args.vin + "', MESDATE='" + args.mesdate + "', MESTIME='" + args.mestime + "', LASTSEQ='" + args.lastsequence +
                //    "', CODE219='" + args.code219 + "', IDPLATE='" + args.idplate + "', DELETEFLAG='" + args.delete + "', TOTALMSG='" + args.totalmsg + "', RAWBODY='" + args.rawbodytype +
                //    "', RAWTRIM='" + args.rawtrim + "', PLCVALUE='" + args.plcvalue + "', REGION='" + args.region + "', BODYTYPE='" + args.bodytype + "', CARTYPE='" + args.cartype +
                //    "', MARKDATE='" + args.markdate + "', MARKTIME='" + args.marktime + "', REMARK='" + args.remark + "', ISMARK='" + args.isMarked + "', COMPLETE='N', EXIST='" + args.exist + "', CHECKFLAG=0 "+
                //"where PRODUCTDATE='" + args.productdate + "' AND SEQUENCE='" + args.sequence + "'";

                if (args.delete == "DLT")
                {
                    //lock (DBLock)
                    {
                        dbwrap.ExecuteCommand(Constants.connstring, "delete from " + tableName + " where PRODUCTDATE ='" + args.productdate + "' and SEQUENCE = '" + args.sequence + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                        //dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=0, ISMARK='Y', COMPLETE='Y' WHERE PRODUCTDATE ='" + args.productdate + "' and SEQUENCE = '" + args.sequence + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.Open(Constants.connstring);
                        //db.CommandText = "delete from plantable where PRODUCTDATE ='" + args.productdate + "' and SEQUENCE = '" + args.sequence + "'";
                        //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                        //db.Close();
                    }

                    //lock (DeleteDBLock)
                    {
                        string insertstring_delete = "INSERT INTO deletetable (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                          args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.rawvin + "','" + args.mesdate + "','" + args.mestime + "','" +
                          args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
                          args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" +
                          args.isMarked + "','N','" + args.exist + "',0)";

                        dbwrap.ExecuteCommand(Constants.connstring_dele, insertstring_delete, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.Open(Constants.connstring_dele);
                        //db.CommandText = insertstring_delete;
                        //retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                        //db.Close();
                    }
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NEW", Thread.CurrentThread.ManagedThreadId);
                    //lock (DBLock)
                    {
                        ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "OPEN", Thread.CurrentThread.ManagedThreadId);
                        dbwrap.ExecuteCommand(Constants.connstring, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.Open(Constants.connstring);
                        //ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "OPEN END");
                        //db.CommandText = searchstring;
                        //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
                        ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEARCH END", Thread.CurrentThread.ManagedThreadId);
                        if (dbMainDataTable.Rows.Count > 0)
                        {
                            //ITNTJobLog.Instance.Trace(0, msg + "  - (SAME DATA)");
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA ALREADY EXIST : {0} - {1}", args.bodyno, args.productdate), Thread.CurrentThread.ManagedThreadId);

                            dbwrap.ExecuteCommand(Constants.connstring, updatestring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                            //db.CommandText = updatestring;
                            //retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                        }
                        else
                        {
                            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NEW2", Thread.CurrentThread.ManagedThreadId);
                            dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                            //db.CommandText = insertstring;
                            //retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INSERT END", Thread.CurrentThread.ManagedThreadId);
                        }
#if MANUAL_MARK
                        db.CommandText = "SELECT COUNT(*) FROM plantable";
                        object sbscalar = new DataTable();
                        db.ExecuteCommandScalar(CommandTypeEnum.Text, ref sbscalar);
                        int count = (int)(long)sbscalar;

                        int max_count = (int)Util.GetPrivateProfileValueUINT("OPTION", "SHOWPLANCOUNT", 500, Constants.PARAMS_INI_FILE);
                        if(max_count < count)
                        {
                            db.CommandText = "DELETE FROM plantable ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC LIMIT " + (count - max_count).ToString();
                            db.ExecuteCommandScalar(CommandTypeEnum.Text, ref sbscalar);
                        }
#endif
                        //db.Close();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CLOSE END", Thread.CurrentThread.ManagedThreadId);
                    }
                }

                //ShowWorkPlanCountNTime(dgdPlanData.Items.Count, DateTime.Now);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION 2: CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
                retval = de.HResult;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private int SaveMESClientReceivedData(string newtbName, string msg, MESReceivedData args)
        {
            string className = "MainWindow";
            string funcName = "SaveMESClientReceivedData";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;
            DateTime dt = DateTime.Now;
            string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
            DataTable dbMainDataTable = new DataTable();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();

            try
            {
                string insertstring = "INSERT INTO " + newtbName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                  args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.markvin + "','" + args.mesdate + "','" + args.mestime + "','" +
                  args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
                  args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" + args.isMarked + "','N','" + args.exist + "',0)";

                dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                //db.CommandText = insertstring;
                //retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INSERT END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION 2: CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
                retval = de.HResult;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        private int SaveMESClientReceivedData2(string newtbName, /*string msg, */MESReceivedData[] args, int count)
        {
            string className = "MainWindow";
            string funcName = "SaveMESClientReceivedData";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;
            DateTime dt = DateTime.Now;
            string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
            DataTable dbMainDataTable = new DataTable();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            string tmpstring = "";

            try
            {
                //DBClear(newtbName);

                string insertstring = "INSERT INTO " + newtbName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG, VISIONFLAG, RAWVIN, NO2, ISINSERT) VALUES";
                for (int i = 0; i < count; i++)
                {
                    tmpstring = "('" + args[i].productdate + "','" + args[i].sequence + "','" + args[i].rawcartype + "','" + args[i].bodyno + "','" + args[i].markvin + "','" + args[i].mesdate + "','" + args[i].mestime + "','" +
                  args[i].lastsequence + "','" + args[i].code219 + "','" + args[i].idplate + "','" + args[i].delete + "','" + args[i].totalmsg + "','" + args[i].rawbodytype + "','" + args[i].rawtrim + "','" +
                  args[i].plcvalue + "','" + args[i].region + "','" + args[i].bodytype + "','" + args[i].cartype + "','" + args[i].markdate + "','" + args[i].marktime + "','" + args[i].remark + "','" + args[i].isMarked + "','N','" + args[i].exist + "',0,0,'" + args[i].rawvin + "', " + args[i].no2 + ",'" + args[i].isInserted + "')";

                    if (i != count - 1)
                        tmpstring += ",";
                    insertstring += tmpstring;
                }

                dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                retval = 0;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INSERT END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION 2: CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
                retval = de.HResult;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }


            ShowPlanDataList4Thread(dgdPlanData);

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        void ShowPlanDataList(DataGrid grid)//, DataTable dt)
        {
            string className = "MainWindow";
            string funcName = "ShowPlanDataList";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            DataTable dplanTable = new DataTable();
            string dbdisplay = "";

            try
            {
                dbdisplay = MakeDBDisplayText(tableName);

                dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dplanTable, ref obj);

                if (grid.CheckAccess())
                {
                    grid.ItemsSource = dplanTable.DefaultView;
                    grid.Items.Refresh();
                    if (grid.Items.Count > 0)
                    {
                        //grid.SelectedIndex = markNum;
                        grid.UpdateLayout();
                    }
                }
                else
                {
                    grid.Dispatcher.Invoke(new Action(delegate
                    {
                        grid.ItemsSource = dplanTable.DefaultView;
                        grid.Items.Refresh();
                        if (grid.Items.Count > 0)
                        {
                            //grid.SelectedIndex = markNum;
                            grid.UpdateLayout();
                        }
                    }));
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        void ShowCompleteDataList(DataGrid grid)//, DataTable dt)
        {
            string className = "MainWindow";
            string funcName = "ShowCompleteDataList";

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            DataTable dbcompTable = new DataTable();

            try
            {
                dbwrap.ExecuteCommand(Constants.connstring_comp, "SELECT * from completetable ORDER BY NO DESC", CommandMode.Reader, CommandTypeEnum.Text, ref dbcompTable, ref obj);
                if (grid.CheckAccess())
                {
                    grid.ItemsSource = dbcompTable.DefaultView;
                    grid.Items.Refresh();
                    if (grid.Items.Count > 0)
                    {
                        //grid.SelectedIndex = 0;
                        grid.UpdateLayout();
                    }
                }
                else
                {
                    grid.Dispatcher.Invoke(new Action(delegate
                    {
                        grid.ItemsSource = dbcompTable.DefaultView;
                        grid.Items.Refresh();
                        if (grid.Items.Count > 0)
                        {
                            //grid.SelectedIndex = 0;
                            grid.UpdateLayout();
                        }
                    }));
                }
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //void ShowWorkPlanCountNTime(int count, DateTime dt, bool writeflag)
        //{
        //    if (lblWorkPlanDataCount.CheckAccess())
        //    {
        //        lblWorkPlanDataCount.Content = count.ToString();
        //    }
        //    else
        //    {
        //        lblWorkPlanDataCount.Dispatcher.Invoke(new Action(delegate
        //        {
        //            lblWorkPlanDataCount.Content = count.ToString();
        //        }));
        //    }

        //    if (lblVINLastUpdateDate.CheckAccess())
        //    {
        //        lblVINLastUpdateDate.Content = dt.ToString("yyyy-MM-dd - HH:mm:ss");
        //    }
        //    else
        //    {
        //        lblVINLastUpdateDate.Dispatcher.Invoke(new Action(delegate
        //        {
        //            lblVINLastUpdateDate.Content = dt.ToString("yyyy-MM-dd - HH:mm:ss");
        //        }));
        //    }

        //    if (writeflag)
        //        Util.WritePrivateProfileValue("SERVER", "DOWNLOADTIME", dt.ToString("yyyy-MM-dd - HH:mm:ss"), Constants.PARAMS_INI_FILE);
        //}

        public async Task<ITNTResponseArgs> InitializeController()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            short initSpeed4NoLoad = 0;
            short targetSpeed4NoLoad = 0;
            short accelSpeed4NoLoad = 0;
            short decelSpeed4NoLoad = 0;

            short initSpeed4Scan = 0;
            short targetSpeed4Scan = 0;
            short accelSpeed4Scan = 0;
            short decelSpeed4Scan = 0;

            short initSpeed4ScanFree = 0;
            short targetSpeed4ScanFree = 0;
            short accelSpeed4ScanFree = 0;
            short decelSpeed4ScanFree = 0;

            short steplength = 0;
            short maxY = 0;
            string className = "MainWindow";
            string funcName = "InitializeController";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                sdata = Encoding.UTF8.GetBytes(sendstring);

                initSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "INITIALSPEED", 50, Constants.MARKING_INI_FILE);
                targetSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "TARGETSPEED", 50, Constants.MARKING_INI_FILE);
                accelSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "ACCELERATION", 10, Constants.MARKING_INI_FILE);
                decelSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "DECELERATION", 10, Constants.MARKING_INI_FILE);

                initSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "INITIALSPEED", 10, Constants.SCANNER_INI_FILE);
                targetSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "TARGETSPEED", 10, Constants.SCANNER_INI_FILE);
                accelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "ACCELERATION", 10, Constants.SCANNER_INI_FILE);
                decelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "DECELERATION", 10, Constants.SCANNER_INI_FILE);

                initSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "INITIALSPEED", 10, Constants.SCANNER_INI_FILE);
                targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "TARGETSPEED", 10, Constants.SCANNER_INI_FILE);
                accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "ACCELERATION", 10, Constants.SCANNER_INI_FILE);
                decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "DECELERATION", 10, Constants.SCANNER_INI_FILE);

                steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                maxY = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, Constants.MARKING_INI_FILE);
                maxY = (short)(steplength * maxY);

                short scansteplength = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);
                double home_u = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, Constants.SCANNER_INI_FILE);
                home_u = (short)(scansteplength * home_u);

                m_currCMD = (byte)'F';
                retval = await MarkControll.LoadSpeed(m_currCMD, initSpeed4NoLoad, targetSpeed4NoLoad, accelSpeed4NoLoad, decelSpeed4NoLoad);    //noload
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-F ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'f';
                retval = await MarkControll.LoadSpeed(m_currCMD, initSpeed4ScanFree, targetSpeed4ScanFree, accelSpeed4ScanFree, decelSpeed4ScanFree);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-f ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'l';
                retval = await MarkControll.LoadSpeed(m_currCMD, initSpeed4Scan, targetSpeed4Scan, accelSpeed4Scan, decelSpeed4Scan);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-l ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'O';
                retval = await MarkControll.TestSolFet(12, true);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("TestSolFet ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                Stopwatch sw = new Stopwatch();
                doingCommand = true;
                currentPoint.X = 10;
                currentPoint.Y = 0;
                m_currCMD = (byte)'H';
                retval = await MarkControll.GoHomeAll(0, maxY, (short)home_u);
                if (retval.execResult != 0)
                {
                    doingCommand = false;
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoHome ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MOVE SCAN START : " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        public async Task<ITNTResponseArgs> InitializeControllerLaser()
        {
            string className = "MainWindow";
            string funcName = "InitializeControllerLaser";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //byte[] sdata = new byte[128];
            PatternValueEx pattern = new PatternValueEx();
            int linktype = 0;
            Stopwatch sw = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            string sCurrentFunc = "INITALIZE CONTROLLER";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //MarkControll.StopThread();

                //bControllerInitFlag = 0;

                retval.errorInfo.sErrorFunc = sCurrentFunc;

                retval = ImageProcessManager.GetPatternValue("Pattern_DEFAULT", bHeadType, ref pattern);
                if(retval.execResult != 0)
                {
                    //ShowLog(className, funcName, 2, "컨트롤러 초기화 실패", retval.sErrorMessage);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INITALIZE INITALIZE ERROR : " + retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'F';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4Home, pattern.speedValue.targetSpeed4Home, pattern.speedValue.accelSpeed4Home, pattern.speedValue.decelSpeed4Home);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-F ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                //m_currCMD = 0;

                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4Fast, pattern.speedValue.targetSpeed4Fast, pattern.speedValue.accelSpeed4Fast, pattern.speedValue.decelSpeed4Fast);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LoadSpeed-F ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'O';
                retval = await MarkControll.TestSolFet(12, true);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("TestSolFet ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                //m_currCMD = 0;



                m_currCMD = (byte)'W';
                retval = await MarkControll.SetWorkArea(pattern.headValue.max_X * pattern.headValue.stepLength, pattern.headValue.max_Y * pattern.headValue.stepLength, pattern.headValue.max_Z * pattern.headValue.stepLength);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetWorkArea ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                //Stopwatch sw = new Stopwatch();
                //doingCommand = true;
                //currentPoint.X = 10;
                //currentPoint.Y = 0;
                //m_currCMD = (byte)'E';
                //retval = await MarkControll.GoHomeAll(0, maxY, (short)home_u);
                m_currCMD = (byte)'H';
                //retval = await MarkControll.GoHome((int)(pattern.positionValue.home3DPos.X + 0.5) * pattern.headValue.stepLength, (int)(pattern.positionValue.home3DPos.Y + 0.5) * pattern.headValue.stepLength, (int)(pattern.positionValue.home3DPos.Z + 0.5) * pattern.headValue.stepLength);
                retval = await MarkControll.GoHome((int)(pattern.headValue.home3DPos.X * pattern.headValue.stepLength + 0.5), (int)(pattern.headValue.home3DPos.Y * pattern.headValue.stepLength + 0.5), (int)(pattern.headValue.home3DPos.Z * pattern.headValue.stepLength + 0.5));
                if (retval.execResult != 0)
                {
                    doingCommand = false;
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoHome ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                doingCommand = false;


                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GO PARKING"), Thread.CurrentThread.ManagedThreadId);
                m_currCMD = (byte)'K';
                retval = await MarkControll.GoParking((short)(pattern.headValue.park3DPos.X * pattern.headValue.stepLength + 0.5), (short)(pattern.headValue.park3DPos.Y * pattern.headValue.stepLength + 0.5), (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                if (retval.execResult != 0)
                {
                    doingCommand = false;
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoParking ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }



                //string val = "";
                ////설정읽기
                //linktype = (int)Util.GetPrivateProfileValueUINT("CONFIG", "USELINK", 0, Constants.SCANNER_INI_FILE);
                //if (linktype != 0)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START LINK CHECK"), Thread.CurrentThread.ManagedThreadId);
                //    m_currCMD = (byte)'M';
                //    retval = await MarkControll.GoPoint((short)(pattern.scanValue.linkPos * pattern.headValue.stepLength+0.5), (short)(pattern.positionValue.park3DPos.Y * pattern.headValue.stepLength + 0.5), (short)(pattern.positionValue.park3DPos.Z * pattern.headValue.stepLength + 0.5), 0);
                //    if (retval.execResult != 0)
                //    {
                //        doingCommand = false;
                //        m_currCMD = 0;
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoParking ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                //        return retval;
                //    }

                //    //Set LINK OFF
                //    retval = await SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_OFF, 1, 0);
                //    if(retval.execResult != 0)
                //    {
                //        doingCommand = false;
                //        m_currCMD = 0;
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR 1 : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                //        return retval;
                //    }

                //    //Set LINK ON
                //    retval = await SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON, 0, 0);
                //    if (retval.execResult != 0)
                //    {
                //        doingCommand = false;
                //        m_currCMD = 0;
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR 2 : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                //        return retval;
                //    }

                //    //Set LINK OFF
                //    retval = await SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_OFF, 0, 0);
                //    if (retval.execResult != 0)
                //    {
                //        doingCommand = false;
                //        m_currCMD = 0;
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetLinkAsync ERROR 2 : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                //        return retval;
                //    }

                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END LINK CHECK"), Thread.CurrentThread.ManagedThreadId);
                //}
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        void ShowWorkPlanCount(Label countLebel, int count)
        {
            if (countLebel.CheckAccess())
            {
                countLebel.Content = count.ToString();
            }
            else
            {
                countLebel.Dispatcher.Invoke(new Action(delegate
                {
                    countLebel.Content = count.ToString();
                }));
            }
        }

        void ShowMESReceivedTime(Label dateLabel, string datetime, bool writeflag)
        {
            if (datetime.Length > 0)
            {
                if (dateLabel.CheckAccess())
                {
                    dateLabel.Content = datetime;
                }
                else
                {
                    dateLabel.Dispatcher.Invoke(new Action(delegate
                    {
                        dateLabel.Content = datetime;
                    }));
                }

                if (writeflag)
                    Util.WritePrivateProfileValue("MES", "DOWNLOADTIME", datetime, Constants.PARAMS_INI_FILE);
            }
        }


        //void ShowWorkPlanCountNTime(Label countLebel, Label dateLabel, int count, string datetime, bool writeflag)
        //{
        //    if (countLebel.CheckAccess())
        //    {
        //        countLebel.Content = count.ToString();
        //    }
        //    else
        //    {
        //        countLebel.Dispatcher.Invoke(new Action(delegate
        //        {
        //            countLebel.Content = count.ToString();
        //        }));
        //    }

        //    if (datetime.Length > 0)
        //    {
        //        if (dateLabel.CheckAccess())
        //        {
        //            dateLabel.Content = datetime;
        //        }
        //        else
        //        {
        //            dateLabel.Dispatcher.Invoke(new Action(delegate
        //            {
        //                dateLabel.Content = datetime;
        //            }));
        //        }

        //        if (writeflag)
        //            Util.WritePrivateProfileValue("SERVER", "DOWNLOADTIME", datetime, Constants.PARAMS_INI_FILE);
        //    }
        //}

        private void ShowMarkingDataList(bool plandb, bool completedb/*, bool deletedb*/)
        {
            //DataTable dplanTable = new DataTable();
            //DataTable dbcompTable = new DataTable();
            //object obj = new object();
            string className = "MainWindow";
            string funcName = "ShowMarkingDataList";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //int retval = 0;
            //ITNTDBWrapper dbwrap = new ITNTDBWrapper();

            try
            {
                if (plandb)
                {
                    //dbwrap.ExecuteCommand(Constants.connstring, DBDisplayCommand, CommandMode.Reader, CommandTypeEnum.Text, ref dplanTable, ref obj);
                    ShowPlanDataList(dgdPlanData);//, dplanTable);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                }

                if (completedb)
                {
                    //lock (CompDBLock)
                    {
                        ShowCompleteDataList(CompleteDataGrid);//, dbcompTable);
                    }
                }
            }
            catch (DataException de)
            {
                string error = de.Message;
                int errcode = de.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DB EXCEPTION - CODE = {0:X}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                int errcode = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }


        private void ShowCurrentStateLabel(byte stateNum)
        {
            string nameString = "";
            try
            {
                //CurrentStateLabel = stateNum;
                oldProcessStatus = currentProcessStatus;
                currentProcessStatus = stateNum;
                //ClearCurrentMarkingInformation
                if (this.CheckAccess())
                {
                    Label label = new Label();
                    for (int i = 0; i < STATUS_LABEL_COUNT; i++)
                    {
                        nameString = string.Format("lblSeqStatusE{0}", i);
                        label = (Label)FindName(nameString);
                        if (label == null)
                            continue;

                        if (stateNum == i)
                        {
                            label.Foreground = Brushes.Black;
                            label.Background = Brushes.Orange;
                        }
                        else
                        {
                            label.Foreground = Brushes.LightGray;
                            label.Background = Brushes.Transparent;
                        }
                    }
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        Label label = new Label();
                        for (int i = 0; i < STATUS_LABEL_COUNT; i++)
                        {
                            //nameString = string.Format("lblSeqStatus{0}", i);
                            nameString = "lblSeqStatusE" + i.ToString("D2");
                            label = (Label)FindName(nameString);
                            if (label == null)
                                continue;

                            if (stateNum == i)
                            {
                                label.Foreground = Brushes.Black;
                                label.Background = Brushes.Orange;
                            }
                            else
                            {
                                label.Foreground = Brushes.LightGray;
                                label.Background = Brushes.Transparent;
                            }
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowStateLabel", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private void ShowCurrentStateLabel2(byte stateNum)
        //{
        //    string nameString = "";
        //    try
        //    {
        //        //CurrentStateLabel = stateNum;
        //        oldProcessStatus = currentProcessStatus;
        //        currentProcessStatus = stateNum;
        //        //ClearCurrentMarkingInformation
        //        if (this.CheckAccess())
        //        {
        //            Label label = new Label();
        //            for (int i = 0; i < STATUS_LABEL_COUNT; i++)
        //            {
        //                nameString = string.Format("lblSeqStatusKIN{0}", i);
        //                label = (Label)FindName(nameString);
        //                if (label == null)
        //                    continue;

        //                if (stateNum == i)
        //                {
        //                    label.Foreground = Brushes.Black;
        //                    label.Background = Brushes.Orange;
        //                }
        //                else
        //                {
        //                    label.Foreground = Brushes.LightGray;
        //                    label.Background = Brushes.Transparent;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Dispatcher.Invoke(new Action(delegate
        //            {
        //                Label label = new Label();
        //                for (int i = 0; i < STATUS_LABEL_COUNT; i++)
        //                {
        //                    //nameString = string.Format("lblSeqStatus{0}", i);
        //                    nameString = "lblSeqStatusKIN" + i.ToString("D2");
        //                    label = (Label)FindName(nameString);
        //                    if (label == null)
        //                        continue;

        //                    if (stateNum == i)
        //                    {
        //                        label.Foreground = Brushes.Black;
        //                        label.Background = Brushes.Orange;
        //                    }
        //                    else
        //                    {
        //                        label.Foreground = Brushes.LightGray;
        //                        label.Background = Brushes.Transparent;
        //                    }
        //                }
        //            }));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowCurrentStateLabel2", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private void ShowCurrentStateLabelManual(byte stateNum)
        {
            string nameString = "";
            try
            {
                if (this.CheckAccess())
                {
                    Label label = new Label();
                    for (int i = 0; i < 10; i++)
                    {
                        nameString = string.Format("lblSeqStatusM{0}", i);
                        label = (Label)FindName(nameString);
                        if (label == null)
                            continue;

                        if (stateNum == i)
                        {
                            label.Foreground = Brushes.Black;
                            label.Background = Brushes.Orange;
                        }
                        else
                        {
                            label.Foreground = Brushes.LightGray;
                            label.Background = Brushes.Transparent;
                        }
                    }
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        Label label = new Label();
                        for (int i = 0; i < 10; i++)
                        {
                            nameString = string.Format("lblSeqStatusM{0}", i);
                            label = (Label)FindName(nameString);
                            if (label == null)
                                continue;

                            if (stateNum == i)
                            {
                                label.Foreground = Brushes.Black;
                                label.Background = Brushes.Orange;
                            }
                            else
                            {
                                label.Foreground = Brushes.LightGray;
                                label.Background = Brushes.Transparent;
                            }
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ShowStateLabel", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<int> SendErrorInfo2PLC(byte error, string address)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(32);

            try
            {
                if(plcComm != null)
                    retval = await plcComm.SendErrorInfo(error, address);
            }
            catch(Exception ex)
            {

            }
            //recvArg = await plcComm.SendErrorInfo(error, address);
            return retval.execResult;
            ////int retval = 0;

            //plcComm.SendPCError2PLC(error.ToString());

        }

        private void ShowErrorMessage(string errormsg, bool clearflag)
        {
            Brush brush;
            if (lblError.CheckAccess())
            {
                if (clearflag)
                {
                    brush = new SolidColorBrush(Color.FromArgb(255, (byte)0xc0, (byte)0xc0, (byte)0xc0));
                    lblError.Background = brush;
                    lblError.Content = "";
                }
                else
                {
                    brush = new SolidColorBrush(Color.FromArgb(125, (byte)0, (byte)0, (byte)0));
                    lblError.Background = brush;
                    //lblError.Background = Brushes.Red;
                    lblError.Content = errormsg;
                    //SaveErrorDB(errormsg, "");
                }
            }
            else
            {
                lblError.Dispatcher.Invoke(new Action(delegate
                {
                    if (clearflag)
                    {
                        brush = new SolidColorBrush(Color.FromArgb(255, (byte)0xc0, (byte)0xc0, (byte)0xc0));
                        lblError.Background = brush;
                        lblError.Content = "";
                    }
                    else
                    {
                        lblError.Background = Brushes.Red;
                        lblError.Content = errormsg;
                        //SaveErrorDB(errormsg, "");
                    }
                }));
            }
        }

        //private async Task SaveErrorDB(string error, string descrption)
        //{
        //    System.Action action = delegate ()
        //    {
        //        string className = "MainWindow";
        //        string funcName = "SaveErrorDB";
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //        DateTime now = DateTime.Now;
        //        ITNTDBWrapper dbwrap = new ITNTDBWrapper();
        //        object obj = new object();

        //        //lock (ErrorDBLock)
        //        {
        //            try
        //            {
        //                string insertstring = "INSERT INTO errortable (DATE, TIME, MESSAGE, DESCRIPTION) VALUES('" +
        //                                      now.ToString("yyyyMMdd") + "','" + now.ToString("HHmmss") + "','" + error + "','" + descrption + "')";

        //                DataTable dbMainDataTable = new DataTable();
        //                //ITNTDBManage db = new ITNTDBManage(Constants.connstring);

        //                dbwrap.ExecuteCommand(Constants.connstring_error, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

        //                //db.Open(Constants.connstring_error);
        //                //db.CommandText = insertstring;
        //                //int retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
        //                //db.Close();
        //            }
        //            catch (Exception ex)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //            }
        //        }

        //        //bUsingDB = false;
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //        return;
        //    };

        //    Task task = Task.Factory.StartNew(action);
        //    //task.ConfigureAwait(false);
        //    return;
        //}

        private async Task SaveErrorDB(ErrorInfo errorInfo, string currentProc)
        {
            string className = "MainWindow";
            string funcName = "SaveErrorDB";
            DateTime now = DateTime.Now;
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            string insertstring = "";
            DataTable dbMainDataTable = new DataTable();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                insertstring = "INSERT INTO errortable (DATE, TIME, CODE, PROCEDURE, ERRFUNC, MESSAGE, DEVICE, DEVFUNC, DEVMSG, DESCRIPTION1 VALUES('" +
                                now.ToString("yyyyMMdd") + "','" + now.ToString("HHmmss") + "','" + errorInfo.sErrorCode + "','" + currentProc + "','" +
                                errorInfo.sErrorFunc + "','" + errorInfo.sErrorMessage + "','" + errorInfo.devErrorInfo.sDeviceName + "','" + errorInfo.devErrorInfo.sErrorFunc + "','" +
                                errorInfo.devErrorInfo.sErrorMessage + "','" + errorInfo.sErrorDetail1 + "')";

                dbwrap.ExecuteCommand(Constants.connstring_error, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                //db.Open(Constants.connstring_error);
                //db.CommandText = insertstring;
                //int retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                //db.Close();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            return;
        }

        string GetRawVIN(DataRow row, string colName, int colNum, string defalut)
        {
            if (row.Table.Columns.Contains(colName) == true)
                return row.ItemArray[colNum].ToString();
            else
                return defalut;
        }

        private int GetMarkDataInfomation(DataGrid grid, ref MESReceivedData recvMsg)
        {
            //DataRowView row = null;
            DataRow row = null;
            MESReceivedData ret = new MESReceivedData();

            try
            {
                if (grid.CheckAccess())
                {
                    if (grid.Items.Count <= 0)
                        return -1;

                    row = GetCurrentMarkPointData(0).Result;
                    if (row == null)
                        return -2;

                    //recv.productdate = row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString();
                    DateTime dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                    ret.productdate = dateValue.ToString("yyyy-MM-dd");

                    ret.sequence = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                    ret.rawcartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    ret.bodyno = row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                    ret.rawvin = GetRawVIN(row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                    ret.markvin = AddMonthCode(ret.rawvin);

                    //recv.mesdate = row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                    //recv.mestime = row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                    DateTime timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                    ret.mesdate = dateValue.ToString("yyyy-MM-dd");
                    ret.mestime = timeValue.ToString("HH:mm:ss");

                    ret.lastsequence = row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                    ret.code219 = row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                    ret.idplate = row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                    ret.delete = row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                    ret.totalmsg = row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                    ret.rawbodytype = row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                    ret.rawtrim = row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                    ret.region = row.ItemArray[Constants.DB_NAME_REGION].ToString();
                    ret.bodytype = row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                    ret.cartype = row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                    ret.plcvalue = row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                    //recv.markdate = row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString();
                    //recv.marktime = row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString();
                    dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                    timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                    ret.markdate = dateValue.ToString("yyyy-MM-dd");
                    ret.marktime = timeValue.ToString("HH:mm:ss");

                    ret.remark = row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                    ret.exist = row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                    ret.isInserted = row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                    recvMsg = (MESReceivedData)ret.Clone();
                    return 0;
                }
                else
                {
                    recvMsg = Dispatcher.Invoke<MESReceivedData>(new Func<MESReceivedData>(delegate
                    {
                        MESReceivedData recv = new MESReceivedData();
                        if (grid.Items.Count <= 0)
                        {
                            recv.execResult = -1;
                            return recv;
                        }

                        //dgdPlanData.SelectedIndex = m_CurrentMarkNum;
                        //row = dgdPlanData.SelectedItem as DataRowView;
                        row = GetCurrentMarkPointData(0).Result;
                        if (row == null)
                        {
                            recv.execResult = -1;
                            return recv;
                        }

                        //recv.productdate = row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString();
                        DateTime dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                        recv.productdate = dateValue.ToString("yyyy-MM-dd");

                        recv.sequence = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                        recv.rawcartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                        recv.bodyno = row.ItemArray[Constants.DB_NAME_BODYNO].ToString();

                        ret.rawvin = GetRawVIN(row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                        ret.markvin = AddMonthCode(ret.rawvin);

                        //recv.mesdate = row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                        //recv.mestime = row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                        dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                        DateTime timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                        recv.mesdate = dateValue.ToString("yyyy-MM-dd");
                        recv.mestime = timeValue.ToString("HH:mm:ss");

                        recv.lastsequence = row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                        recv.code219 = row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                        recv.idplate = row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                        recv.delete = row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                        recv.totalmsg = row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                        recv.rawbodytype = row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                        recv.rawtrim = row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                        recv.region = row.ItemArray[Constants.DB_NAME_REGION].ToString();
                        recv.bodytype = row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                        recv.cartype = row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                        recv.plcvalue = row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                        recv.markdate = row.ItemArray[Constants.DB_NAME_MARKDATE].ToString();
                        recv.marktime = row.ItemArray[Constants.DB_NAME_MARKTIME].ToString();
                        //dateValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                        //timeValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                        //recv.markdate = dateValue.ToString("yyyy-MM-dd");
                        //recv.marktime = timeValue.ToString("HH:mm:ss");

                        recv.remark = row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                        recv.exist = row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                        recv.isInserted = row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();
                        recv.execResult = 0;
                        return recv;
                    }));

                    return recvMsg.execResult;
                }
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        //private async Task<int> ShowSequenceErrorMessage()
        //{
        //    bool bret = false;
        //    try
        //    {
        //        bret = ShowMatchingErrorMessage();
        //        if (bret)
        //        {
        //            //ITNTJobLog.Instance.Trace(0, "차종 또는 VIN 비교 NG 해제 -> 강제 비교 OK 전송");
        //            ShowLabelData("투입 순서 에러", lblCheckResult);
        //            await SendErrorInfo2PLC((byte)0, PLCMELSEQSerial.PLC_ADDRESS_D200);
        //            return 0;
        //        }
        //        else
        //        {
        //            //ITNTJobLog.Instance.Trace(0, "수동조치 모드");
        //            ShowLabelData("MATCHING NG", lblCheckResult);
        //            //await Send2MarkController("01", true, " ");
        //            await SendErrorInfo2PLC((byte)0, PLCMELSEQSerial.PLC_ADDRESS_D200);
        //            return -2;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.HResult;
        //    }
        //}

        private ITNTResponseArgs ShowMatchingErrorMessage(string plcCarType, string mesCarType)
        {
            string className = "MainWindow";
            string funcName = "ShowMatchingErrorMessage";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();
            bool ret = false;
            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                msg1.Message = "CAR TYPE MATCHING ERROR";
                msg1.Fontsize = 20;
                msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg1.VerticalContentAlignment = VerticalAlignment.Center;
                msg1.Foreground = Brushes.Red;
                msg1.Background = Brushes.White;

                msg2.Message = "PLC = " + plcCarType + ", MES = " + mesCarType;
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                msg3.Message = "1. PRESS [ERROR RESET] BUTTON FOR 5 SECONDS.";
                msg3.Fontsize = 16;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Blue;
                msg3.Background = Brushes.White;

                msg4.Message = "2. SET \"SETTING POINT\" AGAIN.";
                msg4.Fontsize = 16;
                msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg4.VerticalContentAlignment = VerticalAlignment.Center;
                msg4.Foreground = Brushes.Blue;
                msg4.Background = Brushes.White;

                msg5.Message = "3. PRESS [REMATCHING] BUTTON.";
                msg5.Fontsize = 16;
                msg5.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg5.VerticalContentAlignment = VerticalAlignment.Center;
                msg5.Foreground = Brushes.Blue;
                msg5.Background = Brushes.White;

                if (CheckAccess())
                {
                    WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    ret = warning.ShowDialog().Value;
                }
                else
                {
                    ret = Dispatcher.Invoke(new Func<bool>(delegate
                    {
                        bool bret = false;
                        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        bret = warning.ShowDialog().Value;
                        return bret;
                    }));
                }

                if(ret == false)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MATCHING ERROR - USER PRESS NO", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = ErrorCodeConstant.ERROR_MATCHING_NG;
                    retval.errorInfo.sErrorMessage = "CAR TYPE CHECK ERROR : PLC = " + plcCarType + ", MES = " + mesCarType;
                    return retval;
                }
                else
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MATCHING ERROR - USE PRESS YES", Thread.CurrentThread.ManagedThreadId);

                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;
                retval.execResult = ex.HResult;
                return retval;
            }
        }

        private bool ShowMatchingErrorMessageNoData()
        {
            string className = "MainWindow";
            string funcName = "ShowMatchingErrorMessageNoData";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                msg1.Message = "There is no car. Please check data.";
                msg1.Fontsize = 20;
                msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg1.VerticalContentAlignment = VerticalAlignment.Center;
                msg1.Foreground = Brushes.Red;
                msg1.Background = Brushes.White;

                msg2.Message = "(VIN = " + currMarkInfo.currMarkData.mesData.rawvin + ", SEQ = " + currMarkInfo.currMarkData.mesData.sequence + ")";
                msg2.Fontsize = 16;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Blue;
                msg2.Background = Brushes.White;

                //msg3.Message = "Select [NO] to reset point.";
                //msg3.Fontsize = 16;
                //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //msg3.Foreground = Brushes.Blue;
                //msg3.Background = Brushes.White;
                bool ret = false;

                if (CheckAccess())
                {
                    WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    ret = warning.ShowDialog().Value;
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        ret = warning.ShowDialog().Value;
                    }));
                }

                return ret;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return false;
            }
        }

        private bool ShowDoubleMarkErrorMessage(string vin)
        {
            string className = "MainWindow";
            string funcName = "ShowDoubleMarkErrorMessage";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();
            bool ret = false;

            try
            {
                //msg1.Message = "이전에 각인 완료한 데이터입니다.";
                msg1.Message = "DOUBLE MARKING ERROR";
                msg1.Fontsize = 20;
                msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg1.VerticalContentAlignment = VerticalAlignment.Center;
                msg1.Foreground = Brushes.Red;
                msg1.Background = Brushes.White;

                msg2.Message = "(" + vin + ")";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                //msg3.Message = "1. 판넬의 [이상 해제]버튼을 누르고, ";
                msg3.Message = "THIS VIN HAS ALEADY BEEN MARKED PREVIOUSLY.";
                msg3.Fontsize = 16;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Blue;
                msg3.Background = Brushes.White;

                //msg4.Message = "2. 화면에서 세팅 포인트를 다시 설정한 후 ";
                msg4.Message = "PRESS [ERROR RESET] FOR 5 SECONDS";
                msg4.Fontsize = 16;
                msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg4.VerticalContentAlignment = VerticalAlignment.Center;
                msg4.Foreground = Brushes.Blue;
                msg4.Background = Brushes.White;

                //msg5.Message = "3. 판넬의 [매칭 재시작] 버튼을 눌러 주세요.";
                msg5.Message = "AFTER SETTING \"SET POINT\", PRESS [REMATCHING]";
                msg5.Fontsize = 16;
                msg5.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg5.VerticalContentAlignment = VerticalAlignment.Center;
                msg5.Foreground = Brushes.Blue;
                msg5.Background = Brushes.White;

                //msg3.Message = "계속 각인하려면 [OK] 버튼을 누르세요.";
                //msg3.Fontsize = 16;
                //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //msg3.Foreground = Brushes.Blue;
                //msg3.Background = Brushes.White;

                //msg4.Message = "각인을 취소하려면 [NO] 버튼을 누르세요.";
                //msg4.Fontsize = 16;
                //msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg4.VerticalContentAlignment = VerticalAlignment.Center;
                //msg4.Foreground = Brushes.Blue;
                //msg4.Background = Brushes.White;

                if (CheckAccess())
                {
                    WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    ret = warning.ShowDialog().Value;
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        ret = warning.ShowDialog().Value;
                    }));
                }
                //WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                //warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //bool ret = warning.ShowDialog().Value;
                return ret;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return false;
            }
        }

        private bool ShowSequenceErrorMessage(string plcseq, string ccrseq)
        {
            string className = "MainWindow";
            string funcName = "ShowSequenceErrorMessage";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();
            bool ret = false;

            try
            {
                msg1.Message = "PLEASE CHECK SEQUENCE.";// (PLC : " + plcseq + ", CCR : " + ccrseq + ")";
                msg1.Fontsize = 20;
                msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg1.VerticalContentAlignment = VerticalAlignment.Center;
                msg1.Foreground = Brushes.Red;
                msg1.Background = Brushes.White;

                msg2.Message = "(PLC : " + plcseq + ", MES : " + ccrseq + ")";
                msg2.Fontsize = 20;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                //msg3.Message = "1. 판넬의 [이상 해제]버튼을 누르고, ";
                //msg3.Fontsize = 16;
                //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //msg3.Foreground = Brushes.Blue;
                //msg3.Background = Brushes.White;

                //msg4.Message = "2. 화면에서 세팅 포인트를 다시 설정한 후 ";
                //msg4.Fontsize = 16;
                //msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg4.VerticalContentAlignment = VerticalAlignment.Center;
                //msg4.Foreground = Brushes.Blue;
                //msg4.Background = Brushes.White;

                //msg5.Message = "3. 판넬의 [매칭 재시작] 버튼을 눌러 주세요.";
                //msg5.Fontsize = 16;
                //msg5.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg5.VerticalContentAlignment = VerticalAlignment.Center;
                //msg5.Foreground = Brushes.Blue;
                //msg5.Background = Brushes.White;

                //msg3.Message = "계속 각인하려면 [OK] 버튼을 누르세요.";
                //msg3.Fontsize = 16;
                //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //msg3.Foreground = Brushes.Blue;
                //msg3.Background = Brushes.White;

                //msg4.Message = "각인을 취소하려면 [Cancel] 버튼을 누르세요.";
                //msg4.Fontsize = 16;
                //msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg4.VerticalContentAlignment = VerticalAlignment.Center;
                //msg4.Foreground = Brushes.Blue;
                //msg4.Background = Brushes.White;

                if (CheckAccess())
                {
                    WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    ret = warning.ShowDialog().Value;
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        ret = warning.ShowDialog().Value;
                    }));
                }
                //WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                //warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //bool ret = warning.ShowDialog().Value;
                return ret;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return false;
            }
        }

        //private async Task<int> Send2MarkController(string cmd, bool flag, string data)
        //{
        //    int retval = 0;
        //    //Func<object, object, object, int> action = delegate (object c, object b, object d)
        //    //{
        //    //    string className = "MainWindow";
        //    //    string funcName = "SaveCompleteData2DBAsync";
        //    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //    //    string auto = "";
        //    //    string remark = "";
        //    //    ////ITNTJobLog.Instance.Trace(0, "{0}", " 데이터 저장 시작");

        //    //    //bUsingDB = false;
        //    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SAVE END");
        //    //    return 1;
        //    //};

        //    //Task<int> task = Task<int>.Factory.StartNew(action, cmd, flag, data);
        //    //retval = await task(cmd, flag, data);
        //    return retval;
        //}

        private async Task<int> WriteCompleteData(MESReceivedData data, byte remarkFlag)
        {
            int retval = 0;
            DateTime dt = DateTime.Now;
            string curDir = "";
            string filename = curDir + "\\" + dt.ToString("dd") + ".dat";
            string[] msg = new string[1];
            string className = "MainWindow";
            string funcName = "WriteCompleteData";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                curDir = AppDomain.CurrentDomain.BaseDirectory + "\\Complete File\\" + dt.ToString("yyyy") + "\\" + dt.ToString("MM");
                if (System.IO.Directory.Exists(curDir) == false)
                    System.IO.Directory.CreateDirectory(curDir);

                msg[0] = string.Format("{0} {1} {2} {3} {4} {5} {6}\r\n", dt.ToString("yyyyMMdd-HH:mm:ss"), data.sequence, data.rawvin, data.rawcartype.Trim(), data.rawbodytype.Trim(), data.idplate.Trim(), data.plcvalue);
                byte[] result = Encoding.UTF8.GetBytes(msg[0]);
                using (FileStream SourceStream = File.Open(filename, FileMode.Append))
                {
                    SourceStream.Seek(0, SeekOrigin.End);
                    SourceStream.Write(result, 0, result.Length);
                }
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async Task<int> CheckSequence(string seqNew)//, string seqMarked)
        {
            int SeqMarkedValue = 0;
            int SeqNewValue = 0;
            int retval = 0;
            string seqMarked = "";
            try
            {
                if (CheckAccess())
                {
                    if (chbCheckSeq.IsChecked == true)
                    {
                        DataRowView selMarkedRow;
                        if (CompleteDataGrid.Items.Count >= 1)
                        {
                            //dgdCompleteData.SelectedIndex = 0;
                            //selMarkedRow = dgdCompleteData.SelectedItem as DataRowView;
                            selMarkedRow = (DataRowView)CompleteDataGrid.Items[0];
                            seqMarked = selMarkedRow.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                            Int32.TryParse(seqNew.ToString(), out SeqNewValue);
                            Int32.TryParse(seqMarked, out SeqMarkedValue);
                            if ((SeqNewValue == 0) || (SeqNewValue == 1) || ((SeqNewValue - SeqMarkedValue) == 1))
                                return 0;
                            else
                            {
                                ShowLabelData("SEQ COMPARE NG", lblCheckResult);
                                //ITNTJobLog.Instance.Trace(0, "SEQ NG " + seqNew + "-- " + seqMarked);
                                ShowErrorMessage("SEQUENCE COMPARE NG. PLEASE CHECK SEQUENCE!! " + seqNew + "-- " + seqMarked, false);

                                //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                                return 1;
                            }
                        }
                    }
                    return 0;
                }
                else
                {
                    retval = await Dispatcher.Invoke(new Func<Task<int>>(async delegate
                    {
                        if (chbCheckSeq.IsChecked == true)
                        {
                            DataRowView selMarkedRow;
                            if (CompleteDataGrid.Items.Count >= 1)
                            {
                                //dgdCompleteData.SelectedIndex = 0;
                                //selMarkedRow = dgdCompleteData.SelectedItem as DataRowView;
                                selMarkedRow = (DataRowView)CompleteDataGrid.Items[0];
                                seqMarked = selMarkedRow.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                                Int32.TryParse(seqNew.ToString(), out SeqNewValue);
                                Int32.TryParse(seqMarked, out SeqMarkedValue);
                                if ((SeqNewValue == 0) || (SeqNewValue == 1) || ((SeqNewValue - SeqMarkedValue) == 1))
                                    return 0;
                                else
                                {
                                    ShowLabelData("SEQ COMPARE NG", lblCheckResult);
                                    //ShowLabelData("SEQ 비교 NG", lblCheckResult);
                                    //ITNTJobLog.Instance.Trace(0, "SEQ NG " + seqNew + "-- " + seqMarked);
                                    ShowErrorMessage("SEQUENCE COMPARE NG. PLEASE CHECK SEQUENCE!! " + seqNew + "-- " + seqMarked, false);

                                    //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, PLCMELSEQSerial.PLC_ADDRESS_D200);
                                    return 1;
                                }
                            }
                        }
                        return 0;
                    }));
                    return retval;
                }
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        private async Task<ITNTResponseArgs> CheckSequence2(string seqNew, string sProcess)//, string seqMarked)
        {
            string className = "MainWindow";
            string funcName = "CheckSequence2";
            //int retval = 0;
            string plcseq = "";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            ITNTSendArgs sendarg = new ITNTSendArgs();
            bool bret = false;
            string sCurrentFunc = "CHECK SEQUENCE";

            try
            {
#if AGING_TEST_PLC
                plcseq = seqNew;
#else
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                retval = await plcComm.ReadPLCSequence();
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCSequence ERROR = " + retval.execResult, Thread.CurrentThread.ManagedThreadId);
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCSequence) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                    ShowLabelData("----", lblPLCSEQValue);
                    ShowLabelData(seqNew, lblMESSEQValue);
                    return retval;
                }

                //recvArg.recvString = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadPLCSequence {0}", retval.recvString), Thread.CurrentThread.ManagedThreadId);
                if (retval.recvString.Length < 8)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("COMMUNICATION ERROR TO PLC (ReadPLCSequence) - SHORT LENGTH {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                    retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                    retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCSequence) : " + retval.recvString.Length + " - " + retval.recvString;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;
                    retval.errorInfo.devErrorInfo.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;

                    ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                    return retval;
                }

                plcseq = retval.recvString.Substring(4, 4);
#endif

                ShowLabelData(plcseq, lblPLCSEQValue);
                ShowLabelData(seqNew, lblMESSEQValue);

                if (seqNew != plcseq)
                {
                    await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);


                    ShowLabelData("SEQUENCE MATCHING NG", lblCheckResult, Brushes.Red);
                    //else
                    //{
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEQUENCE ERROR : PLC = " + plcseq + ", MES = " + seqNew, Thread.CurrentThread.ManagedThreadId);
                    //ShowErrorMessage("SEQUENCE COMPARE ERROR : PLC(" + plcseq + "), CCR(" + seqNew + ")", false);
                    //ShowLabelData("SEQUENCE COMPARE NG", lblCheckResult);
                    //recvarg.execResult = -2;
                    //recvarg.sErrorMessage = "PLC = " + plcseq + ", MES = " + seqNew;
                    retval.recvString = plcseq;

                    retval.execResult = ErrorCodeConstant.ERROR_MATCHING_NG;
                    retval.errorInfo.sErrorMessage = "MATCHING (SEQUENCE) NG : " + "PLC = " + plcseq + ", MES = " + seqNew;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_SEQ_NG;

                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;
                    retval.errorInfo.devErrorInfo.execResult = ErrorCodeConstant.ERROR_MATCHING_NG;

                    ITNTErrorCode(className, funcName, sProcess, retval.errorInfo, 0);

                    //SaveErrorDB(retval.errorInfo, sCurrentFunc);
                    bret = ShowSequenceErrorMessage(plcseq, seqNew);
                    //if (bret)
                    //{
                    //    //ITNTJobLog.Instance.Trace(0, "Sequence NG signal release -> Send OK signal forced.");
                    //    //await SendErrorInfo2PLC((byte)0, PLCMELSEQSerial.PLC_ADDRESS_D200);

                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEQUENCE ERROR : PLC = " + plcseq + ", MES = " + seqNew, Thread.CurrentThread.ManagedThreadId);

                    //    recvarg.execResult = 0;
                    //    return recvarg;
                    //}

                    return retval;
                }
                ShowLabelData("MATCHING OK", lblCheckResult, Brushes.Blue);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;
                retval.errorInfo.devErrorInfo.execResult = ErrorCodeConstant.ERROR_MATCHING_NG;

                ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                return retval;
            }
            //recvarg.execResult = 0;
            return retval;
        }

        //private async Task<int>CheckVisionPorcess()
        //{
        //    int retval = 0;
        //    return retval;
        //}

        private async Task<int> CheckDoubleMarking(string vin, int days)
        {
            int retval = 0;
            Func<object, int> func = x =>
            {
                //ITNTDBManage dbmanager = new ITNTDBManage();
                DataTable dbMainDataTable = new DataTable();
                DateTime dt = new DateTime();
                int count = 0;
                string start = dt.ToString("yyyy-MM-dd");
                string end = DateTime.Now.ToString("yyyy-MM-dd");
                string commandstr = "SELECT COUNT(*) FROM COMPLETETABLE WHERE VIN='" + vin + "' AND (DATE(MARKDATE) between '" + start + "' AND '" + end + "')";
                ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                object obj = new DataTable();

                try
                {
                    //lock (CompDBLock)
                    {
                        dbwrap.ExecuteCommand(Constants.connstring_comp, commandstr, CommandMode.Scalar, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                        count = (int)(long)obj;

                        //dbmanager.Open(Constants.connstring_comp);
                        //dbmanager.CommandText = commandstr;
                        //object sbscalar = new DataTable();
                        //dbmanager.ExecuteCommandScalar(CommandTypeEnum.Text, ref sbscalar);
                        //count = (int)(long)sbscalar;
                        //dbmanager.Close();
                    }
                }
                catch (DataException de)
                {
                    string error = de.Message;
                    int errcode = de.HResult;
                    return de.HResult;
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                    int errcode = ex.HResult;
                    return ex.HResult;
                }
                return count;
            };

            try
            {
                retval = await Task.Factory.StartNew(func, days);
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
            return retval;
        }

        private async Task<ITNTResponseArgs> CheckDoubleMarking(string vin, string sProcess)
        {
            //int retval = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string className = "MainWindow";
            string funcName = "CheckDoubleMarking";

            DataTable dbMainDataTable = new DataTable();
            DateTime dt = new DateTime();
            int count = 0;
            string start = "";
            string end = "";
            string commandstr = "";
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();
            bool bret = false;
            string sCurrentFunc = "CHECK DOUBLE MARK";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                start = dt.ToString("yyyy-MM-dd");
                end = DateTime.Now.ToString("yyyy-MM-dd");
                commandstr = "SELECT COUNT(*) FROM COMPLETETABLE WHERE VIN='" + vin + "'";

                dbwrap.ExecuteCommand(Constants.connstring_comp, commandstr, CommandMode.Scalar, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                count = (int)(long)obj;

//#if AGING_TEST_PLC
//                count = 0;
//#endif

                if (count > 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "[" + vin + "] DOUBLE MARKING CHECK NG !!!");
                    await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);

                    ShowLabelData("DOUBLE MARKING ERROR", lblCheckResult, Brushes.Red);
                    //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "[" + vin + "] WAS MARKED BEFORE", "");

                    retval.execResult = ErrorCodeConstant.ERROR_DOUBLEMARK_NG;
                    retval.errorInfo.sErrorMessage = "DOUBLE MARKING ERROR : " + vin;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_DOUBLE_MARK;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    ITNTErrorCode(className, funcName, sProcess, retval.errorInfo, 0);

                    //SaveErrorDB(retval.errorInfo, sCurrentFunc);
                    bret = ShowDoubleMarkErrorMessage(vin);

                    return retval;
                }
                ShowLabelData("DOUBLE MARKING OK", lblCheckResult, Brushes.Blue);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                ITNTErrorCode(className, funcName, sProcess, retval.errorInfo, 0);


                //ShowLog(className, funcName, (byte)LOGTYPE.LOG_FAILURE, "DOUBLE MARKING ERROR - EXCEPTION ERROR = " + ex.Message, "");

                //SaveErrorDB(retval.errorInfo, sCurrentFunc);

                return retval;
            }
            return retval;
        }

        private void ShowLabelData(string Data, Label label)//, Brush foreground, Brush background)
        {
            if (label.CheckAccess())
            {
                //label.Foreground = foreground;
                //label.Background = background;
                label.Content = Data;
            }
            else
            {
                label.Dispatcher.Invoke(new Action(delegate
                {
                    //label.Foreground = foreground;
                    //label.Background = background;
                    label.Content = Data;
                }));
            }
        }

        private void ShowLabelData(string Data, Label label, Brush forebrush = null, Brush backbrush = null, Brush boarderbrush = null)//, Brush foreground, Brush background)
        {
            if (label.CheckAccess())
            {
                if (forebrush == null)
                    forebrush = label.Foreground;
                if (backbrush == null)
                    backbrush = label.Background;
                if(boarderbrush == null)
                    boarderbrush = label.BorderBrush;
                //label.Foreground = foreground;
                //label.Background = background;
                label.Content = Data;
                label.Foreground = forebrush;
                label.Background = backbrush;
                label.BorderBrush = boarderbrush;
            }
            else
            {
                label.Dispatcher.Invoke(new Action(delegate
                {
                    if (forebrush == null)
                        forebrush = label.Foreground;
                    if (backbrush == null)
                        backbrush = label.Background;
                    if (boarderbrush == null)
                        boarderbrush = label.BorderBrush;
                    //label.Foreground = foreground;
                    //label.Background = background;
                    label.Content = Data;
                    label.Foreground = forebrush;
                    label.Background = backbrush;
                    label.BorderBrush = boarderbrush;
                }));
            }
        }

        private string GetLabelData(Label control)//, Brush foreground, Brush background)
        {
            string retstring = "";
            if (control.CheckAccess())
            {
                //label.Foreground = foreground;
                //label.Background = background;
                retstring = control.Content.ToString();
            }
            else
            {
                retstring = control.Dispatcher.Invoke(new Func<string>(delegate
                {
                    //label.Foreground = foreground;
                    //label.Background = background;
                    string ret = "";
                    ret = control.Content.ToString();
                    return ret;
                }));
            }

            return retstring;
        }

        private string GetFrameTypeFromPLCData(string carType, byte DefaultFlag = 0)
        {
            string value = "";
            Util.GetPrivateProfileValue("FRAMETYPEPLC", carType, "", ref value, Constants.PARAMS_INI_FILE);
            if ((DefaultFlag != 0) && (value.Length <= 0))
                value = carType;
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodytype">Body Type of Frame
        ///                         1 = "장축"
        ///                         3 = "4WD"
        ///                         4 = "초장축"
        ///                         8 = "고상" </param>
        /// <param name="DefaultFlag"></param>
        /// <returns></returns>
        private string GetFrameTypeDescription(string frametype, byte DefaultFlag = 0)
        {
            string value = "";
            Util.GetPrivateProfileValue("FRAMETYPE", frametype, "", ref value, Constants.PARAMS_INI_FILE);
            if ((DefaultFlag != 0) && (value.Length <= 0))
                value = frametype;
            //if(body.Contains("PLC") == true)
            //{
            //    body = "PLC" + body + plcNo;
            //    Util.GetPrivateProfileValue("FRAMETYPEBODY", body, "", ref value, Constants.PARAMS_INI_FILE);
            //    if ((DefaultFlag != 0) && (value.Length <= 0))
            //        value = body;
            //}
            return value;
        }

        private string GetCarTypeDescription(string cartype, byte DefaultFlag = 0)
        {
            string value = "";
            string key = cartype.Substring(0, 1);
            Util.GetPrivateProfileValue("CARTYPE", key, "", ref value, Constants.PARAMS_INI_FILE);
            if ((DefaultFlag != 0) && (value.Length <= 0))
                value = cartype;
            return value;
        }

        public string GetCarTypeFromCarName(string editedtype, string rawcartype)
        {
            string value = "";
            string raw = editedtype.Trim();
            Util.GetPrivateProfileValue("CARTYPE", editedtype, "", ref value, Constants.PARAMS_INI_FILE);
            if (value.Length <= 0)
                value = rawcartype;
            return value;
        }

        public string GetCarType(string rawcartype)
        {
            string retval = "";
            string value = "";
            string tmpType = "";

            try
            {
                Util.GetPrivateProfileValue("OPTION", "CARTYPECOMPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "1")
                {
                    retval = GetCarTypeFromNumber(rawcartype.Trim());
                }
                else
                {
                    if(rawcartype.Trim().Length >= 2)
                        tmpType = rawcartype.Trim().Substring(0, 2);
                    else
                        tmpType = rawcartype.Trim();
                    retval = GetCarTypeFromCarName(tmpType, rawcartype.Trim());
                }
            }
            catch (Exception ex)
            {
                retval = rawcartype;
            }

            return retval;
        }


        //public string GetCarTypeFromPLC(string plcCarType)
        //{
        //    string value = "";
        //    string raw = plcCarType.Trim();
        //    //Util.GetPrivateProfileValue("CARTYPE", plcCarType, "", ref value, Constants.PLCVAL_INI_FILE);
        //    Util.GetPrivateProfileValue("CARTYPE", plcCarType, "", ref value, Constants.PARAMS_INI_FILE);
        //    if (value.Length <= 0)
        //    {
        //        value = plcCarType;
        //    }
        //    return value;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="plcNo"></param>
        /// <param name="DefaultFlag"> 데이터가 없는 경우 기본값을 무슨 값으로 할지를 설정
        ///                             1 = 입력값으로 리턴
        ///                             0 = 설정한 문자열("")를 리턴
        ///                             </param>
        /// <returns></returns>
        private string GetBodyType(string cartype, string rawbodytype, string trimtype, string defaultval, byte DefaultFlag = 0)
        {
            string value = "";
            int retval = 0;
            retval = Util.GetPrivateProfileValue(rawbodytype, trimtype, defaultval, ref value, "Parameter\\BodyTypeList\\" + cartype + ".ini");
            if ((DefaultFlag != 0) && (value == defaultval))
                value = rawbodytype;

            //if (body.Contains("PLC") == true)
            //{
            //    body = "PLC" + body + plcNo;
            //    Util.GetPrivateProfileValue("BODYTYPEBODY", body, "", ref value, Constants.PARAMS_INI_FILE);
            //    if ((DefaultFlag != 0) && (value.Length <= 0))
            //        value = body;
            //}
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="plcNo"></param>
        /// <param name="DefaultFlag"> 데이터가 없는 경우 기본값을 무슨 값으로 할지를 설정
        ///                             1 = 입력값으로 리턴
        ///                             0 = 설정한 문자열("")를 리턴
        ///                             </param>
        /// <returns></returns>
        private string GetPLCValue(string cartype, string rawbodytype, string trimtype, string defaultval, byte DefaultFlag = 0)
        {
            string value = "";
            int retval = 0;
            string sec = "";
            string key2 = "";
            string key = "";
            sec = cartype.Trim();// + "_" + rawbodytype;
            key = rawbodytype.Trim();
            key2 = rawbodytype.Trim() + "_" + trimtype.Trim();

            //retval = Util.GetPrivateProfileValue(sec, key2, "", ref value, Constants.PLCVAL_INI_FILE);// ".\\Parameter\\PLCValueList\\" + cartype + ".ini");
            retval = Util.GetPrivateProfileValue(sec, key2, "", ref value, Constants.PARAMS_INI_FILE);// ".\\Parameter\\PLCValueList\\" + cartype + ".ini");
            if (value == "")
            {
                //retval = Util.GetPrivateProfileValue(sec, key, defaultval, ref value, Constants.PLCVAL_INI_FILE);// ".\\Parameter\\PLCValueList\\" + cartype + ".ini");
                retval = Util.GetPrivateProfileValue(sec, key, defaultval, ref value, Constants.PARAMS_INI_FILE);// ".\\Parameter\\PLCValueList\\" + cartype + ".ini");
            }
            return value;

            //string value = "";
            //int retval = 0;
            //retval = Util.GetPrivateProfileValue(rawbodytype, trimtype, defaultval, ref value, Constants.PLCVAL_INI_FILE);// ".\\Parameter\\PLCValueList\\" + cartype + ".ini");
            //if ((DefaultFlag != 0) && (value == defaultval))
            //    value = rawbodytype;

            ////if (body.Contains("PLC") == true)
            ////{
            ////    body = "PLC" + body + plcNo;
            ////    Util.GetPrivateProfileValue("BODYTYPEBODY", body, "", ref value, Constants.PARAMS_INI_FILE);
            ////    if ((DefaultFlag != 0) && (value.Length <= 0))
            ////        value = body;
            ////}
            //return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="plcNo"></param>
        /// <param name="DefaultFlag"> 데이터가 없는 경우 기본값을 무슨 값으로 할지를 설정
        ///                             1 = 입력값으로 리턴
        ///                             0 = 설정한 문자열("")를 리턴
        ///                             </param>
        /// <returns></returns>
        private string GetFrameType4MES(string cartype, string mesframetype, string mestrim, string defaultval, byte DefaultFlag = 0)
        {
            string value = "";
            int retval = 0;
            string key = "";
            key = mesframetype + "_" + mestrim;
            retval = Util.GetPrivateProfileValue(cartype, key, "", ref value, Constants.FRAMETYPE_INI_FILE);
            if (value.Length <= 0)
            {
                key = mesframetype;
                retval = Util.GetPrivateProfileValue(cartype, key, defaultval, ref value, Constants.FRAMETYPE_INI_FILE);
                if ((DefaultFlag != 0) && (value == defaultval))
                    value = mesframetype;
            }
            return value;
        }

        private string GetPLCValue(string cartype, string bodytype, byte DefaultFlag = 0)
        {
            string retval = "9999";
            //Util.GetPrivateProfileValue(cartype, bodytype, "", ref retval, Constants.PLCVAL_INI_FILE);
            Util.GetPrivateProfileValue(cartype, bodytype, "", ref retval, Constants.PARAMS_INI_FILE);
            if ((DefaultFlag != 0) && (retval.Length <= 0))
                retval = "9999";

            return retval;
        }

        private async Task ShowCurrentSequenceVIN(int markpass, CURRENTMARKDATA currData, byte showFlag, byte clearFlag)
        {
            string className = "MainWindow";
            string funcName = "ShowCurrentSequenceVIN";

            try
            {
                if (CheckAccess())
                {
                    await ShowMarkingDataDelegate(markpass, currData.mesData, currData.pattern, currData.fontData, currData.fontSizeX, currData.fontSizeY, currData.shiftValue, showFlag, clearFlag);
                }
                else
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
                        await ShowMarkingDataDelegate(markpass, currData.mesData, currData.pattern, currData.fontData, currData.fontSizeX, currData.fontSizeY, currData.shiftValue, showFlag, clearFlag);
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private async Task ShowCurrentMarkingInformation2(int markpass, MESReceivedData mesdata, PatternValueEx pattern, List<List<FontDataClass>> fontdata, double fontSizeX, double fontSizeY, double shiftVal, byte showFlag, byte clearFlag)
        {
            string className = "MainWindow";
            string funcName = "ShowCurrentMarkingInformation";
            //string stringcolorfore = "#FFCBCBCB";
            //string stringcolorback = "#FFE1E1E1";
            //Color colorback;
            //Color colorfore;

            try
            {
                if (CheckAccess())
                {
                    await ShowMarkingDataDelegate(markpass, mesdata, pattern, fontdata, fontSizeX, fontSizeY, shiftVal, showFlag, clearFlag);
                }
                else
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
                        await ShowMarkingDataDelegate(markpass, mesdata, pattern, fontdata, fontSizeX, fontSizeY, shiftVal, showFlag, clearFlag);
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task ShowMarkingDataDelegate(int markpass, MESReceivedData mesdata, PatternValueEx pattern, List<List<FontDataClass>> fontdata, double fontSizeX, double fontSizeY, double shiftVal, byte showFlag, byte clearFlag)
        {
            string className = "MainWindow";
            string funcName = "ShowMarkingDataDelegate";
            string stringcolorfore = "#FFCBCBCB";
            string stringcolorback = "#FFE1E1E1";
            Color colorback;
            Color colorfore;
            string value = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("OPTION", "SHOWCARVALUETYPE", "0", ref value, Constants.PARAMS_INI_FILE);

                if (markpass != 0)
                {
                    lblCurrentSeq.Content = mesdata.sequence;
                    if(value == "0")
                        lblCurrentType.Content = mesdata.rawcartype.Trim();
                    else
                        lblCurrentType.Content = mesdata.cartype.Trim();

                    stpVINBypass.Visibility = Visibility.Visible;
                    stpVINDisplay.Visibility = Visibility.Collapsed;

                    colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
                    colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
                }
                else
                {
                    lblCurrentSeq.Content = mesdata.sequence;
                    //lbllblCurrentType.Content = mesdata.rawcartype.Trim();
                    if (value == "0")
                        lblCurrentType.Content = mesdata.rawcartype.Trim();
                    else
                        lblCurrentType.Content = mesdata.cartype.Trim();

                    stpVINBypass.Visibility = Visibility.Collapsed;
                    stpVINDisplay.Visibility = Visibility.Visible;

                    if (showFlag == 1)
                    {
                        Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND1", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND1", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
                        colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
                        colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
                    }
                    else if (showFlag == 2)
                    {
                        Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND2", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND2", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
                        colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
                        colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
                    }
                    else if (showFlag == 3)
                    {
                        Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND3", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND3", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
                        colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
                        colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
                    }
                    else if (showFlag == 0)
                    {
                        return;
                    }
                    else
                    {
                        Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND1", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND1", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
                        colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
                        colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
                    }
                }
                await showRecogCharacters(mesdata.markvin, fontdata, pattern, fontSizeX, fontSizeY, shiftVal, colorfore, colorback, clearFlag);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task showRecogCharacters(string vin, List<List<FontDataClass>> fontdata, PatternValueEx pattern, double fontSizeX, double fontSizeY, double shiftVal, Color fore, Color back, byte clearFlag)
        {
            string className = "MainWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "showRecogCharacters";// MethodBase.GetCurrentMethod().Name;

            int charNum = 0;
            Canvas[] cvsshowChar = new Canvas[Constants.MAX_VIN_NO_SIZE];
            string ctrlName = "";
            VinNoInfo vininfo = new VinNoInfo();
            int count = 0;
            Brush brush;
            int retval = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                count = vin.Length;

                vininfo.vinNo = vin;
                vininfo.fontName = pattern.fontValue.fontName;
                vininfo.width = pattern.fontValue.width;
                vininfo.height = pattern.fontValue.height;
                vininfo.pitch = pattern.fontValue.pitch;
                vininfo.thickness = pattern.fontValue.thickness;

                for (int i = 0; i < count; i++)
                {
                    charNum = (int)vin[i] - 1;
                    ctrlName = string.Format("cvsshowChar{0:D2}", i);
                    if (cvsshowChar.Length <= i)
                        continue;

                    cvsshowChar[i] = (Canvas)FindName(ctrlName);
                    if (cvsshowChar[i] == null)
                        continue;

                    cvsshowChar[i].Background = new SolidColorBrush(back);
                    brush = new SolidColorBrush(fore);

                    List<FontDataClass> fdata = new List<FontDataClass>();
                    if (fontdata.Count >= i)
                        fdata = fontdata[i].ToList();
                    else
                        continue;

                    if(CheckAccess() == true)
                        retval = await ShowOneVinNoCharacter2(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, clearFlag);
                    else
                    {
                        retval = await Dispatcher.Invoke(new Func<Task<int>>(async delegate
                        {
                            int ret = 0;
                            ret = await ShowOneVinNoCharacter2(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, clearFlag);
                            return ret;
                        }));
                    }
                }
                //if (CheckAccess())
                //{
                //    for (int i = 0; i < count; i++)
                //    {
                //        charNum = (int)vin[i] - 1;
                //        ctrlName = string.Format("cvsshowChar{0:D2}", i);
                //        cvsshowChar[i] = (Canvas)FindName(ctrlName);
                //        if (cvsshowChar[i] == null)
                //            continue;

                //        cvsshowChar[i].Background = new SolidColorBrush(back);
                //        brush = new SolidColorBrush(fore);

                //        List<FontDataClass> fdata = new List<FontDataClass>();
                //        if (fontdata.Count >= i)
                //            fdata = fontdata[i].ToList();
                //        else
                //            continue;

                //        await ShowOneVinNoCharacter2(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, clearFlag);
                //    }
                //}
                //else
                //{
                //    Dispatcher.Invoke(new Action(async delegate
                //    {
                //        for (int i = 0; i < count; i++)
                //        {
                //            charNum = (int)vin[i] - 1;
                //            ctrlName = string.Format("cvsshowChar{0:D2}", i);
                //            cvsshowChar[i] = (Canvas)FindName(ctrlName);
                //            if (cvsshowChar[i] == null)
                //                continue;

                //            cvsshowChar[i].Background = new SolidColorBrush(back);
                //            brush = new SolidColorBrush(fore);

                //            List<FontDataClass> fdata = new List<FontDataClass>();
                //            if (fontdata.Count >= i)
                //                fdata = fontdata[i].ToList();
                //            else
                //                continue;

                //            await ShowOneVinNoCharacter2(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, clearFlag);
                //        }
                //    }));
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("showRecogCharacters excption {0} - {1}", ex.HResult, ex.Message);
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }


        //private async Task ShowMarkingDataDelegate2(int markpass, MESReceivedData mesdata, PatternValueEx pattern, List<List<FontDataClass>> fontdata, double fontSizeX, double fontSizeY, double shiftVal, byte showFlag, byte clearFlag)
        //{
        //    string className = "MainWindow";
        //    string funcName = "ShowMarkingDataDelegate";
        //    string stringcolorfore = "#FFCBCBCB";
        //    string stringcolorback = "#FFE1E1E1";
        //    Color colorback;
        //    Color colorfore;

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //        if (markpass != 0)
        //        {
        //            lblCurrentSeq.Content = mesdata.sequence;
        //            lblCurrentType.Content = mesdata.rawcartype.Trim();

        //            colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
        //            colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);

        //            await showRecogCharacters3(markpass, mesdata.vin, fontdata, pattern, fontSizeX, fontSizeY, shiftVal, colorfore, colorback, clearFlag);
        //            return;
        //        }

        //        if (showFlag == 1)
        //        {
        //            Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND1", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
        //            Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND1", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
        //            colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
        //            colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
        //        }
        //        else if (showFlag == 2)
        //        {
        //            Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND2", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
        //            Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND2", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
        //            colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
        //            colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
        //        }
        //        else if (showFlag == 3)
        //        {
        //            Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND3", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
        //            Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND3", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
        //            colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
        //            colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
        //        }
        //        else if (showFlag == 0)
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            Util.GetPrivateProfileValue("COLOR", "VINBACKGROUND1", stringcolorback, ref stringcolorback, Constants.PARAMS_INI_FILE);
        //            Util.GetPrivateProfileValue("COLOR", "VINFOREGROUND1", stringcolorfore, ref stringcolorfore, Constants.PARAMS_INI_FILE);
        //            colorback = (Color)ColorConverter.ConvertFromString(stringcolorback);
        //            colorfore = (Color)ColorConverter.ConvertFromString(stringcolorfore);
        //        }

        //        await showRecogCharacters3(markpass, mesdata.vin, fontdata, pattern, fontSizeX, fontSizeY, shiftVal, colorfore, colorback, clearFlag);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        //private async Task showRecogCharacters3(int markpass, string vin, List<List<FontDataClass>> fontdata, PatternValueEx pattern, double fontSizeX, double fontSizeY, double shiftVal, Color fore, Color back, byte clearFlag)
        //{
        //    string className = "MainWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "showRecogCharacters3";// MethodBase.GetCurrentMethod().Name;

        //    int retval = 0;
        //    int charNum = 0;
        //    Canvas[] cvsshowChar = new Canvas[19];
        //    string ctrlName = "";
        //    VinNoInfo vininfo = new VinNoInfo();
        //    int count = 0;// vin.Length;
        //    Brush brush;

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //        count = vin.Length;

        //        if (markpass == 1)
        //        {
        //            stpVINBypass.Visibility = Visibility.Visible;
        //            stpVINDisplay.Visibility = Visibility.Collapsed;
        //            //lblVINDislay.Content = "MARKING PASS";
        //            return;
        //        }
        //        else if (markpass == -1)
        //        {
        //            stpVINDisplay.Visibility = Visibility.Visible;
        //            stpVINBypass.Visibility = Visibility.Collapsed;

        //            vininfo.vinNo = vin;
        //            vininfo.fontName = pattern.fontValue.fontName;
        //            vininfo.width = pattern.fontValue.width;
        //            vininfo.height = pattern.fontValue.height;
        //            vininfo.pitch = pattern.fontValue.pitch;
        //            vininfo.thickness = pattern.fontValue.thickness;

        //            for (int i = 0; i < count; i++)
        //            {
        //                charNum = (int)vin[i] - 1;
        //                ctrlName = string.Format("cvsshowChar{0:D2}", i);
        //                cvsshowChar[i] = (Canvas)FindName(ctrlName);
        //                if (cvsshowChar[i] == null)
        //                    continue;

        //                cvsshowChar[i].Background = new SolidColorBrush(back);
        //                brush = new SolidColorBrush(fore);

        //                //if ((charNum >= 31) && (charNum <= 128))
        //                {
        //                    List<FontDataClass> fdata = new List<FontDataClass>();
        //                    if (fontdata.Count >= i)
        //                        fdata = fontdata[i].ToList();
        //                    else
        //                        continue;

        //                    //List<FontDataClass> fdata = new List<FontDataClass>();
        //                    //if (fdata.Count >= i)
        //                    //    fdata = fontdata[i].ToList();
        //                    //else
        //                    //    continue;
        //                    //fdata = fontData[charNum];
        //                    if (CheckAccess())
        //                    {
        //                        await ShowOneVinNoCharacter2(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, clearFlag);
        //                        //if (bHeadType == 0)
        //                        //    retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                        //else
        //                        //    retval = await ShowOneVinNoCharacterLaser(fdata, vininfo, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                    }
        //                    else
        //                    {
        //                        Dispatcher.Invoke(new Action(async delegate
        //                        {
        //                            await ShowOneVinNoCharacter2(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, clearFlag);

        //                            //if (bHeadType == 0)
        //                            //    retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                            //else
        //                            //    retval = await ShowOneVinNoCharacterLaser(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                        }));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("showRecogCharacters excption {0} - {1}", ex.HResult, ex.Message);
        //    }

        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //}



        private void Window_Closed(object sender, EventArgs e)
        {
            //ConfirmWindowString msg1 = new ConfirmWindowString();
            //ConfirmWindowString msg2 = new ConfirmWindowString();
            //ConfirmWindowString msg3 = new ConfirmWindowString();
            //ConfirmWindowString msg4 = new ConfirmWindowString();

            //msg2.Message = "프로그램을 종료하시겠습니까?";
            //msg2.Fontsize = 18;
            //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
            //msg2.VerticalContentAlignment = VerticalAlignment.Center;
            //msg2.Foreground = Brushes.Red;
            //msg2.Background = Brushes.White;

            //ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, msg5, "YES", "NO", this);// , "", "", this);
            //window.Owner = this;
            //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            //if (window.ShowDialog() == false)
            //    return;

            //closeFlag = true;
            CloseVisionServer();
            CloseMESServer();
            CloseMarkController();
            ClosePLC();

            if ((bHeadType != 0) || (bUseDispalcementSensor))
                CloseDistanceSensor();

            string value = "";
            //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
            if (bHeadType != 0)
            {
                laserSource.LaserControllerStatusEventFunc -= OnLaserControllerStatusChangedEventReceivedFunc;
                laserSource.LaserConnectionStatusChangedEventFunc -= OnLaserConnectionStatusChangedEventReceivedFunc;

                laserSource.CloseClient(1);
                //return retval;
            }

            CloseCommunicationToDIO();
            CloseLPM();

            //Environment.Exit(0);
            //System.Diagnostics.Process.GetCurrentProcess().Kill();
            //this.Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "Window_Loaded";

            int width, height;
            string value = "";

            try
            {
//                this.WindowState = System.Windows.WindowState.Normal;
//                this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
//                //Rectangle retc = new Rectangle();
//                //retc.Height = this.Height;
//                //retc.Width = this.Width;

//                this.Left = 0;
//                this.Top = 1080;
////                this.Width = 1600;
////                this.Height = 990;

//                orginalWidth = this.Width;
//                originalHeight = this.Height;

//                Util.GetPrivateProfileValue("SCREEN", "SCREENWIDTH", "1920", ref value, Constants.PARAMS_INI_FILE);
//                int.TryParse(value, out width);

//                Util.GetPrivateProfileValue("SCREEN", "SCREENHEIGHT", "1080", ref value, Constants.PARAMS_INI_FILE);
//                int.TryParse(value, out height);

//                this.Show();
//                this.WindowState = System.Windows.WindowState.Maximized;

                if (this.WindowState == WindowState.Maximized)
                {
                    //ChangeSize(this.ActualWidth, this.ActualHeight);
                    ChangeSize(this.Width, this.Height);
                    //ChangeSize(width, height);
                }
                this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            orginalWidth = this.Width;
            originalHeight = this.Height;

            ChangeSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void btnControllerSet_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "btnControllerSet_Click";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                ControllerSettingWindow window = new ControllerSettingWindow();
                window.Show();

                this.Focus();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<int> ShowOneVinNoCharacter2(List<FontDataClass> font, VinNoInfo vininfo, double density, double fontSizeX, double fontSizeY, double shiftVal, Canvas showcanvas, Brush brush, Brush background, byte clearFlag, int interval = 0)
        {
            string className = "MainWindow";
            string funcName = "ShowOneVinNoCharacter2";

            double canvaswidth = showcanvas.Width;
            double canvasheight = showcanvas.Height;
            double OriginX = 1.5d * Util.PXPERMM;
            double OriginY = 2.5d * Util.PXPERMM;
            double orgWidth = (vininfo.width) * Util.PXPERMM + OriginX * 2;
            double orgHeight = Util.PXPERMM * vininfo.height + OriginY * 2;

            /***********************************
            1 inch  25.4mm
            1 inch  72 pt
            1 inch  96 px        dpi
            1 mm    2.83465 pt
            1 mm    3.7795 px    dpi/ 25.4
            ***********************************/

            double CharHeight = vininfo.height * Util.PXPERMM;
            double CharWidth = vininfo.width * Util.PXPERMM;
            //double pitch_px = vin.pitch * Util.PXPERMM;
            double CharThick = vininfo.thickness * Util.PXPERMM * ((canvaswidth / orgWidth) + 0.2);
            //double CharThick = vin.thickness * Util.PXPERMM * canvaswidth / orgWidth;

            double heightRation = canvasheight / orgHeight;
            double widthRation = canvaswidth / orgWidth;
            int index = 0;
            int Dotsize = 5;
            Line[] line = new Line[font.Count];
            Ellipse[] CurrentDot = new Ellipse[font.Count];

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                //showcanvas.UpdateLayout();
                //showcanvas.Background = background;

                if (clearFlag != 0)
                    showcanvas.Children.Clear();

                for (int j = 0; j < font.Count; j++)
                {
                    if ((font[j] == null) || (font[j].Flag < 0))
                        continue;

                    switch (density)
                    {
                        case 0:     // Dot Marking : Dot
                        case 1:     // Dot Marking : Dot for Line
                            if (font[j].Flag >= 0)
                            {
                                //2022.03.25 Replace by drawing ellipse.
                                CurrentDot[index] = new Ellipse();
                                CurrentDot[index].Stroke = brush;
                                CurrentDot[index].StrokeThickness = CharThick;
                                //Canvas.SetZIndex(CurrentDot[index], (int)(CharThick + 0.5));
                                CurrentDot[index].Height = (double)Dotsize;
                                CurrentDot[index].Width = (double)Dotsize;
                                CurrentDot[index].Fill = brush;
                                CurrentDot[index].Margin = new Thickness((OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation - (double)Dotsize / 2.0, (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightRation - (double)Dotsize / 2.0, 0.0, 0.0);
                                showcanvas.Children.Add(CurrentDot[index]);
                                index++;
                            }
                            break;

                        default:
                            if (font[j].Flag == 1)
                            {
                                line[index] = new Line();
                                line[index].Stroke = brush;
                                line[index].StrokeThickness = CharThick;
                                line[index].StrokeStartLineCap = PenLineCap.Round;
                                line[index].StrokeEndLineCap = PenLineCap.Round;
                                line[index].StrokeLineJoin = PenLineJoin.Round;

                                line[index].X1 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                line[index].Y1 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightRation;
                            }
                            else if (font[j].Flag == 2 || font[j].Flag == 3 || font[j].Flag == 5)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                    line[index].Y2 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightRation;

                                    showcanvas.Children.Add(line[index]);
                                    if (interval > 0)
                                        await Task.Delay(interval);
                                    index++;
                                }
                                line[index] = new System.Windows.Shapes.Line();
                                line[index].Stroke = brush;
                                line[index].StrokeThickness = CharThick;
                                line[index].StrokeStartLineCap = PenLineCap.Round;
                                line[index].StrokeEndLineCap = PenLineCap.Round;
                                line[index].StrokeLineJoin = PenLineJoin.Round;

                                line[index].X1 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                line[index].Y1 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightRation;
                            }
                            else if (font[j].Flag == 4)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                    line[index].Y2 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightRation;
                                    showcanvas.Children.Add(line[index]);
                                    if (interval > 0)
                                        await Task.Delay(interval);
                                }
                            }
                            else
                            {
                            }
                            break;
                    }
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION1 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Debug.WriteLine("ShowOneVinNoCharacter excption {0} - {1}", ex.HResult, ex.Message);
                return ex.HResult;
            }
        }

        private void MenuItem_SearchData_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "MenuItem_SearchData_Click";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            string value = "";
            try
            {
                Util.GetPrivateProfileValue("OPTION", "SEARCHTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "0")
                {
                    SearchCompleteWindow window = new SearchCompleteWindow();
                    window.Show();
                }
                else
                {
                    SearchCompleteWindow2 window = new SearchCompleteWindow2();
                    window.Show();
                }

                this.Focus();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void MenuItem_ViewLog_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_SetupLaser_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow chgWindow = new ChangePasswordWindow();
            chgWindow.Owner = System.Windows.Application.Current.MainWindow;
            chgWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            chgWindow.ShowDialog();
        }

        private void MenuItem_ResetSystem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();

            msg2.Message = "Are you sure you want to quit program?";
            msg2.Fontsize = 18;
            msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
            msg2.VerticalContentAlignment = VerticalAlignment.Center;
            msg2.Foreground = Brushes.Red;
            msg2.Background = Brushes.White;

            ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);

            if (window.ShowDialog() == false)
                return;

            bcloseThread = true;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Vision Server S", Thread.CurrentThread.ManagedThreadId);
            CloseVisionServer();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Vision Server E", Thread.CurrentThread.ManagedThreadId);
            CloseMESServer();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close MES Server E", Thread.CurrentThread.ManagedThreadId);

            CloseMarkController();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Mark Controller E", Thread.CurrentThread.ManagedThreadId);

            ClosePLC();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close PLC E", Thread.CurrentThread.ManagedThreadId);

            if ((bHeadType != 0) || (bUseDispalcementSensor))
            {
                CloseDistanceSensor();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Displacement E", Thread.CurrentThread.ManagedThreadId);
            }

            string value = "";
            //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
            if (bHeadType != 0)
            {
                laserSource.LaserControllerStatusEventFunc -= OnLaserControllerStatusChangedEventReceivedFunc;
                laserSource.LaserConnectionStatusChangedEventFunc -= OnLaserConnectionStatusChangedEventReceivedFunc;

                laserSource.CloseClient(1);
                //return retval;
            }

            CloseCommunicationToDIO();

            CloseLPM();

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CloseCommunicationToDIO", Thread.CurrentThread.ManagedThreadId);

            Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            this.Close();
        }


        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //List<FontDataClass> temp = new List<FontDataClass>();
            //string ErrorCode = "";
            //MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();
            //int k = 0;
            //try
            //{
            //    if (!currMarkInfo.isReady)
            //    {
            //        currMarkInfo.Initialize();
            //        GetMarkDataInfomation(ref currMarkInfo.currMarkData.mesData);
            //        currMarkInfo.currMarkData.patternName = GetPatternName(currMarkInfo.currMarkData.mesData);

            //        GetPatternData(currMarkInfo.currMarkData.patternName, ref currMarkInfo.currMarkData.pattern);
            //        //GetMarkFontInfomation(ref currMarkInfo.font);
            //        ShowCurrnetMarkingInformation(currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.mesData.sequence, currMarkInfo.currMarkData.mesData.rawcartype, currMarkInfo.currMarkData.pattern);
            //        for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++)
            //        {
            //            List<FontDataClass> FontDataClass = new List<FontDataClass>();
            //            ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.currMarkData.pattern.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
            //            currMarkInfo.currMarkData.fontData.Add(fontData);
            //        }
            //        currMarkInfo.isReady = true;

            //        //currMarkInfo.Initialize();
            //        //GetMarkDataInfomation(ref currMarkInfo.currMarkData.mesData);
            //        //GetMarkFontInfomation(ref currMarkInfo.font);
            //        //for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++)
            //        //{
            //        //    List<FontDataClass> FontDataClass = new List<FontDataClass>();
            //        //    ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.font.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
            //        //    currMarkInfo.markData.Add(fontData);
            //        //}
            //        //currMarkInfo.isReady = true;
            //    }

            //    arg.receiveBuffer[k++] = (byte)ASCII.SOH;
            //    arg.receiveBuffer[k++] = 0x31;
            //    arg.receiveBuffer[k++] = 0x44;
            //    arg.receiveBuffer[k++] = (byte)0x30;
            //    arg.receiveBuffer[k++] = (byte)ASCII.STX;
            //    arg.cmd = 0x30;

            //    string curvin = currMarkInfo.curvin.ToString("X8");
            //    byte[] tmp1 = Encoding.UTF8.GetBytes(curvin);
            //    Array.Copy(tmp1, 0, arg.receiveBuffer, k, 4);
            //    k += 4;

            //    string curFontPoint = currMarkInfo.curFontPoint.ToString("X8");
            //    byte[] tmp2 = Encoding.UTF8.GetBytes(curFontPoint);
            //    Array.Copy(tmp2, 0, arg.receiveBuffer, k, 4);
            //    k += 4;

            //    Array.Copy(tmp2, 0, arg.receiveBuffer, k, 4);
            //    k += 4;

            //    Array.Copy(tmp2, 0, arg.receiveBuffer, k, 2);
            //    k += 2;

            //    arg.receiveBuffer[k++] = (byte)ASCII.ETX;
            //    arg.receiveBuffer[k++] = (byte)ASCII.STX;
            //    arg.receiveBuffer[k++] = (byte)ASCII.CR;
            //    arg.receiveSize = k;
            //    //arg.receiveMsg = string.Format("1{0:D4}{1:D4}0000", currMarkInfo.curvin, currMarkInfo.curFontPoint);

            //    if (currMarkInfo.curvin < currMarkInfo.currMarkData.mesData.vin.Length)
            //        temp = currMarkInfo.currMarkData.fontData[currMarkInfo.curvin];

            //    currMarkInfo.curFontPoint++;
            //    if (currMarkInfo.curFontPoint >= temp.Count)
            //    {
            //        currMarkInfo.curvin++;
            //        currMarkInfo.curFontPoint = 0;
            //        if (currMarkInfo.curvin >= currMarkInfo.currMarkData.mesData.vin.Length)
            //        {
            //            arg.cmd = 0x32;
            //            arg.receiveBuffer[3] = 0x32;
            //            OnMarkControllerEventFunc(this, arg);
            //            return;
            //        }
            //    }
            //    OnMarkControllerEventFunc(this, arg);
            //}
            //catch(Exception ex)
            //{
            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "Button_Click_2", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            //}

        }


        private void SaveMarkResultData(MESReceivedData result, byte completeflag, int remarkflagvalue, byte multiMakrFlag, byte markorderFlag, CheckAreaData chkdata, int dirty)
        {
            string className = "MainWindow";
            string funcName = "SaveMarkResultData";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //string auto = "";
            //string remark = "";
            //ITNTJobLog.Instance.Trace(0, " START TO SAVE DATA");
            string comp = "";
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();
            DataTable dbMainDataTable = new DataTable();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if (completeflag == 1)
                    comp = "Y";
                else
                    comp = "N";

                string insertstring = "INSERT INTO completetable (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, " +
                                        "LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, " + 
                                        "PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, " +
                                        "ISMARK, COMPLETE, EXIST, CHECKFLAG, MULTIFLAG, MARKORDER, " +
                                        "PLATECHECK01, PLATECHECK02, PLATECHECK03, PLATECHECK04, PLATECHECK05, " +
                                        "PLATECHECK06, PLATECHECK07, PLATECHECK08, PLATECHECK09, PLATECHECK10, RAWVIN, DIRTY) VALUES('" +
                      result.productdate + "','" + result.sequence + "','" + result.rawcartype + "','" + result.bodyno + "','" + result.markvin + "','" + result.mesdate + "','" + result.mestime + "','" +
                      result.lastsequence + "','" + result.code219 + "','" + result.idplate + "','" + result.delete + "','" + result.totalmsg + "','" + result.rawbodytype + "','" + result.rawtrim + "','" +
                      result.plcvalue + "','" + result.region + "','" + result.bodytype + "','" + result.cartype + "','" + result.markdate + "','" + result.marktime + "','" + result.remark + "','" +
                      result.isMarked + "','" + comp + "','" + result.exist + "',0," + multiMakrFlag + "," + markorderFlag + ",'" +
                      chkdata.checkdistance[0].ToString("F3") + "','" + chkdata.checkdistance[1].ToString("F3") + "','" + chkdata.checkdistance[2].ToString("F3") + "','" +
                      chkdata.checkdistance[3].ToString("F3") + "','" + chkdata.checkdistance[4].ToString("F3") + "','" + chkdata.checkdistance[5].ToString("F3") + "','" +
                      chkdata.checkdistance[6].ToString("F3") + "','" + chkdata.checkdistance[7].ToString("F3") + "','" + chkdata.checkdistance[8].ToString("F3") + "','" +
                      chkdata.checkdistance[9].ToString("F3") + "','" + result.rawvin + "'," + dirty + ")";

                dbwrap.ExecuteCommand(Constants.connstring_comp, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                Thread thread = new Thread(ShowCompleteDataList);
                thread.Start();
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            //bUsingDB = false;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            //ITNTJobLog.Instance.Trace(0, " COMPLETE TO SAVE DATA");
        }

        private void ShowCompleteDataList()
        {
            string className = "MainWindow";
            string funcName = "ShowCompleteDataList";
            //string comp = "";

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbMainDataTable = new DataTable();
            Object obj = new Object();
            string commandstring = "";
            int showcnt = 0;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                showcnt = (int)Util.GetPrivateProfileValueUINT("CONFIG", "SHOWCOMPLETECOUNT", 0, Constants.PARAMS_INI_FILE);
                if (showcnt <= -1)
                    commandstring = "SELECT * from completetable ORDER BY DATE(MARKDATE) DESC, TIME(MARKTIME) DESC";
                else if (showcnt == 0)
                    commandstring = "SELECT * from completetable ORDER BY DATE(MARKDATE) DESC, TIME(MARKTIME) DESC limit 10000";
                else
                    commandstring = "SELECT * from completetable ORDER BY DATE(MARKDATE) DESC, TIME(MARKTIME) DESC limit " + showcnt.ToString();
                dbwrap.ExecuteCommand(Constants.connstring_comp, commandstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                if (CompleteDataGrid.CheckAccess())
                {
                    CompleteDataGrid.ItemsSource = dbMainDataTable.DefaultView;
                    CompleteDataGrid.Items.Refresh();
                    if ((CompleteDataGrid.Items.Count > 0) && (CompleteDataGrid.SelectedItems.Count < 2))
                    {
                        CompleteDataGrid.UpdateLayout();
                    }
                }
                else
                {
                    CompleteDataGrid.Dispatcher.Invoke(new Action(delegate
                    {
                        CompleteDataGrid.ItemsSource = dbMainDataTable.DefaultView;
                        CompleteDataGrid.Items.Refresh();
                        if ((CompleteDataGrid.Items.Count > 0) && (CompleteDataGrid.SelectedItems.Count < 2))
                        {
                            CompleteDataGrid.UpdateLayout();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void btnMove2PlanData_Click(object sender, RoutedEventArgs e)
        {
            DataRowView row = null;
            DateTime dt = DateTime.Now;
            MESReceivedData recvMsg = new MESReceivedData();
            MESReceivedData planMsg = new MESReceivedData();
            try
            {
                if (CompleteDataGrid.Items.Count <= 0)
                {
                    return;
                }

                if (CompleteDataGrid.SelectedIndex < 0)
                {
                    return;
                }

                row = CompleteDataGrid.SelectedItem as DataRowView;

                DateTime tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());

                recvMsg.productdate = tmp.ToString("yyyy-MM-dd");
                recvMsg.sequence = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                recvMsg.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                recvMsg.bodyno = row.Row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                //recvMsg.vin = row.Row.ItemArray[Constants.DB_NAME_VIN].ToString();

                recvMsg.rawvin = GetRawVIN(row.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                recvMsg.markvin = AddMonthCode(recvMsg.rawvin);

                tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                recvMsg.mesdate = tmp.ToString("yyyy-MM-dd");

                tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                recvMsg.mestime = tmp.ToString("HH:mm:ss");

                recvMsg.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                recvMsg.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                recvMsg.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                recvMsg.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                recvMsg.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                recvMsg.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                recvMsg.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                recvMsg.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                recvMsg.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                recvMsg.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                recvMsg.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                recvMsg.markdate = dt.ToString("yyyy-MM-dd");
                recvMsg.marktime = dt.ToString("HH:mm:ss");
                recvMsg.remark = row.Row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                recvMsg.exist = row.Row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                recvMsg.isInserted = row.Row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                planMsg = (MESReceivedData)recvMsg.Clone();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnMove2PlanData_Click", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private void btnMove2Complete_Click(object sender, RoutedEventArgs e)
        //{
        //    DataRowView row = null;
        //    DateTime dt = DateTime.Now;
        //    MESReceivedData recvMsg = new MESReceivedData();
        //    try
        //    {
        //        //dgdPlanData.SelectedIndex = 0;
        //        row = dgdPlanData.SelectedItem as DataRowView;

        //        DateTime tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());

        //        recvMsg.productdate = tmp.ToString("yyyy-MM-dd");
        //        recvMsg.sequence = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
        //        recvMsg.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
        //        recvMsg.bodyno = row.Row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
        //        recvMsg.vin = row.Row.ItemArray[Constants.DB_NAME_VIN].ToString();

        //        tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
        //        recvMsg.mesdate = tmp.ToString("yyyy-MM-dd");

        //        tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
        //        recvMsg.mestime = tmp.ToString("HH:mm:ss");

        //        recvMsg.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
        //        recvMsg.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
        //        recvMsg.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
        //        recvMsg.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
        //        recvMsg.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
        //        recvMsg.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
        //        recvMsg.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
        //        recvMsg.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
        //        recvMsg.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
        //        recvMsg.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
        //        recvMsg.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

        //        recvMsg.markdate = dt.ToString("yyyy-MM-dd");
        //        recvMsg.marktime = dt.ToString("HH:mm:ss");
        //        recvMsg.remark = row.Row.ItemArray[Constants.DB_NAME_REMARK].ToString();
        //        recvMsg.exist = row.Row.ItemArray[Constants.DB_NAME_EXIST].ToString();

        //        //SaveCompleteData(recvMsg, 0).Wait();
        //        SaveMarkResultData(recvMsg, 1, 0);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnMove2Complete_Click", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private void MenuItem_SendVIN2MarkController_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void MenuItem_SetComport_Click(object sender, RoutedEventArgs e)
        {
            ComPortSettingWindow window = new ComPortSettingWindow(0);

            if (window.ShowDialog() == false)
            {
                if (window.saveFlag == true)
                {
                    CloseMarkController();
                    await Task.Delay(500);
                    await OpenMarkController();
                }
            }
        }

        private void MenuItem_ManualMarking_Click(object sender, RoutedEventArgs e)
        {
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();
            bool selResult = false;

            try
            {
                if (m_autoExecuteFlag == 1)
                {
                    //msg1.Message = "차종 매칭 이상이 발생하였습니다.";
                    //msg1.Fontsize = 20;
                    //msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                    //msg1.VerticalContentAlignment = VerticalAlignment.Center;
                    //msg1.Foreground = Brushes.Red;
                    //msg1.Background = Brushes.White;

                    msg2.Message = "Please run after changing to manual mode.";
                    msg2.Fontsize = 16;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Blue;
                    msg2.Background = Brushes.White;

                    //msg3.Message = "";
                    //msg3.Fontsize = 16;
                    //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                    //msg3.VerticalContentAlignment = VerticalAlignment.Center;
                    //msg3.Foreground = Brushes.Blue;
                    //msg3.Background = Brushes.White;

                    if (CheckAccess())
                    {
                        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        selResult = warning.ShowDialog().Value;
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                            warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            selResult = warning.ShowDialog().Value;
                        }));
                    }
                    return;
                }

                if(bHeadType == 0)
                {
                    ManualMarkWindow window = new ManualMarkWindow();
                    window.ShowDialog();
                }
                else
                {
                    ManualMarkWindow3 window = new ManualMarkWindow3();
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_ExecuteManualPrint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            SetControllerWindow window = new SetControllerWindow();
            window.ShowDialog();
        }

        private void MenuItem_SearchDeletedData_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "MenuItem_SearchDeletedData_Click";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                SearchDeleteWindow window = new SearchDeleteWindow();
                window.Show();

                this.Focus();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private async void btnMove2CompleteData_Click(object sender, RoutedEventArgs e)
        //{
        //    string className = "MainWindow";
        //    string funcName = "btnMove2CompleteData_Click";

        //    DataRowView row = null;
        //    DateTime dt = DateTime.Now;
        //    MESReceivedData recvMsg = new MESReceivedData();
        //    try
        //    {
        //        row = dgdPlanData.SelectedItem as DataRowView;
        //        DateTime tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
        //        recvMsg.productdate = tmp.ToString("yyyyMMdd");
        //        recvMsg.sequence = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
        //        recvMsg.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
        //        recvMsg.bodyno = row.Row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
        //        recvMsg.vin = row.Row.ItemArray[Constants.DB_NAME_VIN].ToString();

        //        tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
        //        recvMsg.mesdate = tmp.ToString("yyyy-MM-dd");

        //        tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
        //        recvMsg.mestime = tmp.ToString("HH:mm:ss");

        //        recvMsg.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
        //        recvMsg.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
        //        recvMsg.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
        //        recvMsg.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
        //        recvMsg.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
        //        recvMsg.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
        //        recvMsg.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
        //        recvMsg.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
        //        recvMsg.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
        //        recvMsg.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
        //        recvMsg.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

        //        recvMsg.markdate = dt.ToString("yyyy-MM-dd");
        //        recvMsg.marktime = dt.ToString("HH:mm:ss");
        //        recvMsg.remark = row.Row.ItemArray[Constants.DB_NAME_REMARK].ToString();
        //        recvMsg.exist = row.Row.ItemArray[Constants.DB_NAME_EXIST].ToString();

        //        //SaveCompleteData(recvMsg, 0).Wait();
        //        SaveMarkResultData(recvMsg, 1, 0);
        //        UpdateSelectedPlanData(recvMsg);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private void btnShowErrorList_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "btnShowErrorList_Click";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                ErrorListWindow window = new ErrorListWindow();
                window.ShowDialog();
                this.Focus();
                //ShowErrorAlarmList();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void MenuItem_SettingPLC_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "MenuItem_SettingPLC_Click";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {

            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void MenuItem_TestPLC_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "MenuItem_TestPLC_Click";
            PLCTestWindow window = new PLCTestWindow();
            PLCTestWindow02 window2 = new PLCTestWindow02();
            string value = "";
            int plcType = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                Util.GetPrivateProfileValue("PLCCOMM", "PLCTYPE", "1", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out plcType);
                if (plcType == (int)PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_AB)
                    window2.Show();
                else if (plcType == (int)PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_MELSEC)
                {
                    window.Show();
                }
                else if (plcType == (int)PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_SIMENS)
                {
                    window.Show();
                }
                else if (plcType == (int)PLC_DEVICE_TYPE.PLC_DEVICE_TYPE_LS)
                    window.Show();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        //public void OnLaserSourceDataReceivedEventFunc(object sender, LaserSourceControllerEvnetArgs e)
        //{
        //    string className = "MainWindow";
        //    string funcName = "OnLaserSourceDataReceivedEventFunc";
        //    Brush brushesEmission = Brushes.Black;
        //    Brush brushesAiming = Brushes.Black;
        //    Brush brushesBorder = Brushes.Black;
        //    LASERSTATUS Status = 0;
        //    UInt32 ists = 0;
        //    string errstring = "";

        //    try
        //    {
        //        if (e.execResult == 0)
        //        {
        //            if (e.laserStatus.Length > 0)
        //            {
        //                if (e.laserStatus == laserStatusBack)
        //                {
        //                    return;
        //                }

        //                laserStatusBack = e.laserStatus;

        //                UInt32.TryParse(e.laserStatus, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ists);
        //                Status = (LASERSTATUS)ists;

        //                errstring = ists.ToString("X");

        //                if (errstring != laserStatusBack)
        //                {
        //                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("LASER STATUS CHANGED : {0} -> {1}", laserStatusBack, errstring), Thread.CurrentThread.ManagedThreadId);
        //                    laserStatusBack = errstring;
        //                    ShowLabelData(errstring, lblLaserStatusValue);
        //                }

        //                if ((ists & (uint)LASERSTATUS.StatusError) != 0)
        //                {
        //                    ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);
        //                }
        //                else
        //                {
        //                    ShowLabelData("NORMAL", lblLaserStatus, Brushes.Black, Brushes.Green);
        //                }

        //                brushesBorder = (brushesBorder != Brushes.Black) ? Brushes.Black : Brushes.LightGreen;
        //                if ((Status & LASERSTATUS.StatusNormalOn) != LASERSTATUS.StatusNormalOn)
        //                {
        //                    brushesBorder = (brushesBorder != Brushes.Black) ? Brushes.Black : Brushes.OrangeRed;
        //                }
        //                if ((Status & LASERSTATUS.StatusNormalOff) != 0)
        //                {
        //                    brushesBorder = (brushesBorder != Brushes.Black) ? Brushes.Black : Brushes.LightBlue;
        //                }

        //                if ((Status & LASERSTATUS.EmissionOnOff) != 0) brushesEmission = Brushes.Red; else brushesEmission = Brushes.Black;
        //                ShowRectangle(brushesEmission, EmissionLamp);

        //                if ((Status & LASERSTATUS.AimingBeamOnOff) != 0) brushesAiming = Brushes.Red; else brushesAiming = Brushes.Black;
        //                ShowRectangle(brushesAiming, AimingLamp);

        //                ShowLabelData(errstring, lblLastErrorValue);
        //            }

        //            if ((e.avgPower != "Low") && (e.avgPower.Length > 0))
        //                ShowLabelData(e.avgPower, lblAvgPowerValue);
        //            //else
        //            //    ShowLabelData("Low", lblAvgPowerValue);

        //            if ((e.peakPower != "Low") && (e.peakPower.Length > 0))
        //                ShowLabelData(e.peakPower, lblPeakPowerValue);
        //            //else
        //            //    ShowLabelData("Low", lblPeakPowerValue);

        //            if (e.temperature.Length > 0)
        //                ShowLabelData(e.temperature, lblTemperatureValue);
        //        }
        //        }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        //return ex.HResult;
        //    }
        //}

        private void ShowRectangle(Brush brush, System.Windows.Shapes.Rectangle rect)
        {
            if (rect.CheckAccess())
            {
                rect.Fill = brush;
            }
            else
            {
                rect.Dispatcher.Invoke(new Action(delegate
                {
                    rect.Fill = brush;
                }));
            }
        }

        private void btnSetMarkPoint_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeletePlanData_Click(object sender, RoutedEventArgs e)
        {

        }


        private void MenuItem_Change2DB_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            MESReceivedData mesData = new MESReceivedData();
            MESReceivedData[] receivedMsg = new MESReceivedData[512];
            int count = 0;
            int retval = 0;
            int idx = 1;

            try
            {
                string curDir = AppDomain.CurrentDomain.BaseDirectory;
                string path = curDir + "Data";
                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists == false)
                {
                    return;
                }

                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "DATA";             // Default file name
                dlg.DefaultExt = ".dat";                // Default file extension
                dlg.Filter = "DATA (.dat)|*.dat";    // Filter files by extension
                dlg.InitialDirectory = path;

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    // Open document
                    string filename = dlg.FileName;
                    filename.ToUpper();
                    string[] lines = System.IO.File.ReadAllLines(filename);
                    if (filename.Contains("Next.dat"))
                    {
                        foreach (string line in lines)
                        {
                            AnalyzeNSaveFileData(line, 1, 1);
                        }
                    }
                    else if (filename.Contains("CCR.DAT"))
                    {
                        foreach (string line in lines)
                        {
                            AnalyzeNSaveFileDataCCR(line, 1, 1);
                        }
                    }
                    else if (filename.Contains("COMPLETE.DB"))
                    {

                    }
                    else if (filename.Contains("kaga.dat"))
                    {
                        foreach (string line in lines)
                        {
                            AnalyzeNSaveKAGAData(line, 1, 1);
                        }
                    }
                    else if (filename.Contains("vin_main.dat"))
                    {
                        //foreach (string line in lines)
                        //{
                        //    AnalyzeMESReceivedData_Alogorithm02(line, 0, ref mesData);
                        //}
                        for (int i = 0; i < receivedMsg.Length; i++)
                            receivedMsg[i] = new MESReceivedData();

                        foreach (string line in lines)
                        {
                            retval = AnalyzeMESReceivedData_Alogorithm02(line, 0, ref receivedMsg[count]);
                            count++;
                            if (count >= receivedMsg.Length)
                            {
                                //retval = SaveMESClientReceivedData2(newtableName, orderdate + recorddata, receivedMsg, count);
                                retval = SaveMESClientReceivedData2("plantable", /*line, */receivedMsg, count);
                                if (retval != 0)
                                {
                                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                    return;
                                }
                                count = 0;
                            }
                        }

                        if (count >= 0)
                        {
                            retval = SaveMESClientReceivedData2("plantable", /*"", */receivedMsg, count);
                            if (retval != 0)
                            {
                                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                return;
                            }
                            count = 0;
                        }
                    }
                    else if (filename.Contains("HMI3.dat"))
                    {
                        //foreach (string line in lines)
                        //{
                        //    AnalyzeMESReceivedData_Alogorithm02(line, 0, ref mesData);
                        //}
                        for (int i = 0; i < receivedMsg.Length; i++)
                            receivedMsg[i] = new MESReceivedData();

                        foreach (string line in lines)
                        {
                            retval = AnalyzeMESReceivedData_Alogorithm_HMI_3(line, 0, ref receivedMsg[count], idx++);
                            count++;
                            if (count >= receivedMsg.Length)
                            {
                                //retval = SaveMESClientReceivedData2(newtableName, orderdate + recorddata, receivedMsg, count);
                                retval = SaveMESClientReceivedData2("plantable", /*line, */receivedMsg, count);
                                if (retval != 0)
                                {
                                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                    return;
                                }
                                count = 0;
                            }
                        }

                        if (count >= 0)
                        {
                            retval = SaveMESClientReceivedData2("plantable", /*"", */receivedMsg, count);
                            if (retval != 0)
                            {
                                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                return;
                            }
                            count = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private async Task AnalyzeNSaveKAGAData(string msg, byte skipflag, byte showdataflag)
        {
            string className = "MainWindow";
            string funcName = "AnalyzeReceiveServerData";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int idx = 0;
            int length = 0;
            DateTime dt = DateTime.Now;
            string value = "";
            //string rawtype = "";
            MESReceivedData args = new MESReceivedData();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();

            //$ORD_DT[0, 6] + $BODY_PLAN_SEQ[0, 4] + $CAR_TYPE[0, 4] + $BODY_NO[0, 6] + "  " + "L" + #AS_ORD[0,1] + $SIDE_E[0,4] + $FLR_E[0,4] + $CRP_E[0,4] +
            //$CRP_E[0,4] + $MOV_E[0,4] + $MOV_E[0,4] + "    " + #MODEL[0,4] +

            //7                 LHD           AT                   2WD        SSR 
            //#BODY_TYPE[0,1] + #DRIVE[0,3] + #TRANSMISSION[1,2] + #WD[0,3] + #SUN_ROOF[0,4] +

            //R/R               Y               N               N            N                    "  "
            //#ROOF_RACK[0,3] + #COOLING[0,1] + #SPOILER[0,1] + #HMSL[0,1] + #GARNISH_HOLE[0,1] + #AS_MON[0,2] + "                   " +
            //#VIN19[0,19] + #WOOFER[0,3] + #CURTAIN_HOLE[0,1] + #POWER_LIFT[0,1]
            string dbdisplay = "";
            string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                length = 6;
                if (msg.Length >= (length + idx))
                    args.productdate = "20" + msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.sequence = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.bodytype = msg.Substring(idx, length);
                args.bodytype.Trim();
                idx += length;

                length = 6;
                if (msg.Length >= (length + idx))
                    args.bodyno = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 16;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.rawcartype = msg.Substring(idx, length);
                idx += length;

                length = 8;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.cartype = msg.Substring(idx, length);
                idx += length;

                length = 41;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 19;
                if (msg.Length >= (length + idx))
                {
                    args.markvin = msg.Substring(idx, length);
                    args.rawvin = msg.Substring(idx, length);
                }
                idx += length;

                //args.productdate = dt.ToString("yyyy-MM-dd");
                args.mesdate = dt.ToString("yyyy-MM-dd");
                args.mestime = dt.ToString("HH:mm:ss");

                args.markdate = dt.ToString("yyyy-MM-dd");
                args.marktime = dt.ToString("HH:mm:ss");

                if (skipflag != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, Thread.CurrentThread.ManagedThreadId);

                    //lock (DBLock)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK", Thread.CurrentThread.ManagedThreadId);
                        DataTable dbMainDataTable = new DataTable();
                        //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
                        string searchstring = string.Format("select * from " + tableName + " where RAWVIN='" + args.rawvin + "'");
                        dbwrap.ExecuteCommand(Constants.connstring, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.Open(Constants.connstring);
                        //db.CommandText = searchstring;
                        //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
                        if (dbMainDataTable.Rows.Count > 0)
                        {
                            return;
                        }

                        dbdisplay = MakeDBDisplayText(tableName);
                        string insertstring = "INSERT INTO " + tableName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                          args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.markvin + "','" + args.mesdate + "','" + args.mestime + "','" +
                          args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
                          args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" +
                          args.isMarked + "','N','" + args.exist + "',0)";

                        dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                        dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        if (dgdPlanData.CheckAccess())
                        {
                            dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                            dgdPlanData.Items.Refresh();
                            if (dgdPlanData.Items.Count > 0)
                            {
                                //dgdPlanData.SelectedIndex = 0;
                                dgdPlanData.UpdateLayout();
                            }
                        }
                        else
                        {
                            dgdPlanData.Dispatcher.Invoke(new Action(delegate
                            {
                                dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                                dgdPlanData.Items.Refresh();
                                if (dgdPlanData.Items.Count > 0)
                                {
                                    //dgdPlanData.SelectedIndex = 0;
                                    dgdPlanData.UpdateLayout();
                                }
                            }));
                        }
                        //db.Close();
                    }
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        private async Task AnalyzeNSaveFileData(string msg, byte skipflag, byte showdataflag)
        {
            string className = "MainWindow";
            string funcName = "AnalyzeReceiveServerData";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int idx = 0;
            int length = 0;
            DateTime dt = DateTime.Now;
            //string seq = "";
            string rawtype = "";
            string vin = "";
            //string plcvalue = "";
            //string rawregion = "";
            //string bodytype = "";
            MESReceivedData args = new MESReceivedData();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();
            string dbdisplay = "";

            string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                length = 4;
                if (msg.Length >= (length + idx))
                    args.sequence = msg.Substring(idx, length);
                idx += length;
                idx++;

                length = 19;
                if (msg.Length >= (length + idx))
                {
                    args.rawvin = msg.Substring(idx, length);
                    args.markvin = AddMonthCode(msg.Substring(idx, length));
                }
                idx += length;
                idx++;

                length = 3;
                if (msg.Length >= (length + idx))
                    args.rawcartype = msg.Substring(idx, length);
                rawtype += " ";
                idx += length;

                length = 1;
                if (msg.Length >= (length + idx))
                    args.rawbodytype = msg.Substring(idx, length);
                idx += length;
                idx++;

                length = 3;
                if (msg.Length >= (length + idx))
                    args.idplate = msg.Substring(idx, length);
                idx += length;
                idx++;

                length = 1;
                if (msg.Length >= (length + idx))
                    args.plcvalue = msg.Substring(idx, length);
                idx += length;

                args.productdate = dt.ToString("yyyy-MM-dd");
                args.mesdate = dt.ToString("yyyy-MM-dd");
                args.mestime = dt.ToString("HH:mm:ss");

                args.markdate = dt.ToString("yyyy-MM-dd");
                args.marktime = dt.ToString("HH:mm:ss");

                if (skipflag != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock", Thread.CurrentThread.ManagedThreadId);

                    //lock (DBLock)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK", Thread.CurrentThread.ManagedThreadId);
                        DataTable dbMainDataTable = new DataTable();
                        //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
                        string searchstring = string.Format("select * from " + tableName + " where VIN='" + args.markvin + "'");

                        dbwrap.ExecuteCommand(Constants.connstring, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.Open(Constants.connstring);
                        //db.CommandText = searchstring;
                        //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
                        if (dbMainDataTable.Rows.Count > 0)
                        {
                            return;
                        }

                        string insertstring = "INSERT INTO " + tableName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                          args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.markvin + "','" + args.mesdate + "','" + args.mestime + "','" +
                          args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
                          args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" +
                          args.isMarked + "','N','" + args.exist + "',0)";

                        dbdisplay = MakeDBDisplayText(tableName);

                        dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                        dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.CommandText = insertstring;
                        //int retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);

                        ////db.CommandText = "SELECT * from plantable ORDER BY NO ASC";
                        //db.CommandText = DBDisplayCommand;
                        ////db.CommandText = "SELECT * from plantable ORDER BY VIN ASC, SEQUENCE ASC";
                        //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);

                        if (dgdPlanData.CheckAccess())
                        {
                            dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                            dgdPlanData.Items.Refresh();
                            if (dgdPlanData.Items.Count > 0)
                            {
                                //dgdPlanData.SelectedIndex = 0;
                                dgdPlanData.UpdateLayout();
                            }
                        }
                        else
                        {
                            dgdPlanData.Dispatcher.Invoke(new Action(delegate
                            {
                                dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                                dgdPlanData.Items.Refresh();
                                if (dgdPlanData.Items.Count > 0)
                                {
                                    //dgdPlanData.SelectedIndex = 0;
                                    dgdPlanData.UpdateLayout();
                                }
                            }));
                        }
                        //db.Close();
                    }
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        private async Task AnalyzeNSaveFileDataCCR(string msg, byte skipflag, byte showdataflag)
        {
            string className = "MainWindow";
            string funcName = "AnalyzeNSaveFileDataCCR";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int idx = 0;
            int length = 0;
            DateTime dt = DateTime.Now;
            string rawtype = "";
            string vin = "";
            MESReceivedData args = new MESReceivedData();
            DateTime datetime;
            string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
            string syear, smonth, sday, shour, sminute, ssecond;
            int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();
            string dbdisplay = "";

            try
            {
                length = 4;
                if (msg.Length >= (length + idx))
                {
                    syear = msg.Substring(idx, length);
                    int.TryParse(syear, out year);
                }
                idx += length;

                length = 2;
                if (msg.Length >= (length + idx))
                {
                    smonth = msg.Substring(idx, length);
                    int.TryParse(smonth, out month);
                }
                idx += length;

                length = 2;
                if (msg.Length >= (length + idx))
                {
                    sday = msg.Substring(idx, length);
                    int.TryParse(sday, out day);
                }
                idx += length;

                length = 2;
                if (msg.Length >= (length + idx))
                {
                    shour = msg.Substring(idx, length);
                    int.TryParse(shour, out hour);
                }
                idx += length;

                length = 2;
                if (msg.Length >= (length + idx))
                {
                    sminute = msg.Substring(idx, length);
                    int.TryParse(sminute, out minute);
                }
                idx += length;

                length = 2;
                if (msg.Length >= (length + idx))
                {
                    ssecond = msg.Substring(idx, length);
                    int.TryParse(ssecond, out second);
                }
                idx += length;
                idx++;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.sequence = msg.Substring(idx, length);
                idx += length;
                idx++;

                length = 19;
                if (msg.Length >= (length + idx))
                {
                    args.rawvin = msg.Substring(idx, length);
                    args.marktime = AddMonthCode(args.rawvin);
                }
                idx += length;
                idx++;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.rawcartype = msg.Substring(idx, length);
                idx += length;

                length = 1;
                if (msg.Length >= (length + idx))
                    args.rawbodytype = msg.Substring(idx, length);
                idx += length;
                idx++;

                length = 3;
                if (msg.Length >= (length + idx))
                    args.idplate = msg.Substring(idx, length);
                idx += length;
                idx++;

                length = 1;
                if (msg.Length >= (length + idx))
                    args.plcvalue = msg.Substring(idx, length);
                idx += length;

                datetime = new DateTime(year, month, day, hour, minute, second);
                args.productdate = datetime.ToString("yyyy-MM-dd");
                args.mesdate = datetime.ToString("yyyy-MM-dd");
                args.mestime = datetime.ToString("HH:mm:ss");

                args.markdate = dt.ToString("yyyy-MM-dd");
                args.marktime = dt.ToString("HH:mm:ss");

                if (skipflag != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock", Thread.CurrentThread.ManagedThreadId);

                    //lock (DBLock)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK", Thread.CurrentThread.ManagedThreadId);
                        DataTable dbMainDataTable = new DataTable();
                        //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
                        string searchstring = string.Format("select * from " + tableName + " where VIN='" + vin + "'");
                        dbwrap.ExecuteCommand(Constants.connstring, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.Open(Constants.connstring);
                        //db.CommandText = searchstring;
                        //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
                        if (dbMainDataTable.Rows.Count > 0)
                        {
                            return;
                        }
                        dbdisplay = MakeDBDisplayText(tableName);
                        string insertstring = "INSERT INTO " + tableName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                          args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.markvin + "','" + args.mesdate + "','" + args.mestime + "','" +
                          args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
                          args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" +
                          args.isMarked + "','N','" + args.exist + "',0)";

                        dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                        dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        if (dgdPlanData.CheckAccess())
                        {
                            dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                            dgdPlanData.Items.Refresh();
                            if (dgdPlanData.Items.Count > 0)
                            {
                                //dgdPlanData.SelectedIndex = 0;
                                dgdPlanData.UpdateLayout();
                            }
                        }
                        else
                        {
                            dgdPlanData.Dispatcher.Invoke(new Action(delegate
                            {
                                dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                                dgdPlanData.Items.Refresh();
                                if (dgdPlanData.Items.Count > 0)
                                {
                                    //dgdPlanData.SelectedIndex = 0;
                                    dgdPlanData.UpdateLayout();
                                }
                            }));
                        }
                        //db.Close();
                    }
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        /*
         *  |0  |0  |CAR_TYPE|BODY_NO|CMT_NO|ORD_DATA                                                                                      |'N'|PROD_DT |IDX_NO|REG_DATE      |
            |---|---|--------|-------|------|----------------------------------------------------------------------------------------------|---|--------|------|--------------|
            |0  |0  |HWW     |322348 |3204  |202406113204115111151116111511111111NQ WGNDCTSUNEXP-U5YPX81AGSL322348-;                       |N  |20240611|4,488 |20240611130338|
            |0  |0  |JYW     |183575 |3205  |202406113205156115251521152515211521NQ WGNAT6SUNEXP-U5YPX81GMSL183575-;                       |N  |20240611|4,489 |20240611130338|
         * */
        //private async Task AnalyzeNSaveFileData4KASK(string msg, byte skipflag, byte showdataflag)
        //{
        //    string className = "MainWindow";
        //    string funcName = "AnalyzeReceiveServerData";
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    int idx = 0;
        //    int length = 0;
        //    DateTime dt = DateTime.Now;
        //    //string seq = "";
        //    string rawtype = "";
        //    string vin = "";
        //    //string plcvalue = "";
        //    //string rawregion = "";
        //    //string bodytype = "";
        //    MESReceivedData args = new MESReceivedData();
        //    ITNTDBWrapper dbwrap = new ITNTDBWrapper();
        //    object obj = new DataTable();

        //    string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
        //    try
        //    {
        //        length = 4;
        //        if (msg.Length >= (length + idx))
        //            args.sequence = msg.Substring(idx, length);
        //        idx += length;
        //        idx++;

        //        length = 19;
        //        if (msg.Length >= (length + idx))
        //            args.vin = msg.Substring(idx, length);
        //        idx += length;
        //        idx++;

        //        length = 3;
        //        if (msg.Length >= (length + idx))
        //            args.rawcartype = msg.Substring(idx, length);
        //        rawtype += " ";
        //        idx += length;

        //        length = 1;
        //        if (msg.Length >= (length + idx))
        //            args.rawbodytype = msg.Substring(idx, length);
        //        idx += length;
        //        idx++;

        //        length = 3;
        //        if (msg.Length >= (length + idx))
        //            args.idplate = msg.Substring(idx, length);
        //        idx += length;
        //        idx++;

        //        length = 1;
        //        if (msg.Length >= (length + idx))
        //            args.plcvalue = msg.Substring(idx, length);
        //        idx += length;

        //        args.productdate = dt.ToString("yyyy-MM-dd");
        //        args.mesdate = dt.ToString("yyyy-MM-dd");
        //        args.mestime = dt.ToString("HH:mm:ss");

        //        args.markdate = dt.ToString("yyyy-MM-dd");
        //        args.marktime = dt.ToString("HH:mm:ss");

        //        if (skipflag != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock", Thread.CurrentThread.ManagedThreadId);

        //            //lock (DBLock)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK", Thread.CurrentThread.ManagedThreadId);
        //                DataTable dbMainDataTable = new DataTable();
        //                //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
        //                string searchstring = string.Format("select * from " + tableName + " where VIN='" + vin + "'");

        //                dbwrap.ExecuteCommand(Constants.connstring, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

        //                //db.Open(Constants.connstring);
        //                //db.CommandText = searchstring;
        //                //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
        //                if (dbMainDataTable.Rows.Count > 0)
        //                {
        //                    return;
        //                }

        //                string insertstring = "INSERT INTO " + tableName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
        //                  args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.vin + "','" + args.mesdate + "','" + args.mestime + "','" +
        //                  args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
        //                  args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" +
        //                  args.isMarked + "','N','" + args.exist + "',0)";

        //                dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
        //                dbwrap.ExecuteCommand(Constants.connstring, DBDisplayCommand, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

        //                //db.CommandText = insertstring;
        //                //int retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);

        //                ////db.CommandText = "SELECT * from plantable ORDER BY NO ASC";
        //                //db.CommandText = DBDisplayCommand;
        //                ////db.CommandText = "SELECT * from plantable ORDER BY VIN ASC, SEQUENCE ASC";
        //                //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);

        //                if (dgdPlanData.CheckAccess())
        //                {
        //                    dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
        //                    dgdPlanData.Items.Refresh();
        //                    if (dgdPlanData.Items.Count > 0)
        //                    {
        //                        //dgdPlanData.SelectedIndex = 0;
        //                        dgdPlanData.UpdateLayout();
        //                    }
        //                }
        //                else
        //                {
        //                    dgdPlanData.Dispatcher.Invoke(new Action(delegate
        //                    {
        //                        dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
        //                        dgdPlanData.Items.Refresh();
        //                        if (dgdPlanData.Items.Count > 0)
        //                        {
        //                            //dgdPlanData.SelectedIndex = 0;
        //                            dgdPlanData.UpdateLayout();
        //                        }
        //                    }));
        //                }
        //                //db.Close();
        //            }
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
        //        }
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //    }
        //    catch (DataException de)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return;
        //    }
        //}

        /// <summary>
        /// For KaSK
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="orderLength"></param>
        /// <param name="retMsg"></param>
        private int AnalyzeMESReceivedData_Alogorithm06(string msg, int orderLength, CarTypeOption optFlag, ref MESReceivedData retMsg, int no2)
        {
            /*
                JYW 195492;202408126363152515211521152115211521NQ WGNAT6GEMEXP-U5YPX81GMSL195492-;
                J7S 089418;202408126364577157265721572657235721CD SB DCTSUNEXP-U5YH2G15VSL089418-;
             */

            int idx = 0;
            int length = 0;
            string value = "";
            string tmpVIN = "";
            int retval = -1;
            System.DateTime dt;
            //string[] vals;

            try
            {
                //if(msg.Length > 30)
                //{
                //    ////bodyno
                //    length = 10;
                //    if (msg.Length >= (length + idx))
                //        retMsg.bodyno = msg.Substring(idx, length);
                //    idx += length;

                //}
                //else
                //{
                //    if(msg.Substring(0, 3) == "OVR")
                //    {

                //    }
                //}
                //if(orderLength >= 8)
                //{
                //    //order date
                //    length = 8;
                //    if (msg.Length >= (length + idx))
                //        retMsg.productdate = msg.Substring(idx, length);
                //    idx += length;

                //    length = 8;
                //}

                ////Body Number (10)
                length = 10;
                retMsg.bodyno = msg.Substring(idx, length);
                idx += length;
                idx++; //;

                //order date
                length = 8;
                if (msg.Length >= (length + idx))
                    retMsg.productdate = msg.Substring(idx, length);
                idx += length;

                ////Commit (Body Plan)
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.sequence = msg.Substring(idx, length);
                idx += length;


                //temp 4 digits
                length = 4;
                idx += length;


                ////raw car type
                length = 4;
                if (msg.Length >= (length + idx))
                    retMsg.rawcartype = msg.Substring(idx, length).Trim();
                idx += length;

                idx += 16;

                /////Car Name (NQ, CD)
                length = 3;
                if (msg.Length >= (length + idx))
                    retMsg.cartype = msg.Substring(idx, length);
                idx += length;
                //retMsg.cartype = retMsg.cartype.Trim(' ');// msg.Substring(idx, length);

                retMsg.cartype = GetCarTypeFromNumber(retMsg.rawcartype);

                //if (retMsg.cartype == "NQ")
                //{
                //    if (retMsg.rawcartype.Length > 2)
                //    {
                //        string tmp = retMsg.rawcartype.Substring(2, 1);
                //        if ((tmp == "3") || (tmp == "7"))
                //            retMsg.cartype = "NQPHEV";
                //    }
                //    else
                //        retMsg.cartype = "NQ";
                //}

                idx += 12;

                //////body type
                //length = 4;
                //if (msg.Length >= (length + idx))
                //    retMsg.bodytype = msg.Substring(idx, length);
                //retMsg.bodytype.Trim();
                //idx += length;


                //length = 4;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;

                //length = 16;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;


                //length = 8;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;


                //length = 41;
                //if (msg.Length >= (length + idx))
                //    value = msg.Substring(idx, length);
                //idx += length;

                ////vin
                length = 19;
                if (msg.Length >= (length + idx))
                {
                    retMsg.rawvin = msg.Substring(idx, length);
                    retMsg.markvin = AddMonthCode(retMsg.rawvin);
                }
                idx += length;

                dt = System.DateTime.Now;
                retMsg.markdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.marktime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.mesdate = dt.ToString("yyyy-MM-dd"); //dt.ToString("yyyy-MM-dd");
                retMsg.mestime = dt.ToString("HH:mm:ss");// dt.ToString("HH:mm:ss");
                retMsg.remark = "";
                retMsg.exist = "Y";
                retMsg.no2 = no2 * 10;
                retMsg.isInserted = "0";
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

            return 0;
        }

        private async Task AnalyzeNSaveHMCU51Data(string msg, byte skipflag, byte showdataflag)
        {
            string className = "MainWindow";
            string funcName = "AnalyzeNSaveHMCU51Data";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int idx = 0;
            int length = 0;
            DateTime dt = DateTime.Now;
            string value = "";
            //string rawtype = "";
            MESReceivedData args = new MESReceivedData();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();

            //$ORD_DT[0, 6] + $BODY_PLAN_SEQ[0, 4] + $CAR_TYPE[0, 4] + $BODY_NO[0, 6] + "  " + "L" + #AS_ORD[0,1] + $SIDE_E[0,4] + $FLR_E[0,4] + $CRP_E[0,4] +
            //$CRP_E[0,4] + $MOV_E[0,4] + $MOV_E[0,4] + "    " + #MODEL[0,4] +

            //7                 LHD           AT                   2WD        SSR 
            //#BODY_TYPE[0,1] + #DRIVE[0,3] + #TRANSMISSION[1,2] + #WD[0,3] + #SUN_ROOF[0,4] +

            //R/R               Y               N               N            N                    "  "
            //#ROOF_RACK[0,3] + #COOLING[0,1] + #SPOILER[0,1] + #HMSL[0,1] + #GARNISH_HOLE[0,1] + #AS_MON[0,2] + "                   " +
            //#VIN19[0,19] + #WOOFER[0,3] + #CURTAIN_HOLE[0,1] + #POWER_LIFT[0,1]
            string dbdisplay = "";
            string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                length = 6;
                if (msg.Length >= (length + idx))
                    args.productdate = "20" + msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.sequence = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.bodytype = msg.Substring(idx, length);
                args.bodytype.Trim();
                idx += length;

                length = 6;
                if (msg.Length >= (length + idx))
                    args.bodyno = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 16;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.rawcartype = msg.Substring(idx, length);
                idx += length;

                length = 8;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 4;
                if (msg.Length >= (length + idx))
                    args.cartype = msg.Substring(idx, length);
                idx += length;

                length = 41;
                if (msg.Length >= (length + idx))
                    value = msg.Substring(idx, length);
                idx += length;

                length = 19;
                if (msg.Length >= (length + idx))
                {
                    args.rawvin = msg.Substring(idx, length);
                    args.markvin = AddMonthCode(args.rawvin);
                }
                idx += length;

                //args.productdate = dt.ToString("yyyy-MM-dd");
                args.mesdate = dt.ToString("yyyy-MM-dd");
                args.mestime = dt.ToString("HH:mm:ss");

                args.markdate = dt.ToString("yyyy-MM-dd");
                args.marktime = dt.ToString("HH:mm:ss");

                if (skipflag != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, Thread.CurrentThread.ManagedThreadId);

                    //lock (DBLock)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK", Thread.CurrentThread.ManagedThreadId);
                        DataTable dbMainDataTable = new DataTable();
                        //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
                        string searchstring = string.Format("select * from " + tableName + " where VIN='" + args.rawvin + "'");
                        dbwrap.ExecuteCommand(Constants.connstring, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.Open(Constants.connstring);
                        //db.CommandText = searchstring;
                        //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
                        if (dbMainDataTable.Rows.Count > 0)
                        {
                            return;
                        }

                        dbdisplay = MakeDBDisplayText(tableName);

                        string insertstring = "INSERT INTO " + tableName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                          args.productdate + "','" + args.sequence + "','" + args.rawcartype + "','" + args.bodyno + "','" + args.markvin + "','" + args.mesdate + "','" + args.mestime + "','" +
                          args.lastsequence + "','" + args.code219 + "','" + args.idplate + "','" + args.delete + "','" + args.totalmsg + "','" + args.rawbodytype + "','" + args.rawtrim + "','" +
                          args.plcvalue + "','" + args.region + "','" + args.bodytype + "','" + args.cartype + "','" + args.markdate + "','" + args.marktime + "','" + args.remark + "','" +
                          args.isMarked + "','N','" + args.exist + "',0)";

                        dbwrap.ExecuteCommand(Constants.connstring, insertstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                        dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                        //db.CommandText = insertstring;
                        //int retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);

                        ////db.CommandText = "SELECT * from plantable ORDER BY NO ASC";
                        //db.CommandText = DBDisplayCommand;
                        ////db.CommandText = "SELECT * from plantable ORDER BY VIN ASC, SEQUENCE ASC";
                        //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);

                        if (dgdPlanData.CheckAccess())
                        {
                            dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                            dgdPlanData.Items.Refresh();
                            if (dgdPlanData.Items.Count > 0)
                            {
                                //dgdPlanData.SelectedIndex = 0;
                                dgdPlanData.UpdateLayout();
                            }
                        }
                        else
                        {
                            dgdPlanData.Dispatcher.Invoke(new Action(delegate
                            {
                                dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                                dgdPlanData.Items.Refresh();
                                if (dgdPlanData.Items.Count > 0)
                                {
                                    //dgdPlanData.SelectedIndex = 0;
                                    dgdPlanData.UpdateLayout();
                                }
                            }));
                        }
                        //db.Close();
                    }
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        private void MenuItem_SetController_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "MenuItem_SetController_Click";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            MESReceivedData mesData = new MESReceivedData();
            string patternName = "";
            //string value = "";
            try
            {
                EnterPWWindow enterpw = new EnterPWWindow();
                enterpw.Owner = System.Windows.Application.Current.MainWindow;
                enterpw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (enterpw.ShowDialog() == false)
                {
                    return;
                }

                GetMarkDataInfomation(dgdPlanData, ref mesData);
                //patternName = GetPatternName(mesData);
                patternName = GetPatternName(mesData.rawcartype, mesData.rawbodytype, mesData.rawtrim);

                if (bHeadType == 0)
                {
                    SetControllerWindow window = new SetControllerWindow(patternName, mesData.markvin);
                    window.ShowDialog();
                }
                else
                {
#if LASER_YLR_PULSEMODE
                    SetControllerWindow2 window = new SetControllerWindow2(patternName, mesData.vin);
#else
                    SetControllerWindow3 window = new SetControllerWindow3(patternName, mesData.markvin);
#endif
                    window.Show();
                }
                //                this.Focus();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task Move2Complete()
        {
            string className = "MainWindow";
            string funcName = "Move2Complete";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            bool ret = false;
            string markvin = "";
            string rawvin = "";
            string seq = "";

            DataRowView row = null;
            DataRowView rowNext = null;
            DateTime dt = DateTime.Now;
            MESReceivedData recvMsg = new MESReceivedData();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();
            DataTable dbMainDataTable = new DataTable();
            //ITNTDBManage db = new ITNTDBManage();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock", Thread.CurrentThread.ManagedThreadId);

                if (dgdPlanData.Items.Count <= 0)
                    return;

                row = dgdPlanData.SelectedItem as DataRowView;
                rawvin = row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                seq = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                markvin = AddMonthCode(rawvin);

                //if (row.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() == "Y")
                //{
                //    msg2.Message = "COMPLETION DATA FAILURE";
                //    msg2.Fontsize = 18;
                //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg2.Foreground = Brushes.Blue;
                //    msg2.Background = Brushes.White;

                //    msg3.Message = "이미 각인 완료된 데이터입니다.";
                //    msg3.Fontsize = 18;
                //    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg3.Foreground = Brushes.Red;
                //    msg3.Background = Brushes.White;

                //    WarningWindow warning1 = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                //    warning1.ShowDialog();
                //    return;
                //}

                //if (row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() == "DLT")
                //{
                //    msg2.Message = "각인 완료 처리 ERROR";
                //    msg2.Fontsize = 18;
                //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg2.Foreground = Brushes.Blue;
                //    msg2.Background = Brushes.White;

                //    msg3.Message = "이미 삭제된 데이터입니다.";
                //    msg3.Fontsize = 18;
                //    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg3.Foreground = Brushes.Red;
                //    msg3.Background = Brushes.White;

                //    WarningWindow warning1 = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                //    warning1.ShowDialog();
                //    return;
                //}

                //if (row.Row.ItemArray[Constants.DB_NAME_ISMARK].ToString() == "Y")
                //{
                //    msg2.Message = "각인 완료 처리 ERROR";
                //    msg2.Fontsize = 18;
                //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg2.Foreground = Brushes.Blue;
                //    msg2.Background = Brushes.White;

                //    msg3.Message = "이미 각인 완료된 데이터입니다.";
                //    msg3.Fontsize = 18;
                //    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg3.Foreground = Brushes.Red;
                //    msg3.Background = Brushes.White;

                //    WarningWindow warning1 = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                //    warning1.ShowDialog();
                //    return;
                //}

                msg2.Message = "Do you want to move to marking completion?";// (" + seq + ", " + vin + ")?";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                msg3.Message = "(SEQ = " + seq + ", VIN = " + rawvin + ")";
                msg3.Fontsize = 18;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Red;
                msg3.Background = Brushes.White;

                //msg4.Message = "재 설정이 필요하시면[NO]를 선택하세요.";
                //msg4.Fontsize = 16;
                //msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg4.VerticalContentAlignment = VerticalAlignment.Center;
                //msg4.Foreground = Brushes.Blue;
                //msg4.Background = Brushes.White;
                //bool ret = false;

                WarningWindow warning = new WarningWindow("DATA COMPLETED", msg1, msg2, msg3, msg4, msg5, "YES", "NO", this, 1);
                warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ret = warning.ShowDialog().Value;
                if (!ret)
                    return;

                string check = row.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString();
                int icheck = 0;
                int.TryParse(check, out icheck);
                if (icheck != 0)
                {
                    rowNext = GetNextMarkPointData();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED", Thread.CurrentThread.ManagedThreadId);
                    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=0 WHERE CHECKFLAG=1", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    //UpdatePlanDatabase(dgdPlanData, rowNext);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                }

                dt = DateTime.Now;
                //row = dgdPlanData.SelectedItem as DataRowView;
                DateTime tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                recvMsg.productdate = tmp.ToString("yyyy-MM-dd");
                recvMsg.sequence = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                recvMsg.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                recvMsg.bodyno = row.Row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                recvMsg.rawvin = GetRawVIN(row.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                //recvMsg.rawvin = row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                recvMsg.markvin = AddMonthCode(recvMsg.rawvin);

                tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                recvMsg.mesdate = tmp.ToString("yyyy-MM-dd");

                tmp = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                recvMsg.mestime = tmp.ToString("HH:mm:ss");

                recvMsg.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                recvMsg.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                recvMsg.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                recvMsg.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                recvMsg.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                recvMsg.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                recvMsg.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                recvMsg.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                recvMsg.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                recvMsg.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                recvMsg.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();

                recvMsg.markdate = dt.ToString("yyyy-MM-dd");
                recvMsg.marktime = dt.ToString("HH:mm:ss");
                recvMsg.remark = row.Row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                recvMsg.exist = row.Row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                recvMsg.isInserted = row.Row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                //SaveCompleteData(recvMsg, 0).Wait();
                CheckAreaData chkdata = new CheckAreaData();
                SaveMarkResultData(recvMsg, 1, 0, 0, 1, chkdata, 0);
                UpdateSelectedPlanData(recvMsg);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void MenuItem_Move2Complete(object sender, RoutedEventArgs e)
        {
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                if (CheckAccess())
                {
                    //msg2.Message = "Do you want to complete selected data?";
                    //msg2.Fontsize = 18;
                    //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    //msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    //msg2.Foreground = Brushes.Red;
                    //msg2.Background = Brushes.White;

                    //ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
                    //window.Owner = System.Windows.Application.Current.MainWindow;
                    //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnClearPINCount_Click", "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);

                    //if (window.ShowDialog() == false)
                    //    return;

                    if (dgdPlanData.Items.Count <= 0)
                        return;

                    await Move2Complete();
                }
                else
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
                        //msg2.Message = "Do you want to complete selected data?";
                        //msg2.Fontsize = 18;
                        //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                        //msg2.VerticalContentAlignment = VerticalAlignment.Center;
                        //msg2.Foreground = Brushes.Red;
                        //msg2.Background = Brushes.White;

                        //ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
                        //window.Owner = System.Windows.Application.Current.MainWindow;
                        //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnClearPINCount_Click", "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);

                        //if (window.ShowDialog() == false)
                        //    return;

                        if (dgdPlanData.Items.Count <= 0)
                            return;

                        await Move2Complete();
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_SetMarkPoint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////
#region Functions4Thread

        private void ShowMarkingDataList4Thread(bool plandb, bool completedb/*, bool deletedb*/)
        {
            string className = "MainWindow";
            string funcName = "ShowMarkingDataList4Thread";
            //DataTable dplanTable = new DataTable();
            //DataTable dbcompTable = new DataTable();
            //object obj = new object();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //int retval = 0;
            //ITNTDBWrapper dbwrap = new ITNTDBWrapper();

            try
            {
                if (plandb)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DBLock", Thread.CurrentThread.ManagedThreadId);
                    //lock (DBLock)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED", Thread.CurrentThread.ManagedThreadId);
                        //dbwrap.ExecuteCommand(Constants.connstring, DBDisplayCommand, CommandMode.Reader, CommandTypeEnum.Text, ref dplanTable, ref obj);
                        ShowPlanDataList4Thread(dgdPlanData);//, dplanTable);
                    }
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);
                }

                if (completedb)
                {
                    //lock (CompDBLock)
                    {
                        //dbwrap.ExecuteCommand(Constants.connstring_comp, "SELECT * from completetable ORDER BY NO DESC", CommandMode.Reader, CommandTypeEnum.Text, ref dbcompTable, ref obj);
                        ShowCompleteDataList4Thread(CompleteDataGrid);//, dbcompTable);
                    }
                }
            }
            catch (DataException de)
            {
                string error = de.Message;
                int errcode = de.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DB EXCEPTION - CODE = {0:X}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                int errcode = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }


        void ShowPlanDataList4Thread(DataGrid grid, string dispstring="")//, DataTable dt)
        {
            string className = "MainWindow";
            string funcName = "ShowPlanDataList4Thread";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dtable = new DataTable();
            object obj = new object();
            string dbdisplay = "";

            try
            {
                if (dispstring.Length <= 0)
                {
                    dbdisplay = MakeDBDisplayText(tableName);
                    dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dtable, ref obj);
                }
                else
                    dbwrap.ExecuteCommand(Constants.connstring, dispstring, CommandMode.Reader, CommandTypeEnum.Text, ref dtable, ref obj);

                if (grid.CheckAccess())
                {
                    grid.ItemsSource = dtable.DefaultView;
                    grid.Items.Refresh();
                    if (grid.Items.Count > 0)
                    {
                        //grid.SelectedIndex = markNum;
                        grid.UpdateLayout();
                    }
                }
                else
                {
                    grid.Dispatcher.Invoke(new Action(delegate
                    {
                        grid.ItemsSource = dtable.DefaultView;
                        grid.Items.Refresh();
                        if (grid.Items.Count > 0)
                        {
                            //grid.SelectedIndex = markNum;
                            grid.UpdateLayout();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        //private void ShowErrorMessage4Thread(string errormsg, bool clearflag)
        //{
        //    Brush brush;
        //    if (lblError.CheckAccess())
        //    {
        //        if (clearflag)
        //        {
        //            brush = new SolidColorBrush(Color.FromArgb(255, (byte)0xc0, (byte)0xc0, (byte)0xc0));
        //            lblError.Background = brush;
        //            lblError.Content = "";
        //        }
        //        else
        //        {
        //            brush = new SolidColorBrush(Color.FromArgb(125, (byte)0, (byte)0, (byte)0));
        //            lblError.Background = brush;
        //            //lblError.Background = Brushes.Red;
        //            lblError.Content = errormsg;
        //            //SaveErrorDB(errormsg, "").Wait();
        //        }
        //    }
        //    else
        //    {
        //        lblError.Dispatcher.Invoke(new Action(delegate
        //        {
        //            if (clearflag)
        //            {
        //                brush = new SolidColorBrush(Color.FromArgb(255, (byte)0xc0, (byte)0xc0, (byte)0xc0));
        //                lblError.Background = brush;
        //                lblError.Content = "";
        //            }
        //            else
        //            {
        //                lblError.Background = Brushes.Red;
        //                lblError.Content = errormsg;
        //                //SaveErrorDB(errormsg, "").Wait();
        //            }
        //        }));
        //    }
        //}

        private async Task<int> ChangeDBProcess4Thread(/*string vin, string seq*/)
        {
            string className = "MainWindow";
            string funcName = "ChangeDBProcess4Thread";

            int retval = 0;
            string value = "";
            //string oldtbName = "";
            Stopwatch sw = new Stopwatch();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbMainDataTable = new DataTable();
            object obj = new object();
            string newtbName = "";
            string dbdiplay = "";

            //Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref tableName, Constants.PARAMS_INI_FILE);
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                changeDBProcessFlag = true;

                sw.Start();

                if (tableName == "plantable")
                    newtbName = "plantable2";
                else
                    newtbName = "plantable";

                dbwrap.ExecuteCommand(Constants.connstring, "SELECT COUNT(*) FROM " + newtbName, CommandMode.Scalar, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                int count = (int)(long)obj;
                if (count <= 0)
                {
                    //tableName = oldtbName;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NEW TABLE DATA NONE", Thread.CurrentThread.ManagedThreadId);
                    changeDBProcessFlag = false;
                    return -1;
                }

                tableName = newtbName;

                //Util.GetPrivateProfileValue("OPTION", "SHOWDBTEXT", "0", ref value, Constants.PARAMS_INI_FILE);
                //if (value == "1")
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY SEQUENCE ASC, DATE(PRODUCTDATE) ASC";
                //else if (value == "2")
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY NO ASC";
                //else
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";

                dbdiplay = MakeDBDisplayText(tableName);
                //ShowMarkingDataList(true, false);

                ShowPlanDataList4Thread(dgdPlanData, dbdiplay);//, dplanTable);
                GetLastMarkedData(dgdPlanData);
                ShowMarkingDataList4Thread(true, false);

                ScrollViewToPoint(dgdPlanData);

                //int count = GetMarkPlanDataCount2(dgdPlanData);
                int totcount = 0;
                (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                string datetime = DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss");
                ShowWorkPlanCount(lblWorkPlanDataCount, count);
                ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                CheckPlanDataCountWarning(count, lblPlanDataWarning);

                //ShowMarkingDataList4Thread(true, false);

                ////int count = GetMarkPlanDataCount4Thread(dgdPlanData);
                //count = GetCount4MarkPlanData(dgdPlanData);
                //string datetime = DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss");
                //ShowWorkPlanCount4Thread(lblWorkPlanDataCount, count);
                ////ShowMESReceivedTime4Thread(lblVINLastUpdateDate, datetime, true);
                ////ShowWorkPlanCountNTime4Thread(lblWorkPlanDataCount, lblVINLastUpdateDate, count, datetime, false);
                ////DBClear(oldtbName);
                //CheckPlanDataCountWarning(count, lblPlanDataWarning);
                //ScrollViewToPoint(dgdPlanData);

                mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_IDLE;
                value = mesDBUpdateFlag.ToString();
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", value, Constants.PARAMS_INI_FILE);
                Util.WritePrivateProfileValue("OPTION", "TABLENAME", tableName, Constants.PARAMS_INI_FILE);
                sw.Stop();
                changeDBProcessFlag = false;

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END - TIME : " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                changeDBProcessFlag = false;
                retval = ex.HResult;
            }
            changeDBProcessFlag = false;
            return retval;
        }

        void ShowCompleteDataList4Thread(DataGrid grid)//, DataTable dt)
        {
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbcompTable = new DataTable();
            object obj = new object();

            try
            {
                dbwrap.ExecuteCommand(Constants.connstring_comp, "SELECT * from completetable ORDER BY NO DESC", CommandMode.Reader, CommandTypeEnum.Text, ref dbcompTable, ref obj);

                if (grid.CheckAccess())
                {
                    grid.ItemsSource = dbcompTable.DefaultView;
                    grid.Items.Refresh();
                    if (grid.Items.Count > 0)
                    {
                        //grid.SelectedIndex = 0;
                        grid.UpdateLayout();
                    }
                }
                else
                {
                    grid.Dispatcher.Invoke(new Action(delegate
                    {
                        grid.ItemsSource = dbcompTable.DefaultView;
                        grid.Items.Refresh();
                        if (grid.Items.Count > 0)
                        {
                            //grid.SelectedIndex = 0;
                            grid.UpdateLayout();
                        }
                    }));
                }
            }
            catch(Exception ex)
            {

            }

        }
#endregion

        private void NextVINMarkPonit()
        {
            DataTable dbMainDataTable = new DataTable();
            //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
            string nextvin = "";
            string nextseq = "";
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();

            {
                //dgdPlanData.SelectedIndex = nextMarkNum;
                DataRowView row = GetNextMarkPointData();
                nextvin = row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                nextseq = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "NextVINMarkPonit", "DBLock", Thread.CurrentThread.ManagedThreadId);

                //lock (DBLock)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "NextVINMarkPonit", "LOCKED", Thread.CurrentThread.ManagedThreadId);
                    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=0 WHERE CHECKFLAG=1", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE RAWVIN='" + nextvin + "' AND SEQUENCE='" + nextseq + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    dbwrap.ExecuteCommand(Constants.connstring, "SELECT * from " + tableName + " ORDER BY NO ASC", CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                    //db.Open(Constants.connstring);
                    //db.CommandText = "UPDATE plantable SET CHECKFLAG=0 WHERE CHECKFLAG=1";
                    //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                    //db.CommandText = "UPDATE plantable SET CHECKFLAG=1 WHERE VIN='" + nextvin + "'";
                    //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                    //db.CommandText = "SELECT * from plantable ORDER BY NO ASC";
                    ////db.CommandText = "SELECT * from plantable ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
                    //db.ExecuteCommandReader(CommandTypeEnum.Text, ref dbMainDataTable);
                    //db.Close();
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "NextVINMarkPonit", "LOCK END", Thread.CurrentThread.ManagedThreadId);

                if (dgdPlanData.CheckAccess())
                {
                    dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                    dgdPlanData.Items.Refresh();
                    if (dgdPlanData.Items.Count > 0)
                    {
                        //dgdPlanData.SelectedIndex = 0;
                        dgdPlanData.UpdateLayout();
                    }
                }
                else
                {
                    dgdPlanData.Dispatcher.Invoke(new Action(delegate
                    {
                        dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                        dgdPlanData.Items.Refresh();
                        if (dgdPlanData.Items.Count > 0)
                        {
                            //dgdPlanData.SelectedIndex = 0;
                            dgdPlanData.UpdateLayout();
                        }
                    }));
                }
            }
        }

        private async Task DeleteSelectPlanData()
        {
            string className = "MainWindow";
            string funcName = "DeleteSelectPlanData";
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();
            bool ret = false;
            MESReceivedData data = new MESReceivedData();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();
            string dbdisplay = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                DataRowView row = dgdPlanData.SelectedItem as DataRowView;
                DateTime dateValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                data.productdate = dateValue.ToString("yyyy-MM-dd");

                data.sequence = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                data.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                data.bodyno = row.Row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                data.rawvin = GetRawVIN(row.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                data.markvin = AddMonthCode(data.rawvin);
                //data.markvin = row.Row.ItemArray[Constants.DB_NAME_VIN].ToString();
                //data.rawvin = row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();

                //recv.mesdate = row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString();
                //recv.mestime = row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString();
                dateValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                DateTime timeValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                data.mesdate = dateValue.ToString("yyyy-MM-dd");
                data.mestime = timeValue.ToString("HH:mm:ss");

                data.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                data.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                data.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                data.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                data.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                data.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                data.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                data.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                data.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                data.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                data.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();
                data.isInserted = row.Row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                //recv.markdate = row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString();
                //recv.marktime = row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString();
                //dateValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                //timeValue = Convert.ToDateTime(row.Row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());

                data.markdate = DateTime.Now.ToString("yyyy-MM-dd");
                data.marktime = DateTime.Now.ToString("HH:mm:ss");

                data.remark = row.Row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                data.exist = row.Row.ItemArray[Constants.DB_NAME_EXIST].ToString();

                msg2.Message = "Do you want to delete the selected data?"; 
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                msg3.Message = "(" + data.sequence + ", " + data.rawvin + ")";
                msg3.Fontsize = 18;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Red;
                msg3.Background = Brushes.White;

                //msg4.Message = "재 설정이 필요하시면[NO]를 선택하세요.";
                //msg4.Fontsize = 16;
                //msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg4.VerticalContentAlignment = VerticalAlignment.Center;
                //msg4.Foreground = Brushes.Red;
                //msg4.Background = Brushes.White;

                WarningWindow warning = new WarningWindow("DELETE DATA", msg1, msg2, msg3, msg4, msg5, "YES", "NO", this, 1);
                warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ret = warning.ShowDialog().Value;
                if (!ret)
                    return;

                EnterPWWindow enterpw = new EnterPWWindow();
                enterpw.Owner = System.Windows.Application.Current.MainWindow;
                enterpw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (enterpw.ShowDialog() == false)
                    return;

                DataTable dbMainDataTable = new DataTable();
                //ITNTDBManage db = new ITNTDBManage(Constants.connstring);
                DataRowView rowview = GetNextMarkPointData();

                dbdisplay = MakeDBDisplayText(tableName);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCKED", Thread.CurrentThread.ManagedThreadId);
                dbwrap.ExecuteCommand(Constants.connstring, "DELETE FROM " + tableName + " WHERE RAWVIN='" + data.rawvin + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                dbwrap.ExecuteCommand(Constants.connstring, "SELECT COUNT(*) FROM " + tableName + " WHERE CHECKFLAG=1", CommandMode.Scalar, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                int count = (int)(long)obj;
                if ((count <= 0) && (rowview != null))
                {
                    dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE RAWVIN='" + rowview.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString() + "' AND SEQUENCE='" + rowview.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString() + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                    //db.CommandText = "Update plantable set CHECKFLAG=1 where vin='" + rowview.Row.ItemArray[Constants.DB_NAME_VIN].ToString() + "'";
                    //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                }
                dbwrap.ExecuteCommand(Constants.connstring, dbdisplay, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LOCK END", Thread.CurrentThread.ManagedThreadId);

                dgdPlanData.ItemsSource = dbMainDataTable.DefaultView;
                dgdPlanData.Items.Refresh();
                if (dgdPlanData.Items.Count > 0)
                {
                    //dgdPlanData.SelectedIndex = 0;
                    dgdPlanData.UpdateLayout();
                }

                //lock (DeleteDBLock)
                {
                    string insertstring_delete = "INSERT INTO deletetable (PRODUCTDATE, SEQUENCE, RAWCARTYPE, BODYNO, VIN, MESDATE, MESTIME, LASTSEQ, CODE219, IDPLATE, DELETEFLAG, TOTALMSG, RAWBODY, RAWTRIM, PLCVALUE, REGION, BODYTYPE, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG) VALUES('" +
                      data.productdate + "','" + data.sequence + "','" + data.rawcartype + "','" + data.bodyno + "','" + data.rawvin + "','" + data.mesdate + "','" + data.mestime + "','" +
                      data.lastsequence + "','" + data.code219 + "','" + data.idplate + "','" + data.delete + "','" + data.totalmsg + "','" + data.rawbodytype + "','" + data.rawtrim + "','" +
                      data.plcvalue + "','" + data.region + "','" + data.bodytype + "','" + data.cartype + "','" + data.markdate + "','" + data.marktime + "','" + data.remark + "','" +
                      data.isMarked + "','N','" + data.exist + "',0)";

                    dbwrap.ExecuteCommand(Constants.connstring_dele, insertstring_delete, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                    //db.Open(Constants.connstring_dele);
                    //db.CommandText = insertstring_delete;
                    //int retval = db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                    //db.Close();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
            return;
        }

        private async void MenuItem_DeleteData(object sender, RoutedEventArgs e)
        {
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();

            if (CheckAccess())
            {
                //msg2.Message = "선택한 데이터를 삭제하시겠습니까?";
                //msg2.Fontsize = 18;
                //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //msg2.Foreground = Brushes.Red;
                //msg2.Background = Brushes.White;

                //ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
                //window.Owner = System.Windows.Application.Current.MainWindow;
                //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnClearPINCount_Click", "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);

                //if (window.ShowDialog() == false)
                //    return;

                if (dgdPlanData.Items.Count <= 0)
                    return;
                await DeleteSelectPlanData();
            }
            else
            {
                Dispatcher.Invoke(new Action(async delegate
                {
                    //msg2.Message = "선택한 데이터를 삭제하시겠습니까~?";
                    //msg2.Fontsize = 18;
                    //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    //msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    //msg2.Foreground = Brushes.Red;
                    //msg2.Background = Brushes.White;

                    //ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
                    //window.Owner = System.Windows.Application.Current.MainWindow;
                    //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnClearPINCount_Click", "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);

                    //if (window.ShowDialog() == false)
                    //    return;

                    if (dgdPlanData.Items.Count <= 0)
                        return;
                    await DeleteSelectPlanData();
                }));
            }
        }

        private void MenuItem_ExecuteManualPrint(object sender, RoutedEventArgs e)
        {
            DataRowView row;
            string patternName = "";
            //string rawcartype = "";
            string vin = "";
            MESReceivedData ret = new MESReceivedData();

            try
            {
                row = CompleteDataGrid.SelectedItem as DataRowView;
                ret.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                //rawvin = row.Row.ItemArray[Constants.DB_NAME_VIN].ToString();
                //ret.rawvin = row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                ret.rawvin = GetRawVIN(row.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                ret.markvin = AddMonthCode(ret.rawvin);
                ret.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                ret.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                ret.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                ret.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                ret.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                ret.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                ret.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                ret.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                ret.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                ret.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                ret.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();
                ret.isInserted = row.Row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                //patternName = GetPatternName(ret);
                patternName = GetPatternName(ret.rawcartype, ret.rawbodytype, ret.rawtrim);
                ManualMarkWindow3 window = new ManualMarkWindow3(patternName, vin);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_ExecuteManualPrint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task SetMarkPoint(DataGrid grid)
        {
            string className = "MainWindow";
            string funcName = "SetMarkPoint";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            DataRowView row = null;
            //string vin = "";
            //string seq = "";

            //int delcount = 0;
            bool selResult = false;
            int index = 0;
            //List<string> seqvin = new List<string>();

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbMainDataTable = new DataTable();
            Object obj = new Object();
            string commandstring = "";
            int count = 0;
            int totcount = 0;
            MESReceivedData rowData = new MESReceivedData();
            string patternName = "";
            PatternValueEx pattern = new PatternValueEx();
            //int retval = 0;
            string ErrorCode = "";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if (grid.Items.Count <= 0)
                {
                    msg2.Message = "SET POINT FAILURE (THERE IS NO MES DATA)";
                    msg2.Fontsize = 18;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Blue;
                    msg2.Background = Brushes.White;

                    WarningWindow warning1 = new WarningWindow("SET POINT", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning1.ShowDialog();
                    return;
                }
                row = grid.SelectedItem as DataRowView;
                if (row == null)
                {
                    msg2.Message = "SET POINT FAILURE.";
                    msg2.Fontsize = 18;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Blue;
                    msg2.Background = Brushes.White;

                    msg3.Message = "PLEASE SELECT DATA.";
                    msg3.Fontsize = 18;
                    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                    msg3.Foreground = Brushes.Blue;
                    msg3.Background = Brushes.White;


                    WarningWindow warning1 = new WarningWindow("SET POINT", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning1.ShowDialog();
                    return;
                }

                if (row.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString() != "0")
                {
                    msg2.Message = "THE SAME LOCATION IS ALREADY SET AS SET POINT.";
                    msg2.Fontsize = 18;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Blue;
                    msg2.Background = Brushes.White;

                    WarningWindow warning1 = new WarningWindow("SET POINT", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning1.ShowDialog();
                    return;
                }

                //if (row.Row.ItemArray[Constants.DB_NAME_COMPLETE].ToString() == "Y")
                //{
                //    msg2.Message = "완료 처리된 데이터입니다.";
                //    msg2.Fontsize = 18;
                //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg2.Foreground = Brushes.Blue;
                //    msg2.Background = Brushes.White;

                //    WarningWindow warning1 = new WarningWindow("시작 포인트 설정", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                //    warning1.ShowDialog();
                //    return;
                //}

                //if (row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString() == "DLT")
                //{
                //    msg2.Message = "삭제된 데이터입니다.";
                //    msg2.Fontsize = 18;
                //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg2.Foreground = Brushes.Blue;
                //    msg2.Background = Brushes.White;

                //    WarningWindow warning1 = new WarningWindow("시작 포인트 설정", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                //    warning1.ShowDialog();
                //    return;
                //}

                //if (row.Row.ItemArray[Constants.DB_NAME_ISMARK].ToString() == "Y")
                //{
                //    msg2.Message = "이미 각인된 데이터입니다.";
                //    msg2.Fontsize = 18;
                //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg2.Foreground = Brushes.Blue;
                //    msg2.Background = Brushes.White;

                //    WarningWindow warning1 = new WarningWindow("시작 포인트 설정", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                //    warning1.ShowDialog();
                //    return;
                //}
                //                index = dgdPlanData.SelectedIndex;
                //                delcount = (index / 4) * 4;

                //vin_cur = row.Row.ItemArray[DB_NAME_VIN].ToString();
                //seq_cur = row.Row.ItemArray[DB_NAME_SEQUENCE].ToString();

                rowData.sequence = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                rowData.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                rowData.rawvin = GetRawVIN(row.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                //rowData.rawvin = row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                rowData.markvin = AddMonthCode(rowData.rawvin);

                rowData.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                rowData.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                rowData.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                rowData.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                rowData.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                rowData.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                rowData.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                rowData.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                rowData.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                rowData.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                rowData.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();
                rowData.isInserted = row.Row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                //patternName = GetPatternName(rowData);
                patternName = GetPatternName(rowData.rawcartype, rowData.rawbodytype, rowData.rawtrim);

                msg2.Message = "DO YOU WANT TO SET THE SELECTED DATA AS A SET POINT?";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Blue;
                msg2.Background = Brushes.White;

                //msg3.Message = "(" + seq_cur + ", " + vin_cur + ")";
                msg3.Message = "(" + rowData.sequence + ", " + rowData.rawvin + ")";
                msg3.Fontsize = 18;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Blue;
                msg3.Background = Brushes.White;

                WarningWindow warning = new WarningWindow("SET POINT", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                selResult = warning.ShowDialog().Value;
                if (!selResult)
                    return;

                //string value = "";
                //int checkvalue = 0;

                //foreach (DataRowView rv in grid.Items)
                //{
                //    value = rv.Row.ItemArray[Constants.DB_NAME_CHECKFLAG].ToString();
                //    Int32.TryParse(value, out checkvalue);
                //    if (rowData.vin == rv.Row.ItemArray[Constants.DB_NAME_VIN].ToString())
                //    {
                //        break;
                //    }
                //    else
                //    {
                //        seqvin.Add(rv.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString().Trim() + "|" + rv.Row.ItemArray[Constants.DB_NAME_VIN].ToString().Trim());
                //    }
                //    index++;
                //}

                //string[] vals;
                //delcount = (index / 4) * 4;
                //string val = "";

                commandstring = "UPDATE " + tableName + " SET CHECKFLAG=0 WHERE CHECKFLAG=1";
                dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                commandstring = "UPDATE " + tableName +" SET CHECKFLAG=1 WHERE RAWVIN='" + rowData.rawvin + "' AND SEQUENCE='" + rowData.sequence + "'";
                dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                //for (int i = 0; i < delcount; i++)
                //{
                //    val = seqvin[i];
                //    vals = val.Split('|');
                //    vin = vals[1];
                //    seq = vals[0];
                //    commandstring = "delete from plantable where vin ='" + vin + "' and SEQUENCE = '" + seq + "'";
                //    dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                //    //db.ExecuteCommandNonQuery(CommandTypeEnum.Text);
                //}

                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", rowData.sequence.Trim() + "|" + rowData.rawvin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);

                commandstring = MakeDBDisplayText(tableName);
                dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);

                grid.ItemsSource = dbMainDataTable.DefaultView;
                grid.Items.Refresh();
                if (grid.Items.Count > 0)
                {
                    //dgdPlanData.SelectedIndex = 0;
                    grid.UpdateLayout();
                }
#if MANUAL_MARK
                ImageProcessManager.GetPatternData(patternName, ref pattern, rowData.rawcartype);
#else
                //ImageProcessManager.GetPatternData(patternName, ref pattern);
                ImageProcessManager.GetPatternValue(patternName, bHeadType, ref pattern);
#endif
                List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
                VinNoInfo vininfo = new VinNoInfo();
                vininfo.fontName = pattern.fontValue.fontName;
                vininfo.vinNo = rowData.markvin;
                vininfo.width = pattern.fontValue.width;
                vininfo.height = pattern.fontValue.height;
                vininfo.pitch = pattern.fontValue.pitch;
                vininfo.thickness = pattern.fontValue.thickness;

                //byte fontdirection = 0; string value = "";
                //Util.GetPrivateProfileValue("OPTION", "FONTDIRECTION", "0", ref value, Constants.PARAMS_INI_FILE);
                //byte.TryParse(value, out fontdirection);

                retval = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref ErrorCode);
                if (retval.execResult != 0)
                {
                    return;
                }

                //await ShowCurrentMarkingInformation(rowData.vin, rowData.sequence, rowData.rawcartype, pattern);
                await ShowCurrentMarkingInformation2(0, rowData, pattern, fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 1, 1);

                //lock (getCountLock)
                {
                    //count = GetMarkPlanDataCount(grid);
                    (count, totcount) = GetCount4MarkPlanData(grid);
                }
                ShowWorkPlanCount(lblWorkPlanDataCount, count);
                ShowWorkPlanCount(lblWorkPlanToalCount, totcount);
                //ShowWorkPlanCountNTime(lblWorkPlanDataCount, lblVINLastUpdateDate, count, "", false);
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void MenuItem_RequestMESDataData_Click(object sender, RoutedEventArgs e)
        {

        }

        //private async Task MenuItem_LoadData4File_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfirmWindowString msg1 = new ConfirmWindowString();
        //    ConfirmWindowString msg2 = new ConfirmWindowString();
        //    ConfirmWindowString msg3 = new ConfirmWindowString();
        //    ConfirmWindowString msg4 = new ConfirmWindowString();

        //    try
        //    {
        //        if (CheckAccess())
        //            await LoadMESDataFromFile(dgdPlanData);
        //        else
        //        {
        //            Dispatcher.Invoke(new Action(async delegate
        //            {
        //                await LoadMESDataFromFile(dgdPlanData);
        //            }));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_SetMarkPoint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}


        private async void MenuItem_SetMarkPoint(object sender, RoutedEventArgs e)
        {
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();

            try
            {
                if (CheckAccess())
                    await SetMarkPoint(dgdPlanData);
                else
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
                        await SetMarkPoint(dgdPlanData);
                    }));
                }

                //if (CheckAccess())
                //{
                //    msg2.Message = "Do you want to set [SRTART POINT]?";
                //    msg2.Fontsize = 18;
                //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //    msg2.Foreground = Brushes.Red;
                //    msg2.Background = Brushes.White;

                //    ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, msg5, "YES", "NO", this);// , "", "", this);
                //    window.Owner = System.Windows.Application.Current.MainWindow;
                //    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnClearPINCount_Click", "SHOW DIALOG");

                //    if (window.ShowDialog() == false)
                //        return;

                //    if (dgdPlanData.Items.Count <= 0)
                //        return;

                //    await SetMarkPoint();
                //}
                //else
                //{
                //    Dispatcher.Invoke(new Action(async delegate
                //    {
                //        msg2.Message = "Do you want to clear count?";
                //        msg2.Fontsize = 18;
                //        msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                //        msg2.VerticalContentAlignment = VerticalAlignment.Center;
                //        msg2.Foreground = Brushes.Red;
                //        msg2.Background = Brushes.White;

                //        ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, msg5, "YES", "NO", this);// , "", "", this);
                //        window.Owner = System.Windows.Application.Current.MainWindow;
                //        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnClearPINCount_Click", "SHOW DIALOG");

                //        if (window.ShowDialog() == false)
                //            return;
                //        if (dgdPlanData.Items.Count <= 0)
                //            return;

                //        await SetMarkPoint();
                //    }));
                //}
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_SetMarkPoint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void MenuItem_TestDIO_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        /*
         *  |0  |0  |CAR_TYPE|BODY_NO|CMT_NO|ORD_DATA                                                                                      |'N'|PROD_DT |IDX_NO|REG_DATE      |
            |---|---|--------|-------|------|----------------------------------------------------------------------------------------------|---|--------|------|--------------|
            |0  |0  |HWW     |322348 |3204  |202406113204115111151116111511111111NQ WGNDCTSUNEXP-U5YPX81AGSL322348-;                       |N  |20240611|4,488 |20240611130338|
            |0  |0  |JYW     |183575 |3205  |202406113205156115251521152515211521NQ WGNAT6SUNEXP-U5YPX81GMSL183575-;                       |N  |20240611|4,489 |20240611130338|
         * */
        //private void worker_MESDataSave_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    string className = "MainWindow";
        //    string funcName = "worker_MESDataSave_DoWork";

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    string value = "";
        //    string filename = "";
        //    string newtableName = "";
        //    string linestring = "";
        //    string cmt_no = "";
        //    DateTime producDate = new DateTime();
        //    string cartype = "";
        //    string vin = "";
        //    string tmpstr = "";
        //    string tmpstr2 = "";
        //    string rawcartype = "";
        //    string body_no = "";
        //    int rawcartypePos = 0;
        //    int cartypePos = 0;
        //    int vinPos = 0;
        //    int productPos = 0;
        //    int count = 0;
        //    MESReceivedData mesData = new MESReceivedData();
        //    MESReceivedData[] mesDataArray = new MESReceivedData[512];

        //    try
        //    {
        //        Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
        //        if (value == "plantable")
        //        {
        //            //fname = curDir + "CCR2.DAT";
        //            newtableName = "plantable2";
        //        }
        //        else
        //        {
        //            //fname = curDir + "CCR.DAT";
        //            newtableName = "plantable";
        //        }

        //        filename = e.Argument as string;

        //        Util.GetPrivateProfileValue("PRODUCTTIME", "POSITION", "0", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out productPos);

        //        Util.GetPrivateProfileValue("RAWCARTYPE", "POSITION", "12", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out rawcartypePos);

        //        Util.GetPrivateProfileValue("CARTYPE", "POSITION", "36", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out cartypePos);

        //        Util.GetPrivateProfileValue("VIN", "POSITION", "51", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out vinPos);

        //        //Util.GetPrivateProfileValue("BODYNO", "POSITION", "51", ref value, Constants.MESCONF_INI_FILE);
        //        //int.TryParse(value, out vinPos);

        //        for (int j = 0; j < mesDataArray.Length; j++)
        //            mesDataArray[j] = new MESReceivedData();

        //        DBClear(newtableName);

        //        StreamReader sr = new StreamReader(filename);
        //        while ((linestring = sr.ReadLine()) != null)
        //        {
        //            string[] vals = linestring.Split('|');

        //            if ((vals.Length > 10) && ((vals[6].Contains(";")) == true) && (vals[6].Length >= 72))
        //            {
        //                tmpstr = vals[8];//.Substring(productPos, 8);
        //                if(tmpstr.Length < 8)
        //                {
        //                    mesData.productdate = DateTime.Now.ToString("yyyy-MM-dd");
        //                }
        //                else
        //                {
        //                    tmpstr = tmpstr.Insert(6, "-");
        //                    tmpstr = tmpstr.Insert(4, "-");
        //                    producDate = Convert.ToDateTime(tmpstr);
        //                    mesData.productdate = producDate.ToString("yyyy-MM-dd");
        //                }

        //                mesData.bodyno = vals[4].Trim();
        //                mesData.sequence = vals[5].Trim();

        //                mesData.rawvin = vals[6].Substring(vinPos, 19);
        //                mesData.markvin = AddMonthCode(mesData.rawvin);
        //                mesData.rawcartype = vals[6].Substring(rawcartypePos, 4);

        //                tmpstr = vals[10].Trim();
        //                if(tmpstr.Length < 14)
        //                {
        //                    mesData.mesdate = DateTime.Now.ToString("yyyy-MM-dd");
        //                    mesData.mestime = DateTime.Now.ToString("HH:mm:dd");
        //                }
        //                else
        //                {
        //                    tmpstr = tmpstr.Substring(0, 8);
        //                    tmpstr = tmpstr.Insert(6, "-");
        //                    tmpstr = tmpstr.Insert(4, "-");
        //                    //producDate = Convert.ToDateTime(tmpstr);

        //                    tmpstr2 = vals[10].Substring(8, 6);
        //                    tmpstr2 = tmpstr2.Insert(4, ":");
        //                    tmpstr2 = tmpstr2.Insert(2, ":");
        //                    tmpstr = tmpstr + " " + tmpstr2;
        //                    producDate = Convert.ToDateTime(tmpstr);

        //                    //producDate = DateTime.ParseExact(tmpstr, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        //                    mesData.mesdate = producDate.ToString("yyyy-MM-dd");
        //                    mesData.mestime = producDate.ToString("HH:mm:dd");

        //                    mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
        //                    mesData.marktime = DateTime.Now.ToString("HH:mm:dd");
        //                }

        //                //body_no = vals[4].Trim();
        //                //cmt_no = vals[5].Trim();
        //                //tmpstr = vals[6].Substring(productPos, 8);
        //                //tmpstr = tmpstr.Insert(6, "-");
        //                //tmpstr = tmpstr.Insert(4, "-");
        //                //producDate = Convert.ToDateTime(tmpstr);
        //                //vin = vals[6].Substring(vinPos, 19);
        //                //rawcartype = vals[6].Substring(rawcartypePos, 4);
        //                mesData.cartype = vals[6].Substring(36, 3).Trim();

        //                mesDataArray[count] = (MESReceivedData)mesData.Clone();

        //                count++;
        //                if (count >= mesDataArray.Length)
        //                {
        //                    retval.execResult = SaveMESClientReceivedData2(newtableName, /*"", */mesDataArray, count);
        //                    if (retval.execResult != 0)
        //                    {
        //                        //mesDBUpdateFlag = backmesDBUpdateFlag;
        //                        if (sr != null)
        //                            sr.Close();
        //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
        //                        return;
        //                    }
        //                    mesDataArray.Initialize();
        //                    count = 0;
        //                }
        //            }
        //        }

        //        if (count >= 0)
        //        {
        //            retval.execResult = SaveMESClientReceivedData2(newtableName, /*"", */mesDataArray, count);
        //            if (retval.execResult != 0)
        //            {
        //                //mesDBUpdateFlag = backmesDBUpdateFlag;
        //                if (sr != null)
        //                    sr.Close();
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
        //                return;
        //            }
        //            count = 0;
        //        }
        //        if (sr != null)
        //            sr.Close();

        //        ChangeDBProcess4Thread();
        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //}

        //private void worker_MESDataSave_DoWork2(object sender, DoWorkEventArgs e)
        //{
        //    string className = "MainWindow";
        //    string funcName = "worker_MESDataSave_DoWork";

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    string value = "";
        //    string filename = "";
        //    string newtableName = "";
        //    string linestring = "";
        //    string cmt_no = "";
        //    DateTime producDate = new DateTime();
        //    string cartype = "";
        //    string vin = "";
        //    string tmpstr = "";
        //    string tmpstr2 = "";
        //    string rawcartype = "";
        //    string body_no = "";
        //    int rawcartypePos = 0;
        //    int cartypePos = 0;
        //    int vinPos = 0;
        //    int productPos = 0;
        //    int count = 0;
        //    MESReceivedData mesData = new MESReceivedData();
        //    MESReceivedData[] mesDataArray = new MESReceivedData[512];

        //    try
        //    {
        //        Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
        //        if (value == "plantable")
        //        {
        //            //fname = curDir + "CCR2.DAT";
        //            newtableName = "plantable2";
        //        }
        //        else
        //        {
        //            //fname = curDir + "CCR.DAT";
        //            newtableName = "plantable";
        //        }

        //        filename = e.Argument as string;

        //        Util.GetPrivateProfileValue("SEQUENCE", "POSITION", "0", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out productPos);

        //        Util.GetPrivateProfileValue("RAWCARTYPE", "POSITION", "12", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out rawcartypePos);

        //        Util.GetPrivateProfileValue("CARTYPE", "POSITION", "36", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out cartypePos);

        //        Util.GetPrivateProfileValue("VIN", "POSITION", "51", ref value, Constants.MESCONF_INI_FILE);
        //        int.TryParse(value, out vinPos);

        //        //Util.GetPrivateProfileValue("BODYNO", "POSITION", "51", ref value, Constants.MESCONF_INI_FILE);
        //        //int.TryParse(value, out vinPos);

        //        for (int j = 0; j < mesDataArray.Length; j++)
        //            mesDataArray[j] = new MESReceivedData();

        //        DBClear(newtableName);

        //        StreamReader sr = new StreamReader(filename);
        //        while ((linestring = sr.ReadLine()) != null)
        //        {
        //            string[] vals = linestring.Split(' ');

        //            if (vals.Length >= 3)
        //            {
        //                mesData.productdate = DateTime.Now.ToString("yyyy-MM-dd");

        //                //mesData.bodyno = vals[4].Trim();
        //                mesData.sequence = vals[0].Trim();

        //                mesData.rawvin = vals[1];
        //                mesData.markvin = AddMonthCode(vals[1]);
        //                mesData.rawcartype = vals[2];

        //                mesData.mesdate = DateTime.Now.ToString("yyyy-MM-dd");
        //                mesData.mestime = DateTime.Now.ToString("HH:mm:dd");

        //                mesData.cartype = GetCarType(mesData.rawcartype.Substring(0, 1), mesData.rawcartype);

        //                mesDataArray[count] = (MESReceivedData)mesData.Clone();

        //                count++;
        //                if (count >= mesDataArray.Length)
        //                {
        //                    retval.execResult = SaveMESClientReceivedData2(newtableName, /*"", */mesDataArray, count);
        //                    if (retval.execResult != 0)
        //                    {
        //                        //mesDBUpdateFlag = backmesDBUpdateFlag;
        //                        if (sr != null)
        //                            sr.Close();
        //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
        //                        return;
        //                    }
        //                    mesDataArray.Initialize();
        //                    count = 0;
        //                }
        //            }
        //        }

        //        if (count >= 0)
        //        {
        //            retval.execResult = SaveMESClientReceivedData2(newtableName, /*"", */mesDataArray, count);
        //            if (retval.execResult != 0)
        //            {
        //                //mesDBUpdateFlag = backmesDBUpdateFlag;
        //                if (sr != null)
        //                    sr.Close();
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
        //                return;
        //            }
        //            count = 0;
        //        }
        //        if (sr != null)
        //            sr.Close();

        //        ChangeDBProcess4Thread();
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        private void worker_MESDataSave_DoWork3(object sender, DoWorkEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "worker_MESDataSave_DoWork3";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            string filename = "";
            string newtableName = "";
            string linestring = "";
            string cmt_no = "";
            //DateTime producDate = new DateTime();
            string cartype = "";
            string vin = "";
            string tmpstr = "";
            string tmpstr2 = "";
            string rawcartype = "";
            string body_no = "";

            int rawcartypePos = 0;
            int cartypePos = 0;
            int vinPos = 0;
            int productDatePos = 0;
            int seqPos = 0;
            int count = 0;
            MESReceivedData mesData = new MESReceivedData();
            MESReceivedData[] mesDataArray = new MESReceivedData[512];
            int idx = 0;

            try
            {
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
                if (value == "plantable")
                {
                    //fname = curDir + "CCR2.DAT";
                    newtableName = "plantable2";
                }
                else
                {
                    //fname = curDir + "CCR.DAT";
                    newtableName = "plantable";
                }

                filename = e.Argument as string;

                Util.GetPrivateProfileValue("PRODUCTTIME", "POSITION", "0", ref value, Constants.MESCONF_INI_FILE);
                int.TryParse(value, out productDatePos);

                Util.GetPrivateProfileValue("SEQUENCE", "POSITION", "8", ref value, Constants.MESCONF_INI_FILE);
                int.TryParse(value, out seqPos);

                Util.GetPrivateProfileValue("RAWCARTYPE", "POSITION", "16", ref value, Constants.MESCONF_INI_FILE);
                int.TryParse(value, out rawcartypePos);

                Util.GetPrivateProfileValue("CARTYPE", "POSITION", "36", ref value, Constants.MESCONF_INI_FILE);
                int.TryParse(value, out cartypePos);

                Util.GetPrivateProfileValue("VIN", "POSITION", "51", ref value, Constants.MESCONF_INI_FILE);
                int.TryParse(value, out vinPos);

                //Util.GetPrivateProfileValue("BODYNO", "POSITION", "51", ref value, Constants.MESCONF_INI_FILE);
                //int.TryParse(value, out vinPos);

                for (int j = 0; j < mesDataArray.Length; j++)
                    mesDataArray[j] = new MESReceivedData();

                DBClear(newtableName);

                StreamReader sr = new StreamReader(filename);
                while ((linestring = sr.ReadLine()) != null)
                {
                    //string[] vals = linestring.Split((char)3);
                    string[] vals = linestring.Split((char)';');

                    if (vals.Length >= 3)
                    {
                        mesData.bodyno = vals[0];

                        if (vals[1].Length >= productDatePos + 8)
                        {
                            string dt = "";
                            //string tmp2 = vals[2].Substring(productDatePos, 8);
                            string tmp = vals[1].Substring(productDatePos, 4);
                            dt = tmp + "-";
                            tmp = vals[1].Substring(productDatePos+4, 2);
                            dt += tmp + "-";
                            tmp = vals[1].Substring(productDatePos + 4 + 2, 2);
                            dt += tmp;
                            mesData.productdate = dt;
                        }
                        else
                            mesData.productdate = DateTime.Now.ToString("yyyy-MM-dd");

                        if (vals[1].Length >= seqPos + 4)
                            mesData.sequence = vals[1].Substring(seqPos, 4);
                        else
                            continue;

                        if (vals[1].Length >= rawcartypePos + 4)
                            mesData.rawcartype = vals[1].Substring(rawcartypePos, 4);
                        else
                            continue;

                        if (vals[1].Length >= cartypePos + 3)
                        {
                            mesData.cartype = vals[1].Substring(cartypePos, 3);
                            mesData.cartype = mesData.cartype.Trim(' ');// msg.Substring(idx, length);
                            //Util.GetPrivateProfileValue("CARTYPE", "USEMULTIDATA", "0", ref value, Constants.PARAMS_INI_FILE);
                            //if (value != "0")
                            //{
                            //    byte pos1 = 0;
                            //    byte pos2 = 0;
                            //    Util.GetPrivateProfileValue("CARTYPE", "TYPEPOS1", "0", ref value, Constants.PARAMS_INI_FILE);
                            //    byte.TryParse(value, out pos1);
                            //    Util.GetPrivateProfileValue("CARTYPE", "TYPEPOS2", "0", ref value, Constants.PARAMS_INI_FILE);
                            //    byte.TryParse(value, out pos2);

                            //    string tmp1 = mesData.rawcartype.Length > pos1 ? mesData.rawcartype.Substring(pos1, 1) : "1";
                            //    string tmp2 = mesData.rawcartype.Length > pos2 ? mesData.rawcartype.Substring(pos2, 1) : "5";

                            //    Util.GetPrivateProfileValue("CARTYPE", tmp1 + tmp2, "NQPHEV", ref mesData.cartype, Constants.PARAMS_INI_FILE);
                            //}


                            //if (value != "0")
                            //{
                            //    if (mesData.rawcartype.Length > 2)
                            //    {
                            //        string tmp00 = mesData.rawcartype.Substring(0, 1);
                            //        string tmp02 = mesData.rawcartype.Substring(2, 1);
                            //        if ((tmp02 == "3") || (tmp02 == "7"))
                            //        {
                            //            Util.GetPrivateProfileValue("CARTYPE", tmp00 + tmp02, "NQPHEV", ref mesData.cartype, Constants.PARAMS_INI_FILE);
                            //            if (mesData.cartype.Length < 2)
                            //                mesData.cartype = "NQ";
                            //        }
                            //    }
                            //}

                            if (mesData.cartype == "NQ")
                            {
                                if (mesData.rawcartype.Length > 2)
                                {
                                    string tmp = mesData.rawcartype.Substring(2, 1);
                                    if ((tmp == "3") || (tmp == "7"))
                                        mesData.cartype = "NQPHEV";
                                }
                                else
                                    mesData.cartype = "NQ";
                            }
                        }
                        else
                            continue;

                        if (vals[1].Length >= vinPos + 19)
                        {
                            mesData.rawvin = vals[1].Substring(vinPos, 19);
                            mesData.markvin = AddMonthCode(mesData.rawvin);
                        }
                        else
                            continue;

                        mesData.mesdate = DateTime.Now.ToString("yyyy-MM-dd");
                        mesData.mestime = DateTime.Now.ToString("HH:mm:dd");

                        mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
                        mesData.marktime = DateTime.Now.ToString("HH:mm:dd");
                        //idx = idx * 20;
                        mesData.no2 = idx++;
                        mesData.isInserted = "0";

                        mesDataArray[count] = (MESReceivedData)mesData.Clone();

                        count++;
                        if (count >= mesDataArray.Length)
                        {
                            retval.execResult = SaveMESClientReceivedData2(newtableName, /*"", */mesDataArray, count);
                            if (retval.execResult != 0)
                            {
                                //mesDBUpdateFlag = backmesDBUpdateFlag;
                                if (sr != null)
                                    sr.Close();
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                return;
                            }
                            mesDataArray.Initialize();
                            count = 0;
                        }
                    }
                }

                if (count >= 0)
                {
                    retval.execResult = SaveMESClientReceivedData2(newtableName, /*"", */mesDataArray, count);
                    if (retval.execResult != 0)
                    {
                        //mesDBUpdateFlag = backmesDBUpdateFlag;
                        if (sr != null)
                            sr.Close();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    count = 0;
                }
                if (sr != null)
                    sr.Close();

                ChangeDBProcess4Thread();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ERROR EXCEPTION : CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void worker_MESDataSave_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //ShowLog();
        }

        private async Task LoadMESDataFromFile(DataGrid grid)
        {
            string className = "MainWindow";
            string funcName = "LoadMESDataFromFile";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string filename = "";
            int i = 0;
            BackgroundWorker backSaveData = new BackgroundWorker();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if((currentProcessStatus != (byte)STATUS_LABEL_NUM.STATUS_LABEL_READY_MARK) && (currentProcessStatus != (byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_VISION))
                {
                    msg2.Message = "SYSTEM IS MARKING NOW.";
                    msg2.Fontsize = 18;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Blue;
                    msg2.Background = Brushes.White;

                    msg3.Message = "PLEASE WAIT UNTIL MARKING IS COMPLETED.";
                    msg3.Fontsize = 18;
                    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                    msg3.Foreground = Brushes.Blue;
                    msg3.Background = Brushes.White;

                    WarningWindow warning1 = new WarningWindow("LOAD DATA", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning1.ShowDialog();
                    return;
                }

                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "DATA";             // Default file name
                dlg.DefaultExt = ".dat";                // Default file extension
                dlg.Filter = "DATA (.dat)|*.dat|All files (*.*)|*.*";    // Filter files by extension

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();
                // Process open file dialog box results
                if (result == true)
                {
                    // Open document
                    filename = dlg.FileName;
                    filename.ToUpper();
                    //string[] lines = System.IO.File.ReadAllLines(filename);


                    backSaveData.DoWork += worker_MESDataSave_DoWork3;
                    backSaveData.RunWorkerCompleted += worker_MESDataSave_RunWorkerCompleted;

                    backSaveData.RunWorkerAsync(filename);
                }
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnDisplayVIN_Click(object sender, RoutedEventArgs e)
        {
            //string sendmsg = "M5" + "1234" + "TUVWXYZ0123456789*-" + "HE  " + "1";
            ITNTSendArgs args = new ITNTSendArgs();
            int i = 0;
            //while (true)
            //{
            //    sendmsg = "M2" + i.ToString("D4") + "TUVWXYZ*-012345" + i.ToString("D4") + "HE  " + "1";

            //    args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
            //    args.sendString = sendmsg;
            //    args.dataSize = sendmsg.Length;
            //    await vision.SendMessage(args);

            //    await Task.Delay(1000);
            //    await Task.Delay(1000);

            //    sendmsg = "M5" + i.ToString("D4") + "TUVWXYZ*-01234" + i.ToString("D5") + "HE  " + "1";

            //    args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
            //    args.sendString = sendmsg;
            //    args.dataSize = sendmsg.Length;
            //    await vision.SendMessage(args);




            //    //btnMoveData_Copy_Click(this, e);
            //    //await Task.Delay(1000);
            //    //btnVisionComplete_Click(this, e);
            //    //while(true)
            //    //{
            //    //    if (visionFinish == 1)
            //    //    {
            //    //        visionFinish = 0;
            //    //        break;
            //    //    }
            //    //    await Task.Delay(100);
            //    //}
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(1000);

            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(1000);

            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    await Task.Delay(100);
            //    i++;
            //    if (i >= 10000)
            //        i = 0;
            //}



            //PatternValue pattern = new PatternValue();
            //pattern.fontName = "11X16";
            //pattern.width = 5;
            //pattern.height = 7;
            //pattern.pitch = 6;
            //pattern.thickness = 0.4;
            //string vin = "198765ABCDTHDE891HN";

            //await showRecogCharacters(vin, pattern);//, vin.Length);

            //DataRow row = GetCurrentMarkPointData(0);
            //vin = row.ItemArray[Constants.DB_NAME_VIN].ToString();
            //PatternValue pat = new PatternValue();
            //MESReceivedData recvMsg = new MESReceivedData();
            //GetMarkDataInfomation(ref recvMsg);
            //int retval = 0;
            //VinNoInfo vinInfo = new VinNoInfo();
            //double fontSizeX = 0, fontSizeY = 0;
            //string ErrorCode = "";

            //vinInfo.vinNo = vin;
            //vinInfo.height = 7;
            //vinInfo.width = 4;
            //vinInfo.pitch = 6;
            //vinInfo.fontName = "11X16";

            //Dictionary<int, List<FontDataClass>> MyData = new Dictionary<int, List<FontDataClass>>();
            //List<FontDataClass> fdata = new List<FontDataClass>();

            //retval = ImageProcessManager.GetFontData(vinInfo, ref MyData, out fontSizeX, out fontSizeY, out ErrorCode);
            //for ()
            //{

            //}
            //fdata = MyData[];

            //ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
            //showRecogCharacters(vin, recvMsg);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        private void MenuItem_ManualMarkPlanData(object sender, RoutedEventArgs e)
        {
            DataRowView row;
            string patternName = "";
            //string rawcartype = "";
            //string vin = "";
            MESReceivedData ret = new MESReceivedData();

            try
            {
                if(dgdPlanData.SelectedIndex < 0)
                {
                    return;
                }

                row = dgdPlanData.SelectedItem as DataRowView;
                ret.rawcartype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                ret.rawvin = GetRawVIN(row.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                //ret.rawvin = row.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                ret.markvin = AddMonthCode(ret.rawvin);
                ret.lastsequence = row.Row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                ret.code219 = row.Row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                ret.idplate = row.Row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                ret.delete = row.Row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                ret.totalmsg = row.Row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                ret.rawbodytype = row.Row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                ret.rawtrim = row.Row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                ret.region = row.Row.ItemArray[Constants.DB_NAME_REGION].ToString();
                ret.bodytype = row.Row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                ret.cartype = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                ret.plcvalue = row.Row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();
                ret.isInserted = row.Row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                //patternName = GetPatternName(ret);
                patternName = GetPatternName(ret.rawcartype, ret.rawbodytype, ret.rawtrim);
                ManualMarkWindow3 window = new ManualMarkWindow3(patternName, ret.markvin);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_ManualMarkPlanData", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            //ShowMarkingDataList(true, false);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();

            msg2.Message = "DO YOU WANT TO EXIT A PROGRAM?";
            msg2.Fontsize = 18;
            msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
            msg2.VerticalContentAlignment = VerticalAlignment.Center;
            msg2.Foreground = Brushes.Red;
            msg2.Background = Brushes.White;

            ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == false)
            {
                e.Cancel = true;
                return;
            }

            bcloseThread = true;
        }

        //private void chbCheckSeq_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    chbCheckSeq.Background = Brushes.Red;
        //    chbCheckSeq.Foreground = Brushes.White;
        //}

        //private void chbCheckSeq_Checked(object sender, RoutedEventArgs e)
        //{
        //    chbCheckSeq.Background = Brushes.White;
        //    chbCheckSeq.Foreground = Brushes.Black;

        //}

        //private void MenuItem_Move2Complete(object sender, RoutedEventArgs e)
        //{

        //}

        private void ChangeSize(double width, double height)
        {
            try
            {
                scale.ScaleX = width / orginalWidth;
                scale.ScaleY = height / originalHeight;
                FrameworkElement rootElement = this.Content as FrameworkElement;
                rootElement.LayoutTransform = scale;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "ChangeSize", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void chbCheckSeq_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isShowWarningBlink != 0)
            {
                showBlink = true;
                lblSeqCheckWarning.Visibility = Visibility.Visible;
                SoftBlink(lblSeqCheckWarning, Color.FromRgb(255, 255, 255), Color.FromRgb(255, 0, 0), 2000);
            }
        }

        private void chbCheckSeq_Checked(object sender, RoutedEventArgs e)
        {
            lblSeqCheckWarning.Visibility = Visibility.Collapsed;
            showBlink = false;
        }

        private async void btnSensorView_Click(object sender, RoutedEventArgs e)
        {
            string sendmsg = "S01";
            ITNTSendArgs args = new ITNTSendArgs();
            args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
            args.sendString = sendmsg;
            args.dataSize = sendmsg.Length;
            //await vision.SendMessage(args);

        }

        private async void SoftBlink(Control ctrl, Color backc1, Color backc2, short CycleTime_ms)//, bool BkClr)
        {
            var sw = new Stopwatch();
            sw.Start();
            short halfCycle = (short)Math.Round(CycleTime_ms * 0.5);
            SolidColorBrush backbrush;// = new SolidColorBrush(clr);
            while (showBlink)
            {
                await Task.Delay(2);
                var n = sw.ElapsedMilliseconds % CycleTime_ms;
                var per = (double)Math.Abs(n - halfCycle) / halfCycle;
                var red = (short)Math.Round((backc2.R - backc1.R) * per) + backc1.R;
                var grn = (short)Math.Round((backc2.G - backc1.G) * per) + backc1.G;
                var blw = (short)Math.Round((backc2.B - backc1.B) * per) + backc1.B;
                var clrback = Color.FromArgb(255, (byte)red, (byte)grn, (byte)blw);

                backbrush = new SolidColorBrush(clrback);

                ctrl.Background = backbrush;
            }
            sw.Stop();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;

            string value = "";
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                msg2.Message = "Do you want to exit a program?";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);

                if (window.ShowDialog() == false)
                    return;

                bcloseThread = true;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Vision Server S", Thread.CurrentThread.ManagedThreadId);
                CloseVisionServer();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Vision Server E", Thread.CurrentThread.ManagedThreadId);
                CloseMESServer();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close MES Server E", Thread.CurrentThread.ManagedThreadId);

                CloseMarkController();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Mark Controller E", Thread.CurrentThread.ManagedThreadId);

                ClosePLC();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close PLC E", Thread.CurrentThread.ManagedThreadId);



                if (bHeadType != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Displacement E1", Thread.CurrentThread.ManagedThreadId);
                    CloseDistanceSensor();

                    laserSource.LaserControllerStatusEventFunc -= OnLaserControllerStatusChangedEventReceivedFunc;
                    laserSource.LaserConnectionStatusChangedEventFunc -= OnLaserConnectionStatusChangedEventReceivedFunc;
                    laserSource.CloseClient(1);

                    CloseLPM();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Displacement E2", Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    Util.GetPrivateProfileValue("CONFIG", "USE", "0", ref value, Constants.DISPLACEMENT_INI_FILE);
                    if (value != "0")
                    {
                        CloseDistanceSensor();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Close Displacement E", Thread.CurrentThread.ManagedThreadId);
                    }
                }

                CloseCommunicationToDIO();

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CloseCommunicationToDIO", Thread.CurrentThread.ManagedThreadId);

                Environment.Exit(0);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                this.Close();
            }
            catch (Exception ex)
            { }
        }

        const string SOURCE_FILE = ".\\Data\\CCR.dat";
        //const string DEST_FILE = ".\\Data\\CCR_T.dat";

        private async void CCR2WORK4GMES(object obj)
        {
            string className = "MainWindow";
            string funcName = "CCR2WORK4GMES";

            int type = (int)obj;

            int retval = 0;
            string linestring = "";
            FileInfo fi;// = new FileInfo(SOURCE_FILE);
            MESReceivedData[] receivedMsg = new MESReceivedData[512];
            int count = 0;
            //MESUPDATETHREADArgs arg = (MESUPDATETHREADArgs)obj;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //string newtableName = "";
            int backmesDBUpdateFlag = mesDBUpdateFlag;
            int reccount = 0;
            //string strcount = "";
            //int leng = 0;
            int recordsize = 0;
            //string strrecordsize = "";
            //string recorddata = "";
            //string totrecorddata = "";
            int idxvalue = 0;
            int idx = 0;
            int size = 0;
            //string orderdate = "";
            string value = "";
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string fname = "CCR.DAT";
            Stopwatch sw = new Stopwatch();
            //int saveMESStatus = currentProcessStatus;
            int mesAlgoType = 0;
            string newtableName = "";
            DateTime dt = DateTime.Now;
            CarTypeOption optFlag = new CarTypeOption();
            int no2 = 0;

            try
            {
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
                curDir = curDir + "DATA\\";

                if (value == "plantable")
                {
                    fname = curDir + "CCR2.DAT";
                    newtableName = "plantable2";
                }
                else
                {
                    fname = curDir + "CCR.DAT";
                    newtableName = "plantable";
                }

                fi = new FileInfo(fname);
                if (!fi.Exists)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CCR file does not exist", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                for (int i = 0; i < receivedMsg.Length; i++)
                    receivedMsg[i] = new MESReceivedData();

                if (bcloseThread)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "bcloseThread = true", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if ((mesDBUpdateFlag != (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE) && (mesDBUpdateFlag != (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING))
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "mesDBUpdateFlag = " + mesDBUpdateFlag.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CURRENT STATUS 3: " + mesDBUpdateFlag.ToString(), Thread.CurrentThread.ManagedThreadId);

                mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);

                DBClear(newtableName);
                //DBClear(tableName);
                mesAlgoType = (int)Util.GetPrivateProfileValueUINT("MES", "ALGORITHMTYPE", 0, Constants.PARAMS_INI_FILE);

                StreamReader sr = new StreamReader(fname);
                //orderdate = dt.ToString("yyyyMMddHHmmss");

                Util.GetPrivateProfileValue("CARTYPE", "USEMULTIDATA", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out optFlag.useMultiData);
                Util.GetPrivateProfileValue("CARTYPE", "TYPEPOS1", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out optFlag.carTypePos1);
                Util.GetPrivateProfileValue("CARTYPE", "TYPEPOS2", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out optFlag.carTypePos2);

                sw.Start();
                while ((linestring = sr.ReadLine()) != null)
                {
                    if (bcloseThread)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "bcloseThread = true 2", Thread.CurrentThread.ManagedThreadId);
                        mesDBUpdateFlag = backmesDBUpdateFlag;
                        if (sr != null)
                            sr.Close();
                        return;
                    }
                    no2++;
                    if(linestring.Length > 30)
                    {
                        if(mesAlgoType == 6)
                            retval = AnalyzeMESReceivedData_Alogorithm06(linestring, 0, optFlag, ref receivedMsg[count], no2);
                        if(retval == 0)
                            count++;
                    }
                    else
                    {

                    }
                    if (retval != 0)
                        continue;

                    if (count >= receivedMsg.Length)
                    {
                        retval = SaveMESClientReceivedData2(newtableName, /*orderdate + recorddata, */receivedMsg, count);
                        //retval = SaveMESClientReceivedData2(tableName, orderdate + recorddata, receivedMsg, count);
                        if (retval != 0)
                        {
                            mesDBUpdateFlag = backmesDBUpdateFlag;
                            if (sr != null)
                                sr.Close();
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                            return;
                        }
                        count = 0;
                    }
                }
                sw.Stop();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 TIME : " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
                if (count >= 0)
                {
                    retval = SaveMESClientReceivedData2(newtableName, /*orderdate + recorddata, */receivedMsg, count);
                    //retval = SaveMESClientReceivedData2(tableName, orderdate + recorddata, receivedMsg, count);
                    if (retval != 0)
                    {
                        mesDBUpdateFlag = backmesDBUpdateFlag;
                        if (sr != null)
                            sr.Close();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    count = 0;
                }

                if (sr != null)
                    sr.Close();

                mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);

                string datetime = DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss");
                Util.WritePrivateProfileValue("MES", "DOWNLOADTIME", datetime, Constants.PARAMS_INI_FILE);
                ShowLabelData(datetime, lblVINLastUpdateDate);

                if ((currentProcessStatus >= (int)STATUS_LABEL_NUM.STATUS_LABEL_RECV_NEXTVIN) && (currentProcessStatus <= (int)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_MARKING))
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CURRENT STATUS IS WORKING!!!!!", Thread.CurrentThread.ManagedThreadId);
                else
                    await ChangeDBProcess4Thread();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                mesDBUpdateFlag = backmesDBUpdateFlag;
            }

            return;
        }

        private async void CCR2WORK3(object obj)
        {
            string className = "MainWindow";
            string funcName = "CCR2WORK3";

            int type = (int)obj;

            int retval = 0;
            string linestring = "";
            FileInfo fi;// = new FileInfo(SOURCE_FILE);
            MESReceivedData[] receivedMsg = new MESReceivedData[512];
            int count = 0;
            //MESUPDATETHREADArgs arg = (MESUPDATETHREADArgs)obj;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //string newtableName = "";
            int backmesDBUpdateFlag = mesDBUpdateFlag;
            int reccount = 0;
            string strcount = "";
            //int leng = 0;
            int recordsize = 0;
            string strrecordsize = "";
            string recorddata = "";
            string totrecorddata = "";
            int idxvalue = 0;
            int idx = 0;
            int size = 0;
            string orderdate = "";
            string value = "";
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string fname = "CCR.DAT";
            Stopwatch sw = new Stopwatch();
            //int saveMESStatus = currentProcessStatus;
            int mesAlgoType = 0;
            string newtableName = "";
            CarTypeOption optFlag = new CarTypeOption();
            int no2 = 1;

            try
            {
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
                curDir = curDir + "DATA\\";

                if (value == "plantable")
                {
                    fname = curDir + "CCR2.DAT";
                    newtableName = "plantable2";
                }
                else
                {
                    fname = curDir + "CCR.DAT";
                    newtableName = "plantable";
                }

                fi = new FileInfo(fname);
                if (!fi.Exists)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CCR file does not exist", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                for (int i = 0; i < receivedMsg.Length; i++)
                    receivedMsg[i] = new MESReceivedData();

                if (bcloseThread)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "bcloseThread = true", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if ((mesDBUpdateFlag != (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE) && (mesDBUpdateFlag != (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING))
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "mesDBUpdateFlag = " + mesDBUpdateFlag.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CURRENT STATUS 3: " + mesDBUpdateFlag.ToString(), Thread.CurrentThread.ManagedThreadId);

                mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);

                DBClear(newtableName);
                //DBClear(tableName);
                mesAlgoType = (int)Util.GetPrivateProfileValueUINT("MES", "ALGORITHMTYPE", 0, Constants.PARAMS_INI_FILE);

                Util.GetPrivateProfileValue("CARTYPE", "USEMULTIDATA", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out optFlag.useMultiData);
                Util.GetPrivateProfileValue("CARTYPE", "TYPEPOS1", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out optFlag.carTypePos1);
                Util.GetPrivateProfileValue("CARTYPE", "TYPEPOS2", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out optFlag.carTypePos2);

                StreamReader sr = new StreamReader(fname);
                while ((linestring = sr.ReadLine()) != null)
                {
                    if (bcloseThread)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "bcloseThread = true 2", Thread.CurrentThread.ManagedThreadId);
                        mesDBUpdateFlag = backmesDBUpdateFlag;
                        if (sr != null)
                            sr.Close();
                        return;
                    }

                    idx = 0;

                    size = 14;
                    orderdate = linestring.Substring(idx, size);
                    idx += size;

                    size = 5;
                    strcount = linestring.Substring(idx, size);
                    idx += size;

                    size = 5;
                    strrecordsize = linestring.Substring(idx, size);
                    idx += size;

                    totrecorddata = linestring.Substring(idx);
                    int.TryParse(strcount, out reccount);
                    int.TryParse(strrecordsize, out recordsize);
                    idxvalue = 0;
                    sw.Reset();
                    sw.Start();
                    for (int i = 0; i < reccount; i++)
                    {
                        //leng = recordsize;
                        if (totrecorddata.Length >= (recordsize + idxvalue))
                        {
                            recorddata = totrecorddata.Substring(idxvalue, recordsize);
                            idxvalue += recordsize;

                            if (mesAlgoType == 0)
                                retval = AnalyzeMESReceivedData_Alogorithm00(orderdate + recorddata, ref receivedMsg[count]);
                            else if (mesAlgoType == 1)
                                retval = AnalyzeMESReceivedData_Alogorithm01(orderdate + recorddata, orderdate.Length, ref receivedMsg[count]);
                            else if (mesAlgoType == 2)
                                retval = AnalyzeMESReceivedData_Alogorithm02(orderdate + recorddata, orderdate.Length, ref receivedMsg[count]);
                            else if (mesAlgoType == 5)
                                retval = AnalyzeMESReceivedData_Alogorithm05(orderdate + recorddata, ref receivedMsg[count]);
                            else if (mesAlgoType == 6)
                                retval = AnalyzeMESReceivedData_Alogorithm06(orderdate + recorddata, orderdate.Length, optFlag, ref receivedMsg[count], no2);
                            else if (mesAlgoType == 7)
                                retval = AnalyzeMESReceivedData_Alogorithm_HMI_3(orderdate + recorddata, orderdate.Length, ref receivedMsg[count], no2);
                            else
                                retval = AnalyzeMESReceivedData_Alogorithm00(orderdate + recorddata, ref receivedMsg[count]);
                            //retval = AnalyzeMESReceivedData_Alogorithm01(orderdate + recorddata, orderdate.Length, ref receivedMsg[count]);
                            if (retval != 0)
                                continue;
                            no2++;
                            count++;
                            if (count >= receivedMsg.Length)
                            {
                                retval = SaveMESClientReceivedData2(newtableName, /*orderdate + recorddata, */receivedMsg, count);
                                //retval = SaveMESClientReceivedData2(tableName, orderdate + recorddata, receivedMsg, count);
                                if (retval != 0)
                                {
                                    mesDBUpdateFlag = backmesDBUpdateFlag;
                                    if (sr != null)
                                        sr.Close();
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                    return;
                                }
                                count = 0;
                            }
                        }
                        else
                            break;
                    }

                    if (count >= 0)
                    {
                        retval = SaveMESClientReceivedData2(newtableName, /*orderdate + recorddata, */receivedMsg, count);
                        //retval = SaveMESClientReceivedData2(tableName, orderdate + recorddata, receivedMsg, count);
                        if (retval != 0)
                        {
                            mesDBUpdateFlag = backmesDBUpdateFlag;
                            if (sr != null)
                                sr.Close();
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                            return;
                        }
                        count = 0;
                    }
                    sw.Stop();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SaveMESClientReceivedData2 TIME : " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
                }
                if (sr != null)
                    sr.Close();

                mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);

                string datetime = DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss");
                Util.WritePrivateProfileValue("MES", "DOWNLOADTIME", datetime, Constants.PARAMS_INI_FILE);
                ShowLabelData(datetime, lblVINLastUpdateDate);

                //if (((saveMESStatus >= (int)STATUS_LABEL_NUM.STATUS_LABEL_RECV_NEXTVIN) && (saveMESStatus <= (int)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_MARKING)) ||
                if ((currentProcessStatus >= (int)STATUS_LABEL_NUM.STATUS_LABEL_RECV_NEXTVIN) && (currentProcessStatus <= (int)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_MARKING))
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CURRENT STATUS IS WORKING!!!!!", Thread.CurrentThread.ManagedThreadId);
                else
                    await ChangeDBProcess4Thread();
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CURRENT STATUS 2: " + saveMESStatus.ToString() + ", " + currentProcessStatus.ToString(), Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                mesDBUpdateFlag = backmesDBUpdateFlag;
            }

            return;
        }

        private void DBClear(string tbName)
        {
            string className = "MainWindow";
            string funcName = "DBClear";
            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            int retval = 0;

            try
            {
                string delstring = "delete from " + tbName;
                DataTable dbMainDataTable = new DataTable();
                ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                object obj = new object();
                dbwrap.ExecuteCommand(Constants.connstring, delstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);

                delstring = "ALTER TABLE " + tbName + " AUTO_INCREMENT = 1";
                dbwrap.ExecuteCommand(Constants.connstring, delstring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (DataException de)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION 2: CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
                retval = de.HResult;
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }

            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return;
        }

        private void btnClearPINCount_Click(object sender, RoutedEventArgs e)
        {
            string value = "";
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                msg2.Message = "Do you want t0 clear marking count?";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnClearPINCount_Click", "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);

                if (window.ShowDialog() == false)
                    return;

                value = "0";
                ShowLabelData(value, lblMarkingCount);
                Util.WritePrivateProfileValue("OPTION", "MarkingCount", value, Constants.PARAMS_INI_FILE);

                value = DateTime.Now.ToString("yyyy/MM/dd");
                ShowLabelData(value, lblPINChangeDate);
                Util.WritePrivateProfileValue("OPTION", "MarkingClearDate", value, Constants.PARAMS_INI_FILE);

                value = DateTime.Now.ToString("HH:mm:ss");
                ShowLabelData(value, lblPINChangeTime);
                Util.WritePrivateProfileValue("OPTION", "MarkingClearTime", value, Constants.PARAMS_INI_FILE);

            }
            catch (Exception ex)
            {

            }
        }

        private async void btnCheckDisplacement_Click(object sender, RoutedEventArgs e)
        {
            string sensor = "";
            byte[] senddata = new byte[64];
            ITNTResponseArgs recvArg = new ITNTResponseArgs();
            int retval = 0;
            string className = "MainWindow";
            string funcName = "btnCheckDisplacement_Click";
            //int nLength = 0;
            if (bUseDispalcementSensor)
            {
                recvArg = await distanceSensor.ReadSensor(3, 0);
                //recvArg = await ReadDisplacementSensor(3);
                if (recvArg.execResult != 0)
                {
                    ShowErrorMessage("LASER DISPLACEMENT SENSOR ERROR", false);
                    //ITNTErrorLog.Instance.Trace(0, "LASER DISPLACEMENT SENSOR ERROR");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISPLACEMENT SENSOR ERROR", Thread.CurrentThread.ManagedThreadId);
                    currMarkInfo.distance.sdistance1 = "EEEEEEEEEE";
                    currMarkInfo.distance.sdistance2 = "EEEEEEEEEE";
                    //OpenDistanceSensor();
                }
                else
                {
                    currMarkInfo.distance.sdistance1 = "+000000000";
                    currMarkInfo.distance.sdistance2 = "+000000000";

                    sensor = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);//, recvArg.recvSize);
                    sensor = sensor.ToString();
                    string[] vals = sensor.Split(',');
                    if (vals.Length <= 1)
                    {
                        ShowErrorMessage("LASER DISPLACEMENT DATA ERROR", false);
                        //ITNTErrorLog.Instance.Trace(0, "LASER DISPLACEMENT DATA ERROR");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISPLACEMENT DATA ERROR", Thread.CurrentThread.ManagedThreadId);
                    }
                    else
                    {
                        if (vals.Length >= 2)
                        {
                            currMarkInfo.distance.sdistance1 = vals[1];
                            double.TryParse(currMarkInfo.distance.sdistance1, out currMarkInfo.distance.distance1);
                            currMarkInfo.distance.distance1 = currMarkInfo.distance.distance1 / 1000.0d;
                            ShowLabelData(currMarkInfo.distance.distance1.ToString(), lblShowSensor1);
                            //                            ShowLabelData(currMarkInfo.sdistance1, lblShowSensor1);
                            //CheckDistanceSensor(currMarkInfo);
                        }

                        if (vals.Length >= 3)
                        {
                            currMarkInfo.distance.sdistance2 = vals[2];
                            double.TryParse(currMarkInfo.distance.sdistance2, out currMarkInfo.distance.distance2);
                            currMarkInfo.distance.distance2 = currMarkInfo.distance.distance2 / 1000.0d;
                            //ShowLabelData(currMarkInfo.distance2.ToString(), lblShowSensor2);
                            //                            ShowLabelData(currMarkInfo.sdistance2, lblShowSensor2);
                            retval = CheckDisplacement(currMarkInfo);
                            int dispoption = (int)Util.GetPrivateProfileValueUINT("CONFIG", "OPTION", 0, Constants.DISPLACEMENT_INI_FILE);
                            //if (dispoption == 1)
                            //{
                            //    recvArg = await plcComm.SendMatchingResult(4);
                            //    if (recvArg.execResult != 0)
                            //    {

                            //    }
                            //}
                            //else if (dispoption == 2)
                            //{
                            //    if (retval == 0)
                            //        recvArg = await plcComm.SendMovingRobot(0);
                            //    else
                            //        recvArg = await plcComm.SendMovingRobot(8);
                            //    if (recvArg.execResult != 0)
                            //    {

                            //    }
                            //}
                        }
                    }
                }
                //sendmsg = "M2" + seq + currMarkInfo.currMarkData.mesData.vin + rawtype + "1" + currMarkInfo.sdistance1 + currMarkInfo.sdistance2;
            }
            else
            {
                //sendmsg = "M0" + seq + currMarkInfo.currMarkData.mesData.vin + rawtype + "1";
            }

        }

        private async void cycleTimerHandler(object sender, EventArgs e)
        {
            string className = "MainWindow";
            string funcName = "cycleTimerHandler";
            //TimeSpan ts = TimeSpan.FromMilliseconds(10);

            try
            {
                string timer = string.Format("{0:mm\\:ss\\:f}", cycleWatch.Elapsed); ;// cycleWatch.ElapsedMilliseconds.ToString("");
                ShowLabelData(timer, lblcycleTime);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        //private async Task<DistanceData> GetDisplacementSensor()
        //{
        //    string sensor = "";
        //    byte[] senddata = new byte[64];
        //    ITNTResponseArgs recvArg = new ITNTResponseArgs();
        //    int retval = 0;
        //    string className = "";
        //    string funcName = "";
        //    //string distValue = "";
        //    //string sdistance1 = "";
        //    //string sdistance2 = "";
        //    DistanceData distData = new DistanceData();
        //    //int nLength = 0;

        //    try
        //    {
        //        //recvArg = await distanceSensor.ReadSensor(1, 1);
        //        distData = await ReadDisplacementSensor(1);
        //        if (distData.execResult != 0)
        //        {
        //            //ShowErrorMessage("LASER DISPLACEMENT SENSOR ERROR", false);
        //            //ITNTErrorLog.Instance.Trace(0, "LASER DISPLACEMENT SENSOR ERROR");
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISPLACEMENT SENSOR ERROR", Thread.CurrentThread.ManagedThreadId);
        //            distData.sdistance1 = "E";
        //            distData.sdistance2 = "E";
        //            distData.execResult = -1;
        //            //OpenDistanceSensor();
        //        }
        //        else
        //        {
        //            distData.sdistance1 = "+000000000";
        //            distData.sdistance2 = "+000000000";

        //            sensor = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);//, recvArg.recvSize);
        //            sensor = sensor.ToString();
        //            string[] vals = sensor.Split(',');
        //            if (vals.Length <= 1)
        //            {
        //                //ShowErrorMessage("LASER DISPLACEMENT DATA ERROR", false);
        //                //ITNTErrorLog.Instance.Trace(0, "LASER DISPLACEMENT DATA ERROR");
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISPLACEMENT DATA ERROR", Thread.CurrentThread.ManagedThreadId);
        //                distData.distance1 = 0;
        //                distData.distance1 = 0;
        //            }
        //            else
        //            {
        //                if (vals.Length >= 2)
        //                {
        //                    distData.sdistance1 = vals[1];
        //                    double.TryParse(distData.sdistance1, out distData.distance1);
        //                    distData.distance1 = distData.distance1 / 1000.0d;
        //                    //distValue = distData.distance1.ToString();
        //                    //if (showflag != 0)
        //                    //    ShowLabelData(distValue, lblShowSensor1);
        //                }

        //                if (vals.Length >= 3)
        //                {
        //                    distData.sdistance2 = vals[2];
        //                    double.TryParse(distData.sdistance2, out distData.distance2);
        //                    distData.distance2 = distData.distance2 / 1000.0d;
        //                    //distValue = distData.distance2.ToString();
        //                    //if (showflag != 0)
        //                    //    ShowLabelData(distValue, lblShowSensor2);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        distData.execResult = ex.HResult;
        //    }
        //    return distData;
        //}

        //private async Task ShowDisplacementSensor()
        //{
        //    string sensor = "";
        //    byte[] senddata = new byte[64];
        //    ITNTResponseArgs recvArg = new ITNTResponseArgs();
        //    int retval = 0;
        //    string className = "";
        //    string funcName = "";
        //    string distValue = "";
        //    //int nLength = 0;

        //    try
        //    {
        //        if (bUseDispalcementSensor)
        //        {
        //            //recvArg = await distanceSensor.ReadSensor();
        //            recvArg = await distanceSensor.ReadSensor2(1, 1);
        //            if (recvArg.execResult != 0)
        //            {
        //                ShowErrorMessage("LASER DISPLACEMENT SENSOR ERROR", false);
        //                //ITNTErrorLog.Instance.Trace(0, "LASER DISPLACEMENT SENSOR ERROR");
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISPLACEMENT SENSOR ERROR", Thread.CurrentThread.ManagedThreadId);
        //                currMarkInfo.distance.sdistance1 = "EEEEEEEEEE";
        //                currMarkInfo.distance.sdistance2 = "EEEEEEEEEE";
        //                //OpenDistanceSensor();
        //            }
        //            else
        //            {
        //                currMarkInfo.distance.sdistance1 = "+000000000";
        //                currMarkInfo.distance.sdistance2 = "+000000000";

        //                sensor = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);//, recvArg.recvSize);
        //                sensor = sensor.ToString();
        //                string[] vals = sensor.Split(',');
        //                if (vals.Length <= 1)
        //                {
        //                    ShowErrorMessage("LASER DISPLACEMENT DATA ERROR", false);
        //                    //ITNTErrorLog.Instance.Trace(0, "LASER DISPLACEMENT DATA ERROR");
        //                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "READ LASER DISPLACEMENT DATA ERROR", Thread.CurrentThread.ManagedThreadId);
        //                }
        //                else
        //                {
        //                    if (vals.Length >= 2)
        //                    {
        //                        currMarkInfo.distance.sdistance1 = vals[1];
        //                        double.TryParse(currMarkInfo.distance.sdistance1, out currMarkInfo.distance.distance1);
        //                        currMarkInfo.distance.distance1 = currMarkInfo.distance.distance1 / 1000.0d;
        //                        distValue = currMarkInfo.distance.distance1.ToString();
        //                    }

        //                    if (vals.Length >= 3)
        //                    {
        //                        currMarkInfo.distance.sdistance2 = vals[2];
        //                        double.TryParse(currMarkInfo.distance.sdistance2, out currMarkInfo.distance.distance2);
        //                        currMarkInfo.distance.distance2 = currMarkInfo.distance.distance2 / 1000.0d;
        //                        distValue = currMarkInfo.distance.distance2.ToString();
        //                    }

        //                    ShowLabelData(distValue, lblShowSensor1);
        //                }
        //            }
        //            //sendmsg = "M2" + seq + currMarkInfo.currMarkData.mesData.vin + rawtype + "1" + currMarkInfo.sdistance1 + currMarkInfo.sdistance2;
        //        }
        //        else
        //        {
        //            //sendmsg = "M0" + seq + currMarkInfo.currMarkData.mesData.vin + rawtype + "1";
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            //mesDataSaveThread = new Thread(MESDataThreadFunc);
            //mesDataSaveThread.Start();

            string value = "";

            try
            {
                Util.GetPrivateProfileValue("MES", "MESTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "5")
                    mesClient.RequestData();
                else if(value == "6")
                    mesFTB.RequestData();
            }
            catch (Exception ex)
            {

            }

        }

        private async void btnControllerReady_Click(object sender, RoutedEventArgs e)
        {
            await InitializeController();
        }

        private bool ShowErrorMessageWindow(ConfirmWindowString msg1, ConfirmWindowString msg2, ConfirmWindowString msg3, ConfirmWindowString msg4, ConfirmWindowString msg5)//, Brush msgbrush1, Brush msgbrush2, Brush msgbrush3, Brush msgbrush4)
        {
            bool ret = false;
            string className = "MainWindow";
            string funcName = "ShowErrorMessageWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                if (CheckAccess())
                {
                    WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                    warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    ret = warning.ShowDialog().Value;
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        ret = warning.ShowDialog().Value;
                    }));
                }

                return ret;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION1 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return false;
            }
        }

        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DriveInfoDeligate()
        {
            if (this.CheckAccess())
            {
                ShowDriveInfo2();
            }
            else
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    ShowDriveInfo2();
                }));
            }
        }

        string DriveCapacity(long capa, ref float capasize)
        {
            string retval = "";
            float ssize = 0;
            //ftotalsize = drvinfo.TotalSize / 1024 / 1024 / 1024;
            //ltotalsize = (long)ftotalsize;
            //lusedsize = (long)(drvinfo.TotalSize - drvinfo.AvailableFreeSpace) / 1024 / 1024 / 1024;

            if (capa >= 1024 * 1024 * 1024)
            {
                ssize = (float)capa / 1024 / 1024 / 1024;
                if (ssize > 1024)
                {
                    ssize /= 1024;
                    retval = String.Format("{0:N2}", ssize) + "TB";
                }
                else
                    retval = String.Format("{0:N2}", ssize) + "GB";
            }
            else if (capa >= 1024 * 1024)
            {
                ssize = capa / 1024 / 1024;
                retval = string.Format("{0:N2}", ssize) + "MB";
            }
            else if (capa >= 1024)
            {
                ssize = capa / 1024;
                retval = String.Format("{0:##.##}", ssize) + "KB";
            }
            else
            {
                ssize = capa;
                retval = string.Format("{0:N2}", ssize) + "Bytes";
            }

            capasize = ssize;
            return retval;
        }


        int ShowDiskInfo(int num, DriveInfo drvinfo)
        {
            int retval = 0;
            float ftotalsize = 0;
            string stotalsize = "";
            float favailsize = 0;
            string savailsize = "";
            string susedsize = "";
            float fusedsize = 0;

            stotalsize = DriveCapacity(drvinfo.TotalSize, ref ftotalsize);
            savailsize = DriveCapacity(drvinfo.AvailableFreeSpace, ref favailsize);
            susedsize = DriveCapacity(drvinfo.TotalSize - drvinfo.AvailableFreeSpace, ref fusedsize);

            if (num == 0)
            {
                //if(stpLocalC.CheckAccess() == true)
                {
                    if (stpLocalC.Visibility != Visibility)
                        stpLocalC.Visibility = Visibility;

                    //lblLocalC.Content = drvinfo.Name;
                    ShowLabelData(drvinfo.Name, lblLocalC);
                    if (drvinfo.TotalSize <= 0)
                    {
                        //lblMemSizeC.Content = "디스크 용량 없음";
                        ShowLabelData("NO DISK SPACE", lblMemSizeC);
                        retval = 0;
                    }
                    else
                    {
                        if ((drvinfo.TotalSize != 0) && (drvinfo.AvailableFreeSpace <= (drvinfo.TotalSize * 1 / 10)))
                        {
                            retval = -1;
                            //pgblocaC.Foreground = Brushes.Red;
                            //showProgressBar(pgblocaC, Brushes.Red, (double)ftotalsize, (double)fusedsize);
                            showProgressBar(pgblocaC, Brushes.Red, (double)drvinfo.TotalSize, (double)(drvinfo.TotalSize - drvinfo.AvailableFreeSpace));
                        }
                        else
                        {
                            retval = 0;
                            //pgblocaC.Foreground = Brushes.Green;
                            showProgressBar(pgblocaC, Brushes.Green, (double)drvinfo.TotalSize, (double)(drvinfo.TotalSize - drvinfo.AvailableFreeSpace));
                        }
                        //pgblocaC.Maximum = (int)ftotalsize;
                        //pgblocaC.Value = (int)(fusedsize);
                        //lblMemSizeC.Content = stotalsize + " 중 " + savailsize + " 사용 가능";
                        //ShowLabelData(stotalsize + " 중 " + savailsize + " 사용 가능", lblMemSizeC);
                        ShowLabelData(savailsize + " free of " + stotalsize, lblMemSizeC);
                    }
                }
                //else
                //{
                //    stpLocalC.Dispatcher.Invoke(new Action(delegate
                //    {
                //        if (stpLocalC.Visibility != Visibility)
                //            stpLocalC.Visibility = Visibility;
                //        //lblLocalC.Content = drvinfo.Name;
                //        ShowLabelData(drvinfo.Name, lblLocalC);
                //        if (drvinfo.TotalSize <= 0)
                //        {
                //            //lblMemSizeC.Content = "디스크 용량 없음";
                //            ShowLabelData("디스크 용량 없음", lblMemSizeC);
                //            retval = 0;
                //        }
                //        else
                //        {
                //            if ((drvinfo.TotalSize != 0) && (drvinfo.AvailableFreeSpace <= (drvinfo.TotalSize * 1 / 10)))
                //            {
                //                retval = -1;
                //                pgblocaC.Foreground = Brushes.Red;
                //            }
                //            else
                //            {
                //                retval = 0;
                //                pgblocaC.Foreground = Brushes.Green;
                //            }
                //            pgblocaC.Maximum = (int)ftotalsize;
                //            pgblocaC.Value = (int)(fusedsize);
                //            //lblMemSizeC.Content = stotalsize + " 중 " + savailsize + " 사용 가능";
                //            ShowLabelData(stotalsize + " 중 " + savailsize + " 사용 가능", lblMemSizeC);
                //        }
                //    }));
                //}
            }
            else
            {
                if (stpLocalD.Visibility != Visibility)
                    stpLocalD.Visibility = Visibility;
                ShowLabelData(drvinfo.Name, lblLocalD);

                //lblLocalC.Content = drvinfo.Name;
                if (drvinfo.TotalSize <= 0)
                {
                    //ShowLabelData("NO DISK SPACE 용량 없음", lblMemSizeD);
                    ShowLabelData("NO DISK SPACE", lblMemSizeC);
                    retval = 0;
                }
                else
                {
                    if ((drvinfo.TotalSize != 0) && (drvinfo.AvailableFreeSpace <= (drvinfo.TotalSize * 1 / 10)))
                    {
                        retval = -1;
                        showProgressBar(pgblocaD, Brushes.Red, (double)drvinfo.TotalSize, (double)(drvinfo.TotalSize - drvinfo.AvailableFreeSpace));
                    }
                    else
                    {
                        retval = 0;
                        showProgressBar(pgblocaD, Brushes.Green, (double)drvinfo.TotalSize, (double)(drvinfo.TotalSize - drvinfo.AvailableFreeSpace));
                    }
                    //ShowLabelData(stotalsize + " 중 " + savailsize + " 사용 가능", lblMemSizeD);
                    ShowLabelData(savailsize + " free of " + stotalsize, lblMemSizeD);
                }
            }

            return retval;
        }

        private void ShowDriveInfo2()
        {
            string className = "MainWindow";
            string funcName = "ShowDriveInfo2";
            int count = 0;
            int[] retval = new int[2];
            DriveInfo[] drv = new DriveInfo[2];
            string value = "";
            string[] drvName = new string[2];

            try
            {
                Util.GetPrivateProfileValue("OPTION", "USEHDD", "C|D", ref value, Constants.PARAMS_INI_FILE);
                string[] vals = value.Split('|');
                if(vals.Length > 0)
                    drvName[0] = vals[0];
                if(value.Length > 1)
                    drvName[1] = vals[1];

                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    if (count >= 2)
                        break;

                    retval[count] = 0;

                    if ((d.DriveType == DriveType.Fixed) && (d.IsReady == true) && (drvName[count].Length > 0) && (d.Name.Contains(drvName[count])))
                    {
                        if (CheckAccess())
                        {
                            retval[count] = ShowDiskInfo(count, d);
                        }
                        else
                        {
                            Dispatcher.Invoke(new Action(delegate
                            {
                                retval[count] = ShowDiskInfo(count, d);
                            }));
                        }
                        drv[count] = d;
                        count++;
                    }
                }

                if ((retval[0] != 0) || (retval[1] != 0))
                {
                    HDDSpaceWarning2(retval, drv);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2: CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void HDDSpaceWarning2(int[] result, DriveInfo[] drvinfo)
        {
            string className = "MainWindow";
            string funcName = "HDDSpaceWarning2";

            string errmsg = "";
            string drivename = "";

            try
            {
                if (result[0] != 0)
                    drivename = drvinfo[0].Name;

                if (result[1] != 0)
                {
                    if (drivename.Length > 0)
                        drivename += ", " + drvinfo[1].Name;
                    else
                        drivename = drvinfo[1].Name;
                }
                if (drivename.Length > 0)
                    errmsg = "Insufficient storage capacity on disk " + drivename;

                if (lblDiskCapacityWarning.CheckAccess())
                {
                    if (drivename.Length > 0)
                    {
                        lblDiskCapacityWarning.Content = errmsg;
                        lblDiskCapacityWarning.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        lblDiskCapacityWarning.Content = "";
                        lblDiskCapacityWarning.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    lblDiskCapacityWarning.Dispatcher.Invoke(new Action(delegate
                    {
                        if (drivename.Length > 0)
                        {
                            lblDiskCapacityWarning.Content = errmsg;
                            lblDiskCapacityWarning.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            lblDiskCapacityWarning.Content = "";
                            lblDiskCapacityWarning.Visibility = Visibility.Collapsed;
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}/{0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private void showProgressBar(ProgressBar progress, Brush forebrush, double maximum, double value)
        {
            if(progress.CheckAccess() == true)
            {
                progress.Foreground = forebrush;
                progress.Maximum = maximum;
                progress.Value = value;
            }
            else
            {
                progress.Dispatcher.Invoke(new Action(delegate
                {
                    progress.Foreground = forebrush;
                    progress.Maximum = maximum;
                    progress.Value = value;
                }));
            }
        }


        private void ScrollViewToPoint(DataGrid grid)
        {
            DataRowView dr = null;// = new DataRowView();
            try
            {
                if (grid.CheckAccess())
                {
                    foreach (DataRowView row in grid.Items)
                    {
                        if (row.Row[Constants.DB_NAME_CHECKFLAG].ToString() == "1")
                        {
                            dr = row;
                            break;
                        }
                    }

                    if (dr != null)
                        grid.ScrollIntoView(dr);
                }
                else
                {
                    grid.Dispatcher.Invoke(new Action(delegate
                    {
                        foreach (DataRowView row in grid.Items)
                        {
                            if (row.Row[Constants.DB_NAME_CHECKFLAG].ToString() == "1")
                            {
                                dr = row;
                                break;
                            }
                        }

                        if (dr != null)
                            grid.ScrollIntoView(dr);
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "btnGotoPoint_Click", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        void ShowWorkPlanCount4Thread(Label countLabel, int count)
        {
            if (countLabel.CheckAccess())
            {
                countLabel.Content = count.ToString();
            }
            else
            {
                countLabel.Dispatcher.Invoke(new Action(delegate
                {
                    countLabel.Content = count.ToString();
                }));
            }
        }

        void ShowMESReceivedTime4Thread(Label dateLabel, string datetime, bool writeflag)
        {
            if (dateLabel.CheckAccess())
            {
                dateLabel.Content = datetime;
            }
            else
            {
                dateLabel.Dispatcher.Invoke(new Action(delegate
                {
                    dateLabel.Content = datetime;
                }));
            }

            if (writeflag)
                Util.WritePrivateProfileValue("MES", "DOWNLOADTIME", datetime, Constants.PARAMS_INI_FILE);
        }


        private async void CheckPlanDataCountWarning(int count, Label lblWarning/*DataGrid datagrid*/)
        {
            string className = "MainWindow";
            string funcName = "CheckPlanDataCountWarning";
            string value = "";
            int setcount = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                Util.GetPrivateProfileValue("OPTION", "MESCOUNT", "50", ref value, Constants.PARAMS_INI_FILE);
                int.TryParse(value, out setcount);

                if (setcount == -1)
                    return;

                if (lblWarning.CheckAccess())
                {
                    if (setcount <= 0)
                    {
                        showBlinkCount = false;
                        if (lblWarning.Visibility == Visibility.Visible)
                            lblWarning.Visibility = Visibility.Collapsed;
                        return;
                    }

                    if (count >= setcount)
                    {
                        if (lblWarning.Visibility == Visibility.Visible)
                        {
                            await plcComm.SendCountWanring(0);
                            lblWarning.Visibility = Visibility.Hidden;
                        }
                    }
                    else
                    {
                        if (lblWarning.Visibility != Visibility.Visible)
                        {
                            await plcComm.SendCountWanring(1);
                            lblWarning.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    lblWarning.Dispatcher.Invoke(new Action(delegate
                    {
                        if (setcount <= 0)
                        {
                            showBlinkCount = false;
                            if (lblWarning.Visibility == Visibility.Visible)
                                lblWarning.Visibility = Visibility.Collapsed;
                            return;
                        }

                        if (count >= setcount)
                        {
                            //if(showBlinkCount == true)
                            //{
                            //    showBlinkCount = false;
                            //}
                            if (lblWarning.Visibility == Visibility.Visible)
                                lblWarning.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            //if (showBlinkCount == false)
                            //{
                            //    showBlinkCount = true;
                            //}
                            if (lblWarning.Visibility != Visibility.Visible)
                                lblWarning.Visibility = Visibility.Visible;
                        }
                    }));
                }

                if (setcount > count)
                    ShowMESConnectionError();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        async void SoftBlinkCountWarning(Object obj)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int count = 0;
            string value = "";
            string[] vals;
            int idx = 0;
            WARNINGBLINKPARAM param = (WARNINGBLINKPARAM)obj;
            string message = "";

            try
            {
                Util.GetPrivateProfileValue("MESSAGE", "WARNING1", "   Data is less than 50.|   Press [REQUEST DATA] button. Call MES attendant.", ref value, Constants.PARAMS_INI_FILE);
                vals = value.Split('|');
                for (int i = 0; i < vals.Length; i++)
                {
                    if (vals[i].Length > 0)
                    {
                        if (idx != 0)
                            message += "\r";
                        message += vals[i];
                        idx++;
                    }
                }

                if (param.ctrl.CheckAccess())
                {
                    short halfCycle = (short)Math.Round(param.CycleTime_ms * 0.5);
                    SolidColorBrush backbrush;// = new SolidColorBrush(clr);
                    ((Label)param.ctrl).Content = message;
                    while (showWarningFlag)
                    {
                        await Task.Delay(2);
                        var n = count % param.CycleTime_ms;
                        //var n = sw.ElapsedMilliseconds % param.CycleTime_ms;
                        var per = (double)Math.Abs(n - halfCycle) / halfCycle;
                        var red = (short)Math.Round((param.backc2.R - param.backc1.R) * per) + param.backc1.R;
                        var grn = (short)Math.Round((param.backc2.G - param.backc1.G) * per) + param.backc1.G;
                        var blw = (short)Math.Round((param.backc2.B - param.backc1.B) * per) + param.backc1.B;
                        var clrback = Color.FromArgb(255, (byte)red, (byte)grn, (byte)blw);

                        backbrush = new SolidColorBrush(clrback);
                        param.ctrl.Background = backbrush;
                        count += 10;
                        if (count > param.CycleTime_ms)
                            count = 0;
                        //param.ctrl.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    param.ctrl.Dispatcher.Invoke(new Action(async delegate
                    {
                        short halfCycle = (short)Math.Round(param.CycleTime_ms * 0.5);
                        SolidColorBrush backbrush;// = new SolidColorBrush(clr);
                        ((Label)param.ctrl).Content = message;
                        while (showWarningFlag)
                        {
                            await Task.Delay(2);
                            var n = count % param.CycleTime_ms;
                            //var n = sw.ElapsedMilliseconds % param.CycleTime_ms;
                            var per = (double)Math.Abs(n - halfCycle) / halfCycle;
                            var red = (short)Math.Round((param.backc2.R - param.backc1.R) * per) + param.backc1.R;
                            var grn = (short)Math.Round((param.backc2.G - param.backc1.G) * per) + param.backc1.G;
                            var blw = (short)Math.Round((param.backc2.B - param.backc1.B) * per) + param.backc1.B;
                            var clrback = Color.FromArgb(255, (byte)red, (byte)grn, (byte)blw);

                            backbrush = new SolidColorBrush(clrback);
                            param.ctrl.Background = backbrush;
                            count += 10;
                            if (count > param.CycleTime_ms)
                                count = 0;
                        }
                    }));
                }
                sw.Stop();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "SoftBlinkCountWarning", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnGotoPoint_Click(object sender, RoutedEventArgs e)
        {
            ScrollViewToPoint(dgdPlanData);
        }

        private void MenuItem_TestLaser_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_SetLaserSource_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_ResetJob_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "MenuItem_ResetJob_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs();

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();


            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                msg2.Message = "DO YOU WANT TO INITIALIZE JOB?";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Blue;
                msg2.Background = Brushes.White;

                msg3.Message = "DO NOT USE THIS FUNCTION DURING MARKING.";
                msg3.Fontsize = 18;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Black;
                msg3.Background = Brushes.White;

                //msg4.Message = "작업에는 사용금지";
                //msg4.Fontsize = 16;
                //msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg4.VerticalContentAlignment = VerticalAlignment.Center;
                //msg4.Foreground = Brushes.Red;
                //msg4.Background = Brushes.White;

                ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "YES", "NO", this);// , "", "", this);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == false)
                    return;

                recvStatus = "";

                ShowLog(className, funcName, 0, "EXECUTE INITIALIZING JOB", "");
                ShowErrorMessage("", true);
                m_bDoingMarkingFlag = false;
                currMarkInfo.Initialize();

                currMarkInfo.currMarkData.isReady = false;
                doingCommand = false;
                seqcheckError = 0;
                m_currCMD = 0;

                //laserErrorFlag = 0;
                //motorErrorFlag = 0;
                lblLaserError.Background = Brushes.Green;
                lblLaserError.Content = "0";

                lblMotorError.Background = Brushes.Green;
                lblMotorError.Content = "0";

                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_READY_MARK);
                //currentProcessStatus = 0;

                markRunTimer.Stop();


                //if (bControllerInitFlag == 0)
                //{
                //    retval = InitializeControllerLaser().Result;
                //    if (retval.execResult == 0)
                //        bControllerInitFlag = 1;
                //}
            }
            catch (Exception ex)
            {

            }
        }

        //private void btnStatusDescription_Click(object sender, RoutedEventArgs e)
        //{

        //}

        void ShowMESConnectionError()
        {
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            string className = "MainWindow";
            string funcName = "ShowMESConnectionError";

            try
            {
                msg1.Message = "MES CONNECTION ERROR!!!!!!!";
                msg1.Fontsize = 18;
                msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg1.VerticalContentAlignment = VerticalAlignment.Center;
                msg1.Foreground = Brushes.Red;
                msg1.Background = Brushes.White;

                msg2.Message = "Close the program and turn off the PC.";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                msg3.Message = "Turn on the PC and wait for about 5 minutes.";
                msg3.Fontsize = 18;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Red;
                msg3.Background = Brushes.White;

                msg4.Message = "Please restart the program after 5 minutes.";
                msg4.Fontsize = 18;
                msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg4.VerticalContentAlignment = VerticalAlignment.Center;
                msg4.Foreground = Brushes.Red;
                msg4.Background = Brushes.White;

                if (CheckAccess())
                {
                    ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "", "", "", this);
                    window.Owner = System.Windows.Application.Current.MainWindow;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Topmost = true;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);
                    window.Show();
                }
                else
                {
                    this.Dispatcher.Invoke(new Action(delegate
                    {
                        ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, "", "", "", this);
                        window.Owner = System.Windows.Application.Current.MainWindow;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        window.Topmost = true;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SHOW DIALOG", Thread.CurrentThread.ManagedThreadId);
                        window.Show();
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private async void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    string className = "MainWindow";
        //    string funcName = "TESTVISION1";
        //    ITNTSendArgs args = new ITNTSendArgs();
        //    string sendmsg = "";
        //    string seq = "0034";
        //    string rawtype = "3555";
        //    string vin = " KMFZCY7KBNU905102 ";
        //    int retval = 0;

        //    //
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 DISPLACEMENT START", Thread.CurrentThread.ManagedThreadId);
        //    sendmsg = "M6" + seq + vin + rawtype + "1" + "+000000195" + "+100000000";
        //    args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
        //    args.sendString = sendmsg;
        //    args.dataSize = sendmsg.Length;
        //    retval = await vision.SendMessage(args);
        //    if (retval != 0)
        //    {
        //        //Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
        //        //if (value == "0")
        //        //    await plcComm.SendVisionResult("N");
        //        //else
        //        //{
        //        //    await plcComm.SendVisionResult("O");
        //        //    ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
        //        //}

        //        //ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO VISION ERROR");
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO VISION ERROR", Thread.CurrentThread.ManagedThreadId);
        //        return;
        //    }
        //}

        //private void btnVisionTest2_Click(object sender, RoutedEventArgs e)
        //{

        //}

        //private async void Laser_DoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    retval = await laserSource.ResetErrors();
        //}

        private void CheckUpgradeDatabase()
        {
            string className = "MainWindow";
            string funcName = "CheckUpgradeDatabase";
            string value = "";
            Stopwatch sw = new Stopwatch();

            try
            {
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref tableName, Constants.PARAMS_INI_FILE);
                //Util.GetPrivateProfileValue("OPTION", "SHOWDBTEXT", "0", ref value, Constants.PARAMS_INI_FILE);
                //if (value == "1")
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY SEQUENCE ASC, DATE(PRODUCTDATE) ASC";
                //else if (value == "2")
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY NO ASC";
                //else
                //    DBDisplayCommand = "SELECT * from " + tableName + " ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";

                Util.GetPrivateProfileValue("OPTION", "MESUPDATEFLAG", "0", ref value, Constants.PARAMS_INI_FILE);
                int.TryParse(value, out mesDBUpdateFlag);
                if ((mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE) || (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING))
                {
                    mesDataSaveThread = new Thread(new ParameterizedThreadStart(CCR2WORK3));
                    mesDataSaveThread.Start(0);

                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromSeconds(10))
                    {
                        Task.Delay(10).Wait();
                        if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                            break;
                    }
                    sw.Stop();
                }
                else
                    mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_IDLE;

                if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                    ChangeDBProcess().Wait();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnSearchData_Click(object sender, RoutedEventArgs e)
        {
            string className = "MainWindow";
            string funcName = "btnSearch_Click";

            SearchSequenceWindow window = new SearchSequenceWindow();

            try
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}/{0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnResetErrorList_Click(object sender, RoutedEventArgs e)
        {
            Brush brush;
            if (lblError.CheckAccess())
            {
                brush = new SolidColorBrush(Color.FromArgb(255, (byte)0xc0, (byte)0xc0, (byte)0xc0));
                lblError.Background = brush;
                lblError.Content = "";
            }
            else
            {
                lblError.Dispatcher.Invoke(new Action(delegate
                {
                    brush = new SolidColorBrush(Color.FromArgb(255, (byte)0xc0, (byte)0xc0, (byte)0xc0));
                    lblError.Background = brush;
                    lblError.Content = "";
                }));
            }
        }

        private async void MenuItem_LoadDataFile_Click(object sender, RoutedEventArgs e)
        {
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();

            try
            {
                if (CheckAccess())
                    await LoadMESDataFromFile(dgdPlanData);
                else
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
                        await LoadMESDataFromFile(dgdPlanData);
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_SetMarkPoint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

        }

        private void btnShowStatusCode_Click(object sender, RoutedEventArgs e)
        {

        }

        //private void MenuItem_LoadData4File_Click(object sender, RoutedEventArgs e)
        //{

        //}
        private string MakeDBDisplayText(string tbName)
        {
            string retval = "";
            string value = "";
            try
            {
                Util.GetPrivateProfileValue("OPTION", "SHOWDBTEXT", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "1")
                    retval = "SELECT * from " + tbName + " ORDER BY SEQUENCE ASC, DATE(PRODUCTDATE) ASC";
                else if (value == "2")
                    retval = "SELECT * from " + tbName + " ORDER BY NO2 ASC";
                else
                    retval = "SELECT * from " + tbName + " ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
            }
            catch (Exception ex)
            {
                retval = "SELECT * from " + tbName + " ORDER BY SEQUENCE ASC, DATE(PRODUCTDATE) ASC";
            }

            return retval;
        }

        private async Task InsertPlanData(DataGrid grid)
        {
            string className = "MainWindow";
            string funcName = "InsertPlanData";

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string filename = "";
            int i = 0;
            BackgroundWorker backSaveData = new BackgroundWorker();
            DataRowView rv1 = null;
            DataRowView rv2 = null;
            MESReceivedData rowData = new MESReceivedData();
            MESReceivedData rowData2 = new MESReceivedData();
            MESReceivedData insData = new MESReceivedData();
            bool selResult = false;

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dt = new DataTable();
            object obj = new object();


            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                rv1 = grid.SelectedItem as DataRowView;
                if(rv1 == null)
                {
                    //msg2.Message = "PLEASE SELECT DATA IS MARKING NOW.";
                    //msg2.Fontsize = 18;
                    //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    //msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    //msg2.Foreground = Brushes.Blue;
                    //msg2.Background = Brushes.White;

                    msg3.Message = "PLEASE SELECT DATA.";
                    msg3.Fontsize = 18;
                    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                    msg3.Foreground = Brushes.Blue;
                    msg3.Background = Brushes.White;

                    WarningWindow warning1 = new WarningWindow("INSERT DATA", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning1.ShowDialog();
                    return;
                }

                if ((currentProcessStatus >= (byte)STATUS_LABEL_NUM.STATUS_LABEL_RECV_NEXTVIN) && (currentProcessStatus <= (byte)STATUS_LABEL_NUM.STATUS_LABEL_RUN_MARKING))
                {
                    msg2.Message = "MARKING IS CURRENTLY IN PROCESS. (" + rowData.sequence + " AND " + rowData2.sequence + ")";// THE SELECTED DATA AS A SET POINT?";
                    msg2.Fontsize = 18;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Blue;
                    msg2.Background = Brushes.White;

                    //msg3.Message = "(" + seq_cur + ", " + vin_cur + ")";
                    msg3.Message = "PLEASE TRY AGAIN AFTER MARKING IS COMPLETE";
                    msg3.Fontsize = 18;
                    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                    msg3.Foreground = Brushes.Blue;
                    msg3.Background = Brushes.White;

                    WarningWindow warning1 = new WarningWindow("INSERT DATA", msg1, msg2, msg3, msg4, msg5, "OK", "", this, 1);
                    warning1.ShowDialog();
                    return;
                }

                int index = grid.Items.IndexOf(rv1);
                if (index < 0 || index < grid.Items.Count - 2)
                {
                    rv2 = grid.Items[index + 1] as DataRowView;
                }
                else
                    rv2 = rv1;

                rowData.sequence = rv1.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                //rowData.markvin = rv1.Row.ItemArray[Constants.DB_NAME_VIN].ToString();
                rowData.rawvin = GetRawVIN(rv1.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, rv1.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                rowData.markvin = AddMonthCode(rowData.rawvin);
                int.TryParse(rv1.Row.ItemArray[Constants.DB_NAME_NO2].ToString(), out rowData.no2);

                rowData2.sequence = rv2.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                //rowData2.markvin = rv2.Row.ItemArray[Constants.DB_NAME_VIN].ToString();
                rowData2.rawvin = GetRawVIN(rv2.Row, "RAWVIN", Constants.DB_NAME_RAWVIN, rv2.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString());
                rowData2.markvin = AddMonthCode(rowData2.rawvin);
                int.TryParse(rv2.Row.ItemArray[Constants.DB_NAME_NO2].ToString(), out rowData2.no2);

                //if (rowData.sequence == rowData2.sequence)
                    msg2.Message = "DO YOU WANT TO INSERT DATA AFTER " + rowData.sequence + "?";// THE SELECTED DATA AS A SET POINT?";
                //else
                //    msg2.Message = "DO YOU WANT TO INSERT DATA BETWEEN " + rowData.sequence + " AND " + rowData2.sequence + "?";// THE SELECTED DATA AS A SET POINT?";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Blue;
                msg2.Background = Brushes.White;

                ////msg3.Message = "(" + seq_cur + ", " + vin_cur + ")";
                //msg3.Message = "(" + rowData.sequence + ", " + rowData.vin + ")";
                //msg3.Fontsize = 18;
                //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                //msg3.VerticalContentAlignment = VerticalAlignment.Center;
                //msg3.Foreground = Brushes.Blue;
                //msg3.Background = Brushes.White;


                WarningWindow warning = new WarningWindow("INSERT DATA", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                selResult = warning.ShowDialog().Value;
                if (!selResult)
                    return;

                EnterPWWindow enterpw = new EnterPWWindow();
                enterpw.Owner = System.Windows.Application.Current.MainWindow;
                enterpw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (enterpw.ShowDialog() == false)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "USER CANCEL PASSWORD", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                selResult = false;
                DataRowView tmprv = null;
                if (rowData.sequence == rowData2.sequence)
                    tmprv = null;
                else
                    tmprv = rv2;

                InsertDataWindow insWindow = new InsertDataWindow(rv1, tmprv, this);
                selResult = insWindow.ShowDialog().Value;
                if (!selResult)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "USER CANCEL INSERTING", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "BEFORE ADD + 1", Thread.CurrentThread.ManagedThreadId);
                // STEP 1: 삽입할 위치 이상의 모든 데이터를 +1씩 밀기
                if (rowData2.no2 > rowData.no2)
                {
                    string updateQuery = "UPDATE " + tableName + " SET NO2 = NO2 + 1 WHERE NO2 >= " + rowData2.no2.ToString();
                    dbwrap.ExecuteCommand(Constants.connstring, updateQuery, CommandMode.NonQuery, CommandTypeEnum.Text, ref dt, ref obj);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "AFTER ADD + 1", Thread.CurrentThread.ManagedThreadId);

                // STEP 2: 새 데이터 삽입
                insData.no2 = rowData2.no2;
                insData.sequence = insWindow.txtSequence.Text;
                insData.markvin = insWindow.txtVINData.Text;
                insData.rawvin = insWindow.txtVINData.Text;
                insData.rawcartype = insWindow.txtCarType.Text;
                if (insData.rawcartype.Length <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CAR TYPE VALUE ERROR : '" + insData.rawcartype + "'", Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                //string tmp = insData.rawcartype.Substring(0, 1);
                //insData.cartype = GetCarType(tmp, insData.rawcartype);
                insData.cartype = GetCarType(insData.rawcartype);

                if (insData.cartype == "NQ")
                {
                    if (insData.rawcartype.Length > 2)
                    {
                        string tmp2 = insData.rawcartype.Substring(2, 1);
                        if ((tmp2 == "3") || (tmp2 == "7"))
                            insData.cartype = "NQPHEV";
                    }
                    else
                        insData.cartype = "NQ";
                }


                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INSERTING DATA SEQ = '" + insData.sequence + "'", Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INSERTING DATA VIN = '" + insData.rawvin + "'", Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INSERTING DATA CAR TYPE = '" + insData.rawcartype + "'", Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INSERTING DATA MODEL = '" + insData.cartype + "'", Thread.CurrentThread.ManagedThreadId);

                insData.isInserted = "1";
                DateTime datetime = DateTime.Now;
                string insertQuery = "INSERT INTO " + tableName + " (PRODUCTDATE, SEQUENCE, RAWCARTYPE, VIN, MESDATE, MESTIME, CARTYPE, MARKDATE, MARKTIME, REMARK, ISMARK, COMPLETE, EXIST, CHECKFLAG, RAWVIN, NO2, ISINSERT) VALUES ('" +
                                    datetime.ToString("yyyy-MM-dd") + "', '" + insData.sequence + "', '" + insData.rawcartype + "', '" + insData.markvin + "', '" +
                                    datetime.ToString("yyyy-MM-dd") + "', '" + datetime.ToString("HH:mm:ss") + "', '" + insData.cartype + "', '" +
                                    datetime.ToString("yyyy-MM-dd") + "', '" + datetime.ToString("HH:mm:ss") + "', 'N', 'N', 'N', 'Y', 0, '" + insData.rawvin + "', " + insData.no2 + ",'" + insData.isInserted + "')";
                dbwrap.ExecuteCommand(Constants.connstring, insertQuery, CommandMode.NonQuery, CommandTypeEnum.Text, ref dt, ref obj);

                ShowPlanDataList(dgdPlanData);
                ScrollViewToPoint(dgdPlanData);

                //using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                //{
                //    updateCmd.Parameters.AddWithValue("@insertValue", insertValue);
                //    updateCmd.ExecuteNonQuery();
                //}


                //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                //dlg.FileName = "DATA";             // Default file name
                //dlg.DefaultExt = ".dat";                // Default file extension
                //dlg.Filter = "DATA (.dat)|*.dat|All files (*.*)|*.*";    // Filter files by extension

                //// Show open file dialog box
                //Nullable<bool> result = dlg.ShowDialog();
                //// Process open file dialog box results
                //if (result == true)
                //{
                //    // Open document
                //    filename = dlg.FileName;
                //    filename.ToUpper();
                //    //string[] lines = System.IO.File.ReadAllLines(filename);


                //    backSaveData.DoWork += worker_MESDataSave_DoWork3;
                //    backSaveData.RunWorkerCompleted += worker_MESDataSave_RunWorkerCompleted;

                //    backSaveData.RunWorkerAsync(filename);
                //}
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DATA EXCEPTION : CODE = {0}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private async void MenuItem_InsertPlanData(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckAccess())
                    await InsertPlanData(dgdPlanData);
                else
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
                        await InsertPlanData(dgdPlanData);
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_SetMarkPoint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }
        private async void MenuItem_SendDataVision(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (CheckAccess())
                //    await Send2Vision(dgdCompleteData2);
                //else
                //{
                //    Dispatcher.Invoke(new Action(async delegate
                //    {
                //        await Send2Vision(dgdCompleteData2);
                //    }));
                //}
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "MenuItem_SetMarkPoint", string.Format("Error - Exception : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private async void btnSendVision_Click(object sender, RoutedEventArgs e)
        //{
        //    string className = "MainWindow";
        //    string funcName = "btnSendVision_Click";

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    try
        //    {
        //        retval = await MakeCurrentMarkData(dgdPlanData, 1);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
        //            ITNTErrorCode(className, funcName, "TEST", retval.errorInfo);

        //            return;
        //        }

        //        retval.execResult = await SendData2Vision(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern.name, currMarkInfo.distance, currMarkInfo.currMarkData.multiMarkFlag, 1, bHeadType);
        //        if (retval.execResult != 0)
        //        {
        //            //Util.GetPrivateProfileValue("VISION", "BYPASSOPTION", "0", ref value, Constants.PARAMS_INI_FILE);
        //            //if (value == "0")
        //            //    await plcComm.SendVisionResult("N");
        //            //else
        //            //{
        //            //    await plcComm.SendVisionResult("O");
        //            //    //ShowErrorMessage("COMMUNICATION TO VISION ERROR", false);
        //            //}
        //            ////ITNTErrorLog.Instance.Trace(0, "COMMUNICATION TO VISION ERROR");
        //            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO VISION ERROR", Thread.CurrentThread.ManagedThreadId);

        //            ////log = "SEND DATA TO VISION (SendData2Vision) ERROR = " + recvArg.execResult.ToString();
        //            ////ShowLog(className, funcName, 2, log);

        //            ////ShowLog(className, funcName, 2, stepstring + "-1] SEND DATA TO VISION ERROR", recvArg.execResult.ToString());
        //            ////ITNTErrorCode();
        //            //retval.errorInfo.sErrorMessage = "COMMUNICATION ERROR TO VISION";
        //            //retval.errorInfo.sErrorFunc = sCurrentFunc;

        //            ITNTErrorCode(className, funcName, "TEST", retval.errorInfo);

        //            return;
        //        }
        //    }
        //    catch (Exception ex)
        //    { }
        //}

        private void btnReConnPLC_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnReConnMark_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnReConnLaser_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnReConnDistance_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            _isBusy = true;
            _cts = new CancellationTokenSource();
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                //ResultText.Text = "측정값 읽는 중...";
                //string response = await distanceSensor!.SendCommandAsync("M0", _cts.Token);
                retval = await distanceSensor.ReadSensor(3, 0);
                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //ResultText.Text = retval.recvString;
            }
            catch (OperationCanceledException)
            {
                //ResultText.Text = "작업 취소됨.";
            }
            catch (TimeoutException)
            {
                //ResultText.Text = "응답 시간 초과";
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류 발생: " + ex.Message);
            }
            finally
            {
                _isBusy = false;
                _cts.Dispose();
                _cts = null;
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            _isBusy = true;
            _cts = new CancellationTokenSource();

            try
            {
                //ResultText.Text = "초기화 중...";
                //string response = await distanceSensor!.SendCommandAsync("RS", cancellationToken:_cts.Token);
                //ResultText.Text = response;
            }
            catch (OperationCanceledException)
            {
                //ResultText.Text = "작업 취소됨.";
            }
            catch (TimeoutException)
            {
                //ResultText.Text = "응답 시간 초과";
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류 발생: " + ex.Message);
            }
            finally
            {
                _isBusy = false;
                _cts.Dispose();
                _cts = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        private ITNTResponseArgs UpdateNextMarkData(MESReceivedData mesdata)
        {
            string className = "MainWindow";
            string funcName = "UpdateNextMarkData";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            DataTable dbMainDataTable = new DataTable();
            DataTable dbTable = new DataTable();
            object obj = new object();
            string sCurrentFunc = "UPDATE NEXT DATA";

            try
            {
                dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=0 WHERE CHECKFLAG=1", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                dbwrap.ExecuteCommand(Constants.connstring, "UPDATE " + tableName + " SET CHECKFLAG=1 WHERE RAWVIN='" + mesdata.rawvin + "' AND SEQUENCE='" + mesdata.sequence + "'", CommandMode.NonQuery, CommandTypeEnum.Text, ref dbMainDataTable, ref obj);
                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", mesdata.sequence.Trim() + "|" + mesdata.rawvin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "UPDATE END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;

                return retval;
            }

            return retval;
        }

        private void showToolTip()
        {
            int i = 0;
            string keyHead = "TOOLTIP";
            string value = "";
            string text1 = "";
            string text2 = "";
            string text3 = "";
            string lblHead = "lblSeqStatusE";
            Label label = null;

            try
            {
                Util.GetPrivateProfileValue("OPTION", "USETOOLTIP", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value == "0")
                    return;

                for (i = 0; i < 14; i++)
                {
                    keyHead = "TOOLTIP" + i.ToString("D2");
                    Util.GetPrivateProfileValue(keyHead, "USE", "0", ref value, Constants.TOOLTIP_INI_FILE);
                    if(value != "0")
                    {
                        Util.GetPrivateProfileValue(keyHead, "TEXT1", "", ref text1, Constants.TOOLTIP_INI_FILE);
                        Util.GetPrivateProfileValue(keyHead, "TEXT2", "", ref text2, Constants.TOOLTIP_INI_FILE);
                        Util.GetPrivateProfileValue(keyHead, "TEXT3", "", ref text3, Constants.TOOLTIP_INI_FILE);

                        var stack = new StackPanel { Orientation = Orientation.Vertical };

                        stack.Children.Add(new TextBlock
                        {
                            Text = text1,
                            FontWeight = FontWeights.Bold
                        });

                        stack.Children.Add(new TextBlock
                        {
                            Text = text2,
                            Foreground = Brushes.Blue
                        });

                        stack.Children.Add(new TextBlock
                        {
                            Text = text3,
                            Foreground = Brushes.Red
                        });

                        keyHead = "lblSeqStatusE" + i.ToString("D2");
                        label = (Label)FindName(keyHead);
                        if(label != null)
                            label.ToolTip = new ToolTip { Content = stack };
                    }
                }
            }
            catch (Exception ex)
            { }
        }
    }

}
