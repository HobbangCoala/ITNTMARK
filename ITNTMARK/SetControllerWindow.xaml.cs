using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ITNTUTIL;
using ITNTCOMMON;
using ITNTCOMMM;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using System.Threading;
//using S7.Net.Types;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{

    public struct MotorSpeed
    {
        public short initSpeed;
        public short targetSpeed;
        public short accelSpeed;
        public short decelSpeed;
    };

    /// <summary>
    /// SetControllerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SetControllerWindow : Window
    {
        public static bool Wait_IO;
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();
        Point NOW_POS = new Point();
        byte m_currCMD = 0;
        bool doingCommand = false;
        PatternValueEx orgPattern = new PatternValueEx();
        bool saveFlag = false;
        short stepLength_u = 0;
        short stepLength = 0;
        byte fwVersionFlag = 0;

        bool bshowAlready = false;


        public SetControllerWindow()
        {
            string value = "";
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;

            ////DisplayValue();
            //CB_font.Items.Add("11X16");
            //CB_font.Items.Add("5X7");
            //CB_font.Items.Add("OCR");
            //CB_font.Items.Add("HMC5");
            Util.GetPrivateProfileValue("FONTLIST", "NAME", "7X12|11X16|5X7|OCR|HMC5", ref value, Constants.FONT_INI_FILE);
            string[] vals = value.Split('|');
            for (int i = 0; i < vals.Length; i++)
            {
                if (vals[i].Length > 0)
                    cbxFontName.Items.Add(vals[i]);
            }

            List<string> names = new List<string>();
            string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
            names = DirFileSearch(patternfile, "*.ini").Result;
            for(int i = 0; i < names.Count; i++)
                cbxPatternList.Items.Add(names[i]);
            if (names.Count > 0)
            {
                //DisplayValue(names[0]);
                cbxPatternList.SelectedIndex = 0;
            }

            stepLength_u = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);
            if (stepLength_u <= 0)
                stepLength = 100;
            stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
            if (stepLength <= 0)
                stepLength = 50;

            if(((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion.Length <= 0)
            {
                string ver = "";
                ver = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GetFWVersion().Result;
                if (ver.Length > 0)
                {
                    if (ver.CompareTo("101") >= 0)
                        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 1;
                    else
                        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
                    //lblfwVersion.Content = ver;
                    ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion = ver;
                }
                else
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
                }
            }

            if(((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion.Length > 0)
                lblfwVersion.Content = "FIRMWARE VER : " + ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion;

            ((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 2;
        }

        public SetControllerWindow(string pattern, string vin)
        {
            string value = "";

            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;

            //Util.GetPrivateProfileValue("FONTLIST", "NAME", "", ref value, Constants.FONT_INI_FILE);
            ////DisplayValue();
            //CB_font.Items.Add("11X16");
            //CB_font.Items.Add("5X7");
            //CB_font.Items.Add("OCR");
            //CB_font.Items.Add("HMC5");
            Util.GetPrivateProfileValue("FONTLIST", "NAME", "7X12|11X16|5X7|OCR|HMC5", ref value, Constants.FONT_INI_FILE);
            string[] vals = value.Split('|');
            for (int i = 0; i < vals.Length; i++)
            {
                if (vals[i].Length > 0)
                    cbxFontName.Items.Add(vals[i]);
            }

            List<string> names = new List<string>();
            string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
            names = DirFileSearch(patternfile, "*.ini").Result;
            for (int i = 0; i < names.Count; i++)
                cbxPatternList.Items.Add(names[i]);
            if (names.Count > 0)
            {
                //DisplayValue(names[0]);
                if (names.Contains(pattern))
                    cbxPatternList.SelectedItem = pattern;
                else
                    cbxPatternList.SelectedIndex = 0;
            }

            stepLength_u = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);
            if (stepLength_u <= 0)
                stepLength = 100;
            stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
            if (stepLength <= 0)
                stepLength = 50;

            Txt_data_Manual.Text = vin;

            if (((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion.Length <= 0)
            {
                string ver = "";
                ver = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GetFWVersion().Result;
                if (ver.Length > 0)
                {
                    if (ver.CompareTo("101") >= 0)
                        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 1;
                    else
                        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
                    //lblfwVersion.Content = ver;
                    ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion = ver;
                }
                else
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion = "";
                    ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
                }
            }

            lblfwVersion.Content = ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion;

            ((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 2;
        }

        private void ShowLog(string log)
        {
            string trace = "";
            DateTime dt = DateTime.Now;
            try
            {
                trace = dt.ToString("yyyy-MM-dd HH:mm:ss    ");
                trace += log;
                if (lsbResult.CheckAccess())
                {
                    lsbResult.Items.Add(trace);
                    if(lsbResult.Items.Count > 0)
                    {
                        lsbResult.SelectedIndex = lsbResult.Items.Count;
                        lsbResult.ScrollIntoView(lsbResult.SelectedItem);
                    }
                }
                else
                {
                    lsbResult.Dispatcher.Invoke(new Action(delegate
                    {
                        lsbResult.Items.Add(trace);
                        if (lsbResult.Items.Count > 0)
                        {
                            lsbResult.SelectedIndex = lsbResult.Items.Count;
                            lsbResult.ScrollIntoView(lsbResult.SelectedItem);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {

            }
        }


        private void ShowLog(byte flag, string cmd, string logmsg = "", string error = "")
        {
            string className = "SetControllerWindow";
            string funcName = "ShowLog";
            string trace = "";
            DateTime dt = DateTime.Now;

            try
            {
                trace = dt.ToString("yyyy-MM-dd HH:mm:ss    ");
                if (cmd.Length > 0)
                    trace += "[" + cmd + "] ";
                switch (flag)
                {
                    case (byte)LOGTYPE.LOG_START:                 // START
                        trace += "START";
                        break;
                    case (byte)LOGTYPE.LOG_SUCCESS:                 // SUCCESS
                        trace += "SUCCESS";
                        break;
                    case (byte)LOGTYPE.LOG_FAILURE:                 // ERROR
                        trace += "ERROR";
                        if (error.Length > 0)
                            trace += " - " + error;
                        break;
                    case (byte)LOGTYPE.LOG_NORMAL:
                        if (logmsg.Length > 0)
                            trace += logmsg;
                        break;
                    case (byte)LOGTYPE.LOG_END:
                        trace += "END";
                        break;
                    //case 3:
                    //    break;
                    //case 4:
                    //    break;
                    default:
                        //trace += logmsg;
                        break;
                }

                if (lsbResult.CheckAccess())
                {
                    if (lsbResult.Items.Count > 1000)
                        lsbResult.Items.RemoveAt(0);

                    lsbResult.Items.Add(trace);
                    if (lsbResult.Items.Count > 0)
                    {
                        lsbResult.SelectedIndex = lsbResult.Items.Count;
                        lsbResult.ScrollIntoView(lsbResult.SelectedItem);
                    }
                }
                else
                {
                    lsbResult.Dispatcher.Invoke(new Action(delegate
                    {
                        if (lsbResult.Items.Count > 1000)
                            lsbResult.Items.RemoveAt(0);

                        lsbResult.Items.Add(trace);
                        if (lsbResult.Items.Count > 0)
                        {
                            lsbResult.SelectedIndex = lsbResult.Items.Count;
                            lsbResult.ScrollIntoView(lsbResult.SelectedItem);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
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


        private void ShowTextBoxData(TextBox tbx, string data)
        {
            try
            {
                if (tbx == null)
                    return;
                if (tbx.CheckAccess())
                {
                    tbx.Text = data;
                }
                else
                {
                    tbx.Dispatcher.Invoke(new Action(delegate
                    {
                        tbx.Text = data;
                    }));
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void OnMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        {
            string className = "SetControllerWindow";
            string funcName = "OnMarkControllerEventFunc";

            string param1 = "";
            string param2 = "";
            string showstringX = "";
            string showstringY = "";
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            double dchi = 0.0d;
            double dpti = 0.0d;
            byte currCMD = 0;

            try
            {
                //if (((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow != 2)
                //    return;
                i = 6;
                if(e.receiveSize >= 10)
                {
                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    chindex = Convert.ToInt32(param1, 16);
                }
                if (e.receiveSize >= 14)
                {
                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    ptindex = Convert.ToInt32(param2, 16);
                }
                currCMD = e.execmd;

                switch (e.stscmd)
                {
                    case 0x30:      //stand by
                    case 0x31:      //running
                    case 0x32:      //run ok
                        switch ((char)m_currCMD)
                        {
                            case 'A':
                            case 'D':
                            case 'N':
                            case 'R':
                            case 'S':
                            case 'F':
                            case 'L':
                            case 'O':
                            case 'C':
                            case 'G':
                            case 'X':
                            case 'I':
                            case 'V':
                            case 'l':
                            case 'g':
                            case 'r':
                                break;

                            case 'J':
                            case 'H':
                            case 'M':
                            case 'K':   // XY Axis
                                dchi = (double)chindex / (double)stepLength;
                                dpti = (double)ptindex / (double)stepLength;
                                showstringX = dchi.ToString("F2");
                                showstringY = dpti.ToString("F2");
                                ShowLabelData(showstringX, lblCurrentPosX);
                                ShowLabelData(showstringY, lblCurrentPosY);
                                //ShowTextBoxData(txtCurrentPosX, showstringX);
                                //ShowTextBoxData(txtCurrentPosY, showstringY);
                                break;
                            case 'j':
                            case 'h':
                            case 'U':
                            case 'k':   // U Axis
                                dchi = (double)chindex / (double)stepLength_u;
                                showstringX = dchi.ToString("F2");
                            ShowTextBoxData(txtCurrentPosU, showstringX);
                                break;

                            default:
                                break;
                        }
                        break;

                    //case 0x33:      //home ok
                    //    break;
                    //case 0x34:      //jog ok
                    //    break;
                    //case 0x35:      //test ok
                    //    break;
                    //case 0x36:      //go ok
                    //    break;
                    //case 0x37:      //scan ok
                    //    break;

                    case 0x38:      // Action OK
                        if ((currCMD == 'R') && (m_currCMD == 'R'))
                        {
                            //ITNTJobLog.Instance.Trace(0, "[4] : RECEIVE MARKING COMPLETE");

//#if MANUAL_MARK
//                                //ShowCurrentStateLabel(5);
//                                ShowCurrentStateLabelManual(4);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
//#else
//#endif
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[4] : MARKING COMPLETE", Thread.CurrentThread.ManagedThreadId);
                            //ITNTJobLog.Instance.Trace(0, "[4] : MARKING COMPLETE");
                            ShowLog("MARKING COMPLETE!!!!");
                            m_currCMD = 0;
                        }
                        //doingCommand = false;
                        //switch ((char)currentCommand)
                        //{
                        //    case 'J':
                        //    case 'H':
                        //    case 'M':
                        //    case 'K':   // XY Axis
                        //        dchi = (double)chindex / (double)stepLength;
                        //        dpti = (double)ptindex / (double)stepLength;
                        //        showstringX = dchi.ToString("F2");
                        //        showstringY = dpti.ToString("F2");
                        //        ShowTextBoxData(txtCurrentPosX, showstringX);
                        //        ShowTextBoxData(txtCurrentPosY, showstringY);
                        //        break;
                        //    case 'j':
                        //    case 'h':
                        //    case 'U':
                        //    case 'k':   // U Axis
                        //        dchi = (double)chindex / (double)stepLength_u;
                        //        showstringX = dchi.ToString("F2");
                        //        ShowTextBoxData(txtCurrentPosU, showstringX);
                        //        break;

                        //    default:
                        //        break;
                        //}
                        //currentCommand = 0;
                        break;

                    case 0x39:      //emergency
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<List<string>> DirFileSearch(string selpath, string file)
        {
            List<string> names = new List<string>();
            try
            {
                string fileFullName = "";
                string value = "";
                VinNoInfo vin = new VinNoInfo();

                string[] dirs = Directory.GetDirectories(selpath);
                string[] files = Directory.GetFiles(selpath, $"*{file}");
                foreach (string fileName in files)
                {
                    fileFullName = fileName.Replace(".ini", "");
                    fileFullName = fileFullName.Replace(selpath, "");
                    fileFullName = fileFullName.Replace("\\", "");
                    //fileFullName = fileFullName.Replace("Pattern_", "");
                    names.Add(fileFullName);
                }
                if (dirs.Length > 0)
                {
                    foreach (string dir in dirs)
                    {
                        await DirFileSearch(dir, file);
                    }
                }
                return names;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return names;
            }
        }

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
                MessageBox.Show(ex.ToString());
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                ChangeSize(this.ActualWidth, this.ActualHeight);
            }
            this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            orginalWidth = this.Width;
            originalHeight = this.Height;
            ChangeSize(e.NewSize.Width, e.NewSize.Height);
        }

        public void DisplayPatternValue(byte byheadType, PatternValueEx pattern)
        {
            string className = "SetControllerWindow";
            string funcName = "DisplayPatternValue";

            try
            {
                if (cbxPatternList.Items.Contains(pattern.name) == true)
                    cbxPatternList.SelectedItem = pattern.name;
                else
                    cbxPatternList.SelectedIndex = 0;

                DisplayPatternFontValue(byheadType, pattern.fontValue);
                DisplayPatternHeadValue(byheadType, pattern.headValue);
                DisplayPatternLaserValue(byheadType, pattern.laserValue);
                DisplayPatternPositionValue(byheadType, pattern.positionValue);
                DisplayPatternScanValue(byheadType, pattern.scanValue);
                DisplayPatternSpeedValue(byheadType, pattern.laserValue.density, pattern.speedValue);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void DisplayPatternFontValue(byte byheadType, FontValue fontValue)
        {
            //string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "SetControllerWindow";
            string funcName = "DisplayPatternFontValue";

            try
            {
                if (cbxFontName.Items.Contains(fontValue.fontName) == true)
                    cbxFontName.SelectedItem = fontValue.fontName;
                else
                    cbxFontName.SelectedIndex = 0;

                txtHeight.Text = fontValue.height.ToString("F2");
                txtWidth.Text = fontValue.width.ToString("F2");
                txtPitch.Text = fontValue.pitch.ToString("F2");
                txtAngle.Text = fontValue.rotateAngle.ToString("F2");
                txtThickness.Text = fontValue.thickness.ToString("F2");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void DisplayPatternHeadValue(byte byheadType, HeadValue headValue)
        {
            string className = "SetControllerWindow";
            string funcName = "DisplayPatternHeadValue";
            //public short opmode;
            //public short stepLength;
            //public double angleDegree;
            //public byte sensorPosition;
            //public byte spatterType;

            try
            {
                lblMAX_X.Content = headValue.max_X.ToString();
                lblMAX_Y.Content = headValue.max_Y.ToString();
                //lblMAX_Z.Content = headValue.max_Z.ToString();

                lblStepLength.Content = headValue.stepLength.ToString();
                //txtSensorAngle.Text = headValue.angleDegree.ToString("F2");
                //txtOpmode.Text = headValue.opmode.ToString();

                //if (headValue.sensorPosition == 0)
                //{
                //    rdbtnSensorPosLeft.IsChecked = false;
                //    rdbtnSensorPosRight.IsChecked = true;
                //}
                //else
                //{
                //    rdbtnSensorPosLeft.IsChecked = true;
                //    rdbtnSensorPosRight.IsChecked = false;
                //}

                //if (headValue.spatterType == 0)
                //{
                //    rdbtnSpatterPosDown.IsChecked = false;
                //    rdbtnSpatterPosUp.IsChecked = true;
                //}
                //else
                //{
                //    rdbtnSpatterPosDown.IsChecked = true;
                //    rdbtnSpatterPosUp.IsChecked = false;
                //}

                txtPark_X.Text = headValue.park3DPos.X.ToString("F2");
                txtPark_Y.Text = headValue.park3DPos.Y.ToString("F2");
                //txtPark_Z.Text = headValue.park3DPos.Z.ToString("F2");

                txtHome_X.Text = headValue.home3DPos.X.ToString("F2");
                txtHome_Y.Text = headValue.home3DPos.Y.ToString("F2");
                //txtHome_Z.Text = headValue.home3DPos.Z.ToString("F2");

                //txtRasterStartPoint.Text = headValue.rasterSP.ToString("F2");
                //txtRasterEndPoint.Text = headValue.rasterEP.ToString("F2");

                //txtDistance0Pos.Text = headValue.distance0Position.ToString("F2");

                //txtMarkDelayTime1.Text = headValue.markDelayTime1.ToString();
                //txtMarkDelayTime2.Text = headValue.markDelayTime2.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void DisplayPatternPositionValue(byte byheadType, PositionValue positionValue)
        {
            string className = "SetControllerWindow";
            string funcName = "DisplayPatternPositionValue";

            try
            {
                //txtCenter_X.Text = positionValue.center3DPos.X.ToString("F2");
                //txtCenter_Y.Text = positionValue.center3DPos.Y.ToString("F2");
                //txtCenter_Z.Text = positionValue.center3DPos.Z.ToString("F2");

                //txtCheckHeight.Text = positionValue.checkDistanceHeight.ToString("F2");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void DisplayPatternLaserValue(byte byheadType, LaserValue laserValue)
        {
            string className = "SetControllerWindow";
            string funcName = "DisplayPatternLaserValue";

            try
            {
                //txtProfileNum.Text = laserValue.waveformNum.ToString();
                //txtCleanProfileNum.Text = laserValue.waveformClean.ToString();
                //txtCleanPosition.Text = laserValue.cleanPosition.ToString("F2");
                //txtCleanDelta.Text = laserValue.cleanDelta.ToString("F2");
                //txtDensity.Text = laserValue.density.ToString("F2");
                //if (laserValue.useCleaning != 0)
                //{
                //    chkboxuseCleaning.IsChecked = true;
                //    //rdbtnUseCleaningYes.IsChecked = true;
                //    //rdbtnUseCleaningNo.IsChecked = false;
                //}
                //else
                //{
                //    chkboxuseCleaning.IsChecked = false;
                //    //rdbtnUseCleaningNo.IsChecked = true;
                //    //rdbtnUseCleaningYes.IsChecked = false;
                //}
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// //
        /// </summary>
        /// <param name="byheadType"></param>
        /// <param name="density"></param>
        /// <param name="speedValue"></param>
        public void DisplayPatternSpeedValue(byte byheadType, double density, SpeedValue speedValue)
        {
            string className = "SetControllerWindow";
            string funcName = "DisplayPatternSpeedValue";

            try
            {
                if (density != 1)
                {
                    txbSpeedMarkInitialValue.Text = speedValue.initSpeed4MarkV.ToString();
                    txbSpeedMarkTargetValue.Text = speedValue.targetSpeed4MarkV.ToString();
                    txbSpeedMarkAccelValue.Text = speedValue.accelSpeed4MarkV.ToString();
                    txbSpeedMarkDecelValue.Text = speedValue.decelSpeed4MarkV.ToString();
                }
                else
                {
                    txbSpeedMarkInitialValue.Text = speedValue.initSpeed4MarkR.ToString();
                    txbSpeedMarkTargetValue.Text = speedValue.targetSpeed4MarkR.ToString();
                    txbSpeedMarkAccelValue.Text = speedValue.accelSpeed4MarkR.ToString();
                    txbSpeedMarkDecelValue.Text = speedValue.decelSpeed4MarkR.ToString();
                }

                //
                txbSpeedFastInitialValue.Text = speedValue.initSpeed4Fast.ToString();
                txbSpeedFastTargetValue.Text = speedValue.targetSpeed4Fast.ToString();
                txbSpeedFastAccelValue.Text = speedValue.accelSpeed4Fast.ToString();
                txbSpeedFastDecelValue.Text = speedValue.decelSpeed4Fast.ToString();

                txbSpeedHomeInitialValue.Text = speedValue.initSpeed4Home.ToString();
                txbSpeedHomeTargetValue.Text = speedValue.targetSpeed4Home.ToString();
                txbSpeedHomeAccelValue.Text = speedValue.accelSpeed4Home.ToString();
                txbSpeedHomeDecelValue.Text = speedValue.decelSpeed4Home.ToString();

                txbSpeedMeasureInitialValue.Text = speedValue.initSpeed4Measure.ToString();
                txbSpeedMeasureTargetValue.Text = speedValue.targetSpeed4Measure.ToString();
                txbSpeedMeasureAccelValue.Text = speedValue.accelSpeed4Measure.ToString();
                txbSpeedMeasureDecelValue.Text = speedValue.decelSpeed4Measure.ToString();

                txtSolOnTime.Text = speedValue.solOnTime.ToString();
                txtSolOffTime.Text = speedValue.solOffTime.ToString();
                txtDWellTime.Text = speedValue.dwellTime.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        public void DisplayPatternScanValue(byte byheadType, ScanValue scanValue)
        {
            string className = "SetControllerWindow";
            string funcName = "DisplayPatternScanValue";

            try
            {
                txtVisionStartPos.Text = scanValue.startU.ToString("F2");
                txtVisionScanLength.Text = scanValue.scanLen.ToString("F2");
                //txtVisionParkPos.Text = scanValue.parkingU.ToString("F2");
                //txtVisionHomePos.Text = scanValue.home_U.ToString("F2");
                //txtVisionLinkPos.Text = scanValue.linkPos.ToString("F2");
                //lblVisionMaxLength.Content = scanValue.max_U.ToString();
                //lblVisionStepLength.Content = scanValue.stepLength_U.ToString();

                txbSpeedScanInitialValue.Text = scanValue.initSpeed4Scan.ToString();
                txbSpeedScanTargetValue.Text = scanValue.targetSpeed4Scan.ToString();
                txbSpeedScanAccelValue.Text = scanValue.accelSpeed4Scan.ToString();
                txbSpeedScanDecelValue.Text = scanValue.decelSpeed4Scan.ToString();
                txbSpeedScanFastInitialValue.Text = scanValue.initSpeed4ScanFree.ToString();
                txbSpeedScanFastTargetValue.Text = scanValue.targetSpeed4ScanFree.ToString();
                txbSpeedScanFastAccelValue.Text = scanValue.accelSpeed4ScanFree.ToString();
                txbSpeedScanFastDecelValue.Text = scanValue.decelSpeed4ScanFree.ToString();

                //if (scanValue.reverseScan == 0)
                //{
                //    rdbtnVisionLeft.IsChecked = false;
                //    rdbtnVisionRight.IsChecked = true;
                //}
                //else
                //{
                //    rdbtnVisionLeft.IsChecked = true;
                //    rdbtnVisionRight.IsChecked = false;
                //}
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private void DisplayPatternValue(PatternValueEx pat)
        //{
        //    try
        //    {
        //        cbxPatternList.SelectedItem = pat.name;
        //        cbxFontName.SelectedItem = pat.fontValue.fontName;

        //        //
        //        txtStartX.Text = pat.positionValue.center3DPos.X.ToString("F2");
        //        txtStartY.Text = pat.positionValue.center3DPos.Y.ToString("F2");
        //        txtHeight.Text = pat.fontValue.height.ToString("F2");
        //        txtWidth.Text = pat.fontValue.width.ToString("F2");
        //        txtPitch.Text = pat.fontValue.pitch.ToString("F2");
        //        txtAngle.Text = pat.fontValue.rotateAngle.ToString("F2");
        //        txtStrike.Text = pat.fontValue.strikeCount.ToString();
        //        lblMAX_X.Content = pat.headValue.max_X.ToString();
        //        lblMAX_Y.Content = pat.headValue.max_Y.ToString();
        //        lblPark_X.Content = pat.headValue.park3DPos.X.ToString();
        //        lblPark_Y.Content = pat.headValue.park3DPos.Y.ToString();

        //        //
        //        //Load_Ispeed.Value = pat.speedValue.initSpeed4MarkV;//.initSpeed4Load;
        //        //Load_value_Ispeed.Text = pat.speedValue.initSpeed4MarkV.ToString();

        //        //Load_Tspeed.Value = pat.speedValue.targetSpeed4MarkV;
        //        //Load_value_Tspeed.Text = pat.speedValue.targetSpeed4MarkV.ToString();

        //        //Load_Accel.Value = pat.speedValue.accelSpeed4MarkV;
        //        //Load_value_Accel.Text = pat.speedValue.accelSpeed4MarkV.ToString();

        //        //Load_Decel.Value = pat.speedValue.decelSpeed4MarkV;
        //        //Load_value_Decel.Text = pat.speedValue.decelSpeed4MarkV.ToString();

        //        ////
        //        //Nload_Ispeed.Value = pat.speedValue.initSpeed4Home;
        //        //Nload_value_Ispeed.Text = pat.speedValue.initSpeed4Home.ToString();

        //        //Nload_Tspeed.Value = pat.speedValue.targetSpeed4Home;
        //        //Nload_value_Tspeed.Text = pat.speedValue.targetSpeed4Home.ToString();

        //        //Nload_Accel.Value = pat.speedValue.accelSpeed4Home;
        //        //Nload_value_Accel.Text = pat.speedValue.accelSpeed4Home.ToString();

        //        //Nload_Decel.Value = pat.speedValue.decelSpeed4Home;
        //        //Nload_value_Decel.Text = pat.speedValue.decelSpeed4Home.ToString();

        //        ////
        //        //Scan_Ispeed.Value = pat.scanValue.initSpeed4Scan;
        //        //Scan_value_Ispeed.Text = pat.scanValue.initSpeed4Scan.ToString();

        //        //Scan_Tspeed.Value = pat.scanValue.targetSpeed4Scan;
        //        //Scan_value_Tspeed.Text = pat.scanValue.targetSpeed4Scan.ToString();

        //        //Scan_Accel.Value = pat.scanValue.accelSpeed4Scan;
        //        //Scan_value_Accel.Text = pat.scanValue.accelSpeed4Scan.ToString();

        //        //Scan_Decel.Value = pat.scanValue.decelSpeed4Scan;
        //        //Scan_value_Decel.Text = pat.scanValue.decelSpeed4Scan.ToString();

        //        ////
        //        //ScanFree_Ispeed.Value = pat.scanValue.initSpeed4ScanFree;
        //        //ScanFree_value_Ispeed.Text = pat.scanValue.initSpeed4ScanFree.ToString();

        //        //ScanFree_Tspeed.Value = pat.scanValue.targetSpeed4ScanFree;
        //        //ScanFree_value_Tspeed.Text = pat.scanValue.targetSpeed4ScanFree.ToString();

        //        //ScanFree_Accel.Value = pat.scanValue.accelSpeed4ScanFree;
        //        //ScanFree_value_Accel.Text = pat.scanValue.accelSpeed4ScanFree.ToString();

        //        //ScanFree_Decel.Value = pat.scanValue.decelSpeed4ScanFree;
        //        //ScanFree_value_Decel.Text = pat.scanValue.decelSpeed4ScanFree.ToString();

        //        ////
        //        //txtSolOnTime.Text = pat.speedValue.solOnTime.ToString();
        //        //txtSolOffTime.Text = pat.speedValue.solOffTime.ToString();
        //        //txtdwellTime.Text = pat.speedValue.dwellTime.ToString();


        //        txbSpeedMarkInitialValue.Text = pat.speedValue.initSpeed4MarkV.ToString();
        //        txbSpeedMarkTargetValue.Text = pat.speedValue.targetSpeed4MarkV.ToString();
        //        txbSpeedMarkAccelValue.Text = pat.speedValue.accelSpeed4MarkV.ToString();
        //        txbSpeedMarkDecelValue.Text = pat.speedValue.decelSpeed4MarkV.ToString();

        //        //
        //        txbSpeedHomeInitialValue.Text = pat.speedValue.initSpeed4Home.ToString();
        //        txbSpeedHomeTargetValue.Text = pat.speedValue.targetSpeed4Home.ToString();
        //        txbSpeedHomeAccelValue.Text = pat.speedValue.accelSpeed4Home.ToString();
        //        txbSpeedHomeDecelValue.Text = pat.speedValue.decelSpeed4Home.ToString();

        //        txbSpeedFastInitialValue.Text = pat.speedValue.initSpeed4Home.ToString();
        //        txbSpeedFastTargetValue.Text = pat.speedValue.targetSpeed4Home.ToString();
        //        txbSpeedFastAccelValue.Text = pat.speedValue.accelSpeed4Home.ToString();
        //        txbSpeedFastDecelValue.Text = pat.speedValue.decelSpeed4Home.ToString();

        //        //
        //        txbSpeedScanInitialValue.Text = pat.scanValue.initSpeed4Scan.ToString();
        //        txbSpeedScanTargetValue.Text = pat.scanValue.targetSpeed4Scan.ToString();
        //        txbSpeedScanAccelValue.Text = pat.scanValue.accelSpeed4Scan.ToString();
        //        txbSpeedScanDecelValue.Text = pat.scanValue.decelSpeed4Scan.ToString();

        //        txbSpeedScanFastInitialValue.Text = pat.scanValue.initSpeed4ScanFree.ToString();
        //        txbSpeedScanFastTargetValue.Text = pat.scanValue.targetSpeed4ScanFree.ToString();
        //        txbSpeedScanFastAccelValue.Text = pat.scanValue.accelSpeed4ScanFree.ToString();
        //        txbSpeedScanFastDecelValue.Text = pat.scanValue.decelSpeed4ScanFree.ToString();

        //        //
        //        txtSolOnTime.Text = pat.speedValue.solOnTime.ToString();
        //        txtSolOffTime.Text = pat.speedValue.solOffTime.ToString();
        //        txtDWellTime.Text = pat.speedValue.dwellTime.ToString();
        //        //
        //        txtVisionStartPos.Text = pat.scanValue.startU.ToString("F2");
        //        txtVisionScanLength.Text = pat.scanValue.scanLen.ToString("F2");
        //        lblVisionParkPos.Content = pat.scanValue.parkingU.ToString("F2");
        //        lblMAX_U.Content = pat.scanValue.max_U.ToString();
        //        lblVisionHomePos.Content = pat.scanValue.home_U.ToString("F2");
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        //private void ReadPatternData(ref PatternValueEx data)
        //{
        //    double tmp = 0;
        //    try
        //    {
        //        data = (PatternValueEx)orgData.Clone();

        //        if(cbxPatternList.SelectedIndex >= 0)
        //            data.name = cbxPatternList.SelectedItem.ToString();
        //        if(CB_font.SelectedIndex >= 0)
        //            data.fontValue.fontName = CB_font.SelectedItem.ToString();

        //        double.TryParse(txtStartX.Text, out tmp);
        //        data.positionValue.center3DPos.X = tmp;
        //        double.TryParse(txtStartY.Text, out tmp);
        //        data.positionValue.center3DPos.Y = tmp;
        //        double.TryParse(txtHeight.Text, out data.fontValue.height);
        //        double.TryParse(txtWidth.Text, out data.fontValue.width);
        //        double.TryParse(txtPitch.Text, out data.fontValue.pitch);
        //        double.TryParse(txtAngle.Text, out data.fontValue.rotateAngle);
        //        short.TryParse(txtStrike.Text, out data.fontValue.strikeCount);


        //        short.TryParse(txbSpeedHomeInitialValue.Text, out data.speedValue.initSpeed4Home);
        //        short.TryParse(txbSpeedHomeTargetValue.Text, out data.speedValue.targetSpeed4Home);
        //        short.TryParse(txbSpeedHomeAccelValue.Text, out data.speedValue.accelSpeed4Home);
        //        short.TryParse(txbSpeedHomeDecelValue.Text, out data.speedValue.decelSpeed4Home);

        //        short.TryParse(txbSpeedFastInitialValue.Text, out data.speedValue.initSpeed4Fast);
        //        short.TryParse(txbSpeedFastTargetValue.Text, out data.speedValue.targetSpeed4Fast);
        //        short.TryParse(txbSpeedFastAccelValue.Text, out data.speedValue.accelSpeed4Fast);
        //        short.TryParse(txbSpeedFastDecelValue.Text, out data.speedValue.decelSpeed4Fast);

        //        short.TryParse(txbSpeedMarkInitialValue.Text, out data.speedValue.initSpeed4MarkV);
        //        short.TryParse(txbSpeedMarkTargetValue.Text, out data.speedValue.targetSpeed4MarkV);
        //        short.TryParse(txbSpeedMarkAccelValue.Text, out data.speedValue.accelSpeed4MarkV);
        //        short.TryParse(txbSpeedMarkDecelValue.Text, out data.speedValue.decelSpeed4MarkV);

        //        short.TryParse(txbSpeedScanInitialValue.Text, out data.scanValue.initSpeed4Scan);
        //        short.TryParse(txbSpeedScanTargetValue.Text, out data.scanValue.targetSpeed4Scan);
        //        short.TryParse(txbSpeedScanAccelValue.Text, out data.scanValue.accelSpeed4Scan);
        //        short.TryParse(txbSpeedScanDecelValue.Text, out data.scanValue.decelSpeed4Scan);

        //        short.TryParse(txbSpeedScanFastInitialValue.Text, out data.scanValue.initSpeed4ScanFree);
        //        short.TryParse(txbSpeedScanFastTargetValue.Text, out data.scanValue.targetSpeed4ScanFree);
        //        short.TryParse(txbSpeedScanFastAccelValue.Text, out data.scanValue.accelSpeed4ScanFree);
        //        short.TryParse(txbSpeedScanFastDecelValue.Text, out data.scanValue.decelSpeed4ScanFree);

        //        short.TryParse(txtSolOnTime.Text, out data.speedValue.solOnTime);
        //        short.TryParse(txtSolOffTime.Text, out data.speedValue.solOffTime);
        //        short.TryParse(txtDWellTime.Text, out data.speedValue.dwellTime);

        //        double.TryParse(txtStartU.Text, out data.scanValue.startU);

        //        ////short.TryParse(txtMAX_X.Text, out data.max_x);
        //        ////short.TryParse(txtMAX_Y.Text, out data.max_y);
        //        ////short.TryParse(txtParkX.Text, out data.park_x);
        //        ////short.TryParse(txtParkY.Text, out data.park_y);

        //        ////short.TryParse(Nload_value_Ispeed.Text, out data.speedValue.initSpeed4NoLoad);
        //        ////short.TryParse(Nload_value_Tspeed.Text, out data.speedValue.targetSpeed4NoLoad);
        //        ////short.TryParse(Nload_value_Accel.Text, out data.speedValue.accelSpeed4NoLoad);
        //        ////short.TryParse(Nload_value_Decel.Text, out data.speedValue.decelSpeed4NoLoad);

        //        ////short.TryParse(Load_value_Ispeed.Text, out data.speedValue.initSpeed4Load);
        //        ////short.TryParse(Load_value_Tspeed.Text, out data.speedValue.targetSpeed4Load);
        //        ////short.TryParse(Load_value_Accel.Text, out data.speedValue.accelSpeed4Load);
        //        ////short.TryParse(Load_value_Decel.Text, out data.speedValue.decelSpeed4Load);

        //        //short.TryParse(Nload_value_Ispeed.Text, out data.speedValue.initSpeed4Home);
        //        //short.TryParse(Nload_value_Tspeed.Text, out data.speedValue.targetSpeed4Home);
        //        //short.TryParse(Nload_value_Accel.Text, out data.speedValue.accelSpeed4Home);
        //        //short.TryParse(Nload_value_Decel.Text, out data.speedValue.decelSpeed4Home);

        //        //short.TryParse(Load_value_Ispeed.Text, out data.speedValue.initSpeed4MarkV);
        //        //short.TryParse(Load_value_Tspeed.Text, out data.speedValue.targetSpeed4MarkV);
        //        //short.TryParse(Load_value_Accel.Text, out data.speedValue.accelSpeed4MarkV);
        //        //short.TryParse(Load_value_Decel.Text, out data.speedValue.decelSpeed4MarkV);

        //        //short.TryParse(Scan_value_Ispeed.Text, out data.scanValue.initSpeed4Scan);
        //        //short.TryParse(Scan_value_Tspeed.Text, out data.scanValue.targetSpeed4Scan);
        //        //short.TryParse(Scan_value_Accel.Text, out data.scanValue.accelSpeed4Scan);
        //        //short.TryParse(Scan_value_Decel.Text, out data.scanValue.decelSpeed4Scan);

        //        //short.TryParse(ScanFree_value_Ispeed.Text, out data.scanValue.initSpeed4ScanFree);
        //        //short.TryParse(ScanFree_value_Tspeed.Text, out data.scanValue.targetSpeed4ScanFree);
        //        //short.TryParse(ScanFree_value_Accel.Text, out data.scanValue.accelSpeed4ScanFree);
        //        //short.TryParse(ScanFree_value_Decel.Text, out data.scanValue.decelSpeed4ScanFree);

        //        //short.TryParse(txtSolOnTime.Text, out data.speedValue.solOnTime);
        //        //short.TryParse(txtSolOffTime.Text, out data.speedValue.solOffTime);
        //        //short.TryParse(txtdwellTime.Text, out data.speedValue.dwellTime);

        //        //double.TryParse(txtStartU.Text, out data.scanValue.startU);

        //        ////short.TryParse(txtStartU.Text, out data.startU);
        //        ////short.TryParse(txtScanLeng_U.Text, out data.scanLen);
        //        ////short.TryParse(txtParkU.Text, out data.parkingU);
        //        ////short.TryParse(txtMAX_U.Text, out data.max_u);
        //        ////short.TryParse(txt, out data.home_u);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        public int ReadPatternValue(ref PatternValueEx pattern)
        {
            string className = "SetControllerWindow";
            string funcName = "ReadPatternValue";
            int retval = 0;
            string fName = "";
            double density = 0;
            string cmd = "SAVE PATTERN FILE";

            try
            {
                if (cbxPatternList.SelectedIndex >= 0)
                    fName = cbxPatternList.SelectedItem.ToString();
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "Please select pattern name");
                    return -1;
                }
                    //fName = "Pattern_DEFAULT";

                //double.TryParse(txtDensity.Text, out density);

                pattern.name = fName;
                ReadPatternFontValue(ref pattern.fontValue);
                //ReadPatternLaserValue(ref pattern.laserValue);
                ReadPatternSpeedValue(ref pattern.speedValue);
                ReadPatternHeadValue(ref pattern.headValue);
                ReadPatternPositionValue(ref pattern.positionValue);
                ReadPatternScanValue(ref pattern.scanValue);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            return retval;
        }


        /// <summary>
        /// //
        /// </summary>
        /// <param name="fontValue"></param>
        public int ReadPatternFontValue(ref FontValue fontValue)
        {
            //string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "SetControllerWindow";
            string funcName = "ReadPatternFontValue";

            try
            {
                //fontValue = (FontValue)orgPattern.fontValue.Clone();

                if (cbxFontName.SelectedIndex >= 0)
                    fontValue.fontName = cbxFontName.SelectedItem.ToString();
                else
                {

                    return -1;
                }

                double.TryParse(txtHeight.Text, out fontValue.height);
                double.TryParse(txtWidth.Text, out fontValue.width);
                double.TryParse(txtPitch.Text, out fontValue.pitch);
                double.TryParse(txtAngle.Text, out fontValue.rotateAngle);
                //double.TryParse(txtThickness.Text, out fontValue.thickness);
                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        /// <summary>
        /// //
        /// </summary>
        /// <param name="headValue"></param>
        public void ReadPatternHeadValue(ref HeadValue headValue)
        {
            string className = "SetControllerWindow";
            string funcName = "ReadPatternHeadValue";
            short svalue = 0;
            double dbvalue = 0.0;

            try
            {
                headValue = (HeadValue)orgPattern.headValue.Clone();
                short.TryParse(lblMAX_X.Content.ToString(), out svalue); headValue.max_X = svalue;
                short.TryParse(lblMAX_Y.Content.ToString(), out svalue); headValue.max_Y = svalue;
                //short.TryParse(lblMAX_Z.Content.ToString(), out svalue); headValue.max_Z = svalue;

                short.TryParse(lblStepLength.Content.ToString(), out headValue.stepLength);

                //double.TryParse(txtSensorAngle.Text, out headValue.angleDegree);
                //short.TryParse(txtOpmode.Text, out headValue.opmode);

                //if (rdbtnSensorPosLeft.IsChecked == true)
                //    headValue.sensorPosition = 1;
                //else
                //    headValue.sensorPosition = 0;

                //if (rdbtnSpatterPosDown.IsChecked == true)
                //    headValue.spatterType = 1;
                //else
                //    headValue.spatterType = 0;

                double.TryParse(txtPark_X.Text, out dbvalue); headValue.park3DPos.X = dbvalue;
                double.TryParse(txtPark_Y.Text, out dbvalue); headValue.park3DPos.Y = dbvalue;
                //double.TryParse(txtPark_Z.Text, out dbvalue); headValue.park3DPos.Z = dbvalue;

                //double.TryParse(txtHome_X.Text, out dbvalue); headValue.home3DPos.X = dbvalue;
                //double.TryParse(txtHome_Y.Text, out dbvalue); headValue.home3DPos.Y = dbvalue;
                //double.TryParse(txtHome_Z.Text, out dbvalue); headValue.home3DPos.Z = dbvalue;

                //double.TryParse(txtRasterStartPoint.Text, out headValue.rasterSP);
                //double.TryParse(txtRasterEndPoint.Text, out headValue.rasterEP);

                //double.TryParse(txtDistance0Pos.Text, out headValue.distance0Position);

                //short.TryParse(txtMarkDelayTime1.Text, out headValue.markDelayTime1);
                //short.TryParse(txtMarkDelayTime2.Text, out headValue.markDelayTime2);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// //
        /// </summary>
        /// <param name="positionValue"></param>
        public void ReadPatternPositionValue(ref PositionValue positionValue)
        {
            string className = "SetControllerWindow";
            string funcName = "ReadPatternPositionValue";
            double dbvalue = 0;

            try
            {
                positionValue = (PositionValue)orgPattern.positionValue.Clone();

                double.TryParse(txtStartX.Text, out dbvalue); positionValue.center3DPos.X = dbvalue;
                double.TryParse(txtStartY.Text, out dbvalue); positionValue.center3DPos.Y = dbvalue;
                //double.TryParse(txtCenter_Z.Text, out dbvalue); positionValue.center3DPos.Z = dbvalue;

                //double.TryParse(txtCheckHeight.Text, out dbvalue); positionValue.checkDistanceHeight = dbvalue;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// //
        /// </summary>
        /// <param name="laserValue"></param>
        //public void ReadPatternLaserValue(ref LaserValue laserValue)
        //{
        //    string className = "SetControllerWindow";
        //    string funcName = "ReadPatternLaserValue";

        //    try
        //    {
        //        laserValue = (LaserValue)orgPattern.laserValue.Clone();
        //        short.TryParse(txtProfileNum.Text, out laserValue.waveformNum);
        //        short.TryParse(txtCleanProfileNum.Text, out laserValue.waveformClean);
        //        double.TryParse(txtCleanPosition.Text, out laserValue.cleanPosition);
        //        double.TryParse(txtCleanDelta.Text, out laserValue.cleanDelta);
        //        //if (rdbtnUseCleaningYes.IsChecked == true)
        //        if (chkboxuseCleaning.IsChecked == true)
        //            laserValue.useCleaning = 1;
        //        else
        //            laserValue.useCleaning = 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        /// <summary>
        /// //
        /// </summary>
        /// <param name="density"></param>
        /// <param name="speedValue"></param>
        public void ReadPatternSpeedValue(ref SpeedValue speedValue)
        {
            string className = "SetControllerWindow";
            string funcName = "ReadPatternSpeedValue";

            try
            {
                speedValue = (SpeedValue)orgPattern.speedValue.Clone();

                short.TryParse(txbSpeedMarkInitialValue.Text, out speedValue.initSpeed4MarkV);//.ToString();
                short.TryParse(txbSpeedMarkTargetValue.Text, out speedValue.targetSpeed4MarkV);//.ToString();
                short.TryParse(txbSpeedMarkAccelValue.Text, out speedValue.accelSpeed4MarkV);//.ToString();
                short.TryParse(txbSpeedMarkDecelValue.Text, out speedValue.decelSpeed4MarkV);//.ToString();

                //
                short.TryParse(txbSpeedFastInitialValue.Text, out speedValue.initSpeed4Fast);
                short.TryParse(txbSpeedFastTargetValue.Text, out speedValue.targetSpeed4Fast);//.ToString();
                short.TryParse(txbSpeedFastAccelValue.Text, out speedValue.accelSpeed4Fast);//.ToString();
                short.TryParse(txbSpeedFastDecelValue.Text, out speedValue.decelSpeed4Fast);//.ToString();

                short.TryParse(txbSpeedHomeInitialValue.Text, out speedValue.initSpeed4Home);//.ToString();
                short.TryParse(txbSpeedHomeTargetValue.Text, out speedValue.targetSpeed4Home);//.ToString();
                short.TryParse(txbSpeedHomeAccelValue.Text, out speedValue.accelSpeed4Home);//.ToString();
                short.TryParse(txbSpeedHomeDecelValue.Text, out speedValue.decelSpeed4Home);//.ToString();

                short.TryParse(txbSpeedMeasureInitialValue.Text, out speedValue.initSpeed4Measure);//.ToString();
                short.TryParse(txbSpeedMeasureTargetValue.Text, out speedValue.targetSpeed4Measure);//.ToString();
                short.TryParse(txbSpeedMeasureAccelValue.Text, out speedValue.accelSpeed4Measure);//.ToString();
                short.TryParse(txbSpeedMeasureDecelValue.Text, out speedValue.decelSpeed4Measure);//ToString();



                short.TryParse(txtSolOnTime.Text, out speedValue.solOnTime);//.ToString();
                short.TryParse(txtSolOffTime.Text, out speedValue.solOffTime);//.ToString();
                short.TryParse(txtDWellTime.Text, out speedValue.dwellTime);//.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// //
        /// </summary>
        /// <param name="scanvalue"></param>
        public void ReadPatternScanValue(ref ScanValue scanvalue)
        {
            string className = "SetControllerWindow";
            string funcName = "ReadPatternScanValue";

            try
            {
                double.TryParse(txtVisionStartPos.Text, out scanvalue.startU);
                double.TryParse(txtVisionScanLength.Text, out scanvalue.scanLen);
                double.TryParse(lblVisionParkPos.Content.ToString(), out scanvalue.parkingU);
                double.TryParse(lblVisionHomePos.Content.ToString(), out scanvalue.home_U);
                //double.TryParse(txtVisionLinkPos.Text, out scanvalue.linkPos);
                //short.TryParse(lblVisionMaxLength.Content.ToString(), out scanvalue.max_U);
                //short.TryParse(lblVisionStepLength.Content.ToString(), out scanvalue.stepLength_U);

                short.TryParse(txbSpeedScanInitialValue.Text, out scanvalue.initSpeed4Scan);
                short.TryParse(txbSpeedScanTargetValue.Text, out scanvalue.targetSpeed4Scan);
                short.TryParse(txbSpeedScanAccelValue.Text, out scanvalue.accelSpeed4Scan);
                short.TryParse(txbSpeedScanDecelValue.Text, out scanvalue.decelSpeed4Scan);
                short.TryParse(txbSpeedScanFastInitialValue.Text, out scanvalue.initSpeed4ScanFree);
                short.TryParse(txbSpeedScanFastTargetValue.Text, out scanvalue.targetSpeed4ScanFree);
                short.TryParse(txbSpeedScanFastAccelValue.Text, out scanvalue.accelSpeed4ScanFree);
                short.TryParse(txbSpeedScanFastDecelValue.Text, out scanvalue.decelSpeed4ScanFree);

                //if (rdbtnVisionLeft.IsChecked == true) scanvalue.reverseScan = 1;
                //if (rdbtnVisionRight.IsChecked == true) scanvalue.reverseScan = 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private void SavePatternData(string patternName, PatternValueEx data, byte saveFlag)
        {
            //string patternfile;
            string value = "";
            byte bHeadType = 0;

            try
            {
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                ImageProcessManager.SetPatternValue(patternName, bHeadType, data, 0);

                //patternfile = Constants.PATTERN_PATH + patternName + ".ini";
                //value = data.fontValue.fontName;
                //Util.WritePrivateProfileValue("FONT", "NAME", value, patternfile); // load FONT

                //value = data.positionValue.start_X.ToString();
                //Util.WritePrivateProfileValue("FONT", "STARTPOSX", value, patternfile); // load X

                //value = data.positionValue.start_Y.ToString();
                //Util.WritePrivateProfileValue("FONT", "STARTPOSY", value, patternfile); // load Y

                //value = data.fontValue.height.ToString();
                //Util.WritePrivateProfileValue("FONT", "HEIGHT", value, patternfile); // load height

                //value = data.fontValue.width.ToString();
                //Util.WritePrivateProfileValue("FONT", "WIDTH", value, patternfile); // load width

                //value = data.fontValue.pitch.ToString();
                //Util.WritePrivateProfileValue("FONT", "PITCH", value, patternfile); // load pitch

                //value = data.fontValue.rotateAngle.ToString();
                //Util.WritePrivateProfileValue("FONT", "ANGLE", value, patternfile); // load angle

                //value = data.fontValue.strikeCount.ToString();
                //Util.WritePrivateProfileValue("FONT", "STRIKE", value, patternfile); //load strike

                ////value = data.speedValue.initSpeed4Load.ToString();
                //value = data.speedValue.initSpeed4MarkV.ToString();
                //Util.WritePrivateProfileValue("LOAD", "INITIALSPEED", value, patternfile); // load initial speed

                ////value = data.speedValue.targetSpeed4Load.ToString();
                //value = data.speedValue.targetSpeed4MarkV.ToString();
                //Util.WritePrivateProfileValue("LOAD", "TARGETSPEED", value, patternfile); // load target speed

                ////value = data.speedValue.accelSpeed4Load.ToString();
                //value = data.speedValue.accelSpeed4MarkV.ToString();
                //Util.WritePrivateProfileValue("LOAD", "ACCELERATION", value, patternfile); // load accel

                ////value = data.speedValue.decelSpeed4Load.ToString();
                //value = data.speedValue.decelSpeed4MarkV.ToString();
                //Util.WritePrivateProfileValue("LOAD", "DECELERATION", value, patternfile); // load decel

                //value = data.speedValue.solOnTime.ToString();
                //Util.WritePrivateProfileValue("SOLENOID", "SOLONTIME", value, patternfile); // load sol on 

                //value = data.speedValue.solOffTime.ToString();
                //Util.WritePrivateProfileValue("SOLENOID", "SOLOFFTIME", value, patternfile); // load sol off

                //value = data.speedValue.dwellTime.ToString();
                //Util.WritePrivateProfileValue("SOLENOID", "DWELLTIME", value, patternfile); // load sol off

                //value = data.scanValue.initSpeed4Scan.ToString();
                //Util.WritePrivateProfileValue("SCAN", "INITIALSPEED", value, patternfile); // load initial speed

                //value = data.scanValue.targetSpeed4Scan.ToString();
                //Util.WritePrivateProfileValue("SCAN", "TARGETSPEED", value, patternfile); // load target speed

                //value = data.scanValue.accelSpeed4Scan.ToString();
                //Util.WritePrivateProfileValue("SCAN", "ACCELERATION", value, patternfile); // load accel

                //value = data.scanValue.decelSpeed4Scan.ToString();
                //Util.WritePrivateProfileValue("SCAN", "DECELERATION", value, patternfile); // load decel

                //value = data.scanValue.initSpeed4ScanFree.ToString();
                //Util.WritePrivateProfileValue("SCANFREE", "INITIALSPEED", value, patternfile); // load initial speed

                //value = data.scanValue.targetSpeed4ScanFree.ToString();
                //Util.WritePrivateProfileValue("SCANFREE", "TARGETSPEED", value, patternfile); // load target speed

                //value = data.scanValue.accelSpeed4ScanFree.ToString();
                //Util.WritePrivateProfileValue("SCANFREE", "ACCELERATION", value, patternfile); // load accel

                //value = data.scanValue.decelSpeed4ScanFree.ToString();
                //Util.WritePrivateProfileValue("SCANFREE", "DECELERATION", value, patternfile); // load decel

                //value = data.scanValue.startU.ToString();
                //Util.WritePrivateProfileValue("PROFILER", "STARTPOS", value, patternfile); // load accel

                //value = data.scanValue.scanLen.ToString();
                //Util.WritePrivateProfileValue("PROFILER", "SCANLEN", value, patternfile); // load accel


                //if ((saveFlag & 0x01) != 0)
                //{
                //    value = data.speedValue.initSpeed4Home.ToString();
                //    Util.WritePrivateProfileValue("NOLOAD", "INITIALSPEED", value, Constants.MARKING_INI_FILE); // load initial speed

                //    value = data.speedValue.targetSpeed4Home.ToString();
                //    Util.WritePrivateProfileValue("NOLOAD", "TARGETSPEED", value, Constants.MARKING_INI_FILE); // load target speed

                //    value = data.speedValue.accelSpeed4Home.ToString();
                //    Util.WritePrivateProfileValue("NOLOAD", "ACCELERATION", value, Constants.MARKING_INI_FILE); // load accel

                //    value = data.speedValue.decelSpeed4Home.ToString();
                //    Util.WritePrivateProfileValue("NOLOAD", "DECELERATION", value, Constants.MARKING_INI_FILE); // load decel

                //    value = data.headValue.max_X.ToString();
                //    Util.WritePrivateProfileValue("MARK", "MAX_X", value, Constants.MARKING_INI_FILE); // load accel

                //    value = data.headValue.max_Y.ToString();
                //    Util.WritePrivateProfileValue("MARK", "MAX_Y", value, Constants.MARKING_INI_FILE); // load accel

                //    value = data.positionValue.park_X.ToString();
                //    Util.WritePrivateProfileValue("PARKING", "X_POSITION", value, Constants.MARKING_INI_FILE); // load accel

                //    value = data.positionValue.park_Y.ToString();
                //    Util.WritePrivateProfileValue("PARKING", "Y_POSITION", value, Constants.MARKING_INI_FILE); // load accel
                //}

                //if ((saveFlag & 0x02) != 0)
                //{
                //    value = data.scanValue.parkingU.ToString();
                //    Util.WritePrivateProfileValue("CONFIG", "PARKING", value, Constants.SCANNER_INI_FILE); // load accel

                //    value = data.scanValue.max_U.ToString();
                //    Util.WritePrivateProfileValue("CONFIG", "MAX_U", value, Constants.SCANNER_INI_FILE); // load accel

                //    value = data.scanValue.home_U.ToString();
                //    Util.WritePrivateProfileValue("CONFIG", "HOME_U", value, Constants.SCANNER_INI_FILE); // load accel
                //}
            }
            catch (Exception ex)
            {

            }
        }

        private void LoadPatternData(string patternfile, ref PatternValueEx data)
        {
            string value = "";
            string patternName = "";
            byte bHeadType = 0;

            try
            {
                //data = new PatternValueEx();
                patternName = Constants.PATTERN_PATH + patternfile + ".ini";
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                ImageProcessManager.GetPatternValue(patternfile, bHeadType, ref data);


                //data.scanValue.stepLength_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);

                //Util.GetPrivateProfileValue("FONT", "NAME", "11X16", ref data.fontValue.fontName, patternName); // load FONT
                //data.startX = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSX", 20, patternName);
                //data.startY = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSY", 50, patternName);

                //data.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4, patternName);
                //data.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7, patternName);
                //data.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6, patternName);
                //data.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternName);
                //data.strikeCount = (short)Util.GetPrivateProfileValueUINT("FONT", "STRIKECOUNT", 0, patternName);

                //data.initSpeed4Load = (short)Util.GetPrivateProfileValueUINT("LOAD", "INITIALSPEED", 10, patternName);
                //data.targetSpeed4Load = (short)Util.GetPrivateProfileValueUINT("LOAD", "TARGETSPEED", 10, patternName);
                //data.accelSpeed4Load = (short)Util.GetPrivateProfileValueUINT("LOAD", "ACCELERATION", 15, patternName);
                //data.decelSpeed4Load = (short)Util.GetPrivateProfileValueUINT("LOAD", "DECELERATION", 15, patternName);
                //data.solOnTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "SOLONTIME", 10, patternName);
                //data.solOffTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "SOLOFFTIME", 10, patternName);
                //data.dwellTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "DWELLTIME", 10, patternName);

                //data.stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                //data.max_x = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_X", 0, Constants.MARKING_INI_FILE);
                //data.max_y = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 0, Constants.MARKING_INI_FILE);

                //data.park_x = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, Constants.MARKING_INI_FILE);
                //data.park_y = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, Constants.MARKING_INI_FILE);

                //data.initSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "INITIALSPEED", 50, Constants.MARKING_INI_FILE);
                //data.targetSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "TARGETSPEED", 50, Constants.MARKING_INI_FILE);
                //data.accelSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "ACCELERATION", 10, Constants.MARKING_INI_FILE);
                //data.decelSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "DECELERATION", 10, Constants.MARKING_INI_FILE);

                //data.max_u = (short)Util.GetPrivateProfileValueUINT("CONFIG", "MAX_U", 190, Constants.SCANNER_INI_FILE);
                //data.parkingU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, Constants.SCANNER_INI_FILE);
                //data.home_u = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, Constants.SCANNER_INI_FILE);

                //data.initSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "INITIALSPEED", 10, patternName);
                //data.targetSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "TARGETSPEED", 10, patternName);
                //data.accelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "ACCELERATION", 10, patternName);
                //data.decelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "DECELERATION", 10, patternName);

                //data.initSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "INITIALSPEED", 10, patternName);
                //data.targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "TARGETSPEED", 10, patternName);
                //data.accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "ACCELERATION", 10, patternName);
                //data.decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "DECELERATION", 10, patternName);

                //data.startU = (double)Util.GetPrivateProfileValueDouble("PROFILER", "STARTPOS", 20, patternName); // load Max U_Scan
                //data.scanLen = (double)Util.GetPrivateProfileValueDouble("PROFILER", "SCANLEN", 130, patternName);
            }
            catch (Exception ex)
            {

            }
        }

        private short Set_Res()
        {
            if (CB_JOG_SOLUTION.SelectedIndex >= 0)
                return (short)(CB_JOG_SOLUTION.SelectedIndex + 1);
            else
                return 1;
        }

        private short GetDistanceXY()
        {
            if (CB_JOG_SOLUTION.SelectedIndex >= 0)
                return (short)(CB_JOG_SOLUTION.SelectedIndex + 1);
            else
                return 1;
        }

        private async void Btn_SendData_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string patName = "";
            short ten = 10;
            short Max_x = 0;
            short Max_y = 0;
            string value = "";
            byte bHeadType = 0;
            PatternValueEx pat = new PatternValueEx();

            try
            {
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                ShowLog("SEND FONT DATA - FLUSH START");
                m_currCMD = (byte)'B';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.FontFlush();
                if(retval.execResult != 0)
                {
                    ShowLog("SEND FONT DATA - FLUSH ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("SEND FONT DATA - FLUSH SUCCESS");

                ShowLog("SEND FONT DATA - SEND START");
                patName = cbxPatternList.SelectedItem.ToString();
                //byte[] gearu = Encoding.UTF8.GetBytes(ten.ToString("X4") + ten.ToString("X4"));
                if(Txt_data_Manual.Text.Length <= 0)
                {
                    ShowLog("SEND FONT DATA - SEND ERROR (VIN EMPTY)");
                    return;

                }

                //GetPatternData(ref pat);

                //ImageProcessManager.GetPatternData(cbxPatternList.SelectedItem.ToString(), ref pat);
                string patname = cbxPatternList.SelectedItem.ToString();
                ImageProcessManager.GetPatternValue(patname, bHeadType, ref pat);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).SendFontData(Txt_data_Manual.Text.ToUpper(), patName);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).SendFontData(Txt_data_Manual.Text.ToUpper(), pat);
                if (retval.execResult != 0)
                {
                    ShowLog("SEND FONT DATA - SEND ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("SEND FONT DATA - SEND SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("SEND FONT DATA EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void Btn_Pin_test_Click(object sender, RoutedEventArgs e)
        {
            byte[] sbuff;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short zero = 0;
            short one = 1;
            m_currCMD = (byte)'O';

            try
            {
                switch (Btn_Pin_test.Content)
                {
                    case "PIN ON":
                        ShowLog("PIN ON START");
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(10, true);
                        if (retval.execResult != 0)
                        {
                            ShowLog("PIN ON ERROR (" + retval.execResult + ")");
                            return;
                        }
                        Btn_Pin_test.Content = "PIN OFF";
                        ShowLog("PIN ON SUCCESS");
                        break;

                    case "PIN OFF":
                        ShowLog("PIN OFF START");
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(10, false);
                        if (retval.execResult != 0)
                        {
                            ShowLog("PIN OFF ERROR (" + retval.execResult + ")");
                            return;
                        }
                        Btn_Pin_test.Content = "PIN ON";
                        ShowLog("PIN OFF SUCCESS");
                        break;

                        //case "PIN ON":
                        //    sbuff = Encoding.UTF8.GetBytes(zero.ToString("X4") + one.ToString("X4"));
                        //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.TestSolFet(sbuff, sbuff.Length);
                        //    if (retval.execResult == 0)
                        //        Btn_Pin_test.Content = "PIN OFF";
                        //    break;
                        //case "PIN OFF":
                        //    sbuff = Encoding.UTF8.GetBytes(zero.ToString("X4") + zero.ToString("X4"));
                        //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.TestSolFet(sbuff, sbuff.Length);
                        //    if(retval.execResult == 0)
                        //        Btn_Pin_test.Content = "PIN ON";
                        //    break;
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            //ConfirmWindowString msg1 = new ConfirmWindowString();
            //ConfirmWindowString msg2 = new ConfirmWindowString();
            //ConfirmWindowString msg3 = new ConfirmWindowString();
            //ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();
            //bool ret = false;

            //try
            //{
            //    msg2.Message = orgData.name + " : Do you want to save this Pattern?";
            //    msg2.Fontsize = 20;
            //    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
            //    msg2.VerticalContentAlignment = VerticalAlignment.Center;
            //    msg2.Foreground = Brushes.Red;
            //    msg2.Background = Brushes.White;

            //    if (CheckAccess())
            //    {
            //        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
            //        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //        ret = warning.ShowDialog().Value;
            //    }
            //    else
            //    {
            //        Dispatcher.Invoke(new Action(delegate
            //        {
            //            WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
            //            warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //            ret = warning.ShowDialog().Value;
            //        }));
            //    }

            //    if (ret == false)
            //        return;

            //    PatternValueEx pattern = new PatternValueEx();
            //    ReadPatternData(ref pattern);
            //    SavePatternData(orgData.name, pattern, 0);
            //}
            //catch (Exception ex)
            //{
            //    return;
            //}



            string className = "SetControllerWindow";
            string funcName = "btnSavePattern_Click";

            byte bHeadType = 0;
            string value = "";
            bool bret = false;
            PatternValueEx pattern = new PatternValueEx();
            string cmd = "SAVE PATTERN FILE";
            string patName = "";
            int retval = 0;

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                if (cbxPatternList.SelectedIndex < 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "패턴을 선택해주세요");
                    return;
                }

                patName = cbxPatternList.SelectedItem.ToString();

                bret = CheckSavePatternData(patName);
                if (bret == false)
                    return;

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                ReadPatternValue(ref pattern);
                retval = ImageProcessManager.SetPatternValue(pattern.name, bHeadType, pattern, 0);
                if (retval != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "");
                    return;
                }
                orgPattern = (PatternValueEx)pattern.Clone();
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //public void Savelog(string log_name)
        //{   // SAVE LOG 
        //    //string sBuf;
        //    //string CurrentDate;
        //    ////2021-05-27 오후 5:32:04
        //    //CurrentDate = DateTime.Now.ToString("yyyyMMdd");
        //    //sBuf = Mode_File.LOGGIN_PATH + CurrentDate.Substring(0, 4) + "\\" + CurrentDate.Substring(5, 2) + "\\" + CurrentDate.Substring(8, 2);
        //    //Directory.CreateDirectory(sBuf);
        //    //if (!string.IsNullOrEmpty(log_name))
        //    //{
        //    //    StreamWriter FILE_ = new StreamWriter(sBuf + "\\" + "login.txt", false);
        //    //    FILE_.WriteLine(DateAndTime.Now + ": " + log_name + Environment.NewLine);
        //    //    FILE_.Close();
        //    //}
        //}

        private async void Btn_AreaTest_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            string filename = AppDomain.CurrentDomain.BaseDirectory + "Parameter.ini";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string X_Position = "";
            string Y_Position = "";
            string Z_Position = "";
            string Height = "";
            string Width = "";
            string Pitch = "";
            string Angle = "";
            string vin = "";
            double SpX, SpY, SpZ, Ht, Wd, Pt, Ag;
            int NoVin;

            try
            {

                ShowLog("HEAD AREA TEST START");
                vin = Txt_data_Manual.Text;
                if(vin.Length <= 0)
                {
                    ShowLog("HEAD AREA TEST - PLEASE ENTER VIN");
                    return;
                }


                Util.GetPrivateProfileValue("VINDATA", "X", "0.0", ref X_Position, "Parameter.ini");            // load X position
                SpX = double.Parse(X_Position);

                Util.GetPrivateProfileValue("VINDATA", "Y", "0.0", ref Y_Position, "Parameter.ini");           // load Y position ;
                SpY = double.Parse(Y_Position);

                Util.GetPrivateProfileValue("VINDATA", "Z", "0.0", ref Z_Position, "Parameter.ini");           // load Z position ;
                SpZ = double.Parse(Z_Position);

                Util.GetPrivateProfileValue("VINDATA", "HEIGHT", "0.0", ref Height, "Parameter.ini");          // load Height;
                Ht = double.Parse(Height);

                Util.GetPrivateProfileValue("VINDATA", "WIDTH", "0.0", ref Width, "Parameter.ini");           // load  Width;
                Wd = double.Parse(Width);

                Util.GetPrivateProfileValue("VINDATA", "PITCH", "0.0", ref Pitch, "Parameter.ini");           // load Pitch;
                Pt = double.Parse(Pitch);

                Util.GetPrivateProfileValue("VINDATA", "ANGLE", "0.0", ref Angle, "Parameter.ini");           // load  Angle;
                Ag = double.Parse(Angle);

                //NoVin = ManStr.Length;

                //// ABS BLU
                //Mode_File.gMinX = (short)(SpX * Mode_File.Step_Length + 0.5);
                //Mode_File.gMaxX = (short)((SpX + (double)(NoVin - 1) * Pt + Wd) * Mode_File.Step_Length + 0.5);
                //Mode_File.gMinY = (short)(SpY * Mode_File.Step_Length + 0.5);
                //Mode_File.gMaxY = (short)((SpY + Ht) * Mode_File.Step_Length + 0.5);

                //var Parking_Z = (short)(Util.GetPrivateProfileValueUINT("PARKING", "PARKING_Z", 0, "Parameter.ini") * Mode_File.Step_Length);// LOAD PARKING Z

                //var Range_CenterXY = ((Mode_File.gMaxX + Mode_File.gMinX) / 2).ToString("X4") + ((Mode_File.gMaxY + Mode_File.gMinY) / 2).ToString("X4")
                //                     + Parking_Z.ToString("X4") + 0.ToString("X4");
                //GoPoint("M", Range_CenterXY);
                //Mode_File.HeightCT = 0.0;
                //for (int ii = 0; ii < 4; ii++)
                //{
                //    while (Mode_File.SensorShift == null) ;
                //    Mode_File.HeightCT += (double)Mode_File.SensorShift;
                //    Mode_File.SensorShift = null;
                //}
                //Mode_File.HeightCT /= 4;
                //// Sensor Shift compensation
                //Mode_File.gMinX -= (short)(Mode_File.HeightCT * Mode_File.Step_Length + 0.5);
                //Mode_File.gMaxX -= (short)(Mode_File.HeightCT * Mode_File.Step_Length + 0.5);
                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                //{
                //    ControlWindow.lblDispCenterXY.Content = ControlWindow.lblDisplacementVal2.Content;
                //    ControlWindow.lblDispMinXMinY.Content = "";
                //    ControlWindow.lblDispMinXMaxY.Content = "";
                //    ControlWindow.lblDispMaxXMinY.Content = "";
                //    ControlWindow.lblDispMaxXMaxY.Content = "";
                //}
                //));

                //short tCX = (short)(((double)Mode_File.gMaxX + (double)Mode_File.gMinX) / 2.0);
                //short tCY = (short)(((double)Mode_File.gMaxY + (double)Mode_File.gMinY) / 2.0);
                //var Range_MinXMinY = Mode_File.gMinX.ToString("X4") + Mode_File.gMinY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
                //var Range_MinXMaxY = Mode_File.gMinX.ToString("X4") + Mode_File.gMaxY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
                //var Range_MaxXMaxY = Mode_File.gMaxX.ToString("X4") + Mode_File.gMaxY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
                //var Range_MaxXMinY = Mode_File.gMaxX.ToString("X4") + Mode_File.gMinY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
                //var Range_CenXMinY = tCX.ToString("X4") + Mode_File.gMinY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
                //Range_CenterXY = tCX.ToString("X4") + tCY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");


                //retval = await((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.Head_Range_Test();
                if (retval.execResult != 0)
                {
                    ShowLog("HEAD AREA TEST ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("HEAD AREA TEST SUCCESS");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("HEAD AREA TEST EXCEPTION = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            //try
            //{
            //    ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.CHECK_Area(Txt_data_Manual.Text.ToUpper(), filename);
            //    if (Mode_File.AREA_Test == true)
            //    {
            //        ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.Range_Test();
            //        Mode_File.AREA_Test = false;
            //    }
            //    else
            //    {
            //        ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.Head_Range_Test();

            //    }

            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);

            //}
            //catch (Exception err)
            //{
            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Btn_AreaTest_Click = {0:X}, MSG = {1}", err.HResult, err.Message));
            //}
        }

        public void GoPosition(string DATA_)
        {

            //COMMAND_RESULT Retval = new COMMAND_RESULT();

            //int X = (int)(double.Parse(Txt_X.Text) * Mode_File.Step_Length);
            //int Y = (int)(double.Parse(Txt_Y.Text) * Mode_File.Step_Length);

            //try
            //{

            //    switch (Txt_Angle.Text)
            //    {
            //        case "0":


            //            var XY_O = (X.ToString("X4"), Y.ToString("X4"));
            //            Retval = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint("M", XY_O.ToString()).Result;
            //            break;

            //        case "180":
            //            X = ((int.Parse(Txt_X.Text)) + (int.Parse(Txt_Width.Text)) * (DATA_.Length - 1) + int.Parse(Txt_Height.Text) * Mode_File.Step_Length);
            //            var XY_180 = (X.ToString("X4"), Y.ToString("X4"));
            //            Retval = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint("M", XY_180.ToString()).Result;

            //            break;
            //        default:

            //            var Default = (X.ToString("X4"), Y.ToString("X4"));
            //            Retval = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint("M", Default.ToString()).Result;

            //            break;

            //    }
            //}
            //catch (Exception ERR)
            //{
            //    MessageBox.Show(ERR.ToString());
            //}

        }

        private  async void Btn_GO_Position_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short iposX = 0;
            short iposY = 0;
            short steplength = 0;
            double dposX = 0;
            double dposY = 0;
            short pos = 0;

            try
            {
                ShowLog("GO POSITION START");
                steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                //double.TryParse(txtCurrentPosX.Text, out dposX);
				double.TryParse(txtStartX.Text, out dposX);
                iposX = (short)(dposX * steplength);

                //double.TryParse(txtCurrentPosY.Text, out dposY);
                double.TryParse(txtStartY.Text, out dposY);
                iposY = (short)(dposY * steplength);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(iposX, iposY, pos);//  mark.GoPoint("M", XY_O).Result;
                if (retval.execResult != 0)
                {
                    ShowLog("GO POSITION ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("GO POSITION SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("GO POSITION EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private void Btn_Open_File_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                openFileDialog.Filter = "ini files (*.ini)|*.ini";

                //if (openFileDialog.ShowDialog() == true)
                //{
                //    string value = "";
                //    Util.GetPrivateProfileValue2("VINDATA", "TEXT", "", ref value, openFileDialog.FileName); //LOAD VIN NUMBER
                //    Txt_data_Manual.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "FONT", "", ref value, openFileDialog.FileName); // load FONT
                //    CB_font.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "X", "", ref value, openFileDialog.FileName); // load X
                //    Txt_X.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "Y", "", ref value, openFileDialog.FileName); // load Y
                //    Txt_Y.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "HEIGHT", "", ref value, openFileDialog.FileName); // load height
                //    Txt_Height.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "WIDTH", "", ref value, openFileDialog.FileName); // load width
                //    Txt_Width.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "PITCH", "", ref value, openFileDialog.FileName); // load pitch
                //    Txt_Pitch.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "ANGLE", "", ref value, openFileDialog.FileName); // load angle
                //    Txt_Angle.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("VINDATA", "STRIKE", "", ref value, openFileDialog.FileName); //load strike
                //    CB_strike.Text = value;
                //    value = "";

                //    Util.GetPrivateProfileValue2("NOLOAD", "INITIAL", "", ref value, openFileDialog.FileName); // load initial speed
                //    Nload_Ispeed.Value = double.Parse(value);
                //    Nload_value_Ispeed.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("NOLOAD", "TARGET", "", ref value, openFileDialog.FileName); // load target speed
                //    Nload_Tspeed.Value = double.Parse(value);
                //    Nload_value_Tspeed.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("NOLOAD", "ACCEL", "", ref value, openFileDialog.FileName); // load accel
                //    Nload_Accel.Value = double.Parse(value);
                //    Nload_value_Accel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("NOLOAD", "DECEL", "", ref value, openFileDialog.FileName); // load decel
                //    Nload_Decel.Value = double.Parse(value);
                //    Nload_value_Decel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("NOLOAD", "DECEL", "", ref value, openFileDialog.FileName); // load decel
                //    Nload_Decel.Value = double.Parse(value);
                //    Nload_value_Decel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("LOAD", "INITIAL", "", ref value, openFileDialog.FileName); // load initial speed
                //    Load_Ispeed.Value = double.Parse(value);
                //    Load_value_Ispeed.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("LOAD", "TARGET", "", ref value, openFileDialog.FileName); // load target speed
                //    Load_Tspeed.Value = double.Parse(value);
                //    Load_value_Tspeed.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("LOAD", "ACCEL", "", ref value, openFileDialog.FileName); // load accel
                //    Load_Accel.Value = double.Parse(value);
                //    Load_value_Accel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("LOAD", "DECEL", "", ref value, openFileDialog.FileName); // load decel
                //    Load_Decel.Value = double.Parse(value);
                //    Load_value_Decel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("LOAD", "DECEL", "", ref value, openFileDialog.FileName); // load decel
                //    Load_Decel.Value = double.Parse(value);
                //    Load_value_Decel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("SCAN", "INITIAL", "", ref value, openFileDialog.FileName); // load initial speed
                //    Scan_Tspeed.Value = double.Parse(value);
                //    Scan_value_Tspeed.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("SCAN", "TARGET", "", ref value, openFileDialog.FileName); // load target speed
                //    Scan_Tspeed.Value = double.Parse(value);
                //    Scan_value_Tspeed.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("SCAN", "ACCEL", "", ref value, openFileDialog.FileName); // load accel
                //    Scan_Accel.Value = double.Parse(value);
                //    Scan_value_Accel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("SCAN", "DECEL", "", ref value, openFileDialog.FileName); // load decel
                //    Scan_Decel.Value = double.Parse(value);
                //    Scan_value_Decel.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("SOL", "SOL_ON", "", ref value, openFileDialog.FileName); // load sol on 
                //    Sol_Ontime.Value = double.Parse(value);
                //    Txt_Sol_ONtime.Text = double.Parse(value).ToString();
                //    value = "";

                //    Util.GetPrivateProfileValue2("SOL", "SOL_OFF", "", ref value, openFileDialog.FileName); // load sol off
                //    Sol_Offtime.Value = double.Parse(value);
                //    Txt_Sol_OFFtime.Text = double.Parse(value).ToString();
                //    value = "";
                //    File.Exists(openFileDialog.FileName);
                //}
            }
            catch (Exception ERR)
            {
                MessageBox.Show(ERR.ToString());
            }
        }

        private async void TXT_JOG_UP_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //string value = "";
            short stepLength = 0;
            //short zero = 0;
            short distance = 0;

            try
            {
                ShowLog("JOG Y+ START");

                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                //value = zero.ToString("X4") + (Set_Res() * stepLength).ToString("X4");
                //byte[] sbuff = Encoding.UTF8.GetBytes(value);
                //m_currCMD = (byte)'J';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.Jog_XY(sbuff, sbuff.Length);

                distance = (short)(GetDistanceXY() * stepLength);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(0, distance, 0);
                if (retval.execResult != 0)
                {
                    ShowLog("JOG Y+ ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("JOG Y+ SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("JOG Y+ EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void TXT_JOG_DOWN_Click(object sender, RoutedEventArgs e)
        {
            //string value = "";
            short stepLength = 0;
            //short zero = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //short val = 0;
            short distance = 0;

            try
            {
                ShowLog("JOG Y- START");

                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                //val = (short)-(Set_Res() * stepLength);
                //value = zero.ToString("X4") + val.ToString("X4");
                //byte[] sbuff = Encoding.UTF8.GetBytes(value);
                //m_currCMD = (byte)'J';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.Jog_XY(sbuff, sbuff.Length);

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                distance = (short)(GetDistanceXY() * (-1) * stepLength);
                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(0, distance, 0);
                if (retval.execResult != 0)
                {
                    ShowLog("JOG Y- ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("JOG Y- SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("JOG Y- EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void TXT_JOG_LEFT_Click(object sender, RoutedEventArgs e)
        {
            //string value = "";
            short stepLength = 0;
            //short zero = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //short val = 0;
            short distance = 0;

            try
            {
                ShowLog("JOG X- START");

                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                //val = (short)-(Set_Res() * stepLength);
                //value = val.ToString("X4") + zero.ToString("X4");
                //byte[] sbuff = Encoding.UTF8.GetBytes(value);
                //m_currCMD = (byte)'J';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.Jog_XY(sbuff, sbuff.Length);

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                distance = (short)(GetDistanceXY() * (-1) * stepLength);
                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(distance, 0, 0);
                if (retval.execResult != 0)
                {
                    ShowLog("JOG X- ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("JOG X- SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("JOG X- EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void TXT_JOG_RIGHT_Click(object sender, RoutedEventArgs e)
        {
            //string value = "";
            short stepLength = 0;
            //short zero = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short distance = 0;

            try
            {
                ShowLog("JOG X+ START");

                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                //value = (Set_Res() * stepLength).ToString("X4") + zero.ToString("X4");
                //byte[] sbuff = Encoding.UTF8.GetBytes(value);
                //m_currCMD = (byte)'J';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.Jog_XY(sbuff, sbuff.Length);

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                distance = (short)(GetDistanceXY() * stepLength);
                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(distance, 0, 0);
                if (retval.execResult != 0)
                {
                    ShowLog("JOG X+ ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("JOG X+ SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("JOG X+ EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void TXT_JOG_HOME_Click(object sender, RoutedEventArgs e)
        {
            string value = "";
            short Length = 0;
            short steplength = 0;
            short zero = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                ShowLog("JOG HOME START");

                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                Length = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 0, Constants.MARKING_INI_FILE);
                //steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                Length = (short)(Length * steplength);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoHome(0, Length);
                if (retval.execResult != 0)
                {
                    ShowLog("JOG HOME ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("JOG HOME SUCCESS");

                //value = zero.ToString("X4") + Length.ToString("X4");
                //byte[] sbuff = Encoding.UTF8.GetBytes(value);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.GoHome(sbuff, sbuff.Length);
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("JOG HOME EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        //private async void BT_GoPointXY_Click(object sender, RoutedEventArgs e)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    //short Sol = 0;
        //    short startX = 0;
        //    short startY = 0;
        //    short zero = 0;
        //    try
        //    {
        //        if ((txtCurrentPosX.Text.Length <= 0) || (txtCurrentPosY.Text.Length <= 0))
        //            return;

        //        ShowLog("JOG HOME START");
        //        short.TryParse(txtCurrentPosX.Text, out startX);
        //        short.TryParse(txtCurrentPosY.Text, out startY);
        //        byte[] sbuff = Encoding.UTF8.GetBytes(startX.ToString("X4") + startY.ToString("X4") + zero.ToString("X4"));

        //        currentCommand = (byte)'R';
        //        retval = await((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.GoPoint(sbuff, sbuff.Length);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        private async Task<ITNTResponseArgs> SendMotorSpeed(byte motortype)
        {
            MotorSpeed load = new MotorSpeed();
            MotorSpeed noload = new MotorSpeed();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                if (motortype == 0)
                {
                    short.TryParse(txbSpeedFastInitialValue.Text, out noload.initSpeed);
                    short.TryParse(txbSpeedFastTargetValue.Text, out noload.targetSpeed);
                    short.TryParse(txbSpeedFastAccelValue.Text, out noload.accelSpeed);
                    short.TryParse(txbSpeedFastDecelValue.Text, out noload.decelSpeed);

                    short.TryParse(txbSpeedMarkInitialValue.Text, out load.initSpeed);
                    short.TryParse(txbSpeedMarkTargetValue.Text, out load.targetSpeed);
                    short.TryParse(txbSpeedMarkAccelValue.Text, out load.accelSpeed);
                    short.TryParse(txbSpeedMarkDecelValue.Text, out load.decelSpeed);


                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.LoadSpeed((byte)'L', load.initSpeed, load.targetSpeed, load.accelSpeed, load.decelSpeed);
                    if (retval.execResult != 0)
                    {
                        ShowLog("SEND SPEED ERROR - LOAD LOAD SPEED ERROR(" + retval.execResult + ")");
                        return retval;
                    }

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.LoadSpeed((byte)'F', noload.initSpeed, noload.targetSpeed, noload.accelSpeed, noload.decelSpeed);
                    if (retval.execResult != 0)
                    {
                        ShowLog("SEND SPEED ERROR - LOAD NOLOAD SPEED ERROR(" + retval.execResult + ")");
                        return retval;
                    }
                }
                else
                {
                    short.TryParse(txbSpeedScanInitialValue.Text, out load.initSpeed);
                    short.TryParse(txbSpeedScanTargetValue.Text, out load.targetSpeed);
                    short.TryParse(txbSpeedScanAccelValue.Text, out load.accelSpeed);
                    short.TryParse(txbSpeedScanDecelValue.Text, out load.decelSpeed);

                    short.TryParse(txbSpeedScanFastInitialValue.Text, out noload.initSpeed);
                    short.TryParse(txbSpeedScanFastTargetValue.Text, out noload.targetSpeed);
                    short.TryParse(txbSpeedScanFastAccelValue.Text, out noload.accelSpeed);
                    short.TryParse(txbSpeedScanFastDecelValue.Text, out noload.decelSpeed);

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.LoadSpeed((byte)'l', load.initSpeed, load.targetSpeed, load.accelSpeed, load.decelSpeed);
                    if (retval.execResult != 0)
                    {
                        ShowLog("SEND SPEED ERROR - LOAD LOAD SPEED ERROR(" + retval.execResult + ")");
                        return retval;
                    }

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.LoadSpeed((byte)'f', noload.initSpeed, noload.targetSpeed, noload.accelSpeed, noload.decelSpeed);
                    if (retval.execResult != 0)
                    {
                        ShowLog("SEND SPEED ERROR - LOAD NOLOAD SPEED ERROR(" + retval.execResult + ")");
                        return retval;
                    }

                }

                return retval;
            }
            catch(Exception ex)
            {
                string log = "";
                log = string.Format("SEND MOTOR SPEED EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "SetControllerWindow", "SendMotorSpeed", string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        private async void BT_GoParking_Point_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            double parkX = 0;
            double parkY = 0;
            short steplength = 0;
            MotorSpeed load = new MotorSpeed();
            MotorSpeed noload = new MotorSpeed();
            try
            {
                ShowLog("GO TO PARKING POSITION START");

                //if ((lblPark_X.Content.ToString().Length <= 0) || (lblPark_Y.Content.ToString().Length <= 0))
                //if ((txtCurrentPosX.Text.Length <= 0) || (txtCurrentPosY.Text.Length <= 0))
                if ((txtPark_X.Text.Length <= 0) || (txtPark_Y.Text.Length <= 0))
                {
                    ShowLog("GO TO PARKING POSITION ERROR - ENTETR CURRENT X/Y POSITION");
                    return;
                }

                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.LoadSpeed((byte)'L', load.initSpeed, load.targetSpeed, load.accelSpeed, load.decelSpeed);
                if(retval.execResult != 0)
                {
                    ShowLog("GO TO PARKING POSITION ERROR - LOAD LOAD SPEED ERROR(" + retval.execResult + ")");
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.LoadSpeed((byte)'L', noload.initSpeed, noload.targetSpeed, noload.accelSpeed, noload.decelSpeed);
                if (retval.execResult != 0)
                {
                    ShowLog("GO TO PARKING POSITION ERROR - LOAD NOLOAD SPEED ERROR(" + retval.execResult + ")");
                    return;
                }

                parkX = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, Constants.PARAMS_INI_FILE); // load X_Parking Position
                parkY = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, Constants.PARAMS_INI_FILE); // load Y Parking Position
                steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

                m_currCMD = (byte)'K';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(parkX, parkY);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking((short)(parkX * steplength), (short)(parkY * steplength));
                if (retval.execResult != 0)
                {
                    ShowLog("GO PARKING TO POSITION ERROR - GO PARK ERROR(" + retval.execResult + ")");
                    return;
                }
                ShowLog("GO TO PARKING POSITION SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("GO TO PARKING POSITION EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void Btn_Scan_Left_Jog_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            short stepLength = 0;

            try
            {
                ShowLog("JOG SCAN U- START");

                if (double.Parse(txtCurrentPosU.Text) >= int.Parse(lblMAX_U.Content.ToString()))
                {
                    ShowLog("JOG SCAN U- MAX");
                    return;
                }

                retval = await SendMotorSpeed(1);
                if (retval.execResult != 0)
                {
                    return;
                }

                stepLength = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);
                m_currCMD = (byte)'j';
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.ScanJog(0, Set_Res(), stepLength);
                if (retval.execResult != 0)
                {
                    ShowLog("JOG SCAN U- ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("JOG SCAN U- SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("JOG SCAN U- EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void Btn_ScanRight_Jog_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            double stepLength = 0.0d;

            try
            {
                ShowLog("JOG SCAN U+ START");

                if (double.Parse(txtCurrentPosU.Text) >= int.Parse(lblMAX_U.Content.ToString()))
                {
                    ShowLog("JOG SCAN U+ MAX");
                    return;
                }

                retval = await SendMotorSpeed(1);
                if (retval.execResult != 0)
                {
                    return;
                }

                Util.GetPrivateProfileValue("CONFIG", "STEP_LENGTH", "100", ref value, Constants.SCANNER_INI_FILE);
                double.TryParse(value, out stepLength);
                m_currCMD = (byte)'j';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.ScanJog(1, Set_Res(), stepLength);
                if(retval.execResult != 0)
                {
                    ShowLog("JOG SCAN U+ ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("JOG SCAN U+ SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("JOG SCAN U+ EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void Btn_Scan_Home_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //PatternValue pattern = new PatternValue();
            short HOM_U;
            double stepLength_u = 0.0d;
            double home = 0.0d;
            try
            {
                ShowLog("MOVE SCAN HOME START");

                retval = await SendMotorSpeed(1);
                if (retval.execResult != 0)
                {
                    return;
                }

                //GetPatternData(ref pattern);

                string value = "";
                Util.GetPrivateProfileValue("CONFIG", "STEP_LENGTH", "100", ref value, Constants.SCANNER_INI_FILE);
                double.TryParse(value, out stepLength_u);
                double.TryParse(lblVisionHomePos.Content.ToString(), out home);
                HOM_U = (short)(home * stepLength_u + 0.5d);

                m_currCMD = (byte)'h';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoHome_Z(HOM_U);
                if (retval.execResult != 0)
                {
                    ShowLog("MOVE SCAN HOME ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("MOVE SCAN HOME SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("MOVE SCAN HOME EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void Btn_Profile_Scan_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //byte[] ProfileSpeed;
            //byte[] scan;
            short value = 0;
            double ScanLenU = 0;
            double StartPosU = 0;
            short Step_Length_U = 0;
            int timeoutval = 8;
            try
            {
                //StartPosU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "STARTPOS", 35, Constants.SCANNER_INI_FILE);// LOAD Scan Start Position U Axis
                double.TryParse(txtVisionStartPos.Text, out StartPosU);
                double.TryParse(txtVisionScanLength.Text, out ScanLenU);
                //ScanLenU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "SCANLEN", 130, Constants.SCANNER_INI_FILE);// LOAD Scan Length U Axis
                Step_Length_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);// LOAD Step_Length U Axis
                value = (short)((ScanLenU + StartPosU) * Step_Length_U);
                timeoutval = (int)Util.GetPrivateProfileValueUINT("PROFILE", "SCANTIMEOUT", 8, Constants.PARAMS_INI_FILE);
                m_currCMD = (byte)'U';
                ShowLog("SCAN PROFILE START");

                retval = await SendMotorSpeed(1);
                if (retval.execResult != 0)
                {
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.ScanProfile(value, timeoutval);
                if (retval.execResult != 0)
                {
                    ShowLog("SCAN PROFILE ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("SCAN PROFILE SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("SCAN PROFILE EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }
         
        private async void Btn_Parking_U_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            double Parking_Value = 0;
            short Step_Length_U = 0;
            string value = "";
            int timeoutval = 8; 
            try
            {
                ShowLog("PARK PROFILE START");

                retval = await SendMotorSpeed(1);
                if (retval.execResult != 0)
                {
                    return;
                }

                Parking_Value = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 0, Constants.SCANNER_INI_FILE);
                Step_Length_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);// LOAD Step_Length U Axis
                timeoutval = (int)Util.GetPrivateProfileValueUINT("PROFILE", "SCANTIMEOUT", 8, Constants.PARAMS_INI_FILE);
                Parking_Value = Parking_Value * stepLength_u;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveScanProfile((short)Parking_Value, timeoutval);// .   mark.GoParking_U("k", (Parking_Value * Mode_File.Step_Length_U).ToString("X4")).Result;
                if (retval.execResult != 0)
                {
                    ShowLog("PARK PROFILE ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("PARK PROFILE SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("PARK PROFILE EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void Btn_Start_Pos_U_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            double StartPosU = 0;
            short Step_Length_U = 0;
            //string patternfile = "";
            int timeoutval = 8;
            try
            {
                ShowLog("MOVE PROFILE TO STARTING POINT START");

                retval = await SendMotorSpeed(1);
                if (retval.execResult != 0)
                {
                    return;
                }

                timeoutval = (int)Util.GetPrivateProfileValueUINT("PROFILE", "SCANTIMEOUT", 8, Constants.PARAMS_INI_FILE);
                double.TryParse(txtVisionStartPos.Text, out StartPosU);
                Step_Length_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);// LOAD Step_Length U Axis
                StartPosU = StartPosU * Step_Length_U;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveScanProfile((short)StartPosU, timeoutval);
                if (retval.execResult != 0)
                {
                    ShowLog("MOVE PROFILE TO STARTING POINT ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("MOVE PROFILE TO STARTING POINT SUCCESS");

            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("MOVE PROFILE TO STARTING POINT EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        //private void Btn_Set_GearU_Click(object sender, RoutedEventArgs e)
        //{
        //    short ten = 10;
        //    //var Gear_U = string.Concat(ten.ToString("X8"), ten.ToString("X8"));
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    byte[] gearu = Encoding.UTF8.GetBytes(ten.ToString("X4") + ten.ToString("X4"));
        //    retval = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.GearRatio_U(gearu, gearu.Length).Result;
        //}

        //private void Btn_Set_Gear_Head_Click(object sender, RoutedEventArgs e)
        //{
        //    short ten = 10;
        //    short sixteen = 16;
        //    string gear = sixteen.ToString("X4") + ten.ToString("X4");
        //    byte[] bygear = Encoding.UTF8.GetBytes(gear);
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    retval = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GearRatio(bygear, bygear.Length).Result;
        //}

        private async void Btn_Run_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                ShowLog("RUN START");
                m_currCMD = (byte)'R';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart(0);
                if (retval.execResult != 0)
                {
                    ShowLog("RUN ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("RUN SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("RUN EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void btnInPort_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.Inport();
            if (retval.execResult <= 0)
            {
                return;
            }

            if ((Array.IndexOf(retval.recvBuffer, (byte)ASCII.CR) < 0) && (Array.IndexOf(retval.recvBuffer, (byte)ASCII.ACK) < 0))
            {
                //int nSTX = retval.response.IndexOf(((char)2).ToString());
                //string MSG_ = retval.response.Substring(nSTX + 1, 2);
                //string sensor = retval.response.Substring(nSTX + 2, 2);

                //Txt_Sensor.Text = "G_input: " + Convert.ToInt32(MSG_, 16) + " G_sensor: " + Convert.ToInt32(sensor, 16);

                //Mode_File.G_Input = Convert.ToInt32(MSG_, 16);
                //Mode_File.G_Sensor = Convert.ToInt32(sensor, 16);

                //if ((Mode_File.Dec2Bin(Mode_File.G_Input).Substring(System.Convert.ToInt32(Mode_File.Dec2Bin(Mode_File.G_Input).Length - 1), 1) == "0"))
                //{

                //    MarkController.DONE_IO(false);
                //    MarkController.READY_IO(false);
                //    Start_TEXT(Txt_data_Manual.Text, "Parameter.ini");
                //}
                //else if ((Mode_File.Dec2Bin(Mode_File.G_Sensor).Substring(System.Convert.ToInt32(Mode_File.Dec2Bin(Mode_File.G_Sensor).Length - 3), 1) == "0"))
                //{

                //    MarkController.DONE_IO(false);
                //    MarkController.READY_IO(false);
                //    Start_TEXT(Txt_data_Manual.Text, "Parameter.ini");

                //}
                //if ((Mode_File.Dec2Bin(Mode_File.G_Input).Substring(Mode_File.Dec2Bin(Mode_File.G_Input).Length - 2, 1) == "0"))
                //{

                //    retval = ((MainWindow)System.Windows.Application.Current.MainWindow).mark.Resume("r", 0.ToString("X4")).Result;
                //    MarkController.DONE_IO(false);
                //    MarkController.READY_IO(false);
                //}
            }
            else
            {

            }
        }

        //private void Nload_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Ispeed.Text = n.ToString();
        //}

        //private void Nload_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Tspeed.Text = n.ToString();
        //}

        //private void Nload_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Accel.Text = n.ToString();
        //}

        //private void Nload_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Decel.Text = n.ToString();
        //}

        //private void Load_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Ispeed.Text = n.ToString();
        //}

        //private void Load_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Tspeed.Text = n.ToString();
        //}

        //private void Load_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Accel.Text = n.ToString();
        //}

        //private void Load_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Decel.Text = n.ToString();
        //}

        //private void Scan_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Ispeed.Text = n.ToString();
        //}

        //private void Scan_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Tspeed.Text = n.ToString();
        //}

        //private void Scan_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Accel.Text = n.ToString();
        //}

        //private void Scan_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Decel.Text = n.ToString();
        //}

        //private void LoadScanFree_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(ScanFree_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    ScanFree_value_Ispeed.Text = n.ToString();
        //}

        //private void LoadScanFree_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(ScanFree_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    ScanFree_value_Tspeed.Text = n.ToString();
        //}

        //private void LoadScanFree_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(ScanFree_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    ScanFree_value_Accel.Text = n.ToString();
        //}

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Btn_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow";
            string funcName = "btnSaveAsPattern_Click";

            string patName = "";
            byte bHeadType = 0;
            string value = "";
            //bool bret = false;
            //string filename = "";
            PatternValueEx pattern = new PatternValueEx();
            List<string> names = new List<string>();
            SaveFileDialog savedialog = new SaveFileDialog();
            string cmd = "SAVE AS PATTERN FILE";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                //ShowLog("START [SAVE AS PATTERN FILE]");

                if (cbxPatternList.SelectedIndex < 0)
                {
                    //ShowLog("[SAVE AS PATTERN FILE] ERROR - SELECT PATTERN");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "PATTERN NOT SELECTED");
                    return;
                }

                savedialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
                savedialog.FileName = cbxPatternList.SelectedValue.ToString() + ".ini";
                savedialog.Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*";
                if (savedialog.ShowDialog() == false)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "USER CANCEL");
                    return;
                }

                patName = System.IO.Path.GetFileNameWithoutExtension(savedialog.FileName);
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                ReadPatternValue(ref pattern);
                pattern.name = patName;
                ImageProcessManager.SetPatternValue(pattern.name, bHeadType, pattern, 0);

                //string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
                //names = DirFileSearch(patternfile, "*.ini").Result;
                //cbxPatternList.Items.Clear();
                //for (int i = 0; i < names.Count; i++)
                //{
                //    if (cbxPatternList.Items.Contains(Name[i]) == false)
                //        cbxPatternList.Items.Add(names[i]);
                //}
                int index = cbxPatternList.Items.IndexOf(patName);
                if (index < 0)
                    cbxPatternList.Items.Add(patName);

                if (cbxPatternList.Items.Count > 0)
                {
                    cbxPatternList.SelectedItem = patName;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //if(txtPatternName.Text.Length <= 0)
            //{
            //    ShowLog("Enter pattern name saved.");
            //    return;
            //}

            //PatternValueEx pat = new PatternValueEx();
            //ReadPatternData(ref pat);
            //pat.name = txtPatternName.Text;
            //string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
            //string patName = patternfile + txtPatternName.Text + ".ini";
            //SavePatternData(patName, pat, 0);
            //ShowLog(txtPatternName.Text + "is saved");
        }



        private bool CheckSavePatternData(string patternName)
        {
            string className = "SetControllerWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "CheckSavePatternData";// MethodBase.GetCurrentMethod().Name;

            bool ret = false;
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                msg2.Message = "Do you want to save pattern?";
                msg2.Fontsize = 20;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

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
            }
            catch (Exception ex)
            {
                ret = false;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            return ret;
        }



        private void cbxPatternList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string name = cbxPatternList.SelectedItem.ToString();
            ////string patternname = Constants.PATTERN_PATH + name + ".ini";
            //PatternValueEx patData = new PatternValueEx();

            //if(!saveFlag)
            //{
            //    saveFlag = true;
            //}
            //else
            //{
            //    ReadPatternValue(ref patData);
            //    if (patData != orgPattern)
            //    {
            //        ConfirmWindowString msg1 = new ConfirmWindowString();
            //        ConfirmWindowString msg2 = new ConfirmWindowString();
            //        ConfirmWindowString msg3 = new ConfirmWindowString();
            //        ConfirmWindowString msg4 = new ConfirmWindowString();
            //        ConfirmWindowString msg5 = new ConfirmWindowString();

            //        msg2.Message = orgPattern.name + " : Do you want to save this Pattern?";
            //        msg2.Fontsize = 20;
            //        msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
            //        msg2.VerticalContentAlignment = VerticalAlignment.Center;
            //        msg2.Foreground = Brushes.Red;
            //        msg2.Background = Brushes.White;

            //        //msg2.Message = "각인하시려면 [OK]를 선택하시고,";
            //        //msg2.Fontsize = 16;
            //        //msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
            //        //msg2.VerticalContentAlignment = VerticalAlignment.Center;
            //        //msg2.Foreground = Brushes.Blue;
            //        //msg2.Background = Brushes.White;

            //        //msg3.Message = "재 설정이 필요하시면[NO]를 선택하세요.";
            //        //msg3.Fontsize = 16;
            //        //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
            //        //msg3.VerticalContentAlignment = VerticalAlignment.Center;
            //        //msg3.Foreground = Brushes.Blue;
            //        //msg3.Background = Brushes.White;

            //        bool ret = false;
            //        if (CheckAccess())
            //        {
            //            WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
            //            warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //            ret = warning.ShowDialog().Value;
            //        }
            //        else
            //        {
            //            Dispatcher.Invoke(new Action(delegate
            //            {
            //                WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
            //                warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //                ret = warning.ShowDialog().Value;
            //            }));
            //        }

            //        if (ret == true)
            //        {
            //            PatternValueEx pattern = new PatternValueEx();
            //            ReadPatternValue(ref pattern);
            //            SavePatternData(orgPattern.name, pattern, 0);
            //        }
            //    }
            //}

            //LoadPatternData(name, ref orgPattern);
            //DisplayPatternData(orgPattern);
            //orgPattern.name = name;


            string className = "SetControllerWindow";
            string funcName = "cbxPatternList_SelectionChanged";
            PatternValueEx newpattern = new PatternValueEx();
            PatternValueEx curpattern = new PatternValueEx();
            string patname = "";
            string value = "";
            byte bHeadType = 0;
            bool bret = false;
            string AreaPosition = "";

            //int tmpDelayTime1 = 0;
            //int tmpDelayTime2 = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                if (cbxPatternList.SelectedIndex < 0)
                {
                    return;
                }
                //cbxPatternList.SelectedIndex = 0;

                patname = cbxPatternList.SelectedItem.ToString();
                if (patname.Length <= 0)
                    patname = "Pattern_DEFAULT";

                ImageProcessManager.GetPatternValue(patname, bHeadType, ref newpattern);
                Util.GetPrivateProfileValue("PLATE", "AREAPOSITION", "25.00", ref AreaPosition, Constants.PARAMS_INI_FILE);

                //currMarkInfo.currMarkData.pattern = (PatternValueEx)newpattern.Clone();

                if (bshowAlready == false)
                {
                    bshowAlready = true;
                }
                else
                {
                    ReadPatternValue(ref curpattern);
                    //int.TryParse(txtMarkDelayTime1.Text, out tmpDelayTime1);
                    //int.TryParse(txtMarkDelayTime2.Text, out tmpDelayTime2);

                    if ((curpattern.fontValue != orgPattern.fontValue) || (curpattern.positionValue != orgPattern.positionValue) ||
                        (curpattern.headValue != orgPattern.headValue) || (curpattern.speedValue != orgPattern.speedValue) ||
                        (curpattern.laserValue != orgPattern.laserValue))// || (tmpDelayTime1 != markDelayTime1) || (tmpDelayTime2 != markDelayTime2))
                    {
                        bret = CheckSavePatternData(orgPattern.name);
                        if (bret == true)
                        {
                            ImageProcessManager.SetPatternValue(orgPattern.name, bHeadType, orgPattern, 0);
                        }
                    }
                }

                DisplayPatternValue(0, newpattern);
                //txtAreaPosition.Text = AreaPosition;
                orgPattern = (PatternValueEx)newpattern.Clone();
                orgPattern.name = patname;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnServoOnOff_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short maxY = 0;
            short stepLength = 0;
            short HOM_U;
            double stepLength_u = 0.0d;
            double home = 0.0d;

            maxY = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, Constants.MARKING_INI_FILE);
            stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

            if (btnServoOnOff.Content.ToString() == "SERVO ON")
            {
                ShowLog("SERVO ON START");
                m_currCMD = (byte)'O';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(12, true);
                //var num = string.Concat(0.ToString("X4"), ((short)Mode_File.MAX_Y).ToString("X4"));

                string value = "";
                Util.GetPrivateProfileValue("CONFIG", "STEP_LENGTH", "100", ref value, Constants.SCANNER_INI_FILE);
                double.TryParse(value, out stepLength_u);
                double.TryParse(lblVisionHomePos.Content.ToString(), out home);
                HOM_U = (short)(home * stepLength_u + 0.5d);

                //Go Home
                m_currCMD = (byte)'E';
                maxY = (short)(maxY * stepLength);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoHome(0, maxY);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoHomeAll(0, maxY, HOM_U);
                if (retval.execResult != 0)
                {
                    ShowLog("SERVO ON ERROR (" + retval.execResult + ")");
                    return;
                }
                btnServoOnOff.Content = "SERVO OFF";
                ShowLog("SERVO ON SUCCESS");
            }
            else
            {
                ShowLog("SERVO OFF START");
                m_currCMD = (byte)'O';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(12, false);
                if (retval.execResult != 0)
                {
                    ShowLog("SERVO OFF ERROR (" + retval.execResult + ")");
                    return;
                }
                btnServoOnOff.Content = "SERVO ON";
                ShowLog("SERVO OFF SUCCESS");
            }
        }

        private async void BT_Limit_XY_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short Max_x = 0;
            short Max_y = 0;
            try
            {
                ShowLog("LIMIT XY START");
                m_currCMD = (byte)'W';

                Max_x = (short)Util.GetPrivateProfileValueUINT("MAX", "MAX_X", 0, Constants.PARAMS_INI_FILE); // load  MAX_LIMIT_X;
                Max_y = (short)Util.GetPrivateProfileValueUINT("MAX", "MAX_Y", 0, Constants.PARAMS_INI_FILE); // load  MAX_LIMIT_Y;
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.Move2LimitXY(Max_x, Max_y);
                if (retval.execResult != 0)
                {
                    ShowLog("LIMIT XY ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("LIMIT XY SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("LIMIT XY EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void BT_Limit_U_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short Max_U = 0;
            try
            {
                Max_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "MAX_U", 0, Constants.SCANNER_INI_FILE); // load  MAX_LIMIT_U;
                ShowLog("LIMIT U START");
                m_currCMD = (byte)'w';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart(0);
                if (retval.execResult != 0)
                {
                    ShowLog("LIMIT U ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("LIMIT U SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("RUN EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void Btn_DryRun_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                ShowLog("DRY RUN START");
                m_currCMD = (byte)'R';
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart(1);
                if (retval.execResult != 0)
                {
                    ShowLog("DRY RUN ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("DRY RUN SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("DRY RUN EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void BT_GoPointXY_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short stepLength = 0;
            double posX = 0.0d;
            double posY = 0.0d;
            try
            {
                stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

                ShowLog("GO POINT START");

                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                double.TryParse(txtGoPointX.Text, out posX);
                double.TryParse(txtGoPointY.Text, out posY);

                posX = posX * stepLength;
                posY = posY * stepLength;

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint((short)posX, (short)posY, 0);
                if (retval.execResult != 0)
                {
                    ShowLog("GO POINT ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("GO POINT SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("GO POINT EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private void BT_SetStartPoint_Click(object sender, RoutedEventArgs e)
        {
            //ITNTResponseArgs retval = new ITNTResponseArgs();
            //short stepLength = 0;
            //double posX = 0.0d;
            //double posY = 0.0d;
            string patternName = "";
            string patternFileName;
            try
            {
                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

                //double.TryParse(txtGoPointX.Text, out posX);
                //double.TryParse(txtGoPointY.Text, out posY);

                if((cbxPatternList.Items.Count > 0) && (cbxPatternList.SelectedIndex >= 0))
                {
                    patternName = cbxPatternList.SelectedItem.ToString();
                    patternFileName = Constants.PATTERN_PATH + patternName + ".ini";

                    Util.WritePrivateProfileValue("FONT", "STARTPOSX", txtGoPointX.Text.ToString(), patternFileName);
                    Util.WritePrivateProfileValue("FONT", "STARTPOSY", txtGoPointY.Text.ToString(), patternFileName);

                    txtStartX.Text = txtGoPointX.Text.ToString();
                    txtStartY.Text = txtGoPointY.Text.ToString();
                }
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("GO POINT EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc -= OnMarkControllerEventFunc;
            ((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 0;
        }

        private async void btnTest4RealMarkArea_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                ShowLog("TEST MARK AREA - FLUSH START");
                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                m_currCMD = (byte)'M';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.Head_Range_Test();
                if (retval.execResult != 0)
                {
                    ShowLog("TEST MARK AREA - ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("TEST MARK AREA - SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("TEST MARK AREA - EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void BT_GoStartPoint_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            double startX = 0.0d;
            double startY = 0.0d;
            int istartX = 0;
            int istartY = 0;

            try
            {
                ShowLog("GO START POINT - FLUSH START");
                retval = await SendMotorSpeed(0);
                if (retval.execResult != 0)
                {
                    return;
                }

                double.TryParse(txtStartX.Text, out startX);
                double.TryParse(txtStartY.Text, out startY);

                istartX = (int)(startX * stepLength + 0.5);
                istartY = (int)(startY * stepLength + 0.5);

                m_currCMD = (byte)'M';
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoStartPoint(istartX, istartY, 0);
                if (retval.execResult != 0)
                {
                    ShowLog("GO START POINT - ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("GO START POINT - SUCCESS");
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("GO START POINT - EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private async void btnFirmwareVersion_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                ShowLog("FIRMWARE VERSION START");
                m_currCMD = (byte)'V';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GetFWVersion2();// (istartX, istartY, 0);
                if (retval.execResult != 0)
                {
                    ShowLog("FIRMWARE VERSION - ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("FIRMWARE VERSION : " + retval.recvString);
            }
            catch (Exception ex)
            {
                string log = "";
                log = string.Format("FIRMWARE VERSION- EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog(log);
            }
        }

        private void btnMarkCenter_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Bt_CheckPlate_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //try
            //{
            //    //var ManStr = Txt_data_Manual.Text.ToUpper();

            //    //var jobtask = Task.Run(() => mark.Range_Test(ManStr));
            //    //await jobtask;

            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);

            //}
            //catch (Exception err)
            //{
            //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Btn_AreaTest_Click = {0:X}, MSG = {1}", err.HResult, err.Message));
            //}
        }

        //private void LoadScanFree_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(ScanFree_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    ScanFree_value_Decel.Text = n.ToString();
        //}

        //private void Nload_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Ispeed.Text = n.ToString();
        //}

        //private void Nload_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Tspeed.Text = n.ToString();
        //}

        //private void Nload_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Accel.Text = n.ToString();
        //}

        //private void Nload_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Nload_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Nload_value_Decel.Text = n.ToString();
        //}

        //private void Load_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Ispeed.Text = n.ToString();
        //}

        //private void Load_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Tspeed.Text = n.ToString();
        //}

        //private void Load_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Accel.Text = n.ToString();
        //}

        //private void Load_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Load_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Load_value_Decel.Text = n.ToString();
        //}

        //private void Scan_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Ispeed.Text = n.ToString();
        //}

        //private void Scan_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Tspeed.Text = n.ToString();
        //}

        //private void Scan_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Accel.Text = n.ToString();
        //}

        //private void Scan_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Scan_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Scan_value_Decel.Text = n.ToString();
        //}

        //private void Sol_Ontime_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Sol_Ontime.Value);
        //    int n = Convert.ToInt32(x);
        //    Txt_Sol_ONtime.Text = n.ToString();
        //}

        //private void Sol_Offtime_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Sol_Offtime.Value);
        //    int n = Convert.ToInt32(x);
        //    Txt_Sol_OFFtime.Text = n.ToString();
        //}
    }
}
