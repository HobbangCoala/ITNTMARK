using ITNTCOMMM;
using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// ManualMarkWindow3.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManualMarkWindow3 : Window
    {

        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        byte m_currCMD = 0;
        PatternValueEx orgPattern = new PatternValueEx();
        //byte fwVersionFlag = 0;

        bool bReadFontValue = false;

        public static MarkVINInformEx currMarkInfo = new MarkVINInformEx();
        Line charline = new Line();
        Ellipse Dotline = new Ellipse();
        bool bshowAlready = false;


        DispatcherTimer statusTimer = new DispatcherTimer();

        enum LOGTYPE
        {
            LOG_START = 0,
            LOG_END = 1,
            LOG_SUCCESS = 2,
            LOG_FAILURE = 3,
            LOG_NORMAL = 4,
        }

        public ManualMarkWindow3()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            ////DisplayValue();
            //string value = "";

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

            //ShowColorMapSample();

            statusTimer.Tick += statusTimerHandler;
            statusTimer.Interval = TimeSpan.FromMilliseconds(1000);
            statusTimer.Start();

            //((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
            //((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
        }

        public ManualMarkWindow3(string pattern, string vin)
        {
            string value = "";
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

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

            //ShowColorMapSample();

            statusTimer.Tick += statusTimerHandler;
            statusTimer.Interval = TimeSpan.FromMilliseconds(1000);
            statusTimer.Start();

            //((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.LaserControllerStatusEventFunc += OnLaserControllerStatusChangedEventReceivedFunc;
            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
        }



        /// ///////////////////////////////////////////////////////////////////////////////////////////
        public async void OnLaserControllerStatusChangedEventReceivedFunc(object sender, LaserControllerStatusEvnetArgs e)
        {
            string className = "ManualMarkWindow3";
            string funcName = "OnLaserControllerStatusChangedEventReceivedFunc";
            System.Windows.Media.Brush brushesBorder = System.Windows.Media.Brushes.Black;
            LASERSTATUS Status = 0;
            UInt32 ists = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                //if ((e.datatype == 0) & (e.recvdata1 != GetTextBoxData(LaserErrorTxt)))
                //{
                //    ShowTextBoxData(LaserErrorTxt, e.recvdata1);
                //    //UInt32.TryParse(e.recvdata1, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ists);
                //    UInt32.TryParse(e.recvdata1, out ists);
                //    Status = (LASERSTATUS)ists;

                //    if ((Status & LASERSTATUS.StatusNormalOn) != LASERSTATUS.StatusNormalOn)
                //    {
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Status 1 Error(Must be 1) : {0:X}", Status), Thread.CurrentThread.ManagedThreadId);
                //        //Debug.WriteLine(string.Format("Status 1 Error(Must be 1) : {0:X}", Status));
                //        //if ((Status & LASERSTATUS.PulseModeCWMode) == 0) Debug.WriteLine("0 : " + LASERSTATUS.PulseModeCWMode.ToString());
                //        //if ((Status & LASERSTATUS.GateModeEnableDisable) == 0) Debug.WriteLine("0 : " + LASERSTATUS.GateModeEnableDisable.ToString());
                //        //if ((Status & LASERSTATUS.FrontPanelDisplayLockedUnlocked) == 0) Debug.WriteLine("0 : " + LASERSTATUS.FrontPanelDisplayLockedUnlocked.ToString());
                //        //if ((Status & LASERSTATUS.KeyswitchIsRemOnPosition) == 0) Debug.WriteLine("0 : " + LASERSTATUS.KeyswitchIsRemOnPosition.ToString());
                //        if ((Status & LASERSTATUS.WaveformPulseModeOnOff) == 0)
                //        {
                //            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "0 : " + LASERSTATUS.WaveformPulseModeOnOff.ToString() + " => Set 1", Thread.CurrentThread.ManagedThreadId);
                //            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.EnableWaveformPulseMode();
                //        }
                //        brushesBorder = (brushesBorder != Brushes.Black) ? Brushes.Black : Brushes.OrangeRed;
                //    }
                //    if ((Status & LASERSTATUS.StatusNormalOff) != 0)
                //    {
                //        //Debug.WriteLine(string.Format("Status 0 Error(Must be 0): {0:X}", Status));
                //        //if ((Status & LASERSTATUS.AnalogPowerControlEnableDisable) != 0) Debug.WriteLine("1 : " + LASERSTATUS.AnalogPowerControlEnableDisable.ToString());
                //        //if ((Status & LASERSTATUS.PowerSupplyOnOff) != 0) Debug.WriteLine("1 : " + LASERSTATUS.PowerSupplyOnOff.ToString());
                //        //if ((Status & LASERSTATUS.ModulationEnabledDisabled) != 0) Debug.WriteLine("1 : " + LASERSTATUS.ModulationEnabledDisabled.ToString());
                //        //if ((Status & LASERSTATUS.HWEmissionCtrlEnabledDisabled) != 0) Debug.WriteLine("1 : " + LASERSTATUS.HWEmissionCtrlEnabledDisabled.ToString());
                //        //if ((Status & LASERSTATUS.HWAimingBeamCtrlEnabledDisabled) != 0) Debug.WriteLine("1 : " + LASERSTATUS.HWAimingBeamCtrlEnabledDisabled.ToString());
                //        //if ((Status & LASERSTATUS.FiberInterlockActiveOK) != 0) Debug.WriteLine("1 : " + LASERSTATUS.FiberInterlockActiveOK.ToString());
                //        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Status 0 Error(Must be 0) : {0:X}", Status), Thread.CurrentThread.ManagedThreadId);
                //        brushesBorder = (brushesBorder != Brushes.Black) ? Brushes.Black : Brushes.LightBlue;
                //    }
                //    if ((Status & LASERSTATUS.StatusError) != 0)
                //    {
                //        //ErrorLaserSource = true;
                //        //ShowLabelData("ERROR", lblLaserStatus, Brushes.White, Brushes.Red);//, brushesBorder);
                //        //ShowLabelData(Status.ToString("X"), lblLastErrorValue, Brushes.Red, null, brushesBorder);
                //        ShowTextBoxData(LaserErrorTxt, Status.ToString("X8"));
                //    }
                //    else
                //    {
                //        //ErrorLaserSource = false;
                //        //ShowLabelData("NORMAL", lblLaserStatus, Brushes.Black, Brushes.Green);//, brushesBorder);
                //        //ShowLabelData(Status.ToString("X"), lblLastErrorValue, Brushes.Black, null, brushesBorder);
                //    }

                //    //if ((ists & (uint)LASERSTATUS.StatusError) != 0)
                //    //{
                //    //    ShowLabelData("ERROR", lblLaserStatus, System.Windows.Media.Brushes.White, System.Windows.Media.Brushes.Red);
                //    //}
                //    //else
                //    //{
                //    //    ShowLabelData("NORMAL", lblLaserStatus, System.Windows.Media.Brushes.Black, System.Windows.Media.Brushes.Green);
                //    //}

                //    //brushesEmission = Brushes.Red;
                //    //brushesEmission = Brushes.Black;
                //    if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                //        ShowRectangle(EmissionLamp, Brushes.Red);
                //    else
                //        ShowRectangle(EmissionLamp, Brushes.Black);

                //    //brushesAiming = Brushes.Red;
                //    //brushesAiming = Brushes.Black;
                //    if ((Status & LASERSTATUS.AimingBeamOnOff) != 0)
                //        ShowRectangle(AimingLamp, Brushes.Red);
                //    else
                //        ShowRectangle(AimingLamp, Brushes.Black);

                //    //ShowRectangle(brushesEmission, EmissionLamp);
                //    //ShowRectangle(brushesAiming, AimingLamp);

                //    //if ((Status & LASERSTATUS.StatusNormalOn) != LASERSTATUS.StatusNormalOn)
                //    //{
                //    //    brushesBorder = (brushesBorder != System.Windows.Media.Brushes.Black) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.OrangeRed;
                //    //}
                //    //if ((Status & LASERSTATUS.StatusNormalOff) != 0)
                //    //{
                //    //    brushesBorder = (brushesBorder != System.Windows.Media.Brushes.Black) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.LightBlue;
                //    //}


                //    //ControlWindow.EmissionLamp.Fill = brushesEmission;
                //    //ControlWindow.AimingLamp.Fill = brushesAiming;


                //    //brushesErrorForground = (ErrorLaserSource) ? Brushes.Red : Brushes.Black;
                //    //ControlWindow.LaserErrorTxt.Text = Status.ToString("X");
                //    //ControlWindow.LaserErrorTxt.BorderBrush = brushesBorder;
                //}


                //if (e.datatype == 1)
                //{
                //    if (e.recvdata1 != GetTextBoxData(AvgPowerTxt))
                //        ShowTextBoxData(AvgPowerTxt, e.recvdata1);

                //    if (e.recvdata2 != GetTextBoxData(PeakPowerTxt))
                //        ShowTextBoxData(PeakPowerTxt, e.recvdata2);
                //}

                //if ((e.datatype == 2) & (e.recvdata1 != GetTextBoxData(TemperatureTxt)))
                //    ShowTextBoxData(TemperatureTxt, e.recvdata1);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //return ex.HResult;
            }
        }


        private async void statusTimerHandler(object sender, EventArgs e)
        {
            string className = "ManualMarkWindow3";
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
            // TM SHIN
            const int Pallete_WIDTH = 35;
            const int Pallete_HEIGHT = 107;

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

        //private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //{
        //    //((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.LaserControllerStatusEventFunc -= OnLaserControllerStatusChangedEventReceivedFunc;
        //    ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc -= OnMarkControllerEventFunc;
        //    statusTimer.Stop();
        //}

        //private void Window_Closed(object sender, EventArgs e)
        //{

        //}

        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (this.WindowState == WindowState.Maximized)
        //    {
        //        ChangeSize(this.ActualWidth, this.ActualHeight);
        //    }
        //    this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
        //}

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            //DialogResult = true;
            Close();
        }

        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    orginalWidth = this.Width;
        //    originalHeight = this.Height;
        //    ChangeSize(e.NewSize.Width, e.NewSize.Height);
        //}





        private async void OnMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        {
            string className = "ManualMarkWindow3";
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
            //string tmpstring = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //Brush backbrushXY = null;
            //Brush backbrushZ = null;
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

                                if(m_currCMD == '@')
                                {
                                    if (this.CheckAccess())
                                    {
                                        if (currMarkInfo.senddata.CleanFireFlag == true && currMarkInfo.checkdata.TwoLineDisplay == true)
                                            ShowMarkingOneLine(chindex, ptindex - 1);//, Mode_File.Density232);
                                        ShowMarkingOneLine(chindex, ptindex);//, Density232);

                                    }
                                    //ShowMarkingOneLine(chindex, ptindex);//, Density232);
                                    else
                                    {
                                        this.Dispatcher.Invoke(new Action(delegate
                                        {
                                            if (currMarkInfo.senddata.CleanFireFlag == true && currMarkInfo.checkdata.TwoLineDisplay == true)
                                                ShowMarkingOneLine(chindex, ptindex - 1);//, Mode_File.Density232);
                                            ShowMarkingOneLine(chindex, ptindex);//, Density232);
                                            //ShowMarkingOneLine(chindex, ptindex);//, Density232);
                                        }));
                                    }
                                }

                                //tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
                                //ShowTextBoxData(txtCurrentPosX, tmpstring);
                                //tmpstring = string.Format("{0:0.00}", (double)iparam2 / 100.0);
                                //ShowTextBoxData(txtCurrentPosY, tmpstring);
                                //tmpstring = string.Format("{0:0.00}", (double)iparam3 / 100.0);
                                //ShowTextBoxData(txtCurrentPosZ, tmpstring);

                                //backbrushXY = ((currMarkInfo.currMarkData.pattern.positionValue.home3DPos.X == iparam1) && (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Y == iparam2)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                //ShowLabelColor(lblMotorHomeXY, backbrushXY, null);

                                //backbrushZ = (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Z == iparam3) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                //ShowLabelColor(lblMotorHomeXY, backbrushZ, null);
                                break;

                            case (byte)'1': // RUNNING
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

                                    //Brush backbrushXY1 = ((currMarkInfo.currMarkData.pattern.positionValue.home3DPos.X == iparam1) && (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Y == iparam2)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                    //ShowLabelColor(lblMotorHomeXY, backbrushXY1, null);

                                    //Brush backbrushZ1 = (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Z == iparam3) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                    //ShowLabelColor(lblMotorHomeXY, backbrushZ1, null);

                                    //Brush backAlaramX1 = ((iparam4 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, backAlaramX1, null);
                                    //Brush backAlaramY1 = ((iparam4 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, backAlaramY1, null);
                                    //Brush backAlaramZ1 = ((iparam4 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, backAlaramZ1, null);

                                    if ((iparam4 & 0x8000) != 0)
                                    {

                                    }
                                }
                                break;

                            case (byte)'8': // Action OK
                                //tmpstring = string.Format("{0:0.00}", (double)iparam1 / 100.0);
                                //ShowTextBoxData(txtCurrentPosX, tmpstring);
                                //tmpstring = string.Format("{0:0.00}", (double)iparam2 / 100.0);
                                //ShowTextBoxData(txtCurrentPosY, tmpstring);
                                //tmpstring = string.Format("{0:0.00}", (double)iparam3 / 100.0);
                                //ShowTextBoxData(txtCurrentPosZ, tmpstring);

                                //ShowLabelData(lblInputPort, iparam4.ToString("X4"));

                                //Brush backbrushXY8 = ((currMarkInfo.currMarkData.pattern.positionValue.home3DPos.X == iparam1) && (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Y == iparam2)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                //ShowLabelColor(lblMotorHomeXY, backbrushXY8, null);

                                //Brush backbrushZ8 = (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Z == iparam3) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                                //ShowLabelColor(lblMotorHomeXY, backbrushZ8, null);

                                //Brush backAlaramX8 = ((iparam4 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, backAlaramX8, null);
                                //Brush backAlaramY8 = ((iparam4 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, backAlaramY8, null);
                                //Brush backAlaramZ8 = ((iparam4 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, backAlaramZ8, null);

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
                                if ((currCMD == '@') && (currMarkInfo.currMarkData.pattern.laserValue.density == 1))
                                {
                                    //Brush backAlaramXA = ((iparam1 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, backAlaramXA, null);
                                    //Brush backAlaramYA = ((iparam1 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, backAlaramYA, null);
                                    //Brush backAlaramZA = ((iparam1 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, backAlaramZA, null);
                                    //if ((iparam1 & 0x8000) != 0)
                                    //{
                                    //    ;
                                    //}

                                    //if ((iparam1 & 0x07) != 0)
                                    //{

                                    //}

                                    //ShowLabelData(lblInputPort, iparam1.ToString("X4"));

                                    //{   // @, ACK, INPORT => INPORT Processing
                                    //    byte[] ackBytes = new byte[4];
                                    //    Array.Copy(buffer, 3, ackBytes, 0, 4);
                                    //    string ackstr = Encoding.Default.GetString(ackBytes);
                                    //    retval = int.Parse(ackstr, System.Globalization.NumberStyles.HexNumber);

                                    //    ControlWindow.Dispatcher.Invoke(new Action(delegate
                                    //    {
                                    //        ControlWindow.M4_CommStatus.Background = Brushes.WhiteSmoke;
                                    //        ControlWindow.X_AXIS_ALARM.Background = ((retval & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.Y_AXIS_ALARM.Background = ((retval & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.Z_AXIS_ALARM.Background = ((retval & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.TXT_JOG_RIGHT.Background = ((retval & 0x10) != 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.TXT_JOG_LEFT.Background = ((retval & 0x08) != 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.TXT_JOG_UP.Background = ((retval & 0x40) != 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.TXT_JOG_DOWN.Background = ((retval & 0x20) != 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.Btn_ScanLeft_Jog5.Background = ((retval & 0x100) != 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.Btn_ScanRight_Jog5.Background = ((retval & 0x80) != 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        ControlWindow.Btn_Dry_Run.Background = ((retval & 0x8000) == 0) ? Brushes.AntiqueWhite : Brushes.Red;
                                    //        if ((retval & 0x8000) != 0)
                                    //        {
                                    //            // Laser Alarm Error
                                    //            ControlWindow.txt_log.AppendText(DateTime.Now + " LASER Alarm" + Environment.NewLine);
                                    //            Mode_File.ErrorLaserSource = true;
                                    //        }
                                    //        else
                                    //        {
                                    //            Mode_File.ErrorLaserSource = false;
                                    //        }
                                    //        if ((retval & 0x07) != 0)
                                    //        {
                                    //            // Motor Alarm Error 
                                    //            ControlWindow.txt_log.AppendText(DateTime.Now + " Servo Motor Alarm" + Environment.NewLine);
                                    //        }

                                    //        ControlWindow.InputTxt.Text = retval.ToString("X4");
                                    //    }));

                                    //}

                                    //string m_font = "";

                                    //if ((Mode_File.SendDataIndex < Mode_File.SendDataCount) && !Mode_File.ErrorLaserSource) // 11/28
                                    //{
                                    //    m_font = (Mode_File.CleanFireFlag == false) ? Mode_File.SendData.ElementAt(Mode_File.SendDataIndex++) : Mode_File.SendClean.ElementAt(Mode_File.SendDataIndex++);

                                    //    i = 0; sendLength = m_font.Length;
                                    //    byte[] tmpc = Encoding.UTF8.GetBytes("@");
                                    //    leng = sendLength / 4; strLeng = leng.ToString("X2"); byLeng = Encoding.UTF8.GetBytes(strLeng);

                                    //    sendmsg[i++] = (byte)ASCII.SOH;
                                    //    sendmsg[i++] = byLeng[0];
                                    //    sendmsg[i++] = byLeng[1];
                                    //    sendmsg[i++] = tmpc[0];
                                    //    sendmsg[i++] = (byte)ASCII.STX;
                                    //    tmp = Encoding.UTF8.GetBytes(m_font); Array.Copy(tmp, 0, sendmsg, i, sendLength); i += sendLength;
                                    //    sendmsg[i++] = (byte)ASCII.ETX;
                                    //    bcc = GetBCC(sendmsg, 1, i); strLeng = bcc.ToString("X2"); byLeng = Encoding.UTF8.GetBytes(strLeng);

                                    //    sendmsg[i++] = byLeng[0];
                                    //    sendmsg[i++] = byLeng[1];
                                    //    sendmsg[i++] = (byte)ASCII.CR;

                                    //    retval = WritePort(sendmsg, 0, i);

                                    //    if (retval <= 0)
                                    //    {
                                    //        return retval;
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    S_TIME = DateTime.Now;

                                    //    Mode_File.EndOfSend = true;
                                    //}
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

                        //exitFlag = true;
                        break;
                    case (byte)'h':         //
                        switch (e.stscmd)
                        {
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
//                                ShowLog((byte)LOGTYPE.LOG_SUCCESS, "SCAN HOME", "MOVE TO SCAN HOME COMPLETE!!!!");
//                                m_currCMD = 0;
//                                break;

                            default:
                                break;
                        }
                        break;
                    default:
                        if (e.stscmd == (byte)ASCII.ACK)
                        {
                            //Brush backbrushXYD = ((currMarkInfo.currMarkData.pattern.positionValue.home3DPos.X == iparam1) && (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Y == iparam2)) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //ShowLabelColor(lblMotorHomeXY, backbrushXYD, null);

                            //Brush backbrushZD = (currMarkInfo.currMarkData.pattern.positionValue.home3DPos.Z == iparam3) ? Brushes.LightGreen : Brushes.AntiqueWhite;
                            //ShowLabelColor(lblMotorHomeXY, backbrushZD, null);

                            //Brush backAlaramXD = ((iparam4 & 0x01) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramX, backAlaramXD, null);
                            //Brush backAlaramYD = ((iparam4 & 0x02) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramY, backAlaramYD, null);
                            //Brush backAlaramZD = ((iparam4 & 0x04) == 0) ? Brushes.AntiqueWhite : Brushes.Red; ShowLabelColor(lblMotorAlramZ, backAlaramZD, null);

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


        private async void btnReadFont_Click(object sender, RoutedEventArgs e)
        {
            string className = "ManualMarkWindow3";
            string funcName = "btnReadFont_Click";
            //int retval = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string cmd = "READ FONT";
            string patternName = "";
            string value = "";
            byte bHeadType = 0;
            PatternValueEx pattern = new PatternValueEx();
            VinNoInfo vininfo = new VinNoInfo();
            string errorCode = "";
            string sCurrentFunc = "READ FONT";

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
                if (cbxPatternList.SelectedIndex < 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, "", "SELECT PATTERN");
                    return;
                }

                patternName = cbxPatternList.Text;

                if (txtVIN.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "VIN IS EMPTY. ENTER VIN");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN IS BLANK. ENTER VIN", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ClearMarkVINDisplay();
                currMarkInfo.Initialize();

                currMarkInfo.currMarkData.mesData.markvin = txtVIN.Text;
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                retval = ImageProcessManager.GetPatternValue(patternName, bHeadType, ref pattern);
                if(retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET PATTERN ERROR : " + retval.errorInfo.sErrorMessage);
                    return;
                }

                currMarkInfo.currMarkData.pattern = (PatternValueEx)pattern.Clone();

                vininfo.vinNo = currMarkInfo.currMarkData.mesData.markvin;
                vininfo.fontName = currMarkInfo.currMarkData.pattern.fontValue.fontName;
                vininfo.width = currMarkInfo.currMarkData.pattern.fontValue.width;
                vininfo.height = currMarkInfo.currMarkData.pattern.fontValue.height;
                vininfo.pitch = currMarkInfo.currMarkData.pattern.fontValue.pitch;
                vininfo.thickness = currMarkInfo.currMarkData.pattern.fontValue.thickness;

                retval = ImageProcessManager.GetFontDataEx(vininfo, bHeadType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref currMarkInfo.currMarkData.fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref errorCode);
                if (retval.execResult != 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "GET FONT ERROR" + retval.ToString());
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


        private async void btnAreaCheck_Click(object sender, RoutedEventArgs e)
        {
            string className = "ManualMarkWindow3";
            string funcName = "btnAreaCheck_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            PatternValueEx pattern = new PatternValueEx();

            string log = "";
            Stopwatch sw;
            CheckAreaData chkdata = new CheckAreaData();
            string patName = "";
            string cmd = "PLATE CHECK";
            string value = "";
            byte byHeadType = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                if (cbxPatternList.SelectedIndex < 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, "", "SELECT PATTERN");
                    return;
                }
                patName = cbxPatternList.Text;


                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out byHeadType);

                retval = ImageProcessManager.GetPatternValue(patName, byHeadType, ref pattern);
                if (retval.execResult != 0)
                {
                    log = "READ PATTERN ERROR : (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if(txtVIN.Text.Length <= 0)
                {
                    return;

                }
                currMarkInfo.checkdata.Clear();

                retval = ImageProcessManager.GetPatternValue(patName, 1, ref pattern);
                if (retval.execResult != 0)
                {
                    log = "READ FONT ERROR : (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                sw = Stopwatch.StartNew();

//#if LASER_YLR
                chkdata = await Range_Test("PLATE CHECK", txtVIN.Text, pattern);
                if (chkdata.execResult != 0)
                {
                    log = "RANGE TEST ERROR : (RESULT = " + chkdata.execResult.ToString() + ")";
                    //ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }
//#else
//                //currMarkInfo.checkdata.NormalDir.X = currMarkInfo.checkdata.NormalDir.Y = 0;
//                //currMarkInfo.checkdata.NormalDir.Z = 1;
//                chkdata = await GetMarkPosition("PLATE CHECK", currMarkInfo.currMarkData.pattern, 0, 0);
//                if (chkdata.execResult != 0)
//                {
//                    log = "RANGE TEST ERROR : (RESULT = " + chkdata.execResult.ToString() + ")";
//                    //ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, log);
//                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
//                    return;
//                }
//#endif
                //chkdata = await Range_Test(cmd, currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.pattern);
                //if (chkdata.execResult != 0)
                //{
                //    log = "RANGE TEST ERROR : (RESULT = " + chkdata.execResult.ToString() + ")";
                //    //ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, log);
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //    return;
                //}
                //else
                {
                    currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                }

                log = "MEASURE TIME : " + sw.ElapsedMilliseconds.ToString();
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);

                currMarkInfo.checkdata.bReady = true;

                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CHECK PLATE - SUCCESS", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
            }
        }

        //private void txtVIN_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    string className = "ManualMarkWindow3";
        //    string funcName = "txtVIN_TextChanged";

        //    try
        //    {
        //        if (lblVINLength != null)
        //            lblVINLength.Content = txtVIN.Text.Length.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private async void btnMarkRun_Click(object sender, RoutedEventArgs e)
        {
            string className = "ManualMarkWindow3";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMarkRun_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            LASERSTATUS Status = 0;
            //string vin = "";
            string value = "";
            //string fName = "";
            //byte bHeadType = 0;
            string log = "";
            PositionValue posValue = new PositionValue();
            HeadValue headValue = new HeadValue();
            //int repeatCount = 0;
            string patName = "";
            string cmd = "MARKING";
            byte useEmission = 0;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                ShowLog((byte)LOGTYPE.LOG_START, cmd, "");

                if (cbxPatternList.SelectedIndex < 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, "", "SELECT PATTERN");
                    return;
                }
                patName = cbxPatternList.Text;
                //retval = ImageProcessManager.GetPatternValue(patName, byheadType, ref pattern);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                if (retval.execResult != 0)
                {
                    log = "SendAirAsync ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (bReadFontValue == false)
                {
                    retval.execResult = ReadFontData(cmd, patName);
                    if (retval.execResult != 0)
                    {
                        log = "READ FONT ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
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
                    //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    //return;
                }
                //ShowRectangle(AimingLamp, Brushes.Black);

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
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                string[] st = retval.recvString.Split(':');
                if (st.Length < 2)
                {
                    log = "READ LASER STATUS. (STATUS STRING)";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }
                //2-1. Check emission satus
                Status = (LASERSTATUS)UInt32.Parse(st[1]);
                if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                {
                    //ShowLog("MARKING - STOP EMISSION");
                    //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //{
                    //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //}
                    retval = await EmissionONOFF(0);
                    if (retval.execResult != 0)
                    {
                        log = "STOP EMISSION. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                    //ShowRectangle(EmissionLamp, Brushes.Black);
                    //EmissionLamp.Fill = Brushes.Black;
                }

                //3. load waveform profile number
#if LASER_YLR_PULSEMODE
                // ShowLog("MARKING - SELECT PROFILE");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString());
                if (retval.execResult != 0)
                {
                    log = "SELECT PROFILE. (" + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                string[] prsel = retval.recvString.Split('[', ']');
                if (prsel.Length < 2)
                {
                    log = "SELECT PROFILE. (PROFILE STRING)";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                if (prsel[0] != "PRSEL: ")
                {
                    log = "SELECT PROFILE. (PROFILE SETTING RESPONSE ERROR)";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                string[] sel = prsel[1].Split(':');
                if (currMarkInfo.currMarkData.pattern.laserValue.waveformNum.ToString() != sel[0])
                {
                    log = "SELECT PROFILE. (PROFILE SETTING ERROR)";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                //4. get waveform mode
                //ShowLog("MARKING - CONFIG WAVEFORM MODE");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ConfigWaveformMode(0);
                if (retval.execResult != 0)
                {
                    log = "CONFIG WAVEFORM MODE. (" + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                string[] pcfg = retval.recvString.Split('[', ']');
                if (pcfg.Length < 2)
                {
                    log = "CONFIG WAVEFORM MODE. (PROFILE STRING)";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }
#else
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseRepetitionRate("1.00");
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseRepetitionRate("1.00");
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseRepetitionRate("1.00");
                }
                if (retval.execResult != 0)
                {
                    log = "MARKING ERROR - SET PULSE RATE. (" + retval.execResult.ToString() + ")";
                    //ShowLog(log);
                    retval.errorInfo.sErrorMessage = log;
                    retval.errorInfo.sErrorCode = "";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.markPower);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.markPower);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.markPower);
                }
                if (retval.execResult != 0)
                {
                    log = "MARKING ERROR - SELECT MARK POWER. (" + retval.execResult.ToString() + ")";
                    //ShowLog(log);
                    retval.errorInfo.sErrorMessage = log;
                    retval.errorInfo.sErrorCode = "";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                    return;
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.markWidth);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.markWidth);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.markWidth);
                }
                if (retval.execResult != 0)
                {
                    log = "MARKING ERROR - SELECT MARK WIDTH. (" + retval.execResult.ToString() + ")";
                    //ShowLog(log);
                    retval.errorInfo.sErrorMessage = log;
                    retval.errorInfo.sErrorCode = "";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                    return;
                }
#endif
                //5. Clear Dispaly
                //ClearMarkVINDisplay();

                //6. Start Text
                //retval = await Start_TEXT(cmd, currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.pattern);
                retval = await Start_TEXT2(cmd, currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern);
                if (retval.execResult != 0)
                {
                    log = "Start_TEXT3. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                posValue = (PositionValue)currMarkInfo.currMarkData.pattern.positionValue.Clone();
                headValue = (HeadValue)currMarkInfo.currMarkData.pattern.headValue.Clone();

                //7. Go to parking point (Check Point)
                //ShowLog("MARKING - LOAD SPEED");

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving, currMarkInfo.currMarkData.pattern);
                if (retval.execResult != 0)
                {
                    log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                //5. Set Font Data Buffer Flush
                //ShowLog("MARKING - FLUSH START");
                m_currCMD = (byte)'B';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.FontFlush();
                if (retval.execResult != 0)
                {
                    log = "FONT FLUSH. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
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
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
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

                //StPoint.Substring(4, 4 + 4);
                m_currCMD = (byte)'K';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoParking(posX, posY, posZ);
                if (retval.execResult != 0)
                {
                    log = "GO PARK. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.MarkMoving, currMarkInfo.currMarkData.pattern);
                if (retval.execResult != 0)
                {
                    log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
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
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                m_currCMD = (byte)'d';
                short dwelltime = 0;
                if (rbtMarkingPos1.IsChecked == true)
                    dwelltime = currMarkInfo.currMarkData.pattern.headValue.markDelayTime1;
                else
                    dwelltime = currMarkInfo.currMarkData.pattern.headValue.markDelayTime2;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.dwellTimeSet(dwelltime);
                if (retval.execResult != 0)
                {
                    log = "SET DENSITY. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("dwellTimeSet ERROR = {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                // Run Marking
                Stopwatch sw = Stopwatch.StartNew();

                Util.GetPrivateProfileValue("LASER", "EMISSION", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out useEmission);

                if (useEmission != 0)
                {
                    //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                    //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //{
                    //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                    //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                    //}
                    retval = await EmissionONOFF(1);
                    if (retval.execResult != 0)
                    {
                        log = "EMISSION ON. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                    //ShowRectangle(EmissionLamp, Brushes.Red);
                    //EmissionLamp.Fill = Brushes.Red;
                }

                //Marking Start
                //ShowLog("MARKING - START MARKING");

                Util.GetPrivateProfileValue("OPTION", "MARKINGLOGLEVEL", "0", ref value, Constants.PARAMS_INI_FILE);
                byte logLevel = 0;
                byte.TryParse(value, out logLevel);

                m_currCMD = (byte)'@';
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(StPoint);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(currMarkInfo, false, logLevel);
                if (retval.execResult != 0)
                {
                    log = "MARKING ERROR. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                //sw.Stop();
                log = "MARKING TIME : " + sw.ElapsedMilliseconds.ToString();
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                ShowLog((byte)LOGTYPE.LOG_END, cmd, log);

                bReadFontValue = false;
                currMarkInfo.checkdata.bReady = false;
                currMarkInfo.senddata.bReady = false;
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                //{
                //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                //}
                retval = await EmissionONOFF(0);
                if (retval.execResult != 0)
                {
                    log = "StopEmission. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                    return;
                }

                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                //if (retval.execResult != 0)
                //{
                //    log = "ReadDeviceStatus. (RESULT = " + retval.execResult.ToString() + ")";
                //    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                //    return;
                //}
                //else
                //{
                    //st = retval.recvString.Split(':');
                    //Status = (LASERSTATUS)UInt32.Parse(st[1]);

                //if ((bool)CleaningBox.IsChecked && !currMarkInfo.checkdata.ErrorDistanceSensor)
                //if ((bool)CleaningBox.IsChecked && !currMarkInfo.checkdata.ErrorDistanceSensor && (currMarkInfo.currMarkData.pattern.laserValue.combineFireClean == 0))
                if ((currMarkInfo.currMarkData.pattern.laserValue.useCleaning != 0) && !currMarkInfo.checkdata.ErrorDistanceSensor)
                {
                    //ShowLog("MARKING - START CLEANING");

                    //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //{
                    //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //}
                    retval = await EmissionONOFF(0);
                    if (retval.execResult != 0)
                    {
                        log = "StopEmission. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                    //ShowRectangle(EmissionLamp, Brushes.Black);

#if LASER_YLR_PULSEMODE
                    //Util.GetPrivateProfileValue("VINDATA", "PROFILECLEAN", "0", ref value, "Parameter.ini");                 // load waveform profile number
                    value = currMarkInfo.currMarkData.pattern.laserValue.waveformClean.ToString();
                    //ShowTextBoxData(txtCurrProfile, value);
                    //txtCurrProfile.Text = value;
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(value);
                    prsel = retval.recvString.Split('[', ']');
                    if (prsel[0] != "PRSEL: ")
                    {
                        log = "Profile setting Error2!. (PRSEL[0] = " + prsel[0] + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                    sel = prsel[1].Split(':');
                    if (value != sel[0])
                    {
                        log = "Profile setting Error!. (SEL[0] = " + sel[0] + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
#else
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseRepetitionRate("1.00");
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseRepetitionRate("1.00");
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseRepetitionRate("1.00");
                    }
                    if (retval.execResult != 0)
                    {
                        log = "MARKING ERROR - SET PULSE RATE. (" + retval.execResult.ToString() + ")";
                        //ShowLog(log);
                        retval.errorInfo.sErrorMessage = log;
                        retval.errorInfo.sErrorCode = "";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                        return;
                    }

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.cleanPower);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.cleanPower);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.cleanPower);
                    }
                    if (retval.execResult != 0)
                    {
                        log = "MARKING ERROR - SELECT CLEAN POWER. (" + retval.execResult.ToString() + ")";
                        //ShowLog(log);
                        retval.errorInfo.sErrorMessage = log;
                        retval.errorInfo.sErrorCode = "";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                        return;
                    }

                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.cleanWidth);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.cleanWidth);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.cleanWidth);
                    }
                    if (retval.execResult != 0)
                    {
                        log = "MARKING ERROR - SELECT CLEAN WIDTH. (" + retval.execResult.ToString() + ")";
                        //ShowLog(log);
                        retval.errorInfo.sErrorMessage = log;
                        retval.errorInfo.sErrorCode = "";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                        return;
                    }
#endif
                    //if ((bool)EmissionAuto.IsChecked)
                    //if(currMarkInfo.currMarkData.pattern.laserValue.)
                    //{
                    //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                    //    ShowRectangle(EmmisionLamp, Brushes.Red);
                    //    //EmmisionLamp.Fill = Brushes.Red;
                    //}


                    //Util.GetPrivateProfileValue("LASER", "EMISSION", "0", ref value, Constants.PARAMS_INI_FILE);
                    if (useEmission != 0)
                    {
                        //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                        //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        //{
                        //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                        //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission();
                        //}
                        retval = await EmissionONOFF(1);
                        if (retval.execResult != 0)
                        {
                            log = "MARKING ERROR - START EMISSION. (" + retval.execResult.ToString() + ")";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", log);
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                            return;
                        }
                    }


                    //currMarkInfo.senddata.SendDataIndex = 0;
                    currMarkInfo.senddata.CleanFireFlag = true;    // Clean sending
                                                                   //currMarkInfo.senddata.SendDataCount = (short)currMarkInfo.senddata.sendDataClean.Count;

                    //StPoint = currMarkInfo.senddata.sendDataClean.ElementAt(currMarkInfo.senddata.SendDataIndex++);
                    //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(StPoint);
                    Util.GetPrivateProfileValue("OPTION", "MARKINGLOGLEVEL", "0", ref value, Constants.PARAMS_INI_FILE);
                    logLevel = 0;
                    byte.TryParse(value, out logLevel);

                    m_currCMD = (byte)'@';
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.RunStart_S(currMarkInfo, true, logLevel);
                    if (retval.execResult != 0)
                    {
                        log = "RUN CLEANING. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }

                    log = "CLEAN TIME : " + sw.ElapsedMilliseconds.ToString();
                    ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                    ShowLog((byte)LOGTYPE.LOG_END, cmd, "");

                    //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //{
                    //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    //}
                    retval = await EmissionONOFF(0);
                    if (retval.execResult != 0)
                    {
                        log = "CLEANINGF ERROR RUN EMISSION OFF. (RESULT = " + retval.execResult.ToString() + ")";
                        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        return;
                    }
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                }
                if (retval.execResult == 0)
                {
                    st = retval.recvString.Split(':');
                    Status = (LASERSTATUS)UInt32.Parse(st[1]);

                    if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                    {
                        ////retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        //{
                        //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                        //}
                        retval = await EmissionONOFF(0);
                        //if (retval.execResult != 0)
                        //{
                        //    log = "RUN EMISSION OFF. (RESULT = " + retval.execResult.ToString() + ")";
                        //    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        //    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                        //    return;
                        //}
                        ////ShowRectangle(EmmisionLamp, Brushes.Black);
                        ////EmmisionLamp.Fill = Brushes.Black;
                    }
                }

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);
                retval = await SendMotorSpeed(cmd, (byte)motorSpeedType.FastMoving, currMarkInfo.currMarkData.pattern);
                if (retval.execResult != 0)
                {
                    log = "LOAD SPEED. (RESULT = " + retval.execResult.ToString() + ")";
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
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
                    return;
                }

                //Debug.WriteLine("Running Time : " + sw.Elapsed);
                sw.Stop();
                log = "TOTAL MARKING TIME : " + sw.ElapsedMilliseconds.ToString();
                ShowLog((byte)LOGTYPE.LOG_NORMAL, cmd, log);
                ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Running Time : " + sw.Elapsed.ToString(), Thread.CurrentThread.ManagedThreadId);

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(0);
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
            }
        }


        public async Task<CheckAreaData> Range_Test(string cmd, string vin, PatternValueEx pattern)//, byte errHeightFlag, byte errClineFlag)   // Plating  by TM SHIN
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
            double AZ = 0.0;
            string AreaPosition = "";

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


                //if (ckbSkipCheckPlane.IsChecked == true)
                //{
                //    retval.NormalDir.X = 0;
                //    retval.NormalDir.Y = 0;
                //    retval.NormalDir.Z = 1;

                //    retval.bReady = true;
                //    retval.execResult = 0;
                //    return retval;
                //}

                // Laser Beam ON
                respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
                if ((respArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (respArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
                    if ((respArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (respArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
                    }
                }
                if (respArg.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + respArg.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET BEAM CONTROLL ERROR - " + respArg.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = respArg.execResult;
                    return retval;
                }

                respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                if ((respArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (respArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                    if ((respArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (respArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                    }
                }
                if (respArg.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM ON ERROR : " + respArg.execResult.ToString();
                    //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM ON ERROR - " + respArg.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //retval.execResult = respArg.execResult;
                    //return retval;
                }
                //ShowRectangle(AimingLamp, Brushes.Red);

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


                respArg = await SendMotorSpeed(cmd, (byte)motorSpeedType.MeasureMoving, pattern);
                if (respArg.execResult != 0)
                {
                    //return retval;
                    log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + respArg.execResult.ToString();
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
                    ShowLabelData(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Red, null);
                    //ShowLabelColor(lblDispCenXCenY, Brushes.Red, null);

                    ShowLabelData(lblDispMinXMaxY, "");
                    ShowLabelData(lblDispMinXMinY, "");
                    ShowLabelData(lblDispMaxXMaxY, "");
                    ShowLabelData(lblDispMaxXMinY, "");
                    ShowLabelData(lblDispCenXMaxY, "");
                    ShowLabelData(lblDispCenXMinY, "");

                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "CENTER Z Range is OUT OF RANGE : " + HeightCT.ToString("F3"));
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
                    //if (errHeightFlag == 0)
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

                ShowLabelData(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Black, null);

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
                if ((respArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (respArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                    if ((respArg.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (respArg.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                }
                //ShowRectangle(AimingLamp, Brushes.Black);

                if (respArg.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //retval.execResult = respArg.execResult;
                    //return retval;
                }


                if (retval.ErrorDistanceSensor)
                {
                    Vector3D TmpPoint = new Vector3D();
                    planePoints.Clear();

                    HeightCT0 = HeightCT;
                    HeightCT = pattern.positionValue.checkDistanceHeight;

                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);


                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //if (errHeightFlag == 0)
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

                double dSlope = 0.0d;
                Util.GetPrivateProfileValue("OPTION", "SLOPE4MANUAL", "4.0", ref value, Constants.MARKING_INI_FILE);
                double.TryParse(value, out dSlope);

                if (PdiffDiff > dSlope)
                {
                    Debug.WriteLine(string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff));
                    log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, "RANGETEST", log, log);

                    // Error handling required!!
                    //if (errClineFlag == 0)
                    {
                        retval.execResult = -5;
                        return retval;
                    }
                }

                ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", "NORMALDIR = " + retval.NormalDir.X.ToString("F4") + ", " + retval.NormalDir.Y.ToString("F4") + ", " + retval.NormalDir.Z.ToString("F4") + ", PLANECENTERZ = " + retval.PlaneCenterZ.ToString("F4"));


                ShowLabelData(lblDispCenXCenY, retval.PlaneCenterZ.ToString("0.000;-0.000;0.000"), Brushes.DarkGreen, null);
                if (PdiffLU > 0)
                    ShowLabelData(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);
                else
                    ShowLabelData(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);

                if (PdiffLD > 0)
                    ShowLabelData(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);
                else
                    ShowLabelData(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);

                if (PdiffRU > 0)
                    ShowLabelData(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

                if (PdiffRD > 0)
                    ShowLabelData(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

                if (PdiffCU > 0)
                    ShowLabelData(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);

                if (PdiffCD > 0)
                    ShowLabelData(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), Brushes.Red, null);
                else
                    ShowLabelData(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), Brushes.LightSkyBlue, null);


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
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                log = "EXCEPTION. ERROR = " + ex.HResult.ToString() + ", MSG = " + ex.Message;
                retval.errorInfo.sErrorMessage = log;
                ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
            }

            return retval;
        } //End of Function



        //public async Task<CheckAreaData> Range_Test2(string cmd, string vin, PatternValueEx pattern)   // Plating  by TM SHIN
        //{
        //    string className = "SetControllerWindow2";
        //    string funcName = "Range_Test";

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

        //        //if (cbxPatternList.SelectedIndex >= 0)
        //        //    fName = cbxPatternList.SelectedItem.ToString();
        //        //else
        //        //{
        //        //    log = "SELECT PATTERN";
        //        //    retval.sErrorMessage = log;
        //        //    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, log, log);
        //        //    return retval;
        //        //}


        //        //if (ckbSkipCheckPlane.IsChecked == true)
        //        //{
        //        //    retval.NormalDir.X = 0;
        //        //    retval.NormalDir.Y = 0;
        //        //    retval.NormalDir.Z = 1;

        //        //    retval.bReady = true;
        //        //    retval.execResult = 0;
        //        //    return retval;
        //        //}

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
        //        //ShowRectangle(AimingLamp, Brushes.Red);

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
        //        double CX = (MaxX + MinX) / 2.0;
        //        double CY = (MaxY + MinY) / 2.0;


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


        //        //respArg = await SendMotorSpeed("RANGE_TEST", (byte)motorSpeedType.MeasureMoving);
        //        //if (respArg.execResult != 0)
        //        //{
        //        //    //return retval;
        //        //    log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + respArg.execResult.ToString();
        //        //    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "SET MOTOR SPEED(MEASURE) ERROR - " + respArg.execResult.ToString());
        //        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
        //        //    retval.execResult = respArg.execResult;
        //        //    return retval;
        //        //}

        //        // SET Motor Speed
        //        respArg = await SendMotorSpeed(cmd, (byte)motorSpeedType.MeasureMoving, pattern);
        //        if (respArg.execResult != 0)
        //        {
        //            log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, "SET MOTOR SPEED(MEASURE) ERROR - " + retval.execResult.ToString());
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

        //        if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
        //        {
        //            retval.ErrorDistanceSensor = true;
        //            ShowLabelData(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Red, null);
        //            //ShowLabelColor(lblDispCenXCenY, Brushes.Red, null);

        //            ShowLabelData(lblDispMinXMaxY, "");
        //            ShowLabelData(lblDispMinXMinY, "");
        //            ShowLabelData(lblDispMaxXMaxY, "");
        //            ShowLabelData(lblDispMaxXMinY, "");
        //            ShowLabelData(lblDispCenXMaxY, "");
        //            ShowLabelData(lblDispCenXMinY, "");

        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "CENTER Z Range is OUT OF RANGE");
        //            retval.execResult = -2;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
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
        //            if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
        //                gMinX = pattern.headValue.stepLength;
        //            gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
        //            if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
        //                gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
        //        }

        //        //ShowLabelData(lblDispCenXCenY, HeightCT.ToString("0.000"));
        //        //ShowLabelColor(lblDispCenXCenY, null, Brushes.Black);
        //        ShowLabelData(lblDispCenXCenY, HeightCT.ToString("0.000"), Brushes.Black, null);

        //        ShowLabelData(lblDispMinXMaxY, "");
        //        ShowLabelData(lblDispMinXMinY, "");
        //        ShowLabelData(lblDispMaxXMaxY, "");
        //        ShowLabelData(lblDispMaxXMinY, "");
        //        ShowLabelData(lblDispCenXMaxY, "");
        //        ShowLabelData(lblDispCenXMinY, "");

        //        tCX = (short)((gMaxX + gMinX) / 2.0 + 0.5);
        //        tCY = (short)((gMaxY + gMinY) / 2.0 + 0.5);

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
        //            ShowLabelData(lblValue[i], HeightVal[i].ToString("0.000"));
        //            HeightCT = HeightVal[i];    // TM SHIN
        //        }

        //        // Laser Aiming Beam OFF
        //        respArg = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
        //        //ShowRectangle(AimingLamp, Brushes.Black);

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


        //            retval.execResult = -4;
        //            log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "BEAM OFF ERROR - " + retval.execResult.ToString());
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

        //            return retval;
        //            // ?????
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

        //        if (PdiffDiff > 1.0)
        //        {
        //            Debug.WriteLine(string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff));
        //            log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
        //            ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", log, log);
        //            retval.execResult = -5;

        //            // Error handling required!!
        //            return retval;
        //        }

        //        ShowLog((byte)LOGTYPE.LOG_NORMAL, "RANGETEST", "NORMALDIR = " + retval.NormalDir.X.ToString("F4") + ", " + retval.NormalDir.Y.ToString("F4") + ", " + retval.NormalDir.Z.ToString("F4") + ", PLANECENTERZ = " + retval.PlaneCenterZ.ToString("F4"));


        //        ShowLabelData(lblDispCenXCenY, retval.PlaneCenterZ.ToString("0.000;-0.000;0.000"), null, Brushes.DarkGreen);
        //        if (PdiffLU > 0)
        //            ShowLabelData(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.LightSkyBlue);
        //        else
        //            ShowLabelData(lblDispMinXMaxY, PdiffLU.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.Red);

        //        if (PdiffLD > 0)
        //            ShowLabelData(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.LightSkyBlue);
        //        else
        //            ShowLabelData(lblDispMinXMinY, PdiffLD.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.Red);

        //        if (PdiffRU > 0)
        //            ShowLabelData(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.Red);
        //        else
        //            ShowLabelData(lblDispMaxXMaxY, PdiffRU.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.LightSkyBlue);

        //        if (PdiffRD > 0)
        //            ShowLabelData(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.Red);
        //        else
        //            ShowLabelData(lblDispMaxXMinY, PdiffRD.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.LightSkyBlue);

        //        if (PdiffCU > 0)
        //            ShowLabelData(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.Red);
        //        else
        //            ShowLabelData(lblDispCenXMaxY, PdiffCU.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.LightSkyBlue);

        //        if (PdiffCD > 0)
        //            ShowLabelData(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.Red);
        //        else
        //            ShowLabelData(lblDispCenXMinY, PdiffCD.ToString("+ 0.000;- 0.000;0.000"), null, Brushes.LightSkyBlue);

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
        //    //    SP1.X = CP - SP1;
        //} //End of Function


        /// <summary>
        /// //////////////////////////////////////////////////////////////////////////////////////////////
        public async Task<distanceSensorData> GetMeasureLength(Vector3D vp3, int pos, byte count)
        {
            string className = "ManualMarkWindow3";
            string funcName = "GetMeasureLength";

            distanceSensorData sensorData = new distanceSensorData();
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                m_currCMD = (byte)'M';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GoPoint((int)(vp3.X + 0.5), (int)(vp3.Y + 0.5), (int)(vp3.Z + 0.5), pos);
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

        private async Task<ITNTResponseArgs> SendMotorSpeed(string cmd, byte speedtype, PatternValueEx pattern)
        {
            string className = "ManualMarkWindow3";
            string funcName = "SendMotorSpeed";

            MotorSpeed speed = new MotorSpeed();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte sendCommand = (byte)'L';

            try
            {
                switch (speedtype)
                {
                    case (byte)motorSpeedType.HomeMoving:
                        speed.initSpeed   = pattern.speedValue.initSpeed4Home;
                        speed.targetSpeed = pattern.speedValue.targetSpeed4Home;
                        speed.accelSpeed  = pattern.speedValue.accelSpeed4Home;
                        speed.decelSpeed  = pattern.speedValue.decelSpeed4Home;

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
                        speed.initSpeed = pattern.speedValue.initSpeed4Fast;
                        speed.targetSpeed = pattern.speedValue.targetSpeed4Fast;
                        speed.accelSpeed = pattern.speedValue.accelSpeed4Fast;
                        speed.decelSpeed = pattern.speedValue.decelSpeed4Fast;

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 100;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 1200;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 2000;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 2000;
                        break;

                    case (byte)motorSpeedType.MarkMoving:
                        if(pattern.laserValue.density != 1)
                        {
                            speed.initSpeed = pattern.speedValue.initSpeed4MarkV;
                            speed.targetSpeed = pattern.speedValue.targetSpeed4MarkV;
                            speed.accelSpeed = pattern.speedValue.accelSpeed4MarkV;
                            speed.decelSpeed = pattern.speedValue.decelSpeed4MarkV;
                        }
                        else
                        {
                            speed.initSpeed = pattern.speedValue.initSpeed4MarkR;
                            speed.targetSpeed = pattern.speedValue.targetSpeed4MarkR;
                            speed.accelSpeed = pattern.speedValue.accelSpeed4MarkR;
                            speed.decelSpeed = pattern.speedValue.decelSpeed4MarkR;
                        }

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 70;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 100;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 5000;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 5000;
                        break;

                    case (byte)motorSpeedType.MeasureMoving:
                        speed.initSpeed = pattern.speedValue.initSpeed4Measure;
                        speed.targetSpeed = pattern.speedValue.targetSpeed4Measure;
                        speed.accelSpeed = pattern.speedValue.accelSpeed4Measure;
                        speed.decelSpeed = pattern.speedValue.decelSpeed4Measure;

                        if (speed.initSpeed <= 0)
                            speed.initSpeed = 110;
                        if (speed.targetSpeed <= 0)
                            speed.targetSpeed = 1400;
                        if (speed.accelSpeed <= 0)
                            speed.accelSpeed = 2000;
                        if (speed.decelSpeed <= 0)
                            speed.decelSpeed = 2000;
                        break;

                    case (byte)motorSpeedType.ScanMoving:
                        speed.initSpeed = pattern.scanValue.initSpeed4Scan;
                        speed.targetSpeed = pattern.scanValue.targetSpeed4Scan;
                        speed.accelSpeed = pattern.scanValue.accelSpeed4Scan;
                        speed.decelSpeed = pattern.scanValue.decelSpeed4Scan;

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
                        speed.initSpeed = pattern.scanValue.initSpeed4ScanFree;
                        speed.targetSpeed = pattern.scanValue.targetSpeed4ScanFree;
                        speed.accelSpeed = pattern.scanValue.accelSpeed4ScanFree;
                        speed.decelSpeed = pattern.scanValue.decelSpeed4ScanFree;

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
                        speed.initSpeed = pattern.speedValue.initSpeed4Fast;
                        speed.targetSpeed = pattern.speedValue.targetSpeed4Fast;
                        speed.accelSpeed = pattern.speedValue.accelSpeed4Fast;
                        speed.decelSpeed = pattern.speedValue.decelSpeed4Fast;

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


        private async Task ShowCurrentMarkingInformation(string vin, PatternValueEx pattern, List<List<FontDataClass>> fontdata, double fontSizeX, double fontSizeY, double shiftVal, byte showFlag)
        {
            string className = "ManualMarkWindow3";
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


        private async Task showRecogCharacters2(string vin, List<List<FontDataClass>> fontdata, PatternValueEx pattern, double fontSizeX, double fontSizeY, double shiftVal, Color fore, Color back)
        {
            string className = "ManualMarkWindow3";
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

        private async Task<int> ShowOneVinNoCharacter(List<FontDataClass> font, VinNoInfo vin, double Density, double fontSizeX, double fontSizeY, double shiftVal, Canvas showcanvas, Brush brush, Brush background, byte clearFlag, int interval = 0)
        {
            string className = "ManualMarkWindow3";
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

        public static int GetVinCharacterFontDot(string vin, List<List<FontDataClass>> fontdata, double fontsizeX, double fontsizeY, double shiftVal, string fontName)
        {
            string className = "ManualMarkWindow3";
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

        public int ReadFontData(string cmd, string patternName)
        {
            string className = "ManualMarkWindow3";
            string funcName = "ReadFontData";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string value = "";
            byte bHeadType = 0;
            VinNoInfo vininfo = new VinNoInfo();
            string log = "";
            string errorCode = "";
            PatternValueEx pattern = new PatternValueEx();

            try
            {
                if (txtVIN.Text.Length <= 0)
                {
                    ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "VIN IS BLANK. ENTER VIN");
                    retval.execResult = -1;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN IS BLANK. ENTER VIN", Thread.CurrentThread.ManagedThreadId);
                    return retval.execResult;
                }

                ClearMarkVINDisplay();
                currMarkInfo.Initialize();

                currMarkInfo.currMarkData.mesData.markvin = txtVIN.Text;
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                retval = ImageProcessManager.GetPatternValue(patternName, bHeadType, ref pattern);

                currMarkInfo.currMarkData.pattern = (PatternValueEx)pattern.Clone();


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
                    chkdata = await Range_Test(cmd, vin, pattern);//, 0, 0);
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


        private void ShowLabelData(Label label, string data, Brush fore=null, Brush back = null)
        {
            string className = "ManualMarkWindow3";
            string funcName = "ShowLabelData";

            try
            {
                if (label == null)
                    return;
                if (label.CheckAccess())
                {
                    label.Content = data;
                    if(back != null)
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

        public void ShowMarkingOneLine(int xcharIndex, int fontIndex)
        {
            string className = "ManualMarkWindow3";
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
                                    //Dotline = new Ellipse
                                    //{
                                    //    Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen,
                                    //    if (currMarkInfo.senddata.CleanFireFlag == false)
                                    //    Stroke = Brushes.Red;
                                    //    StrokeThickness = CharThick,
                                    //    Height = (double)Dotsize,
                                    //    Width = (double)Dotsize,
                                    //    Fill = Brushes.Red,
                                    //    Margin = new Thickness(left, right, 0, 0)
                                    //};

                                    Dotline = new Ellipse();
                                    Dotline.Stroke = (currMarkInfo.senddata.CleanFireFlag == false) ? Brushes.Red : Brushes.LightGreen;
                                    Dotline.StrokeThickness = CharThick;
                                    Dotline.Height = (double)Dotsize;
                                    Dotline.Width = (double)Dotsize;
                                    Dotline.Fill = Brushes.Red;
                                    Dotline.Margin = new Thickness(left, right, 0, 0);

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

        //private void ShowLog(byte flag, string cmd, string log)
        //{
        //    string className = "ManualMarkWindow3";
        //    string funcName = "ShowLog";
        //    string trace = "";
        //    DateTime dt = DateTime.Now;

        //    try
        //    {
        //        trace = dt.ToString("yyyy-MM-dd HH:mm:ss    ");
        //        trace += "[" + cmd + "] ";
        //        switch (flag)
        //        {
        //            case 0:                 // START
        //                trace += "START";
        //                break;
        //            case 1:                 // END
        //                trace += "SUCCESS";
        //                break;
        //            case 2:                 // ERROR
        //                trace += "ERROR : " + log;
        //                break;
        //            //case 3:
        //            //    break;
        //            //case 4:
        //            //    break;
        //            default:
        //                trace += log;
        //                break;
        //        }

        //        if (lsbResult.CheckAccess())
        //        {
        //            lsbResult.Items.Add(trace);
        //            if (lsbResult.Items.Count > 0)
        //            {
        //                lsbResult.SelectedIndex = lsbResult.Items.Count;
        //                lsbResult.ScrollIntoView(lsbResult.SelectedItem);
        //            }
        //        }
        //        else
        //        {
        //            lsbResult.Dispatcher.Invoke(new Action(delegate
        //            {
        //                lsbResult.Items.Add(trace);
        //                if (lsbResult.Items.Count > 0)
        //                {
        //                    lsbResult.SelectedIndex = lsbResult.Items.Count;
        //                    lsbResult.ScrollIntoView(lsbResult.SelectedItem);
        //                }
        //            }));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //ShowLog((byte)LOGTYPE.LOG_SUCCESS, cmd, "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private void ShowLog(byte flag, string cmd, string logmsg="", string error="")
        {
            string className = "ManualMarkWindow3";
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
                    case (byte)LOGTYPE.LOG_END:                 // START
                        trace += "END";
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, trace, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                //ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
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
                areaData.NormalDir.X = areaData.NormalDir.Y = 0;
                areaData.NormalDir.Z = 1;


                ShiftCT = sensorData.sensorshift;
                HeightCT = sensorData.sensoroffset;

                if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over (Z axis unit 0 ~ 60mm) : " + HeightCT.ToString("0.0000"), Thread.CurrentThread.ManagedThreadId);
                    areaData.ErrorDistanceSensor = true;

                    ShowLabelData(lblDispCenXCenY, HeightCT.ToString("0.000"), System.Windows.Media.Brushes.Red);
                    ShowLabelData(lblDispMinXMaxY, "");
                    ShowLabelData(lblDispMinXMinY, "");
                    ShowLabelData(lblDispMaxXMaxY, "");
                    ShowLabelData(lblDispMaxXMinY, "");
                    ShowLabelData(lblDispCenXMaxY, "");
                    ShowLabelData(lblDispCenXMinY, "");

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

                    ShowLabelData(lblDispCenXCenY, HeightCT.ToString("0.000;-0.000;0.000"), Brushes.Black);
                    ShowLabelData(lblDispCenXMaxY, ShiftCT.ToString("+ 0.000;- 0.000;0.000"));//, (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    ShowLabelData(lblDispCenXMinY, sensorData.rawdistance.ToString("+ 0.000;- 0.000;0.000"));//, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);
                    ShowLabelData(lblDispMinXMaxY, "");
                    ShowLabelData(lblDispMinXMinY, "");
                    ShowLabelData(lblDispMaxXMaxY, "");
                    ShowLabelData(lblDispMaxXMinY, "");

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

        private void txtVIN_TextChanged(object sender, TextChangedEventArgs e)
        {
            string className = "ManualMarkWindow3";
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            orginalWidth = this.Width;
            originalHeight = this.Height;
            ChangeSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                ChangeSize(this.ActualWidth, this.ActualHeight);
            }
            this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.LaserControllerStatusEventFunc -= OnLaserControllerStatusChangedEventReceivedFunc;
            statusTimer.Stop();
            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc -= OnMarkControllerEventFunc;
        }

        //private void txtVIN_TextChanged_1(object sender, TextChangedEventArgs e)
        //{

        //}


        //private async void btnReadFont_Click(object sender, RoutedEventArgs e)
        //{
        //    string className = "ManualMarkWindow3";
        //    string funcName = "btnReadFont_Click";
        //    int retval = 0;
        //    string cmd = "READ FONT";
        //    string patternName = "";

        //    try
        //    {
        //        ShowLog((byte)LOGTYPE.LOG_START, cmd, "");
        //        if (cbxPatternList.SelectedIndex < 0)
        //            patternName = "Pattern_DEFAULT";
        //        else
        //            patternName = cbxPatternList.Text;

        //        retval = ReadFontData(cmd, patternName);
        //        if (retval != 0)
        //        {
        //            ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "GET FONT ERROR");
        //            return;
        //        }
        //        await ShowCurrentMarkingInformation(currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2);
        //        GetVinCharacterFontDot(currMarkInfo.currMarkData.mesData.vin, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, currMarkInfo.currMarkData.pattern.fontValue.fontName);
        //        bReadFontValue = true;
        //        ShowLog((byte)LOGTYPE.LOG_END, cmd, "");
        //        //GetVinCharacterFontDot(vin, fName, pattern.fontValue.fontName);
        //    }
        //    catch (Exception ex)
        //    {
        //        ShowLog((byte)LOGTYPE.LOG_FAILURE, cmd, "", "EXCEPTION CODE = " + ex.HResult.ToString() + ", MSG = " + ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }

        //}

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

                execfunc = (emission != 0) ? ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StartEmission : ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission;

                if (value != "0")
                {
                    //sDevice = "PLC (SetEmissionOnOff) ERROR = ";
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SetEmissionOnOff(emission);
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
                        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
                            if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            {
                                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus();
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

        private async Task<ITNTResponseArgs> EmissionONOFF(byte emission)
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
                    retval = await EmissionONOFFDelegate(emission);
                }
                else
                {
                    retval = await Dispatcher.Invoke(new Func<Task<ITNTResponseArgs>>(async delegate
                    {
                        ITNTResponseArgs ret = new ITNTResponseArgs();
                        ret = await EmissionONOFFDelegate(emission);
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

    }
}
