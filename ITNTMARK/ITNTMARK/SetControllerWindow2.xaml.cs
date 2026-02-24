using ITNTCOMMM;
using ITNTCOMMON;
using ITNTUTIL;
using Microsoft.Win32;
//using S7.Net.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static ITNTCOMMON.StateObject;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Data;
//using S7.Net.Types;
//using S7.Net.Types;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// SetControllerWindow2.xaml에 대한 상호 작용 논리
    /// </summary>

    enum motorSpeedType
    {
        HomeMoving = 0,
        FastMoving = 1,
        MarkMoving = 2,
        MeasureMoving = 3,
        ScanMoving = 4,
        ScanFastMoving = 5,
        CleanMoving = 6,
    }

    enum LOGTYPE
    {
        LOG_START=0,
        LOG_END=1,
        LOG_SUCCESS=2,
        LOG_FAILURE=3,
        LOG_NORMAL = 4,
    }

    public partial class SetControllerWindow2 : Window
    {
        //public static bool Wait_IO;
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        //Point NOW_POS = new Point();
        byte m_currCMD = 0;
        //bool doingCommand = false;
        PatternValueEx orgPattern = new PatternValueEx();
        //bool saveFlag = false;
        //short stepLength_u = 0;
        //short stepLength = 0;
        //byte fwVersionFlag = 0;
        //List<string> SendData = new List<string>();
        //List<string> SendClean = new List<string>();

        //MarkStatusInfo2 markdata2 = new MarkStatusInfo2();
        //MarkDataLaser markdataInfo = new MarkDataLaser();

        //short Density232 = 0;

        bool bReadFontValue = false;

        //MarkStatusInfo markdata = new MarkStatusInfo();
        //public static MarkVINInformLaser currMarkInfo = new MarkVINInformLaser();
        public static MarkVINInformEx currMarkInfo = new MarkVINInformEx();
        Line charline = new Line();
        Ellipse Dotline = new Ellipse();
        bool bshowAlready = false;


        DispatcherTimer statusTimer = new DispatcherTimer();

        public SetControllerWindow2()
        {
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            ////DisplayValue();
            string value = "";
            //Util.GetPrivateProfileValue("FONT", "LIST", "7X12|11X16|5X7|OCR|HMC5", ref value, Constants.PARAMS_INI_FILE);
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
                cbxPatternList.SelectedIndex = 0;
            }

            //stepLength_u = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);
            //if (stepLength_u <= 0)
            //    stepLength_u = 100;
            //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 100, Constants.MARKING_INI_FILE);
            //if (stepLength <= 0)
            //    stepLength = 100;

            //if (((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion.Length <= 0)
            //{
            //    string ver = "";
            //    ver = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GetFWVersion().Result;
            //    if (ver.Length > 0)
            //    {
            //        if (ver.CompareTo("101") >= 0)
            //            ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 1;
            //        else
            //            ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
            //        //lblfwVersion.Content = ver;
            //        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion = ver;
            //    }
            //    else
            //    {
            //        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
            //    }
            //}

            //if (((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion.Length > 0)
            //    lblfwVersion.Content = "FIRMWARE VER : " + ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion;
            ShowColorMapSample();
            ((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 2;

            statusTimer.Tick += statusTimerHandler;
            statusTimer.Interval = TimeSpan.FromMilliseconds(1000);
            statusTimer.Start();
            //mesRunningTimer.Interval = TimeSpan.FromSeconds(10);
            //statusTimer.IsEnabled = false;
            //cycleTimer.Stop();

            ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markCommLaser.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
            //((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
        }

        public SetControllerWindow2(string pattern, string vin)
        {
            string value = "";
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            //DisplayValue();
            Util.GetPrivateProfileValue("FONTLIST", "NAME", "7X12|11X16|5X7|OCR|HMC5", ref value, Constants.FONT_INI_FILE);
            string[] vals = value.Split('|');
            for (int i = 0; i < vals.Length; i++)
            {
                if (vals[i].Length > 0)
                    cbxFontName.Items.Add(vals[i]);
            }

            List<string> patternnames = new List<string>();
            string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
            patternnames = DirFileSearch(patternfile, "*.ini").Result;
            for (int i = 0; i < patternnames.Count; i++)
                cbxPatternList.Items.Add(patternnames[i]);
            if (patternnames.Count > 0)
            {
                //DisplayValue(names[0]);
                if (patternnames.Contains(pattern))
                    cbxPatternList.SelectedItem = pattern;
                else
                    cbxPatternList.SelectedIndex = 0;
            }

            txtVIN.Text = vin;

            //if (((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion.Length <= 0)
            //{
            //    string ver = "";
            //    ver = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GetFWVersion().Result;
            //    if (ver.Length > 0)
            //    {
            //        if (ver.CompareTo("101") >= 0)
            //            ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 1;
            //        else
            //            ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
            //        //lblfwVersion.Content = ver;
            //        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion = ver;
            //    }
            //    else
            //    {
            //        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion = "";
            //        ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
            //    }
            //}

            //lblfwVersion.Content = ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion;
            ShowColorMapSample();
            ((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 2;

            statusTimer.Tick += statusTimerHandler;
            statusTimer.Interval = TimeSpan.FromMilliseconds(1000);
            statusTimer.Start();

            ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markCommLaser.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
        }


        private async void statusTimerHandler(object sender, EventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "cycleTimerHandler";
            //TimeSpan ts = TimeSpan.FromMilliseconds(10);
            distanceSensorData sensorData = new distanceSensorData();

            try
            {
                sensorData = await ((MainWindow)System.Windows.Application.Current.MainWindow).ReadDisplacementSensor(1, 1);
                if (sensorData.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadDisplacementSensor : ERROR = " + sensorData.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //string timer = string.Format("{0:mm\\:ss\\:f}", cycleWatch.Elapsed); ;// cycleWatch.ElapsedMilliseconds.ToString("");
                //ShowLabelData(timer, lblcycleTime);

                ShowLabelData(lblDisplacementVal1, sensorData.rawdistance.ToString("F4"));
                ShowLabelData(lblDisplacementVal2, sensorData.sensoroffset.ToString("F4"));
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowColorMapSample()
        {
            byte[] data = new byte[1024];
            //TemperatureLabel.Content = "(\u00b0C) Temperature";

            // TM SHIN
            const int Pallete_WIDTH = 35;
            const int Pallete_HEIGHT = 110;

            byte[,,] pixelArrayP = new byte[Pallete_HEIGHT, Pallete_WIDTH, 4];
            for (int y = 0; y < Pallete_HEIGHT; y++)
            {
                for (int x = 0; x < Pallete_WIDTH; x++)
                {
                    if ((y == 0) || (y == (Pallete_HEIGHT - 1)))
                    {
                        pixelArrayP[y, x, 0] = pixelArrayP[y, x, 1] = pixelArrayP[y, x, 2] = pixelArrayP[y, x, 3] = 255;
                    }
                    else
                    {
                        if ((x == 0) || (x == (Pallete_WIDTH - 1)))
                        {
                            pixelArrayP[y, x, 0] = pixelArrayP[y, x, 1] = pixelArrayP[y, x, 2] = pixelArrayP[y, x, 3] = 255;
                        }
                        else
                        {
                            var rgb = GetColorMap((byte)((255.0 * (double)(Pallete_HEIGHT - y) / (double)Pallete_HEIGHT)), false);
                            if ((y == 10 || y == 53 || y == 97) && (x < Pallete_WIDTH / 2))
                            {
                                pixelArrayP[y, x, 0] = (byte)((byte)255 - rgb.Item3);
                                pixelArrayP[y, x, 1] = (byte)((byte)255 - rgb.Item2);
                                pixelArrayP[y, x, 2] = (byte)((byte)255 - rgb.Item1);
                                pixelArrayP[y, x, 3] = 255;
                            }
                            else
                            {
                                pixelArrayP[y, x, 0] = rgb.Item3;
                                pixelArrayP[y, x, 1] = rgb.Item2;
                                pixelArrayP[y, x, 2] = rgb.Item1;
                                pixelArrayP[y, x, 3] = 255;
                            }
                        }
                    }
                }
            }

            byte[] byteArrayP = new byte[Pallete_HEIGHT * Pallete_WIDTH * 4];
            int index = 0;
            for (int row = 0; row < Pallete_HEIGHT; row++)
            {
                for (int col = 0; col < Pallete_WIDTH; col++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        byteArrayP[index++] = pixelArrayP[row, col, i];
                    }
                }
            }

            WriteableBitmap PallteBitmap = new WriteableBitmap
            (
                Pallete_WIDTH,
                Pallete_HEIGHT,
                96,
                96,
                PixelFormats.Bgra32,
                null
            );

            Int32Rect rectangleP = new Int32Rect(0, 0, Pallete_WIDTH, Pallete_HEIGHT);
            int strideP = 4 * Pallete_WIDTH;
            PallteBitmap.WritePixels(rectangleP, byteArrayP, strideP, 0);

            Image imageP = new Image();
            imageP.Stretch = Stretch.None;
            imageP.Margin = new Thickness(0);

            GridPallete.Children.Add(imageP);
            imageP.Source = PallteBitmap;
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //string value = "";
            //byte bHeadType = 0;

            try
            {
                statusTimer.Stop();

                //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                //byte.TryParse(value, out bHeadType);

                ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.LaserControllerStatusEventFunc -= OnLaserControllerStatusChangedEventReceivedFunc;
                ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markCommLaser.MarkControllerDataArrivedEventFunc -= OnMarkControllerEventFunc;
            }
            catch (Exception ex)
            {

            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                ChangeSize(this.ActualWidth, this.ActualHeight);
            }
            this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            //DialogResult = true;
            Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            orginalWidth = this.Width;
            originalHeight = this.Height;
            ChangeSize(e.NewSize.Width, e.NewSize.Height);
        }


        private async void OnLaserMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        {
            string className = "SetControllerWindow2";
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

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                i = 6;
                if (e.receiveSize >= i + 4)
                {
                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam1 = Convert.ToInt32(param1, 16);
                }
                if (e.receiveSize >= i + 4)
                {
                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam2 = Convert.ToInt32(param2, 16);
                }
                if (e.receiveSize >= i + 4)
                {
                    param3 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam3 = Convert.ToInt32(param3, 16);
                    //param3Flag = true;
                }
                if (e.receiveSize >= i + 4)
                {
                    param4 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam4 = Convert.ToInt32(param4, 16);
                    //param4Flag = true;
                }

                currCMD = e.execmd;

                switch (e.stscmd)
                {
                    case 0x30:      //stand by
                        ITNTTraceLog.Instance.TraceHex(1, className + "::" + funcName + "()  RECV MARK :  ", e.receiveSize, e.receiveBuffer);
                        chindex = (iparam4 >> 8);
                        ptindex = iparam4 & 0xff;

                        if (chindex == (byte)'J')
                        {
                            chindex = 0;
                        }

                        //if (((currCMD == 'R') && (m_currCMD == 'R')) || ((currCMD == '@') && (m_currCMD == '@')))
                        if ((currCMD == 'R') || (currCMD == '@'))
                        {
                            //if (!m_bDoingMarkingFlag)
                            //    m_bDoingMarkingFlag = true;

                            if (this.CheckAccess())
                                ShowMarkingOneLine(chindex, ptindex);//, Density232);
                            //ShowLaserMarkingOneLine(chindex, ptindex);
                            else
                            {
                                this.Dispatcher.Invoke(new Action(delegate
                                {
                                    ShowMarkingOneLine(chindex, ptindex);//, Density232);
                                    //ShowLaserMarkingOneLine(chindex, ptindex);
                                }));
                            }
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

                            //tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
                            //ShowTextBoxData(txtCurrentPosX, tmpstring);
                            //tmpstring = string.Format("{0:0.00}", (double)iparam2 / 100.0);
                            //ShowTextBoxData(txtCurrentPosY, tmpstring);
                            //tmpstring = string.Format("{0:0.00}", (double)iparam3 / 100.0);
                            //ShowTextBoxData(txtCurrentPosZ, tmpstring);

                            //ShowLabelData(lblInputPort, iparam4.ToString("X4"));

                            //ControlWindow.Dispatcher.Invoke(new Action(delegate
                            //{
                            //    ControlWindow.TXT_CURRENT_X.Text = string.Format("{0:0.00}", (double)xpos / 100.0);
                            //    ControlWindow.TXT_CURRENT_Y.Text = string.Format("{0:0.00}", (double)ypos / 100.0);
                            //    ControlWindow.TXT_CURRENT_Z.Text = string.Format("{0:0.00}", (double)zpos / 100.0);

                            //    ControlWindow.XY_AXIS_HOME.Background = (((short)xpos == Mode_File.OFFSET_X) && ((short)ypos == Mode_File.OFFSET_Y)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //    ControlWindow.Z_AXIS_HOME.Background = ((short)zpos == Mode_File.OFFSET_Z) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //    ControlWindow.X_AXIS_ALARM.Background = ((port & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                            //    ControlWindow.Y_AXIS_ALARM.Background = ((port & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                            //    ControlWindow.Z_AXIS_ALARM.Background = ((port & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                            //    ControlWindow.InputTxt.Text = port.ToString("X4");


                            //}));


                            //tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
                            //ShowTextBoxData(txtCurrentPosX, tmpstring);
                            //tmpstring = string.Format("{0:0.00}", (double)iparam2 / 100.0);
                            //ShowTextBoxData(txtCurrentPosY, tmpstring);
                            //tmpstring = string.Format("{0:0.00}", (double)iparam3 / 100.0);
                            //ShowTextBoxData(txtCurrentPosZ, tmpstring);

                            //ShowLabelData(lblInputPort, iparam4.ToString("X4"));
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
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "SetControllerWindow2", "OnLaserMarkControllerEventFunc", "COLD BOOT", Thread.CurrentThread.ManagedThreadId);

                        //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref headType, Constants.PARAMS_INI_FILE);
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
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "SetControllerWindow2", "OnLaserMarkControllerEventFunc", "COLD BOOT END", Thread.CurrentThread.ManagedThreadId);
                        break;

                    case 0x38:
                        //count = 0;
                        double dvalue = 0.0d;
                        double.TryParse(param1, out dvalue);
                        //currentPoint.X = dvalue;
                        double.TryParse(param1, out dvalue);
                        //currentPoint.Y = dvalue;

                        //doingCommand = false;
                        if ((currCMD == 'U') && (m_currCMD == 'U'))
                        {
                            //ITNTJobLog.Instance.Trace(0, "[4] : RECEIVE SCAN COMPLETE");
                            Util.GetPrivateProfileValue("OPTION", "VISIONQUICKEND", "0", ref value, Constants.PARAMS_INI_FILE);
                            if (value != "0")
                                recvarg = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendScanComplete(1);
                        }
                        break;

                    case (byte)ASCII.ACK:
                        if (currCMD == '@')
                        {
                            string m_font = "";

                            //if (currMarkInfo.senddata.SendDataIndex < currMarkInfo.senddata.SendDataCount)
                            //{
                            //    m_font = (currMarkInfo.senddata.CleanFireFlag == false) ? currMarkInfo.senddata.sendDataFire.ElementAt(currMarkInfo.senddata.SendDataIndex++) :  currMarkInfo.senddata.sendDataClean.ElementAt(currMarkInfo.senddata.SendDataIndex++);

                            //    Dispatcher.Invoke(new Action(async delegate
                            //    {
                            //        recvarg = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(m_font);
                            //    }));

                            //    if (recvarg.execResult != 0)
                            //    {
                            //        return;
                            //    }
                            //}
                            //else
                            //{
                            //    //S_TIME = DateTime.Now;
                            //    //ControlWindow.Dispatcher.Invoke(new Action(delegate
                            //    //{
                            //    //    if (BatchJobStop) { ControlWindow.Batch_Start.IsEnabled = true; }
                            //    //    ControlWindow.lB_Marking_count.Text = "Marking Count: " + Mark_Counter;
                            //    //    ControlWindow.txt_log.AppendText(DateAndTime.Now + " Mark Sequence Complete (" + Mark_Counter + ")" + Environment.NewLine);
                            //    //    ControlWindow.txt_log.ScrollToEnd();
                            //    //    ControlWindow.cycle_time.Content = " CYCLE TIME:  " + (S_TIME - W_TIME).Minutes + "분: " + (S_TIME - W_TIME).Seconds + "초: " + (S_TIME - W_TIME).Milliseconds;
                            //    //}));

                            //    //Mode_File.EndOfSend = true;
                            //}
                        }
                        break;
                    case 0x39:      //emergency
                        break;

                    default:
                        break;
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "SetControllerWindow2", "OnMarkControllerEventFunc", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "SetControllerWindow2", "OnMarkControllerEventFunc", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void OnMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "OnMarkControllerEventFunc";
            string param1 = "";
            string param2 = "";
            string param3 = "";
            string param4 = "";
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            byte currCMD = 0;
            int iparam1 = 0, iparam2 = 0, iparam3 = 0, iparam4 = 0;
            string tmpstring = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            Brush backbrushXY = null;
            Brush backbrushZ = null;
            //int pos1 = 0;
            //int pos2 = 0;
            //int pos3 = 0;
            PatternValueEx pattern = new PatternValueEx();

            try
            {
                //if (((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow != 2)
                //    return;
                i = 6;
                if (e.receiveSize >= i + 4)
                {
                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam1 = Convert.ToInt32(param1, 16);
                }
                if (e.receiveSize >= i + 4)
                {
                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam2 = Convert.ToInt32(param2, 16);
                }
                if (e.receiveSize >= i + 4)
                {
                    param3 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam3 = Convert.ToInt32(param3, 16);
                    //param3Flag = true;
                }
                if (e.receiveSize >= i + 4)
                {
                    param4 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    iparam4 = Convert.ToInt32(param4, 16);
                    //param4Flag = true;
                }
                currCMD = e.execmd;


                switch (currCMD)
                {
                    case (byte)'R':
                    case (byte)'r':
                    case (byte)'H':
                    case (byte)'J':
                    case (byte)'C':
                    case (byte)'M':
                    case (byte)'K':         // 
                    case (byte)'h':         // Return
                    case (byte)'k':         // Return Job End : 08
                    case (byte)'@':         // Return without Job end   // TM SHIN 3/16
                        switch (e.stscmd)
                        {
                            case (byte)'0': // DONE POINT 
                                chindex = (iparam4 >> 8);
                                ptindex = iparam4 & 0xff;

                                if (chindex == (byte)'J')
                                {
                                    chindex = 0;
                                }

                                if (this.CheckAccess())
                                {
                                    if (currMarkInfo.senddata.CleanFireFlag == true && currMarkInfo.checkdata.TwoLineDisplay == true)
                                        ShowMarkingOneLine(chindex, ptindex - 1);//, Mode_File.Density232);

                                    ShowMarkingOneLine(chindex, ptindex);//, Density232);
                                }
                                else
                                {
                                    this.Dispatcher.Invoke(new Action(delegate
                                    {
                                        if (currMarkInfo.senddata.CleanFireFlag == true && currMarkInfo.checkdata.TwoLineDisplay == true)
                                            ShowMarkingOneLine(chindex, ptindex - 1);//, Mode_File.Density232);

                                        ShowMarkingOneLine(chindex, ptindex);//, Density232);
                                    }));
                                }

                                tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
                                ShowTextBoxData(txtCurrentPosX, tmpstring);
                                tmpstring = string.Format("{0:0.00}", (double)iparam2 / 100.0);
                                ShowTextBoxData(txtCurrentPosY, tmpstring);
                                tmpstring = string.Format("{0:0.00}", (double)iparam3 / 100.0);
                                ShowTextBoxData(txtCurrentPosZ, tmpstring);

                                backbrushXY = (((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.X == (int)(iparam1/100)) && ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Y == (int)(iparam2/100))) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                ShowLabelColor(lblMotorHomeXY, null, backbrushXY);

                                backbrushZ = ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Z == (int)(iparam3/100)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                ShowLabelColor(lblMotorHomeZ, null, backbrushZ);

                                //Brush backAlaramX0 = ((iparam4 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, null, backAlaramX0);
                                //Brush backAlaramY0 = ((iparam4 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, null, backAlaramY0);
                                //Brush backAlaramZ0 = ((iparam4 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, null, backAlaramZ0);

                                break;

                            case (byte)'1': // RUNNING
                                if ((currCMD == (byte)'@') ||
                                    (currCMD == (byte)'H') ||
                                    (currCMD == (byte)'J') ||
                                    (currCMD == (byte)'M') ||
                                    (currCMD == (byte)'K'))
                                {
                                    tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
                                    ShowTextBoxData(txtCurrentPosX, tmpstring);
                                    tmpstring = string.Format("{0:0.00}", (double)iparam2 / 100.0);
                                    ShowTextBoxData(txtCurrentPosY, tmpstring);
                                    tmpstring = string.Format("{0:0.00}", (double)iparam3 / 100.0);
                                    ShowTextBoxData(txtCurrentPosZ, tmpstring);

                                    ShowLabelData(lblInputPort, iparam4.ToString("X4"));

                                    //Brush backbrushXY1 = ((currMarkInfo.currMarkData.pattern.headValue.home3DPos.X == iparam1) && (currMarkInfo.currMarkData.pattern.headValue.home3DPos.Y == iparam2)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                    Brush backbrushXY1 = (((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.X == (int)(iparam1 / 100)) && ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Y == (int)(iparam2 / 100))) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                    ShowLabelColor(lblMotorHomeXY, null, backbrushXY1);

                                    //Brush backbrushZ1 = (currMarkInfo.currMarkData.pattern.headValue.home3DPos.Z == iparam3) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                    Brush backbrushZ1 = ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Z == (int)(iparam3 / 100)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                    ShowLabelColor(lblMotorHomeZ, null, backbrushZ1);

                                    Brush backAlaramX1 = ((iparam4 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, null, backAlaramX1);
                                    Brush backAlaramY1 = ((iparam4 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, null, backAlaramY1);
                                    Brush backAlaramZ1 = ((iparam4 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, null, backAlaramZ1);

                                    Brush backJogLeft1 = ((iparam4 & 0x08) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXMinus, null, backJogLeft1);
                                    Brush backJogRight1 = ((iparam4 & 0x10) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXPlus, null, backJogRight1);
                                    Brush backJogDown1 = ((iparam4 & 0x20) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYMinus, null, backJogDown1);
                                    Brush backJogUp1 = ((iparam4 & 0x40) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYPlus, null, backJogUp1);
                                    Brush backJogDwonZ1 = ((iparam4 & 0x80) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZMinus, null, backJogDwonZ1);
                                    Brush backJogUpZ1 = ((iparam4 & 0x100) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZPlus, null, backJogUpZ1);

                                    //ShowLog((byte)LOGTYPE.LOG_NORMAL, "1", param1 + "/" + param2 + "/" + param3 + "/" + iparam4, "");

                                    if ((iparam4 & 0x8000) != 0)
                                    {
                                        ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "LASER ERROR!!!!");
                                        ShowButtonColor(btnLaserReset, null, Brushes.Red);
                                    }
                                    else
                                    {
                                        ShowButtonColor(btnLaserReset, null, Brushes.LightGray);
                                    }

                                    if ((iparam4 & 0x07) != 0)
                                    {
                                        ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "MOTOR ERROR!!!!");
                                    }
                                }
                                break;

                            case (byte)'8': // Action OK
                                tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
                                ShowTextBoxData(txtCurrentPosX, tmpstring);
                                tmpstring = string.Format("{0:0.00}", (double)iparam2 / 100.0);
                                ShowTextBoxData(txtCurrentPosY, tmpstring);
                                tmpstring = string.Format("{0:0.00}", (double)iparam3 / 100.0);
                                ShowTextBoxData(txtCurrentPosZ, tmpstring);

                                ShowLabelData(lblInputPort, iparam4.ToString("X4"));

                                //Brush backbrushXY8 = ((currMarkInfo.currMarkData.pattern.headValue.home3DPos.X == iparam1) && (currMarkInfo.currMarkData.pattern.headValue.home3DPos.Y == iparam2)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                Brush backbrushXY8 = (((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.X == (int)(iparam1 / 100)) && ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Y == (int)(iparam2 / 100))) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                ShowLabelColor(lblMotorHomeXY, null, backbrushXY8);

                                //Brush backbrushZ8 = (currMarkInfo.currMarkData.pattern.headValue.home3DPos.Z == iparam3) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                Brush backbrushZ8 = ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Z == (int)(iparam3 / 100)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                ShowLabelColor(lblMotorHomeZ, null, backbrushZ8);

                                Brush backAlaramX8 = ((iparam4 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, null, backAlaramX8);
                                Brush backAlaramY8 = ((iparam4 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, null, backAlaramY8);
                                Brush backAlaramZ8 = ((iparam4 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, null, backAlaramZ8);

                                Brush backJogLeft8 = ((iparam4 & 0x08) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXMinus, null, backJogLeft8);
                                Brush backJogRight8 = ((iparam4 & 0x10) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXPlus, null, backJogRight8);
                                Brush backJogDown8 = ((iparam4 & 0x20) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYMinus, null, backJogDown8);
                                Brush backJogUp8 = ((iparam4 & 0x40) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYPlus, null, backJogUp8);
                                Brush backJogDwonZ8 = ((iparam4 & 0x80) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZMinus, null, backJogDwonZ8);
                                Brush backJogUpZ8 = ((iparam4 & 0x100) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZPlus, null, backJogUpZ8);

                                //ShowLog((byte)LOGTYPE.LOG_NORMAL, "8", param1 + "/" + param2 + "/" + param3 + "/" + iparam4, "");

                                if ((iparam4 & 0x8000) != 0)
                                {
                                    ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "LASER ERROR!!!!");
                                    ShowButtonColor(btnLaserReset, null, Brushes.Red);
                                }
                                else
                                {
                                    ShowButtonColor(btnLaserReset, null, Brushes.LightGray);
                                }

                                if ((iparam4 & 0x07) != 0)
                                {
                                    ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "MOTOR ERROR!!!!");
                                }

                                if ((currCMD == (byte)'R') || (currCMD == (byte)'@'))
                                {
#if MANUAL_MARK
                                    //ShowCurrentStateLabel(5);
                                    ShowCurrentStateLabelManual(4);
                                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
#else
#endif
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[4] : MARKING COMPLETE", Thread.CurrentThread.ManagedThreadId);
                                    ShowLog((byte)LOGTYPE.LOG_SUCCESS, "MARKING", "MARKING COMPLETE!!!!");
                                    m_currCMD = 0;
                                }
                                m_currCMD = 0;
                                break;

                            case (byte)ASCII.ACK:
                                if (e.receiveSize < 10)
                                    break;
                                if ((currCMD == '@') && (currMarkInfo.currMarkData.pattern.laserValue.density == 1))
                                {
                                    Brush backAlaramXA = ((iparam1 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, null, backAlaramXA);
                                    Brush backAlaramYA = ((iparam1 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, null, backAlaramYA);
                                    Brush backAlaramZA = ((iparam1 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, null, backAlaramZA);

                                    Brush backJogLeftA = ((iparam1 & 0x08) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXMinus, null, backJogLeftA);
                                    Brush backJogRightA = ((iparam1 & 0x10) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXPlus, null, backJogRightA);
                                    Brush backJogDownA = ((iparam1 & 0x20) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYMinus, null, backJogDownA);
                                    Brush backJogUpA = ((iparam1 & 0x40) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYPlus, null, backJogUpA);
                                    Brush backJogDwonZA = ((iparam1 & 0x80) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZMinus, null, backJogDwonZA);
                                    Brush backJogUpZA = ((iparam1 & 0x100) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZPlus, null, backJogUpZA);

                                    //ShowLog((byte)LOGTYPE.LOG_NORMAL, "ACK", param1 + "/" + param2 + "/" + param3 + "/" + iparam4, "");

                                    if ((iparam1 & 0x8000) != 0)
                                    {
                                        ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "LASER ERROR!!!!");
                                        ShowButtonColor(btnLaserReset, null, Brushes.Red);
                                    }
                                    else
                                    {
                                        ShowButtonColor(btnLaserReset, null, Brushes.LightGray);
                                    }

                                    if ((iparam1 & 0x07) != 0)
                                    {
                                        ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "MOTOR ERROR!!!!");
                                    }

                                    ShowLabelData(lblInputPort, iparam1.ToString("X4"));
                                }
                                break;

                            default:
                                break;
                        }
                        break;
                    case (byte)'V':     // Return value : Version Number
                    case (byte)'I':     // Return value : Input Port
                    case (byte)'o':     // Return value : Output Port
                                        //byte[] retBytes = new byte[4];
                                        //Array.Copy(buffer, 3, retBytes, 0, 4);
                                        //string retstr = Encoding.Default.GetString(retBytes);
                                        //retval = int.Parse(retstr, System.Globalization.NumberStyles.HexNumber);

                        ShowLabelData(lblInputPort, iparam1.ToString("X4"));

                        //exitFlag = true;
                        break;
//                    case (byte)'h':         //
//                        switch (e.stscmd)
//                        {
//                            case (byte)'0': // DONE POINT 
//                                tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
//                                ShowTextBoxData(txtCurrentPosScan, tmpstring);
//                                break;

//                            case (byte)'1': // RUNNING
//                                tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
//                                ShowTextBoxData(txtCurrentPosScan, tmpstring);
//                                break;

//                            case (byte)'8': // Action OK
//                                tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
//                                ShowTextBoxData(txtCurrentPosScan, tmpstring);

//                                ShowLabelData(lblInputPort, iparam4.ToString("X4"));

//#if MANUAL_MARK
//                                    //ShowCurrentStateLabel(5);
//                                    ShowCurrentStateLabelManual(4);
//                                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
//                                    DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
//#else
//#endif
//                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[4] : MARKING COMPLETE", Thread.CurrentThread.ManagedThreadId);
//                                ShowLog((byte)LOGTYPE.LOG_SUCCESS, "스캐너 홈 이동", "MOVE TO SCAN HOME COMPLETE!!!!", "");
//                                m_currCMD = 0;
//                                break;

//                            default:
//                                break;
//                        }
//                        break;
                    default:
                        if ((e.stscmd == (byte)ASCII.ACK) && (e.receiveSize >= 10))
                        {
                            ////Brush backbrushXYD = ((currMarkInfo.currMarkData.pattern.headValue.home3DPos.X == iparam1) && (currMarkInfo.currMarkData.pattern.headValue.home3DPos.Y == iparam2)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //Brush backbrushXYD = (((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.X == (int)(iparam1 / 100)) && ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Y == (int)(iparam2 / 100))) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //ShowLabelColor(lblMotorHomeXY, null, backbrushXYD);

                            ////Brush backbrushZD = (currMarkInfo.currMarkData.pattern.headValue.home3DPos.Z == iparam3) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //Brush backbrushZD = ((int)currMarkInfo.currMarkData.pattern.headValue.home3DPos.Z == (int)(iparam1 / 100)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //ShowLabelColor(lblMotorHomeZ, null, backbrushZD);

                            Brush backAlaramXD = ((iparam1 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, null, backAlaramXD);
                            Brush backAlaramYD = ((iparam1 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, null, backAlaramYD);
                            Brush backAlaramZD = ((iparam1 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, null, backAlaramZD);

                            Brush backJogLeftD = ((iparam1 & 0x08) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXMinus, null, backJogLeftD);
                            Brush backJogRightD = ((iparam1 & 0x10) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveXPlus, null, backJogRightD);
                            Brush backJogDownD = ((iparam1 & 0x20) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYMinus, null, backJogDownD);
                            Brush backJogUpD = ((iparam1 & 0x40) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveYPlus, null, backJogUpD);
                            Brush backJogDwonZD = ((iparam1 & 0x80) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZMinus, null, backJogDwonZD);
                            Brush backJogUpZD = ((iparam1 & 0x100) != 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowButtonColor(btnMoveZPlus, null, backJogUpZD);

                            //ShowLog((byte)LOGTYPE.LOG_NORMAL, "D", param1 + "/" + param2 + "/" + param3 + "/" + iparam4, "");

                            if ((iparam1 & 0x8000) != 0)
                            {
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "", "LASER ERROR!!!!");
                                ShowButtonColor(btnLaserReset, null, Brushes.Red);
                            }
                            else
                            {
                                ShowButtonColor(btnLaserReset, null, Brushes.LightGray);
                            }

                            if ((iparam1 & 0x07) != 0)
                            {
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, "MARKING", "", "MOTOR ERROR!!!!");
                            }

                            ShowLabelData(lblInputPort, iparam1.ToString("X4"));

                            //byte[] ackBytes = new byte[4];
                            //Array.Copy(buffer, 3, ackBytes, 0, 4);
                            //string ackstr = Encoding.Default.GetString(ackBytes);
                            //retval = int.Parse(ackstr, System.Globalization.NumberStyles.HexNumber);

                        }
                        //ControlWindow.Dispatcher.Invoke(new Action(delegate
                        //{
                        //    ControlWindow.M4_CommStatus.Background = Brushes.WhiteSmoke;
                        //    ControlWindow.X_AXIS_ALARM.Background = ((retval & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                        //    ControlWindow.Y_AXIS_ALARM.Background = ((retval & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                        //    ControlWindow.Z_AXIS_ALARM.Background = ((retval & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                        //    ControlWindow.InputTxt.Text = retval.ToString("X4");
                        //}));

                        //exitFlag = true;
                        break;
                } // switch
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<List<string>> DirFileSearch(string selpath, string file)
        {
            List<string> names = new List<string>();
            string fileFullName = "";
            VinNoInfo vin = new VinNoInfo();

            try
            {
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

        private void ShowLog(byte flag, string cmd, string logmsg="", string error="")
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowLog";
            string trace = "";
            DateTime dt = DateTime.Now;

            try
            {
                trace = dt.ToString("yyyy-MM-dd HH:mm:ss    ");
                if(cmd.Length > 0)
                    trace += "[" + cmd + "] ";
                switch(flag)
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
                    if(lsbResult.Items.Count > 1000)
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

        private void cbxPatternList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string name = cbxPatternList.SelectedItem.ToString();
            //string patternname = Constants.PATTERN_PATH + name + ".ini";
            string className = "SetControllerWindow2";
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

                currMarkInfo.currMarkData.pattern = (PatternValueEx)newpattern.Clone();

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

                DisplayPatternValue(bHeadType, newpattern);
                txtAreaPosition.Text = AreaPosition;
                orgPattern = (PatternValueEx)newpattern.Clone();
                orgPattern.name = patname;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private bool CheckSavePatternData(string patternName)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "CheckSavePatternData";// MethodBase.GetCurrentMethod().Name;

            bool ret = false;
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                msg2.Message = patternName + " : 패턴을 저장하시겠습니까?";
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

        private async void btnServoOnOff_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnServoOnOff_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            double dHomeX = 0;
            double dHomeY = 0;
            double dHomeZ = 0;
            short sHomeX = 0;
            short sHomeY = 0;
            short sHomeZ = 0;

            double dParkX = 0;
            double dParkY = 0;
            double dParkZ = 0;
            short sParkX = 0;
            short sParkY = 0;
            short sParkZ = 0;

            short stepLength = 0;
            bool onoff = false;
            string cmd = "SERVO ON";

            try
            {
                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                if (btnServoOnOff.Content.ToString() == "SERVO ON")
                    onoff = true;
                else
                    onoff = false;

                cmd = btnServoOnOff.Content.ToString();

                if (onoff)
                {
                    ShowLog((byte)LOGTYPE.LOG_START, cmd, "", "");

                    short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                    if (stepLength <= 0)
                        stepLength = 100;

                    if (txtHome_X.Text.Length <= 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER OFFSET X value");
                        return;
                    }

                    if (txtHome_Y.Text.Length <= 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER OFFSET Y value");
                        return;
                    }

                    if (txtHome_Z.Text.Length <= 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER OFFSET Z value");
                        return;
                    }

                    if (txtPark_X.Text.Length <= 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER Park X value");
                        return;
                    }

                    if (txtPark_Y.Text.Length <= 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER Park Y value");
                        return;
                    }

                    if (txtPark_Z.Text.Length <= 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER Park Z value");
                        return;
                    }

                    double.TryParse(txtHome_X.Text, out dHomeX);
                    dHomeX *= stepLength;
                    sHomeX = (short)(dHomeX + 0.5);

                    double.TryParse(txtHome_Y.Text, out dHomeY);
                    dHomeY *= stepLength;
                    sHomeY = (short)(dHomeY + 0.5);

                    double.TryParse(txtHome_Z.Text, out dHomeZ);
                    dHomeZ *= stepLength;
                    sHomeZ = (short)(dHomeZ + 0.5);

                    double.TryParse(txtPark_X.Text, out dParkX);
                    dParkX *= stepLength;
                    sParkX = (short)(dParkX + 0.5);

                    double.TryParse(txtPark_Y.Text, out dParkY);
                    dParkY *= stepLength;
                    sParkY = (short)(dParkY + 0.5);

                    double.TryParse(txtPark_Z.Text, out dParkZ);
                    dParkZ *= stepLength;
                    sParkZ = (short)(dParkZ + 0.5);

                    m_currCMD = (byte)'O';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(12, true);
                    if (retval.execResult != 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                        return;
                    }
                    Thread.Sleep(500);

                    retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.HomeMoving);
                    if (retval.execResult != 0)
                    {
                        return;
                    }

                    m_currCMD = (byte)'H';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoHome(sHomeX, sHomeY, sHomeZ);
                    if (retval.execResult != 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                        return;
                    }

                    retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                    if (retval.execResult != 0)
                    {
                        return;
                    }

                    m_currCMD = (byte)'K';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(sParkX, sParkY, sParkZ);
                    if (retval.execResult != 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                        return;
                    }
                    btnServoOnOff.Content = "SERVO OFF";
                    ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                }
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                    m_currCMD = (byte)'O';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(12, false);
                    if (retval.execResult != 0)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                        return;
                    }
                    btnServoOnOff.Content = "SERVO ON";
                    ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                }
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("SERVO ON/OFF EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnGoPark_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnGoPark_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            short stepLength = 0;
            short sParkX = 0;
            short sParkY = 0;
            short sParkZ = 0;
            PositionValue posvalue = new PositionValue();
            HeadValue headvalue = new HeadValue();
            string cmd = "GO TO PARK POSITION";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                ReadPatternPositionValue(ref posvalue);
                ReadPatternHeadValue(ref headvalue);

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                sParkX = (short)(headvalue.park3DPos.X * stepLength + 0.5);
                sParkY = (short)(headvalue.park3DPos.Y * stepLength + 0.5);
                sParkZ = (short)(headvalue.park3DPos.Z * stepLength + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }
                m_currCMD = (byte)'K';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(sParkX, sParkY, sParkZ);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnGoHome_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnGoHome_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //double doffsetX = 0;
            //double doffsetY = 0;
            //double doffsetZ = 0;
            short sHomeX = 0;
            short sHomeY = 0;
            short sHomeZ = 0;
            short stepLength = 0;
            PositionValue posvalue = new PositionValue();
            HeadValue headvalue = new HeadValue();
            //short initspeed, targetspeed, accel, decel;
            string cmd = "GO HOME POSITION";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                ReadPatternPositionValue(ref posvalue);
                ReadPatternHeadValue(ref headvalue);

                //sHomeX = (short)((posvalue.home3DPos.X + 0.5) * stepLength);
                //sHomeY = (short)((posvalue.home3DPos.Y + 0.5) * stepLength);
                //sHomeZ = (short)((posvalue.home3DPos.Z + 0.5) * stepLength);

                m_currCMD = (byte)'W';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SetWorkArea(headvalue.max_X * headvalue.stepLength, headvalue.max_Y * headvalue.stepLength, headvalue.max_Z * headvalue.stepLength);
                if (retval.execResult != 0)
                {
                    m_currCMD = 0;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SetWorkArea ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                sHomeX = (short)(headvalue.home3DPos.X * stepLength);
                sHomeY = (short)(headvalue.home3DPos.Y * stepLength);
                sHomeZ = (short)(headvalue.home3DPos.Z * stepLength);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.HomeMoving);
                if (retval.execResult != 0)
                {
                    //ShowLog("GO HOME ERROR - MOTOR SPEED ERROR");
                    return;
                }

                m_currCMD = (byte)'H';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoHome(sHomeX, sHomeY, sHomeZ);
                if (retval.execResult != 0)
                {
                    //ShowLog("GO HOME ERROR (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnCenterPoint_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnCenterPoint_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short stepLength = 0;

            double dstartX = 0;
            double dstartY = 0;
            double dstartZ = 0;
            short sstartX = 0;
            short sstartY = 0;
            short sstartZ = 0;
            string cmd = "GO TO CENTER POINT";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                if (txtCenter_X.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [CENTER POSITION X] VALUE");
                    return;
                }

                if (txtCenter_Y.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [CENTER POSITION Y] VALUE");
                    return;
                }

                if (txtCenter_Z.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [CENTER POSITION Z] VALUE");
                    return;
                }

                double.TryParse(txtCenter_X.Text, out dstartX);
                if (dstartX < 0)
                    dstartX = 70;
                sstartX = (short)(dstartX * stepLength + 0.5);

                double.TryParse(txtCenter_Y.Text, out dstartY);
                if (dstartY < 0)
                    dstartY = 20;
                sstartY = (short)(dstartY * stepLength + 0.5);

                double.TryParse(txtCenter_Z.Text, out dstartZ);
                if (dstartZ <= 0)
                    dstartZ = 26;
                sstartZ = (short)(dstartZ * stepLength + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                m_currCMD = (byte)'M';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoStartPoint2(sstartX, sstartY, sstartZ);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    //ShowLog("GO CENTER POINT - ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                //ShowLog("GO CENTER POINT - SUCCESS");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("GO CENTER POINT - EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnDryRun_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnDryRun_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            short stepLength = 0;
            string cmd = "DRY MARKING";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.MarkMoving);
                if (retval.execResult != 0)
                {
                    //ShowLog("DRY RUN : SET MOTOR SPEED ERROR (" + retval.execResult.ToString() + ")");
                    return;
                }

                m_currCMD = (byte)'R';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart(1);
                if (retval.execResult != 0)
                {
                    //ShowLog("DRY RUN ERROR (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnAreaCheck_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnAreaCheck_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string log = "";
            Stopwatch sw = new Stopwatch();
            //int repeatCount = 0;
            CheckAreaData chkdata = new CheckAreaData();
            string patName = "";
            string cmd = "AREA CHECK";
            byte errHeight = 0;
            byte errCline = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if (cbxPatternList.SelectedIndex < 0)
                    patName = "Pattern_DEFAULT";
                else
                    patName = cbxPatternList.Text;

                currMarkInfo.checkdata.Clear();
                retval.execResult = ReadFontData(cmd, patName);
                if (retval.execResult != 0)
                {
                    log = "READ FONT ERROR : (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (ckbErrHeight.IsChecked == false)
                    errHeight = 0;
                else
                    errHeight = 1;

                if (ckbErrCline.IsChecked == false)
                    errCline = 0;
                else
                    errCline = 1;

                sw.Start();
                while (true)
                {

                    chkdata = await Range_Test(cmd, currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern, errHeight, errCline);
                    if (chkdata.execResult != 0)
                    {
                        log = "RANGE TEST ERROR : (RESULT = " + chkdata.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    else
                    {
                        currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                    }

                    if (ckbRepeatCheck.IsChecked == false)
                        break;
                    //repeatCount--;
                    await Task.Delay(500);
                }

                log = "MEASURE TIME : " + sw.ElapsedMilliseconds.ToString();
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);

                currMarkInfo.checkdata.bReady = true;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK PLATE - SUCCESS", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnCalculateData_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnCalculateData_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string patName = "";
            PatternValueEx pattern = new PatternValueEx();
            string value = "";
            byte bHeadType = 0;
            string log = "";
            string strvin = "";
            string cmd = "CALCULATE DATA";

            try
            {
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                strvin = txtVIN.Text;
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                if (strvin.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "VIN IS BLANK. ENTER VIN");
                    //ShowLog("SEND FONT DATA - VIN EMPTY");
                    return;
                }

                if (cbxPatternList.SelectedIndex >= 0)
                    patName = cbxPatternList.SelectedItem.ToString();
                else
                    patName = "Pattern_DEFAULT";

                ReadPatternValue(ref pattern);
                //ImageProcessManager.GetPatternValue(patName, bHeadType, ref pattern);

                retval = await Start_TEXT2(cmd, txtVIN.Text, pattern);
            }
            catch (Exception ex)
            {
                //log = "";
                //log = string.Format("SEND FONT DATA EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //ShowLog(log);
            }
        }

        private async void btnMarkRun_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMarkRun_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            LASERSTATUS Status = 0;
            //string vin = "";
            string value = "";
            //string fName = "";
            //byte bHeadType = 0;
            string log = "";
            PositionValue posValue = new PositionValue();
            HeadValue headValue = new HeadValue();
            int repeatCount = 0;
            string patName = "";
            string cmd = "MARKING";
            bool ret = false;
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            Stopwatch swMark = new Stopwatch();
            Stopwatch swLaser = new Stopwatch();
            Stopwatch swClean = new Stopwatch();

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                if(EmissionAuto.IsChecked == true)
                {
                    msg2.Message = "레이저 EMISSION이 켜져 있습니다.";
                    msg2.Fontsize = 20;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Red;
                    msg2.Background = Brushes.White;
                }
                else
                {
                    msg2.Message = "레이저 EMISSION이 꺼져 있습니다.";
                    msg2.Fontsize = 20;
                    msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg2.VerticalContentAlignment = VerticalAlignment.Center;
                    msg2.Foreground = Brushes.Red;
                    msg2.Background = Brushes.White;
                }

                msg3.Message = "레이저 각인을 실행하시겠습니까?";
                msg3.Fontsize = 20;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Red;
                msg3.Background = Brushes.White;

                WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ret = warning.ShowDialog().Value;

                if(ret == false)
                {
                    return;
                }

                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                int.TryParse(tbxRunRepeatCount.Text, out repeatCount);
                if (repeatCount <= 0)
                    repeatCount = 1;

                if(AirOnOff.IsChecked == true)
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                    if (retval.execResult != 0)
                    {
                        log = "SendAirAsync ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                }

                while (repeatCount > 0)
                {
                    swMark.Start();

                    if (cbxPatternList.SelectedIndex < 0)
                        patName = "Pattern_DEFAULT";
                    else
                        patName = cbxPatternList.Text;

                    if (bReadFontValue == false)
                    {
                        retval.execResult = ReadFontData(cmd, patName);
                        if(retval.execResult != 0)
                        {
                            log = "READ FONT ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                            return;
                        }
                        await ShowCurrentMarkingInformation(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2);
                        GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);
                        bReadFontValue = true;
                    }

                    //1. Aiming Beam OFF
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                    }
                    if (retval.execResult != 0)
                    {
                        log = "AimingBeamOFF. (RESULT = " + retval.execResult.ToString() + ")";
                        //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        //await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        //return;
                    }
                    ShowRectangle(AimingLamp, Brushes.Black);

                    //2.read laser status
                    //ShowLog("MARKING - READ LASER STATUS");
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                    }
                    if (retval.execResult != 0)
                    {
                        log = "READ LASER STATUS. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_START, cmd, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    string[] st = retval.recvString.Split(':');
                    if (st.Length < 2)
                    {
                        log = "READ LASER STATUS. (STATUS STRING)";
                        ShowLog((byte)LOGTYPE.LOG_START, cmd, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                    //2-1. Check emission status
                    Status = (LASERSTATUS)UInt32.Parse(st[1]);
                    if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                    {
                        //ShowLog("MARKING - STOP EMISSION");
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        }
                        if (retval.execResult != 0)
                        {
                            log = "STOP EMISSION. (RESULT = " + retval.execResult.ToString() + ")";
                            ShowLog((byte)LOGTYPE.LOG_START, cmd, log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                            return;
                        }
                        ShowRectangle(EmissionLamp, Brushes.Black);
                        //EmissionLamp.Fill = Brushes.Black;
                    }

                    //3. load waveform profile number
                    // ShowLog("MARKING - SELECT PROFILE");
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString());
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString());
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString());
                    }
                    if (retval.execResult != 0)
                    {
                        log = "SELECT PROFILE. (" + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    string[] prsel = retval.recvString.Split('[', ']');
                    if (prsel.Length < 2)
                    {
                        log = "SELECT PROFILE. (PROFILE STRING)";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    if (prsel[0] != "PRSEL: ")
                    {
                        log = "SELECT PROFILE. (PROFILE SETTING RESPONSE ERROR)";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    string[] sel = prsel[1].Split(':');
                    if (currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString() != sel[0])
                    {
                        log = "SELECT PROFILE. (PROFILE SETTING ERROR)";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    //4. get waveform mode
                    //ShowLog("MARKING - CONFIG WAVEFORM MODE");
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ConfigWaveformMode(0);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ConfigWaveformMode(0);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ConfigWaveformMode(0);
                    }
                    if (retval.execResult != 0)
                    {
                        log = "CONFIG WAVEFORM MODE. (" + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    string[] pcfg = retval.recvString.Split('[', ']');
                    if (pcfg.Length < 2)
                    {
                        log = "CONFIG WAVEFORM MODE. (PROFILE STRING)";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    //5. Clear Dispaly
                    //ClearMarkVINDisplay();

                    //6. Start Text
                    retval = await Start_TEXT2(cmd, currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern);
                    if (retval.execResult != 0)
                    {
                        log = "Start_TEXT3. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    //posValue = (PositionValue)currMarkInfo.currMarkData.pattern.positionValue.Clone();
                    //headValue = (headValue)currMarkInfo.currMarkData.pattern.positionValue.Clone();
                    ReadPatternPositionValue(ref posValue);
                    ReadPatternHeadValue(ref headValue);

                    //5. Set Font Data Buffer Flush
                    //ShowLog("MARKING - FLUSH START");
                    m_currCMD = (byte)'B';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.FontFlush();
                    if (retval.execResult != 0)
                    {
                        log = "FONT FLUSH. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                    //ShowLog("MARKING - FONT FLUSH SUCCESS");

                    //if(pattern.laserValue.density == 1)
                    {
                        //ShowLog("MARKING - SET PHASE COMPENSATION");
                        Single pc = Convert.ToSingle(currMarkInfo.currMarkData.pattern.laserValue.sPhaseComp);
                        //pc = pattern.laserValue.phaseComp;
                        //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SetPhaseComp(currMarkInfo.currMarkData.pattern.laserValue.phaseComp);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SetPhaseComp(pc);
                        if (retval.execResult != 0)
                        {
                            log = "SET PHASE COMPENSATION. (RESULT = " + retval.execResult.ToString() + ")";
                            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                            return;
                        }
                    }

                    //currMarkInfo.senddata.SendDataIndex = 0;
                    //markdata.EndOfSend = false;
                    currMarkInfo.senddata.CleanFireFlag = false;    // Fire sending
                    //currMarkInfo.senddata.SendDataCount = (short)currMarkInfo.senddata.sendDataFire.Count;
                    string StPoint = currMarkInfo.senddata.sendDataFire.ElementAt(0);

                    short posX = 0;
                    short posY = 0;
                    short posZ = 0;
                    m_currCMD = (byte)'K';
                    value = StPoint.Substring(4, 4);
                    short.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out posX);

                    value = StPoint.Substring(8, 4);
                    short.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out posY);

                    value = StPoint.Substring(12, 4);
                    short.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out posZ);
                    posZ += 500;

                    //7. Go to parking point (Check Point)
                    //ShowLog("MARKING - LOAD SPEED");

                    retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                    if (retval.execResult != 0)
                    {
                        log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    //StPoint.Substring(4, 4 + 4);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(posX, posY, posZ);
                    if (retval.execResult != 0)
                    {
                        log = "GO PARK. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }


                    //mark.Density("N", Density.ToString("X4"));
                    //ShowLog("MARKING - SET DENSITY");
                    m_currCMD = (byte)'N';
                    //Density232 = (short)currMarkInfo.currMarkData.pattern.laserValue.density;
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SetDensity((short)currMarkInfo.currMarkData.pattern.laserValue.density);
                    if (retval.execResult != 0)
                    {
                        log = "SET DENSITY. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.MarkMoving);
                    if (retval.execResult != 0)
                    {
                        log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    // Run Marking
                    //Stopwatch sw = Stopwatch.StartNew();

                    swLaser.Start();

                    if ((bool)EmissionAuto.IsChecked)
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                        }
                        ShowRectangle(EmissionLamp, Brushes.Red);
                        //EmissionLamp.Fill = Brushes.Red;
                    }

                    //Marking Start
                    //ShowLog("MARKING - START MARKING");

                    m_currCMD = (byte)'d';
                    short dwelltime = 0;
                    if(rbtMarkingPos1.IsChecked == true)
                        short.TryParse(txtMarkDelayTime1.Text, out dwelltime);
                    else
                        short.TryParse(txtMarkDelayTime2.Text, out dwelltime);

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(dwelltime);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("dwellTimeSet ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }

                    m_currCMD = (byte)'@';
                    //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(StPoint);
                    Util.GetPrivateProfileValue("OPTION", "MARKINGLOGLEVEL", "0", ref value, Constants.PARAMS_INI_FILE);
                    byte logLevel = 0;
                    byte.TryParse(value, out logLevel);

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(currMarkInfo, false, logLevel);
                    if (retval.execResult != 0)
                    {
                        log = "MARKING ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    bReadFontValue = false;
                    currMarkInfo.checkdata.bReady = false;
                    currMarkInfo.senddata.bReady = false;
                    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                    }
                    if (retval.execResult != 0)
                    {
                        log = "ReadDeviceStatus. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                    else
                    {
                        swClean.Start();
                        st = retval.recvString.Split(':');
                        Status = (LASERSTATUS)UInt32.Parse(st[1]);

                        //if ((bool)CleaningBox.IsChecked && !currMarkInfo.checkdata.ErrorDistanceSensor)
                        if ((bool)CleaningBox.IsChecked && !currMarkInfo.checkdata.ErrorDistanceSensor && (currMarkInfo.currMarkData.pattern.laserValue.combineFireClean == 0))
                        {
                            //ShowLog("MARKING - START CLEANING");

                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            {
                                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                            }
                            if (retval.execResult != 0)
                            {
                                log = "StopEmission. (RESULT = " + retval.execResult.ToString() + ")";
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                                await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                                return;
                            }
                            ShowRectangle(EmissionLamp, Brushes.Black);

                            //Util.GetPrivateProfileValue("VINDATA", "PROFILECLEAN", "0", ref value, "Parameter.ini");                 // load waveform profile number
                            value = currMarkInfo.currMarkData.pattern.laserValue.waveformClean.ToString();
                            ShowTextBoxData(txtCurrProfile, value);
                            //txtCurrProfile.Text = value;
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(value);
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            {
                                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(value);
                                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(value);
                            }
                            prsel = retval.recvString.Split('[', ']');
                            if (prsel[0] != "PRSEL: ")
                            {
                                log = "Profile setting Error2!. (PRSEL[0] = " + prsel[0] + ")";
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                                await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                                return;
                            }
                            sel = prsel[1].Split(':');
                            if (value != sel[0])
                            {
                                log = "Profile setting Error!. (SEL[0] = " + sel[0] + ")";
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                                await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                                return;
                            }

                            if ((bool)EmissionAuto.IsChecked)
                            {
                                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                                ShowRectangle(EmissionLamp, Brushes.Red);
                                //EmissionLamp.Fill = Brushes.Red;
                            }

                            //currMarkInfo.senddata.SendDataIndex = 0;
                            currMarkInfo.senddata.CleanFireFlag = true;    // Clean sending
                                                                           //currMarkInfo.senddata.SendDataCount = (short)currMarkInfo.senddata.sendDataClean.Count;

                            //StPoint = currMarkInfo.senddata.sendDataClean.ElementAt(currMarkInfo.senddata.SendDataIndex++);
                            //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(StPoint);
                            Util.GetPrivateProfileValue("OPTION", "MARKINGLOGLEVEL", "0", ref value, Constants.PARAMS_INI_FILE);
                            logLevel = 0;
                            byte.TryParse(value, out logLevel);

                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(currMarkInfo, true, logLevel);
                            if (retval.execResult != 0)
                            {
                                log = "RUN CLEANING. (RESULT = " + retval.execResult.ToString() + ")";
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                                await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                                return;
                            }

                            swClean.Stop();
                            log = "CLEAN TIME : " + swClean.ElapsedMilliseconds.ToString();
                            swClean.Reset();
                            ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                        }

                        if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                        {
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                            ShowRectangle(EmissionLamp, Brushes.Black);
                            //EmissionLamp.Fill = Brushes.Black;
                        }
                    }

                    retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                    if (retval.execResult != 0)
                    {
                        log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    short stepLeng = currMarkInfo.currMarkData.pattern.headValue.stepLength;
                    if (stepLeng <= 0)
                        stepLeng = 100;

                    posX = (short)(headValue.park3DPos.X * stepLeng); if (posX <= 0) posX = (short)(70 * stepLeng);
                    posY = (short)(headValue.park3DPos.Y * stepLeng); if (posY <= 0) posY = (short)(20 * stepLeng);
                    posZ = (short)(headValue.park3DPos.Z * stepLeng); if (posZ <= 0) posZ = (short)(110 * stepLeng);
                    m_currCMD = (byte)'K';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(posX, posY, posZ);
                    if (retval.execResult != 0)
                    {
                        log = "GO PARK. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    //Debug.WriteLine("Running Time : " + sw.Elapsed);
                    //sw.Stop();
                    //log = "TOTAL MARKING TIME : " + sw.ElapsedMilliseconds.ToString();
                    //ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);

                    swMark.Stop();
                    swLaser.Stop();
                    log = "MARKING TIME : " + swMark.ElapsedMilliseconds.ToString();
                    ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                    log = "LASER TIME : " + swLaser.ElapsedMilliseconds.ToString();
                    ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                    swMark.Reset();
                    swLaser.Reset();

                    ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Running Time : " + sw.Elapsed.ToString(), Thread.CurrentThread.ManagedThreadId);

                    repeatCount--;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                if (retval.execResult != 0)
                {
                    log = "SendAirAsync ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Btn_Start_Click = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
            }
        }


        private async void btnGoPoint_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnGoPoint_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //short stepLength = 0;
            double dposX = 0.0d;
            double dposY = 0.0d;
            double dposZ = 0.0d;
            short sposX = 0;
            short sposY = 0;
            short sposZ = 0;
            string cmd = "GO TO POINT";
            HeadValue headValue = new HeadValue();

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                ReadPatternHeadValue(ref headValue);

                //short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                //if (stepLength <= 0)
                //    stepLength = 100;


                if (txtGoPoint_X.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [X] VALUE");
                    return;
                }

                if (txtGoPoint_Y.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [Y] VALUE");
                    return;
                }

                if (txtGoPoint_Z.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [Z] VALUE");
                    return;
                }

                double.TryParse(txtGoPoint_X.Text, out dposX);
                sposX = (short)(dposX * headValue.stepLength + 0.5);

                double.TryParse(txtGoPoint_Y.Text, out dposY);
                sposY = (short)(dposY * headValue.stepLength + 0.5);

                double.TryParse(txtGoPoint_Z.Text, out dposZ);
                sposZ = (short)(dposZ * headValue.stepLength + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SEND MOTOR SPEED ERROR : " + retval.execResult.ToString());
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(sposX, sposY, sposZ, 0);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GO POINT ERROR : " + retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("GO POINT EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSetStartPoint_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnSetStartPoint_Click";// MethodBase.GetCurrentMethod().Name;
            double dposX = 0.0d;
            double dposY = 0.0d;
            double dposZ = 0.0d;
            short sposX = 0;
            short sposY = 0;
            short sposZ = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string patternName = "";
            string patternFileName;
            //short stepLength = 0;
            string cmd = "마킹 시작 좌표 설정";
            HeadValue headValue = new HeadValue();
            string value = "";
            byte bHeadType = 0;

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "START");

                //short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                //if (stepLength <= 0)
                //    stepLength = 100;

                ReadPatternHeadValue(ref headValue);

                if (txtGoPoint_X.Text.Length > 0)
                {
                    double.TryParse(txtHome_X.Text, out dposX);
                    //sposX = (short)(dposX + 0.5);
                    //sposX = (short)(dposX + 0.5);
                }
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [X] VALUE");
                    return;
                }

                if (txtGoPoint_Y.Text.Length > 0)
                {
                    double.TryParse(txtHome_Y.Text, out dposY);
                    //sposY = (short)(dposY + 0.5);
                }
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [Y] VALUE");
                    return;
                }

                if (txtGoPoint_Z.Text.Length > 0)
                {
                    double.TryParse(txtHome_Z.Text, out dposZ);
                    //dposZ *= headValue.stepLength;
                    //sposZ = (short)(dposZ + 0.5);
                }
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "ENTER [Z] VALUE");
                    return;
                }

                if ((cbxPatternList.Items.Count > 0) && (cbxPatternList.SelectedIndex >= 0))
                {
                    patternName = cbxPatternList.SelectedItem.ToString();
                    patternFileName = Constants.PATTERN_PATH + patternName + ".ini";

                    Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out bHeadType);

                    if (bHeadType == 0)
                    {
                        Util.WritePrivateProfileValue("FONT", "STARTPOSX", dposX.ToString("F2"), patternFileName);
                        Util.WritePrivateProfileValue("FONT", "STARTPOSY", dposY.ToString("F2"), patternFileName);
                        Util.WritePrivateProfileValue("FONT", "STARTPOSZ", dposZ.ToString("F2"), patternFileName);
                    }
                    else
                    {
                        Util.WritePrivateProfileValue("POSITION", "STARTPOSX", dposX.ToString("F2"), patternFileName);
                        Util.WritePrivateProfileValue("POSITION", "STARTPOSY", dposY.ToString("F2"), patternFileName);
                        Util.WritePrivateProfileValue("POSITION", "STARTPOSZ", dposZ.ToString("F2"), patternFileName);
                    }
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "SET START POINT SUCCESS");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnMoveXMinus_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMoveXMinus_Click";// MethodBase.GetCurrentMethod().Name;
            short stepLength = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short distance = 0;
            string cmd = "MOVE X-";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "MOVE X- START");

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

                distance = (short)(GetDistanceXY() * (-1) * stepLength / 10);
//                distance *= stepLength;

                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(distance, 0, 0);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "MOVE X- SUCCESS");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("MOVE X- EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnMoveXPlus_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMoveXPlus_Click";// MethodBase.GetCurrentMethod().Name;

            short stepLength = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            short distance = 0;
            string cmd = "MOVE X+";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "MOVE X+ START");

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

                distance = (short)(GetDistanceXY() * stepLength / 10);
                //distance *= stepLength;

                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(distance, 0, 0);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "MOVE X+ SUCCESS");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("MOVE X+ EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnMoveZMinus_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMoveZMinus_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            short distance = 0;
            short stepLength = 0;
            string cmd = "MOVE Z-";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "MOVE Z- START");
                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                //distance = GetDistanceXY();
                distance = (short)(GetDistanceXY() * (-1) * stepLength / 10);
                //distance = (short)(GetDistanceXY() * (-1));
                //distance *= stepLength;

                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(0, 0, distance);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "MOVE Z- SUCCESS");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private async void btnMoveZHome_Click(object sender, RoutedEventArgs e)
        //{
        //    string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "btnMoveZHome_Click";// MethodBase.GetCurrentMethod().Name;
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    double doffsetZ = 0.0d;
        //    short soffsetZ = 0;
        //    short stepLength = 0;
        //    string cmd = "MOVE SCANNER HOME";

        //    try
        //    {
        //        ShowLog((byte)LOGTYPE.LOG_START, cmd, "GO Z HOME START");
        //        short.TryParse(lblStepLength.Content.ToString(), out stepLength);
        //        if (stepLength <= 0)
        //            stepLength = 100;

        //        stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
        //        if (txtOffset_Z.Text.Length > 0)
        //        {
        //            double.TryParse(txtOffset_Z.Text, out doffsetZ);
        //            doffsetZ *= stepLength;
        //            soffsetZ = (short)(doffsetZ * stepLength + 0.5);
        //        }
        //        else
        //        {
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "ENTER SCANER OFFSET value");
        //            return;
        //        }

        //        retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.HomeMoving);
        //        if (retval.execResult != 0)
        //        {
        //            return;
        //        }


        //        m_currCMD = (byte)'h';
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoHome_Z(soffsetZ);
        //        if (retval.execResult != 0)
        //        {
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
        //            return;
        //        }
        //        ShowLog((byte)LOGTYPE.LOG_END, cmd, "GO Z HOME SUCCESS");
        //    }
        //    catch (Exception ex)
        //    {
        //        //string log = "";
        //        //log = string.Format("GO Z HOME EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
        //        //ShowLog(log);
        //        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private async void btnMoveZPlus_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMoveZPlus_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            short distance = 0;
            short stepLength = 0;
            string cmd = "MOVE Z+";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "MOVE Z+ START");
                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                //distance = (short)(GetDistanceZ() * stepLength);
                distance = (short)(GetDistanceXY() * stepLength / 10);
                //distance = (short)(GetDistanceXY());
                //distance *= stepLength;

                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(0, 0, distance);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "MOVE Z+ SUCCESS");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowTextBoxData(TextBox tbx, string data)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowTextBoxData";

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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowButtonColor(Button btn, Brush fore, Brush back)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowButtonColor";

            try
            {
                if (btn == null)
                    return;
                if (btn.CheckAccess())
                {
                    if (fore != null)
                        btn.Foreground = fore;

                    if (back != null)
                        btn.Background = back;
                }
                else
                {
                    btn.Dispatcher.Invoke(new Action(delegate
                    {
                        if (fore != null)
                            btn.Foreground = fore;

                        if (back != null)
                            btn.Background = back;
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowLabelData(string Data, Label label, System.Windows.Media.Brush forebrush = null, System.Windows.Media.Brush backbrush = null, System.Windows.Media.Brush boarderbrush = null)//, Brush foreground, Brush background)
        {
            if (label.CheckAccess())
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


        private void ShowLabelData2(Label label, string data, Brush fore, Brush back)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowLabelData2";

            try
            {
                if (label == null)
                    return;
                if (label.CheckAccess())
                {
                    label.Content = data;
                    if (back != null)
                        label.Background = back;
                    if (fore != null)
                        label.Foreground = fore;
                }
                else
                {
                    label.Dispatcher.Invoke(new Action(delegate
                    {
                        label.Content = data;
                        if (back != null)
                            label.Background = back;
                        if (fore != null)
                            label.Foreground = fore;
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowLabelData(Label label, string data)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowLabelData";

            try
            {
                if (label == null)
                    return;
                if (label.CheckAccess())
                {
                    label.Content = data;
                }
                else
                {
                    label.Dispatcher.Invoke(new Action(delegate
                    {
                        label.Content = data;
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowLabelColor(Label label, Brush fore, Brush back)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowLabelColor";

            try
            {
                if (label == null)
                    return;
                if (label.CheckAccess())
                {
                    if(back != null)
                        label.Background = back;
                    if (fore != null)
                        label.Foreground = fore;
                }
                else
                {
                    label.Dispatcher.Invoke(new Action(delegate
                    {
                        if (back != null)
                            label.Background = back;
                        if (fore != null)
                            label.Foreground = fore;
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private short GetDistanceXY()
        {
            short sret = 1;
            if (rbt01mm.IsChecked == true)
                sret = 1;
            else if (rbt10mm.IsChecked == true)
                sret = 10;
            else if (rbt20mm.IsChecked == true)
                sret = 20;
            else if (rbt30mm.IsChecked == true)
                sret = 30;
            else
                sret = 10;
            return sret;
        }

        private short GetDistanceZ()
        {
            short stepLength = 0;
            short sret = 0;
            stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);

            if (rbt0001mmZ.IsChecked == true)
                sret = 1;
            else if (rbt0010mmZ.IsChecked == true)
                sret = 10;
            else if (rbt0100mmZ.IsChecked == true)
                sret = (short)(1 * stepLength);
            //else if (rbt1000mmZ.IsChecked == true)
            //    sret = (short)(10 * stepLength);
            else
                sret = (short)(1 * stepLength);
            return sret;
        }

        private async void btnMoveYPlus_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMoveYPlus_Click";// MethodBase.GetCurrentMethod().Name;
            short stepLength = 0;
            short distance = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "MOVE Y+";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                distance = (short)(GetDistanceXY() * stepLength / 10);
                //distance = GetDistanceXY();
                //distance *= stepLength;

                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(0, distance, 0);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "MOVE Y+ SUCCESS");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("MOVE Y+ EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnMoveYMinus_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMoveYMinus_Click";// MethodBase.GetCurrentMethod().Name;
            short stepLength = 0;
            short distance = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "MOVE Y-";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                //stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                distance = (short)(GetDistanceXY() * (-1) * stepLength / 10);
                //distance = (short)(GetDistanceXY() * (-1));
                //distance *= stepLength;

                m_currCMD = (byte)'J';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.MoveHead(0, distance, 0);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("MOVE Y- EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void txtVIN_TextChanged(object sender, TextChangedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "txtVIN_TextChanged";

            try
            {
                if (lblVINLength != null)
                    lblVINLength.Content = txtVIN.Text.Length.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnLaserInfo_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnLaserInfo_Click";// MethodBase.GetCurrentMethod().Name;
            //MotorSpeed load = new MotorSpeed();
            //MotorSpeed noload = new MotorSpeed();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";
            string cmd = "GET LASER INFORMATION";
            string log = "";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadSWVersion();
                if (retval.execResult != 0)
                {
                    //ShowLog("GET LASER STATUS - ReadSWVersion ERROR : " + retval.execResult.ToString());
                    log = "READ SW VERSION ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "    <LASER STATUS>");
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "    SW VER : " + retval.recvString);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadSerialNumber();
                if (retval.execResult != 0)
                {
                    //ShowLog("GET LASER STATUS - ReadSerialNumber ERROR : " + retval.execResult.ToString());
                    log = "READ SERIAL NUMBER ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "    SERIAL : " + retval.recvString);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadLaserVersion();
                if (retval.execResult != 0)
                {
                    log = "READ LASER VERSION ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //ShowLog("GET LASER STATUS - ReadLaserVersion ERROR : " + retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "    LASER VER : " + retval.recvString);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadLaserConfiguration();
                if (retval.execResult != 0)
                {
                    log = "READ LASER CONFIG ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //ShowLog("GET LASER STATUS - ReadLaserConfiguration ERROR : " + retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "    CONFIG : " + retval.recvString);

                //Util.GetPrivateProfileValue("VINDATA", "PROFILEFIRE", "0", ref value, Constants.PARAMS_INI_FILE);                 // load waveform profile number
                //ProfileTxt.Text = value;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(value);
                if (retval.execResult != 0)
                {
                    log = "SELECT PROFILE ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //ShowLog("GET LASER STATUS - SelectProfile ERROR : " + retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "    SEL ID : " + retval.recvString + Environment.NewLine);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                if (retval.execResult != 0)
                {
                    log = "READ DEVICE STATUS ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //ShowLog("GET LASER STATUS - ReadDeviceStatus ERROR : " + retval.execResult.ToString());
                    return;
                }
                string[] states = retval.recvString.Split(':');
                LASERSTATUS lsts = (LASERSTATUS)UInt32.Parse(states[1]);
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "    STATUS : " + string.Format("STA: 0x{0:X}", lsts));
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd,"    ----END------");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("GET LASER STATUS - EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnPINTest_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnPINTest_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "PIN TEST";
            string value = "";
            string patternName = "";
            byte bHeadType = 0;
            PatternValueEx pattern = new PatternValueEx();
            string log = "";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                if (retval.execResult != 0)
                {
                    log = "STOP EMISSION ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                ShowRectangle(EmissionLamp, Brushes.Black);

                if (cbxPatternList.SelectedIndex >= 0)
                    patternName = cbxPatternList.SelectedItem.ToString();
                else
                    patternName = "Pattern_DEFAULT";

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);
                ImageProcessManager.GetPatternValue(patternName, bHeadType, ref pattern);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(pattern.laserValue.waveformNum.ToString());
                if (retval.execResult != 0)
                {
                    log = "SELECT PROFILE ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                string[] prsel = retval.recvString.Split('[', ']');
                if (prsel.Length < 2)
                {
                    log = "SELECT PROFILE STRING ERROR (LENGTH = " + prsel.Length.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (prsel[0] != "PRSEL: ")
                {
                    log = "SELECT PROFILE SETTING ERROR (PRSEL[0] = " + prsel[0] + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                string[] sel = prsel[1].Split(':');
                if (pattern.laserValue.waveformNum.ToString() != sel[0])
                {
                    log = "SELECT PROFILE NOT MATCH ERROR (SEL[0] = " + sel[0] + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //4. get waveform mode
                //ShowLog("MARKING - CONFIG WAVEFORM MODE");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ConfigWaveformMode(0);
                if (retval.execResult != 0)
                {
                    log = "CONFIG WAVEFORM ERROR (" + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                string[] pcfg = retval.recvString.Split('[', ']');
                if (pcfg.Length < 2)
                {
                    log = "CONFIG WAVEFORM PROFILE ERROR (LENGTH = " + pcfg.Length.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if ((bool)EmissionAuto.IsChecked)
                {
                    ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "LASER EMISSION ON");
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                    if (retval.execResult != 0)
                    {
                        log = "LASER EMISSION ON ERROR (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    //ShowLog("LASER EMISSION ON SUCCESS");
                    ShowRectangle(EmissionLamp, Brushes.Red);
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(5, true);    //  Firmware Version 2.55 :  5 -> On-Delay-Off
                if (retval.execResult != 0)
                {
                    log = "TEST SOL FET ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //ShowLog("LASER DEVICE STATUS READ");

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                if (retval.execResult != 0)
                {
                    log = "READ LASER STATUS ERROR (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //ShowLog("LASER DEVICE STATUS SUCCESS");
                string[] st = retval.recvString.Split(':');
                LASERSTATUS Status = (LASERSTATUS)UInt32.Parse(st[1]);
                if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                {
                    //ShowLog("LASER EMISSION OFF START");
                    ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "LASER EMISSION OFF");
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    if (retval.execResult != 0)
                    {
                        log = "LASER EMISSION OFF ERROR (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    //ShowLog("LASER EMISSION OFF SUCCESS");
                    ShowRectangle(EmissionLamp, Brushes.Black);
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="speedtype">
        /// 0 : move home
        /// 1 : free speed
        /// 2 : marking speed
        /// 3 : measuring speed
        /// </param>
        /// <returns></returns>
        private async Task<ITNTResponseArgs> SendMotorSpeed(string cmd, byte speedtype)
        {
            string className = "SetControllerWindow2";
            string funcName = "SendMotorSpeed";

            MotorSpeed speed = new MotorSpeed();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte sendCommand = (byte)'L';

            try
            {
                switch (speedtype)
                {
                    case  (byte)motorSpeedType.HomeMoving :
                        short.TryParse(txbSpeedHomeInitialValue.Text, out speed.initSpeed);
                        short.TryParse(txbSpeedHomeTargetValue.Text, out speed.targetSpeed);
                        short.TryParse(txbSpeedHomeAccelValue.Text, out speed.accelSpeed);
                        short.TryParse(txbSpeedHomeDecelValue.Text, out speed.decelSpeed);

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 50;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 80;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 400;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 400;

                        sendCommand = (byte)'F';
                        break;

                    case (byte)motorSpeedType.FastMoving:
                        short.TryParse(txbSpeedFastInitialValue.Text, out speed.initSpeed);
                        short.TryParse(txbSpeedFastTargetValue.Text, out speed.targetSpeed);
                        short.TryParse(txbSpeedFastAccelValue.Text, out speed.accelSpeed);
                        short.TryParse(txbSpeedFastDecelValue.Text, out speed.decelSpeed);

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 100;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 1200;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 2000;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 2000;
                        break;

                    case (byte)motorSpeedType.MarkMoving :
                        short.TryParse(txbSpeedMarkInitialValue.Text, out speed.initSpeed);
                        short.TryParse(txbSpeedMarkTargetValue.Text, out speed.targetSpeed);
                        short.TryParse(txbSpeedMarkAccelValue.Text, out speed.accelSpeed);
                        short.TryParse(txbSpeedMarkDecelValue.Text, out speed.decelSpeed);

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 70;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 100;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 5000;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 5000;
                        break;

                    case (byte)motorSpeedType.MeasureMoving :
                        short.TryParse(txbSpeedMeasureInitialValue.Text, out speed.initSpeed);
                        short.TryParse(txbSpeedMeasureTargetValue.Text, out speed.targetSpeed);
                        short.TryParse(txbSpeedMeasureAccelValue.Text, out speed.accelSpeed);
                        short.TryParse(txbSpeedMeasureDecelValue.Text, out speed.decelSpeed);

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 110;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 1400;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 2000;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 2000;
                        break;

                    case (byte)motorSpeedType.ScanMoving :
                        short.TryParse(txbSpeedScanInitialValue.Text, out speed.initSpeed);
                        short.TryParse(txbSpeedScanTargetValue.Text, out speed.targetSpeed);
                        short.TryParse(txbSpeedScanAccelValue.Text, out speed.accelSpeed);
                        short.TryParse(txbSpeedScanDecelValue.Text, out speed.decelSpeed);

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 50;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 80;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 400;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 400;
                        break;

                    case (byte)motorSpeedType.ScanFastMoving: 
                        short.TryParse(txbSpeedScanFastInitialValue.Text, out speed.initSpeed);
                        short.TryParse(txbSpeedScanFastTargetValue.Text, out speed.targetSpeed);
                        short.TryParse(txbSpeedScanFastAccelValue.Text, out speed.accelSpeed);
                        short.TryParse(txbSpeedScanFastDecelValue.Text, out speed.decelSpeed);

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 50;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 80;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 400;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 400;
                        break;

                    default:
                        short.TryParse(txbSpeedMarkInitialValue.Text, out speed.initSpeed);
                        short.TryParse(txbSpeedMarkTargetValue.Text, out speed.targetSpeed);
                        short.TryParse(txbSpeedMarkAccelValue.Text, out speed.accelSpeed);
                        short.TryParse(txbSpeedMarkDecelValue.Text, out speed.decelSpeed);

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 50;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 80;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 400;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 400;
                        break;
                }

                //string logtmp = "SPEED = " + speed.initSpeed.ToString() + ", " + speed.targetSpeed.ToString() + ", " + speed.accelSpeed.ToString() + ", " + speed.decelSpeed.ToString();
                //ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, logtmp, logtmp);
                m_currCMD = sendCommand;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.LoadSpeed(sendCommand, speed.initSpeed, speed.targetSpeed, speed.accelSpeed, speed.decelSpeed);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "LOAD SPEED ERROR (RESULT = " + retval.execResult.ToString() + ")");
                    return retval;
                }

                return retval;
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SEND MOTOR SPEED EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
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

            Image imageB = new Image();
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


        public static int GetVinCharacterFontDot(string vin, List<List<FontDataClass>> fontdata, double fontsizeX, double fontsizeY, double shiftVal, string fontName)
        {
            string className = "SetControllerWindow2";
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

        private async void btnReadFont_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnReadFont_Click";
            int retval = 0;
            string cmd = "READ FONT";
            string patternName = "";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                if (cbxPatternList.SelectedIndex < 0)
                    patternName = "Pattern_DEFAULT";
                else
                    patternName = cbxPatternList.Text;

                retval = ReadFontData(cmd, patternName);
                if(retval != 0)
                {
                    return;
                }
                await ShowCurrentMarkingInformation(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2);
                GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);
                bReadFontValue = true;
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                //GetVinCharacterFontDot(vin, fName, pattern.fontValue.fontName);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async Task<int> ShowOneVinNoCharacter(List<FontDataClass> font, VinNoInfo vin, double Density, double fontSizeX, double fontSizeY, double shiftVal, Canvas showcanvas, Brush brush, Brush background, byte clearFlag, int interval = 0)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowOneVinNoCharacter";

            double canvaswidth = showcanvas.Width;
            double canvasheight = showcanvas.Height;
            double OriginX = 1.5d * Util.PXPERMM;
            double OriginY = 2.5d * Util.PXPERMM;
            double orgWidth = vin.width * Util.PXPERMM + OriginX * 2.0;
            double orgHeight = Util.PXPERMM * vin.height + OriginY * 2.0;
            /***********************************
            1 inch  25.4mm
            1 inch  72 pt
            1 inch  96 px        dpi
            1 mm    2.83465 pt
            1 mm    3.7795 px    dpi/ 25.4
            ***********************************/
            double CharHeight = vin.height * Util.PXPERMM;
            double CharWidth = vin.width * Util.PXPERMM;
            double CharThick = vin.thickness * Util.PXPERMM * canvaswidth / orgWidth;
            double heightthRation = canvasheight / orgHeight;
            double widthRation = canvaswidth / orgWidth;
            int index = 0;
            int Dotsize = 5;
            Line[] line = new Line[font.Count];
            Ellipse[] CurrentDot = new Ellipse[font.Count];
            //int Density = 0;
            try
            {
                //if (clearFlag != 0)
                //    showcanvas.Children.Clear();
                //showcanvas.UpdateLayout();
                //showcanvas.Background = background;

                for (int j = 0; j < font.Count; j++)
                {
                    if ((font[j] == null) || (font[j].Flag < 0))
                        continue;

                    switch (Density)
                    {
                        case 0:     // Dot Marking : Dot
                        case 1:     // Dot Marking : Dot for Line
                            if (font[j].Flag >= 0)
                            {
                                //2022.03.25 Replace by drawing ellipse.
                                CurrentDot[index] = new Ellipse();
                                CurrentDot[index].Stroke = brush;
                                CurrentDot[index].StrokeThickness = CharThick;
                                Canvas.SetZIndex(CurrentDot[index], (int)(CharThick + 0.5));
                                CurrentDot[index].Height = (double)Dotsize;
                                CurrentDot[index].Width = (double)Dotsize;
                                CurrentDot[index].Fill = brush;
                                CurrentDot[index].Margin = new Thickness((OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation - (double)Dotsize / 2.0, (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation - (double)Dotsize / 2.0, 0.0, 0.0);
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
                                line[index].Y1 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;
                            }
                            else if (font[j].Flag == 2 || font[j].Flag == 3 || font[j].Flag == 5)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                    line[index].Y2 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;

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
                                line[index].Y1 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;
                            }
                            else if (font[j].Flag == 4)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                    line[index].Y2 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;
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

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION1 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //Debug.WriteLine("ShowOneVinNoCharacter excption {0} - {1}", ex.HResult, ex.Message);
                return ex.HResult;
            }
        }

        private async Task<int> ShowOneVinNoCharacter(List<FontDataClass> font, VinNoInfo vin, double fontSizeX, double fontSizeY, Canvas showcanvas, Brush brush, Brush background, byte clearFlag, int interval = 0)
        {
            //double canvaswidth = 61.55; //showcanvas.ActualWidth;
            string className = "SetControllerWindow2";
            string funcName = "ShowOneVinNoCharacter";
            double canvaswidth = showcanvas.Width;
            double canvasheight = showcanvas.Height;
            double OriginX = 1.5d * Util.PXPERMM;
            double OriginY = 2.5d * Util.PXPERMM;
            double orgWidth = vin.width * Util.PXPERMM + OriginX * 2.0;
            double orgHeight = Util.PXPERMM * vin.height + OriginY * 2.0;
            /***********************************
            1 inch  25.4mm
            1 inch  72 pt
            1 inch  96 px        dpi
            1 mm    2.83465 pt
            1 mm    3.7795 px    dpi/ 25.4
            ***********************************/
            double CharHeight = vin.height * Util.PXPERMM;
            double CharWidth = vin.width * Util.PXPERMM;
            double CharThick = vin.thickness * Util.PXPERMM * canvaswidth / orgWidth;
            double heightthRation = canvasheight / orgHeight;
            double widthRation = canvaswidth / orgWidth;
            int index = 0;
            int Dotsize = 5;
            Line[] line = new Line[font.Count];
            Ellipse[] CurrentDot = new Ellipse[font.Count];
            int density = 1;

            try
            {
                showcanvas.UpdateLayout();
                showcanvas.Background = background;

                if (clearFlag != 0)
                    showcanvas.Children.Clear();

                for (int j = 0; j < font.Count; j++)
                {
                    //switch (int.Parse(Txt_Density.Text))
                    switch (density)
                    {
                        case 0:
                            if (font[j].Flag == 1 || font[j].Flag == 2 || font[j].Flag == 4 || font[j].Flag == 5)
                            {
                                //2022.03.25 Replace by drawing ellipse.
                                CurrentDot[index] = new Ellipse();
                                CurrentDot[index].Stroke = brush;
                                CurrentDot[index].StrokeThickness = CharThick;
                                Canvas.SetZIndex(CurrentDot[index], (int)(CharThick + 0.5));
                                CurrentDot[index].Height = Dotsize;
                                CurrentDot[index].Width = Dotsize;
                                CurrentDot[index].Fill = brush;
                                CurrentDot[index].Margin = new Thickness((OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation - (double)Dotsize / 2.0, (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation - (double)Dotsize / 2.0, 0.0, 0.0);
                                showcanvas.Children.Add(CurrentDot[index]);
                                index++;
                            }
                            break;
                        case 1:
                            {
                                //2022.03.25 Replace by drawing ellipse.
                                CurrentDot[index] = new Ellipse();
                                CurrentDot[index].Stroke = brush;
                                CurrentDot[index].StrokeThickness = CharThick;
                                Canvas.SetZIndex(CurrentDot[index], (int)(CharThick + 0.5));
                                CurrentDot[index].Height = (double)Dotsize;
                                CurrentDot[index].Width = (double)Dotsize;
                                CurrentDot[index].Fill = brush;
                                CurrentDot[index].Margin = new Thickness((OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation - (double)Dotsize / 2.0, (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation - (double)Dotsize / 2.0, 0.0, 0.0);
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
                                line[index].Y1 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;
                            }
                            else if (font[j].Flag == 2 || font[j].Flag == 3 || font[j].Flag == 5)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                    line[index].Y2 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;

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
                                line[index].Y1 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;
                            }
                            else if (font[j].Flag == 4)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                                    line[index].Y2 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;
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

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION1 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //Debug.WriteLine("ShowOneVinNoCharacter excption {0} - {1}", ex.HResult, ex.Message);
                return ex.HResult;
            }
        }

        public void ClearMarkVINDisplay()
        {
            if (CheckAccess())
            {
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

        public int ReadFontData(string cmd, string patternName)
        {
            string className = "SetControllerWindow2";
            string funcName = "ReadFontData";

            //int retval = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string value = "";
            byte bHeadType = 0;
            //string patName = "";
            VinNoInfo vininfo = new VinNoInfo();
            string log = "";
            string errorCode = "";
            //byte fontdirection = 0;

            try
            {
                if (txtVIN.Text.Length <= 0)
                {
                    //log = "READ FONT DATA FAIL - ENTER VIN ";
                    //ShowLog(log);
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "VIN IS BLANK. ENTER VIN");
                    retval.execResult = -1;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN IS BLANK. ENTER VIN", Thread.CurrentThread.ManagedThreadId);
                    return retval.execResult;
                }

                //currMarkInfo.curvin = txtVIN.Text;
                ClearMarkVINDisplay();
                currMarkInfo.Initialize();

                currMarkInfo.currMarkData.mesData.markvin = txtVIN.Text;
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                //if (cbxPatternList.SelectedIndex < 0)
                //    patternName = "Pattern_DEFAULT";
                //else
                //    patternName = cbxPatternList.Text;

                currMarkInfo.currMarkData.pattern.fontValue.fontName = cbxFontName.Text;
                double.TryParse(txtWidth.Text, out currMarkInfo.currMarkData.pattern.fontValue.width);
                double.TryParse(txtHeight.Text, out currMarkInfo.currMarkData.pattern.fontValue.height);
                double.TryParse(txtPitch.Text, out currMarkInfo.currMarkData.pattern.fontValue.pitch);

                ReadPatternValue(ref currMarkInfo.currMarkData.pattern);
                //pattern.fontValue.thickness = 0.4;
                //ImageProcessManager.GetPatternValue(patName, bHeadType, ref currMarkInfo.currMarkData.pattern);
                //fName = ImageProcessManager.GetFontFileName(bHeadType, pattern.fontValue.fontName, pattern.laserValue.density);

                vininfo.vinNo = currMarkInfo.currMarkData.mesData.markvin;
                vininfo.fontName = currMarkInfo.currMarkData.pattern.fontValue.fontName;
                vininfo.width = currMarkInfo.currMarkData.pattern.fontValue.width;
                vininfo.height = currMarkInfo.currMarkData.pattern.fontValue.height;
                vininfo.pitch = currMarkInfo.currMarkData.pattern.fontValue.pitch;
                vininfo.thickness = currMarkInfo.currMarkData.pattern.fontValue.thickness;

                currMarkInfo.currMarkData.pattern = (PatternValueEx)currMarkInfo.currMarkData.pattern.Clone();

                //Util.GetPrivateProfileValue("OPTION", "FONTDIRECTION", "0", ref value, Constants.PARAMS_INI_FILE);
                //byte.TryParse(value, out fontdirection);

                //retval = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, ref currMarkInfo.currMarkData.fontData, ref currMarkInfo.revsData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref errorCode);
                retval = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref currMarkInfo.currMarkData.fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref errorCode);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET FONT ERROR" + retval.ToString());
                    return retval.execResult;
                }
                //bReadFontValue = true;
                //GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.vin, currMarkInfo.revsData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval.execResult;
        }

        private async Task ShowCurrentMarkingInformation(string vin, PatternValueEx pattern, List<List<FontDataClass>> fontdata, double fontSizeX, double fontSizeY, double shiftVal, byte showFlag)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowCurrentMarkingInformation";
            string stringcolorfore = "#FFCBCBCB";
            string stringcolorback = "#FFE1E1E1";
            Color colorback;
            Color colorfore;

            try
            {
                if (CheckAccess())
                {
                    //lbllblCurrentSerial.Content = seq;
                    //lbllblCurrentType.Content = cartype.Trim();
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

                    await showRecogCharacters2(vin, fontdata, pattern, fontSizeX, fontSizeY, shiftVal, colorfore, colorback);
                }
                else
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
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

                        await showRecogCharacters2(vin, fontdata, pattern, fontSizeX, fontSizeY, shiftVal, colorfore, colorback);
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }



        //private async Task showRecogCharacters2(string vin, PatternValueLaser pattern, Color fore, Color back)
        //{
        //    string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "showRecogCharacters2";// MethodBase.GetCurrentMethod().Name;
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    //bool bShowLabel = false;
        //    double fontSizeX = 0.0d;
        //    double fontSizeY = 0.0d;
        //    double fontSizeZ = 0.0d;
        //    Dictionary<int, List<FontDataLaser>> MyData = new Dictionary<int, List<FontDataLaser>>();
        //    string ErrorCode = "";
        //    int retval = 0;
        //    int charNum = 0;
        //    Canvas[] cvsshowChar = new Canvas[19];
        //    string ctrlName = "";
        //    VinNoInfo vininfo = new VinNoInfo();
        //    int count = vin.Length;
        //    string colorstring = "#FFC8C8C8";
        //    Color color;// = (Color)ColorConverter.ConvertFromString(colorstring);
        //    Brush brush;// = new Brush();
        //    //string fonttype = "";

        //    try
        //    {
        //        vininfo.vinNo = vin;
        //        vininfo.fontName = pattern.fontValue.fontName;
        //        vininfo.width = pattern.fontValue.width;
        //        vininfo.height = pattern.fontValue.height;
        //        vininfo.pitch = pattern.fontValue.pitch;
        //        vininfo.thickness = pattern.fontValue.thickness;

        //        retval = ImageProcessManagerLaser.GetFontData(vininfo, ref MyData, out fontSizeX, out fontSizeY, out fontSizeZ, out ErrorCode);
        //        if (retval != 0)
        //            return;
        //        //bShowLabel = true;

        //        //Util.GetPrivateProfileValue("USEFONT", "TYPE", "0", ref fonttype, "Parameter/FONT.ini");

        //        for (int i = 0; i < count; i++)
        //        {
        //            charNum = (int)vin[i] - 1;
        //            ctrlName = string.Format("cvsshowChar{0:D2}", i);
        //            cvsshowChar[i] = (Canvas)FindName(ctrlName);
        //            if (cvsshowChar[i] == null)
        //                continue;

        //            cvsshowChar[i].Background = new SolidColorBrush(back);
        //            brush = new SolidColorBrush(fore);

        //            if ((charNum >= 31) && (charNum <= 128))
        //            {
        //                List<FontDataLaser> fdata = new List<FontDataLaser>();
        //                fdata = MyData[charNum];
        //                if (CheckAccess())
        //                {
        //                    //if (fonttype == "0")
        //                    //    retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                    //else
        //                        retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                }
        //                else
        //                {
        //                    Dispatcher.Invoke(new Action(async delegate
        //                    {
        //                        //if (fonttype == "0")
        //                        //    retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                        //else
        //                            retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                    }));
        //                }
        //            }

        //            //lblshowScore[0, i].Content = string.Format("{0:00.00}", confidence[i - 1] * 100);
        //            //lblshowScore[1, i].Content = string.Format("{0:00.00}", quality[i - 1] * 100);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //Debug.WriteLine("showRecogCharacters excption {0} - {1}", ex.HResult, ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }

        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //}


        //private async Task showRecogCharacters2(string vin, Dictionary<int,List<FontData>>MyData, Color fore, Color back)
        //{
        //    string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "showRecogCharacters2";// MethodBase.GetCurrentMethod().Name;
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    //bool bShowLabel = false;
        //    //double fontSizeX = 0.0d;
        //    //double fontSizeY = 0.0d;
        //    //double fontSizeZ = 0.0d;
        //    ////Dictionary<int, List<FontDataLaser>> MyData = new Dictionary<int, List<FontDataLaser>>();
        //    //string ErrorCode = "";
        //    int retval = 0;
        //    int charNum = 0;
        //    Canvas[] cvsshowChar = new Canvas[19];
        //    string ctrlName = "";
        //    //VinNoInfo vininfo = new VinNoInfo();
        //    int count = vin.Length;
        //    string colorstring = "#FFC8C8C8";
        //    Color color;// = (Color)ColorConverter.ConvertFromString(colorstring);
        //    Brush brush;// = new Brush();
        //    //string fonttype = "";

        //    try
        //    {
        //        //vininfo.vinNo = vin;
        //        //vininfo.fontName = pattern.fontValue.fontName;
        //        //vininfo.width = pattern.fontValue.width;
        //        //vininfo.height = pattern.fontValue.height;
        //        //vininfo.pitch = pattern.fontValue.pitch;
        //        //vininfo.thickness = pattern.fontValue.thickness;

        //        //retval = ImageProcessManagerLaser.GetFontData(vininfo, ref MyData, out fontSizeX, out fontSizeY, out fontSizeZ, out ErrorCode);
        //        //if (retval != 0)
        //        //    return;
        //        ////bShowLabel = true;

        //        //Util.GetPrivateProfileValue("USEFONT", "TYPE", "0", ref fonttype, "Parameter/FONT.ini");

        //        for (int i = 0; i < count; i++)
        //        {
        //            charNum = (int)vin[i] - 1;
        //            ctrlName = string.Format("cvsshowChar{0:D2}", i);
        //            cvsshowChar[i] = (Canvas)FindName(ctrlName);
        //            if (cvsshowChar[i] == null)
        //                continue;

        //            cvsshowChar[i].Background = new SolidColorBrush(back);
        //            brush = new SolidColorBrush(fore);

        //            if ((charNum >= 31) && (charNum <= 128))
        //            {
        //                List<FontData> fdata = new List<FontData>();
        //                fdata = MyData[charNum];
        //                if (CheckAccess())
        //                {
        //                    //if (fonttype == "0")
        //                    //    retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                    //else
        //                    retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                }
        //                else
        //                {
        //                    Dispatcher.Invoke(new Action(async delegate
        //                    {
        //                        //if (fonttype == "0")
        //                        //    retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                        //else
        //                        retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                    }));
        //                }
        //            }

        //            //lblshowScore[0, i].Content = string.Format("{0:00.00}", confidence[i - 1] * 100);
        //            //lblshowScore[1, i].Content = string.Format("{0:00.00}", quality[i - 1] * 100);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //Debug.WriteLine("showRecogCharacters excption {0} - {1}", ex.HResult, ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }

        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //}

        //public void showRecogCharacters(string vin, PatternValueEx pattern)
        //{
        //    string className = "SetControllerWindow2";
        //    string funcName = "showRecogCharacters";
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    double fontSizeX = 0.0d;
        //    double fontSizeY = 0.0d;
        //    Dictionary<int, List<FontDataClass>> MyData = new Dictionary<int, List<FontDataClass>>();
        //    string ErrorCode = "";
        //    int retval = 0;
        //    int charNum = 0;
        //    Canvas[] cvsshowChar = new Canvas[19];
        //    string ctrlName = "";
        //    VinNoInfo vininfo = new VinNoInfo();
        //    int count = vin.Length;

        //    try
        //    {
        //        vininfo.vinNo = vin;
        //        vininfo.fontName = pattern.fontValue.fontName;
        //        vininfo.width = pattern.fontValue.width;
        //        vininfo.height = pattern.fontValue.height;
        //        vininfo.pitch = pattern.fontValue.pitch;
        //        vininfo.thickness = pattern.fontValue.thickness;

        //        //retval = GetFontData(vininfo, ref MyData, out fontSizeX, out fontSizeY, out ErrorCode);
        //        if (retval != 0)
        //            return;
        //        Brush brush;
        //        string fonttype = AppDomain.CurrentDomain.BaseDirectory + vininfo.fontName + ".FON";

        //        for (int i = 0; i < count; i++)
        //        {
        //            charNum = (int)vin[i] - 1;
        //            ctrlName = string.Format("cvsshowChar{0:D2}", i);
        //            cvsshowChar[i] = Dispatcher.Invoke(() => (Canvas)FindName(ctrlName));
        //            if (cvsshowChar[i] == null)
        //                continue;
        //            brush = Brushes.Black;

        //            if ((charNum >= 31) && (charNum <= 128))
        //            {
        //                List<FontDataClass> fdata = new List<FontDataClass>();
        //                fdata = MyData[charNum];

        //                Dispatcher.Invoke(new Action(async delegate
        //                {
        //                    if (fonttype == "0")
        //                        retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                    else
        //                        retval = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
        //                }));

        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        //Debug.WriteLine("showRecogCharacters excption {0} - {1}", ex.HResult, ex.Message);
        //        //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }

        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //}

        private async Task showRecogCharacters2(string vin, List<List<FontDataClass>> fontdata, PatternValueEx pattern, double fontSizeX, double fontSizeY, double shiftVal, Color fore, Color back)
        {
            string className = "SetControllerWindow2";
            string funcName = "showRecogCharacters2";
            int retval = 0;
            int charNum = 0;
            Canvas[] cvsshowChar = new Canvas[19];
            string ctrlName = "";
            VinNoInfo vininfo = new VinNoInfo();
            int count = 0;
            Brush brush;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
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
                    cvsshowChar[i] = (Canvas)FindName(ctrlName);
                    if (cvsshowChar[i] == null)
                        continue;

                    cvsshowChar[i].Background = new SolidColorBrush(back);
                    brush = new SolidColorBrush(fore);

                    //if ((charNum >= 31) && (charNum <= 128))
                    {
                        List<FontDataClass> fdata = new List<FontDataClass>();
                        if (fontdata.Count >= i)
                            fdata = fontdata[i].ToList();
                        if (CheckAccess())
                            retval = await ShowOneVinNoCharacter(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
                        else
                        {
                            Dispatcher.Invoke(new Action(async delegate
                            {
                                retval = await ShowOneVinNoCharacter(fdata, vininfo, pattern.laserValue.density, fontSizeX, fontSizeY, shiftVal, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("showRecogCharacters excption {0} - {1}", ex.HResult, ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }



        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="patternName"></param>
        /// <param name="data"></param>
        /// <param name="saveFlag"></param>
        //private void SavePatternData(string patternName, PatternValueEx data, byte saveFlag)
        //{
        //    string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "SavePatternData";// MethodBase.GetCurrentMethod().Name;
        //    string patternfile;
        //    string value = "";

        //    try
        //    {
        //        patternfile = Constants.PATTERN_PATH + patternName + ".ini";

        //        //Font Value
        //        value = data.fontValue.fontName;
        //        Util.WritePrivateProfileValue("FONT", "NAME", value, patternfile); // load FONT

        //        value = data.fontValue.height.ToString();
        //        Util.WritePrivateProfileValue("FONT", "HEIGHT", value, patternfile); // load height

        //        value = data.fontValue.width.ToString();
        //        Util.WritePrivateProfileValue("FONT", "WIDTH", value, patternfile); // load width

        //        value = data.fontValue.pitch.ToString();
        //        Util.WritePrivateProfileValue("FONT", "PITCH", value, patternfile); // load pitch

        //        value = data.fontValue.rotateAngle.ToString();
        //        Util.WritePrivateProfileValue("FONT", "ROTATEANGLE", value, patternfile); // load angle

        //        //value = data.fontValue.strikeCount.ToString();
        //        //Util.WritePrivateProfileValue("FONT", "STRIKE", value, patternfile); //load strike

        //        value = data.fontValue.thickness.ToString();
        //        Util.WritePrivateProfileValue("FONT", "THICKNESS", value, patternfile); //load strike

        //        value = data.headValue.stepLength.ToString();
        //        Util.WritePrivateProfileValue("MARK", "STEP_LENGTH", value, Constants.MARKING_INI_FILE);

        //        //Speed value
        //        value = data.speedValue.initSpeed4MarkV.ToString();
        //        Util.WritePrivateProfileValue("MARKVECTOR", "INITIALSPEED", value, patternfile); // load initial speed

        //        value = data.speedValue.targetSpeed4MarkV.ToString();
        //        Util.WritePrivateProfileValue("MARKVECTOR", "TARGETSPEED", value, patternfile); // load target speed

        //        value = data.speedValue.accelSpeed4MarkV.ToString();
        //        Util.WritePrivateProfileValue("MARKVECTOR", "ACCELERATION", value, patternfile); // load accel

        //        value = data.speedValue.decelSpeed4MarkV.ToString();
        //        Util.WritePrivateProfileValue("MARKVECTOR", "DECELERATION", value, patternfile); // load decel

        //        value = data.speedValue.initSpeed4MarkR.ToString();
        //        Util.WritePrivateProfileValue("MARKRASTER", "INITIALSPEED", value, patternfile); // load initial speed

        //        value = data.speedValue.targetSpeed4MarkR.ToString();
        //        Util.WritePrivateProfileValue("MARKRASTER", "TARGETSPEED", value, patternfile); // load target speed

        //        value = data.speedValue.accelSpeed4MarkR.ToString();
        //        Util.WritePrivateProfileValue("MARKRASTER", "ACCELERATION", value, patternfile); // load accel

        //        value = data.speedValue.decelSpeed4MarkR.ToString();
        //        Util.WritePrivateProfileValue("MARKRASTER", "DECELERATION", value, patternfile); // load decel

        //        value = data.speedValue.initSpeed4Home.ToString();
        //        Util.WritePrivateProfileValue("MOVINGHOME", "INITIALSPEED", value, patternfile); // load initial speed

        //        value = data.speedValue.targetSpeed4Home.ToString();
        //        Util.WritePrivateProfileValue("MOVINGHOME", "TARGETSPEED", value, patternfile); // load target speed

        //        value = data.speedValue.accelSpeed4Home.ToString();
        //        Util.WritePrivateProfileValue("MOVINGHOME", "ACCELERATION", value, patternfile); // load accel

        //        value = data.speedValue.decelSpeed4Home.ToString();
        //        Util.WritePrivateProfileValue("MOVINGHOME", "DECELERATION", value, patternfile); // load decel

        //        value = data.speedValue.initSpeed4Measure.ToString();
        //        Util.WritePrivateProfileValue("MOVINGMEASURE", "INITIALSPEED", value, patternfile); // load initial speed

        //        value = data.speedValue.targetSpeed4Measure.ToString();
        //        Util.WritePrivateProfileValue("MOVINGMEASURE", "TARGETSPEED", value, patternfile); // load target speed

        //        value = data.speedValue.accelSpeed4Measure.ToString();
        //        Util.WritePrivateProfileValue("MOVINGMEASURE", "ACCELERATION", value, patternfile); // load accel

        //        value = data.speedValue.decelSpeed4Measure.ToString();
        //        Util.WritePrivateProfileValue("MOVINGMEASURE", "DECELERATION", value, patternfile); // load decel

        //        value = data.speedValue.solOnTime.ToString();
        //        Util.WritePrivateProfileValue("SOLENOID", "SOLONTIME", value, patternfile); // load sol on 

        //        value = data.speedValue.solOffTime.ToString();
        //        Util.WritePrivateProfileValue("SOLENOID", "SOLOFFTIME", value, patternfile); // load sol off

        //        value = data.speedValue.dwellTime.ToString();
        //        Util.WritePrivateProfileValue("SOLENOID", "DWELLTIME", value, patternfile); // load sol off

        //        //Position
        //        value = data.positionValue.center3DPos.X.ToString();
        //        Util.WritePrivateProfileValue("POSITION", "STARTPOSX", value, patternfile); // load X

        //        value = data.positionValue.center3DPos.Y.ToString();
        //        Util.WritePrivateProfileValue("POSITION", "STARTPOSY", value, patternfile); // load Y

        //        value = data.positionValue.center3DPos.Z.ToString();
        //        Util.WritePrivateProfileValue("POSITION", "STARTPOSZ", value, patternfile); // load Z


        //        //Lase Value
        //        value = data.laserValue.waveformNum.ToString();
        //        Util.WritePrivateProfileValue("LASERSOURCE", "WAVEFORMPROFILE", value, patternfile); // load Z

        //        value = data.laserValue.waveformClean.ToString();
        //        Util.WritePrivateProfileValue("LASERSOURCE", "WAVEFORMCLEAN", value, patternfile); // load Z

        //        value = data.laserValue.phaseComp.ToString();
        //        Util.WritePrivateProfileValue("LASERSOURCE", "PHASECOMP", value, patternfile); // load Z

        //        value = data.laserValue.density.ToString();
        //        Util.WritePrivateProfileValue("LASERSOURCE", "DENSITY", value, patternfile); // load Z

        //        value = data.laserValue.cleanPosition.ToString();
        //        Util.WritePrivateProfileValue("LASERSOURCE", "CLEANPOSITION", value, patternfile); // load Z

        //        value = data.laserValue.charClean.ToString();
        //        Util.WritePrivateProfileValue("LASERSOURCE", "CHARCLEAN", value, patternfile); // load Z

        //        value = data.laserValue.combineFireClean.ToString();
        //        Util.WritePrivateProfileValue("LASERSOURCE", "COMBINEFIRECLEAN", value, patternfile); // load Z



        //        if ((saveFlag & 0x01) != 0)
        //        {
        //            value = data.speedValue.initSpeed4Fast.ToString();
        //            Util.WritePrivateProfileValue("FREEMOVING", "INITIALSPEED", value, Constants.MARKING_INI_FILE); // load initial speed

        //            value = data.speedValue.targetSpeed4Fast.ToString();
        //            Util.WritePrivateProfileValue("FREEMOVING", "TARGETSPEED", value, Constants.MARKING_INI_FILE); // load target speed

        //            value = data.speedValue.accelSpeed4Fast.ToString();
        //            Util.WritePrivateProfileValue("FREEMOVING", "ACCELERATION", value, Constants.MARKING_INI_FILE); // load accel

        //            value = data.speedValue.decelSpeed4Fast.ToString();
        //            Util.WritePrivateProfileValue("FREEMOVING", "DECELERATION", value, Constants.MARKING_INI_FILE); // load decel

        //            value = data.headValue.max_X.ToString();
        //            Util.WritePrivateProfileValue("MARK", "MAX_X", value, Constants.MARKING_INI_FILE); // load accel

        //            value = data.headValue.max_Y.ToString();
        //            Util.WritePrivateProfileValue("MARK", "MAX_Y", value, Constants.MARKING_INI_FILE); // load accel

        //            value = data.headValue.max_Z.ToString();
        //            Util.WritePrivateProfileValue("MARK", "MAX_Z", value, Constants.MARKING_INI_FILE); // load accel

        //            value = data.positionValue.park3DPos.X.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "PARKING_X", value, Constants.MARKING_INI_FILE); // Park offset X

        //            value = data.positionValue.park3DPos.Y.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "PARKING_Y", value, Constants.MARKING_INI_FILE); // Park offset Y

        //            value = data.positionValue.park3DPos.Z.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "PARKING_Z", value, Constants.MARKING_INI_FILE); // Park offset Z

        //            value = data.positionValue.home3DPos.X.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "OFFSET_X", value, Constants.MARKING_INI_FILE); // Home offset X

        //            value = data.positionValue.home3DPos.Y.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "OFFSET_Y", value, Constants.MARKING_INI_FILE); // Home offset Y

        //            value = data.positionValue.home3DPos.Z.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "OFFSET_Z", value, Constants.MARKING_INI_FILE); // Home offset Z

        //            value = data.positionValue.rasterSP.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "RASTERSP", value, Constants.MARKING_INI_FILE); // Raster start point

        //            value = data.positionValue.rasterEP.ToString();
        //            Util.WritePrivateProfileValue("POSITION", "RASTEREP", value, Constants.MARKING_INI_FILE); // Raster end point

        //            value = data.headValue.angleDegree.ToString();
        //            Util.WritePrivateProfileValue("SENSOR", "ANGLEDEGREE", value, Constants.MARKING_INI_FILE); // Raster end point
        //        }

        //        //if ((saveFlag & 0x02) != 0)
        //        //{
        //        //    value = data.parkingU.ToString();
        //        //    Util.WritePrivateProfileValue("CONFIG", "PARKING", value, Constants.SCANNER_INI_FILE); // load accel

        //        //    value = data.max_u.ToString();
        //        //    Util.WritePrivateProfileValue("CONFIG", "MAX_U", value, Constants.SCANNER_INI_FILE); // load accel

        //        //    value = data.home_u.ToString();
        //        //    Util.WritePrivateProfileValue("CONFIG", "HOME_U", value, Constants.SCANNER_INI_FILE); // load accel
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        //private void GetPatternData(ref PatternValueEx data)
        //{
        //    string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "GetPatternData";// MethodBase.GetCurrentMethod().Name;

        //    try
        //    {
        //        data = (PatternValueEx)orgData.Clone();

        //        if (cbxPatternList.SelectedIndex >= 0)
        //            data.name = cbxPatternList.SelectedItem.ToString();
        //        else
        //            data.name = "Pattern_ON";

        //        //Font Value
        //        if (cbxFontName.SelectedIndex >= 0)
        //            data.fontValue.fontName = cbxFontName.SelectedItem.ToString();
        //        else
        //            data.fontValue.fontName = "5X7";

        //        double.TryParse(txtHeight.Text, out data.fontValue.height);
        //        double.TryParse(txtWidth.Text, out data.fontValue.width);
        //        double.TryParse(txtPitch.Text, out data.fontValue.pitch);
        //        double.TryParse(txtAngle.Text, out data.fontValue.rotateAngle);
        //        short.TryParse(lblStepLength.Content.ToString(), out data.headValue.stepLength);
        //        //short.TryParse(txtStrike.Text, out data.fontValue.strikeCount);


        //        short.TryParse(Fast_value_Ispeed.Text, out data.speedValue.initSpeed4Fast);
        //        short.TryParse(Fast_value_Tspeed.Text, out data.speedValue.targetSpeed4Fast);
        //        short.TryParse(Fast_value_Accel.Text, out data.speedValue.accelSpeed4Fast);
        //        short.TryParse(Fast_value_Decel.Text, out data.speedValue.decelSpeed4Fast);

        //        if (data.laserValue.density != 1)
        //        {
        //            short.TryParse(Mark_value_Ispeed.Text, out data.speedValue.initSpeed4MarkV);
        //            short.TryParse(Mark_value_Tspeed.Text, out data.speedValue.targetSpeed4MarkV);
        //            short.TryParse(Mark_value_Accel.Text, out data.speedValue.accelSpeed4MarkV);
        //            short.TryParse(Mark_value_Decel.Text, out data.speedValue.decelSpeed4MarkV);
        //        }
        //        else
        //        {
        //            short.TryParse(Mark_value_Ispeed.Text, out data.speedValue.initSpeed4MarkR);
        //            short.TryParse(Mark_value_Tspeed.Text, out data.speedValue.targetSpeed4MarkR);
        //            short.TryParse(Mark_value_Accel.Text, out data.speedValue.accelSpeed4MarkR);
        //            short.TryParse(Mark_value_Decel.Text, out data.speedValue.decelSpeed4MarkR);
        //        }

        //        short.TryParse(Home_value_Ispeed.Text, out data.speedValue.initSpeed4Home);
        //        short.TryParse(Home_value_Tspeed.Text, out data.speedValue.targetSpeed4Home);
        //        short.TryParse(Home_value_Accel.Text, out data.speedValue.accelSpeed4Home);
        //        short.TryParse(Home_value_Decel.Text, out data.speedValue.decelSpeed4Home);

        //        short.TryParse(Measure_value_Ispeed.Text, out data.speedValue.initSpeed4Measure);
        //        short.TryParse(Measure_value_Tspeed.Text, out data.speedValue.targetSpeed4Measure);
        //        short.TryParse(Measure_value_Accel.Text, out data.speedValue.accelSpeed4Measure);
        //        short.TryParse(Measure_value_Decel.Text, out data.speedValue.decelSpeed4Measure);

        //        short.TryParse(txtSolOnTime.Text, out data.speedValue.solOnTime);
        //        short.TryParse(txtSolOffTime.Text, out data.speedValue.solOffTime);
        //        short.TryParse(txtDWellTime.Text, out data.speedValue.dwellTime);

        //        //double.TryParse(txtStart_X.Text, out data.positionValue.center3DPos.X);
        //        //double.TryParse(txtStart_Y.Text, out data.positionValue.start_Y);
        //        //double.TryParse(txtStart_Z.Text, out data.positionValue.start_X);

        //        //double.TryParse(txtPark_X.Text, out data.positionValue.park_X);
        //        //double.TryParse(txtPark_Y.Text, out data.positionValue.park_Y);
        //        //double.TryParse(txtPark_Z.Text, out data.positionValue.park_Z);

        //        //double.TryParse(txtHome_X.Text, out data.positionValue.offset_X);
        //        //double.TryParse(txtOffset_Y.Text, out data.positionValue.offset_Y);
        //        //double.TryParse(txtOffset_Z.Text, out data.positionValue.offset_Z);

        //        //short.TryParse(lblMAX_X.Content.ToString(), out data.headValue.max_x);
        //        //short.TryParse(lblMAX_Y.Content.ToString(), out data.headValue.max_y);
        //        //short.TryParse(lblMAX_Z.Content.ToString(), out data.headValue.max_z);


        //        short.TryParse(txtProfileNum.Text, out data.laserValue.waveformNum);
        //        short.TryParse(txtCleanProfileNum.Text, out data.laserValue.waveformClean);
        //        double.TryParse(txtCleanPosition.Text, out data.laserValue.cleanPosition);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        //private void Home_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Home_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Home_value_Decel.Text = n.ToString();
        //}

        //private void Home_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Home_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Home_value_Accel.Text = n.ToString();
        //}

        //private void Home_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Home_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Home_value_Tspeed.Text = n.ToString();
        //}

        //private void Home_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Home_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Home_value_Ispeed.Text = n.ToString();
        //}

        //private void Fast_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Fast_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Fast_value_Ispeed.Text = n.ToString();
        //}

        //private void Fast_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Fast_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Fast_value_Tspeed.Text = n.ToString();
        //}

        //private void Fast_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Fast_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Fast_value_Accel.Text = n.ToString();
        //}

        //private void Fast_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Fast_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Fast_value_Decel.Text = n.ToString();
        //}

        //private void Mark_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Mark_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Mark_value_Ispeed.Text = n.ToString();
        //}

        //private void Mark_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Mark_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Mark_value_Tspeed.Text = n.ToString();
        //}

        //private void Mark_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Mark_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Mark_value_Accel.Text = n.ToString();
        //}

        //private void Mark_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Mark_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Mark_value_Decel.Text = n.ToString();
        //}

        //private void Measure_Decel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Measure_Decel.Value);
        //    int n = Convert.ToInt32(x);
        //    Measure_value_Decel.Text = n.ToString();
        //}

        //private void Measure_Accel_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Measure_Accel.Value);
        //    int n = Convert.ToInt32(x);
        //    Measure_value_Accel.Text = n.ToString();
        //}

        //private void Measure_Tspeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Measure_Tspeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Measure_value_Tspeed.Text = n.ToString();
        //}

        //private void Measure_Ispeed_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        //{
        //    Decimal x = Convert.ToDecimal(Measure_Ispeed.Value);
        //    int n = Convert.ToInt32(x);
        //    Measure_value_Ispeed.Text = n.ToString();
        //}

        //private void DisplayPatternData(PatternValueEx pat)
        //{
        //    string className = "SetControllerWindow2";
        //    string funcName = "DisplayPatternData";
        //    try
        //    {
        //        cbxPatternList.SelectedItem = pat.name;
        //        cbxFontName.SelectedItem = pat.fontValue.fontName;

        //        //
        //        txtHeight.Text = pat.fontValue.height.ToString("F2");
        //        txtWidth.Text = pat.fontValue.width.ToString("F2");
        //        txtPitch.Text = pat.fontValue.pitch.ToString("F2");
        //        txtAngle.Text = pat.fontValue.rotateAngle.ToString("F2");
        //        //lblMAX_X.Content = pat.headValue.max_x.ToString();
        //        //lblMAX_Y.Content = pat.headValue.max_y.ToString();
        //        //lblMAX_Z.Content = pat.headValue.max_z.ToString();

        //        //
        //        //if(pat.laserValue.density != 1)
        //        //{
        //        //    Mark_Ispeed.Value = pat.speedValue.initSpeed4MarkV;
        //        //    Mark_value_Ispeed.Text = pat.speedValue.initSpeed4MarkV.ToString();

        //        //    Mark_Tspeed.Value = pat.speedValue.targetSpeed4MarkV;
        //        //    Mark_value_Tspeed.Text = pat.speedValue.targetSpeed4MarkV.ToString();

        //        //    Mark_Accel.Value = pat.speedValue.accelSpeed4MarkV;
        //        //    Mark_value_Accel.Text = pat.speedValue.accelSpeed4MarkV.ToString();

        //        //    Mark_Decel.Value = pat.speedValue.decelSpeed4MarkV;
        //        //    Mark_value_Decel.Text = pat.speedValue.decelSpeed4MarkV.ToString();
        //        //}
        //        //else
        //        //{
        //        //    Mark_Ispeed.Value = pat.speedValue.initSpeed4MarkR;
        //        //    Mark_value_Ispeed.Text = pat.speedValue.initSpeed4MarkR.ToString();

        //        //    Mark_Tspeed.Value = pat.speedValue.targetSpeed4MarkR;
        //        //    Mark_value_Tspeed.Text = pat.speedValue.targetSpeed4MarkR.ToString();

        //        //    Mark_Accel.Value = pat.speedValue.accelSpeed4MarkR;
        //        //    Mark_value_Accel.Text = pat.speedValue.accelSpeed4MarkR.ToString();

        //        //    Mark_Decel.Value = pat.speedValue.decelSpeed4MarkR;
        //        //    Mark_value_Decel.Text = pat.speedValue.decelSpeed4MarkR.ToString();
        //        //}

        //        ////
        //        //Fast_Ispeed.Value = pat.speedValue.initSpeed4Fast;
        //        //Fast_value_Ispeed.Text = pat.speedValue.initSpeed4Fast.ToString();

        //        //Fast_Tspeed.Value = pat.speedValue.targetSpeed4Fast;
        //        //Fast_value_Tspeed.Text = pat.speedValue.targetSpeed4Fast.ToString();

        //        //Fast_Accel.Value = pat.speedValue.accelSpeed4Fast;
        //        //Fast_value_Accel.Text = pat.speedValue.accelSpeed4Fast.ToString();

        //        //Fast_Decel.Value = pat.speedValue.decelSpeed4Fast;
        //        //Fast_value_Decel.Text = pat.speedValue.decelSpeed4Fast.ToString();

        //        //Home_Ispeed.Value = pat.speedValue.initSpeed4Home;
        //        //Home_value_Ispeed.Text = pat.speedValue.initSpeed4Home.ToString();

        //        //Home_Tspeed.Value = pat.speedValue.targetSpeed4Home;
        //        //Home_value_Tspeed.Text = pat.speedValue.targetSpeed4Home.ToString();

        //        //Home_Accel.Value = pat.speedValue.accelSpeed4Home;
        //        //Home_value_Accel.Text = pat.speedValue.accelSpeed4Home.ToString();

        //        //Home_Decel.Value = pat.speedValue.decelSpeed4Home;
        //        //Home_value_Decel.Text = pat.speedValue.decelSpeed4Home.ToString();

        //        //Measure_Ispeed.Value = pat.speedValue.initSpeed4Measure;
        //        //Measure_value_Ispeed.Text = pat.speedValue.initSpeed4Measure.ToString();

        //        //Measure_Tspeed.Value = pat.speedValue.targetSpeed4Measure;
        //        //Measure_value_Tspeed.Text = pat.speedValue.targetSpeed4Measure.ToString();

        //        //Measure_Accel.Value = pat.speedValue.accelSpeed4Measure;
        //        //Measure_value_Accel.Text = pat.speedValue.accelSpeed4Measure.ToString();

        //        //Measure_Decel.Value = pat.speedValue.decelSpeed4Measure;
        //        //Measure_value_Decel.Text = pat.speedValue.decelSpeed4Measure.ToString();

        //        if (pat.laserValue.density != 1)
        //        {
        //            lblSpeedMarkSpeedInitialValue.Text = pat.speedValue.initSpeed4MarkV.ToString();
        //            lblSpeedMarkSpeedTargetValue.Text = pat.speedValue.targetSpeed4MarkV.ToString();
        //            lblSpeedMarkSpeedAccelValue.Text = pat.speedValue.accelSpeed4MarkV.ToString();
        //            lblSpeedMarkSpeedDecelValue.Text = pat.speedValue.decelSpeed4MarkV.ToString();
        //        }
        //        else
        //        {
        //            lblSpeedMarkSpeedInitialValue.Text = pat.speedValue.initSpeed4MarkR.ToString();
        //            lblSpeedMarkSpeedTargetValue.Text = pat.speedValue.targetSpeed4MarkR.ToString();
        //            lblSpeedMarkSpeedAccelValue.Text = pat.speedValue.accelSpeed4MarkR.ToString();
        //            lblSpeedMarkSpeedDecelValue.Text = pat.speedValue.decelSpeed4MarkR.ToString();
        //        }

        //        //
        //        lblSpeedFastSpeedInitialValue.Text = pat.speedValue.initSpeed4Fast.ToString();
        //        lblSpeedFastSpeedAccelValue.Text = pat.speedValue.targetSpeed4Fast.ToString();
        //        lblSpeedFastSpeedDecelValue.Text = pat.speedValue.accelSpeed4Fast.ToString();
        //        lblSpeedFastSpeedTargetValue.Text = pat.speedValue.decelSpeed4Fast.ToString();

        //        lblSpeedHomeSpeedInitialValue.Text = pat.speedValue.initSpeed4Fast.ToString();
        //        lblSpeedHomeSpeedAccelValue.Text = pat.speedValue.targetSpeed4Fast.ToString();
        //        lblSpeedHomeSpeedDecelValue.Text = pat.speedValue.accelSpeed4Fast.ToString();
        //        lblSpeedHomeSpeedTargetValue.Text = pat.speedValue.decelSpeed4Fast.ToString();

        //        lblSpeedMeasureSpeedInitialValue.Text = pat.speedValue.initSpeed4Measure.ToString();
        //        lblSpeedMeasureSpeedAccelValue.Text = pat.speedValue.targetSpeed4Measure.ToString();
        //        lblSpeedMeasureSpeedDecelValue.Text = pat.speedValue.accelSpeed4Measure.ToString();
        //        lblSpeedMeasureSpeedTargetValue.Text = pat.speedValue.decelSpeed4Measure.ToString();

        //        txtSolOnTime.Text = pat.speedValue.solOnTime.ToString();
        //        txtSolOffTime.Text = pat.speedValue.solOffTime.ToString();
        //        txtDWellTime.Text = pat.speedValue.dwellTime.ToString();


        //        //txtStart_X.Text = pat.startX.ToString("F2");
        //        //txtStart_Y.Text = pat.startY.ToString("F2");
        //        //txtStart_Z.Text = pat.startZ.ToString("F2");

        //        txtStart_X.Text = pat.positionValue.center3DPos.X.ToString("F2");
        //        txtStart_Y.Text = pat.positionValue.center3DPos.Y.ToString("F2");
        //        txtStart_Z.Text = pat.positionValue.center3DPos.Z.ToString("F2");

        //        txtPark_X.Text = pat.positionValue.park3DPos.X.ToString("F2");
        //        txtPark_Y.Text = pat.positionValue.park3DPos.Y.ToString("F2");
        //        txtPark_Z.Text = pat.positionValue.park3DPos.Z.ToString("F2");

        //        txtHome_X.Text = pat.positionValue.home3DPos.X.ToString("F2");
        //        txtOffset_Y.Text = pat.positionValue.home3DPos.Y.ToString("F2");
        //        txtOffset_Z.Text = pat.positionValue.home3DPos.Z.ToString("F2");

        //        lblMAX_X.Content = pat.headValue.max_X.ToString();
        //        lblMAX_Y.Content = pat.headValue.max_Y.ToString();
        //        lblMAX_Z.Content = pat.headValue.max_Z.ToString();

        //        lblStepLength.Content = pat.headValue.stepLength;

        //        txtProfileNum.Text = pat.laserValue.waveformNum.ToString();
        //        txtCleanProfileNum.Text = pat.laserValue.waveformClean.ToString();
        //        txtCleanPosition.Text = pat.laserValue.cleanPosition.ToString("F2");
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}













        //public void DisplayPatternFontValue(byte byheadType, FontValue fontValue)
        //{
        //    //string patternfile = Constants.PATTERN_PATH + name + ".ini";
        //    string className = "SetControllerWindow2";
        //    string funcName = "DisplayPatternFontValue";

        //    try
        //    {
        //        if (cbxFontName.Items.Contains(fontValue.fontName) == true)
        //            cbxFontName.SelectedItem = fontValue.fontName;
        //        else
        //            cbxFontName.SelectedIndex = 0;

        //        txtHeight.Text = fontValue.height.ToString("F2");
        //        txtWidth.Text = fontValue.width.ToString("F2");
        //        txtPitch.Text = fontValue.pitch.ToString("F2");
        //        txtAngle.Text = fontValue.rotateAngle.ToString("F2");
        //        txtThickness.Text = fontValue.thickness.ToString("F2");


        //        //Util.GetPrivateProfileValue("FONT", "NAME", "OCR", ref font.fontName, patternfile); // load FONT
        //        //font.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4, patternfile);
        //        //font.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7, patternfile);
        //        //font.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6, patternfile);
        //        //font.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternfile);
        //        //font.strikeCount = (short)Util.GetPrivateProfileValueUINT("FONT", "STRIKECOUNT", 0, patternfile);
        //        //font.thickness = (double)Util.GetPrivateProfileValueDouble("FONT", "THICKNESS", 0.5, patternfile);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}


        //public async Task<ITNTResponseArgs> Start_TEXT3(string vin, PatternValueEx pattern)    // Making Fire/Clean data by TM SHIN
        //{
        //    string className = "SetControllerWindow2";
        //    string funcName = "Start_TEXT3";

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    int NoVin = 0;

        //    Vector3D SP0 = new Vector3D();
        //    Vector3D SP = new Vector3D();
        //    Vector3D CP = new Vector3D();

        //    Vector3D VectorNormal = new Vector3D();
        //    Vector3D VectorRot = new Vector3D();
        //    List<Vector3D> Rev_Point = new List<Vector3D>();

        //    double Step_W;
        //    double Step_H;
        //    string value = "";
        //    byte headType = 0;
        //    VinNoInfo vininfo = new VinNoInfo();
        //    //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
        //    List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
        //    double fontsizeX = 0, fontsizeY = 0, shiftVal = 0;
        //    double fontsizeX2 = 0, fontsizeY2 = 0, shiftVal2 = 0;
        //    double cleanPosition = 0;
        //    string errCode = "";
        //    double totWidth = 0;
        //    double R11, R12, R13, R21, R22, R23, R31, R32, R33;
        //    int i, j;
        //    int idx = 0;

        //    Vector3D M1 = new Vector3D();                                   // for fire data mm
        //    Vector3D M2 = new Vector3D();                                   // for clean data mm
        //    Vector3D M = new Vector3D();
        //    Vector3D C = new Vector3D();
        //    //byte fontdirection = 0;

        //    try
        //    {
        //        NoVin = vin.Length;
        //        if (NoVin <= 0)
        //        {
        //            retval.execResult = -1;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN LENGTH IS INVALID (" + NoVin.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        totWidth = (NoVin - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

        //        Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
        //        byte.TryParse(value, out headType);

        //        //Util.GetPrivateProfileValue("OPTION", "FONTDIRECTION", "0", ref value, Constants.PARAMS_INI_FILE);
        //        //byte.TryParse(value, out fontdirection);


        //        vininfo.vinNo = vin;
        //        vininfo.fontName = pattern.fontValue.fontName;
        //        vininfo.width = pattern.fontValue.width;
        //        vininfo.height = pattern.fontValue.height;
        //        vininfo.pitch = pattern.fontValue.pitch;
        //        vininfo.thickness = pattern.fontValue.thickness;
        //        ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 0, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref errCode);

        //        m_currCMD = (byte)'S';
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SolOnOffTime ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        m_currCMD = (byte)'d';
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(pattern.speedValue.dwellTime);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dwellTimeSet ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        if (currMarkInfo.checkdata.bReady == false)
        //        {
        //            CheckAreaData chkdata = new CheckAreaData();
        //            chkdata = await Range_Test2(vin, pattern);
        //            if (chkdata.execResult != 0)
        //            {
        //                return retval;
        //            }
        //            else
        //                currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();

        //            //retval = await Range_Test(vin, pattern);
        //            //if(retval.execResult != 0)
        //            //{
        //            //    return retval;
        //            //}
        //        }

        //        CP = pattern.positionValue.center3DPos;
        //        cleanPosition = pattern.laserValue.cleanPosition;
        //        cleanPosition += CP.Z;

        //        SP0.X = totWidth / 2;
        //        SP0.Y = pattern.fontValue.height / 2;
        //        SP0.Z = 0;
        //        SP = CP - SP0;
        //        SP.Z = 0;

        //        VectorNormal = currMarkInfo.checkdata.NormalDir;
        //        VectorRot.X = -VectorNormal.Y;
        //        VectorRot.Y = VectorNormal.X;
        //        VectorRot.Z = 0;

        //        double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);
        //        if (sqXY != 0)
        //        {
        //            VectorRot.X /= sqXY;
        //            VectorRot.Y /= sqXY;
        //        }

        //        // Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
        //        bool skipRot = false;
        //        double cosValue = VectorNormal.Z / VectorNormal.Length;
        //        double sinValue = Math.Sqrt(1.0d - cosValue * cosValue);

        //        if (cosValue > 0.9999986111)
        //        {      // 0.1 mm difference between 60mm
        //            skipRot = true;
        //            R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
        //        }
        //        else
        //        {
        //            R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
        //            R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
        //            R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
        //            R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
        //            R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
        //            R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
        //            R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
        //            R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
        //            R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
        //        }
        //        /////
        //        Step_W = pattern.fontValue.width / (fontsizeX - 1.0);
        //        Step_H = pattern.fontValue.height / (fontsizeY - 1.0);

        //        FontData4Send[,,] RasterData = new FontData4Send[NoVin + 1, (int)(fontsizeY + 0.5), (int)(fontsizeX + 0.5)];     // BLU
        //        FontData4Send[,,] AllClrData = new FontData4Send[NoVin + 1, (int)(fontsizeY + 0.5), (int)(fontsizeX + 0.5)];

        //        currMarkInfo.senddata.sendDataFire.Clear();
        //        currMarkInfo.senddata.sendDataClean.Clear();

        //        ImageProcessManager.GetStartPointLinear(NoVin, CP, SP, pattern.fontValue.pitch, pattern.fontValue.rotateAngle, ref Rev_Point);

        //        Vector3D[] LeftRightSP = new Vector3D[2];
        //        LeftRightSP[0] = ImageProcessManager.Rotate_Point2(SP.X - pattern.positionValue.rasterSP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
        //        LeftRightSP[1] = ImageProcessManager.Rotate_Point2(SP.X + totWidth + pattern.positionValue.rasterEP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
        //        List<FontDataClass> lineData = new List<FontDataClass>();
        //        List<FontDataClass> lineDataClean = new List<FontDataClass>();
        //        ImageProcessManager.GetFontDataOneEx('^', pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);
        //        for (i = 0; i < Rev_Point.Count; i++)
        //        {
        //            if (vin.Substring(i, 1) != " ")      //Space Skip
        //            {
        //                //lineData = MyData[i];
        //                lineData = fontData[i];
        //                FontDataClass fd = new FontDataClass();
        //                for (j = 0; j < lineData.Count; j++)
        //                {
        //                    fd = (FontDataClass)lineData[j].Clone();
        //                    // ABS mm
        //                    M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
        //                    M2.X = Rev_Point[i].X + Math.Round(fd.vector3d.X) * Step_W;

        //                    // Font offset compensation
        //                    //M1.Y = Rev_Point[i].Y + (fd.vector3d.Y - shiftVal) * Step_H;
        //                    M1.Y = Rev_Point[i].Y + fd.vector3d.Y * Step_H;
        //                    M2.Y = M1.Y;
        //                    M1.Z = M2.Z = SP.Z;

        //                    M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M1.Z = SP.Z;
        //                    M2 = ImageProcessManager.Rotate_Point2(M2.X, M2.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M2.Z = SP.Z;

        //                    // TM SHIN
        //                    M1.X -= CP.X; M1.Y -= CP.Y;
        //                    M2.X -= CP.X; M2.Y -= CP.Y;

        //                    M = (skipRot == true) ? M1 : getRodrigueRotation(M1);
        //                    C = (skipRot == true) ? M2 : getRodrigueRotation(M2);

        //                    M.X += CP.X; M.Y += CP.Y;
        //                    C.X += CP.X; C.Y += CP.Y;
        //                    double Mt = M.Z;
        //                    M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
        //                    C.Z = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

        //                    //Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3},{5,7:F3}", idx, M.X, M.Y, M.Z, C.X, C.Z));

        //                    // Change to BLU(Unit 0.01mm)
        //                    M = M * pattern.headValue.stepLength;
        //                    C = C * pattern.headValue.stepLength;
        //                    //M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length;
        //                    //C.X *= (double)Mode_File.Step_Length; C.Y *= (double)Mode_File.Step_Length; C.Z *= (double)Mode_File.Step_Length;

        //                    FontData4Send font4Send = new FontData4Send();

        //                    font4Send.cN = (byte)i; font4Send.fN = (byte)j;
        //                    font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
        //                    font4Send.mC = (UInt16)(C.Z + 0.5);
        //                    font4Send.mI = (UInt16)(C.X + 0.5);

        //                    if (pattern.laserValue.density == 1)
        //                    {
        //                        font4Send.mF = 0;
        //                        RasterData[i, (int)fd.vector3d.Y, (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
        //                        //RasterData[i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
        //                        //Debug.WriteLine(String.Format("RASTER-({0},{1},{2}):{3}/{4}/{5}/{6}/{7}/{8}/{9}/{10}", i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X), font4Send.cN, font4Send.fN, font4Send.mC, font4Send.mF, font4Send.mI, font4Send.mX, font4Send.mY, font4Send.mZ));
        //                    }
        //                    else
        //                    {
        //                        var m_font = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
        //                        currMarkInfo.senddata.sendDataFire.Add(m_font);
        //                        //var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
        //                        //currMarkInfo.senddata.sendDataClean.Add(m_clean);
        //                    }

        //                    idx++;
        //                }


        //                idx = 0;

        //                lineData = fontData[i];
        //                if (pattern.laserValue.charClean == 0)
        //                    lineData = lineDataClean.ToList();
        //                //FontDataClass fdClean = new FontDataClass();
        //                fd.Clean();
        //                for (j = 0; j < lineData.Count; j++)
        //                {
        //                    fd = (FontDataClass)lineData[j].Clone();
        //                    // ABS mm
        //                    M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
        //                    M2.X = Rev_Point[i].X + Math.Round(fd.vector3d.X) * Step_W;

        //                    // Font offset compensation
        //                    M1.Y = Rev_Point[i].Y + fd.vector3d.Y * Step_H;
        //                    //M1.Y = Rev_Point[i].Y + (fd.vector3d.Y - shiftVal) * Step_H;
        //                    M2.Y = M1.Y;
        //                    M1.Z = M2.Z = SP.Z;

        //                    M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M1.Z = SP.Z;
        //                    M2 = ImageProcessManager.Rotate_Point2(M2.X, M2.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M2.Z = SP.Z;

        //                    // TM SHIN
        //                    M1.X -= CP.X; M1.Y -= CP.Y;
        //                    M2.X -= CP.X; M2.Y -= CP.Y;

        //                    M = (skipRot == true) ? M1 : getRodrigueRotation(M1);
        //                    C = (skipRot == true) ? M2 : getRodrigueRotation(M2);

        //                    M.X += CP.X; M.Y += CP.Y;
        //                    C.X += CP.X; C.Y += CP.Y;
        //                    double Mt = M.Z;
        //                    M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
        //                    C.Z = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

        //                    //Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3},{5,7:F3}", idx, M.X, M.Y, M.Z, C.X, C.Z));

        //                    // Change to BLU(Unit 0.01mm)
        //                    M = M * pattern.headValue.stepLength;
        //                    C = C * pattern.headValue.stepLength;
        //                    //M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length;
        //                    //C.X *= (double)Mode_File.Step_Length; C.Y *= (double)Mode_File.Step_Length; C.Z *= (double)Mode_File.Step_Length;

        //                    FontData4Send font4Send = new FontData4Send();

        //                    font4Send.cN = (byte)i; font4Send.fN = (byte)j;
        //                    font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
        //                    font4Send.mC = (UInt16)(C.Z + 0.5);
        //                    font4Send.mI = (UInt16)(C.X + 0.5);

        //                    if (pattern.laserValue.density == 1)
        //                    {
        //                        font4Send.mF = 0;
        //                        AllClrData[i, (int)fd.vector3d.Y, (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
        //                        //AllClrData[i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
        //                        //Debug.WriteLine(String.Format("RASTER-({0},{1},{2}):{3}/{4}/{5}/{6}/{7}/{8}/{9}/{10}", i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X), font4Send.cN, font4Send.fN, font4Send.mC, font4Send.mF, font4Send.mI, font4Send.mX, font4Send.mY, font4Send.mZ));
        //                    }
        //                    else
        //                    {
        //                        var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
        //                        currMarkInfo.senddata.sendDataClean.Add(m_clean);
        //                        //var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
        //                        //currMarkInfo.senddata.sendDataClean.Add(m_clean);
        //                    }

        //                    idx++;
        //                }
        //            }
        //        }





        //        //Debug.WriteLine("RASTER DATA");
        //        //for (i = 0; i < vin.Length; i++)
        //        //{
        //        //    for(j = 0; j < fontsizeY; j++)
        //        //        for(int k = 0; k < fontsizeX; k++)
        //        //            Debug.WriteLine(String.Format("RASTER[{0},{1},{2}] = {3},{4},{5},{6},{7}", i, j, k, RasterData[i, j, k].mX, RasterData[i, j, k].mY, RasterData[i, j, k].mZ, RasterData[i, j, k].mI, RasterData[i, j, k].mC));
        //        //}



        //        idx = 0;

        //        if (pattern.laserValue.density == 1)       // Dot Firing
        //        {
        //            // Calculate No of Dot point
        //            ushort[] NoPoints = new ushort[(int)(fontsizeY + 0.5)];
        //            ushort[] NoPointsC = new ushort[(int)(fontsizeY + 0.5)];

        //            for (int y = 0; y < (int)(fontsizeY + 0.5); y++)
        //            {
        //                for (i = 0; i < NoVin; i++)
        //                {
        //                    for (int x = 0; x < (int)(fontsizeX + 0.5); x++)
        //                    {
        //                        if (RasterData[i, y, x] != null) NoPoints[y]++;         // Data Number of fire data
        //                    }
        //                }
        //            }

        //            //
        //            // Make Jump/Start data
        //            for (i = 0; i < (int)(fontsizeY + 0.5); i++)
        //            {
        //                for (j = 0; j < 2; j++)         // Jump/Start XXXXX
        //                {
        //                    M1.X = LeftRightSP[j].X;
        //                    M1.Y = LeftRightSP[j].Y + (double)i * Step_H;
        //                    M1.Z = SP.Z;

        //                    M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, LeftRightSP[j].X, LeftRightSP[j].Y, pattern.fontValue.rotateAngle);// double.Parse(Angle));
        //                    M1.Z = SP.Z;

        //                    M1.X -= CP.X; M1.Y -= CP.Y;

        //                    M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

        //                    M.X += CP.X; M.Y += CP.Y;
        //                    double Mt = M.Z;
        //                    M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;       // Fire Z Axis
        //                    C.Z = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Z Axis
        //                    //Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3}", idx, M.X, M.Y, M.Z, C.Z));

        //                    // Change to BLU(Unit 0.01mm)
        //                    M = M * pattern.headValue.stepLength;
        //                    C.Z *= pattern.headValue.stepLength;

        //                    //M.X *= (double)pattern.headValue.stepLength; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length; C.Z *= (double)Mode_File.Step_Length;

        //                    FontData4Send FontData = new FontData4Send();

        //                    FontData.cN = (byte)i; FontData.fN = (byte)j;
        //                    FontData.mX = (UInt16)(M.X + 0.5); FontData.mY = (UInt16)(M.Y + 0.5); FontData.mZ = (UInt16)(M.Z + 0.5);
        //                    FontData.mC = (UInt16)(C.Z + 0.5);
        //                    FontData.mI = 0;
        //                    FontData.mF = 0;

        //                    RasterData[NoVin, i, j] = (FontData4Send)FontData.Clone();

        //                    idx++;
        //                }
        //            }

        //            idx = 0;
        //            for (int y = 0; y < (int)(fontsizeY + 0.5); y++)
        //                for (i = 0; i < NoVin; i++)
        //                    for (int x = 0; x < (int)(fontsizeX + 0.5); x++)
        //                        if (AllClrData[i, y, x] != null) NoPointsC[y]++;    // Data Number of clean data


        //            if (pattern.headValue.spatterType == 0)
        //            {
        //                if (pattern.fontValue.rotateAngle == 0.0)
        //                {   // 0 degree for clean data
        //                    if (pattern.laserValue.combineFireClean != 0)
        //                    {
        //                        //
        //                        // Make firing | clean Data for 0 degree, back spatter [1FC]
        //                        StringBuilder sb = new StringBuilder();
        //                        ushort x2 = 0, y2 = 0, c2 = 0;

        //                        bool DirRight = true;
        //                        for (int y = 0; y < (int)(fontsizeY + 0.5); y++)
        //                        {
        //                            if (DirRight == true)
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, NoPoints[y]);
        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < (int)(fontsizeX + 0.5); x++)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, NoPoints[y]);
        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX + 0.5) - 1; x >= 0; x--)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            DirRight = !DirRight;

        //                            //
        //                            //  Make Cleaning Data for 0 degree, back spatter [1C]
        //                            if (NoPointsC[y] > 0)
        //                            {
        //                                if (DirRight == false)
        //                                {
        //                                    x2 = (ushort)(RasterData[NoVin, y, 1].mX);
        //                                    y2 = (ushort)(RasterData[NoVin, y, 1].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    c2 = (ushort)(RasterData[NoVin, y, 1].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                    x2 = (ushort)(RasterData[NoVin, y, 0].mX);
        //                                    y2 = (ushort)(RasterData[NoVin, y, 0].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    c2 = (ushort)(RasterData[NoVin, y, 0].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                    for (i = NoVin - 1; i >= 0; i--)
        //                                        for (int x = (int)(fontsizeX + 0.5) - 1; x >= 0; x--)
        //                                        {
        //                                            if (AllClrData[i, y, x] != null)
        //                                            {
        //                                                sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                            }
        //                                        }
        //                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                    //Mode_File.SendData.Add(sb.ToString());
        //                                    //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                    sb.Length = 0;
        //                                }
        //                                else
        //                                {
        //                                    x2 = (RasterData[NoVin, y, 0].mX);
        //                                    y2 = (RasterData[NoVin, y, 0].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    c2 = (RasterData[NoVin, y, 0].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                    x2 = (RasterData[NoVin, y, 1].mX);
        //                                    y2 = (RasterData[NoVin, y, 1].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    c2 = (RasterData[NoVin, y, 1].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                    for (i = 0; i < NoVin; i++)
        //                                        for (int x = 0; x < (int)(fontsizeX + 0.5); x++)
        //                                        {
        //                                            if (AllClrData[i, y, x] != null)
        //                                            {
        //                                                sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                            }
        //                                        }
        //                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                    //Mode_File.SendData.Add(sb.ToString());
        //                                    //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                    sb.Length = 0;
        //                                }
        //                                DirRight = !DirRight;
        //                            }   // NoPointsC
        //                            else
        //                            {
        //                                currMarkInfo.checkdata.TwoLineDisplay = false;
        //                                //Mode_File.TwoLineDisplay = false;
        //                            }
        //                        }
        //                    }
        //                    else  // Fire & Clean
        //                    {
        //                        //
        //                        // Make firing Data for 0 degree, back spatter [1F]
        //                        StringBuilder sb = new StringBuilder();
        //                        bool DirRight = true;
        //                        for (int y = 0; y < (int)(fontsizeY + 0.5); y++)
        //                        {
        //                            if (DirRight == true)
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, NoPoints[y]);
        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < (int)(fontsizeX + 0.5); x++)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, NoPoints[y]);
        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX + 0.5) - 1; x >= 0; x--)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            DirRight = !DirRight;
        //                        }

        //                        //
        //                        //  Make Cleaning Data for 0 degree, back spatter [1C]
        //                        sb = new StringBuilder();
        //                        ushort x2 = 0, y2 = 0, c2 = 0;

        //                        if ((int)(fontsizeY + 0.5) > 10)  // Y Font Size > 10
        //                        {
        //                            currMarkInfo.checkdata.TwoLineDisplay = true;
        //                            //Mode_File.TwoLineDisplay = true;
        //                            for (int y = (int)(fontsizeY + 0.5) - 1; y >= 0; y--)
        //                            {
        //                                if (NoPointsC[y] > 0)
        //                                {
        //                                    if (DirRight == false)
        //                                    {
        //                                        x2 = (RasterData[NoVin, y, 1].mX);
        //                                        y2 = (RasterData[NoVin, y, 1].mY);
        //                                        y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                        c2 = (RasterData[NoVin, y, 1].mC);
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                        x2 = (RasterData[NoVin, y, 0].mX);
        //                                        y2 = (RasterData[NoVin, y, 0].mY);
        //                                        y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                        c2 = (RasterData[NoVin, y, 0].mC);
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                        for (i = NoVin - 1; i >= 0; i--)
        //                                            for (int x = (int)(fontsizeX + 0.5) - 1; x >= 0; x--)
        //                                            {
        //                                                if (AllClrData[i, y, x] != null)
        //                                                {
        //                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                                }
        //                                            }
        //                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                        //Mode_File.SendClean.Add(sb.ToString());
        //                                        //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                        sb.Length = 0;
        //                                    }
        //                                    else
        //                                    {
        //                                        x2 = (ushort)(RasterData[NoVin, y, 0].mX);
        //                                        y2 = (ushort)(RasterData[NoVin, y, 0].mY);
        //                                        y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                        c2 = (ushort)(RasterData[NoVin, y, 0].mC);
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                        x2 = (ushort)(RasterData[NoVin, y, 1].mX);
        //                                        y2 = (ushort)(RasterData[NoVin, y, 1].mY);
        //                                        y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                        c2 = (ushort)(RasterData[NoVin, y, 1].mC);
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                        for (i = 0; i < NoVin; i++)
        //                                            for (int x = 0; x < (int)(fontsizeX + 0.5); x++)
        //                                            {
        //                                                if (AllClrData[i, y, x] != null)
        //                                                {
        //                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                                }
        //                                            }
        //                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                        //Mode_File.SendClean.Add(sb.ToString());
        //                                        //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                        sb.Length = 0;
        //                                    }
        //                                    DirRight = !DirRight;
        //                                }   // NoPointsC
        //                                else
        //                                {
        //                                    currMarkInfo.checkdata.TwoLineDisplay = false;
        //                                    //Mode_File.TwoLineDisplay = false;
        //                                }
        //                            }
        //                        }
        //                        else  // Y Font Size <= 10
        //                        {
        //                            currMarkInfo.checkdata.TwoLineDisplay = false;
        //                            //Mode_File.TwoLineDisplay = false;
        //                            for (int y = (int)(fontsizeY + 0.5) - 1; y >= 0; y--)
        //                            {
        //                                if (NoPointsC[y] > 0)
        //                                {
        //                                    if (DirRight == false)
        //                                    {
        //                                        x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                        y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                        //y2 += CDelta;
        //                                        c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                        x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                        y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                        //y2 += CDelta;
        //                                        c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                        for (i = NoVin - 1; i >= 0; i--)
        //                                            for (int x = (int)(fontsizeX + 0.5) - 1; x >= 0; x--)
        //                                            {
        //                                                if (AllClrData[i, y, x] != null)
        //                                                {
        //                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                                }
        //                                            }
        //                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                        //Mode_File.SendClean.Add(sb.ToString());
        //                                        //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                        sb.Length = 0;
        //                                    }
        //                                    else
        //                                    {
        //                                        x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                        y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                        //y2 += CDelta;
        //                                        c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                        x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                        y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                        //y2 += CDelta;
        //                                        c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                        for (i = 0; i < NoVin; i++)
        //                                            for (int x = 0; x < (int)(fontsizeX + 0.5); x++)
        //                                            {
        //                                                if (AllClrData[i, y, x] != null)
        //                                                {
        //                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                                }
        //                                            }
        //                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                        //Mode_File.SendClean.Add(sb.ToString());
        //                                        //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                        sb.Length = 0;
        //                                    }
        //                                    DirRight = !DirRight;

        //                                }   // NoPointsC
        //                            }
        //                        }
        //                    } // CombineFireClean if-else
        //                    //for (int y = (int)(fontsizeY - 1); y >= 0; y -= 2)
        //                    //    for (i = 0; i < NoVin; i++)
        //                    //        for (int x = 0; x < fontsizeX; x++)
        //                    //        {
        //                    //            if (y > 0)
        //                    //            {
        //                    //                if ((RasterData[i, y, x] != null) || (RasterData[i, y - 1, x] != null)) NoPointsC[y]++;
        //                    //            }
        //                    //            else
        //                    //            {
        //                    //                if (RasterData[i, y, x] != null) NoPointsC[y]++;    // Data Number of clean data
        //                    //            }
        //                    //        }
        //                }
        //                else
        //                {   // 180 degree for clean data
        //                    for (int y = 0; y < fontsizeY; y += 2)
        //                        for (i = 0; i < NoVin; i++)
        //                            for (int x = 0; x < fontsizeX; x++)
        //                            {
        //                                if (y < fontsizeY - 1)
        //                                {
        //                                    if ((RasterData[i, y, x] != null) || (RasterData[i, y + 1, x] != null)) NoPointsC[y]++;
        //                                }
        //                                else
        //                                {
        //                                    if (RasterData[i, y, x] != null) NoPointsC[y]++;
        //                                }
        //                            }
        //                }

        //                //
        //                // Make Firing & cleanning Data for back spatter at 0 degree
        //                if (pattern.fontValue.rotateAngle == 0.0)
        //                {
        //                    //
        //                    // Make firing Data for 0 degree
        //                    StringBuilder sb = new StringBuilder();
        //                    for (int y = 0; y < fontsizeY; y++)
        //                    {
        //                        if (y % 2 == 0)
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, NoPoints[y]);
        //                            for (i = 0; i < NoVin; i++)
        //                                for (int x = 0; x < fontsizeX; x++)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        else
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, NoPoints[y]);
        //                            for (i = NoVin - 1; i >= 0; i--)
        //                                for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                    }

        //                    //
        //                    //  Make Cleaning Data for 0 degree
        //                    sb = new StringBuilder();
        //                    ushort x2 = 0, y2 = 0, c2 = 0;

        //                    if (fontsizeY > 10)  // Y Font Size > 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = true;
        //                        for (int y = (int)(fontsizeY - 1); y >= 0; y -= 2)
        //                        {
        //                            if ((y / 2) % 2 == 1)
        //                            {
        //                                x2 = (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y - 1, 1].mX) / 2);
        //                                y2 = (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y - 1, 1].mY) / 2);
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y - 1, 1].mC) / 2);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y - 1, 0].mX) / 2);
        //                                y2 = (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y - 1, 0].mY) / 2);
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y - 1, 0].mC) / 2);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (y > 0)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y - 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)(RasterData[i, y - 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y - 1, 0].mX) / 2) : (ushort)(RasterData[NoVin, y, 0].mX);
        //                                y2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y - 1, 0].mY) / 2) : (ushort)(RasterData[NoVin, y, 0].mY);  // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y - 1, 0].mC) / 2) : (ushort)(RasterData[NoVin, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y - 1, 1].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y - 1, 1].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY);  // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y - 1, 1].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (y > 0)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y - 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)(RasterData[i, y - 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }
        //                    else  // Y Font Size <= 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = false;
        //                        for (int y = (int)(fontsizeY - 1); y >= 0; y--)
        //                        {
        //                            if (y % 2 == 0)
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    // 
        //                    // Make firing data at 180 degree for back spatter
        //                    StringBuilder sb = new StringBuilder();
        //                    int yy = 0;
        //                    for (int y = (int)(fontsizeY - 1); y >= 0; y--, yy++)
        //                    {
        //                        if (yy % 2 == 1)
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, NoPoints[y]);
        //                            for (i = NoVin - 1; i >= 0; i--)
        //                                for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        else
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, NoPoints[y]);
        //                            for (i = 0; i < NoVin; i++)
        //                                for (int x = 0; x < fontsizeX; x++)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                    }

        //                    //
        //                    //  Make Cleaning Data for 180 degree
        //                    sb = new StringBuilder();
        //                    ushort x2 = 0, y2 = 0, c2 = 0;
        //                    if (fontsizeY > 10)  // Y Font Size > 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = true;
        //                        for (int y = 0; y < fontsizeY; y += 2, yy -= 2)
        //                        {
        //                            if ((yy / 2) % 2 == 0)
        //                            {   // even
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y + 1, 0].mX) / 2) : (ushort)(RasterData[NoVin, y, 0].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y + 1, 0].mY) / 2) : (ushort)(RasterData[NoVin, y, 0].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y + 1, 0].mC) / 2) : (ushort)(RasterData[NoVin, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y + 1, 1].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y + 1, 1].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y + 1, 1].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (y < fontsizeY - 1)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y + 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)(RasterData[i, y + 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {   // odd
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y + 1, 1].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y + 1, 1].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y + 1, 1].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y + 1, 0].mX) / 2) : (ushort)(RasterData[NoVin, y, 0].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y + 1, 0].mY) / 2) : (ushort)(RasterData[NoVin, y, 0].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y + 1, 0].mC) / 2) : (ushort)(RasterData[NoVin, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (y < fontsizeY - 1)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y + 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)(RasterData[i, y + 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }
        //                    else  // Y Font Size <= 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = false;
        //                        for (int y = 0; y < fontsizeY; y++, yy--)
        //                        {
        //                            if (yy % 2 == 0)
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }

        //                }   // Back spatter
        //            }
        //            else
        //            {   // Front spatter
        //                //
        //                if (pattern.fontValue.rotateAngle == 0.0)
        //                {   // 0 degree for clean data
        //                    for (int y = 0; y < fontsizeY; y += 2)
        //                        for (i = 0; i < NoVin; i++)
        //                            for (int x = 0; x < fontsizeX; x++)
        //                            {
        //                                if (y < fontsizeY - 1)
        //                                {
        //                                    if ((RasterData[i, y, x] != null) || (RasterData[i, y + 1, x] != null)) NoPointsC[y]++;
        //                                }
        //                                else
        //                                {
        //                                    if (RasterData[i, y, x] != null) NoPointsC[y]++;    // Data Number of clean data
        //                                }
        //                            }
        //                }
        //                else
        //                {   // 180 degree for clean data
        //                    for (int y = (int)(fontsizeY - 1); y >= 0; y -= 2)
        //                        for (i = 0; i < NoVin; i++)
        //                            for (int x = 0; x < fontsizeX; x++)
        //                            {
        //                                if (y > 0)
        //                                {
        //                                    if ((RasterData[i, y, x] != null) || (RasterData[i, y - 1, x] != null)) NoPointsC[y]++;
        //                                }
        //                                else
        //                                {
        //                                    if (RasterData[i, y, x] != null) NoPointsC[y]++;
        //                                }
        //                            }
        //                }

        //                // Make Firing & cleanning Data for front spatter at 0 degree
        //                if (pattern.fontValue.rotateAngle == 0.0)
        //                {
        //                    //
        //                    // Make firing Data for 0 degree
        //                    StringBuilder sb = new StringBuilder();
        //                    for (int y = (int)(fontsizeY - 1); y >= 0; y--)
        //                    {
        //                        if (y % 2 == 0)
        //                        {   // Fire Data string for even line ->
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, NoPoints[y]);
        //                            for (i = 0; i < NoVin; i++)
        //                                for (int x = 0; x < fontsizeX; x++)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        else
        //                        {   // Fire Data string for odd line <-
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, NoPoints[y]);
        //                            for (i = NoVin - 1; i >= 0; i--)
        //                                for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                    }

        //                    //
        //                    //  Make Cleaning Data for 0 degree
        //                    sb = new StringBuilder();
        //                    ushort x2 = 0, y2 = 0, c2 = 0;

        //                    if (fontsizeY > 10)  // Y Font Size > 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = true;
        //                        for (int y = 0; y < fontsizeY; y += 2)
        //                        {
        //                            if ((y / 2) % 2 == 0)   // <-
        //                            {
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y + 1, 1].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y + 1, 1].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY);
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y + 1, 1].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y + 1, 0].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y + 1, 0].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY);
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y + 1, 0].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (y < fontsizeY - 1)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y + 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)(RasterData[i, y + 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {   // ->
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y + 1, 0].mX) / 2) : (ushort)(RasterData[NoVin, y, 0].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y + 1, 0].mY) / 2) : (ushort)(RasterData[NoVin, y, 0].mY);  // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y + 1, 0].mC) / 2) : (ushort)(RasterData[NoVin, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y + 1, 1].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y + 1, 1].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY);  // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y < fontsizeY - 1) ? (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y + 1, 1].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (y < fontsizeY - 1)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y + 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] != null)) x2 = (ushort)(RasterData[i, y + 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y + 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }
        //                    else  // Y Font Size <= 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = false;
        //                        for (int y = 0; y < fontsizeY; y++)
        //                        {
        //                            if (y % 2 == 0) // <-
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    // 
        //                    // Make firing data at 180 degree for front spatter
        //                    StringBuilder sb = new StringBuilder();
        //                    int yy = 0;
        //                    for (int y = 0; y < fontsizeY; y++, yy++)
        //                    {
        //                        if (y % 2 == 1) // odd line <-
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, NoPoints[y]);
        //                            for (i = NoVin - 1; i >= 0; i--)
        //                                for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        else
        //                        {   // Fire Data string even line ->
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[NoVin, y, 0].mX, RasterData[NoVin, y, 0].mY, RasterData[NoVin, y, 0].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[NoVin, y, 1].mX, RasterData[NoVin, y, 1].mY, RasterData[NoVin, y, 1].mZ, NoPoints[y]);
        //                            for (i = 0; i < NoVin; i++)
        //                                for (int x = 0; x < fontsizeX; x++)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                    }

        //                    //
        //                    //  Make Cleaning Data for 180 degree
        //                    sb = new StringBuilder();
        //                    ushort x2 = 0, y2 = 0, c2 = 0;
        //                    if (fontsizeY > 10)  // Y Font Size > 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = true;
        //                        for (int y = (int)(fontsizeY - 1); y >= 0; y -= 2, yy -= 2)
        //                        {
        //                            if ((yy / 2) % 2 == 0) // ->
        //                            {   // even
        //                                x2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y - 1, 0].mX) / 2) : (ushort)(RasterData[NoVin, y, 0].mX);
        //                                y2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y - 1, 0].mY) / 2) : (ushort)(RasterData[NoVin, y, 0].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y - 1, 0].mC) / 2) : (ushort)(RasterData[NoVin, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y - 1, 1].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y - 1, 1].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y - 1, 1].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (y > 0)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y - 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)(RasterData[i, y - 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {   // odd <-
        //                                x2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mX + RasterData[NoVin, y - 1, 1].mX) / 2) : (ushort)(RasterData[NoVin, y, 1].mX);
        //                                y2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mY + RasterData[NoVin, y - 1, 1].mY) / 2) : (ushort)(RasterData[NoVin, y, 1].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 1].mC + RasterData[NoVin, y - 1, 1].mC) / 2) : (ushort)(RasterData[NoVin, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mX + RasterData[NoVin, y - 1, 0].mX) / 2) : (ushort)(RasterData[NoVin, y, 0].mX);
        //                                y2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mY + RasterData[NoVin, y - 1, 0].mY) / 2) : (ushort)(RasterData[NoVin, y, 0].mY); // - (ushort)(Step_H * (double)Mode_File.Step_Length / 2.0 + 0.5));
        //                                y2 += (ushort)(pattern.laserValue.cleanDelta + 0.5);
        //                                c2 = (y > 0) ? (ushort)((RasterData[NoVin, y, 0].mC + RasterData[NoVin, y - 1, 0].mC) / 2) : (ushort)(RasterData[NoVin, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (y > 0)
        //                                        {
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)((RasterData[i, y, x].mI + RasterData[i, y - 1, x].mI) / 2);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] != null)) x2 = (ushort)(RasterData[i, y - 1, x].mI);
        //                                            if ((RasterData[i, y, x] != null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)(RasterData[i, y, x].mI);
        //                                            if ((RasterData[i, y, x] == null) && (RasterData[i, y - 1, x] == null)) x2 = (ushort)0;
        //                                            if (x2 != 0) sb.AppendFormat("{0:X4}", x2);
        //                                        }
        //                                        else
        //                                        {
        //                                            if (RasterData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }
        //                    else  // Y Font Size <= 10
        //                    {
        //                        currMarkInfo.checkdata.TwoLineDisplay = false;
        //                        for (int y = (int)(fontsizeY - 1); y >= 0; y--)
        //                        {
        //                            if (y % 2 == 1) // ->
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = 0; i < NoVin; i++)
        //                                    for (int x = 0; x < fontsizeX; x++)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (ushort)RasterData[NoVin, y, 1].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 1].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[NoVin, y, 0].mX;
        //                                y2 = (ushort)RasterData[NoVin, y, 0].mY;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[NoVin, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPoints[y]);

        //                                for (i = NoVin - 1; i >= 0; i--)
        //                                    for (int x = (int)(fontsizeX - 1); x >= 0; x--)
        //                                    {
        //                                        if (RasterData[i, y, x] != null) sb.AppendFormat("{0:X4}", RasterData[i, y, x].mI);
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                        }
        //                    }
        //                }   // front spatter
        //            }   // Back/front spatter
        //        }   // Dot firing

        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }


        //    //
        //    // Make firing/cleanning data
        //    //

        //    // Local function

        //    Vector3D getRodrigueRotation(Vector3D XY0)
        //    {
        //        Vector3D Tmp = new Vector3D();
        //        Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
        //        Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
        //        Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

        //        return Tmp;
        //    }

        //}


        //public FontData4Send CalculateLaserData(FontDataClass fdc, int i, int j, Vector3D revpt, double Step_W, double Step_H, bool skipRot, double PlaneCenterZ, PatternValueEx pattern)
        //{
        //    FontData4Send retval = new FontData4Send();
        //    double X = 0;
        //    double Y = 0;
        //    double Z = 0;
        //    int F = 0;
        //    Vector3D M1 = new Vector3D();
        //    Vector3D M2 = new Vector3D();
        //    Vector3D M = new Vector3D();
        //    Vector3D C = new Vector3D();

        //    try
        //    {
        //        //fd = (FontDataClass)lineData[j].Clone();
        //        X = fdc.vector3d.X;
        //        Y = fdc.vector3d.Y;
        //        Z = fdc.vector3d.Z;
        //        F = fdc.Flag;

        //        // ABS mm
        //        M1.X = revpt.X + X * Step_W;
        //        M2.X = revpt.X + Math.Round(X) * Step_W;

        //        // Font offset compensation
        //        M1.Y = revpt.Y + Y * Step_H;
        //        M2.Y = M1.Y;
        //        M1.Z = M2.Z = SP.Z;

        //        M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, revpt.X, revpt.Y, pattern.fontValue.rotateAngle);
        //        M1.Z = SP.Z;
        //        M2 = ImageProcessManager.Rotate_Point2(M2.X, M2.Y, revpt.X, revpt.Y, pattern.fontValue.rotateAngle);
        //        M2.Z = SP.Z;

        //        // TM SHIN
        //        M1.X -= pattern.positionValue.center3DPos.X; M1.Y -= pattern.positionValue.center3DPos.Y;
        //        M2.X -= pattern.positionValue.center3DPos.X; M2.Y -= pattern.positionValue.center3DPos.Y;

        //        M = (skipRot == true) ? M1 : getRodrigueRotation(M1);
        //        C = (skipRot == true) ? M2 : getRodrigueRotation(M2);

        //        M.X += pattern.positionValue.center3DPos.X; M.Y += pattern.positionValue.center3DPos.Y;
        //        C.X += pattern.positionValue.center3DPos.X; C.Y += pattern.positionValue.center3DPos.Y;
        //        double Mt = M.Z;
        //        M.Z = Mt + pattern.positionValue.center3DPos.Z + currMarkInfo.checkdata.PlaneCenterZ;
        //        C.Z = Mt + (pattern.laserValue.cleanPosition + pattern.positionValue.center3DPos.Z) + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

        //        // Change mm to BLU(Unit 0.01mm)
        //        M = M * pattern.headValue.stepLength;
        //        C = C * pattern.headValue.stepLength;

        //        retval.cN = (byte)i; retval.fN = (byte)j;
        //        retval.mX = (UInt16)(M.X + 0.5); retval.mY = (UInt16)(M.Y + 0.5); retval.mZ = (UInt16)(M.Z + 0.5); retval.mF = (byte)F;// byte.Parse(xy_[2]);
        //        retval.mC = (UInt16)(C.Z + 0.5);
        //        retval.mI = (UInt16)(C.X + 0.5);
        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //    return retval;

        //    Vector3D getRodrigueRotation(Vector3D XY0)
        //    {
        //        Vector3D Tmp = new Vector3D();
        //        Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
        //        Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
        //        Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

        //        return Tmp;
        //    }
        //}

        //public async Task<ITNTResponseArgs> Start_TEXT2(string vin, PatternValueEx pattern)    // Making Fire/Clean data by TM SHIN
        //{
        //    {
        //        //COMMAND_RESULT retval = new COMMAND_RESULT();

        //        //MPOINT SP = new MPOINT();
        //        //MPOINT CP = new MPOINT();
        //        //MPOINT PointLU = new MPOINT();
        //        //MPOINT PointLD = new MPOINT();
        //        //MPOINT PointRU = new MPOINT();
        //        //MPOINT PointRD = new MPOINT();
        //        //MPOINT PointCT = new MPOINT();
        //        //MPOINT PointCD = new MPOINT();
        //        //MPOINT VectorNormal = new MPOINT();
        //        //MPOINT VectorLuRd = new MPOINT();
        //        //MPOINT VectorRuRd = new MPOINT();
        //        //MPOINT VectorRot = new MPOINT();

        //        //MPOINT[] Rev_Point = new MPOINT[1];
        //        //int XF, YF, SF;
        //        //string[] TEMPF;
        //        //double Step_W;
        //        //double Step_H;
        //    }

        //    string className = "SetControllerWindow2";
        //    string funcName = "Start_TEXT";

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    int vinLength = 0;

        //    Vector3D SP0 = new Vector3D();
        //    Vector3D SP = new Vector3D();
        //    Vector3D CP = new Vector3D();

        //    Vector3D VectorNormal = new Vector3D();
        //    Vector3D VectorRot = new Vector3D();
        //    List<Vector3D> Rev_Point = new List<Vector3D>();

        //    double Step_W;
        //    double Step_H;
        //    string value = "";
        //    byte headType = 0;
        //    VinNoInfo vininfo = new VinNoInfo();
        //    //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
        //    List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
        //    double fontsizeX = 0, fontsizeY = 0, shiftVal = 0;
        //    double fontsizeX2 = 0, fontsizeY2 = 0, shiftVal2 = 0;
        //    double cleanPosition = 0;
        //    string errCode = "";
        //    double totWidth = 0;
        //    double R11, R12, R13, R21, R22, R23, R31, R32, R33;
        //    int i, j;
        //    int idx = 0;
        //    int ifontsizeX = 0, ifontsizeY = 0, ishiftVal = 0;

        //    Vector3D M1F = new Vector3D();                                   // for fire data mm
        //    Vector3D M2F = new Vector3D();                                   // for clean data mm
        //    Vector3D MF = new Vector3D();
        //    Vector3D CF = new Vector3D();

        //    Vector3D M1C = new Vector3D();                                   // for fire data mm
        //    Vector3D M2C = new Vector3D();                                   // for clean data mm
        //    Vector3D MC = new Vector3D();
        //    Vector3D CC = new Vector3D();

        //    //byte fontdirection = 0;

        //    try
        //    {
        //        vinLength = vin.Length;
        //        if (vinLength <= 0)
        //        {
        //            retval.execResult = -1;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN LENGTH IS INVALID (" + vinLength.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

        //        Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
        //        byte.TryParse(value, out headType);

        //        vininfo.vinNo = vin;
        //        vininfo.fontName = pattern.fontValue.fontName;
        //        vininfo.width = pattern.fontValue.width;
        //        vininfo.height = pattern.fontValue.height;
        //        vininfo.pitch = pattern.fontValue.pitch;
        //        vininfo.thickness = pattern.fontValue.thickness;
        //        ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 0, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref errCode);

        //        m_currCMD = (byte)'S';
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SolOnOffTime ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        m_currCMD = (byte)'d';
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(pattern.speedValue.dwellTime);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dwellTimeSet ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        if (currMarkInfo.checkdata.bReady == false)
        //        {
        //            CheckAreaData chkdata = new CheckAreaData();
        //            chkdata = await Range_Test2(vin, pattern);
        //            if (chkdata.execResult != 0)
        //            {
        //                return retval;
        //            }
        //            else
        //                currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
        //        }

        //        ifontsizeX = (int)(fontsizeX + 0.5);
        //        ifontsizeY = (int)(fontsizeY + 0.5);
        //        ishiftVal = (int)(shiftVal + 0.5);

        //        CP = pattern.positionValue.center3DPos;
        //        cleanPosition = pattern.laserValue.cleanPosition;
        //        cleanPosition += CP.Z;         // Relative Cleanning postion

        //        SP0.X = totWidth / 2;
        //        SP0.Y = pattern.fontValue.height / 2;
        //        SP0.Z = 0;
        //        SP = CP - SP0;
        //        SP.Z = 0.0d;

        //        VectorNormal = currMarkInfo.checkdata.NormalDir;
        //        VectorRot.X = -VectorNormal.Y;
        //        VectorRot.Y = VectorNormal.X;
        //        VectorRot.Z = 0.0;

        //        double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);
        //        if (sqXY != 0)
        //        {
        //            VectorRot.X /= sqXY;
        //            VectorRot.Y /= sqXY;
        //        }

        //        // Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
        //        bool skipRot = false;
        //        double cosValue = VectorNormal.Z / VectorNormal.Length;
        //        double sinValue = Math.Sqrt(1.0d - cosValue * cosValue);

        //        if (cosValue > 0.9999986111)
        //        {      // 0.1 mm difference between 60mm
        //            skipRot = true;
        //            R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
        //        }
        //        else
        //        {
        //            R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
        //            R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
        //            R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
        //            R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
        //            R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
        //            R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
        //            R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
        //            R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
        //            R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
        //        }

        //        Step_W = pattern.fontValue.width / (fontsizeX - 1.0);
        //        Step_H = pattern.fontValue.height / (fontsizeY - 1.0);

        //        //FontData4Send[,,] RasterData = new FontData4Send[vinLength + 1, (int)(fontsizeY + 0.5), (int)(fontsizeX + 0.5)];     // BLU
        //        //FontData4Send[,,] AllClrData = new FontData4Send[vinLength + 1, (int)(fontsizeY + 0.5), (int)(fontsizeX + 0.5)];
        //        Int32[] RlowerBounds = { 0, -1, 0 };
        //        Int32[] Rlengths = { vinLength + 1, ifontsizeY + 2, ifontsizeX };
        //        FontData4Send[,,] RasterData = (FontData4Send[,,])Array.CreateInstance(typeof(FontData4Send), Rlengths, RlowerBounds);

        //        // M_FONT[,,] AllClrData = new M_FONT[NoVin, YF, XF];          // BLU
        //        Int32[] ClowerBounds = { 0, -1, -1 };
        //        Int32[] Clengths = { vinLength, ifontsizeY + 2, ifontsizeX + 2 };
        //        FontData4Send[,,] AllClrData = (FontData4Send[,,])Array.CreateInstance(typeof(FontData4Send), Clengths, ClowerBounds);


        //        currMarkInfo.senddata.sendDataFire.Clear();
        //        currMarkInfo.senddata.sendDataClean.Clear();

        //        //List<Vector3D> recvPoint = new List<Vector3D>();
        //        ImageProcessManager.GetStartPointLinear(vinLength, CP, SP, pattern.fontValue.pitch, pattern.fontValue.rotateAngle, ref Rev_Point);

        //        Vector3D[] LeftRightSP = new Vector3D[2];
        //        LeftRightSP[0] = ImageProcessManager.Rotate_Point2(SP.X - pattern.positionValue.rasterSP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
        //        LeftRightSP[1] = ImageProcessManager.Rotate_Point2(SP.X + totWidth + pattern.positionValue.rasterEP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
        //        List<FontDataClass> lineData = new List<FontDataClass>();
        //        List<FontDataClass> lineDataClean = new List<FontDataClass>();
        //        if (pattern.laserValue.charFull == 0)
        //            pattern.laserValue.charFull = ':';
        //        ImageProcessManager.GetFontDataOneEx(pattern.laserValue.charFull, pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);
        //        //ImageProcessManager.GetFontDataOneEx('^', pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);

        //        double XPosF = 0, YPosF = 0, ZPosF = 0;
        //        int FlagF = 0;
        //        double XPosC = 0, YPosC = 0, ZPosC = 0;
        //        int FlagC = 0;

        //        for (i = 0; i < Rev_Point.Count; i++)
        //        {
        //            if (vin.Substring(i, 1) != " ")      //Space Skip
        //            {
        //                lineData = fontData[i];
        //                for (j = 0; j < lineData.Count; j++)
        //                {
        //                    //fd = (FontDataClass)lineData[j].Clone();
        //                    XPosF = lineData[j].vector3d.X;
        //                    YPosF = lineData[j].vector3d.Y;
        //                    ZPosF = lineData[j].vector3d.Z;
        //                    FlagF = lineData[j].Flag;

        //                    // ABS mm
        //                    M1F.X = Rev_Point[i].X + XPosF * Step_W;
        //                    M2F.X = Rev_Point[i].X + Math.Round(XPosF) * Step_W;

        //                    // Font offset compensation
        //                    M1F.Y = Rev_Point[i].Y + YPosF * Step_H;
        //                    M2F.Y = M1F.Y;
        //                    M1F.Z = M2F.Z = SP.Z;

        //                    M1F = ImageProcessManager.Rotate_Point2(M1F.X, M1F.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M1F.Z = SP.Z;
        //                    M2F = ImageProcessManager.Rotate_Point2(M2F.X, M2F.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M2F.Z = SP.Z;

        //                    // TM SHIN
        //                    M1F.X -= CP.X; M1F.Y -= CP.Y;
        //                    M2F.X -= CP.X; M2F.Y -= CP.Y;

        //                    MF = (skipRot == true) ? M1F : getRodrigueRotation(M1F);
        //                    CF = (skipRot == true) ? M2F : getRodrigueRotation(M2F);

        //                    MF.X += CP.X; MF.Y += CP.Y;
        //                    CF.X += CP.X; CF.Y += CP.Y;
        //                    double Mt = MF.Z;
        //                    MF.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
        //                    CF.Z = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

        //                    // Change mm to BLU(Unit 0.01mm)
        //                    MF = MF * pattern.headValue.stepLength;
        //                    CF = CF * pattern.headValue.stepLength;

        //                    FontData4Send FontData = new FontData4Send();

        //                    FontData.cN = (byte)i; FontData.fN = (byte)j;
        //                    FontData.mX = (UInt16)(MF.X + 0.5); FontData.mY = (UInt16)(MF.Y + 0.5); FontData.mZ = (UInt16)(MF.Z + 0.5); FontData.mF = (byte)FlagF;
        //                    FontData.mC = (UInt16)(CF.Z + 0.5);
        //                    FontData.mI = (UInt16)(CF.X + 0.5);

        //                    if (pattern.laserValue.density == 1)
        //                    {
        //                        FontData.mF = 0;
        //                        RasterData[i, (int)YPosF, (int)Math.Round(XPosF)] = (FontData4Send)FontData.Clone();
        //                    }
        //                    else
        //                    {
        //                        var m_font = FontData.cN.ToString("X2") + FontData.fN.ToString("X2") + FontData.mX.ToString("X4") + FontData.mY.ToString("X4") + FontData.mZ.ToString("X4") + FontData.mF.ToString("X4");
        //                        currMarkInfo.senddata.sendDataFire.Add(m_font);
        //                    }

        //                    idx++;
        //                }

        //                idx = 0;
        //                //  make all clear data : 0, char clear data : 1
        //                if (pattern.laserValue.charClean == 0)
        //                    lineData = lineDataClean.ToList();

        //                for (j = 0; j < lineData.Count; j++)
        //                {
        //                    //xy_ = (xy_Data[j]).Split(',');
        //                    XPosC = lineData[j].vector3d.X;
        //                    YPosC = lineData[j].vector3d.Y;
        //                    ZPosC = lineData[j].vector3d.Z;
        //                    FlagC = lineData[j].Flag;

        //                    // ABS mm
        //                    M1C.X = Rev_Point[i].X + XPosC * Step_W;
        //                    M2C.X = Rev_Point[i].X + Math.Round(XPosC) * Step_W;
        //                    // Font offset compensation
        //                    M1C.Y = Rev_Point[i].Y + YPosC * Step_H;
        //                    M2C.Y = M1C.Y;
        //                    M1C.Z = M2C.Z = SP.Z;

        //                    M1C = ImageProcessManager.Rotate_Point2(M1C.X, M1C.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M1C.Z = SP.Z;
        //                    M2C = ImageProcessManager.Rotate_Point2(M2C.X, M2C.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M2C.Z = SP.Z;

        //                    // TM SHIN
        //                    M1C.X -= CP.X; M1C.Y -= CP.Y;
        //                    M2C.X -= CP.X; M2C.Y -= CP.Y;

        //                    MC = (skipRot == true) ? M1C : getRodrigueRotation(M1C);
        //                    CC = (skipRot == true) ? M2C : getRodrigueRotation(M2C);

        //                    MC.X += CP.X; MC.Y += CP.Y;
        //                    CC.X += CP.X; CC.Y += CP.Y;
        //                    double MtC = MC.Z;
        //                    MC.Z = MtC + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
        //                    CC.Z = MtC + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

        //                    // Change mm to BLU(Unit 0.01mm)
        //                    MC = MC * pattern.headValue.stepLength;
        //                    CC = CC * pattern.headValue.stepLength;

        //                    FontData4Send FontDataClean = new FontData4Send();

        //                    FontDataClean.cN = (byte)i; FontDataClean.fN = (byte)j;
        //                    FontDataClean.mX = (UInt16)(MC.X + 0.5); FontDataClean.mY = (UInt16)(MC.Y + 0.5); FontDataClean.mZ = (UInt16)(MC.Z + 0.5); FontDataClean.mF = (byte)FlagC;
        //                    FontDataClean.mC = (UInt16)(CC.Z + 0.5);
        //                    FontDataClean.mI = (UInt16)(CC.X + 0.5);

        //                    if (pattern.laserValue.density == 1)
        //                    {
        //                        FontDataClean.mF = 0;
        //                        AllClrData[i, (int)YPosC, (int)Math.Round(XPosC)] = FontDataClean;
        //                    }
        //                    else
        //                    {
        //                        var m_clean = FontDataClean.cN.ToString("X2") + FontDataClean.fN.ToString("X2") + FontDataClean.mX.ToString("X4") + FontDataClean.mY.ToString("X4") + FontDataClean.mC.ToString("X4") + FontDataClean.mF.ToString("X4");
        //                        currMarkInfo.senddata.sendDataClean.Add(m_clean);
        //                    }

        //                    idx++;
        //                }
        //            }
        //        }

        //        //
        //        // Make firing/cleanning data
        //        //
        //        idx = 0;
        //        if (pattern.laserValue.density == 1)
        //        {
        //            // Calculate No of Dot point
        //            //ushort[] NoPoints = new ushort[ifontsizeY];
        //            ushort[,] NoPoints = (ushort[,])Array.CreateInstance(typeof(ushort), new int[2] { ifontsizeY + 2, 1 }, new int[2] { -1, 0 });

        //            for (int y = -1; y < ifontsizeY + 1; y++)
        //                for (i = 0; i < vinLength; i++)
        //                    for (int x = 0; x < ifontsizeX; x++)
        //                        if (RasterData[i, y, x] != null) NoPoints[y, 0]++;         // Data Number of fire data

        //            ////
        //            Debug.WriteLine("");

        //            //
        //            // Make Jump/Start data
        //            for (i = -1; i < ifontsizeY + 1; i++)
        //            {
        //                for (j = 0; j < 2; j++)         // Jump/Start XXXXX
        //                {
        //                    M1C.X = LeftRightSP[j].X;
        //                    M1C.Y = LeftRightSP[j].Y + (double)i * Step_H;
        //                    M1C.Z = SP.Z;

        //                    M1C = ImageProcessManager.Rotate_Point2(M1C.X, M1C.Y, LeftRightSP[j].X, LeftRightSP[j].Y, pattern.fontValue.rotateAngle);
        //                    M1C.Z = SP.Z;

        //                    M1C.X -= CP.X; M1C.Y -= CP.Y;

        //                    MC = (skipRot == true) ? M1C : getRodrigueRotation(M1C);

        //                    MC.X += CP.X; MC.Y += CP.Y;
        //                    double MtC = MC.Z;
        //                    MC.Z = MtC + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
        //                    CC.Z = MtC + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis
        //                    //Debug.WriteLine(String.Format("J{0:D3}=>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3}", idx, M.X, M.Y, M.Z, C.Z));

        //                    // Change to BLU(Unit 0.01mm)
        //                    MC = MC * pattern.headValue.stepLength;
        //                    CC.Z = CC.Z * pattern.headValue.stepLength;

        //                    //M_FONT FontData = new M_FONT();
        //                    FontData4Send FontData = new FontData4Send();

        //                    FontData.cN = (byte)i;
        //                    FontData.fN = (byte)j;
        //                    FontData.mX = (UInt16)(MC.X + 0.5); FontData.mY = (UInt16)(MC.Y + 0.5); FontData.mZ = (UInt16)(MC.Z + 0.5);
        //                    FontData.mC = (UInt16)(CC.Z + 0.5);
        //                    FontData.mI = 0;
        //                    FontData.mF = 0;

        //                    RasterData[vinLength, i, j] = FontData;

        //                    idx++;
        //                }
        //            }

        //            idx = 0;
        //            ushort[,] NoPointsC = (ushort[,])Array.CreateInstance(typeof(ushort), new int[2] { ifontsizeY + 2, 1 }, new int[2] { -1, 0 });
        //            //
        //            for (int y = -1; y < ifontsizeY + 1; y++)
        //                for (i = 0; i < vinLength; i++)
        //                    for (int x = -1; x < ifontsizeX + 1; x++)
        //                        if (AllClrData[i, y, x] != null) NoPointsC[y, 0]++;    // Data Number of clean data

        //            //
        //            //
        //            //
        //            //if (backSpatter == 0)  // 0 : Back
        //            if (pattern.headValue.spatterType == 0)  // 0 : Back
        //            {
        //                //
        //                // Make Firing & cleanning Data for back spatter at 0 degree
        //                if (pattern.fontValue.rotateAngle == 0.0)
        //                {
        //                    if (pattern.laserValue.combineFireClean != 0)
        //                    {
        //                        //
        //                        // Make firing | clean Data for 0 degree, back spatter [AF0B]
        //                        StringBuilder sb = new StringBuilder();
        //                        ushort x2 = 0, y2 = 0, c2 = 0;

        //                        bool DirRight = true;
        //                        for (int y = 0; y < ifontsizeY; y++)
        //                        {
        //                            if (DirRight == true)
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
        //                                for (i = 0; i < vinLength; i++)
        //                                    for (int x = 0; x < ifontsizeX; x++)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
        //                                for (i = vinLength - 1; i >= 0; i--)
        //                                    for (int x = ifontsizeX - 1; x >= 0; x--)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            DirRight = !DirRight;

        //                            //
        //                            //  Make Cleaning Data for 0 degree, back spatter [AC0B]
        //                            if (NoPointsC[y, 0] > 0)
        //                            {
        //                                if (DirRight == false)
        //                                {
        //                                    x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                    x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                    for (i = vinLength - 1; i >= 0; i--)
        //                                        for (int x = ifontsizeX - 1; x >= 0; x--)
        //                                            if (AllClrData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                    //Mode_File.SendData.Add(sb.ToString());
        //                                    Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                    sb.Length = 0;
        //                                }
        //                                else
        //                                {
        //                                    x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                    x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                    for (i = 0; i < vinLength; i++)
        //                                        for (int x = 0; x < ifontsizeX; x++)
        //                                            if (AllClrData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                    //Mode_File.SendData.Add(sb.ToString());
        //                                    Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                    sb.Length = 0;
        //                                }
        //                                DirRight = !DirRight;
        //                            }   // NoPointsC
        //                            else
        //                            {
        //                                currMarkInfo.checkdata.TwoLineDisplay = false;
        //                                //Mode_File.TwoLineDisplay = false;
        //                            }
        //                        }
        //                    }
        //                    else  // Fire & then Clean : Normal case
        //                    {
        //                        //
        //                        // Make firing Data for 0 degree, back spatter [1F0]
        //                        StringBuilder sb = new StringBuilder();
        //                        bool DirRight = true;
        //                        for (int y = 0; y < ifontsizeY; y++)
        //                        {
        //                            if (DirRight == true)
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
        //                                for (i = 0; i < vinLength; i++)
        //                                    for (int x = 0; x < ifontsizeX; x++)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {   // Fire Data string
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
        //                                for (i = vinLength - 1; i >= 0; i--)
        //                                    for (int x = ifontsizeX - 1; x >= 0; x--)
        //                                        if (RasterData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                                currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                                //Mode_File.SendData.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            DirRight = !DirRight;
        //                        }

        //                        //
        //                        //  Make Cleaning Data for 0 degree, back spatter [1C0]
        //                        sb = new StringBuilder();
        //                        ushort x2 = 0, y2 = 0, c2 = 0;

        //                        currMarkInfo.checkdata.TwoLineDisplay = true;
        //                        //Mode_File.TwoLineDisplay = true;
        //                        for (int y = ifontsizeY; y >= -1; y--)
        //                        {
        //                            if (NoPointsC[y, 0] > 0)
        //                            {
        //                                if (DirRight == false)
        //                                {
        //                                    x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                    x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                    for (i = vinLength - 1; i >= 0; i--)
        //                                        for (int x = ifontsizeX; x >= -1; x--)
        //                                            if (AllClrData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                    currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                    //Mode_File.SendClean.Add(sb.ToString());
        //                                    Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                    sb.Length = 0;
        //                                }
        //                                else
        //                                {
        //                                    x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                    x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                    y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                    y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                    //y2 += CDelta;
        //                                    c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                    for (i = 0; i < vinLength; i++)
        //                                        for (int x = -1; x < ifontsizeX + 1; x++)
        //                                            if (AllClrData[i, y, x] != null)
        //                                                sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                    currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                    //Mode_File.SendClean.Add(sb.ToString());
        //                                    Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                    sb.Length = 0;
        //                                }
        //                                DirRight = !DirRight;
        //                            }   // NoPointsC
        //                            else
        //                            {
        //                                currMarkInfo.checkdata.TwoLineDisplay = false;
        //                                //Mode_File.TwoLineDisplay = false;
        //                            }
        //                        }
        //                    } // CombineFireClean if-else
        //                }
        //                else
        //                {
        //                    // 
        //                    // Make firing data at 180 degree for back spatter [2F180]
        //                    StringBuilder sb = new StringBuilder();
        //                    bool DirRight = true;
        //                    for (int y = ifontsizeY - 1; y >= 0; y--)
        //                    {
        //                        if (DirRight == false)
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
        //                            for (i = vinLength - 1; i >= 0; i--)
        //                                for (int x = ifontsizeX - 1; x >= 0; x--)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Mode_File.SendData.Add(sb.ToString());
        //                            Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        else
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
        //                            for (i = 0; i < vinLength; i++)
        //                                for (int x = 0; x < ifontsizeX; x++)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Mode_File.SendData.Add(sb.ToString());
        //                            Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        DirRight = !DirRight;
        //                    }

        //                    //
        //                    //  Make Cleaning Data for 180 degree, back spatter [2C180]
        //                    sb = new StringBuilder();
        //                    ushort x2 = 0, y2 = 0, c2 = 0;
        //                    currMarkInfo.checkdata.TwoLineDisplay = false;
        //                    //Mode_File.TwoLineDisplay = false;
        //                    for (int y = -1; y < ifontsizeY + 1; y++)
        //                    {
        //                        if (NoPointsC[y, 0] > 0)
        //                        {
        //                            if (DirRight == false)
        //                            {
        //                                x2 = (ushort)RasterData[vinLength, y, 1].mX;
        //                                y2 = (ushort)RasterData[vinLength, y, 1].mY;
        //                                y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[vinLength, y, 1].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)RasterData[vinLength, y, 0].mX;
        //                                y2 = (ushort)RasterData[vinLength, y, 0].mY;
        //                                y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)RasterData[vinLength, y, 0].mC;
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                for (i = vinLength - 1; i >= 0; i--)
        //                                    for (int x = ifontsizeX; x >= -1; x--)
        //                                        if (AllClrData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Mode_File.SendClean.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                y2 += (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 += CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                for (i = 0; i < vinLength; i++)
        //                                    for (int x = -1; x < ifontsizeX + 1; x++)
        //                                        if (AllClrData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Mode_File.SendClean.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            DirRight = !DirRight;
        //                        } //
        //                        else
        //                        {
        //                            currMarkInfo.checkdata.TwoLineDisplay = true;
        //                            //Mode_File.TwoLineDisplay = true;
        //                        }
        //                    }

        //                }   // Back spatter
        //            }
        //            else    // Front spatter
        //            {
        //                //
        //                // Make Firing & cleanning Data for front spatter at 0 degree
        //                if (pattern.fontValue.rotateAngle == 0.0)
        //                {
        //                    //
        //                    // Make firing Data for 0 degree, front spatter [3F0]
        //                    StringBuilder sb = new StringBuilder();
        //                    bool DirRight = true;
        //                    for (int y = ifontsizeY - 1; y >= 0; y--)
        //                    {
        //                        if (DirRight == true)
        //                        {   // Fire Data string for even line ->
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
        //                            for (i = 0; i < vinLength; i++)
        //                                for (int x = 0; x < ifontsizeX; x++)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Mode_File.SendData.Add(sb.ToString());
        //                            Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        else
        //                        {   // Fire Data string for odd line <-
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
        //                            for (i = vinLength - 1; i >= 0; i--)
        //                                for (int x = ifontsizeX - 1; x >= 0; x--)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Mode_File.SendData.Add(sb.ToString());
        //                            Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        DirRight = !DirRight;
        //                    }

        //                    //
        //                    //  Make Cleaning Data for 0 degree, front spatter [3C0]
        //                    sb = new StringBuilder();
        //                    ushort x2 = 0, y2 = 0, c2 = 0;

        //                    currMarkInfo.checkdata.TwoLineDisplay = false;
        //                    //Mode_File.TwoLineDisplay = false;
        //                    for (int y = -1; y < ifontsizeY + 1; y++)
        //                    {
        //                        if (NoPointsC[y, 0] > 0)
        //                        {
        //                            if (DirRight == false)
        //                            {
        //                                x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                for (i = vinLength - 1; i >= 0; i--)
        //                                    for (int x = ifontsizeX; x >= -1; x--)
        //                                        if (AllClrData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Mode_File.SendClean.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                for (i = 0; i < vinLength; i++)
        //                                    for (int x = -1; x < ifontsizeX + 1; x++)
        //                                        if (AllClrData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Mode_File.SendClean.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            DirRight = !DirRight;
        //                        }
        //                        else
        //                        {
        //                            currMarkInfo.checkdata.TwoLineDisplay = true;
        //                            //Mode_File.TwoLineDisplay = true;
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    // 
        //                    // Make firing data at 180 degree for front spatter [4F180]
        //                    StringBuilder sb = new StringBuilder();
        //                    bool DirRight = true;
        //                    for (int y = 0; y < ifontsizeY; y++)
        //                    {
        //                        if (DirRight == false) // odd line <-
        //                        {   // Fire Data string
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
        //                            for (i = vinLength - 1; i >= 0; i--)
        //                                for (int x = ifontsizeX - 1; x >= 0; x--)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Mode_File.SendData.Add(sb.ToString());
        //                            Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        else
        //                        {   // Fire Data string even line ->
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
        //                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
        //                            for (i = 0; i < vinLength; i++)
        //                                for (int x = 0; x < ifontsizeX; x++)
        //                                    if (RasterData[i, y, x] != null)
        //                                        sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
        //                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
        //                            //Mode_File.SendData.Add(sb.ToString());
        //                            Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
        //                            sb.Length = 0;
        //                        }
        //                        DirRight = !DirRight;
        //                    }

        //                    //
        //                    //  Make Cleaning Data for 180 degree, front spatter [4C]
        //                    sb = new StringBuilder();
        //                    ushort x2 = 0, y2 = 0, c2 = 0;

        //                    currMarkInfo.checkdata.TwoLineDisplay = false;
        //                    //Mode_File.TwoLineDisplay = false;
        //                    for (int y = ifontsizeY; y >= -1; y--)
        //                    {
        //                        if (NoPointsC[y, 0] > 0)
        //                        {
        //                            if (DirRight == true)
        //                            {
        //                                x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                for (i = 0; i < vinLength; i++)
        //                                    for (int x = -1; x < ifontsizeX + 1; x++)
        //                                        if (AllClrData[i, y, x] != null)
        //                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Mode_File.SendClean.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            else
        //                            {
        //                                x2 = (ushort)(RasterData[vinLength, y, 1].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 1].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 1].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
        //                                x2 = (ushort)(RasterData[vinLength, y, 0].mX);
        //                                y2 = (ushort)(RasterData[vinLength, y, 0].mY);
        //                                y2 -= (ushort)pattern.laserValue.cleanDelta;
        //                                //y2 -= CDelta;
        //                                c2 = (ushort)(RasterData[vinLength, y, 0].mC);
        //                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

        //                                for (i = vinLength - 1; i >= 0; i--)
        //                                    for (int x = ifontsizeX; x >= -1; x--)
        //                                    {
        //                                        if (AllClrData[i, y, x] != null)
        //                                        {
        //                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
        //                                        }
        //                                    }
        //                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
        //                                //Mode_File.SendClean.Add(sb.ToString());
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
        //                                sb.Length = 0;
        //                            }
        //                            DirRight = !DirRight;
        //                        }
        //                        else
        //                        {
        //                            //Mode_File.TwoLineDisplay = true;
        //                            currMarkInfo.checkdata.TwoLineDisplay = true;
        //                        }
        //                    }

        //                }   // front spatter
        //                if (pattern.laserValue.charClean != 0)
        //                {
        //                    currMarkInfo.checkdata.TwoLineDisplay = false;
        //                    //Mode_File.TwoLineDisplay = false;
        //                }
        //            }   // Back/front spatter
        //        }   // Dot firing
        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }

        //    // Local function

        //    Vector3D getRodrigueRotation(Vector3D XY0)
        //    {
        //        Vector3D Tmp = new Vector3D();
        //        Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
        //        Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
        //        Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

        //        return Tmp;
        //    }
        //}

        public async Task<ITNTResponseArgs> Start_TEXT2(string cmd, string vin, PatternValueEx pattern)    // Making Fire/Clean data by TM SHIN
        {
            string className = "SetControllerWindow2";
            string funcName = "Start_TEXT2";

            Vector3D SP0 = new Vector3D();
            Vector3D SP = new Vector3D();
            Vector3D CP = new Vector3D();
            Vector3D VectorNormal = new Vector3D();
            Vector3D VectorRot = new Vector3D();

            List<Vector3D> Rev_Point = new List<Vector3D>();
            double Step_W;
            double Step_H;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int vinLength = 0;
            string value = "";
            byte headType = 0;
            VinNoInfo vininfo = new VinNoInfo();
            List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();

            double fontsizeX = 0;
            double fontsizeY = 0;
            double shiftVal = 0;
            string errCode = "";

            double fontsizeX2 = 0;
            double fontsizeY2 = 0;
            double shiftVal2 = 0;

            double cleanPosition = 0;
            double totWidth = 0;
            double R11, R12, R13, R21, R22, R23, R31, R32, R33;

            int i, j;
            int idx = 0;
            List<FontDataClass> lineData = new List<FontDataClass>();
            List<FontDataClass> lineDataClean = new List<FontDataClass>();

            Vector3D M1 = new Vector3D();
            Vector3D M = new Vector3D();

            try
            {
                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    retval.execResult = -1;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN LENGTH IS INVALID (" + vinLength.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out headType);

                vininfo.vinNo = vin;
                vininfo.fontName = pattern.fontValue.fontName;
                vininfo.width = pattern.fontValue.width;
                vininfo.height = pattern.fontValue.height;
                vininfo.pitch = pattern.fontValue.pitch;
                vininfo.thickness = pattern.fontValue.thickness;
                ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 0, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref errCode);

                m_currCMD = (byte)'S';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SolOnOffTime ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'d';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(pattern.speedValue.dwellTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dwellTimeSet ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                if (currMarkInfo.checkdata.bReady == false)
                {
                    CheckAreaData chkdata = new CheckAreaData();
                    chkdata = await Range_Test(cmd, vin, pattern, 0, 0);
                    if (chkdata.execResult != 0)
                    {
                        return retval;
                    }
                    else
                        currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                }

                totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

                // ABS mm at Center Point
                CP = pattern.positionValue.center3DPos;
                CP.Z += pattern.headValue.distance0Position;
                cleanPosition = pattern.laserValue.cleanPosition;
                cleanPosition += CP.Z;         // Relative Cleanning postion

                // Relative half size mm
                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;
                SP0.Z = 0;
                SP = CP - SP0;
                SP.Z = 0.0d;

                VectorNormal = currMarkInfo.checkdata.NormalDir;
                VectorRot.X = -VectorNormal.Y;
                VectorRot.Y = VectorNormal.X;
                VectorRot.Z = 0.0;

                double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);
                VectorRot.X /= sqXY; VectorRot.Y /= sqXY;
                // Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
                bool skipRot = false;
                double cosValue = VectorNormal.Z / Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y + VectorNormal.Z * VectorNormal.Z);
                double sinValue = Math.Sqrt(1.0 - cosValue * cosValue);

                if (cosValue > 0.9999986111)
                {      // 0.1 mm difference between 60mm
                    skipRot = true;
                    R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
                    R11 = R22 = R33 = 1.0;
                }
                else
                {
                    R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
                    R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
                    R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
                    R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
                    R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
                    R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
                    R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
                    R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
                    R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
                }
                /////
                Step_W = pattern.fontValue.width / (fontsizeX - 1.0);
                Step_H = pattern.fontValue.height / (fontsizeY - 1.0);

                Int32[] RlowerBounds = { 0, -1, 0 };
                Int32[] Rlengths = { vinLength + 1, (int)(fontsizeY + 2), (int)(fontsizeX) };

                Int32[] ClowerBounds = { 0, -1, -1 };
                Int32[] Clengths = { vinLength, (int)(fontsizeY + 2), (int)(fontsizeX + 2) };

                FontData4Send[,,] RasterData = (FontData4Send[,,])Array.CreateInstance(typeof(FontData4Send), Rlengths, RlowerBounds);   // BLU
                FontData4Send[,,] AllClrData = (FontData4Send[,,])Array.CreateInstance(typeof(FontData4Send), Clengths, ClowerBounds);   // BLU

                currMarkInfo.senddata.sendDataFire.Clear();
                currMarkInfo.senddata.sendDataClean.Clear();

                //List<Vector3D> recvPoint = new List<Vector3D>();
                ImageProcessManager.GetStartPointLinear(vinLength, CP, SP, pattern.fontValue.pitch, pattern.fontValue.rotateAngle, ref Rev_Point);

                Vector3D[] LeftRightSP = new Vector3D[2];
                LeftRightSP[0] = ImageProcessManager.Rotate_Point2(SP.X - pattern.headValue.rasterSP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
                LeftRightSP[1] = ImageProcessManager.Rotate_Point2(SP.X + totWidth + pattern.headValue.rasterEP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);

                ImageProcessManager.GetFontDataOneEx(pattern.laserValue.charFull, pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);

                for (i = 0; i < Rev_Point.Count; i++)
                {
                    if (vin.Substring(i, 1) != " ")      //Space Skip
                    {
                        lineData = fontData[i];
                        FontDataClass fd = new FontDataClass();
                        for (j = 0; j < lineData.Count; j++)
                        {
                            fd = (FontDataClass)lineData[j].Clone();
                            // ABS mm
                            M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
                            // Font offset compensation
                            M1.Y = Rev_Point[i].Y + (fd.vector3d.Y - shiftVal) * Step_H;
                            M1.Z = SP.Z;

                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                            M1.Z = SP.Z;

                            // TM SHIN
                            M1.X -= CP.X; M1.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                            M.X += CP.X; M.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
                            double Cz = 0.0;
                            Cz = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

                            // Change mm to BLU(Unit 0.01mm)
                            M.X *= pattern.headValue.stepLength; M.Y *= pattern.headValue.stepLength; M.Z *= pattern.headValue.stepLength;
                            Cz *= pattern.headValue.stepLength;

                            FontData4Send font4Send = new FontData4Send();
                            font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                            font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
                            font4Send.mC = (UInt16)(Cz + 0.5);
                            font4Send.mI = (UInt16)(fd.vector3d.X + 0.5);

                            if (pattern.laserValue.density == 1)
                            {
                                font4Send.mF = 0;
                                RasterData[i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
                            }
                            else
                            {
                                var m_font = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
                                currMarkInfo.senddata.sendDataFire.Add(m_font);
                            }

                            idx++;
                        }

                        idx = 0;
                        FontDataClass fdc = new FontDataClass();

                        //  make all clear data : 0, char dot clear data : 1
                        if (pattern.laserValue.charClean == 0 || pattern.laserValue.charClean == 1)
                        {
                            for (j = 0; j < lineDataClean.Count - 1; j++)
                            {
                                fdc = (FontDataClass)lineDataClean[j].Clone();

                                // ABS mm
                                M1.X = Rev_Point[i].X + fdc.vector3d.X * Step_W;
                                // Font offset compensation
                                //M1.Y = Rev_Point[i].Y + ((double.Parse(xy_[1])) - SF) * Step_H;
                                M1.Y = Rev_Point[i].Y + (fdc.vector3d.Y - shiftVal2) * Step_H;
                                M1.Z = SP.Z;

                                //M1 = Mode_File.Rotate_Point(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, double.Parse(Angle));
                                M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                                M1.Z = SP.Z;

                                // TM SHIN
                                M1.X -= CP.X; M1.Y -= CP.Y;

                                M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                                M.X += CP.X; M.Y += CP.Y;
                                double Mt = M.Z;
                                M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
                                double Cz = 0.0;
                                Cz = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis mm

                                // Change mm to BLU(Unit 0.01mm)
                                M.X *= pattern.headValue.stepLength; M.Y *= pattern.headValue.stepLength; M.Z *= pattern.headValue.stepLength;
                                Cz *= pattern.headValue.stepLength;

                                FontData4Send font4Send = new FontData4Send();
                                font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                                font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fdc.Flag;
                                font4Send.mC = (UInt16)(Cz + 0.5);
                                font4Send.mI = (UInt16)(fdc.vector3d.X + 0.5);

                                if (pattern.laserValue.density == 1)
                                {
                                    font4Send.mF = 0;
                                    AllClrData[i, (int)(fdc.vector3d.Y - shiftVal2), (int)Math.Round(fdc.vector3d.X)] = (FontData4Send)font4Send.Clone();
                                }
                                else
                                {
                                    var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
                                    currMarkInfo.senddata.sendDataClean.Add(m_clean);
                                }
                                idx++;
                            }
                        }

                        FontDataClass fd2 = new FontDataClass();

                        if (pattern.laserValue.charClean == 1)  // dot by dot clear only
                        {
                            for (j = 0; j < lineData.Count - 1; j++)
                            {
                                fd2 = lineData[j];

                                FontData4Send font4Send = new FontData4Send();

                                font4Send = RasterData[i, (int)(fd2.vector3d.Y - shiftVal), (int)Math.Round(fd2.vector3d.X)];
                                if (pattern.laserValue.density == 1)
                                {
                                    font4Send.mF = 0;
                                    AllClrData[i, (int)(fd2.vector3d.Y - shiftVal), (int)Math.Round(fd2.vector3d.X)] = font4Send;
                                }
                                else
                                {
                                    var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
                                    currMarkInfo.senddata.sendDataClean.Add(m_clean);
                                }

                                idx++;
                            }
                        }
                    }
                }

                //
                // Make firing/cleanning data
                //
                idx = 0;
                int ifontsizeX = (int)(fontsizeX + 0.5);
                int ifontsizeY = (int)(fontsizeY + 0.5);

                if (pattern.laserValue.density == 1)       // Dot Firing
                {
                    // Calculate No of Dot point
                    ushort[,] NoPoints = (ushort[,])Array.CreateInstance(typeof(ushort), new int[2] { ifontsizeY + 2, 1 }, new int[2] { -1, 0 });

                    for (int y = -1; y < ifontsizeY + 1; y++)
                    {
                        for (i = 0; i < vinLength; i++)
                        {
                            for (int x = 0; x < ifontsizeX; x++)
                            {
                                if (RasterData[i, y, x] != null) NoPoints[y, 0]++;         // Data Number of fire data
                            }
                        }
                    }


                    ////
                    Debug.WriteLine("");

                    //
                    // Make Jump/Start data
                    for (i = -1; i < ifontsizeY + 1; i++)
                    {
                        for (j = 0; j < 2; j++)         // Jump/Start XXXXX
                        {
                            M1.X = LeftRightSP[j].X;
                            M1.Y = LeftRightSP[j].Y + (double)i * Step_H;
                            M1.Z = SP.Z;

                            //M1 = Mode_File.Rotate_Point(M1.X, M1.Y, LeftRightSP[j].X, LeftRightSP[j].Y, double.Parse(Angle));
                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, LeftRightSP[j].X, LeftRightSP[j].Y, pattern.fontValue.rotateAngle);
                            M1.Z = SP.Z;

                            M1.X -= CP.X; M1.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                            M.X += CP.X; M.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;       // Fire Z Axis
                            double Cz = 0.0;
                            Cz = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Z Axis
                            Debug.WriteLine(String.Format("J{0:D3}=>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3}", idx, M.X, M.Y, M.Z, Cz));

                            // Change to BLU(Unit 0.01mm)
                            M.X *= pattern.headValue.stepLength; M.Y *= pattern.headValue.stepLength; M.Z *= pattern.headValue.stepLength;
                            Cz *= pattern.headValue.stepLength;

                            FontData4Send font4Send = new FontData4Send();
                            font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                            font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); //font4Send.mF = (byte)fd.Flag;
                            font4Send.mC = (UInt16)(Cz + 0.5);

                            RasterData[vinLength, i, j] = font4Send;
                            idx++;
                        }
                    }

                    idx = 0;
                    ushort[,] NoPointsC = (ushort[,])Array.CreateInstance(typeof(ushort), new int[2] { ifontsizeY + 2, 1 }, new int[2] { -1, 0 });
                    //
                    // Check Dot Pitching 0.6
                    if (pattern.laserValue.charClean == 1)
                    {
                        for (int y = 0; y < ifontsizeY; y++)
                            for (i = 0; i < vinLength; i++)
                                for (int x = 0; x < ifontsizeX; x++)
                                {
                                    if (checkDistance(AllClrData, i, y, x) == false)
                                    {
                                        if (AllClrData[i, y, x].mI == (double)x)
                                        {
                                            AllClrData[i, y, x] = null;
                                            Debug.WriteLine(String.Format("NULL=>V{0:D3},X{1:D3},Y{2:D3}", i, x, y));
                                        }
                                    }
                                }
                    }

                    for (int y = -1; y < ifontsizeY + 1; y++)
                        for (i = 0; i < vinLength; i++)
                            for (int x = -1; x < ifontsizeX + 1; x++)
                                if (AllClrData[i, y, x] != null) NoPointsC[y, 0]++;    // Data Number of clean data

                    //
                    //
                    //
                    if (pattern.headValue.spatterType == 0)  // 0 : Back
                    {
                        //
                        // Make Firing & cleanning Data for back spatter at 0 degree
                        if (pattern.fontValue.rotateAngle == 0.0)
                        {
                            // Fire & then Clean : Normal case
                            {
                                //
                                // Make firing Data for 0 degree, back spatter [B0D]
                                StringBuilder sb = new StringBuilder();
                                bool DirRight = true;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (DirRight == true)
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                        for (i = 0; i < vinLength; i++)
                                            for (int x = 0; x < ifontsizeX; x++)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        //Mode_File.SendData.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX - 1; x >= 0; x--)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        //Mode_File.SendData.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                }

                                //
                                //  Make Cleaning Data for 0 degree, back spatter [B0C]
                                sb = new StringBuilder();
                                ushort x2 = 0, y2 = 0, c2 = 0;

                                //Mode_File.TwoLineDisplay = false;
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = ifontsizeY; y >= -1; y--)
                                {
                                    if (NoPointsC[y, 0] > 0)
                                    {
                                        if (DirRight == false)
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX; x >= -1; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                    }
                                                }
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = -1; x < ifontsizeX + 1; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                    }
                                                }
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    }   // NoPointsC
                                    else
                                    {
                                        //Mode_File.TwoLineDisplay = true;
                                        currMarkInfo.checkdata.TwoLineDisplay = true;
                                    }
                                }

                            } // CombineFireClean if-else
                        }
                        else
                        {
                            // 
                            // Make firing data at 180 degree for back spatter [B180D]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                            {
                                if (DirRight == false)
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 180 degree, back spatter [B180C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;
                            currMarkInfo.checkdata.TwoLineDisplay = false;
                            for (int y = -1; y < ifontsizeY + 1; y++)
                            {
                                if (NoPointsC[y, 0] > 0)
                                {
                                    if (DirRight == false)
                                    {
                                        x2 = (ushort)RasterData[vinLength, y, 1].mX;
                                        y2 = (ushort)RasterData[vinLength, y, 1].mY;
                                        c2 = (ushort)RasterData[vinLength, y, 1].mC;
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)RasterData[vinLength, y, 0].mX;
                                        y2 = (ushort)RasterData[vinLength, y, 0].mY;
                                        c2 = (ushort)RasterData[vinLength, y, 0].mC;
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX; x >= -1; x--)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = 0; i < vinLength; i++)
                                            for (int x = -1; x < ifontsizeX + 1; x++)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                } //
                                else
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = true;
                                }
                            }

                        }   // Back spatter
                    }
                    else    // Front spatter
                    {
                        //
                        // Make Firing & cleanning Data for front spatter at 0 degree
                        if (pattern.fontValue.rotateAngle == 0.0)
                        {
                            //
                            // Make firing Data for 0 degree, front spatter [F0D]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                            {
                                if (DirRight == true)
                                {   // Fire Data string for even line ->
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string for odd line <-
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 0 degree, front spatter [F0C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;

                            currMarkInfo.checkdata.TwoLineDisplay = false;
                            for (int y = -1; y < ifontsizeY + 1; y++)
                            {
                                if (NoPointsC[y, 0] > 0)
                                {
                                    if (DirRight == false)
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX; x >= -1; x--)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = 0; i < vinLength; i++)
                                            for (int x = -1; x < ifontsizeX + 1; x++)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                }
                                else
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = true;
                                }
                            }
                        }
                        else
                        {
                            // 
                            // Make firing data at 180 degree for front spatter [F180D]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = 0; y < ifontsizeY; y++)
                            {
                                if (DirRight == false) // odd line <-
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string even line ->
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 180 degree, front spatter [F180C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;

                            currMarkInfo.checkdata.TwoLineDisplay = false;
                            for (int y = ifontsizeY; y >= -1; y--)
                            {
                                if (NoPointsC[y, 0] > 0)
                                {
                                    if (DirRight == true)
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = 0; i < vinLength; i++)
                                            for (int x = -1; x < ifontsizeX + 1; x++)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX; x >= -1; x--)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                }
                                else
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = true;
                                }
                            }

                        }   // front spatter
                        if (pattern.laserValue.charClean != 0)
                        {
                            currMarkInfo.checkdata.TwoLineDisplay = false;
                        }
                    }   // Back/front spatter
                }   // Dot firing

                //M_Count = idx;
                //Mark_Counter++;

                //Mode_File.Download_Data = true;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }

            return retval;

            // Local function

            Vector3D getRodrigueRotation(Vector3D XY0)
            {
                Vector3D Tmp = new Vector3D();
                Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
                Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
                Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

                return Tmp;
            }

            bool checkDistance(FontData4Send[,,] AllClrData, int ii, int yy, int xx)
            {
                if (AllClrData[ii, yy, xx - 1] != null)
                {
                    if (Math.Abs(AllClrData[ii, yy, xx].mI - AllClrData[ii, yy, xx - 1].mI) < 0.599)
                        return false;
                }
                if (AllClrData[ii, yy, xx + 1] != null)
                {
                    if (Math.Abs(AllClrData[ii, yy, xx].mI - AllClrData[ii, yy, xx + 1].mI) < 0.599)
                        return false;
                }
                return true;
            }

        }

        public async Task<ITNTResponseArgs> Start_TEXT(string cmd, string vin, PatternValueEx pattern)    // Making Fire/Clean data by TM SHIN
        {
            string className = "SetControllerWindow2";
            string funcName = "Start_TEXT";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int vinLength = 0;

            Vector3D SP0 = new Vector3D();
            Vector3D SP = new Vector3D();
            Vector3D CP = new Vector3D();

            Vector3D VectorNormal = new Vector3D();
            Vector3D VectorRot = new Vector3D();
            List<Vector3D> Rev_Point = new List<Vector3D>();

            double Step_W;
            double Step_H;
            string value = "";
            byte headType = 0;
            VinNoInfo vininfo = new VinNoInfo();
            //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
            List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
            double fontsizeX = 0, fontsizeY = 0, shiftVal = 0;
            double fontsizeX2 = 0, fontsizeY2 = 0, shiftVal2 = 0;
            double cleanPosition = 0;
            string errCode = "";
            double totWidth = 0;
            double R11, R12, R13, R21, R22, R23, R31, R32, R33;
            int i, j;
            int idx = 0;
            int ifontsizeX = 0, ifontsizeY = 0, ishiftVal = 0;

            Vector3D M1 = new Vector3D();                                   // for fire data mm
            Vector3D M2 = new Vector3D();                                   // for clean data mm
            Vector3D M = new Vector3D();
            Vector3D C = new Vector3D();
            //byte fontdirection = 0;

            try
            {
                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    retval.execResult = -1;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN LENGTH IS INVALID (" + vinLength.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out headType);

                vininfo.vinNo = vin;
                vininfo.fontName = pattern.fontValue.fontName;
                vininfo.width = pattern.fontValue.width;
                vininfo.height = pattern.fontValue.height;
                vininfo.pitch = pattern.fontValue.pitch;
                vininfo.thickness = pattern.fontValue.thickness;
                ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 0, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref errCode);

                m_currCMD = (byte)'S';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SolOnOffTime ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'d';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(pattern.speedValue.dwellTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dwellTimeSet ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                if (currMarkInfo.checkdata.bReady == false)
                {
                    CheckAreaData chkdata = new CheckAreaData();
                    chkdata = await Range_Test(cmd, vin, pattern, 0, 0);
                    if (chkdata.execResult != 0)
                    {
                        return retval;
                    }
                    else
                        currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                }

                ifontsizeX = (int)(fontsizeX + 0.5);
                ifontsizeY = (int)(fontsizeY + 0.5);
                ishiftVal = (int)(shiftVal + 0.5);

                CP = pattern.positionValue.center3DPos;
                CP.Z += pattern.headValue.distance0Position;
                cleanPosition = pattern.laserValue.cleanPosition;
                cleanPosition += CP.Z;         // Relative Cleanning postion

                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;
                SP0.Z = 0;
                SP = CP - SP0;
                SP.Z = 0.0d;

                VectorNormal = currMarkInfo.checkdata.NormalDir;
                VectorRot.X = -VectorNormal.Y;
                VectorRot.Y = VectorNormal.X;
                VectorRot.Z = 0.0;

                double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);
                //if (sqXY <= 0)
                //{
                //    VectorRot.X = 0;
                //    VectorRot.Y = 0;
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "sqXY is INVALID (" + sqXY.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                //}
                if(sqXY != 0)
                {
                    VectorRot.X /= sqXY;
                    VectorRot.Y /= sqXY;
                }

                // Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
                bool skipRot = false;
                double cosValue = 0;
                if (VectorNormal.Length != 0)
                    cosValue = VectorNormal.Z / VectorNormal.Length;

                double sinValue = Math.Sqrt(1.0d - cosValue * cosValue);

                if (cosValue > 0.9999986111)
                {      // 0.1 mm difference between 60mm
                    skipRot = true;
                    R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
                }
                else
                {
                    R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
                    R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
                    R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
                    R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
                    R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
                    R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
                    R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
                    R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
                    R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
                }
                /////
                Step_W = pattern.fontValue.width / (fontsizeX - 1.0);
                Step_H = pattern.fontValue.height / (fontsizeY - 1.0);

                FontData4Send[,,] RasterData = new FontData4Send[vinLength + 1, ifontsizeY, ifontsizeX];     // BLU
                FontData4Send[,,] AllClrData = new FontData4Send[vinLength + 1, ifontsizeY, ifontsizeX];

                currMarkInfo.senddata.sendDataFire.Clear();
                currMarkInfo.senddata.sendDataClean.Clear();

                //List<Vector3D> recvPoint = new List<Vector3D>();
                ImageProcessManager.GetStartPointLinear(vinLength, CP, SP, pattern.fontValue.pitch, pattern.fontValue.rotateAngle, ref Rev_Point);

                Vector3D[] LeftRightSP = new Vector3D[2];
                LeftRightSP[0] = ImageProcessManager.Rotate_Point2(SP.X - pattern.headValue.rasterSP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
                LeftRightSP[1] = ImageProcessManager.Rotate_Point2(SP.X + totWidth + pattern.headValue.rasterEP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
                List<FontDataClass> lineData = new List<FontDataClass>();
                List<FontDataClass> lineDataClean = new List<FontDataClass>();
                ImageProcessManager.GetFontDataOneEx('^', pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);

                for (i = 0; i < Rev_Point.Count; i++)
                {
                    if (vin.Substring(i, 1) != " ")      //Space Skip
                    {
                        //lineData = MyData[i];
                        lineData = fontData[i];
                        FontDataClass fd = new FontDataClass();
                        for (j = 0; j < lineData.Count; j++)
                        {
                            fd = (FontDataClass)lineData[j].Clone();
                            // ABS mm
                            M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
                            M2.X = Rev_Point[i].X + Math.Round(fd.vector3d.X) * Step_W;

                            // Font offset compensation
                            M1.Y = Rev_Point[i].Y + fd.vector3d.Y * Step_H;
                            M2.Y = M1.Y;
                            M1.Z = M2.Z = SP.Z;

                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                            M1.Z = SP.Z;
                            M2 = ImageProcessManager.Rotate_Point2(M2.X, M2.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                            M2.Z = SP.Z;

                            // TM SHIN
                            M1.X -= CP.X; M1.Y -= CP.Y;
                            M2.X -= CP.X; M2.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);
                            C = (skipRot == true) ? M2 : getRodrigueRotation(M2);

                            M.X += CP.X; M.Y += CP.Y;
                            C.X += CP.X; C.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
                            C.Z = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

                            //Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3},{5,7:F3}", idx, M.X, M.Y, M.Z, C.X, C.Z));

                            // Change to BLU(Unit 0.01mm)
                            M = M * pattern.headValue.stepLength;
                            C = C * pattern.headValue.stepLength;
                            //M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length;
                            //C.X *= (double)Mode_File.Step_Length; C.Y *= (double)Mode_File.Step_Length; C.Z *= (double)Mode_File.Step_Length;

                            FontData4Send font4Send = new FontData4Send();

                            font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                            font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
                            font4Send.mC = (UInt16)(C.Z + 0.5);
                            font4Send.mI = (UInt16)(C.X + 0.5);

                            if (pattern.laserValue.density == 1)
                            {
                                font4Send.mF = 0;
                                RasterData[i, (int)fd.vector3d.Y, (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
                                //Debug.WriteLine(String.Format("RASTER-({0},{1},{2}):{3}/{4}/{5}/{6}/{7}/{8}/{9}/{10}", i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X), font4Send.cN, font4Send.fN, font4Send.mC, font4Send.mF, font4Send.mI, font4Send.mX, font4Send.mY, font4Send.mZ));
                            }
                            else
                            {
                                var m_font = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
                                currMarkInfo.senddata.sendDataFire.Add(m_font);
                                //var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
                                //currMarkInfo.senddata.sendDataClean.Add(m_clean);
                            }

                            idx++;
                        }


                        idx = 0;

                        lineData = fontData[i].ToList();
                        if (pattern.laserValue.charClean == 0)
                            lineData = lineDataClean.ToList();
                        //FontDataClass fdClean = new FontDataClass();
                        fd.Clean();
                        for (j = 0; j < lineData.Count; j++)
                        {
                            fd = (FontDataClass)lineData[j].Clone();
                            // ABS mm
                            M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
                            M2.X = Rev_Point[i].X + Math.Round(fd.vector3d.X) * Step_W;

                            // Font offset compensation
                            M1.Y = Rev_Point[i].Y + fd.vector3d.Y * Step_H;
                            //M1.Y = Rev_Point[i].Y + (fd.vector3d.Y - shiftVal) * Step_H;
                            M2.Y = M1.Y;
                            M1.Z = M2.Z = SP.Z;

                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                            M1.Z = SP.Z;
                            M2 = ImageProcessManager.Rotate_Point2(M2.X, M2.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                            M2.Z = SP.Z;

                            // TM SHIN
                            M1.X -= CP.X; M1.Y -= CP.Y;
                            M2.X -= CP.X; M2.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);
                            C = (skipRot == true) ? M2 : getRodrigueRotation(M2);

                            M.X += CP.X; M.Y += CP.Y;
                            C.X += CP.X; C.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
                            C.Z = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

                            //Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3},{5,7:F3}", idx, M.X, M.Y, M.Z, C.X, C.Z));

                            // Change to BLU(Unit 0.01mm)
                            M = M * pattern.headValue.stepLength;
                            C = C * pattern.headValue.stepLength;
                            //M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length;
                            //C.X *= (double)Mode_File.Step_Length; C.Y *= (double)Mode_File.Step_Length; C.Z *= (double)Mode_File.Step_Length;

                            FontData4Send font4Send = new FontData4Send();

                            font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                            font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
                            font4Send.mC = (UInt16)(C.Z + 0.5);
                            font4Send.mI = (UInt16)(C.X + 0.5);

                            if (pattern.laserValue.density == 1)
                            {
                                font4Send.mF = 0;
                                AllClrData[i, (int)fd.vector3d.Y, (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
                                //AllClrData[i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
                                //Debug.WriteLine(String.Format("RASTER-({0},{1},{2}):{3}/{4}/{5}/{6}/{7}/{8}/{9}/{10}", i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X), font4Send.cN, font4Send.fN, font4Send.mC, font4Send.mF, font4Send.mI, font4Send.mX, font4Send.mY, font4Send.mZ));
                            }
                            else
                            {
                                var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
                                currMarkInfo.senddata.sendDataClean.Add(m_clean);
                                //var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
                                //currMarkInfo.senddata.sendDataClean.Add(m_clean);
                            }

                            idx++;
                        }
                    }
                }





                //Debug.WriteLine("RASTER DATA");
                //for (i = 0; i < vin.Length; i++)
                //{
                //    for(j = 0; j < fontsizeY; j++)
                //        for(int k = 0; k < fontsizeX; k++)
                //            Debug.WriteLine(String.Format("RASTER[{0},{1},{2}] = {3},{4},{5},{6},{7}", i, j, k, RasterData[i, j, k].mX, RasterData[i, j, k].mY, RasterData[i, j, k].mZ, RasterData[i, j, k].mI, RasterData[i, j, k].mC));
                //}

                idx = 0;

                if (pattern.laserValue.density == 1)       // Dot Firing
                {
                    // Calculate No of Dot point
                    ushort[] NoPoints = new ushort[ifontsizeY];
                    ushort[] NoPointsC = new ushort[ifontsizeY];

                    for (int y = 0; y < ifontsizeY; y++)
                    {
                        for (i = 0; i < vinLength; i++)
                        {
                            for (int x = 0; x < ifontsizeX; x++)
                            {
                                if (RasterData[i, y, x] != null)
                                    NoPoints[y]++;         // Data Number of fire data
                            }
                        }
                    }


                    ////
                    Debug.WriteLine("");

                    //
                    // Make Jump/Start data
                    for (i = 0; i < ifontsizeY; i++)
                    {
                        for (j = 0; j < 2; j++)         // Jump/Start XXXXX
                        {
                            M1.X = LeftRightSP[j].X;
                            M1.Y = LeftRightSP[j].Y + (double)i * Step_H;
                            M1.Z = SP.Z;

                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, LeftRightSP[j].X, LeftRightSP[j].Y, pattern.fontValue.rotateAngle);// double.Parse(Angle));
                            M1.Z = SP.Z;

                            M1.X -= CP.X; M1.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                            M.X += CP.X; M.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;       // Fire Z Axis
                            C.Z = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Z Axis
                            Debug.WriteLine(String.Format("J{0:D3}=>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3}", idx, M.X, M.Y, M.Z, C.Z));

                            // Change to BLU(Unit 0.01mm)
                            //M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length; C.Z *= (double)Mode_File.Step_Length;
                            M *= pattern.headValue.stepLength;
                            C.Z *= pattern.headValue.stepLength;

                            //M_FONT FontData = new M_FONT();
                            FontData4Send FontData = new FontData4Send();

                            FontData.cN = (byte)i; FontData.fN = (byte)j;
                            FontData.mX = (UInt16)(M.X + 0.5); FontData.mY = (UInt16)(M.Y + 0.5); FontData.mZ = (UInt16)(M.Z + 0.5);
                            FontData.mC = (UInt16)(C.Z + 0.5);
                            FontData.mI = 0;
                            FontData.mF = 0;

                            RasterData[vinLength, i, j] = FontData;

                            idx++;
                        }
                    }

                    idx = 0;
                    //
                    for (int y = 0; y < ifontsizeY; y++)
                        for (i = 0; i < vinLength; i++)
                            for (int x = 0; x < ifontsizeX; x++)
                                if (AllClrData[i, y, x] != null)
                                    NoPointsC[y]++;    // Data Number of clean data
                                                                                    //
                    if (pattern.headValue.spatterType == 0)  // 0 : Back
                    {
                        //
                        // Make Firing & cleanning Data for back spatter at 0 degree
                        if (pattern.fontValue.rotateAngle == 0.0)
                        {
                            if (pattern.laserValue.combineFireClean != 0)
                            {
                                //
                                // Make firing | clean Data for 0 degree, back spatter [1FC]
                                StringBuilder sb = new StringBuilder();
                                ushort x2 = 0, y2 = 0, c2 = 0;

                                bool DirRight = true;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (DirRight == true)
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y]);
                                        for (i = 0; i < vinLength; i++)
                                            for (int x = 0; x < ifontsizeX; x++)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        //Mode_File.SendData.Add(sb.ToString());
                                        //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y]);
                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX - 1; x >= 0; x--)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        //Mode_File.SendData.Add(sb.ToString());
                                        //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;

                                    //
                                    //  Make Cleaning Data for 0 degree, back spatter [1C]
                                    if (NoPointsC[y] > 0)
                                    {
                                        if (DirRight == false)
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                            //Mode_File.SendData.Add(sb.ToString());
                                            //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = 0; x < ifontsizeX; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                            //Mode_File.SendData.Add(sb.ToString());
                                            //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    }   // NoPointsC
                                    else
                                    {
                                        currMarkInfo.checkdata.TwoLineDisplay = false;
                                    }
                                }
                            }
                            else  // Fire & Clean
                            {
                                //
                                // Make firing Data for 0 degree, back spatter [1F]
                                StringBuilder sb = new StringBuilder();
                                bool DirRight = true;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (DirRight == true)
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y]);
                                        for (i = 0; i < vinLength; i++)
                                            for (int x = 0; x < ifontsizeX; x++)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        //Mode_File.SendData.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y]);
                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX - 1; x >= 0; x--)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        //Mode_File.SendData.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                }

                                //
                                //  Make Cleaning Data for 0 degree, back spatter [1C]
                                sb = new StringBuilder();
                                ushort x2 = 0, y2 = 0, c2 = 0;

                                if (ifontsizeY > 10)  // Y Font Size > 10
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = true;
                                    for (int y = ifontsizeY - 1; y >= 0; y--)
                                    {
                                        if (NoPointsC[y] > 0)
                                        {
                                            if (DirRight == false)
                                            {
                                                x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                                y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                                y2 += (ushort)pattern.laserValue.cleanDelta;
                                                c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                                x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                                y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                                y2 += (ushort)pattern.laserValue.cleanDelta;
                                                c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                                for (i = vinLength - 1; i >= 0; i--)
                                                    for (int x = ifontsizeX - 1; x >= 0; x--)
                                                    {
                                                        if (AllClrData[i, y, x] != null)
                                                        {
                                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                        }
                                                    }
                                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                                //Mode_File.SendClean.Add(sb.ToString());
                                                //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                                sb.Length = 0;
                                            }
                                            else
                                            {
                                                x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                                y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                                y2 += (ushort)pattern.laserValue.cleanDelta;
                                                c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                                x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                                y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                                y2 += (ushort)pattern.laserValue.cleanDelta;
                                                c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                                for (i = 0; i < vinLength; i++)
                                                    for (int x = 0; x < ifontsizeX; x++)
                                                    {
                                                        if (AllClrData[i, y, x] != null)
                                                        {
                                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                        }
                                                    }
                                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                                //Mode_File.SendClean.Add(sb.ToString());
                                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                                sb.Length = 0;
                                            }
                                            DirRight = !DirRight;
                                        }   // NoPointsC
                                        else
                                        {
                                            currMarkInfo.checkdata.TwoLineDisplay = false;
                                        }
                                    }
                                }
                                else  // Y Font Size <= 10
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = false;
                                    for (int y = ifontsizeY - 1; y >= 0; y--)
                                    {
                                        if (NoPointsC[y] > 0)
                                        {
                                            if (DirRight == false)
                                            {
                                                x2 = (ushort)RasterData[vinLength, y, 1].mX;
                                                y2 = (ushort)RasterData[vinLength, y, 1].mY;
                                                //y2 += CDelta;
                                                c2 = (ushort)RasterData[vinLength, y, 1].mC;
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                                x2 = (ushort)RasterData[vinLength, y, 0].mX;
                                                y2 = (ushort)RasterData[vinLength, y, 0].mY;
                                                //y2 += CDelta;
                                                c2 = (ushort)RasterData[vinLength, y, 0].mC;
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                                for (i = vinLength - 1; i >= 0; i--)
                                                    for (int x = ifontsizeX - 1; x >= 0; x--)
                                                    {
                                                        if (AllClrData[i, y, x] != null)
                                                        {
                                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                        }
                                                    }
                                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                                //Mode_File.SendClean.Add(sb.ToString());
                                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                                sb.Length = 0;
                                            }
                                            else
                                            {
                                                x2 = (ushort)RasterData[vinLength, y, 0].mX;
                                                y2 = (ushort)RasterData[vinLength, y, 0].mY;
                                                //y2 += CDelta;
                                                c2 = (ushort)RasterData[vinLength, y, 0].mC;
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                                x2 = (ushort)RasterData[vinLength, y, 1].mX;
                                                y2 = (ushort)RasterData[vinLength, y, 1].mY;
                                                //y2 += CDelta;
                                                c2 = (ushort)RasterData[vinLength, y, 1].mC;
                                                sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                                for (i = 0; i < vinLength; i++)
                                                    for (int x = 0; x < ifontsizeX; x++)
                                                    {
                                                        if (AllClrData[i, y, x] != null)
                                                        {
                                                            sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                        }
                                                    }
                                                currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                                //Mode_File.SendClean.Add(sb.ToString());
                                                Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                                sb.Length = 0;
                                            }
                                            DirRight = !DirRight;

                                        }   // NoPointsC
                                    }
                                }
                            } // CombineFireClean if-else
                        }
                        else
                        {
                            // 
                            // Make firing data at 180 degree for back spatter [2F]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                            {
                                if (DirRight == false)
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    //Mode_File.SendData.Add(sb.ToString());
                                    //Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    //Mode_File.SendData.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 180 degree, back spatter [2C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;
                            if (ifontsizeY > 10)  // Y Font Size > 10
                            {
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (NoPointsC[y] > 0)
                                    {
                                        if (DirRight == false)
                                        {
                                            x2 = (ushort)RasterData[vinLength, y, 1].mX;
                                            y2 = (ushort)RasterData[vinLength, y, 1].mY;
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)RasterData[vinLength, y, 1].mC;
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)RasterData[vinLength, y, 0].mX;
                                            y2 = (ushort)RasterData[vinLength, y, 0].mY;
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)RasterData[vinLength, y, 0].mC;
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            y2 += (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = 0; x < ifontsizeX; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    } //
                                    else
                                    {
                                        currMarkInfo.checkdata.TwoLineDisplay = true;
                                    }
                                }
                            }
                            else  // Y Font Size <= 10
                            {
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (NoPointsC[y] > 0)
                                    {
                                        if (DirRight == false)
                                        {
                                            x2 = (ushort)RasterData[vinLength, y, 1].mX;
                                            y2 = (ushort)RasterData[vinLength, y, 1].mY;
                                            //y2 += CDelta;
                                            c2 = (ushort)RasterData[vinLength, y, 1].mC;
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)RasterData[vinLength, y, 0].mX;
                                            y2 = (ushort)RasterData[vinLength, y, 0].mY;
                                            //y2 += CDelta;
                                            c2 = (ushort)RasterData[vinLength, y, 0].mC;
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            //y2 += CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            //y2 += CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = 0; x < ifontsizeX; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            //Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    } //
                                }
                            }
                        }   // Back spatter
                    }
                    else    // Front spatter
                    {
                        //
                        // Make Firing & cleanning Data for front spatter at 0 degree
                        if (pattern.fontValue.rotateAngle == 0.0)
                        {
                            //
                            // Make firing Data for 0 degree, front spatter [3F]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                            {
                                if (DirRight == true)
                                {   // Fire Data string for even line ->
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    //Mode_File.SendData.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string for odd line <-
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    //Mode_File.SendData.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 0 degree, front spatter [3C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;

                            if (ifontsizeY > 10)  // Y Font Size > 10
                            {
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (NoPointsC[y] > 0)
                                    {
                                        if (DirRight == false)
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = 0; x < ifontsizeX; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    }
                                    else
                                    {
                                        currMarkInfo.checkdata.TwoLineDisplay = true;
                                    }
                                }
                            }
                            else  // Y Font Size <= 10
                            {
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (NoPointsC[y] > 0)
                                    {
                                        if (DirRight == false)
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = 0; x < ifontsizeX; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 
                            // Make firing data at 180 degree for front spatter [4F]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = 0; y < ifontsizeY; y++)
                            {
                                if (DirRight == false) // odd line <-
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    //Mode_File.SendData.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string even line ->
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    //Mode_File.SendData.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 180 degree, front spatter [4C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;
                            if (ifontsizeY > 10)  // Y Font Size > 10
                            {
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = ifontsizeY - 1; y >= 0; y--)
                                {
                                    if (NoPointsC[y] > 0)
                                    {
                                        if (DirRight == true)
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = 0; x < ifontsizeX; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            y2 -= (ushort)pattern.laserValue.cleanDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    }
                                    else
                                    {
                                        currMarkInfo.checkdata.TwoLineDisplay = true;
                                    }
                                }
                            }
                            else  // Y Font Size <= 10
                            {
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = ifontsizeY - 1; y >= 0; y--)
                                {
                                    if (NoPointsC[y] > 0)
                                    {
                                        if (DirRight == true)
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = 0; x < ifontsizeX; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            //y2 -= CDelta;
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mI);
                                                    }
                                                }
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    }
                                }
                            }
                        }   // front spatter
                        if (pattern.laserValue.charClean != 0)
                        {
                            currMarkInfo.checkdata.TwoLineDisplay = false;
                        }
                    }   // Back/front spatter
                }   // Dot firing
                currMarkInfo.checkdata.TwoLineDisplay = true;

                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }


            //
            // Make firing/cleanning data
            //

            // Local function

            Vector3D getRodrigueRotation(Vector3D XY0)
            {
                Vector3D Tmp = new Vector3D();
                Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
                Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
                Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

                return Tmp;
            }

        }

        public async Task<ITNTResponseArgs> Start_DOTMISSING(string cmd, string vin, PatternValueEx pattern, List<string> DotMiss)    // Making Dot Missing data by TM SHIN
        {
            string className = "SetControllerWindow2";
            string funcName = "Start_DOTMISSING";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int vinLength = 0;

            Vector3D SP0 = new Vector3D();
            Vector3D SP = new Vector3D();
            Vector3D CP = new Vector3D();

            Vector3D VectorNormal = new Vector3D();
            Vector3D VectorRot = new Vector3D();
            List<Vector3D> Rev_Point = new List<Vector3D>();

            double Step_W;
            double Step_H;
            string value = "";
            byte headType = 0;
            VinNoInfo vininfo = new VinNoInfo();
            //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
            List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
            double fontsizeX = 0, fontsizeY = 0, shiftVal = 0;
            double fontsizeX2 = 0, fontsizeY2 = 0, shiftVal2 = 0;
            double cleanPosition = 0;
            string errCode = "";
            double totWidth = 0;
            double R11, R12, R13, R21, R22, R23, R31, R32, R33;
            int i, j;
            int idx = 0;
            int ifontsizeX = 0, ifontsizeY = 0, ishiftVal = 0;

            Vector3D M1 = new Vector3D();                                   // for fire data mm
            Vector3D M2 = new Vector3D();                                   // for clean data mm
            Vector3D M = new Vector3D();
            Vector3D C = new Vector3D();

            try
            {
                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    retval.execResult = -1;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN LENGTH IS INVALID (" + vinLength.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out headType);

                vininfo.vinNo = vin;
                vininfo.fontName = pattern.fontValue.fontName;
                vininfo.width = pattern.fontValue.width;
                vininfo.height = pattern.fontValue.height;
                vininfo.pitch = pattern.fontValue.pitch;
                vininfo.thickness = pattern.fontValue.thickness;
                ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 0, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref errCode);

                m_currCMD = (byte)'S';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SolOnOffTime ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                m_currCMD = (byte)'d';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(pattern.speedValue.dwellTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dwellTimeSet ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                if (currMarkInfo.checkdata.bReady == false)
                {
                    CheckAreaData chkdata = new CheckAreaData();
                    chkdata = await Range_Test(cmd, vin, pattern, 0, 0);
                    if (chkdata.execResult != 0)
                    {
                        return retval;
                    }
                    else
                        currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                }

                ifontsizeX = (int)(fontsizeX + 0.5);
                ifontsizeY = (int)(fontsizeY + 0.5);
                ishiftVal = (int)(shiftVal + 0.5);

                CP = pattern.positionValue.center3DPos;
                CP.Z += pattern.headValue.distance0Position;
                cleanPosition = pattern.laserValue.cleanPosition;
                if (cleanPosition < CP.Z) cleanPosition = CP.Z;
                //cleanPosition += CP.Z;         // Relative Cleanning postion

                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;
                SP0.Z = 0;
                SP = CP - SP0;
                SP.Z = 0.0d;

                VectorNormal = currMarkInfo.checkdata.NormalDir;
                VectorRot.X = -VectorNormal.Y;
                VectorRot.Y = VectorNormal.X;
                VectorRot.Z = 0.0;

                double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);

                if (sqXY != 0)
                {
                    VectorRot.X /= sqXY;
                    VectorRot.Y /= sqXY;
                }

                // Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
                bool skipRot = false;
                double cosValue = 0;
                if (VectorNormal.Length != 0)
                    cosValue = VectorNormal.Z / VectorNormal.Length;

                double sinValue = Math.Sqrt(1.0d - cosValue * cosValue);
                if (cosValue > 0.9999986111)
                {      // 0.1 mm difference between 60mm
                    skipRot = true;
                    R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
                    R11 = R22 = R33 = 1.0;
                }
                else
                {
                    R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
                    R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
                    R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
                    R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
                    R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
                    R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
                    R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
                    R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
                    R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
                }
                /////
                Step_W = pattern.fontValue.width / (fontsizeX - 1.0);
                Step_H = pattern.fontValue.height / (fontsizeY - 1.0);

                FontData4Send[,,] RasterData = new FontData4Send[vinLength + 1, ifontsizeY, ifontsizeX];     // BLU
                FontData4Send[,,] AllClrData = new FontData4Send[vinLength + 1, ifontsizeY, ifontsizeX];

                currMarkInfo.senddata.sendDataFire.Clear();
                currMarkInfo.senddata.sendDataClean.Clear();

                //List<Vector3D> recvPoint = new List<Vector3D>();
                ImageProcessManager.GetStartPointLinear(vinLength, CP, SP, pattern.fontValue.pitch, pattern.fontValue.rotateAngle, ref Rev_Point);

                Vector3D[] LeftRightSP = new Vector3D[2];
                LeftRightSP[0] = ImageProcessManager.Rotate_Point2(SP.X - pattern.headValue.rasterSP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
                LeftRightSP[1] = ImageProcessManager.Rotate_Point2(SP.X + totWidth + pattern.headValue.rasterEP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
                List<FontDataClass> lineData = new List<FontDataClass>();
                List<FontDataClass> lineDataClean = new List<FontDataClass>();
                ImageProcessManager.GetFontDataOneEx('^', pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);


                for (i = 0; i < Rev_Point.Count; i++)
                {
                    if (vin.Substring(i, 1) != " ")      //Space Skip
                    {
                        lineData = fontData[i];
                        FontDataClass fd = new FontDataClass();

                        //xy_Temp = (Mode_File.FONT_[Strings.Asc(DATA_.Substring(i, 1))]).Split(';');
                        //string[] xy_Data = new string[0];
                        List<FontDataClass> dotlist = new List<FontDataClass>();
                        //var xy_List = new List<string>();

                        var dmStr = new string[0];
                        if (DotMiss != null)
                        {
                            dmStr = DotMiss[i].Substring(2).Split(',');
                            if (dmStr.Length > 0)
                            {
                                if (Int32.Parse(dmStr[0]) == -1) continue;
                                else
                                {
                                    for (int n = 0; n < dmStr.Length - 1; n++)
                                    {
                                        fd = (FontDataClass)lineData[Int32.Parse(dmStr[n])].Clone();
                                        dotlist.Add(fd);
                                        //xy_List.Add(lineData[Int32.Parse(dmStr[n])]);
                                    }
                                    //xy_Data = xy_List.ToArray();
                                    //xy_List.Clear();
                                }
                            }
                        }

                        for (j = 0; j < dotlist.Count; j++)
                        {
                            fd = (FontDataClass)dotlist[j].Clone();
                            // ABS mm
                            M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
                            // Font offset compensation
                            M1.Y = Rev_Point[i].Y + fd.vector3d.Y * Step_H;
                            M1.Z = SP.Z;

                            //M1 = Mode_File.Rotate_Point(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, double.Parse(Angle));
                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                            M1.Z = SP.Z;

                            // TM SHIN
                            M1.X -= CP.X; M1.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                            M.X += CP.X; M.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
                            double Cz = 0.0;
                            Cz = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

                            Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3}", idx, M.X, M.Y, M.Z, Cz));

                            // Change to BLU(Unit 0.01mm)
                            //M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length;
                            //Cz *= (double)Mode_File.Step_Length;

                            M = M * pattern.headValue.stepLength;
                            Cz = Cz * pattern.headValue.stepLength;

                            FontData4Send font4Send = new FontData4Send();

                            font4Send.cN = (byte)i; font4Send.fN = (byte)int.Parse(dmStr[j]);
                            font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
                            font4Send.mC = (UInt16)(Cz + 0.5);
                            font4Send.mI = (UInt16)(fd.vector3d.X + 0.5);

                            //M_FONT FontData = new M_FONT();

                            //FontData.cN = (byte)i;
                            //FontData.fN = (byte)int.Parse(dmStr[j]);
                            //FontData.mX = (UInt16)(M.X + 0.5); FontData.mY = (UInt16)(M.Y + 0.5); FontData.mZ = (UInt16)(M.Z + 0.5); FontData.mF = byte.Parse(xy_[2]);
                            //FontData.mC = (UInt16)(Cz + 0.5);
                            //FontData.mI = double.Parse(xy_[0]);

                            if (pattern.laserValue.density == 1)
                            {
                                //FontData.mF = 0;
                                font4Send.mF = 0;
                                RasterData[i, (int)(fd.vector3d.Y - ishiftVal), (int)(fd.vector3d.X + 0.5)] = font4Send;
                            }
                            else
                            {
                                var m_font = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
                                currMarkInfo.senddata.sendDataFire.Add(m_font);
                                //Mode_File.SendData.Add(m_font);
                                var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
                                currMarkInfo.senddata.sendDataClean.Add(m_clean);
                                //Mode_File.SendClean.Add(m_clean);
                            }

                            idx++;
                        }
                    }
                }

                if (pattern.laserValue.density == 1)       // Dot Firing
                {
                    ushort[] NoPoints = new ushort[ifontsizeY];

                    for (int y = 0; y < ifontsizeY; y++)
                    {
                        for (i = 0; i < vinLength; i++)
                        {
                            for (int x = 0; x < ifontsizeX; x++)
                            {
                                if (RasterData[i, y, x] != null) NoPoints[y]++;         // Data Number of fire data
                            }
                        }
                    }

                    //
                    Debug.WriteLine("");

                    //
                    // Make Firing Data for dot missing
                    if (pattern.fontValue.rotateAngle == 0.0)
                    {           // 0 degree
                        StringBuilder sb = new StringBuilder();
                        for (i = 0; i < vinLength; i++)
                        {
                            // Fire Data string
                            for (int y = 0; y < ifontsizeY; y++)
                                for (int x = 0; x < ifontsizeX; x++)
                                    if (RasterData[i, y, x] != null)
                                    {
                                        sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mZ, RasterData[i, y, x].mF);
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        sb.Clear();
                                        //Mode_File.SendData.Add(sb.ToString()); sb.Clear();
                                    }
                        }

                        //
                        //  Make Cleaning Data
                        sb = new StringBuilder();
                        for (i = vinLength - 1; i >= 0; i--)
                        {
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                    if (RasterData[i, y, x] != null)
                                    {
                                        sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mC, RasterData[i, y, x].mF);
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        sb.Clear();
                                        //Mode_File.SendClean.Add(sb.ToString()); sb.Clear();
                                    }
                        }
                    }
                    else
                    {       // 180 degree
                        StringBuilder sb = new StringBuilder();
                        for (i = vinLength - 1; i >= 0; i--)
                        {
                            // Fire Data string
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                                for (int x = ifontsizeX - 1; x >= 0; x--)
                                    if (RasterData[i, y, x] != null)
                                    {
                                        sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mZ, RasterData[i, y, x].mF);
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        sb.Clear();
                                        //Mode_File.SendData.Add(sb.ToString()); sb.Clear();
                                    }
                        }

                        //
                        //  Make Cleaning Data
                        sb = new StringBuilder();
                        for (i = 0; i < vinLength; i++)
                        {
                            for (int y = 0; y < ifontsizeY; y++)
                                for (int x = 0; x < ifontsizeX; x++)
                                    if (RasterData[i, y, x] != null)
                                    {
                                        sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mC, RasterData[i, y, x].mF);
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPoints[y].ToString("000") + ":" + sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        sb.Clear();
                                        //Mode_File.SendClean.Add(sb.ToString()); sb.Clear();
                                    }
                        }
                    }
                }

                //M_Count = idx;
                //Mark_Counter++;

                //Mode_File.Download_Data = true;



                //M_Count = idx;
                ////Mark_Counter++;

                //Mode_File.Download_Data = true;

                return retval;

                Vector3D getRodrigueRotation(Vector3D XY0)
                {
                    Vector3D Tmp = new Vector3D();
                    Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
                    Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
                    Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

                    return Tmp;
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        //public async Task<ITNTResponseArgs> Start_DOTMISSING(string cmd, string vin, PatternValueEx pattern, List<string> DotMiss)    // Making Dot Missing data by TM SHIN
        //{
        //    string className = "SetControllerWindow2";
        //    string funcName = "Start_DOTMISSING";

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    int vinLength = 0;

        //    Vector3D SP0 = new Vector3D();
        //    Vector3D SP = new Vector3D();
        //    Vector3D CP = new Vector3D();

        //    Vector3D VectorNormal = new Vector3D();
        //    Vector3D VectorRot = new Vector3D();
        //    List<Vector3D> Rev_Point = new List<Vector3D>();

        //    double Step_W;
        //    double Step_H;
        //    string value = "";
        //    byte headType = 0;
        //    VinNoInfo vininfo = new VinNoInfo();
        //    //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
        //    List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
        //    double fontsizeX = 0, fontsizeY = 0, shiftVal = 0;
        //    double fontsizeX2 = 0, fontsizeY2 = 0, shiftVal2 = 0;
        //    double cleanPosition = 0;
        //    string errCode = "";
        //    double totWidth = 0;
        //    double R11, R12, R13, R21, R22, R23, R31, R32, R33;
        //    int i, j;
        //    int idx = 0;
        //    int ifontsizeX = 0, ifontsizeY = 0, ishiftVal = 0;

        //    Vector3D M1 = new Vector3D();                                   // for fire data mm
        //    Vector3D M2 = new Vector3D();                                   // for clean data mm
        //    Vector3D M = new Vector3D();
        //    Vector3D C = new Vector3D();

        //    try
        //    {
        //        vinLength = vin.Length;
        //        if (vinLength <= 0)
        //        {
        //            retval.execResult = -1;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN LENGTH IS INVALID (" + vinLength.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

        //        Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
        //        byte.TryParse(value, out headType);

        //        vininfo.vinNo = vin;
        //        vininfo.fontName = pattern.fontValue.fontName;
        //        vininfo.width = pattern.fontValue.width;
        //        vininfo.height = pattern.fontValue.height;
        //        vininfo.pitch = pattern.fontValue.pitch;
        //        vininfo.thickness = pattern.fontValue.thickness;
        //        ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 0, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref errCode);

        //        m_currCMD = (byte)'S';
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SolOnOffTime ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        m_currCMD = (byte)'d';
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(pattern.speedValue.dwellTime);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dwellTimeSet ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        if (currMarkInfo.checkdata.bReady == false)
        //        {
        //            CheckAreaData chkdata = new CheckAreaData();
        //            chkdata = await Range_Test2(cmd, vin, pattern);
        //            if (chkdata.execResult != 0)
        //            {
        //                return retval;
        //            }
        //            else
        //                currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
        //        }

        //        ifontsizeX = (int)(fontsizeX + 0.5);
        //        ifontsizeY = (int)(fontsizeY + 0.5);
        //        ishiftVal = (int)(shiftVal + 0.5);

        //        CP = pattern.positionValue.center3DPos;
        //        cleanPosition = pattern.laserValue.cleanPosition;
        //        cleanPosition += CP.Z;         // Relative Cleanning postion

        //        SP0.X = totWidth / 2;
        //        SP0.Y = pattern.fontValue.height / 2;
        //        SP0.Z = 0;
        //        SP = CP - SP0;
        //        SP.Z = 0.0d;

        //        VectorNormal = currMarkInfo.checkdata.NormalDir;
        //        VectorRot.X = -VectorNormal.Y;
        //        VectorRot.Y = VectorNormal.X;
        //        VectorRot.Z = 0.0;

        //        double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);

        //        if (sqXY != 0)
        //        {
        //            VectorRot.X /= sqXY;
        //            VectorRot.Y /= sqXY;
        //        }

        //        // Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
        //        bool skipRot = false;
        //        double cosValue = 0;
        //        if (VectorNormal.Length != 0)
        //            cosValue = VectorNormal.Z / VectorNormal.Length;

        //        double sinValue = Math.Sqrt(1.0d - cosValue * cosValue);
        //        if (cosValue > 0.9999986111)
        //        {      // 0.1 mm difference between 60mm
        //            skipRot = true;
        //            R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
        //            R11 = R22 = R33 = 1.0;
        //        }
        //        else
        //        {
        //            R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
        //            R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
        //            R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
        //            R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
        //            R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
        //            R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
        //            R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
        //            R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
        //            R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
        //        }
        //        /////
        //        Step_W = pattern.fontValue.width / (fontsizeX - 1.0);
        //        Step_H = pattern.fontValue.height / (fontsizeY - 1.0);

        //        FontData4Send[,,] RasterData = new FontData4Send[vinLength + 1, ifontsizeY, ifontsizeX];     // BLU
        //        FontData4Send[,,] AllClrData = new FontData4Send[vinLength + 1, ifontsizeY, ifontsizeX];

        //        currMarkInfo.senddata.sendDataFire.Clear();
        //        currMarkInfo.senddata.sendDataClean.Clear();

        //        //List<Vector3D> recvPoint = new List<Vector3D>();
        //        ImageProcessManager.GetStartPointLinear(vinLength, CP, SP, pattern.fontValue.pitch, pattern.fontValue.rotateAngle, ref Rev_Point);

        //        Vector3D[] LeftRightSP = new Vector3D[2];
        //        LeftRightSP[0] = ImageProcessManager.Rotate_Point2(SP.X - pattern.positionValue.rasterSP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
        //        LeftRightSP[1] = ImageProcessManager.Rotate_Point2(SP.X + totWidth + pattern.positionValue.rasterEP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
        //        List<FontDataClass> lineData = new List<FontDataClass>();
        //        List<FontDataClass> lineDataClean = new List<FontDataClass>();
        //        ImageProcessManager.GetFontDataOneEx('^', pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);








        //        for (i = 0; i < Rev_Point.Count; i++)
        //        {
        //            if (vin.Substring(i, 1) != " ")      //Space Skip
        //            {
        //                lineData = fontData[i];
        //                FontDataClass fd = new FontDataClass();

        //                //xy_Temp = (Mode_File.FONT_[Strings.Asc(DATA_.Substring(i, 1))]).Split(';');
        //                //string[] xy_Data = new string[0];
        //                //var xy_List = new List<string>();

        //                //var dmStr = new string[0];
        //                //if (DotMiss != null)
        //                //{
        //                //    dmStr = DotMiss[i].Substring(2).Split(',');
        //                //    if (dmStr.Length > 0)
        //                //    {
        //                //        if (Int32.Parse(dmStr[0]) == -1) continue;
        //                //        else
        //                //        {
        //                //            for (int n = 0; n < dmStr.Length - 1; n++)
        //                //            {
        //                //                xy_List.Add(xy_Temp[Int32.Parse(dmStr[n])]);
        //                //            }
        //                //            xy_Data = xy_List.ToArray();
        //                //            xy_List.Clear();
        //                //        }
        //                //    }
        //                //}

        //                for (j = 0; j < lineData.Count; j++)
        //                {
        //                    fd = (FontDataClass)lineData[j].Clone();
        //                    // ABS mm
        //                    M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
        //                    // Font offset compensation
        //                    M1.Y = Rev_Point[i].Y + fd.vector3d.Y * Step_H;
        //                    M1.Z = SP.Z;

        //                    //M1 = Mode_File.Rotate_Point(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, double.Parse(Angle));
        //                    M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
        //                    M1.Z = SP.Z;

        //                    // TM SHIN
        //                    M1.X -= CP.X; M1.Y -= CP.Y;

        //                    M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

        //                    M.X += CP.X; M.Y += CP.Y;
        //                    double Mt = M.Z;
        //                    M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
        //                    double Cz = 0.0;
        //                    Cz = Mt + currMarkInfo.currMarkData.pattern.laserValue.cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis

        //                    Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3}", idx, M.X, M.Y, M.Z, Cz));

        //                    // Change to BLU(Unit 0.01mm)
        //                    //M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length;
        //                    //Cz *= (double)Mode_File.Step_Length;

        //                    M = M * pattern.headValue.stepLength;
        //                    C = C * pattern.headValue.stepLength;

        //                    FontData4Send font4Send = new FontData4Send();

        //                    font4Send.cN = (byte)i; font4Send.fN = (byte)j;
        //                    font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
        //                    font4Send.mC = (UInt16)(C.Z + 0.5);
        //                    font4Send.mI = (UInt16)(C.X + 0.5);

        //                    M_FONT FontData = new M_FONT();

        //                    FontData.cN = (byte)i;
        //                    FontData.fN = (byte)int.Parse(dmStr[j]);
        //                    FontData.mX = (UInt16)(M.X + 0.5); FontData.mY = (UInt16)(M.Y + 0.5); FontData.mZ = (UInt16)(M.Z + 0.5); FontData.mF = byte.Parse(xy_[2]);
        //                    FontData.mC = (UInt16)(Cz + 0.5);
        //                    FontData.mI = double.Parse(xy_[0]);

        //                    if (Density == 1)
        //                    {
        //                        FontData.mF = 0;
        //                        RasterData[i, int.Parse(xy_[1]) - SF, (int)(double.Parse(xy_[0]) + 0.5)] = FontData;
        //                    }
        //                    else
        //                    {
        //                        var m_font = FontData.cN.ToString("X2") + FontData.fN.ToString("X2") + FontData.mX.ToString("X4") + FontData.mY.ToString("X4") + FontData.mZ.ToString("X4") + FontData.mF.ToString("X4");
        //                        Mode_File.SendData.Add(m_font);
        //                        var m_clean = FontData.cN.ToString("X2") + FontData.fN.ToString("X2") + FontData.mX.ToString("X4") + FontData.mY.ToString("X4") + FontData.mC.ToString("X4") + FontData.mF.ToString("X4");
        //                        Mode_File.SendClean.Add(m_clean);
        //                    }

        //                    idx++;
        //                }
        //            }
        //        }

        //        if (currMarkInfo.currMarkData.pattern.laserValue.density == 1)       // Dot Firing
        //        {
        //            ushort[] NoPoints = new ushort[YF];

        //            for (int y = 0; y < YF; y++)
        //            {
        //                for (i = 0; i < vinLength; i++)
        //                {
        //                    for (int x = 0; x < XF; x++)
        //                    {
        //                        if (RasterData[i, y, x] != null) NoPoints[y]++;         // Data Number of fire data
        //                    }
        //                }
        //            }

        //            //
        //            Debug.WriteLine("");

        //            //
        //            // Make Firing Data for dot missing
        //            if (Ag == 0.0)
        //            {           // 0 degree
        //                StringBuilder sb = new StringBuilder();
        //                for (i = 0; i < vinLength; i++)
        //                {
        //                    // Fire Data string
        //                    for (int y = 0; y < YF; y++)
        //                        for (int x = 0; x < XF; x++)
        //                            if (RasterData[i, y, x] != null)
        //                            {
        //                                sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mZ, RasterData[i, y, x].mF);
        //                                Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                Mode_File.SendData.Add(sb.ToString()); sb.Clear();
        //                            }
        //                }

        //                //
        //                //  Make Cleaning Data
        //                sb = new StringBuilder();
        //                for (i = vinLength - 1; i >= 0; i--)
        //                {
        //                    for (int y = YF - 1; y >= 0; y--)
        //                        for (int x = XF - 1; x >= 0; x--)
        //                            if (RasterData[i, y, x] != null)
        //                            {
        //                                sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mC, RasterData[i, y, x].mF);
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                Mode_File.SendClean.Add(sb.ToString()); sb.Clear();
        //                            }
        //                }
        //            }
        //            else
        //            {       // 180 degree
        //                StringBuilder sb = new StringBuilder();
        //                for (i = vinLength - 1; i >= 0; i--)
        //                {
        //                    // Fire Data string
        //                    for (int y = YF - 1; y >= 0; y--)
        //                        for (int x = XF - 1; x >= 0; x--)
        //                            if (RasterData[i, y, x] != null)
        //                            {
        //                                sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mZ, RasterData[i, y, x].mF);
        //                                Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                Mode_File.SendData.Add(sb.ToString()); sb.Clear();
        //                            }
        //                }

        //                //
        //                //  Make Cleaning Data
        //                sb = new StringBuilder();
        //                for (i = 0; i < vinLength; i++)
        //                {
        //                    for (int y = 0; y < YF; y++)
        //                        for (int x = 0; x < XF; x++)
        //                            if (RasterData[i, y, x] != null)
        //                            {
        //                                sb.AppendFormat("{0:X2}{1:X2}{2:X4}{3:X4}{4:X4}{5:X4}", RasterData[i, y, x].cN, RasterData[i, y, x].fN, RasterData[i, y, x].mX, RasterData[i, y, x].mY, RasterData[i, y, x].mC, RasterData[i, y, x].mF);
        //                                Debug.WriteLine(y.ToString("00") + "C" + NoPoints[y].ToString("000") + ":" + sb.ToString());
        //                                Mode_File.SendClean.Add(sb.ToString()); sb.Clear();
        //                            }
        //                }
        //            }
        //        }

        //        M_Count = idx;
        //        //Mark_Counter++;

        //        Mode_File.Download_Data = true;






        //        //M_Count = idx;
        //        ////Mark_Counter++;

        //        //Mode_File.Download_Data = true;

        //        return retval;

        //        Vector3D getRodrigueRotation(Vector3D XY0)
        //        {
        //            Vector3D Tmp = new Vector3D();
        //            Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
        //            Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
        //            Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

        //            return Tmp;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //        //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }
        //}

        private async void btnFixDotMissing_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnFixDotMissing_Click";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            List<string> missList = new List<string>();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            PatternValueEx pattern = new PatternValueEx();
            string value = "";
            byte bHeadType = 0;

            CheckAreaData chkdata = new CheckAreaData();
            string patName = "";
            string cmd = "FIX DOT MISSING";
            string log = "";
            short posX = 0;
            short posY = 0;
            short posZ = 0;
            LASERSTATUS Status;

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                openFileDialog.InitialDirectory = "C\\ITNT\\DOTMISSING\\";// AppDomain.CurrentDomain.BaseDirectory + Constants.PARAMS_PATH;
                openFileDialog.Filter = "dot missing files (*.dot)|*.dot";

                if (openFileDialog.ShowDialog() == false)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "USER CANCEL");
                    return;
                }

                if (File.Exists(openFileDialog.FileName) == false)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "FILE NOT FOUND");
                    return;
                }


                string readDotMiss = File.ReadAllText(openFileDialog.FileName);
                string[] tmpmiss = readDotMiss.Replace('\r', ' ').Split('\n');
                for(int i = 0; i < tmpmiss.Length; i++)
                {
                    if (tmpmiss[i].Length > 0)
                        missList.Add(tmpmiss[i]);
                }
                //missList = readDotMiss.Replace('\r', ' ').Split('\n').ToList<string>();
                var strbVin = new StringBuilder();
                for (int i = 0; i < missList.Count; i++)
                    strbVin.Append(missList[i][0]);
                var strVin = strbVin.ToString().ToUpper();

                //SetString4Textbox(strVin, txtVIN, null, null);
                ShowTextBoxData(txtVIN, strVin);


                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                ShowRectangle(AimingLamp, Brushes.Red);

                if(cbxPatternList.SelectedIndex < 0)
                {
                    return;
                }

                patName = cbxPatternList.SelectedItem.ToString();
                //ImageProcessManager.GetPatternValue(patName, byH)
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);
                ImageProcessManager.GetPatternValue(patName, bHeadType, ref pattern);


                chkdata = await Range_Test(cmd, strVin, pattern, 0,0);
                if(chkdata.execResult != 0)
                {
                    return;
                }

                if (chkdata.ErrorDistanceSensor == true)
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                    ShowRectangle(AimingLamp, Brushes.Black);
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                ShowRectangle(AimingLamp, Brushes.Black);


                statusTimer.Stop();



                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                if (retval.execResult != 0)
                {
                    log = "ReadDeviceStatus (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }
                string[] st = retval.recvString.Split(':');
                Status = (LASERSTATUS)UInt32.Parse(st[1]);
                if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    ShowRectangle(EmissionLamp, Brushes.Black);
                    //EmissionLamp.Fill = Brushes.Black;
                }

                //Util.GetPrivateProfileValue("PROFILE", "PROFILEFIRE", "0", ref value, "Parameter.ini");                 // load waveform profile number
                //ProfileTxt.Text = value;
                ShowTextBoxData(txtCurrProfile, pattern.laserValue.waveformNum.ToString());
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(pattern.laserValue.waveformNum.ToString());
                string[] prsel = retval.recvString.Split('[', ']');
                if (prsel[0] == "PRSEL: ")
                {
                    string[] sel = prsel[1].Split(':');
                    if (value != sel[0])
                    {
                        Debug.WriteLine("Profile setting Error! / ");      // Error
                        statusTimer.Start();
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine("Profile setting response Error! / "); // Error
                    statusTimer.Start();
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ConfigWaveformMode(0);
                if (retval.execResult != 0)
                {
                    log = "ConfigWaveformMode (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }
                string[] pcfg = retval.recvString.Split('[', ']');


                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.FontFlush();
                if (retval.execResult != 0)
                {
                    log = "FontFlush (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }
                ClearMarkVINDisplay();


                retval.execResult = ReadFontData(cmd, patName);
                if (retval.execResult != 0)
                {
                    log = "READ FONT ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }
                await ShowCurrentMarkingInformation(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2);
                GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);
                bReadFontValue = true;


                retval = await Start_DOTMISSING(cmd, strVin, pattern, missList);
                if (retval.execResult != 0)
                {
                    log = "Start_DOTMISSING. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }


                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }

                int iDotDelay = 0;
                Util.GetPrivateProfileValue("LASERSOURCE", "DOTDELAY", "100", ref value, Constants.MARKING_INI_FILE);
                int.TryParse(value, out iDotDelay);


                currMarkInfo.senddata.CleanFireFlag = false;    // Fire sending
                                                                //currMarkInfo.senddata.SendDataCount = (short)currMarkInfo.senddata.sendDataFire.Count;
                string StPoint = currMarkInfo.senddata.sendDataFire.ElementAt(0);

                m_currCMD = (byte)'K';
                value = StPoint.Substring(4, 4);
                short.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out posX);

                value = StPoint.Substring(8, 4);
                short.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out posY);

                value = StPoint.Substring(12, 4);
                short.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out posZ);
                posZ += 500;

                //StPoint.Substring(4, 4 + 4);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(posX, posY, posZ);
                if (retval.execResult != 0)
                {
                    log = "GO PARK. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.MarkMoving);
                if (retval.execResult != 0)
                {
                    log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                //mark.Density("N", Density.ToString("X4"));
                //ShowLog("MARKING - SET DENSITY");
                m_currCMD = (byte)'N';
                //Density232 = (short)currMarkInfo.currMarkData.pattern.laserValue.density;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SetDensity((short)0);
                if (retval.execResult != 0)
                {
                    log = "SET DENSITY. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }

                // Run Marking
                Stopwatch sw = Stopwatch.StartNew();

                if ((bool)EmissionAuto.IsChecked)
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                    ShowRectangle(EmissionLamp, Brushes.Red);
                    //EmissionLamp.Fill = Brushes.Red;
                }

                //Marking Start
                //ShowLog("MARKING - START MARKING");
                Util.GetPrivateProfileValue("OPTION", "MARKINGLOGLEVEL", "0", ref value, Constants.PARAMS_INI_FILE);
                byte logLevel = 0;
                byte.TryParse(value, out logLevel);


                foreach (var str in currMarkInfo.senddata.sendDataFire)
                {
                    m_currCMD = (byte)'@';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(str, false, logLevel);
                    if (retval.execResult != 0)
                    {
                        log = "MARKING ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        statusTimer.Start();
                        return;
                    }

                    await Task.Delay(iDotDelay);

                    m_currCMD = (byte)'O';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(0, true);
                    if (retval.execResult != 0)
                    {
                        log = "TestSolFet ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        statusTimer.Start();
                        return;
                    }
                }


                if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    ShowRectangle(EmissionLamp, Brushes.Black);
                    //EmissionLamp.Fill = Brushes.Black;
                }





                bReadFontValue = false;
                currMarkInfo.checkdata.bReady = false;
                currMarkInfo.senddata.bReady = false;
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                if (retval.execResult != 0)
                {
                    log = "ReadDeviceStatus. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }
                else
                {
                    st = retval.recvString.Split(':');
                    Status = (LASERSTATUS)UInt32.Parse(st[1]);

                    //if ((bool)CleaningBox.IsChecked && !currMarkInfo.checkdata.ErrorDistanceSensor)
                    if ((bool)CleaningBox.IsChecked && !currMarkInfo.checkdata.ErrorDistanceSensor && (currMarkInfo.currMarkData.pattern.laserValue.combineFireClean == 0))
                    {
                        //ShowLog("MARKING - START CLEANING");

                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        if (retval.execResult != 0)
                        {
                            log = "StopEmission. (RESULT = " + retval.execResult.ToString() + ")";
                            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            statusTimer.Start();
                            return;
                        }
                        ShowRectangle(EmissionLamp, Brushes.Black);

                        //Util.GetPrivateProfileValue("VINDATA", "PROFILECLEAN", "0", ref value, "Parameter.ini");                 // load waveform profile number
                        value = currMarkInfo.currMarkData.pattern.laserValue.waveformClean.ToString();
                        ShowTextBoxData(txtCurrProfile, value);
                        //txtCurrProfile.Text = value;
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(value);
                        prsel = retval.recvString.Split('[', ']');
                        if (prsel[0] != "PRSEL: ")
                        {
                            log = "Profile setting Error2!. (PRSEL[0] = " + prsel[0] + ")";
                            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            statusTimer.Start();
                            return;
                        }
                        string[] sel = prsel[1].Split(':');
                        if (value != sel[0])
                        {
                            log = "Profile setting Error!. (SEL[0] = " + sel[0] + ")";
                            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            statusTimer.Start();
                            return;
                        }

                        if ((bool)EmissionAuto.IsChecked)
                        {
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                            ShowRectangle(EmissionLamp, Brushes.Red);
                            //EmissionLamp.Fill = Brushes.Red;
                        }

                        //currMarkInfo.senddata.SendDataIndex = 0;
                        currMarkInfo.senddata.CleanFireFlag = true;    // Clean sending
                                                                       //currMarkInfo.senddata.SendDataCount = (short)currMarkInfo.senddata.sendDataClean.Count;




                        foreach (var str in currMarkInfo.senddata.sendDataClean)
                        {
                            m_currCMD = (byte)'@';
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(str, true, logLevel);
                            if (retval.execResult != 0)
                            {
                                log = "MARKING ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                                statusTimer.Start();
                                return;
                            }

                            await Task.Delay(iDotDelay);

                            m_currCMD = (byte)'O';
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.TestSolFet(0, true);
                            if (retval.execResult != 0)
                            {
                                log = "TestSolFet ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                                statusTimer.Start();
                                return;
                            }
                        }


                        log = "CLEAN TIME : " + sw.ElapsedMilliseconds.ToString();
                        ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                    }

                    if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        ShowRectangle(EmissionLamp, Brushes.Black);
                        //EmissionLamp.Fill = Brushes.Black;
                    }
                }


                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SetDensity((short)currMarkInfo.currMarkData.pattern.laserValue.density);
                if (retval.execResult != 0)
                {
                    log = "SetDensity (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }



                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }

                short stepLeng = currMarkInfo.currMarkData.pattern.headValue.stepLength;
                if (stepLeng <= 0)
                    stepLeng = 100;

                posX = (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.X * stepLeng); if (posX <= 0) posX = (short)(70 * stepLeng);
                posY = (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * stepLeng); if (posY <= 0) posY = (short)(20 * stepLeng);
                posZ = (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * stepLeng); if (posZ <= 0) posZ = (short)(110 * stepLeng);
                m_currCMD = (byte)'K';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(posX, posY, posZ);
                if (retval.execResult != 0)
                {
                    log = "GO PARK. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    statusTimer.Start();
                    return;
                }

                //Debug.WriteLine("Running Time : " + sw.Elapsed);
                sw.Stop();
                log = "TOTAL MARKING TIME : " + sw.ElapsedMilliseconds.ToString();
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Running Time : " + sw.Elapsed.ToString(), Thread.CurrentThread.ManagedThreadId);
                statusTimer.Start();



                m_currCMD = (byte)'N';
                //Density232 = (short)currMarkInfo.currMarkData.pattern.laserValue.density;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.SetDensity((short)currMarkInfo.currMarkData.pattern.laserValue.density);
                if (retval.execResult != 0)
                {
                    log = "SET DENSITY. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ERR.ToString());
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void ShowMarkingOneLine(int xcharIndex, int fontIndex)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowMarkingOneLine";

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
            double left, right, top, bottom;

            try
            {
                showcanvas = new Canvas();
                name = string.Format("cvsshowChar{0:D2}", xcharIndex);
                showcanvas = (Canvas)FindName(name);
                if (showcanvas == null)
                    return;

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
                            Dotline = new Ellipse
                            {
                                Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen,
                                //Stroke = Brushes.Red,
                                StrokeThickness = CharThick,
                                Height = (double)Dotsize,
                                Width = (double)Dotsize,
                                Fill = Brushes.Red,
                                Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightRatio - (double)Dotsize / 2.0, 0, 0)
                            };
                            Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                            ////showcanvas.Children.Add(img);
                            showcanvas.Children.Add(Dotline);
                            ////showcanvas.Children.Remove(img);
                        }
                        break;

                    case 1:
                        for (int v = 0; v < currMarkInfo.currMarkData.mesData.markvin.Length; v++)
                        {
                            Canvas showcanvas1 = new Canvas();
                            string names = string.Format("cvsshowChar{0:D2}", v);
                            showcanvas1 = (Canvas)FindName(names);
                            if (showcanvas1 == null)
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
                                    top = 0;
                                    bottom = 0;
                                    //Debug.WriteLine(String.Format("DISP-({3}/{0}:{1},{2}:{4}.{5})", fontIndex, fontdot.vector3d.X, fontdot.vector3d.Y, xcharIndex, left, right));
                                    Dotline = new Ellipse
                                    {
                                        Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen,
                                        StrokeThickness = CharThick,
                                        Height = (double)Dotsize,
                                        Width = (double)Dotsize,
                                        Fill = Brushes.Red,
                                        Margin = new Thickness(left, right, 0, 0)
                                    };
                                    Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                                    ////showcanvas.Children.Add(img);
                                    showcanvas1.Children.Add(Dotline);
                                    ////showcanvas.Children.Remove(img);
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:00}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        public void ShowMarkingOneLine(int xcharIndex, int fontIndex, Int16 densityRun, bool FireCleanDotColor = true)
        {
            string className = "SetControllerWindow2";
            string funcName = "ShowOneVinNoCharacter";
            Canvas showcanvas = new Canvas();
            string name = "";

            try
            {
                name = string.Format("cvsshowChar{0:D2}", xcharIndex);
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

                switch (densityRun)//currMarkInfo.currMarkData.pattern.laserValue.density)
                {
                    case 0:     // Density 0 : Dot Marking : Dot
                        fdata = currMarkInfo.currMarkData.fontData[xcharIndex].ToList();
                        font = (FontDataClass)fdata[fontIndex].Clone();

                        //if (font.Flag == 1 || font.Flag == 2 || font.Flag == 3 || font.Flag == 4 || font.Flag == 5)
                        if (font.Flag != 0)
                        {
                            Dotline = new Ellipse
                            {
                                Stroke = (FireCleanDotColor == false) ? Brushes.Red : Brushes.LightGreen,
                                StrokeThickness = CharThick,
                                Height = (double)Dotsize,
                                Width = (double)Dotsize,
                                Fill = Brushes.Red,
                                Margin = new Thickness((OriginX + (font.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (font.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0)
                            };
                            Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                            ////showcanvas.Children.Add(img);
                            showcanvas.Children.Add(Dotline);
                            ////showcanvas.Children.Remove(img);
                        }
                        break;

                    case 1:     // Density 1 : Dot Marking : Line
                        for (int v = 0; v < currMarkInfo.currMarkData.mesData.markvin.Length; v++)
                        {
                            Canvas showcanvas1 = new Canvas();
                            string names = string.Format("cvsshowChar{0:D2}", v);
                            showcanvas1 = (Canvas)FindName(names);
                            if (showcanvas1 == null)
                                return;

                            for (int x = 0; x < currMarkInfo.currMarkData.fontDot.GetLength(1); x++)
                            {
                                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:00}:{1}()  {2}", className, funcName, "V = " + v.ToString() + ", X = " + x.ToString(), Thread.CurrentThread.ManagedThreadId);

                                FontDataClass fontdot = (FontDataClass)currMarkInfo.currMarkData.fontDot[v, x, fontIndex].Clone();
                                if (fontdot.Flag != 0)
                                {
                                    ////Canvas.SetLeft(img, ((OriginX + (font.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - img.Height / 2));
                                    ////Canvas.SetTop(img, (OriginY + (font.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - img.Width / 2);
                                    Dotline = new Ellipse
                                    {
                                        Stroke = (FireCleanDotColor == false) ? Brushes.Red : Brushes.LightGreen,
                                        StrokeThickness = CharThick,
                                        Height = (double)Dotsize,
                                        Width = (double)Dotsize,
                                        Fill = Brushes.Red,
                                        Margin = new Thickness((OriginX + (fontdot.vector3d.X * CharWidth) / currMarkInfo.currMarkData.fontSizeX) * widthRatio - (double)Dotsize / 2.0, (OriginY + (fontdot.vector3d.Y * CharHeight) / currMarkInfo.currMarkData.fontSizeY) * heightthRatio - (double)Dotsize / 2.0, 0, 0)
                                    };
                                    Canvas.SetZIndex(Dotline, (int)(CharThick + 0.5));
                                    ////showcanvas.Children.Add(img);
                                    showcanvas1.Children.Add(Dotline);
                                    ////showcanvas.Children.Remove(img);
                                }
                            }
                        }
                        break;

                    default:    // Divided Dot Marking  :  Density > 1
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:00}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }



        public async Task<distanceSensorData> GetMeasureLength(Vector3D vp3, int pos, byte count)
        {
            string className = "SetControllerWindow2";
            string funcName = "GetMeasureLength";

            distanceSensorData sensorData = new distanceSensorData();
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                m_currCMD = (byte)'M';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint((int)(vp3.X), (int)(vp3.Y), (int)(vp3.Z), pos);
                if (retval.execResult != 0)
                {
                    sensorData.execResult = retval.execResult;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GoPoint ERROR : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return sensorData;
                }

                sensorData = await ((MainWindow)System.Windows.Application.Current.MainWindow).ReadDisplacementSensor(count);
                if (sensorData.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadDisplacementSensor : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return sensorData;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                sensorData.execResult = ex.HResult;
            }
            return sensorData;
        }


        //public async Task<CheckAreaData> Range_Test2(string cmd, string vin, PatternValueEx pattern)   // Plating  by TM SHIN
        //{
        //    string className = "SetControllerWindow2";
        //    string funcName = "Range_Test2";

        //    CheckAreaData retval = new CheckAreaData();
        //    distanceSensorData snsdata = new distanceSensorData();
        //    ITNTResponseArgs respArg = new ITNTResponseArgs();

        //    int NoVin;
        //    double totWidth = 0;

        //    Vector3D SP0 = new Vector3D();
        //    Vector3D SP1 = new Vector3D();
        //    Vector3D CP = new Vector3D();

        //    Vector3D PointLU = new Vector3D();
        //    Vector3D PointLD = new Vector3D();
        //    Vector3D PointRU = new Vector3D();

        //    Vector3D[] vCheckPos = new Vector3D[7];
        //    double[] HeightVal = new double[7];
        //    Vector3D vector3 = new Vector3D();

        //    double HeightCT0 = 0;
        //    string value = "";
        //    string log = "";
        //    byte bHeadType = 0;
        //    string fName = "";

        //    List<Vector3D> planePoints = new List<Vector3D>();

        //    short gMinX = 0;
        //    short gMaxX = 0;
        //    short gMinY = 0;
        //    short gMaxY = 0;
        //    short? CenterX = null;
        //    short? CenterY = null;
        //    short? CenterZ = null;

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //        Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
        //        byte.TryParse(value, out bHeadType);

        //        NoVin = vin.Length;
        //        if (NoVin <= 0)
        //        {
        //            log = "VIN LENGTH <= 0 (" + NoVin.ToString() + ")";
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = -1;
        //            return retval;
        //        }

        //        if (cbxPatternList.SelectedIndex >= 0)
        //            fName = cbxPatternList.SelectedItem.ToString();
        //        else
        //            fName = "Pattern_DEFAULT";


        //        totWidth = pattern.fontValue.pitch * (NoVin - 1) + pattern.fontValue.width;
        //        planePoints.Clear();
        //        retval.ErrorDistanceSensor = false;

        //        if (ckbSkipCheckPlane.IsChecked == true)
        //        {
        //            retval.NormalDir.X = 0;
        //            retval.NormalDir.Y = 0;
        //            retval.NormalDir.Z = 1;

        //            retval.bReady = true;
        //            retval.execResult = 0;
        //            return retval;
        //        }

        //        // SET Motor Speed
        //        respArg = await SendMotorSpeed(cmd, (byte)motorSpeedType.MeasureMoving);
        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR : " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET MOTOR SPEED(MEASURE) ERROR - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }

        //        //respArg = 


        //        // Laser Beam ON
        //        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET BEAM CONTROLL ERROR - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }

        //        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - BEAM ON ERROR : " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM ON ERROR - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }
        //        ShowRectangle(AimingLamp, Brushes.Red);

        //        // Absolute mm of Center Point
        //        CP = pattern.positionValue.center3DPos;
        //        SP0.X = totWidth / 2;
        //        SP0.Y = pattern.fontValue.height / 2;

        //        SP1 = CP - SP0;
        //        SP1.Z = CP.Z;

        //        // ABS mm
        //        double MinX = SP1.X;
        //        double MaxX = SP1.X + totWidth;
        //        double MinY = SP1.Y;
        //        double MaxY = SP1.Y + pattern.fontValue.height;// Ht;

        //        // ABS BLU
        //        gMinX = (short)(MinX * pattern.headValue.stepLength + 0.5);
        //        gMaxX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
        //        gMinY = (short)(MinY * pattern.headValue.stepLength + 0.5);
        //        gMaxY = (short)(MaxY * pattern.headValue.stepLength + 0.5);

        //        // ABS mm
        //        double CX = (MaxX + MinX) / 2.0;
        //        double CY = (MaxY + MinY) / 2.0;

        //        // ABS BLU
        //        short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
        //        short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);
        //        CenterX = tCX;
        //        CenterY = tCY;
        //        CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);

        //        short Parking_Z = (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5);

        //        vector3.X = tCX;
        //        vector3.Y = tCY;
        //        vector3.Z = Parking_Z;

        //        snsdata = await GetMeasureLength(vector3, 0, 1);
        //        if (snsdata.execResult != 0)
        //        {
        //            retval.execResult = snsdata.execResult;
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR 0 - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        double ShiftCT = snsdata.sensorshift;
        //        double HeightCT = snsdata.sensoroffset;

        //        if (Math.Abs(HeightCT) > 20.0)      // Z Diff Max. 60mm
        //        {
        //            retval.ErrorDistanceSensor = true;
        //            Dispatcher.Invoke(new Action(delegate
        //            {
        //                lblDispCenXCenY.Foreground = Brushes.Red;
        //                lblDispCenXCenY.Content = HeightCT.ToString("0.000");       // Sensor shift value 
        //                lblDispMinXMaxY.Content = "";
        //                lblDispMinXMinY.Content = "";
        //                lblDispMaxXMaxY.Content = "";
        //                lblDispMaxXMinY.Content = "";
        //                lblDispCenXMaxY.Content = "";
        //                lblDispCenXMinY.Content = "";
        //            }));

        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "CENTER Z Range is OUT OF RANGE");
        //            retval.execResult = -2;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }


        //        if (pattern.headValue.sensorPosition == 0)  // RIGHT
        //        {
        //            gMinX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            gMaxX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //        }
        //        else                                       // LEFT
        //        {
        //            gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //        }

        //        // Sensor Shift compensation
        //        Dispatcher.Invoke(new Action(delegate
        //        {
        //            lblDispCenXCenY.Foreground = Brushes.Black;
        //            lblDispCenXCenY.Content = HeightCT.ToString("0.000");       // Sensor shift value 
        //            lblDispMinXMaxY.Content = "";
        //            lblDispMinXMinY.Content = "";
        //            lblDispMaxXMaxY.Content = "";
        //            lblDispMaxXMinY.Content = "";
        //            lblDispCenXMaxY.Content = "";
        //            lblDispCenXMinY.Content = "";
        //        }));

        //        tCX = (short)(((double)gMaxX + (double)gMinX) / 2.0 + 0.5);
        //        tCY = (short)(((double)gMaxY + (double)gMinY) / 2.0 + 0.5);

        //        double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
        //        double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

        //        Vector3D[] vAddPos = new Vector3D[7];
        //        double left = MinX - CP.X;
        //        double centerX = (MaxX + MinX) / 2.0 - CP.X;
        //        double right = MaxX - CP.X;
        //        double up = MaxY - CP.Y;
        //        double centerY = (MaxY + MinY) / 2.0 - CP.Y;
        //        double down = MinY - CP.Y;

        //        double[] vAddPosX = new double[] { left, left, centerX, right, right, centerX, centerX };
        //        double[] vAddPosY = new double[] { up, down, down, down, up, up, centerY };

        //        Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };
        //        for (int i = 0; i < 7; i++)
        //        {
        //            vCheckPos[i].X = vCheckPosX[i];
        //            vCheckPos[i].Y = vCheckPosY[i];
        //            vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

        //            snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
        //            if (snsdata.execResult != 0)
        //            {
        //                retval.execResult = snsdata.execResult;
        //                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR " + (i+1).ToString() + " - " + retval.execResult.ToString());
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(LU) : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
        //                return retval;
        //            }

        //            vAddPos[i].X = vAddPosX[i];
        //            vAddPos[i].Y = vAddPosY[i];
        //            vAddPos[i].Z = snsdata.sensoroffset;
        //            HeightVal[i] = snsdata.sensoroffset;
        //            if (Math.Abs(HeightVal[i]) > 20.0)
        //            {
        //                retval.ErrorDistanceSensor = true;
        //            }

        //            planePoints.Add(vAddPos[i]);
        //            if (lblValue[i] != null)
        //            {
        //                if (lblValue[i].CheckAccess())
        //                    lblValue[i].Content = HeightVal[i].ToString("0.000");
        //                else
        //                {
        //                    lblValue[i].Dispatcher.Invoke(new Action(delegate
        //                    {
        //                        lblValue[i].Content = HeightVal[i].ToString("0.000");
        //                    }));
        //                }
        //            }
        //        }

        //        // Laser Aiming Beam OFF
        //        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
        //        ShowRectangle(AimingLamp, Brushes.Black);

        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }


        //        if (retval.ErrorDistanceSensor)
        //        {
        //            Vector3D TmpPoint = new Vector3D();
        //            planePoints.Clear();

        //            HeightCT0 = HeightCT;
        //            HeightCT = 20.0;

        //            TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = 20.0; planePoints.Add(TmpPoint);
        //            TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = 20.0; planePoints.Add(TmpPoint);
        //            TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = 20.0; planePoints.Add(TmpPoint);
        //            TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = 20.0; planePoints.Add(TmpPoint);
        //            TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = 20.0; planePoints.Add(TmpPoint);
        //            TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = 20.0; planePoints.Add(TmpPoint);
        //            TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = 20.0; planePoints.Add(TmpPoint);

        //            // ?????
        //        }

        //        HeightCT = vAddPos[6].Z;

        //        CenterZ += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

        //        //// REL mm at 4 Corners, CD
        //        PointLU.X = SP1.X - CP.X;
        //        PointLU.Y = SP1.Y - CP.Y + pattern.fontValue.height;
        //        PointLD.Y = SP1.Y - CP.Y;
        //        PointRU.X = SP1.X - CP.X + totWidth;
        //        ////
        //        Vector3D Sum = new Vector3D();
        //        foreach (var mPoint in planePoints)
        //        {
        //            Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
        //        }
        //        Vector3D Centroid = new Vector3D();
        //        Centroid.X = Sum.X / planePoints.Count;
        //        Centroid.Y = Sum.Y / planePoints.Count;
        //        Centroid.Z = Sum.Z / planePoints.Count;
        //        double xx, xy, xz, yy, yz, zz;
        //        xx = xy = xz = yy = yz = zz = 0.0;
        //        foreach (var mPoint in planePoints)
        //        {
        //            xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
        //            xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
        //            xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
        //            yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
        //            yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
        //            zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
        //        }

        //        retval.NormalDir.X = xy * yz - xz * yy;
        //        retval.NormalDir.Y = xy * xz - yz * xx;
        //        retval.NormalDir.Z = xx * yy - xy * xy;

        //        double Ds = retval.NormalDir.X * Centroid.X + retval.NormalDir.Y * Centroid.Y + retval.NormalDir.Z * Centroid.Z;


        //        ////
        //        double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
        //        double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
        //        double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
        //        double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
        //        double PlaneCU = GetZfromPlane(0, PointLU.Y);
        //        double PlaneCD = GetZfromPlane(0, PointLD.Y);

        //        retval.PlaneCenterZ = GetZfromPlane(0, 0);

        //        double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
        //        PdiffLU = PlaneLU - retval.PlaneCenterZ;
        //        PdiffLD = PlaneLD - retval.PlaneCenterZ;
        //        PdiffRU = PlaneRU - retval.PlaneCenterZ;
        //        PdiffRD = PlaneRD - retval.PlaneCenterZ;
        //        PdiffCU = PlaneCU - retval.PlaneCenterZ;
        //        PdiffCD = PlaneCD - retval.PlaneCenterZ;
        //        double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
        //        double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
        //        double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

        //        ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", "NORMALDIR = " + retval.NormalDir.X.ToString("F4") + ", " + retval.NormalDir.Y.ToString("F4") + ", " + retval.NormalDir.Z.ToString("F4") + ", PLANECENTERZ = " + retval.PlaneCenterZ.ToString("F4"));

        //        if (PdiffDiff > 1.0)
        //        {
        //            Debug.WriteLine(string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff));
        //            // Error handling required!!
        //        }

        //        Dispatcher.Invoke(new Action(delegate
        //        {
        //            lblDispCenXCenY.Foreground = (retval.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen;
        //            lblDispCenXCenY.Content = retval.PlaneCenterZ.ToString("0.000;-0.000;0.000");

        //            TxtZeroOffset.Content = retval.PlaneCenterZ.ToString("0.000;-0.000;0.000");

        //            lblDispMinXMaxY.Foreground = (PdiffLU > 0 || retval.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
        //            lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

        //            lblDispMinXMinY.Foreground = (PdiffLD > 0 || retval.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
        //            lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

        //            lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || retval.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
        //            lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

        //            lblDispMaxXMinY.Foreground = (PdiffRD > 0 || retval.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
        //            lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

        //            lblDispCenXMaxY.Foreground = (PdiffCU > 0 || retval.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
        //            lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

        //            lblDispCenXMinY.Foreground = (PdiffCD > 0 || retval.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
        //            lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
        //        }
        //        ));

        //        int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
        //        int ic = (int)((PointRU.X - PointLU.X) + 0.5);
        //        byte[,] HC = new byte[jc, ic];

        //        double Yy = PointLU.Y;
        //        double Xx = PointLU.X;
        //        double Zz = 0;
        //        for (int r = 0; r < jc; r++)
        //        {
        //            Xx = PointLU.X;
        //            for (int c = 0; c < ic; c++)
        //            {
        //                Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
        //                if (Zz > 127.0) Zz = 127.0;
        //                if (Zz < -127.0) Zz = -127.0;
        //                HC[r, c] = (byte)(Zz + 127.0);
        //                Xx += 1.0;  // +1 mm;
        //            }
        //            Yy -= 1.0;      // -1 mm;
        //        }

        //        Dispatcher.Invoke(new Action(delegate
        //        {
        //            PlateColoring(HC);
        //        }));

        //        retval.bReady = true;

        //        double GetZfromPlane(double x, double y)
        //        {
        //            double pz;
        //            if (retval.NormalDir.Z != 0)
        //                pz = Ds / retval.NormalDir.Z - retval.NormalDir.X / retval.NormalDir.Z * x - retval.NormalDir.Y / retval.NormalDir.Z * y;
        //            else
        //                pz = 0;
        //            return pz;
        //        }
        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //        return retval;
        //    }
        //}


        //public async Task<CheckAreaData> Range_Test4High(string cmd, string vin, PatternValueEx pattern, byte errHeightFlag, byte errClineFlag)   // Plating  by TM SHIN
        //{
        //    string className = "SetControllerWindow2";
        //    string funcName = "Range_Test4High";

        //    CheckAreaData retval = new CheckAreaData();
        //    distanceSensorData snsdata = new distanceSensorData();
        //    ITNTResponseArgs respArg = new ITNTResponseArgs();

        //    int vinLength;
        //    double totWidth = 0;

        //    Vector3D SP0 = new Vector3D();
        //    Vector3D SP1 = new Vector3D();
        //    Vector3D CP = new Vector3D();

        //    Vector3D PointLU = new Vector3D();
        //    Vector3D PointLD = new Vector3D();
        //    Vector3D PointRU = new Vector3D();

        //    Vector3D[] vCheckPos = new Vector3D[7];
        //    double[] HeightVal = new double[7];
        //    Vector3D vector3 = new Vector3D();

        //    double HeightCT0 = 0;
        //    string value = "";
        //    string log = "";
        //    byte bHeadType = 0;
        //    string fName = "";

        //    List<Vector3D> planePoints = new List<Vector3D>();

        //    short gMinX = 0;
        //    short gMaxX = 0;
        //    short gMinY = 0;
        //    short gMaxY = 0;
        //    //short? CenterX = null;
        //    //short? CenterY = null;
        //    //short? CenterZ = null;

        //    //double SpX, SpY, SpZ, Ht, Wd, Pt, Ag;
        //    //double AZ = 0.0;
        //    //string AreaPosition = "";
        //    string sCurrentFunc = "PLATE CHECK";

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //        Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
        //        byte.TryParse(value, out bHeadType);

        //        vinLength = vin.Length;
        //        if (vinLength <= 0)
        //        {
        //            log = "EMPTY VIN (LENGTH <= " + vinLength.ToString() + ")";
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = -1;
        //            retval.sErrorMessage = log;
        //            return retval;
        //        }

        //        if (cbxPatternList.SelectedIndex >= 0)
        //            fName = cbxPatternList.SelectedItem.ToString();
        //        else
        //        {
        //            log = "SELECT PATTERN";
        //            retval.sErrorMessage = log;
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
        //            return retval;
        //        }


        //        if (ckbSkipCheckPlane.IsChecked == true)
        //        {
        //            retval.NormalDir.X = 0;
        //            retval.NormalDir.Y = 0;
        //            retval.NormalDir.Z = 1;

        //            retval.bReady = true;
        //            retval.execResult = 0;
        //            return retval;
        //        }

        //        // Laser Beam ON
        //        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + respArg.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET BEAM CONTROLL ERROR - " + respArg.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }

        //        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - BEAM ON ERROR : " + respArg.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM ON ERROR - " + respArg.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }
        //        ShowRectangle(AimingLamp, Brushes.Red);

        //        //txtAreaPosition.Text = "25.0";
        //        //Util.GetPrivateProfileValue("PLATE", "AREAPOSITION", "25.0", ref AreaPosition, Constants.PARAMS_INI_FILE);
        //        //AreaPosition = txtAreaPosition.Text;
        //        //AZ = double.Parse(AreaPosition);

        //        totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

        //        CP = pattern.positionValue.center3DPos;
        //        //if (AZ < CP.Z) AZ = CP.Z;

        //        SP0.X = totWidth / 2;
        //        SP0.Y = pattern.fontValue.height / 2;
        //        SP0.Z = 0;

        //        SP1 = CP - SP0; SP1.Z = CP.Z;




        //        // ABS mm
        //        double MinX = SP1.X;
        //        double MaxX = SP1.X + totWidth;
        //        double MinY = SP1.Y;
        //        double MaxY = SP1.Y + pattern.fontValue.height;

        //        //Mode_File.dMinX = MinX;
        //        //Mode_File.dMaxX = MaxX;
        //        //Mode_File.dMinY = MinY;
        //        //Mode_File.dMaxY = MaxY;

        //        // ABS BLU
        //        gMinX = (short)(MinX * pattern.headValue.stepLength + 0.5);
        //        gMaxX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
        //        gMinY = (short)(MinY * pattern.headValue.stepLength + 0.5);
        //        gMaxY = (short)(MaxY * pattern.headValue.stepLength + 0.5);

        //        // ABS mm
        //        //double CX = (MaxX + MinX) / 2.0;
        //        //double CY = (MaxY + MinY) / 2.0;
        //        double CX = CP.X;
        //        double CY = CP.Y;

        //        // ABS BLU
        //        short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
        //        short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);
        //        //Mode_File.CenterX = tCX;
        //        //Mode_File.CenterY = tCY;
        //        //Mode_File.CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);
        //        //CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);
        //        retval.centerPoint.X = CX;
        //        retval.centerPoint.Y = CY;
        //        retval.centerPoint.Z = CP.Z;
        //        retval.centerPointBLU.X = tCX;
        //        retval.centerPointBLU.Y = tCY;
        //        retval.centerPointBLU.Z = (short)(CP.Z * pattern.headValue.stepLength + 0.5);
        //        //CenterZ = (short)(CP.Z * pattern.headValue.stepLength + 0.5);


        //        respArg = await SendMotorSpeed("RANGE_TEST", (byte)motorSpeedType.MeasureMoving);
        //        if (respArg.execResult != 0)
        //        {
        //            //return retval;
        //            log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + respArg.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET MOTOR SPEED(MEASURE) ERROR - " + respArg.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }

        //        //m_currCMD = (byte)'M';
        //        //respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5), 0);
        //        //if (respArg.execResult != 0)
        //        //{
        //        //    return retval;
        //        //}


        //        //respArg = await SendMotorSpeed("RANGE_TEST", (byte)motorSpeedType.MeasureMoving);
        //        //if (respArg.execResult != 0)
        //        //{
        //        //    return retval;
        //        //}

        //        vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
        //        snsdata = await GetMeasureLength(vector3, 0, 1);
        //        if (snsdata.execResult != 0)
        //        {
        //            retval.execResult = snsdata.execResult;
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR 0 - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        double ShiftCT = snsdata.sensorshift;
        //        double HeightCT = snsdata.sensoroffset;
        //        double sensorDistance = snsdata.rawdistance;

        //        if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
        //        {
        //            retval.ErrorDistanceSensor = true;
        //            ShowLabelData2(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Red, null);
        //            //ShowLabelColor(lblDispCenXCenY, Brushes.Red, null);

        //            ShowLabelData(lblDispMinXMaxY, "");
        //            ShowLabelData(lblDispMinXMinY, "");
        //            ShowLabelData(lblDispMaxXMaxY, "");
        //            ShowLabelData(lblDispMaxXMinY, "");
        //            ShowLabelData(lblDispCenXMaxY, "");
        //            ShowLabelData(lblDispCenXMinY, "");

        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "CENTER Z Range is OUT OF RANGE : " + HeightCT.ToString("F3"));
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
        //            if (errHeightFlag == 0)
        //            {
        //                retval.execResult = -2;
        //                return retval;
        //            }
        //        }

        //        if (pattern.headValue.sensorPosition == 0)      //RIGHT
        //        {
        //            gMinX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
        //                gMinX = pattern.headValue.stepLength;                                   // && 7

        //            gMaxX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
        //                gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
        //        }
        //        else
        //        {
        //            gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            if (gMinX < (short)pattern.headValue.stepLength)                                         // && 7
        //            {
        //                gMinX = (short)pattern.headValue.stepLength;
        //                MinX = 1.0 - ShiftCT;
        //            }
        //            gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            if (gMaxX > (short)(pattern.headValue.max_X - pattern.headValue.stepLength))                     // && 7
        //            {
        //                gMaxX = (short)(pattern.headValue.max_X - pattern.headValue.stepLength);                     // && 7
        //                MaxX = pattern.headValue.max_X / pattern.headValue.stepLength - ShiftCT - 1.0;
        //            }
        //            tCX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);

        //            //gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            //if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
        //            //    gMinX = pattern.headValue.stepLength;
        //            //gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            //if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
        //            //    gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
        //        }

        //        ShowLabelData2(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Black, null);

        //        ShowLabelData(lblDispMinXMaxY, "");
        //        ShowLabelData(lblDispMinXMinY, "");
        //        ShowLabelData(lblDispMaxXMaxY, "");
        //        ShowLabelData(lblDispMaxXMinY, "");
        //        ShowLabelData(lblDispCenXMaxY, "");
        //        ShowLabelData(lblDispCenXMinY, "");

        //        //tCX = (short)((gMaxX + gMinX) / 2.0 + 0.5);
        //        //tCY = (short)((gMaxY + gMinY) / 2.0 + 0.5);

        //        double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
        //        double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

        //        Vector3D[] vAddPos = new Vector3D[7];
        //        double left = MinX - CP.X;
        //        double centerX = CX - CP.X;
        //        double right = MaxX - CP.X;
        //        double up = MaxY - CP.Y;
        //        double centerY = CY - CP.Y;
        //        double down = MinY - CP.Y;

        //        double[] vAddPosX = new double[] { left, left, centerX, right, right, centerX, centerX };
        //        double[] vAddPosY = new double[] { up, down, down, down, up, up, centerY };

        //        Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };
        //        for (int i = 0; i < 7; i++)
        //        {
        //            vCheckPos[i].X = vCheckPosX[i];
        //            vCheckPos[i].Y = vCheckPosY[i];
        //            vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

        //            snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
        //            if (snsdata.execResult != 0)
        //            {
        //                retval.execResult = snsdata.execResult;
        //                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR " + (i + 1).ToString() + " - " + retval.execResult.ToString());
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(LU) : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
        //                return retval;
        //            }

        //            vAddPos[i].X = vAddPosX[i];
        //            vAddPos[i].Y = vAddPosY[i];
        //            vAddPos[i].Z = snsdata.sensoroffset;
        //            HeightVal[i] = snsdata.sensoroffset;
        //            if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
        //            {
        //                retval.ErrorDistanceSensor = true;
        //                retval.sErrorMessage = "";
        //            }

        //            planePoints.Add(vAddPos[i]);
        //            ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
        //            HeightCT = HeightVal[i];    // TM SHIN
        //        }

        //        // Laser Aiming Beam OFF
        //        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
        //        ShowRectangle(AimingLamp, Brushes.Black);

        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            retval.execResult = respArg.execResult;
        //            return retval;
        //        }


        //        if (retval.ErrorDistanceSensor)
        //        {
        //            Vector3D TmpPoint = new Vector3D();
        //            planePoints.Clear();

        //            HeightCT0 = HeightCT;
        //            HeightCT = pattern.positionValue.checkDistanceHeight;

        //            TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
        //            TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
        //            TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
        //            TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
        //            TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
        //            TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
        //            TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);


        //            log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //            if (errHeightFlag == 0)
        //            {
        //                retval.execResult = -4;
        //                return retval;
        //            }
        //        }


        //        //double D_0_Gap_Z = CP.Z + pattern.headValue.distance0Position;                                                                     // && 8             // mm

        //        //CenterZ += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

        //        //// REL mm at 4 Corners, CD
        //        PointLU.X = SP1.X - CP.X;
        //        PointLU.Y = SP1.Y + pattern.fontValue.height - CP.Y;
        //        PointLD.Y = SP1.Y - CP.Y;
        //        PointRU.X = SP1.X + totWidth - CP.X;
        //        ////
        //        ///
        //        Vector3D Sum = new Vector3D();
        //        foreach (var mPoint in planePoints)
        //        {
        //            Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
        //        }

        //        Vector3D Centroid = new Vector3D();
        //        Centroid.X = Sum.X / planePoints.Count;
        //        Centroid.Y = Sum.Y / planePoints.Count;
        //        Centroid.Z = Sum.Z / planePoints.Count;
        //        double xx, xy, xz, yy, yz, zz;
        //        xx = xy = xz = yy = yz = zz = 0.0;
        //        foreach (var mPoint in planePoints)
        //        {
        //            xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
        //            xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
        //            xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
        //            yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
        //            yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
        //            zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
        //        }
        //        retval.NormalDir.X = xy * yz - xz * yy;
        //        retval.NormalDir.Y = xy * xz - yz * xx;
        //        retval.NormalDir.Z = xx * yy - xy * xy;

        //        double Ds = retval.NormalDir.X * Centroid.X + retval.NormalDir.Y * Centroid.Y + retval.NormalDir.Z * Centroid.Z;
        //        ////

        //        //double diffLU, diffLD, diffRD, diffRU, diffCU, diffCD;
        //        //diffLU = HeightLU - Mode_File.HeightCT;
        //        //diffLD = HeightLD - Mode_File.HeightCT;
        //        //diffRU = Mode_File.HeightRU - Mode_File.HeightCT;
        //        //diffRD = Mode_File.HeightRD - Mode_File.HeightCT;
        //        //diffCU = Mode_File.HeightCU - Mode_File.HeightCT;
        //        //diffCD = Mode_File.HeightCD - Mode_File.HeightCT;
        //        //double minDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(diffLU, diffLD), diffRU), diffRD), diffCU), diffCD);
        //        //double maxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(diffLU, diffLD), diffRU), diffRD), diffCU), diffCD);
        //        //double diffDiff = Math.Abs(maxDiff - minDiff);

        //        ////
        //        double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
        //        double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
        //        double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
        //        double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
        //        double PlaneCU = GetZfromPlane(0, PointLU.Y);
        //        double PlaneCD = GetZfromPlane(0, PointLD.Y);
        //        retval.PlaneCenterZ = GetZfromPlane(0, 0);

        //        //CenterZ = (short)((CP.Z + pattern.headValue.distance0Position + retval.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);              // BLU // && 8
        //        retval.centerPoint.Z = CP.Z + pattern.headValue.distance0Position + retval.PlaneCenterZ;
        //        retval.centerPointBLU.Z = (short)((CP.Z + pattern.headValue.distance0Position + retval.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);

        //        double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
        //        PdiffLU = PlaneLU - retval.PlaneCenterZ;
        //        PdiffLD = PlaneLD - retval.PlaneCenterZ;
        //        PdiffRU = PlaneRU - retval.PlaneCenterZ;
        //        PdiffRD = PlaneRD - retval.PlaneCenterZ;
        //        PdiffCU = PlaneCU - retval.PlaneCenterZ;
        //        PdiffCD = PlaneCD - retval.PlaneCenterZ;
        //        double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
        //        double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
        //        double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

        //        if (PdiffDiff > 5.0)
        //        {
        //            Debug.WriteLine(string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff));
        //            log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
        //            ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", log, log);

        //            // Error handling required!!
        //            if (errClineFlag == 0)
        //            {
        //                retval.execResult = -5;
        //                return retval;
        //            }
        //        }

        //        ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", "NORMALDIR = " + retval.NormalDir.X.ToString("F4") + ", " + retval.NormalDir.Y.ToString("F4") + ", " + retval.NormalDir.Z.ToString("F4") + ", PLANECENTERZ = " + retval.PlaneCenterZ.ToString("F4"));


        //        ShowLabelData2(lblDispCenXCenY, retval.PlaneCenterZ.ToString("0.000;-0.000;0.000"), Brushes.DarkGreen, null);
        //        if (PdiffLU > 0)
        //            ShowLabelData2(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);
        //        else
        //            ShowLabelData2(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);

        //        if (PdiffLD > 0)
        //            ShowLabelData2(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);
        //        else
        //            ShowLabelData2(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);

        //        if (PdiffRU > 0)
        //            ShowLabelData2(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
        //        else
        //            ShowLabelData2(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

        //        if (PdiffRD > 0)
        //            ShowLabelData2(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
        //        else
        //            ShowLabelData2(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

        //        if (PdiffCU > 0)
        //            ShowLabelData2(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
        //        else
        //            ShowLabelData2(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

        //        if (PdiffCD > 0)
        //            ShowLabelData2(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
        //        else
        //            ShowLabelData2(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);


        //        //ShowLabelData(TxtZeroOffset, retval.PlaneCenterZ.ToString("0.000;-0.000;0.000"));
        //        //Console.WriteLine(String.Format("Normal Vector ({0:F4},{1:F4},{2:F4}) / CenterZ {3:F4}", Mode_File.NormalDir.X, Mode_File.NormalDir.Y, Mode_File.NormalDir.Z, Mode_File.PlaneCenterZ));

        //        //ControlWindow.Dispatcher.Invoke(new Action(delegate
        //        //{
        //        //    var NormalStr = String.Format("Normal Vector ({0:F4}, {1:F4}, {2:F4}) / CenterZ {3:F4}", Mode_File.NormalDir.X, Mode_File.NormalDir.Y, Mode_File.NormalDir.Z, Mode_File.PlaneCenterZ);
        //        //    ControlWindow.txt_log.AppendText(NormalStr + Environment.NewLine);

        //        //    ControlWindow.normalX.Content = Mode_File.NormalDir.X.ToString("F4");
        //        //    ControlWindow.normalY.Content = Mode_File.NormalDir.Y.ToString("F4");
        //        //    ControlWindow.normalZ.Content = Mode_File.NormalDir.Z.ToString("F4");
        //        //    ControlWindow.planeZ.Content = Mode_File.PlaneCenterZ.ToString("F4");

        //        //    ControlWindow.lblDispCenXCenY.Foreground = (Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen;
        //        //    ControlWindow.lblDispCenXCenY.Content = Mode_File.PlaneCenterZ.ToString("0.000;-0.000;0.000");

        //        //    ControlWindow.TxtZeroOffset.Content = Mode_File.PlaneCenterZ.ToString("0.000;-0.000;0.000");

        //        //    ControlWindow.lblDispMinXMaxY.Foreground = (PdiffLU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
        //        //    ControlWindow.lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

        //        //    ControlWindow.lblDispMinXMinY.Foreground = (PdiffLD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
        //        //    ControlWindow.lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

        //        //    ControlWindow.lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
        //        //    ControlWindow.lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

        //        //    ControlWindow.lblDispMaxXMinY.Foreground = (PdiffRD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
        //        //    ControlWindow.lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

        //        //    ControlWindow.lblDispCenXMaxY.Foreground = (PdiffCU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
        //        //    ControlWindow.lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

        //        //    ControlWindow.lblDispCenXMinY.Foreground = (PdiffCD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
        //        //    ControlWindow.lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
        //        //}
        //        //));

        //        int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
        //        int ic = (int)((PointRU.X - PointLU.X) + 0.5);
        //        byte[,] HC = new byte[jc, ic];

        //        double Yy = PointLU.Y;
        //        double Xx = PointLU.X;
        //        double Zz = 0;
        //        for (int r = 0; r < jc; r++)
        //        {
        //            Xx = PointLU.X;
        //            for (int c = 0; c < ic; c++)
        //            {
        //                Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
        //                if (Zz > 127.0) Zz = 127.0;
        //                if (Zz < -127.0) Zz = -127.0;
        //                HC[r, c] = (byte)(Zz + 127.0);
        //                Xx += 1.0;  // +1 mm;
        //            }
        //            Yy -= 1.0;      // -1 mm;
        //        }

        //        Dispatcher.Invoke(new Action(delegate
        //        {
        //            PlateColoring(HC);
        //        }));


        //        double GetZfromPlane(double x, double y)
        //        {
        //            double pz;
        //            if (retval.NormalDir.Z != 0)
        //                pz = Ds / retval.NormalDir.Z - retval.NormalDir.X / retval.NormalDir.Z * x - retval.NormalDir.Y / retval.NormalDir.Z * y;
        //            else
        //                pz = 0;
        //            return pz;
        //        }

        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //        log = "EXCEPTION. ERROR = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
        //        retval.sErrorMessage = log;
        //        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
        //    }

        //    return retval;
        //} //End of Function


        public async Task<CheckAreaData> Range_Test(string cmd, string vin, PatternValueEx pattern, byte errHeightFlag, byte errClineFlag)   // Plating  by TM SHIN
        {
            string className = "SetControllerWindow2";
            string funcName = "Range_Test";

            CheckAreaData retval = new CheckAreaData();
            distanceSensorData snsdata = new distanceSensorData();
            ITNTResponseArgs respArg = new ITNTResponseArgs();

            int vinLength;
            double totWidth = 0;

            Vector3D SP0 = new Vector3D();
            Vector3D SP1 = new Vector3D();
            Vector3D CP = new Vector3D();

            Vector3D PointLU = new Vector3D();
            Vector3D PointLD = new Vector3D();
            Vector3D PointRU = new Vector3D();

            Vector3D[] vCheckPos = new Vector3D[7];
            double[] HeightVal = new double[7];
            Vector3D vector3 = new Vector3D();

            double HeightCT0 = 0;
            string value = "";
            string log = "";
            byte bHeadType = 0;
            string fName = "";

            List<Vector3D> planePoints = new List<Vector3D>();

            short gMinX = 0;
            short gMaxX = 0;
            short gMinY = 0;
            short gMaxY = 0;
            //short? CenterX = null;
            //short? CenterY = null;
            //short? CenterZ = null;

            //double SpX, SpY, SpZ, Ht, Wd, Pt, Ag;
            //double AZ = 0.0;
            //string AreaPosition = "";
            string sCurrentFunc = "PLATE CHECK";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    log = "EMPTY VIN (LENGTH <= " + vinLength.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = log;
                    return retval;
                }

                if (cbxPatternList.SelectedIndex >= 0)
                    fName = cbxPatternList.SelectedItem.ToString();
                else
                {
                    log = "SELECT PATTERN";
                    retval.errorInfo.sErrorMessage = log;
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    return retval;
                }


                if (ckbSkipCheckPlane.IsChecked == true)
                {
                    retval.NormalDir.X = 0;
                    retval.NormalDir.Y = 0;
                    retval.NormalDir.Z = 1;

                    retval.bReady = true;
                    retval.execResult = 0;
                    return retval;
                }

                // Laser Beam ON
                respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
                if (respArg.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + respArg.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET BEAM CONTROLL ERROR - " + respArg.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = respArg.execResult;
                    return retval;
                }

                respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                if (respArg.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM ON ERROR : " + respArg.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM ON ERROR - " + respArg.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = respArg.execResult;
                    return retval;
                }
                ShowRectangle(AimingLamp, Brushes.Red);

                //txtAreaPosition.Text = "25.0";
                //Util.GetPrivateProfileValue("PLATE", "AREAPOSITION", "25.0", ref AreaPosition, Constants.PARAMS_INI_FILE);
                //AreaPosition = txtAreaPosition.Text;
                //AZ = double.Parse(AreaPosition);

                totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

                CP = pattern.positionValue.center3DPos;
                //if (AZ < CP.Z) AZ = CP.Z;

                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;
                SP0.Z = 0;

                SP1 = CP - SP0; SP1.Z = CP.Z;




                // ABS mm
                double MinX = SP1.X;
                double MaxX = SP1.X + totWidth;
                double MinY = SP1.Y;
                double MaxY = SP1.Y + pattern.fontValue.height;

                //Mode_File.dMinX = MinX;
                //Mode_File.dMaxX = MaxX;
                //Mode_File.dMinY = MinY;
                //Mode_File.dMaxY = MaxY;

                // ABS BLU
                gMinX = (short)(MinX * pattern.headValue.stepLength + 0.5);
                gMaxX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
                gMinY = (short)(MinY * pattern.headValue.stepLength + 0.5);
                gMaxY = (short)(MaxY * pattern.headValue.stepLength + 0.5);

                // ABS mm
                double CX = (MaxX + MinX) / 2.0;
                double CY = (MaxY + MinY) / 2.0;


                // ABS BLU
                short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
                short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);
                //Mode_File.CenterX = tCX;
                //Mode_File.CenterY = tCY;
                //Mode_File.CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);
                //CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);
                retval.centerPoint.X = CX;
                retval.centerPoint.Y = CY;
                retval.centerPoint.Z = CP.Z;
                retval.centerPointBLU.X = tCX;
                retval.centerPointBLU.Y = tCY;
                retval.centerPointBLU.Z = (short)(CP.Z * pattern.headValue.stepLength + 0.5); 
                //CenterZ = (short)(CP.Z * pattern.headValue.stepLength + 0.5);


                respArg = await SendMotorSpeed("RANGE_TEST", (byte)motorSpeedType.MeasureMoving);
                if (respArg.execResult != 0)
                {
                    //return retval;
                    log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR : " + respArg.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET MOTOR SPEED(MEASURE) ERROR - " + respArg.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = respArg.execResult;
                    return retval;
                }

                //m_currCMD = (byte)'M';
                //respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5), 0);
                //if (respArg.execResult != 0)
                //{
                //    return retval;
                //}


                //respArg = await SendMotorSpeed("RANGE_TEST", (byte)motorSpeedType.MeasureMoving);
                //if (respArg.execResult != 0)
                //{
                //    return retval;
                //}

                vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                snsdata = await GetMeasureLength(vector3, 0, 1);
                if (snsdata.execResult != 0)
                {
                    retval.execResult = snsdata.execResult;
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR 0 - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                double ShiftCT = snsdata.sensorshift;
                double HeightCT = snsdata.sensoroffset;

                if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                {
                    retval.ErrorDistanceSensor = true;
                    ShowLabelData2(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Red, null);
                    //ShowLabelColor(lblDispCenXCenY, Brushes.Red, null);

                    ShowLabelData(lblDispMinXMaxY, "");
                    ShowLabelData(lblDispMinXMinY, "");
                    ShowLabelData(lblDispMaxXMaxY, "");
                    ShowLabelData(lblDispMaxXMinY, "");
                    ShowLabelData(lblDispCenXMaxY, "");
                    ShowLabelData(lblDispCenXMinY, "");

                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "CENTER Z Range is OUT OF RANGE : " + HeightCT.ToString("F3"));
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
                    if(errHeightFlag == 0)
                    {
                        retval.execResult = -2;
                        return retval;
                    }
                }

                if (pattern.headValue.sensorPosition == 0)      //RIGHT
                {
                    gMinX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;                                   // && 7

                    gMaxX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }
                else
                {
                    gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;
                    gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }

                ShowLabelData2(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Black, null);

                ShowLabelData(lblDispMinXMaxY, "");
                ShowLabelData(lblDispMinXMinY, "");
                ShowLabelData(lblDispMaxXMaxY, "");
                ShowLabelData(lblDispMaxXMinY, "");
                ShowLabelData(lblDispCenXMaxY, "");
                ShowLabelData(lblDispCenXMinY, "");

                tCX = (short)((gMaxX + gMinX) / 2.0 + 0.5);
                tCY = (short)((gMaxY + gMinY) / 2.0 + 0.5);

                double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

                Vector3D[] vAddPos = new Vector3D[7];
                double left = MinX - CP.X;
                double centerX = CX - CP.X;
                double right = MaxX - CP.X;
                double up = MaxY - CP.Y;
                double centerY = CY - CP.Y;
                double down = MinY - CP.Y;

                double[] vAddPosX = new double[] { left, left, centerX, right, right, centerX, centerX };
                double[] vAddPosY = new double[] { up, down, down, down, up, up, centerY };

                Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };
                for (int i = 0; i < 7; i++)
                {
                    vCheckPos[i].X = vCheckPosX[i];
                    vCheckPos[i].Y = vCheckPosY[i];
                    vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                    snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                    if (snsdata.execResult != 0)
                    {
                        retval.execResult = snsdata.execResult;
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR " + (i + 1).ToString() + " - " + retval.execResult.ToString());
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(LU) : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        return retval;
                    }

                    vAddPos[i].X = vAddPosX[i];
                    vAddPos[i].Y = vAddPosY[i];
                    vAddPos[i].Z = snsdata.sensoroffset;
                    HeightVal[i] = snsdata.sensoroffset;
                    if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                    {
                        retval.ErrorDistanceSensor = true;
                        retval.errorInfo.sErrorMessage = "";
                    }

                    planePoints.Add(vAddPos[i]);
                    ShowLabelData(lblValue[i], HeightVal[i].ToString("0.000"));
                    HeightCT = HeightVal[i];    // TM SHIN
                }

                // Laser Aiming Beam OFF
                respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                ShowRectangle(AimingLamp, Brushes.Black);

                if (respArg.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = respArg.execResult;
                    return retval;
                }


                if (retval.ErrorDistanceSensor)
                {
                    Vector3D TmpPoint = new Vector3D();
                    planePoints.Clear();

                    HeightCT0 = HeightCT;
                    HeightCT = pattern.positionValue.checkDistanceHeight;

                    TmpPoint.X = MinX - CP.X;   TmpPoint.Y = MaxY - CP.Y;   TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MinX - CP.X;   TmpPoint.Y = MinY - CP.Y;   TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X;     TmpPoint.Y = MinY - CP.Y;   TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X;   TmpPoint.Y = MinY - CP.Y;   TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X;   TmpPoint.Y = MaxY - CP.Y;   TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X;     TmpPoint.Y = MaxY - CP.Y;   TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X;     TmpPoint.Y = CY - CP.Y;     TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);


                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    if (errHeightFlag == 0)
                    {
                        retval.execResult = -4;
                        return retval;
                    }
                }


                //double D_0_Gap_Z = CP.Z + pattern.headValue.distance0Position;                                                                     // && 8             // mm

                //CenterZ += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

                //// REL mm at 4 Corners, CD
                PointLU.X = SP1.X - CP.X;
                PointLU.Y = SP1.Y + pattern.fontValue.height - CP.Y;
                PointLD.Y = SP1.Y - CP.Y;
                PointRU.X = SP1.X + totWidth - CP.X;
                ////
                ///
                Vector3D Sum = new Vector3D();
                foreach (var mPoint in planePoints)
                {
                    Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
                }

                Vector3D Centroid = new Vector3D();
                Centroid.X = Sum.X / planePoints.Count;
                Centroid.Y = Sum.Y / planePoints.Count;
                Centroid.Z = Sum.Z / planePoints.Count;
                double xx, xy, xz, yy, yz, zz;
                xx = xy = xz = yy = yz = zz = 0.0;
                foreach (var mPoint in planePoints)
                {
                    xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
                    xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
                    xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
                    yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
                    yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
                    zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
                }
                retval.NormalDir.X = xy * yz - xz * yy;
                retval.NormalDir.Y = xy * xz - yz * xx;
                retval.NormalDir.Z = xx * yy - xy * xy;

                double Ds = retval.NormalDir.X * Centroid.X + retval.NormalDir.Y * Centroid.Y + retval.NormalDir.Z * Centroid.Z;
                ////

                //double diffLU, diffLD, diffRD, diffRU, diffCU, diffCD;
                //diffLU = HeightLU - Mode_File.HeightCT;
                //diffLD = HeightLD - Mode_File.HeightCT;
                //diffRU = Mode_File.HeightRU - Mode_File.HeightCT;
                //diffRD = Mode_File.HeightRD - Mode_File.HeightCT;
                //diffCU = Mode_File.HeightCU - Mode_File.HeightCT;
                //diffCD = Mode_File.HeightCD - Mode_File.HeightCT;
                //double minDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(diffLU, diffLD), diffRU), diffRD), diffCU), diffCD);
                //double maxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(diffLU, diffLD), diffRU), diffRD), diffCU), diffCD);
                //double diffDiff = Math.Abs(maxDiff - minDiff);

                ////
                double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
                double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
                double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
                double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
                double PlaneCU = GetZfromPlane(0, PointLU.Y);
                double PlaneCD = GetZfromPlane(0, PointLD.Y);
                retval.PlaneCenterZ = GetZfromPlane(0, 0);

                //CenterZ = (short)((CP.Z + pattern.headValue.distance0Position + retval.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);              // BLU // && 8
                retval.centerPoint.Z = CP.Z + pattern.headValue.distance0Position + retval.PlaneCenterZ;
                retval.centerPointBLU.Z = (short)((CP.Z + pattern.headValue.distance0Position + retval.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);

                double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
                PdiffLU = PlaneLU - retval.PlaneCenterZ;
                PdiffLD = PlaneLD - retval.PlaneCenterZ;
                PdiffRU = PlaneRU - retval.PlaneCenterZ;
                PdiffRD = PlaneRD - retval.PlaneCenterZ;
                PdiffCU = PlaneCU - retval.PlaneCenterZ;
                PdiffCD = PlaneCD - retval.PlaneCenterZ;
                double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

                if (PdiffDiff > 5.0)
                {
                    Debug.WriteLine(string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff));
                    log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", log, log);

                    // Error handling required!!
                    if(errClineFlag == 0)
                    {
                        retval.execResult = -5;
                        return retval;
                    }
                }

                ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", "NORMALDIR = " + retval.NormalDir.X.ToString("F4") + ", " + retval.NormalDir.Y.ToString("F4") + ", " + retval.NormalDir.Z.ToString("F4") + ", PLANECENTERZ = " + retval.PlaneCenterZ.ToString("F4"));


                ShowLabelData2(lblDispCenXCenY, retval.PlaneCenterZ.ToString("0.000;-0.000;0.000"), Brushes.DarkGreen, null);
                if(PdiffLU > 0)
                    ShowLabelData2(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);
                else
                    ShowLabelData2(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);

                if (PdiffLD > 0)
                    ShowLabelData2(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);
                else
                    ShowLabelData2(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);

                if (PdiffRU > 0)
                    ShowLabelData2(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData2(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

                if (PdiffRD > 0)
                    ShowLabelData2(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData2(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

                if (PdiffCU > 0)
                    ShowLabelData2(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData2(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

                if (PdiffCD > 0)
                    ShowLabelData2(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData2(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);


                //ShowLabelData(TxtZeroOffset, retval.PlaneCenterZ.ToString("0.000;-0.000;0.000"));
                //Console.WriteLine(String.Format("Normal Vector ({0:F4},{1:F4},{2:F4}) / CenterZ {3:F4}", Mode_File.NormalDir.X, Mode_File.NormalDir.Y, Mode_File.NormalDir.Z, Mode_File.PlaneCenterZ));

                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                //{
                //    var NormalStr = String.Format("Normal Vector ({0:F4}, {1:F4}, {2:F4}) / CenterZ {3:F4}", Mode_File.NormalDir.X, Mode_File.NormalDir.Y, Mode_File.NormalDir.Z, Mode_File.PlaneCenterZ);
                //    ControlWindow.txt_log.AppendText(NormalStr + Environment.NewLine);

                //    ControlWindow.normalX.Content = Mode_File.NormalDir.X.ToString("F4");
                //    ControlWindow.normalY.Content = Mode_File.NormalDir.Y.ToString("F4");
                //    ControlWindow.normalZ.Content = Mode_File.NormalDir.Z.ToString("F4");
                //    ControlWindow.planeZ.Content = Mode_File.PlaneCenterZ.ToString("F4");

                //    ControlWindow.lblDispCenXCenY.Foreground = (Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen;
                //    ControlWindow.lblDispCenXCenY.Content = Mode_File.PlaneCenterZ.ToString("0.000;-0.000;0.000");

                //    ControlWindow.TxtZeroOffset.Content = Mode_File.PlaneCenterZ.ToString("0.000;-0.000;0.000");

                //    ControlWindow.lblDispMinXMaxY.Foreground = (PdiffLU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
                //    ControlWindow.lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

                //    ControlWindow.lblDispMinXMinY.Foreground = (PdiffLD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
                //    ControlWindow.lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

                //    ControlWindow.lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
                //    ControlWindow.lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

                //    ControlWindow.lblDispMaxXMinY.Foreground = (PdiffRD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
                //    ControlWindow.lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

                //    ControlWindow.lblDispCenXMaxY.Foreground = (PdiffCU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
                //    ControlWindow.lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

                //    ControlWindow.lblDispCenXMinY.Foreground = (PdiffCD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
                //    ControlWindow.lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
                //}
                //));

                int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
                int ic = (int)((PointRU.X - PointLU.X) + 0.5);
                byte[,] HC = new byte[jc, ic];

                double Yy = PointLU.Y;
                double Xx = PointLU.X;
                double Zz = 0;
                for (int r = 0; r < jc; r++)
                {
                    Xx = PointLU.X;
                    for (int c = 0; c < ic; c++)
                    {
                        Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
                        if (Zz > 127.0) Zz = 127.0;
                        if (Zz < -127.0) Zz = -127.0;
                        HC[r, c] = (byte)(Zz + 127.0);
                        Xx += 1.0;  // +1 mm;
                    }
                    Yy -= 1.0;      // -1 mm;
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    PlateColoring(HC);
                }));


                double GetZfromPlane(double x, double y)
                {
                    double pz;
                    if (retval.NormalDir.Z != 0)
                        pz = Ds / retval.NormalDir.Z - retval.NormalDir.X / retval.NormalDir.Z * x - retval.NormalDir.Y / retval.NormalDir.Z * y;
                    else
                        pz = 0;
                    return pz;
                }

                return retval;
            }
            catch(Exception ex)
            {
                retval.execResult = ex.HResult;
                log = "EXCEPTION. ERROR = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
                retval.errorInfo.sErrorMessage = log;
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
            }

            return retval;
            //    SP1.X = CP - SP1;
                                                                                                                                                                                                      



            //    //double SpX, SpY, SpZ, Ht, Wd, Pt, Ag;
            //    //int NoVin;

            //    //MPOINT SP = new MPOINT();
            //    //MPOINT CP = new MPOINT();

            //    //MPOINT PointLU = new MPOINT();
            //    //MPOINT PointLD = new MPOINT();
            //    //MPOINT PointRU = new MPOINT();
            //    //MPOINT PointRD = new MPOINT();                                                                                  
            //    //MPOINT PointCT = new MPOINT();
            //    //MPOINT PointCU = new MPOINT();
            //    //MPOINT PointCD = new MPOINT();
            //    //MPOINT VectorNormal = new MPOINT();
            //    //MPOINT VectorLuRd = new MPOINT();
            //    //MPOINT VectorRuRd = new MPOINT();
            //    //MPOINT VectorRot = new MPOINT();


            //    //string X_Position = "";
            //    //Util.GetPrivateProfileValue("VINDATA", "X", "0.0", ref X_Position, "Parameter.ini");

            //    //string Y_Position = "";
            //    //Util.GetPrivateProfileValue("VINDATA", "Y", "0.0", ref Y_Position, "Parameter.ini");

            //    //string Z_Position = "";
            //    //Util.GetPrivateProfileValue("VINDATA", "Z", "0.0", ref Z_Position, "Parameter.ini");

            //    //string Height = "";
            //    //Util.GetPrivateProfileValue("VINDATA", "HEIGHT", "0.0", ref Height, "Parameter.ini");
            //    //Ht = double.Parse(Height);

            //    //string Width = "";
            //    //Util.GetPrivateProfileValue("VINDATA", "WIDTH", "0.0", ref Width, "Parameter.ini");
            //    //Wd = double.Parse(Width);

            //    //string Pitch = "";
            //    //Util.GetPrivateProfileValue("VINDATA", "PITCH", "0.0", ref Pitch, "Parameter.ini");
            //    //Pt = double.Parse(Pitch);

            //    //string Angle = "";
            //    //Util.GetPrivateProfileValue("VINDATA", "ANGLE", "0.0", ref Angle, "Parameter.ini");
            //    //Ag = double.Parse(Angle);

            //    //string SensorPosition = "";
            //    //Util.GetPrivateProfileValue("SENSOR", "POSITION", "RIGHT", ref SensorPosition, "Parameter.ini");

            //    ControlWindow.readDisplacementTimer.Interval = TimeSpan.FromMilliseconds(10);

            //    NoVin = (mark6 == true) ? 6 : ManStr.Length;

            //    Mode_File.PlanePoints.Clear();

            //    Mode_File.ErrorDistanceSensor = false;

            //    // Absolute mm of Center Point
            //    CP.X = double.Parse(X_Position);
            //    CP.Y = double.Parse(Y_Position);
            //    CP.Z = double.Parse(Z_Position);
            //    // Relative mm
            //    SP.X = ((double)(NoVin - 1) * Pt + Wd) / 2.0;
            //    SP.Y = Ht / 2.0;
            //    // ABS mm of Start Point
            //    SP.X = CP.X - SP.X; SP.Y = CP.Y - SP.Y; SP.Z = CP.Z;

            //    // ABS mm
            //    double MinX = SP.X;
            //    double MaxX = SP.X + (double)(NoVin - 1) * Pt + Wd;
            //    double MinY = SP.Y;
            //    double MaxY = SP.Y + Ht;

            //    Mode_File.dMinX = MinX;
            //    Mode_File.dMaxX = MaxX;
            //    Mode_File.dMinY = MinY;
            //    Mode_File.dMaxY = MaxY;

            //    // ABS BLU
            //    Mode_File.gMinX = (short)(MinX * Mode_File.Step_Length + 0.5);
            //    Mode_File.gMaxX = (short)(MaxX * Mode_File.Step_Length + 0.5);
            //    Mode_File.gMinY = (short)(MinY * Mode_File.Step_Length + 0.5);
            //    Mode_File.gMaxY = (short)(MaxY * Mode_File.Step_Length + 0.5);

            //    // ABS mm
            //    double CX = (MaxX + MinX) / 2.0;
            //    double CY = (MaxY + MinY) / 2.0;

            //    // ABS BLU
            //    short tCX = (short)(CX * Mode_File.Step_Length + 0.5);
            //    short tCY = (short)(CY * Mode_File.Step_Length + 0.5);
            //    Mode_File.CenterX = tCX;
            //    Mode_File.CenterY = tCY;
            //    Mode_File.CenterZ = (short)(SP.Z * Mode_File.Step_Length + 0.5);

            //    string _Position = ""; Util.GetPrivateProfileValue("PARKING", "PARKING_Z", "110.0", ref _Position, "Parameter.ini");
            //    var Parking_Z = (short)(double.Parse(_Position) * Mode_File.Step_Length);       // LOAD PARKING Z
            //    var Range_CenterXY = tCX.ToString("X4") + tCY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
            //    GoPoint("M", Range_CenterXY);       // CT, 0 with None offset

            //    Mode_File.ShiftCT = 0.0;
            //    Mode_File.SensorShift = null; while (Mode_File.SensorShift == null) ;
            //    Mode_File.SensorShift = null; while (Mode_File.SensorShift == null) ;
            //    Mode_File.HeightCT = (double)Mode_File.SensorOffset;
            //    Mode_File.ShiftCT = (double)Mode_File.SensorShift;

            //    // Sensor Shift compensation
            //    Mode_File.ErrorDistanceSensor = false;

            //    if (Math.Abs(Mode_File.HeightCT) > 40.0)      // Z Diff +- 40mm    // && 7
            //    {
            //        Mode_File.ErrorDistanceSensor = true;

            //        // Error : Z Range over (Z axis unit +-40mm )
            //        ControlWindow.Dispatcher.Invoke(new Action(delegate
            //        {
            //            ControlWindow.txt_log.Text = "OUT OF RANGE Z";
            //            ControlWindow.lblDispCenXCenY.Foreground = (Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.Black;
            //            ControlWindow.lblDispCenXCenY.Content = Mode_File.HeightCT.ToString("0.000");
            //            ControlWindow.lblDispMinXMaxY.Content = "";
            //            ControlWindow.lblDispMinXMinY.Content = "";
            //            ControlWindow.lblDispMaxXMaxY.Content = "";
            //            ControlWindow.lblDispMaxXMinY.Content = "";
            //            ControlWindow.lblDispCenXMaxY.Content = "";
            //            ControlWindow.lblDispCenXMinY.Content = "";
            //        }
            //        ));

            //        return 1;
            //    }

            //    SensorPosition = "LEFT";          // && 7
            //    if (SensorPosition == "RIGHT")
            //    {
            //        Mode_File.gMinX -= (short)(Mode_File.ShiftCT * Mode_File.Step_Length + 0.5);
            //        if (Mode_File.gMinX < (short)Mode_File.Step_Length) // 1mm                            // && 7
            //            Mode_File.gMinX = (short)Mode_File.Step_Length;                                   // && 7
            //        Mode_File.gMaxX -= (short)(Mode_File.ShiftCT * Mode_File.Step_Length + 0.5);
            //        if (Mode_File.gMaxX > (short)(Mode_File.MAX_X - Mode_File.Step_Length))    // MAX_X - 1mm   // && 7
            //            Mode_File.gMaxX = (short)(Mode_File.MAX_X - Mode_File.Step_Length);                     // && 7
            //    }
            //    else
            //    {
            //        Mode_File.gMinX += (short)(Mode_File.ShiftCT * Mode_File.Step_Length + 0.5);
            //        if (Mode_File.gMinX < (short)Mode_File.Step_Length) // 1mm                            // && 7
            //            Mode_File.gMinX = (short)Mode_File.Step_Length;
            //        Mode_File.gMaxX += (short)(Mode_File.ShiftCT * Mode_File.Step_Length + 0.5);
            //        if (Mode_File.gMaxX > (short)(Mode_File.MAX_X - Mode_File.Step_Length))    // MAX_X - 1mm   // && 7
            //            Mode_File.gMaxX = (short)(Mode_File.MAX_X - Mode_File.Step_Length);                     // && 7
            //    }

            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispCenXCenY.Foreground = (Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.Black;
            //        ControlWindow.lblDispCenXCenY.Content = Mode_File.HeightCT.ToString("0.000");
            //        ControlWindow.lblDispMinXMaxY.Content = "";
            //        ControlWindow.lblDispMinXMinY.Content = "";
            //        ControlWindow.lblDispMaxXMaxY.Content = "";
            //        ControlWindow.lblDispMaxXMinY.Content = "";
            //        ControlWindow.lblDispCenXMaxY.Content = "";
            //        ControlWindow.lblDispCenXMinY.Content = "";
            //    }
            //    ));

            //    //
            //    //
            //    tCX = (short)(((double)Mode_File.gMaxX + (double)Mode_File.gMinX) / 2.0 + 0.5);
            //    tCY = (short)(((double)Mode_File.gMaxY + (double)Mode_File.gMinY) / 2.0 + 0.5);

            //    var Range_MinXMinY = Mode_File.gMinX.ToString("X4") + Mode_File.gMinY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
            //    var Range_MinXMaxY = Mode_File.gMinX.ToString("X4") + Mode_File.gMaxY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
            //    var Range_MaxXMaxY = Mode_File.gMaxX.ToString("X4") + Mode_File.gMaxY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
            //    var Range_MaxXMinY = Mode_File.gMaxX.ToString("X4") + Mode_File.gMinY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
            //    var Range_CenXMinY = tCX.ToString("X4") + Mode_File.gMinY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
            //    var Range_CenXMaxY = tCX.ToString("X4") + Mode_File.gMaxY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");
            //    Range_CenterXY = tCX.ToString("X4") + tCY.ToString("X4") + Parking_Z.ToString("X4") + 0.ToString("X4");

            //    GoPoint("M", Range_MinXMaxY);       // LU, 1
            //    Mode_File.HeightLU = 0.0;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.HeightLU = (double)Mode_File.SensorOffset;
            //    if (Math.Abs(Mode_File.HeightLU) > 40.0)  // && 7
            //    {
            //        Mode_File.ErrorDistanceSensor = true;
            //    }
            //    MPOINT TmpPoint = new MPOINT();
            //    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = Mode_File.HeightLU; Mode_File.PlanePoints.Add(TmpPoint);
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispMinXMaxY.Content = Mode_File.HeightLU.ToString("0.000");
            //    }
            //    ));

            //    GoPoint("M", Range_MinXMinY);       // LD, 2
            //    Mode_File.HeightLD = 0.0;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.HeightLD = (double)Mode_File.SensorOffset;
            //    if (Math.Abs(Mode_File.HeightLD) > 40.0) // && 7      
            //    {
            //        Mode_File.ErrorDistanceSensor = true;
            //    }
            //    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = Mode_File.HeightLD; Mode_File.PlanePoints.Add(TmpPoint);
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispMinXMinY.Content = Mode_File.HeightLD.ToString("0.000");
            //    }
            //    ));

            //    GoPoint("M", Range_CenXMinY);       // CD, 3
            //    Mode_File.HeightCD = 0.0;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.HeightCD = (double)Mode_File.SensorOffset;
            //    if (Math.Abs(Mode_File.HeightCD) > 40.0) // && 7     
            //    {
            //        Mode_File.ErrorDistanceSensor = true;
            //    }
            //    TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = Mode_File.HeightCD; Mode_File.PlanePoints.Add(TmpPoint);
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispCenXMinY.Content = Mode_File.HeightCD.ToString("0.000");
            //    }
            //    ));

            //    GoPoint("M", Range_MaxXMinY);       // RD, 4
            //    Mode_File.HeightRD = 0.0;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.HeightRD = (double)Mode_File.SensorOffset;
            //    if (Math.Abs(Mode_File.HeightRD) > 40.0) // && 7      
            //    {
            //        Mode_File.ErrorDistanceSensor = true;
            //    }
            //    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = Mode_File.HeightRD; Mode_File.PlanePoints.Add(TmpPoint);
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispMaxXMinY.Content = Mode_File.HeightRD.ToString("0.000");
            //    }
            //    ));

            //    GoPoint("M", Range_MaxXMaxY);       // RU, 5
            //    Mode_File.HeightRU = 0.0;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.HeightRU = (double)Mode_File.SensorOffset;
            //    if (Math.Abs(Mode_File.HeightRU) > 40.0) // && 7    
            //    {
            //        Mode_File.ErrorDistanceSensor = true;
            //    }
            //    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = Mode_File.HeightRU; Mode_File.PlanePoints.Add(TmpPoint);
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispMaxXMaxY.Content = Mode_File.HeightRU.ToString("0.000");
            //    }
            //    ));

            //    GoPoint("M", Range_CenXMaxY);       // CU, 6
            //    Mode_File.HeightCU = 0.0;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.HeightCU = (double)Mode_File.SensorOffset;
            //    if (Math.Abs(Mode_File.HeightCU) > 40.0) // && 7    
            //    {
            //        Mode_File.ErrorDistanceSensor = true;
            //    }
            //    TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = Mode_File.HeightCU; Mode_File.PlanePoints.Add(TmpPoint);
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispCenXMaxY.Content = Mode_File.HeightCU.ToString("0.000");
            //    }
            //    ));

            //    GoPoint("M", Range_CenterXY);       // CT, 7 point
            //    Mode_File.HeightCT = 0.0;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.SensorOffset = null; while (Mode_File.SensorOffset == null) ;
            //    Mode_File.HeightCT = (double)Mode_File.SensorOffset;
            //    if (Math.Abs(Mode_File.HeightCT) > 40.0) // && 7      
            //    {
            //        Mode_File.ErrorDistanceSensor = true;
            //    }
            //    TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = Mode_File.HeightCT; Mode_File.PlanePoints.Add(TmpPoint);
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.lblDispCenXCenY.Foreground = Brushes.Red;
            //        ControlWindow.lblDispCenXCenY.Content = Mode_File.HeightCT.ToString("0.000");
            //    }
            //    ));

            //    ControlWindow.readDisplacementTimer.Interval = TimeSpan.FromMilliseconds(500);

            //    if (Mode_File.ErrorDistanceSensor)
            //    {
            //        Mode_File.PlanePoints.Clear();

            //        Mode_File.HeightCT0 = Mode_File.HeightCT;
            //        Mode_File.HeightCT = 40.0;              // && 7

            //        TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = 40.0; Mode_File.PlanePoints.Add(TmpPoint); // && 7
            //        TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = 40.0; Mode_File.PlanePoints.Add(TmpPoint);
            //        TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = 40.0; Mode_File.PlanePoints.Add(TmpPoint);
            //        TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = 40.0; Mode_File.PlanePoints.Add(TmpPoint);
            //        TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = 40.0; Mode_File.PlanePoints.Add(TmpPoint);
            //        TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = 40.0; Mode_File.PlanePoints.Add(TmpPoint);
            //        TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = 40.0; Mode_File.PlanePoints.Add(TmpPoint); // && 7

            //        return 2;
            //    }

            //    Mode_File.CenterZ += (short)(Mode_File.HeightCT * Mode_File.Step_Length + 0.5);

            //    //// REL mm at 4 Corners, CD
            //    PointLU.X = SP.X - CP.X;
            //    PointLU.Y = SP.Y + Ht - CP.Y;
            //    PointLD.Y = SP.Y - CP.Y;
            //    PointRU.X = SP.X + Pt * (NoVin - 1) + Wd - CP.X;
            //    ////
            //    MPOINT Sum = new MPOINT();
            //    foreach (var mPoint in Mode_File.PlanePoints)
            //    {
            //        Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
            //    }
            //    MPOINT Centroid = new MPOINT();
            //    Centroid.X = Sum.X / Mode_File.PlanePoints.Count;
            //    Centroid.Y = Sum.Y / Mode_File.PlanePoints.Count;
            //    Centroid.Z = Sum.Z / Mode_File.PlanePoints.Count;
            //    double xx, xy, xz, yy, yz, zz;
            //    xx = xy = xz = yy = yz = zz = 0.0;
            //    foreach (var mPoint in Mode_File.PlanePoints)
            //    {
            //        xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
            //        xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
            //        xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
            //        yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
            //        yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
            //        zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
            //    }
            //    Mode_File.NormalDir.X = xy * yz - xz * yy;
            //    Mode_File.NormalDir.Y = xy * xz - yz * xx;
            //    Mode_File.NormalDir.Z = xx * yy - xy * xy;

            //    double Ds = Mode_File.NormalDir.X * Centroid.X + Mode_File.NormalDir.Y * Centroid.Y + Mode_File.NormalDir.Z * Centroid.Z;
            //    ////

            //    double diffLU, diffLD, diffRD, diffRU, diffCU, diffCD;
            //    diffLU = Mode_File.HeightLU - Mode_File.HeightCT;
            //    diffLD = Mode_File.HeightLD - Mode_File.HeightCT;
            //    diffRU = Mode_File.HeightRU - Mode_File.HeightCT;
            //    diffRD = Mode_File.HeightRD - Mode_File.HeightCT;
            //    diffCU = Mode_File.HeightCU - Mode_File.HeightCT;
            //    diffCD = Mode_File.HeightCD - Mode_File.HeightCT;
            //    double minDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(diffLU, diffLD), diffRU), diffRD), diffCU), diffCD);
            //    double maxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(diffLU, diffLD), diffRU), diffRD), diffCU), diffCD);
            //    double diffDiff = Math.Abs(maxDiff - minDiff);

            //    ////
            //    double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
            //    double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
            //    double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
            //    double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
            //    double PlaneCU = GetZfromPlane(0, PointLU.Y);
            //    double PlaneCD = GetZfromPlane(0, PointLD.Y);
            //    Mode_File.PlaneCenterZ = GetZfromPlane(0, 0);

            //    double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
            //    PdiffLU = PlaneLU - Mode_File.PlaneCenterZ;
            //    PdiffLD = PlaneLD - Mode_File.PlaneCenterZ;
            //    PdiffRU = PlaneRU - Mode_File.PlaneCenterZ;
            //    PdiffRD = PlaneRD - Mode_File.PlaneCenterZ;
            //    PdiffCU = PlaneCU - Mode_File.PlaneCenterZ;
            //    PdiffCD = PlaneCD - Mode_File.PlaneCenterZ;
            //    double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
            //    double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
            //    double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

            //    if (PdiffDiff > 1.0)
            //    {
            //        Debug.WriteLine(string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff));
            //        // Error handling required!!
            //        return 3;
            //    }

            //    Console.WriteLine(String.Format("Normal Vector ({0:F4},{1:F4},{2:F4}) / CenterZ {3:F4}", Mode_File.NormalDir.X, Mode_File.NormalDir.Y, Mode_File.NormalDir.Z, Mode_File.PlaneCenterZ));

            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        var NormalStr = String.Format("Normal Vector ({0:F4}, {1:F4}, {2:F4}) / CenterZ {3:F4}", Mode_File.NormalDir.X, Mode_File.NormalDir.Y, Mode_File.NormalDir.Z, Mode_File.PlaneCenterZ);
            //        ControlWindow.txt_log.AppendText(NormalStr + Environment.NewLine);

            //        ControlWindow.normalX.Content = Mode_File.NormalDir.X.ToString("F4");
            //        ControlWindow.normalY.Content = Mode_File.NormalDir.Y.ToString("F4");
            //        ControlWindow.normalZ.Content = Mode_File.NormalDir.Z.ToString("F4");
            //        ControlWindow.planeZ.Content = Mode_File.PlaneCenterZ.ToString("F4");

            //        ControlWindow.lblDispCenXCenY.Foreground = (Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen;
            //        ControlWindow.lblDispCenXCenY.Content = Mode_File.PlaneCenterZ.ToString("0.000;-0.000;0.000");

            //        ControlWindow.TxtZeroOffset.Content = Mode_File.PlaneCenterZ.ToString("0.000;-0.000;0.000");

            //        ControlWindow.lblDispMinXMaxY.Foreground = (PdiffLU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
            //        ControlWindow.lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

            //        ControlWindow.lblDispMinXMinY.Foreground = (PdiffLD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
            //        ControlWindow.lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

            //        ControlWindow.lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue;
            //        ControlWindow.lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

            //        ControlWindow.lblDispMaxXMinY.Foreground = (PdiffRD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
            //        ControlWindow.lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

            //        ControlWindow.lblDispCenXMaxY.Foreground = (PdiffCU > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
            //        ControlWindow.lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

            //        ControlWindow.lblDispCenXMinY.Foreground = (PdiffCD > 0 || Mode_File.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue; ;
            //        ControlWindow.lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
            //    }
            //    ));

            //    int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
            //    int ic = (int)((PointRU.X - PointLU.X) + 0.5);
            //    byte[,] HC = new byte[jc, ic];

            //    double Yy = PointLU.Y;
            //    double Xx = PointLU.X;
            //    double Zz = 0;
            //    for (int r = 0; r < jc; r++)
            //    {
            //        Xx = PointLU.X;
            //        for (int c = 0; c < ic; c++)
            //        {
            //            Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
            //            if (Zz > 127.0) Zz = 127.0;
            //            if (Zz < -127.0) Zz = -127.0;
            //            HC[r, c] = (byte)(Zz + 127.0);
            //            Xx += 1.0;  // +1 mm;
            //        }
            //        Yy -= 1.0;      // -1 mm;
            //    }
            //    ControlWindow.Dispatcher.Invoke(new Action(delegate
            //    {
            //        ControlWindow.PlateColoring(HC);
            //    }));


            //    double GetZfromPlane(double x, double y)
            //    {
            //        double pz;

            //        pz = Ds / Mode_File.NormalDir.Z - Mode_File.NormalDir.X / Mode_File.NormalDir.Z * x - Mode_File.NormalDir.Y / Mode_File.NormalDir.Z * y;

            //        return pz;
            //    }

            //}
            //catch (Exception)
            //{
            //    MessageBox.Show("Range_Test() error", "Error", MessageBoxButton.OK);
            //}
            //return 0;
        } //End of Function

        public int ReadPatternValue(ref PatternValueEx pattern)
        {
            string className = "SetControllerWindow2";
            string funcName = "ReadPatternValue";
            int retval = 0;
            string fName = "";
            double density = 0;
            try
            {
                if (cbxPatternList.SelectedIndex >= 0)
                    fName = cbxPatternList.SelectedItem.ToString();
                else
                    fName = "Pattern_DEFAULT";

                double.TryParse(txtDensity.Text, out density);

                pattern.name = fName;
                ReadPatternFontValue(ref pattern.fontValue);
                ReadPatternLaserValue(ref pattern.laserValue);
                ReadPatternSpeedValue(density, ref pattern.speedValue);
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
            string className = "SetControllerWindow2";
            string funcName = "ReadPatternFontValue";

            try
            {
                //fontValue = (FontValue)orgPattern.fontValue.Clone();

                if (cbxFontName.SelectedIndex >= 0)
                    fontValue.fontName = cbxFontName.SelectedItem.ToString();
                else
                    return -1;

                double.TryParse(txtHeight.Text, out fontValue.height);
                double.TryParse(txtWidth.Text, out fontValue.width);
                double.TryParse(txtPitch.Text, out fontValue.pitch);
                double.TryParse(txtAngle.Text, out fontValue.rotateAngle);
                double.TryParse(txtThickness.Text, out fontValue.thickness);
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
            string className = "SetControllerWindow2";
            string funcName = "ReadPatternHeadValue";
            short svalue = 0;
            double dbvalue = 0.0;

            try
            {
                headValue = (HeadValue)orgPattern.headValue.Clone();
                short.TryParse(lblMAX_X.Content.ToString(), out svalue); headValue.max_X = svalue;
                short.TryParse(lblMAX_Y.Content.ToString(), out svalue); headValue.max_Y = svalue;
                short.TryParse(lblMAX_Z.Content.ToString(), out svalue); headValue.max_Z = svalue;

                short.TryParse(lblStepLength.Content.ToString(), out headValue.stepLength);

                double.TryParse(txtSensorAngle.Text, out headValue.angleDegree);
                short.TryParse(txtOpmode.Text, out headValue.opmode);

                if (rdbtnSensorPosLeft.IsChecked == true)
                    headValue.sensorPosition = 1;
                else
                    headValue.sensorPosition = 0;

                if (rdbtnSpatterPosDown.IsChecked == true)
                    headValue.spatterType = 1;
                else
                    headValue.spatterType = 0;

                double.TryParse(txtPark_X.Text, out dbvalue); headValue.park3DPos.X = dbvalue;
                double.TryParse(txtPark_Y.Text, out dbvalue); headValue.park3DPos.Y = dbvalue;
                double.TryParse(txtPark_Z.Text, out dbvalue); headValue.park3DPos.Z = dbvalue;

                double.TryParse(txtHome_X.Text, out dbvalue); headValue.home3DPos.X = dbvalue;
                double.TryParse(txtHome_Y.Text, out dbvalue); headValue.home3DPos.Y = dbvalue;
                double.TryParse(txtHome_Z.Text, out dbvalue); headValue.home3DPos.Z = dbvalue;

                double.TryParse(txtRasterStartPoint.Text, out headValue.rasterSP);
                double.TryParse(txtRasterEndPoint.Text, out headValue.rasterEP);

                double.TryParse(txtDistance0Pos.Text, out headValue.distance0Position);

                short.TryParse(txtMarkDelayTime1.Text, out headValue.markDelayTime1);
                short.TryParse(txtMarkDelayTime2.Text, out headValue.markDelayTime2);
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
            string className = "SetControllerWindow2";
            string funcName = "ReadPatternPositionValue";
            double dbvalue = 0;

            try
            {
                positionValue = (PositionValue)orgPattern.positionValue.Clone();

                double.TryParse(txtCenter_X.Text, out dbvalue); positionValue.center3DPos.X = dbvalue;
                double.TryParse(txtCenter_Y.Text, out dbvalue); positionValue.center3DPos.Y = dbvalue;
                double.TryParse(txtCenter_Z.Text, out dbvalue); positionValue.center3DPos.Z = dbvalue;

                double.TryParse(txtCheckHeight.Text, out dbvalue); positionValue.checkDistanceHeight = dbvalue;
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
        public void ReadPatternLaserValue(ref LaserValue laserValue)
        {
            string className = "SetControllerWindow2";
            string funcName = "ReadPatternLaserValue";

            try
            {
                laserValue = (LaserValue)orgPattern.laserValue.Clone();
                short.TryParse(txtProfileNum.Text, out laserValue.waveformNum);
                short.TryParse(txtCleanProfileNum.Text, out laserValue.waveformClean);
                double.TryParse(txtCleanPosition.Text, out laserValue.cleanPosition);
                double.TryParse(txtCleanDelta.Text, out laserValue.cleanDelta);
                //if (rdbtnUseCleaningYes.IsChecked == true)
                if (chkboxuseCleaning.IsChecked == true)
                    laserValue.useCleaning = 1;
                else
                    laserValue.useCleaning = 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// //
        /// </summary>
        /// <param name="density"></param>
        /// <param name="speedValue"></param>
        public void ReadPatternSpeedValue(double density, ref SpeedValue speedValue)
        {
            string className = "SetControllerWindow2";
            string funcName = "ReadPatternSpeedValue";

            try
            {
                speedValue = (SpeedValue)orgPattern.speedValue.Clone();
                if (density != 1)
                {
                    short.TryParse(txbSpeedMarkInitialValue.Text, out speedValue.initSpeed4MarkV);//.ToString();
                    short.TryParse(txbSpeedMarkTargetValue.Text, out speedValue.targetSpeed4MarkV);//.ToString();
                    short.TryParse(txbSpeedMarkAccelValue.Text, out speedValue.accelSpeed4MarkV);//.ToString();
                    short.TryParse(txbSpeedMarkDecelValue.Text, out speedValue.decelSpeed4MarkV);//.ToString();
                }
                else
                {
                    short.TryParse(txbSpeedMarkInitialValue.Text, out speedValue.initSpeed4MarkR);//.ToString();
                    short.TryParse(txbSpeedMarkTargetValue.Text, out speedValue.targetSpeed4MarkR);//.ToString();
                    short.TryParse(txbSpeedMarkAccelValue.Text, out speedValue.accelSpeed4MarkR);//.ToString();
                    short.TryParse(txbSpeedMarkDecelValue.Text, out speedValue.decelSpeed4MarkR);//.ToString();
                }

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
            string className = "SetControllerWindow2";
            string funcName = "ReadPatternScanValue";

            try
            {
                double.TryParse(txtVisionStartPos.Text, out scanvalue.startU);
                double.TryParse(txtVisionScanLength.Text, out scanvalue.scanLen);
                double.TryParse(txtVisionParkPos.Text, out scanvalue.parkingU);
                double.TryParse(txtVisionHomePos.Text, out scanvalue.home_U);
                double.TryParse(txtVisionLinkPos.Text, out scanvalue.linkPos);
                short.TryParse(lblVisionMaxLength.Content.ToString(), out scanvalue.max_U);
                short.TryParse(lblVisionStepLength.Content.ToString(), out scanvalue.stepLength_U);

                short.TryParse(txbSpeedScanInitialValue.Text, out scanvalue.initSpeed4Scan);
                short.TryParse(txbSpeedScanTargetValue.Text, out scanvalue.targetSpeed4Scan);
                short.TryParse(txbSpeedScanAccelValue.Text, out scanvalue.accelSpeed4Scan);
                short.TryParse(txbSpeedScanDecelValue.Text, out scanvalue.decelSpeed4Scan);
                short.TryParse(txbSpeedScanFastInitialValue.Text, out scanvalue.initSpeed4ScanFree);
                short.TryParse(txbSpeedScanFastTargetValue.Text, out scanvalue.targetSpeed4ScanFree);
                short.TryParse(txbSpeedScanFastAccelValue.Text, out scanvalue.accelSpeed4ScanFree);
                short.TryParse(txbSpeedScanFastDecelValue.Text, out scanvalue.decelSpeed4ScanFree);

                if (rdbtnVisionLeft.IsChecked == true) scanvalue.reverseScan = 1;
                if (rdbtnVisionRight.IsChecked == true) scanvalue.reverseScan = 0;

            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        //public int ReadPatternFontValue(ref FontValue fontvalue)
        //{
        //    int retval = 0;

        //    return retval;
        //}


        //public int ReadPatternPositionValue(ref PositionValue posvalue)
        //{
        //    int retval = 0;

        //    return retval;
        //}


        //public int ReadatternSpeedValue(ref SpeedValue speedvalue)
        //{
        //    int retval = 0;

        //    return retval;
        //}

        //public int ReadPatternHeadValue(ref HeadValue headvalue)
        //{
        //    int retval = 0;

        //    return retval;
        //}

        //public int ReadPatternLaserValue(ref LaserValue laservalue)
        //{
        //    int retval = 0;

        //    return retval;
        //}

        //public int ReadPatternScanValue(ref ScanValue scanvalue)
        //{
        //    int retval = 0;

        //    return retval;
        //}


        public void DisplayPatternValue(byte byheadType, PatternValueEx pattern)
        {
            string className = "SetControllerWindow2";
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
            string className = "SetControllerWindow2";
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
            string className = "SetControllerWindow2";
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
                lblMAX_Z.Content = headValue.max_Z.ToString();

                lblStepLength.Content = headValue.stepLength.ToString();
                txtSensorAngle.Text = headValue.angleDegree.ToString("F3");
                txtOpmode.Text = headValue.opmode.ToString();

                if (headValue.sensorPosition == 0)
                {
                    rdbtnSensorPosLeft.IsChecked = false;
                    rdbtnSensorPosRight.IsChecked = true;
                }
                else
                {
                    rdbtnSensorPosLeft.IsChecked = true;
                    rdbtnSensorPosRight.IsChecked = false;
                }

                if (headValue.spatterType == 0)
                {
                    rdbtnSpatterPosDown.IsChecked = false;
                    rdbtnSpatterPosUp.IsChecked = true;
                }
                else
                {
                    rdbtnSpatterPosDown.IsChecked = true;
                    rdbtnSpatterPosUp.IsChecked = false;
                }

                txtPark_X.Text = headValue.park3DPos.X.ToString("F2");
                txtPark_Y.Text = headValue.park3DPos.Y.ToString("F2");
                txtPark_Z.Text = headValue.park3DPos.Z.ToString("F2");

                txtHome_X.Text = headValue.home3DPos.X.ToString("F2");
                txtHome_Y.Text = headValue.home3DPos.Y.ToString("F2");
                txtHome_Z.Text = headValue.home3DPos.Z.ToString("F2");

                txtRasterStartPoint.Text = headValue.rasterSP.ToString("F2");
                txtRasterEndPoint.Text = headValue.rasterEP.ToString("F2");

                txtDistance0Pos.Text = headValue.distance0Position.ToString("F2");

                txtMarkDelayTime1.Text = headValue.markDelayTime1.ToString();
                txtMarkDelayTime2.Text = headValue.markDelayTime2.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void DisplayPatternPositionValue(byte byheadType, PositionValue positionValue)
        {
            string className = "SetControllerWindow2";
            string funcName = "DisplayPatternPositionValue";

            try
            {
                txtCenter_X.Text = positionValue.center3DPos.X.ToString("F2");
                txtCenter_Y.Text = positionValue.center3DPos.Y.ToString("F2");
                txtCenter_Z.Text = positionValue.center3DPos.Z.ToString("F2");

                txtCheckHeight.Text = positionValue.checkDistanceHeight.ToString("F2");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void DisplayPatternLaserValue(byte byheadType, LaserValue laserValue)
        {
            string className = "SetControllerWindow2";
            string funcName = "DisplayPatternLaserValue";

            try
            {
                txtProfileNum.Text = laserValue.waveformNum.ToString();
                txtCleanProfileNum.Text = laserValue.waveformClean.ToString();
                txtCleanPosition.Text = laserValue.cleanPosition.ToString("F2");
                txtCleanDelta.Text = laserValue.cleanDelta.ToString("F2");
                txtDensity.Text = laserValue.density.ToString("F2");
                if (laserValue.useCleaning != 0)
                {
                    chkboxuseCleaning.IsChecked = true;
                    //rdbtnUseCleaningYes.IsChecked = true;
                    //rdbtnUseCleaningNo.IsChecked = false;
                }
                else
                {
                    chkboxuseCleaning.IsChecked = false;
                    //rdbtnUseCleaningNo.IsChecked = true;
                    //rdbtnUseCleaningYes.IsChecked = false;
                }
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
            string className = "SetControllerWindow2";
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
            string className = "SetControllerWindow2";
            string funcName = "DisplayPatternScanValue";

            try
            {
                txtVisionStartPos.Text = scanValue.startU.ToString("F2");
                txtVisionScanLength.Text = scanValue.scanLen.ToString("F2");
                txtVisionParkPos.Text = scanValue.parkingU.ToString("F2");
                txtVisionHomePos.Text = scanValue.home_U.ToString("F2");
                txtVisionLinkPos.Text = scanValue.linkPos.ToString("F2");
                lblVisionMaxLength.Content = scanValue.max_U.ToString();
                lblVisionStepLength.Content = scanValue.stepLength_U.ToString();

                txbSpeedScanInitialValue.Text = scanValue.initSpeed4Scan.ToString();
                txbSpeedScanTargetValue.Text = scanValue.targetSpeed4Scan.ToString();
                txbSpeedScanAccelValue.Text = scanValue.accelSpeed4Scan.ToString();
                txbSpeedScanDecelValue.Text = scanValue.decelSpeed4Scan.ToString();
                txbSpeedScanFastInitialValue.Text = scanValue.initSpeed4ScanFree.ToString();
                txbSpeedScanFastTargetValue.Text = scanValue.targetSpeed4ScanFree.ToString();
                txbSpeedScanFastAccelValue.Text = scanValue.accelSpeed4ScanFree.ToString();
                txbSpeedScanFastDecelValue.Text = scanValue.decelSpeed4ScanFree.ToString();

                if(scanValue.reverseScan == 0)
                {
                    rdbtnVisionLeft.IsChecked = false;
                    rdbtnVisionRight.IsChecked = true;
                }
                else
                {
                    rdbtnVisionLeft.IsChecked = true;
                    rdbtnVisionRight.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private string GetString4Textbox(TextBox tbox)
        {
            string retval = "";

            if (tbox.CheckAccess())
                retval = tbox.Text;
            else
            {
                retval = tbox.Dispatcher.Invoke(new Func<string>(delegate
                {
                    return tbox.Text;
                }));
            }

            return retval;
        }

        private void btnSavePattern_Click(object sender, RoutedEventArgs e)
        {
            //string patName = "";
            string className = "SetControllerWindow2";
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

                if(cbxPatternList.SelectedIndex < 0)
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
                if(retval != 0)
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

        private void btnSaveAsPattern_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
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
        }

        private async void btnLaserReset_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnLaserReset_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //string value = "";
            string cmd = "레이저 에러 초기화";
            string log = "";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ResetErrors();
                if (retval.execResult != 0)
                {
                    log = "에러 리셋 실패 (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ShowLog((byte)LOGTYPE.LOG_END, cmd, "", "");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("GET LASER STATUS - EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnMarkingConer_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnMarkingConer_Click";

            string strVin = "";
            string AreaVin = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //string value = "";
            string patName = "";
            string cmd = "MARKING CONER";

            try
            {
                strVin = txtVIN.Text.ToUpper();

                if (strVin.Length == 17)
                    Util.GetPrivateProfileValue("PLATE", "AREA17", "[---------------]", ref AreaVin, "Parameter.ini");
                else
                    Util.GetPrivateProfileValue("PLATE", "AREA19", "[-----------------]", ref AreaVin, "Parameter.ini");
                //Txt_data_Manual.Text = AreaVin;

                if (cbxPatternList.SelectedIndex < 0)
                    patName = "Pattern_DEFAULT";
                else
                    patName = cbxPatternList.Text;

                retval.execResult = ReadFontData(cmd, patName);
                if(retval.execResult != 0)
                {
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus(); string[] st = retval.recvString.Split(':'); LASERSTATUS Status = (LASERSTATUS)UInt32.Parse(st[1]);
                if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    ShowRectangle(EmissionLamp, Brushes.Black);
                    //EmissionLamp.Fill = Brushes.Black;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnGetLinkStatus_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnGetLinkStatus_Click";
            string log = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "GET LINK STATUS";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadLinkStatusAsync();
                if (retval.execResult != 0)
                {
                    //ShowLog("[GET LINK STATUS] ERROR - (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }

                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "STATUS: " + retval.recvString);
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                //ShowLog("[GET LINK STATUS] STATUS :" + retval.recvString);
                //ShowLog("END [GET LINK STATUS]");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSetLinkOn_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnSetLinkStatus_Click";
            string log = "";
            //byte link = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "SET LINK STATUS";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnVisionLinkPoint_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnVisionPoint_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            short stepLength = 0;
            short sVisionX = 0;
            short sParkY = 0;
            short sParkZ = 0;
            PositionValue posvalue = new PositionValue();
            ScanValue scanval = new ScanValue();
            HeadValue headval = new HeadValue();
            string cmd = "GO TO LINK POSITION";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                ReadPatternPositionValue(ref posvalue);
                ReadPatternScanValue(ref scanval);
                ReadPatternHeadValue(ref headval);

                short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                if (stepLength <= 0)
                    stepLength = 100;

                sVisionX = (short)(headval.park3DPos.X * stepLength + 0.5);
                sParkY = (short)(headval.park3DPos.Y * stepLength + 0.5);
                sParkZ = (short)(headval.park3DPos.Z * stepLength + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                //m_currCMD = (byte)'K';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(sVisionX, sParkY, sParkZ);
                //if (retval.execResult != 0)
                //{
                //    ShowLog("[GO PARKING TO POSITION] ERROR - (" + retval.execResult.ToString() + ")");
                //    return;
                //}

                if (scanval.stepLength_U <= 0)
                    scanval.stepLength_U = 100;

                sVisionX = (short)(scanval.linkPos * scanval.stepLength_U + 0.5);
                sParkY = (short)(headval.park3DPos.Y * scanval.stepLength_U + 0.5);
                sParkZ = (short)(headval.park3DPos.Z * scanval.stepLength_U + 0.5);

                m_currCMD = (byte)'M';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(sVisionX, sParkY, sParkZ, 0);
                if (retval.execResult != 0)
                {
                    //ShowLog("[GO TO VISION POSITION] ERROR - (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GO POINT ERROR - " + retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("[GO TO VISION POSITION] EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnVisionStartPoint_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnVisionStartPoint_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            //short stepLength = 0;
            short sVisionX = 0;
            short sParkY = 0;
            short sParkZ = 0;
            PositionValue posvalue = new PositionValue();
            ScanValue scanval = new ScanValue();
            HeadValue headval = new HeadValue();
            string status = "";
            string cmd = "GO TO VISION POSITION";

            try
            {
                //ShowLog("[GO TO VISION START POSITION] START");
                //ShowLog("[GET LINK STATUS] START");
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadLinkStatusAsync();
                if (retval.execResult != 0)
                {
                    //ShowLog("[GET LINK STATUS] ERROR - (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - " + retval.execResult.ToString());
                    return;
                }

                //ShowLog("[GET LINK STATUS] STATUS :" + retval.recvString);

                if (retval.recvString.Length < 8)
                {
                    //ShowLog("[GET LINK STATUS] ERROR - PLC VALUE ERROR");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - VALUE ERROR (" + retval.recvString + ")");
                    return;
                }

                status = retval.recvString.Substring(4, 4);
                if (status != PLCControlManager.SIGNAL_PLC2PC_ON)
                {
                    //ShowLog("START [SET LINK]");
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                    if (retval.execResult != 0)
                    {
                        //ShowLog("[SET LINK] ERROR - (" + retval.execResult.ToString() + ")");
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET LINK STATUS ERROR - " + retval.execResult.ToString());
                        return;
                    }

                    Thread.Sleep(500);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadLinkStatusAsync();
                    if (retval.execResult != 0)
                    {
                        //ShowLog("[GET LINK STATUS] ERROR - (" + retval.execResult.ToString() + ")");
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - " + retval.execResult.ToString());
                        return;
                    }

                    //ShowLog("[GET LINK STATUS] STATUS :" + retval.recvString);

                    if (retval.recvString.Length < 8)
                    {
                        //ShowLog("[GET LINK STATUS] ERROR - PLC VALUE ERROR");
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - VALUE ERROR (" + retval.recvString + ")");
                        return;
                    }

                    status = retval.recvString.Substring(4, 4);
                    if (status != PLCControlManager.SIGNAL_PLC2PC_ON)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "LINK STATUS ERROR - LINK FAIL");
                        return;
                    }
                }

                ReadPatternPositionValue(ref posvalue);
                ReadPatternScanValue(ref scanval);
                ReadPatternHeadValue(ref headval);

                //short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                //if (stepLength <= 0)
                //    stepLength = 100;

                sParkY = (short)(headval.park3DPos.Y * scanval.stepLength_U + 0.5);
                sParkZ = (short)(headval.park3DPos.Z * scanval.stepLength_U + 0.5);
                if (chbReverseScan.IsChecked == false)
                    sVisionX = (short)(scanval.startU * scanval.stepLength_U + 0.5);
                else
                    sVisionX = (short)((scanval.startU + scanval.scanLen) * scanval.stepLength_U + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                m_currCMD = (byte)'M';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(sVisionX, sParkY, sParkZ, 0);
                if (retval.execResult != 0)
                {
                    //ShowLog("[GO TO VISION POSITION] ERROR - (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GO POINT ERROR - " + retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                //ShowLog("[GO TO VISION POSITION] SUCCESS");
            }
            catch (Exception ex)
            {
                //string log = "";
                //log = string.Format("[GO TO VISION POSITION] EXCEPTION {0:X}, {1}", ex.HResult, ex.Message);
                //ShowLog(log);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnVisionStartScan_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnVisionStartScan_Click";// MethodBase.GetCurrentMethod().Name;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            short stepLength = 0;
            short sVisionX = 0;
            short sParkY = 0;
            short sParkZ = 0;
            PositionValue posvalue = new PositionValue();
            ScanValue scanval = new ScanValue();
            HeadValue headValue = new HeadValue();
            string status = "";
            string cmd = "START SCAN";

            try
            {
                //ShowLog("[MOVE SCAN] START");
                //ShowLog("[GET LINK STATUS] START");
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                ReadPatternPositionValue(ref posvalue);
                ReadPatternScanValue(ref scanval);
                ReadPatternHeadValue(ref headValue);

                sParkY = (short)(headValue.park3DPos.Y * scanval.stepLength_U + 0.5);
                sParkZ = (short)(headValue.park3DPos.Z * scanval.stepLength_U + 0.5);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadLinkStatusAsync();
                if (retval.execResult != 0)
                {
                    //ShowLog("[GET LINK STATUS] ERROR - (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - " + retval.execResult.ToString());
                    return;
                }

                //ShowLog("[GET LINK STATUS] STATUS :" + retval.recvString);

                if (retval.recvString.Length < 8)
                {
                    //ShowLog("[GET LINK STATUS] ERROR - PLC VALUE ERROR");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - VALUE ERROR (" + retval.recvString + ")");
                    return;
                }

                status = retval.recvString.Substring(4, 4);
                if (status != PLCControlManager.SIGNAL_PLC2PC_ON)
                {
                    //Move TO LINK Position
                    retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                    if (retval.execResult != 0)
                    {
                        return;
                    }

                    sVisionX = (short)(scanval.linkPos * scanval.stepLength_U + 0.5);

                    m_currCMD = (byte)'M';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(sVisionX, sParkY, sParkZ, 0);
                    if (retval.execResult != 0)
                    {
                        //ShowLog("[MVOE SCAN] ERROR - (" + retval.execResult.ToString() + ")");
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GO POINT ERROR - " + retval.execResult.ToString());
                        return;
                    }

                    //ShowLog("[SET LINK]");
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                    if (retval.execResult != 0)
                    {
                        //ShowLog("[SET LINK] ERROR - (" + retval.execResult.ToString() + ")");
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET LINK STATUS ERROR - " + retval.execResult.ToString());
                        return;
                    }

                    Thread.Sleep(500);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadLinkStatusAsync();
                    if (retval.execResult != 0)
                    {
                        //ShowLog("[GET LINK STATUS] ERROR - (" + retval.execResult.ToString() + ")");
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - " + retval.execResult.ToString());
                        return;
                    }

                    //ShowLog("[GET LINK STATUS] STATUS :" + retval.recvString);

                    if (retval.recvString.Length < 8)
                    {
                        //ShowLog("[GET LINK STATUS] ERROR - PLC VALUE ERROR");
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET LINK STATUS ERROR - VALUE ERROR (" + retval.recvString + ")");
                        return;
                    }

                    status = retval.recvString.Substring(4, 4);
                    if (status != PLCControlManager.SIGNAL_PLC2PC_ON)
                    {
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "LINK STATUS ERROR - LINK FAIL");
                        return;
                    }
                }

                //ReadPatternPositionValue(ref posvalue);
                //ReadPatternScanValue(ref scanval);

                //short.TryParse(lblStepLength.Content.ToString(), out stepLength);
                //if (stepLength <= 0)
                //    stepLength = 100;

                ////sVisionX = (short)((scanval.startU + scanval.scanLen) * scanval.stepLength_U + 0.5);
                //sParkY = (short)(posvalue.park3DPos.Y * scanval.stepLength_U + 0.5);
                //sParkZ = (short)(posvalue.park3DPos.Z * scanval.stepLength_U + 0.5);

                if (chbReverseScan.IsChecked == true)
                    sVisionX = (short)(scanval.startU * scanval.stepLength_U + 0.5);
                else
                    sVisionX = (short)((scanval.startU + scanval.scanLen) * scanval.stepLength_U + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.ScanMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                m_currCMD = (byte)'M';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(sVisionX, sParkY, sParkZ, 0);
                if (retval.execResult != 0)
                {
                    //ShowLog("[MOVE SCAN] ERROR - (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GO POINT ERROR - " + retval.execResult.ToString());
                    return;
                }

                //ShowLog("[MVOE SCAN] -[GO TO VISION START POSITION] START");
                //sParkY = (short)(posvalue.park3DPos.Y * scanval.stepLength_U + 0.5);
                //sParkZ = (short)(posvalue.park3DPos.Z * scanval.stepLength_U + 0.5);
                if (chbReverseScan.IsChecked == false)
                    sVisionX = (short)(scanval.startU * scanval.stepLength_U + 0.5);
                else
                    sVisionX = (short)((scanval.startU + scanval.scanLen) * scanval.stepLength_U + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if (retval.execResult != 0)
                {
                    return;
                }

                m_currCMD = (byte)'M';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(sVisionX, sParkY, sParkZ, 0);
                if (retval.execResult != 0)
                {
                    //ShowLog("[MVOE SCAN] ERROR - (" + retval.execResult.ToString() + ")");
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GO POINT ERROR - " + retval.execResult.ToString());
                    return;
                }
                //ShowLog("[MVOE SCAN] -[GO TO VISION START POSITION] END");
                //ShowLog("[MOVE SCAN] SUCCESS");
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void ShowRectangle(Rectangle rect, Brush brush)
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

        private void btnMoveScanHome_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnMoveScanMinus_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnMoveScanPlus_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void LaserErrorTxt_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ResetErrors();
        }

        private void SetString4Textbox(string text, TextBox tbox, Brush foreColor, Brush backColor)
        {
            if (tbox.CheckAccess())
            {
                tbox.Text = text;
                if (foreColor != null)
                    tbox.Foreground = foreColor;
                if (backColor != null)
                    tbox.Background = backColor;
            }
            else
            {
                tbox.Dispatcher.Invoke(new Action(delegate
                {
                    tbox.Text = text;
                    if (foreColor != null)
                        tbox.Foreground = foreColor;
                    if (backColor != null)
                        tbox.Background = backColor;
                }));
            }
        }


        public async void OnLaserControllerStatusChangedEventReceivedFunc(object sender, LaserControllerStatusEvnetArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "OnLaserControllerStatusChangedEventReceivedFunc";
            System.Windows.Media.Brush brushesBorder = System.Windows.Media.Brushes.Black;
            LASERSTATUS Status = 0;
            UInt32 ists = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if ((e.datatype == 0) & (e.recvdata1 != GetTextBoxData(LaserErrorTxt)))
                {
                    ShowTextBoxData(LaserErrorTxt, e.recvdata1);
                    //UInt32.TryParse(e.recvdata1, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ists);
                    UInt32.TryParse(e.recvdata1, out ists);
                    Status = (LASERSTATUS)ists;

                    if ((Status & LASERSTATUS.StatusNormalOn) != LASERSTATUS.StatusNormalOn)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Status 1 Error(Must be 1) : {0:X}", Status), Thread.CurrentThread.ManagedThreadId);
                        //Debug.WriteLine(string.Format("Status 1 Error(Must be 1) : {0:X}", Status));
                        //if ((Status & LASERSTATUS.PulseModeCWMode) == 0) Debug.WriteLine("0 : " + LASERSTATUS.PulseModeCWMode.ToString());
                        //if ((Status & LASERSTATUS.GateModeEnableDisable) == 0) Debug.WriteLine("0 : " + LASERSTATUS.GateModeEnableDisable.ToString());
                        //if ((Status & LASERSTATUS.FrontPanelDisplayLockedUnlocked) == 0) Debug.WriteLine("0 : " + LASERSTATUS.FrontPanelDisplayLockedUnlocked.ToString());
                        //if ((Status & LASERSTATUS.KeyswitchIsRemOnPosition) == 0) Debug.WriteLine("0 : " + LASERSTATUS.KeyswitchIsRemOnPosition.ToString());
#if LASER_YLR
                        if ((Status & LASERSTATUS.WaveformPulseModeOnOff) == 0)
                        {
                            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0 : " + LASERSTATUS.WaveformPulseModeOnOff.ToString() + " => Set 1", Thread.CurrentThread.ManagedThreadId);
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.EnableWaveformPulseMode();
                        }
#endif
                        brushesBorder = (brushesBorder != Brushes.Black) ? Brushes.Black : Brushes.OrangeRed;
                    }
                    if ((Status & LASERSTATUS.StatusNormalOff) != 0)
                    {
                        //Debug.WriteLine(string.Format("Status 0 Error(Must be 0): {0:X}", Status));
                        //if ((Status & LASERSTATUS.AnalogPowerControlEnableDisable) != 0) Debug.WriteLine("1 : " + LASERSTATUS.AnalogPowerControlEnableDisable.ToString());
                        //if ((Status & LASERSTATUS.PowerSupplyOnOff) != 0) Debug.WriteLine("1 : " + LASERSTATUS.PowerSupplyOnOff.ToString());
                        //if ((Status & LASERSTATUS.ModulationEnabledDisabled) != 0) Debug.WriteLine("1 : " + LASERSTATUS.ModulationEnabledDisabled.ToString());
                        //if ((Status & LASERSTATUS.HWEmissionCtrlEnabledDisabled) != 0) Debug.WriteLine("1 : " + LASERSTATUS.HWEmissionCtrlEnabledDisabled.ToString());
                        //if ((Status & LASERSTATUS.HWAimingBeamCtrlEnabledDisabled) != 0) Debug.WriteLine("1 : " + LASERSTATUS.HWAimingBeamCtrlEnabledDisabled.ToString());
                        //if ((Status & LASERSTATUS.FiberInterlockActiveOK) != 0) Debug.WriteLine("1 : " + LASERSTATUS.FiberInterlockActiveOK.ToString());
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Status 0 Error(Must be 0) : {0:X}", Status), Thread.CurrentThread.ManagedThreadId);
                        brushesBorder = (brushesBorder != Brushes.Black) ? Brushes.Black : Brushes.LightBlue;
                    }
                    if ((Status & LASERSTATUS.StatusError) != 0)
                    {
                        //ErrorLaserSource = true;
                        //ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
                        //ShowLabelData(Status.ToString("X"), lblLastErrorValue, Brushes.Red, null, brushesBorder);
                        ShowTextBoxData(LaserErrorTxt, Status.ToString("X8"));
                    }
                    else
                    {
                        //ErrorLaserSource = false;
                        //ShowLabelData("NORMAL", lblLaserStatus, Brushes.Black, Brushes.Green);//, brushesBorder);
                        //ShowLabelData(Status.ToString("X"), lblLastErrorValue, Brushes.Black, null, brushesBorder);
                    }

                    //if ((ists & (uint)LASERSTATUS.StatusError) != 0)
                    //{
                    //    ShowLabelData("ERROR", lblLaserStatus, System.Windows.Media.Brushes.White, System.Windows.Media.Brushes.Red);
                    //}
                    //else
                    //{
                    //    ShowLabelData("NORMAL", lblLaserStatus, System.Windows.Media.Brushes.Black, System.Windows.Media.Brushes.Green);
                    //}

                    //brushesEmission = Brushes.Red;
                    //brushesEmission = Brushes.Black;
                    if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                        ShowRectangle(EmissionLamp, Brushes.Red);
                    else
                        ShowRectangle(EmissionLamp, Brushes.Black);

                    //brushesAiming = Brushes.Red;
                    //brushesAiming = Brushes.Black;
                    if ((Status & LASERSTATUS.AimingBeamOnOff) != 0)
                        ShowRectangle(AimingLamp, Brushes.Red);
                    else
                        ShowRectangle(AimingLamp, Brushes.Black);

                    //ShowRectangle(brushesEmission, EmissionLamp);
                    //ShowRectangle(brushesAiming, AimingLamp);

                    //if ((Status & LASERSTATUS.StatusNormalOn) != LASERSTATUS.StatusNormalOn)
                    //{
                    //    brushesBorder = (brushesBorder != System.Windows.Media.Brushes.Black) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.OrangeRed;
                    //}
                    //if ((Status & LASERSTATUS.StatusNormalOff) != 0)
                    //{
                    //    brushesBorder = (brushesBorder != System.Windows.Media.Brushes.Black) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.LightBlue;
                    //}


                    //ControlWindow.EmissionLamp.Fill = brushesEmission;
                    //ControlWindow.AimingLamp.Fill = brushesAiming;


                    //brushesErrorForground = (ErrorLaserSource) ? Brushes.Red : Brushes.Black;
                    //ControlWindow.LaserErrorTxt.Text = Status.ToString("X");
                    //ControlWindow.LaserErrorTxt.BorderBrush = brushesBorder;
                }


                if (e.datatype == 1)
                {
                    if (e.recvdata1 != GetTextBoxData(AvgPowerTxt))
                        ShowTextBoxData(AvgPowerTxt, e.recvdata1);

                    if (e.recvdata2 != GetTextBoxData(PeakPowerTxt))
                        ShowTextBoxData(PeakPowerTxt, e.recvdata2);
                }

                if ((e.datatype == 2) & (e.recvdata1 != GetTextBoxData(TemperatureTxt)))
                    ShowTextBoxData(TemperatureTxt, e.recvdata1);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //return ex.HResult;
            }
        }


        private string GetLabelData(Label label)
        {
            string retval = "";
            if(label.CheckAccess())
            {
                retval = label.Content.ToString();
            }
            else
            {
                retval = label.Dispatcher.Invoke(new Func<string>(delegate
                {
                    string ret = "";
                    ret = label.Content.ToString();
                    return ret;
                }));
            }

            return retval;
        }

        private async void btnVision11_Click(object sender, RoutedEventArgs e)
        {
            string value = "";
            byte byUsePattern = 0;
            string patName = "";
            string patternName = "Pattern_RG";
            string vinstring = " KMTFE11DDRU041221 ";
            string seq = "4432";
            string rawtype = "R   ";
            int retval = 0;
            ITNTSendArgs args = new ITNTSendArgs(64);
            string sendmsg = "";
            string dist1 = "0000000000";
            string dist2 = "0000000000";
            byte sOrder = 1;

            try
            {
                if(cbxPatternList.SelectedIndex < 0)
                {
                    return;
                }

                patternName = cbxPatternList.SelectedItem.ToString();
                Util.GetPrivateProfileValue("PATTERNNAME", patternName, "R", ref value, Constants.PARAMS_INI_FILE);
                rawtype = value.PadRight(4, ' ');

                vinstring = txtVIN.Text.ToString();
                byUsePattern = Util.GetPrivateProfileValueByte("OPTION", "USEPATTERN", 0, Constants.PARAMS_INI_FILE);
                if (byUsePattern != 0)
                {
                    patName = patternName.PadRight(16, ' ');
                    sendmsg = "C3" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + "01" + sOrder.ToString("D2");
                }
                else
                {
                    sendmsg = "C2" + seq + vinstring + rawtype + "1" + dist1 + dist2 + "01" + sOrder.ToString("D2");
                }


                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 SEND VISION DATA", Thread.CurrentThread.ManagedThreadId);
                args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
                args.sendString = sendmsg;
                args.dataSize = sendmsg.Length;
                Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if ((value == "0") || (value == "2"))
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionServer.SendMessage(args);
                else
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionClient.SendMessage(args);
                return;
            }
            catch (Exception ex)
            {

            }
        }

        private async void btnVision12_Click(object sender, RoutedEventArgs e)
        {
            string value = "";
            byte byUsePattern = 0;
            string patName = "";
            string patternName = "";
            string vinstring = " KMTFE11DDRU041221 ";
            string seq = "4432";
            string rawtype = "R   ";
            int retval = 0;
            ITNTSendArgs args = new ITNTSendArgs(64);
            string sendmsg = "";
            string dist1 = "0000000000";
            string dist2 = "0000000000";
            byte sOrder = 1;

            try
            {
                if (cbxPatternList.SelectedIndex < 0)
                {
                    return;
                }

                patternName = cbxPatternList.SelectedItem.ToString();
                Util.GetPrivateProfileValue("PATTERNNAME", patternName, "R", ref value, Constants.PARAMS_INI_FILE);
                rawtype = value.PadRight(4, ' ');

                vinstring = txtVIN.Text.ToString();
                byUsePattern = Util.GetPrivateProfileValueByte("OPTION", "USEPATTERN", 0, Constants.PARAMS_INI_FILE);
                if (byUsePattern != 0)
                {
                    patName = patternName.PadRight(16, ' ');
                    sendmsg = "C7" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + "01" + sOrder.ToString("D2");
                }
                else
                {
                    sendmsg = "C6" + seq + vinstring + rawtype + "1" + dist1 + dist2 + "01" + sOrder.ToString("D2");
                }


                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 SEND VISION DATA", Thread.CurrentThread.ManagedThreadId);
                args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
                args.sendString = sendmsg;
                args.dataSize = sendmsg.Length;

                Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if ((value == "0") || (value == "2"))
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionServer.SendMessage(args);
                else
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionClient.SendMessage(args);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).vision.SendMessage(args);
                return;
            }
            catch (Exception ex)
            {

            }
        }

        private async void btnVision21_Click(object sender, RoutedEventArgs e)
        {
            string value = "";
            byte byUsePattern = 0;
            string patName = "";
            string patternName = "Pattern_RG";
            string vinstring = " KMTFE11DDRU041221 ";
            string seq = "4432";
            string rawtype = "R   ";
            int retval = 0;
            ITNTSendArgs args = new ITNTSendArgs(64);
            string sendmsg = "";
            string dist1 = "0000000000";
            string dist2 = "0000000000";
            byte sOrder = 2;

            try
            {
                if (cbxPatternList.SelectedIndex < 0)
                {
                    return;
                }

                patternName = cbxPatternList.SelectedItem.ToString();
                Util.GetPrivateProfileValue("PATTERNNAME", patternName, "R", ref value, Constants.PARAMS_INI_FILE);
                rawtype = value.PadRight(4, ' ');

                vinstring = txtVIN.Text.ToString();
                byUsePattern = Util.GetPrivateProfileValueByte("OPTION", "USEPATTERN", 0, Constants.PARAMS_INI_FILE);
                if (byUsePattern != 0)
                {
                    patName = patternName.PadRight(16, ' ');
                    sendmsg = "C3" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + "01" + sOrder.ToString("D2");
                }
                else
                {
                    sendmsg = "C2" + seq + vinstring + rawtype + "1" + dist1 + dist2 + "01" + sOrder.ToString("D2");
                }


                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 SEND VISION DATA", Thread.CurrentThread.ManagedThreadId);
                args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
                args.sendString = sendmsg;
                args.dataSize = sendmsg.Length;

                Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if ((value == "0") || (value == "2"))
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionServer.SendMessage(args);
                else
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionClient.SendMessage(args);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).vision.SendMessage(args);
                return;
            }
            catch (Exception ex)
            {

            }
        }

        private async void btnVision22_Click(object sender, RoutedEventArgs e)
        {
            string value = "";
            byte byUsePattern = 0;
            string patName = "";
            string patternName = "";
            string vinstring = " KMTFE11DDRU041221 ";
            string seq = "4432";
            string rawtype = "R   ";
            int retval = 0;
            ITNTSendArgs args = new ITNTSendArgs(64);
            string sendmsg = "";
            string dist1 = "0000000000";
            string dist2 = "0000000000";
            byte sOrder = 2;

            try
            {
                if (cbxPatternList.SelectedIndex < 0)
                {
                    return;
                }

                patternName = cbxPatternList.SelectedItem.ToString();
                Util.GetPrivateProfileValue("PATTERNNAME", patternName, "R", ref value, Constants.PARAMS_INI_FILE);
                rawtype = value.PadRight(4, ' ');

                vinstring = txtVIN.Text.ToString();
                byUsePattern = Util.GetPrivateProfileValueByte("OPTION", "USEPATTERN", 0, Constants.PARAMS_INI_FILE);
                if (byUsePattern != 0)
                {
                    patName = patternName.PadRight(16, ' ');
                    sendmsg = "C7" + seq + vinstring + rawtype + "1" + dist1 + dist2 + patName + "01" + sOrder.ToString("D2");
                }
                else
                {
                    sendmsg = "C6" + seq + vinstring + rawtype + "1" + dist1 + dist2 + "01" + sOrder.ToString("D2");
                }


                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0002 SEND VISION DATA", Thread.CurrentThread.ManagedThreadId);
                args.sendBuffer = Encoding.UTF8.GetBytes(sendmsg);
                args.sendString = sendmsg;
                args.dataSize = sendmsg.Length;

                Util.GetPrivateProfileValue("VISION", "TCPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                if ((value == "0") || (value == "2"))
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionServer.SendMessage(args);
                else
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).visionClient.SendMessage(args);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).vision.SendMessage(args);
                return;
            }
            catch (Exception ex)
            {

            }
        }

        private async void btnAirOn_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnAirOn_Click";
            string log = "";
            byte link = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "SET AIR ON";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnAirOff_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnAirOff_Click";
            string log = "";
            byte link = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "SET AIR OFF";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSetLinkOff_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnSetLinkStatus_Click";
            string log = "";
            //byte link = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string cmd = "SET LINK STATUS";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                //if (chbLink.IsChecked == true)
                //    link = ;
                //else
                //    link = PLCControlManager.SIGNAL_PC2PLC_OFF;

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", retval.execResult.ToString());
                    return;
                }
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnAimingOn_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnAimingOn_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string log = "";
            string cmd = "AIMING ON";
            try
            {
                //1. Aiming Beam OFF
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                if (retval.execResult != 0)
                {
                    log = "AimingBeamOFF. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }
                ShowRectangle(AimingLamp, Brushes.Black);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                if (retval.execResult != 0)
                {
                    log = "AimingBeamON. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }
                ShowRectangle(AimingLamp, Brushes.Red);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

            }
        }

        private async void btnAimingOff_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnAimingOff_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string log = "";
            string cmd = "AIMING OFF";
            try
            {
                //1. Aiming Beam OFF
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                if (retval.execResult != 0)
                {
                    log = "AimingBeamOFF. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }
                ShowRectangle(AimingLamp, Brushes.Black);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

            }
        }

        private async void btnMarkCorner_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnMarkCorner_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string log = "";
            string cmd = "MARK CORNER";
            CheckAreaData AreaData = new CheckAreaData();
            PatternValueEx pattern = new PatternValueEx();
            string value = "";
            short posX = 0, posY = 0, posZ = 0;
            byte bHeadType = 0;
            int vinLength = 19;
            double totWidth = 0;
            Vector3D centerPoint = new Vector3D();
            Vector3D startPoint0 = new Vector3D();
            Vector3D startPoint1 = new Vector3D();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                ReadPatternValue(ref pattern);

                // Laser Beam ON
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + retval.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET BEAM CONTROLL ERROR - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM ON ERROR : " + retval.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM ON ERROR - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                ShowRectangle(AimingLamp, Brushes.Red);

                //txtAreaPosition.Text = "25.0";
                string AreaPosition = "";
                double AreaWidth = 
                Util.GetPrivateProfileValue("PLATE", "AREAPOSITION", "25.0", ref AreaPosition, Constants.PARAMS_INI_FILE);
                AreaPosition = txtAreaPosition.Text;
                double AZ = double.Parse(AreaPosition);

                totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

                centerPoint = pattern.positionValue.center3DPos;
                //if (AZ < CP.Z) AZ = CP.Z;

                startPoint0.X = totWidth / 2;
                startPoint0.Y = pattern.fontValue.height / 2;
                startPoint0.Z = 0;

                startPoint1 = centerPoint - startPoint0; startPoint1.Z = centerPoint.Z;

                // ABS mm
                double MinX = startPoint1.X;
                double MaxX = startPoint1.X + totWidth;
                double MinY = startPoint1.Y;
                double MaxY = startPoint1.Y + pattern.fontValue.height;

                // ABS mm
                double CX = (MaxX + MinX) / 2.0;
                double CY = (MaxY + MinY) / 2.0;


                // ABS BLU
                short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
                short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.MeasureMoving);
                if (retval.execResult != 0)
                {
                    log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                m_currCMD = (byte)'M';
                posX = (short)(MinX * pattern.headValue.stepLength + 0.5);
                posY = (short)(MaxY * pattern.headValue.stepLength + 0.5);
                posZ = (short)(AZ * pattern.headValue.stepLength + 0.5);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(posX, posY, posZ, 0);
                if (retval.execResult != 0)
                {
                    log = "GO POINT 1. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                await Task.Delay(500);

                m_currCMD = (byte)'M';
                posX = (short)(MinX * pattern.headValue.stepLength + 0.5);
                posY = (short)(MinY * pattern.headValue.stepLength + 0.5);
                posZ = (short)(AZ * pattern.headValue.stepLength + 0.5);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(posX, posY, posZ, 0);
                if (retval.execResult != 0)
                {
                    log = "GO POINT 2. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                await Task.Delay(500);

                m_currCMD = (byte)'M';
                posX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
                posY = (short)(MinY * pattern.headValue.stepLength + 0.5);
                posZ = (short)(AZ * pattern.headValue.stepLength + 0.5);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(posX, posY, posZ, 0);
                if (retval.execResult != 0)
                {
                    log = "GO POINT 3. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                await Task.Delay(500);

                m_currCMD = (byte)'M';
                posX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
                posY = (short)(MaxY * pattern.headValue.stepLength + 0.5);
                posZ = (short)(AZ * pattern.headValue.stepLength + 0.5);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint(posX, posY, posZ, 0);
                if (retval.execResult != 0)
                {
                    log = "GO POINT 4. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                await Task.Delay(500);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM ON ERROR - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
                ShowRectangle(AimingLamp, Brushes.Black);

                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

            }
        }


        public async Task<CheckAreaData> Range_data(string vin, PatternValueEx Pattern)    // Making Area Corner data by TM SHIN
        {
            CheckAreaData retval = new CheckAreaData();
            Vector3D startPoint = new Vector3D();
            Vector3D centerPoint = new Vector3D();
            Vector3D PointLU = new Vector3D();
            Vector3D PointLD = new Vector3D();
            Vector3D PointRU = new Vector3D();
            Vector3D PointRD = new Vector3D();
            Vector3D PointCT = new Vector3D();
            Vector3D PointCD = new Vector3D();

            Vector3D vectorNormal = new Vector3D();
            Vector3D vectorLURD = new Vector3D();
            Vector3D vectorRURD = new Vector3D();
            Vector3D vectorRot = new Vector3D();
            Vector3D[] cornerPoint = new Vector3D[4];
            PatternValueEx pattern = new PatternValueEx();
            string value = "";
            string cmd = "MARKING CORNER";
            int vinLength = 0;

            try
            {
                if(vin.Length <= 0)
                {

                }
            }
            catch(Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
            //COMMAND_RESULT retval = new COMMAND_RESULT();

            //MPOINT SP = new MPOINT();
            //MPOINT CP = new MPOINT();
            //MPOINT PointLU = new MPOINT();
            //MPOINT PointLD = new MPOINT();
            //MPOINT PointRU = new MPOINT();
            //MPOINT PointRD = new MPOINT();
            //MPOINT PointCT = new MPOINT();
            //MPOINT PointCD = new MPOINT();
            //MPOINT VectorNormal = new MPOINT();
            //MPOINT VectorLuRd = new MPOINT();
            //MPOINT VectorRuRd = new MPOINT();
            //MPOINT VectorRot = new MPOINT();

            //MPOINT[] Corner_Point = new MPOINT[4];

            //string X_Position = "";
            //Util.GetPrivateProfileValue("VINDATA", "X", "0.0", ref X_Position, Pattern);

            //string Y_Position = "";
            //Util.GetPrivateProfileValue("VINDATA", "Y", "0.0", ref Y_Position, Pattern);

            //string Z_Position = "";
            //Util.GetPrivateProfileValue("VINDATA", "Z", "0.0", ref Z_Position, Pattern);

            //string Height = "";
            //Util.GetPrivateProfileValue("VINDATA", "HEIGHT", "0.0", ref Height, Pattern);
            //double Ht = double.Parse(Height);

            //string Width = "";
            //Util.GetPrivateProfileValue("VINDATA", "WIDTH", "0.0", ref Width, Pattern);
            //double Wd = double.Parse(Width);

            //string Pitch = "";
            //Util.GetPrivateProfileValue("VINDATA", "PITCH", "0.0", ref Pitch, Pattern);
            //double Pt = double.Parse(Pitch);

            //string Angle = "";
            //Util.GetPrivateProfileValue("VINDATA", "ANGLE", "0.0", ref Angle, Pattern);
            //double Ag = double.Parse(Angle);

            //string AreaPosition = "";
            //Util.GetPrivateProfileValue("PLATE", "AREAPOSITION", "50", ref AreaPosition, Pattern);
            //double AZ = double.Parse(AreaPosition);

            //short Sol_On = (short)Util.GetPrivateProfileValueInt("SOL", "SOL_ON", 0, Pattern);
            //short Sol_Off = (short)Util.GetPrivateProfileValueInt("SOL", "SOL_OFF", 0, Pattern);
            //short Dwell_Time = (short)Util.GetPrivateProfileValueInt("SOL", "DWELL", 0, Pattern);
            //short Density = (short)Util.GetPrivateProfileValueInt("VINDATA", "DENSITY", 10, Pattern);

            //var SetSol_OnOff = String.Concat(Sol_On.ToString("X4"), Sol_Off.ToString("X4"));
            //SolOnOffTime("S", SetSol_OnOff);            // send Sol OnOff time

            //var SetDwell_Time = String.Concat(Dwell_Time.ToString("X4"));
            //DwellTime("d", SetDwell_Time);              // send Dwell time 

            //int NoVin = DATA_.Length;

            //// ABS mm at Center Point
            //CP.X = double.Parse(X_Position);
            //CP.Y = double.Parse(Y_Position);
            ////CP.Z = double.Parse(Z_Position);
            //CP.Z = Mode_File.D_0_Gap_Z;                 // Absolute Nozzle position                     // && 8
            //AZ += Mode_File.D_0_Gap_Z;                                                                   // && 8
            //if (AZ < CP.Z) AZ = CP.Z;                   // Corner Z Position >= Marking Center Z Position

            //// Relative half size mm
            //SP.X = (Pt * (NoVin - 1) + Wd) / 2.0;
            //SP.Y = Ht / 2.0;
            //// Absolute mm at Start Point
            //SP.X = CP.X - SP.X; SP.Y = CP.Y - SP.Y; SP.Z = 0.0;

            //VectorNormal = Mode_File.NormalDir;
            //// Get Normalized Rotation Vector by Cross product of VectorNormal with Z Axis (0, 0, 1)
            //VectorRot.X = -VectorNormal.Y;
            //VectorRot.Y = VectorNormal.X;
            //VectorRot.Z = 0.0;
            //double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);
            //VectorRot.X /= sqXY; VectorRot.Y /= sqXY;
            //// Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
            //bool skipRot = false;
            //double cosValue = VectorNormal.Z / Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y + VectorNormal.Z * VectorNormal.Z);
            //double sinValue = Math.Sqrt(1.0 - cosValue * cosValue);
            //double R11, R12, R13, R21, R22, R23, R31, R32, R33;

            //if (cosValue > 0.9999986111)
            //{      // 0.1 mm difference between 60mm
            //    skipRot = true;
            //    R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
            //    R11 = R22 = R33 = 1.0;
            //}
            //else
            //{
            //    R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
            //    R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
            //    R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
            //    R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
            //    R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
            //    R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
            //    R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
            //    R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
            //    R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
            //}
            ///////
            //MPOINT M1 = new MPOINT();                                   // for fire data mm
            //MPOINT M = new MPOINT();                                    // for fire data mm

            //Mode_File.SendArea.Clear();

            //Corner_Point[0].X = Mode_File.dMinX; Corner_Point[0].Y = Mode_File.dMaxY; Corner_Point[0].Z = 0.0;  // LU
            //Corner_Point[1].X = Mode_File.dMinX; Corner_Point[1].Y = Mode_File.dMinY; Corner_Point[1].Z = 0.0;  // LD
            //Corner_Point[2].X = Mode_File.dMaxX; Corner_Point[2].Y = Mode_File.dMinY; Corner_Point[2].Z = 0.0;  // RD
            //Corner_Point[3].X = Mode_File.dMaxX; Corner_Point[3].Y = Mode_File.dMaxY; Corner_Point[3].Z = 0.0;  // RU

            //for (int i = 0; i < 4; i++)
            //{
            //    // ABS mm
            //    M1.X = Corner_Point[i].X;
            //    // Font offset compensation
            //    M1.Y = Corner_Point[i].Y;
            //    M1.Z = SP.Z;

            //    // TM SHIN
            //    M1.X -= CP.X; M1.Y -= CP.Y;

            //    M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

            //    M.X += CP.X; M.Y += CP.Y;
            //    double Mt = M.Z;
            //    M.Z = Mt + AZ + Mode_File.PlaneCenterZ;

            //    Debug.WriteLine(String.Format("{0:D3} =>{1,7:F3},{2,7:F3},{3,7:F3}", i, M.X, M.Y, M.Z));

            //    // Change to BLU(Unit 0.01mm)
            //    M.X *= (double)Mode_File.Step_Length; M.Y *= (double)Mode_File.Step_Length; M.Z *= (double)Mode_File.Step_Length;

            //    M_FONT FontData = new M_FONT();

            //    switch (i)
            //    {
            //        case 0: FontData.cN = (byte)0; FontData.fN = (byte)0; break;
            //        case 1: FontData.cN = (byte)0; FontData.fN = (byte)1; break;
            //        case 2:
            //            FontData.cN = (DATA_.Length == 17) ? (byte)16 : (byte)18; FontData.fN = (byte)0; break;
            //        case 3:
            //            FontData.cN = (DATA_.Length == 17) ? (byte)16 : (byte)18; FontData.fN = (byte)1; break;
            //    }
            //    FontData.mX = (UInt16)(M.X + 0.5); FontData.mY = (UInt16)(M.Y + 0.5); FontData.mZ = (UInt16)(M.Z + 0.5); FontData.mF = (byte)0;
            //    FontData.mC = 0;

            //    var m_font = FontData.cN.ToString("X2") + FontData.fN.ToString("X2") + FontData.mX.ToString("X4") + FontData.mY.ToString("X4") + FontData.mZ.ToString("X4") + FontData.mF.ToString("X4");
            //    Mode_File.SendArea.Add(m_font);
            //}


            ////Mark_Counter++;

            //Mode_File.Download_Data = true;

            //return;

            //MPOINT getRodrigueRotation(MPOINT XY0)
            //{
            //    MPOINT Tmp = new MPOINT();
            //    Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
            //    Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
            //    Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

            //    return Tmp;
            //}

        }

        private async void btnMarkPoint_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnMarkPoint_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string log = "";
            Stopwatch sw = new Stopwatch();
            //int repeatCount = 0;
            CheckAreaData chkdata = new CheckAreaData();
            string patName = "";
            string cmd = "GO TO MARK POINT";
            PatternValueEx pattern = new PatternValueEx();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                if (cbxPatternList.SelectedIndex >= 0)
                    patName = cbxPatternList.SelectedItem.ToString();
                else
                {
                    log = "SELECT PATTERN";
                    retval.errorInfo.sErrorMessage = log;
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    return;
                }

                ReadPatternValue(ref pattern);
                if (txtVIN.Text.Length <= 0)
                    txtVIN.Text = "*ABCDEFGHKLMOPQRST*";

                chkdata = await Range_Test(cmd, txtVIN.Text, pattern, 0 ,0);
                if (chkdata.execResult != 0)
                {
                    log = "RANGE TEST ERROR : (RESULT = " + chkdata.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving);
                if(retval.execResult != 0)
                {
                    log = "MOTOR SPEED SETTING ERROR : (RESULT = " + chkdata.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
#if LASER_YLR
#else
                chkdata = await GetMarkPosition("", pattern, 0, 0);
                if (chkdata.execResult != 0)
                {
                    return;
                }
#endif
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint((short)chkdata.centerPointBLU.X, (short)chkdata.centerPointBLU.Y, (short)chkdata.centerPointBLU.Z, 0);
                if (retval.execResult != 0)
                {
                    log = "GO POINT ERROR : (RESULT = " + chkdata.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK PLATE - SUCCESS", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                log = "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
            }
        }


        public async Task<CheckAreaData> GetMarkPosition(string cmd, PatternValueEx pattern, byte errHeightFlag, byte errClineFlag)
        {
            string className = "SetControllerWindow2";
            string funcName = "GetMarkPosition";

            distanceSensorData sensorData = new distanceSensorData();
            CheckAreaData areaData = new CheckAreaData();

            Vector3D CP = new Vector3D();
            Vector3D vectorCP = new Vector3D();

            short tCX = 0;
            short tCY = 0;

            double ShiftCT = 0;
            double HeightCT = 0;
            string fName = "";
            string log = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_START, className, funcName, "[CHECK MARK POSITION]");

                if (cbxPatternList.SelectedIndex >= 0)
                    fName = cbxPatternList.SelectedItem.ToString();
                else
                {
                    log = "SELECT PATTERN";
                    areaData.errorInfo.sErrorMessage = log;
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    areaData.execResult = -1;
                    return areaData;
                }

                areaData.ErrorDistanceSensor = false;
                // Absolute mm of Center Point
                CP = pattern.positionValue.center3DPos;

                tCX = (short)(CP.X * pattern.headValue.stepLength + 0.5);
                tCY = (short)(CP.Y * pattern.headValue.stepLength + 0.5);

                areaData.centerPoint.X = CP.X;
                areaData.centerPoint.Y = CP.Y;
                areaData.centerPoint.Z = CP.Z + pattern.headValue.distance0Position;

                areaData.centerPointBLU.X = tCX;
                areaData.centerPointBLU.Y = tCY;
                areaData.centerPointBLU.Z = (short)((CP.Z + pattern.headValue.distance0Position) * pattern.headValue.stepLength + 0.5);

                vectorCP.X = tCX;
                vectorCP.Y = tCY;
                vectorCP.Z = (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5);

                sensorData = await GetMeasureLength(vectorCP, 0, 1);
                if (sensorData.execResult != 0)
                {
                    areaData.execResult = sensorData.execResult;

                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR 0 - " + sensorData.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + sensorData.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                    ////retval.execResult = snsdata.execResult;
                    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CENTER) : ERROR = " + sensorData.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                    //log = "SELECT PATTERN";
                    //areaData.sErrorMessage = log;
                    //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);

                    return areaData;
                }

                //ShowLabelData(sensorData.rawdistance.ToString("F4"), lblDispHeightValue);
                //ShowLabelData(sensorData.sensoroffset.ToString("F4"), lblDispHeightCosine);

                ShiftCT = sensorData.sensorshift;
                HeightCT = sensorData.sensoroffset;

                if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
                    areaData.ErrorDistanceSensor = true;

                    ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Red, null, null);
                    ShowLabelData("", lblDispMinXMaxY);
                    ShowLabelData("", lblDispMinXMinY);
                    ShowLabelData("", lblDispMaxXMaxY);
                    ShowLabelData("", lblDispMaxXMinY);
                    ShowLabelData("", lblDispCenXMaxY);
                    ShowLabelData("", lblDispCenXMinY);

                    //double CenterZ = (short)((CP.Z + pattern.headValue.distance0Position) * pattern.headValue.stepLength + 0.5);
                    areaData.centerPoint.Z = CP.Z + pattern.headValue.distance0Position;
                    areaData.centerPointBLU.Z = (short)((CP.Z + pattern.headValue.distance0Position) * pattern.headValue.stepLength + 0.5);

                    areaData.PlaneCenterZ = CP.Z + pattern.headValue.distance0Position;

                    areaData.errorInfo.sErrorMessage = "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000");

                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "CENTER Z Range is OUT OF RANGE : " + HeightCT.ToString("F3"));
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
                    if (errHeightFlag == 0)
                    {
                        areaData.execResult = -2;
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "CENTER Z Range is OUT OF RANGE : " + HeightCT.ToString("F3") + " RETURN");
                        return areaData;
                    }
                }
                else
                {
                    tCX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    vectorCP.X = tCX;

                    sensorData = await GetMeasureLength(vectorCP, 0, 1);
                    if (sensorData.execResult != 0)
                    {
                        areaData.execResult = sensorData.execResult;

                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET MEASURE ERROR 1 - " + sensorData.execResult.ToString());
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + sensorData.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        ////retval.execResult = snsdata.execResult;
                        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CENTER) : ERROR = " + sensorData.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //areaData.execResult = sensorData.execResult;

                        //ITNTErrorCode();
                        //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength = " + sensorData.execResult.ToString(), "");
                        return areaData;
                    }

                    ShiftCT = sensorData.sensorshift;
                    HeightCT = sensorData.sensoroffset;

                    areaData.centerPoint.Z += HeightCT;// CP.Z + pattern.headValue.distance0Position;
                    areaData.centerPointBLU.Z += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

                    areaData.PlaneCenterZ = HeightCT;

                    ShowLabelData(HeightCT.ToString("0.000;-0.000;0.000"), lblDispCenXCenY, Brushes.Black);
                    ShowLabelData(ShiftCT.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMaxY);//, (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    ShowLabelData(sensorData.rawdistance.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY);//, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    ShowLabelData("", lblDispMinXMaxY);
                    ShowLabelData("", lblDispMinXMinY);
                    ShowLabelData("", lblDispMaxXMaxY);
                    ShowLabelData("", lblDispMaxXMinY);

                    //ShowLabelData(PdiffLU.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMaxY, (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    //ShowLabelData(PdiffLD.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMinY, (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    //ShowLabelData(PdiffRU.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMaxY, (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    //ShowLabelData(PdiffRD.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMinY, (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    //ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Black, null, null);
                    //ShowLabelData("", lblDispCenXMaxY);
                    //ShowLabelData("", lblDispCenXMinY);
                }
                return areaData;
            }
            catch (Exception ex)
            {
                areaData.execResult = ex.HResult;
                log = "EXCEPTION. ERROR = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
                areaData.errorInfo.sErrorMessage = log;
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);

                ////MessageBox.Show(ERR.ToString());
                //areaData.execResult = ex.HResult;

                return areaData;
            }
        }
        
        private async void btnFirmware_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnFirmware_Click";

            string ver = "";
            string log = "";
            string cmd = "FIRMWARE VERSION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                //Brush brus = btnFirmware.Background;

                ver = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GetFWVersion();
                if (ver.Length > 0)
                {
                    //if (ver.CompareTo("101") >= 0)
                    //    ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 1;
                    //else
                    //    ((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
                    //lblfwVersion.Content = ver;
                    //((MainWindow)System.Windows.Application.Current.MainWindow).fwVersion = ver;

                    ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, "FIRMWARE VERSION = " + ver, "");
                }
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "Read Failure");
                    //((MainWindow)System.Windows.Application.Current.MainWindow).fwVersionFlag = 0;
                }
            }
            catch (Exception ex)
            {
                log = "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnSaveLaserValue_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnSaveHeaderValue_Click";

            HeadValue headvalue = new HeadValue();
            byte byHeadType = 0;
            string value = "";
            string cmd = "FIRMWARE VERSION";
            string log = "";
            int retval = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out byHeadType);
                retval = ImageProcessManager.SetPatternHeadValue(byHeadType, headvalue, 1);
                if (retval != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "Save Failure");
                }
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                }
            }
            catch (Exception ex)
            {
                log = "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnSaveHeaderValue_Click(object sender, RoutedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "btnSaveHeaderValue_Click";

            HeadValue headvalue = new HeadValue();
            byte byHeadType = 0;
            string value = "";
            string cmd = "FIRMWARE VERSION";
            string log = "";
            int retval = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out byHeadType);
                retval = ImageProcessManager.SetPatternHeadValue(byHeadType, headvalue, 1);
                if(retval != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "Save Failure");
                }
                else
                {
                    ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                }
            }
            catch (Exception ex)
            {
                log = "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
            }
        }

        private string GetTextBoxData(TextBox textBox)
        {
            string retval = "";
            if (textBox.CheckAccess())
            {
                retval = textBox.Text.ToString();
            }
            else
            {
                retval = textBox.Dispatcher.Invoke(new Func<string>(delegate
                {
                    string ret = "";
                    ret = textBox.Text.ToString();
                    return ret;
                }));
            }

            return retval;
        }

    }
}
