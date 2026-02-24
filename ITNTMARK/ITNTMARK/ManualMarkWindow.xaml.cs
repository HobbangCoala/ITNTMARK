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
using ITNTCOMMON;
using ITNTUTIL;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using System.Threading;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// ManualMarkWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManualMarkWindow : Window
    {
        //MarkVINInform2 currMarkInfo = new MarkVINInform2();
        MarkVINInformEx currMarkInfo = new MarkVINInformEx();
        private string patternName = "";
        private bool doingCommand = false;
        short stepLength_u = 0;
        short stepLength = 0;
        byte m_currCMD = 0;
        Line charline = new Line();
        //Point currentPoint = new Point();
        Color lineColor;

        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        public ManualMarkWindow()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);


            List<string> fnames = new List<string>();
            string fontfile = "Parameter\\FONT.ini";
            string value = "";
            int i = 0;
            Util.GetPrivateProfileValue("FONTLIST", "NAME", "OCR|11X16|5X7|HMC5", ref value, fontfile);
            string[] fonts = value.Split('|');
            if (fonts.Length <= 0)
            {
                CB_font.Items.Add("OCR");
                CB_font.Items.Add("11X16");
                CB_font.Items.Add("5X7");
                CB_font.Items.Add("HMC5");
            }
            else
            {
                for (i = 0; i < fonts.Length; i++)
                    CB_font.Items.Add(fonts[i]);
                CB_font.SelectedIndex = 0;
            }

            Util.GetPrivateProfileValue("CONFIG", "STEP_LENGTH", "100", ref value, Constants.SCANNER_INI_FILE);
            if (value.Length <= 0)
                stepLength_u = 100;
            else
                short.TryParse(value, out stepLength_u);
            if (stepLength_u <= 0)
                stepLength_u = 100;

            Util.GetPrivateProfileValue("MARK", "STEP_LENGTH", "50", ref value, Constants.MARKING_INI_FILE);
            if (value.Length <= 0)
                stepLength = 50;
            else
                short.TryParse(value, out stepLength);
            if (stepLength <= 0)
                stepLength = 50;

            PatternValueEx pat = new PatternValueEx();
            string patName = "";
            List<string> names = new List<string>();
            string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
            names = DirFileSearch(patternfile, "*.ini").Result;
            for (i = 0; i < names.Count; i++)
                cbxPatternList.Items.Add(names[i]);
            if (names.Count > 0)
            {
                //DisplayValue(names[0]);
                cbxPatternList.SelectedIndex = 0;
                patName = cbxPatternList.SelectedItem.ToString();
                //LoadPatternData(patName, ref pat);
                //DisplayPatternData(pat);
            }
            else
            {
                ShowDefaultValue();
            }

            Util.GetPrivateProfileValue("COLOR", "CHARACTERLINE", "#FF5072DD", ref value, Constants.PARAMS_INI_FILE);
            if (value.Length > 8)
                lineColor = (Color)ColorConverter.ConvertFromString(value);
            else
                lineColor = (Color)ColorConverter.ConvertFromString("#FF5072DD");

            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
            //((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 1;
        }

        public ManualMarkWindow(string patternName, string vin)
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            List<string> fnames = new List<string>();
            string fontfile = "Parameter\\FONT.ini";
            string value = "";
            int i = 0;
            Util.GetPrivateProfileValue("FONTLIST", "NAME", "11X16|5X7|OCR|HMC5", ref value, fontfile);
            string[] fonts = value.Split('|');
            if (fonts.Length <= 0)
            {
                CB_font.Items.Add("11X16");
                CB_font.Items.Add("5X7");
                CB_font.Items.Add("OCR");
                CB_font.Items.Add("HMC5");
            }
            else
            {
                for (i = 0; i < fonts.Length; i++)
                    CB_font.Items.Add(fonts[i]);
                CB_font.SelectedIndex = 0;
            }

            Util.GetPrivateProfileValue("CONFIG", "STEP_LENGTH", "100", ref value, Constants.SCANNER_INI_FILE);
            if (value.Length <= 0)
                stepLength_u = 100;
            else
                short.TryParse(value, out stepLength_u);
            if (stepLength_u <= 0)
                stepLength_u = 100;

            Util.GetPrivateProfileValue("MARK", "STEP_LENGTH", "50", ref value, Constants.MARKING_INI_FILE);
            if (value.Length <= 0)
                stepLength = 50;
            else
                short.TryParse(value, out stepLength);
            if (stepLength <= 0)
                stepLength = 50;

            string selPattern = "";
            PatternValueEx pat = new PatternValueEx();
            List<string> names = new List<string>();
            string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
            names = DirFileSearch(patternfile, "*.ini").Result;
            for (i = 0; i < names.Count; i++)
                cbxPatternList.Items.Add(names[i]);

            if(cbxPatternList.Items.Count > 0)
            {
                if(names.Contains(patternName))
                {
                    cbxPatternList.SelectedItem = patternName;
                    selPattern = patternName;
                }
                else
                {
                    cbxPatternList.SelectedIndex = 0;
                    selPattern = cbxPatternList.SelectionBoxItem.ToString();
                }
                //LoadPatternData(selPattern, ref pat);
                //if (CB_font.Items.Count > 0)
                //{
                //    if (CB_font.Items.Contains(pat.fontName))
                //    {
                //        CB_font.SelectedItem = pat.fontName;
                //    }
                //    else
                //    {
                //        CB_font.SelectedIndex = 0;
                //        pat.fontName = CB_font.SelectedItem.ToString();
                //    }
                //}
                //DisplayPatternData(pat);
            }
            else
            {
                ShowDefaultValue();
            }

            txtVINNumber.Text = vin;
            Util.GetPrivateProfileValue("COLOR", "CHARACTERLINE", "#FF5072DD", ref value, Constants.PARAMS_INI_FILE);
            if (value.Length > 8)
                lineColor = (Color)ColorConverter.ConvertFromString(value);
            else
                lineColor = (Color)ColorConverter.ConvertFromString("#FF5072DD");

            //((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 1;
            ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
        }

        private void ShowDefaultValue()
        {
            txtStartX.Text = "25";
            txtStartY.Text = "20";
            txtHeight.Text = "7";
            txtWidth.Text = "4";
            txtPitch.Text = "6";
            txtAngle.Text = "0";
            txtStrike.Text = "1";
        }

        public void SetPatternName(string patternName)
        {
            this.patternName = patternName;
        }

        private async Task<List<string>> DirFileSearch(string selpath, string file)
        {
            string className = "ManualMarkWindow";
            string funcName = "DirFileSearch";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            List<string> names = new List<string>();
            try
            {
                string fileFullName = "";
                VinNoInfo vin = new VinNoInfo();

                string[] dirs = Directory.GetDirectories(selpath);
                string[] files = Directory.GetFiles(selpath, $"*{file}");
                foreach (string fileName in files)
                {
                    fileFullName = fileName.Replace(".ini", "");
                    fileFullName = fileFullName.Replace(selpath, "");
                    fileFullName = fileFullName.Replace("\\", "");
                    names.Add(fileFullName);
                }
                if (dirs.Length > 0)
                {
                    foreach (string dir in dirs)
                    {
                        await DirFileSearch(dir, file);
                    }
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return names;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return names;
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "ShowTextBoxData", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        //public async Task<ITNTResponseArgs> SendFontData(string vin, string patternName)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    double stepLength;
        //    string value = "";
        //    Point SP = new Point();
        //    double Step_W;
        //    double Step_H;
        //    string SetSpeed = "";
        //    string SetSol_OnOff = "";
        //    string Strikes = "";
        //    short i;
        //    short j;
        //    int idx = 0;
        //    string sendstring = "";
        //    byte[] sbuff;
        //    Pattern pattern = new Pattern();
        //    double fontSizeX = 0.0d;
        //    double fontSizeY = 0.0d;
        //    Stopwatch sw = new Stopwatch();

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", "START - " + vin);
        //        sw.Start();
        //        Util.GetPrivateProfileValue("MARK", "STEP_LENGTH", "50", ref value, Constants.MARKING_INI_FILE);
        //        double.TryParse(value, out stepLength);

        //        ImageProcessManager.GetPatternData(patternName, ref pattern);

        //        SetSpeed = pattern.initSpeed4Load.ToString("X4") + pattern.targetSpeed4Load.ToString("X4") + pattern.accelSpeed4Load.ToString("X4") + pattern.decelSpeed4Load.ToString("X4");// set speed
        //        SetSol_OnOff = pattern.solOnTime.ToString("X4") + pattern.solOffTime.ToString("X4"); // set Sol OnOff time
        //        Strikes = pattern.strikeCount.ToString("X4"); //set Strike 

        //        byte[] speed = Encoding.UTF8.GetBytes(SetSpeed);
        //        byte[] solOnOff = Encoding.UTF8.GetBytes(SetSpeed);
        //        byte[] strikeCount = Encoding.UTF8.GetBytes(SetSpeed);

        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.LoadSpeed(speed, speed.Length).ConfigureAwait(false); //send speed to MCU
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", string.Format("LoadSpeed ERROR = {0}", retval.execResult));
        //            return retval;
        //        }
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.SolOnOffTime(solOnOff, solOnOff.Length).ConfigureAwait(false);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", string.Format("SolOnOffTime ERROR = {0}", retval.execResult));
        //            return retval;
        //        }
        //        retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.StrikeNo(strikeCount, strikeCount.Length).ConfigureAwait(false); // Marking couter
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", string.Format("StrikeNo ERROR = {0}", retval.execResult));
        //            return retval;
        //        }

        //        switch (pattern.rotateAngle)
        //        {
        //            case 0:
        //                SP.X = pattern.startX * stepLength;
        //                break;

        //            case 180:
        //                SP.X = pattern.startX - pattern.pitch * (vin.Length - 1) + pattern.width * stepLength;
        //                break;
        //            default:
        //                SP.X = pattern.startX * stepLength;
        //                break;
        //        }
        //        SP.Y = pattern.startY * stepLength;

        //        Point TP = new Point();
        //        TP.X = pattern.startX * stepLength;
        //        TP.Y = pattern.startY * stepLength;

        //        List<Point> changedPoint = new List<Point>();
        //        FontDataClass movedData = new FontDataClass();

        //        ImageProcessManager.GetStartPointLinear(vin.Length, TP, SP, pattern.pitch * stepLength, pattern.rotateAngle, ref changedPoint);
        //        for (i = 0; i < changedPoint.Count; i++)
        //        {
        //            List<FontDataClass> fdatas = new List<FontDataClass>();
        //            string error = "";
        //            ImageProcessManager.GetOneCharacterFontData((char)vin[i], pattern.fontName, ref fdatas, out fontSizeX, out fontSizeY, out error);
        //            Step_W = pattern.width / (fontSizeX - 1) * stepLength;
        //            Step_H = pattern.height / (fontSizeY - 1) * stepLength;
        //            //fdatas = markData.fontData[i];
        //            //for (j = 0; j < fdatas.Count - 1; j++) //From big Arrays GET SMALL Arrays in Font
        //for (j = 0; j < fdatas.Count - 1; j++) //From big Arrays GET SMALL Arrays in Font
        //            {
        //                Point RP = new Point();
        //                FontDataClass fontValue = new FontDataClass();
        //                fontValue = fdatas[j];
        //                movedData.X = changedPoint[i].X + fontValue.X * Step_W;
        //                movedData.Y = changedPoint[i].Y + fontValue.Y * Step_H;

        //                if (pattern.fontName == "5X7")
        //                    movedData.Y = changedPoint[i].Y + (fontValue.Y - 3) * Step_H;
        //                else if (pattern.fontName == "11X16")
        //                    movedData.Y = changedPoint[i].Y + (fontValue.Y - 5) * Step_H;

        //                RP = ImageProcessManager.Rotate_Point(movedData.X, movedData.Y, changedPoint[i].X, changedPoint[i].Y, pattern.rotateAngle);

        //                short moveX = (short)(movedData.X + 0.5);
        //                short moveY = (short)(movedData.Y + 0.5);
        //                //sendstring = string.Format("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", i, j, movedData.X, movedData.Y, movedData.Flag);
        //                sendstring = i.ToString("X4") + j.ToString("X4") + moveX.ToString("X4") + moveY.ToString("X4") + movedData.Flag.ToString("X4");
        //                sbuff = Encoding.UTF8.GetBytes(sendstring);
        //                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.LoadFontData(sbuff, sbuff.Length).ConfigureAwait(false); // send Point to MCU
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", string.Format("MARK {0}, {1}", i, j));
        //                if (retval.execResult != 0)
        //                {
        //                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", string.Format("LoadFontData({0}, {1}) ERROR = {2}", i, j, retval.execResult));
        //                    return retval;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //    }
        //    sw.Stop();
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "SendFontData", string.Format("END - {0}", sw.ElapsedMilliseconds));
        //    return retval;
        //}

        private async Task<int> ShowOneVinNoCharacter(List<FontDataClass> font, VinNoInfo vin, double fontSizeX, double fontSizeY, Canvas showcanvas, Brush brush, Brush background, byte clearFlag, int interval = 0)
        {
            double canvaswidth = showcanvas.Width;
            double canvasheight = showcanvas.Height;
            double OriginX = 1.5d * Util.PXPERMM;
            double OriginY = 2.5d * Util.PXPERMM;
            double orgWidth = (vin.width) * Util.PXPERMM + OriginX * 2;
            double orgHeight = Util.PXPERMM * vin.height + OriginY * 2;

            /***********************************
            1 inch  25.4mm
            1 inch  72 pt
            1 inch  96 px        dpi
            1 mm    2.83465 pt
            1 mm    3.7795 px    dpi/ 25.4
            ***********************************/
            double CharHeight = vin.height * Util.PXPERMM;
            double CharWidth = vin.width * Util.PXPERMM;
            //double pitch_px = vin.pitch * Util.PXPERMM;
            double CharThick = vin.thickness * Util.PXPERMM * (canvaswidth / orgWidth + 0.2);
            //double CharThick = vin.thickness * Util.PXPERMM * canvaswidth / orgWidth;

            double heightthRation = canvasheight / orgHeight;
            double widthRation = canvaswidth / orgWidth;
            int index = 0;

            Line[] line = new Line[font.Count];

            try
            {
                showcanvas.UpdateLayout();
                showcanvas.Background = background;

                if (clearFlag != 0)
                    showcanvas.Children.Clear();

                for (int j = 0; j < font.Count; j++)
                {
                    //if(vin.vinNo[j] == (char)' ')
                    //{
                    //    showcanvas.Children.Clear();
                    //    return 0;
                    //}

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
                    else if (font[j].Flag == 2)
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
                }

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "ShowOneVinNoCharacter", string.Format("EXCEPTION1 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Debug.WriteLine("ShowOneVinNoCharacter excption {0} - {1}", ex.HResult, ex.Message);
                return ex.HResult;
            }
        }

        private async Task<int> ShowOneVinNoCharacterLaser(List<FontDataClass> font, VinNoInfo vin, double fontSizeX, double fontSizeY, Canvas showcanvas, Brush brush, Brush background, byte clearFlag, int interval = 0)
        {
            double canvaswidth = showcanvas.Width;
            double canvasheight = showcanvas.Height;

            double OriginX = 1.5d * Util.PXPERMM;
            double OriginY = 2.5d * Util.PXPERMM;

            double orgWidth = (vin.width) * Util.PXPERMM + OriginX * 2;
            double orgHeight = Util.PXPERMM * vin.height + OriginY * 2;

            /***********************************
            1 inch  25.4mm
            1 inch  72 pt
            1 inch  96 px        dpi
            1 mm    2.83465 pt
            1 mm    3.7795 px    dpi/ 25.4
            ***********************************/
            double CharHeight = vin.height * Util.PXPERMM;
            double CharWidth = vin.width * Util.PXPERMM;
            //double pitch_px = vin.pitch * Util.PXPERMM;
            double CharThick = vin.thickness * Util.PXPERMM * canvaswidth / orgWidth;
            //double CharThick = vin.thickness * Util.PXPERMM * canvaswidth / orgWidth;

            double heightthRation = canvasheight / orgHeight;
            double widthRation = canvaswidth / orgWidth;

            System.Windows.Shapes.Line[] line = new System.Windows.Shapes.Line[font.Count];
            int index = 0;

            try
            {
                showcanvas.UpdateLayout();
                showcanvas.Background = background;
                if (clearFlag != 0)
                    showcanvas.Children.Clear();

                for (int j = 0; j < font.Count; j++)
                {
                    if ((font[j].Flag == 1) || (font[j].Flag == 3))
                    {
                        line[index] = new System.Windows.Shapes.Line();
                        line[index].Stroke = brush;
                        line[index].StrokeThickness = CharThick;
                        line[index].StrokeStartLineCap = PenLineCap.Round;
                        line[index].StrokeEndLineCap = PenLineCap.Round;
                        line[index].StrokeLineJoin = PenLineJoin.Round;

                        line[index].X1 = (OriginX + (font[j].vector3d.X * CharWidth) / fontSizeX) * widthRation;
                        line[index].Y1 = (OriginY + (font[j].vector3d.Y * CharHeight) / fontSizeY) * heightthRation;
                    }
                    else if ((font[j].Flag == 2) || (font[j].Flag == 4))
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
                    else if (font[j].Flag == 5)
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
                }

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "ShowOneVinNoCharacterLaser", string.Format("EXCEPTION1 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }


        private async Task showRecogCharacters2(string vin, PatternValueEx pattern, Color fore, Color back)
        {
            string className = "ManualMarkWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "showRecogCharacters2";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //bool bShowLabel = false;
            double fontSizeX = 0.0d;
            double fontSizeY = 0.0d;
            double shiftValue = 0;
            //Dictionary<int, List<FontDataClass>> MyData = new Dictionary<int, List<FontDataClass>>();
            //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
            List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
            string ErrorCode = "";
            //int retval = 0;
            int charNum = 0;
            Canvas[] cvsshowChar = new Canvas[19];
            string ctrlName = "";
            VinNoInfo vininfo = new VinNoInfo();
            int count = vin.Length;
            //string colorstring = "#FFC8C8C8";
            //Color color;// = (Color)ColorConverter.ConvertFromString(colorstring);
            Brush brush;// = new Brush();
            //string fonttype = "";
            //string headType = "";
            byte headType = 0;
            string value = "";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                vininfo.vinNo = vin;
                vininfo.fontName = pattern.fontValue.fontName;
                vininfo.width = pattern.fontValue.width;
                vininfo.height = pattern.fontValue.height;
                vininfo.pitch = pattern.fontValue.pitch;
                vininfo.thickness = pattern.fontValue.thickness;
                //Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref headType, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out headType);

                //byte fontdirection = 0;
                //Util.GetPrivateProfileValue("OPTION", "FONTDIRECTION", "0", ref value, Constants.PARAMS_INI_FILE);
                //byte.TryParse(value, out fontdirection);

                retval = ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 1, ref fontData, ref fontSizeX, ref fontSizeY, ref shiftValue, ref ErrorCode);
                if (retval.execResult != 0)
                {

                }
                //retval = ImageProcessManager.GetFontData(vininfo, ref MyData, out fontSizeX, out fontSizeY, out ErrorCode);
                //if (retval != 0)
                //    return;
                //bShowLabel = true;

                //Util.GetPrivateProfileValue("USEFONT", "TYPE", "0", ref fonttype, "Parameter/FONT.ini");
                //Util.GetPrivateProfileValue("FONT", "CONTROLTYPE", "0", ref fonttype, Constants.PARAMS_INI_FILE);

                for (int i = 0; i < count; i++)
                {
                    charNum = (int)vin[i] - 1;
                    ctrlName = string.Format("cvsshowChar{0:D2}", i);
                    cvsshowChar[i] = (Canvas)FindName(ctrlName);
                    if (cvsshowChar[i] == null)
                        continue;

                    cvsshowChar[i].Background = new SolidColorBrush(back);
                    brush = new SolidColorBrush(fore);

                    if ((charNum >= 31) && (charNum <= 128))
                    {
                        List<FontDataClass> fdata = new List<FontDataClass>();
                        fdata = fontData[charNum];
                        if (CheckAccess())
                        {
                            if (headType == 0)
                                retval.execResult = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
                            else
                                retval.execResult = await ShowOneVinNoCharacterLaser(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
                        }
                        else
                        {
                            Dispatcher.Invoke(new Action(async delegate
                            {
                                if (headType == 0)
                                    retval.execResult = await ShowOneVinNoCharacter(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
                                else
                                    retval.execResult = await ShowOneVinNoCharacterLaser(fdata, vininfo, fontSizeX, fontSizeY, cvsshowChar[i], brush, cvsshowChar[i].Background, 1);
                            }));
                        }
                    }

                    //lblshowScore[0, i].Content = string.Format("{0:00.00}", confidence[i - 1] * 100);
                    //lblshowScore[1, i].Content = string.Format("{0:00.00}", quality[i - 1] * 100);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("showRecogCharacters excption {0} - {1}", ex.HResult, ex.Message);
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private async Task ShowCurrentMarkingInformation(string vin, PatternValueEx pattern, byte showFlag)
        {
            string className = "ManualMarkWindow";
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

                    showRecogCharacters2(vin, pattern, colorfore, colorback).Wait();
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

                        showRecogCharacters2(vin, pattern, colorfore, colorback).Wait();
                    }));
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSendData_Click(object sender, RoutedEventArgs e)
        {
            string className = "ManualMarkWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnSendData_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            PatternValueEx pat = new PatternValueEx();
            string name = "";
            string ErrorCode = "";
            string value = "";
            byte headType = 0;

            try
            {
                if (cbxPatternList.Items.Count <= 0)
                    name = "5X7";
                else
                    name = cbxPatternList.SelectedItem.ToString();

                ShowLog("SEND FONT DATA START");
                if (txtVINNumber.Text.Length <= 0)
                {
                    ShowLog("SEND FONT DATA - SEND ERROR (VIN EMPTY)");
                    return;
                }

                ShowLog("SEND FONT DATA - FLUSH START");
                m_currCMD = (byte)'B';
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.FontFlush();
                if (retval.execResult != 0)
                {
                    ShowLog("SEND FONT DATA - FLUSH ERROR (" + retval.execResult + ")");
                    return;
                }
                ShowLog("SEND FONT DATA - FLUSH SUCCESS");

                ShowLog("SEND FONT DATA - SEND START");

                LoadPatternData(name, ref pat);
                //GetPatternData(ref pat);

                currMarkInfo.currMarkData.mesData.markvin = txtVINNumber.Text.ToUpper();
                if (currMarkInfo.currMarkData.mesData.markvin.Length > 19)
                    currMarkInfo.currMarkData.mesData.markvin = currMarkInfo.currMarkData.mesData.markvin.Substring(0, 19);

                currMarkInfo.currMarkData.pattern = (PatternValueEx)pat.Clone();
                currMarkInfo.currMarkData.pattern.name = name;

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out headType);

                List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();
                ////PatternValueEx pattern = new PatternValueEx();
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


                retval = ImageProcessManager.GetFontDataEx(vininfo, headType, currMarkInfo.currMarkData.pattern.laserValue.density, 1, ref fontData, ref currMarkInfo.currMarkData.fontSizeX, ref currMarkInfo.currMarkData.fontSizeY, ref currMarkInfo.currMarkData.shiftValue, ref ErrorCode);
                if (retval.execResult != 0)
                {
                    ShowLog("SEND FONT DATA - GetFontDataEx ERROR (" + retval.execResult + ")");
                    return;
                }


                //for (int i = 0; i < currMarkInfo.currMarkData.mesData.vin.Length; i++)
                //{
                //    List<FontDataClass> FontDataClass = new List<FontDataClass>();
                //    ImageProcessManager.GetOneCharacterFontData(currMarkInfo.currMarkData.mesData.vin[i], currMarkInfo.currMarkData.pattern.fontValue.fontName, ref fontData, out currMarkInfo.currMarkData.fontSizeX, out currMarkInfo.currMarkData.fontSizeY, out ErrorCode);
                //    currMarkInfo.currMarkData.fontData.Add(fontData);
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManaualMarkWindow", "btnSendData_Click", string.Format("FONT DATA {0}CH, {1}PT", i, fontData.Count));
                //}

                currMarkInfo.currMarkData.isReady = true;

                ShowCurrentMarkingInformation(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern, 2).Wait();
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).SendFontData(Txt_data_Manual.Text.ToUpper(), patName);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).SendFontData(currMarkInfo.currMarkData.mesData.vin, pat);
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private async void btnMarkStart_Click(object sender, RoutedEventArgs e)
        {
            string className = "ManualMarkWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "btnMarkStart_Click";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string value = "";

            try
            {
                ShowLog("RUN START");
                m_currCMD = (byte)'R';
                if (txtVINNumber.Text.Length <= 0)
                {
                    ShowLog("SEND FONT DATA - SEND ERROR (VIN EMPTY)");
                    return;
                }

                Util.GetPrivateProfileValue("COLOR", "CHARACTERLINE", "#FF5072DD", ref value, Constants.PARAMS_INI_FILE);
                if (value.Length > 8)
                    lineColor = (Color)ColorConverter.ConvertFromString(value);
                else
                    lineColor = (Color)ColorConverter.ConvertFromString("#FF5072DD");

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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void cbxPatternList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string name = "";
            //string patternname = Constants.PATTERN_PATH + name + ".ini";
            PatternValueEx patData = new PatternValueEx();
            string className = "ManualMarkWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "cbxPatternList_SelectionChanged";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                //GetPatternData(ref patData);
                if (cbxPatternList.SelectedIndex < 0)
                    return;

                name = cbxPatternList.SelectedItem.ToString();
                LoadPatternData(name, ref patData);
                if (CB_font.Items.Count > 0)
                {
                    if (CB_font.Items.Contains(patData.fontValue.fontName))
                    {
                        CB_font.SelectedItem = patData.fontValue.fontName;
                    }
                    else
                    {
                        CB_font.SelectedIndex = 0;
                        patData.fontValue.fontName = CB_font.SelectedItem.ToString();
                    }
                }

                DisplayPatternData(patData);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void DisplayPatternData(PatternValueEx pat)
        {
            string className = "ManualMarkWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "DisplayPatternData";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                cbxPatternList.SelectedItem = pat.name;
                CB_font.SelectedItem = pat.fontValue.fontName;

                //
                txtStartX.Text = pat.positionValue.center3DPos.X.ToString("F2");
                txtStartY.Text = pat.positionValue.center3DPos.Y.ToString("F2");
                txtHeight.Text = pat.fontValue.height.ToString("F2");
                txtWidth.Text = pat.fontValue.width.ToString("F2");
                txtPitch.Text = pat.fontValue.pitch.ToString("F2");
                txtAngle.Text = pat.fontValue.rotateAngle.ToString();
                txtStrike.Text = pat.fontValue.strikeCount.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        //private void GetPatternData(ref PatternValue data)
        //{
        //    string className = "ManualMarkWindow";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "GetPatternData";// MethodBase.GetCurrentMethod().Name;
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    try
        //    {
        //        if (cbxPatternList.SelectedIndex >= 0)
        //            data.name = cbxPatternList.SelectedItem.ToString();
        //        if (CB_font.SelectedIndex >= 0)
        //            data.fontName = CB_font.SelectedItem.ToString();

        //        double.TryParse(txtStartX.Text, out data.startX);
        //        double.TryParse(txtStartY.Text, out data.startY);
        //        double.TryParse(txtHeight.Text, out data.height);
        //        double.TryParse(txtWidth.Text, out data.width);
        //        double.TryParse(txtPitch.Text, out data.pitch);
        //        double.TryParse(txtAngle.Text, out data.rotateAngle);
        //        short.TryParse(txtStrike.Text, out data.strikeCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //}

        private void LoadPatternData(string patternfile, ref PatternValueEx data)
        {
            string className = "ManualMarkWindow";
            string funcName = "LoadPatternData";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                data = new PatternValueEx();
                string patternName = Constants.PATTERN_PATH + patternfile + ".ini";

                data.scanValue.stepLength_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);

                Util.GetPrivateProfileValue("FONT", "NAME", "11X16", ref data.fontValue.fontName, patternName); // load FONT
                data.positionValue.center3DPos.X = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSX", 20, patternName);
                data.positionValue.center3DPos.Y = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSY", 50, patternName);

                data.fontValue.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4, patternName);
                data.fontValue.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7, patternName);
                data.fontValue.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6, patternName);
                data.fontValue.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternName);
                data.fontValue.strikeCount = (short)Util.GetPrivateProfileValueUINT("FONT", "STRIKECOUNT", 0, patternName);

                data.speedValue.initSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "INITIALSPEED", 10, patternName);
                data.speedValue.targetSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "TARGETSPEED", 10, patternName);
                data.speedValue.accelSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "ACCELERATION", 15, patternName);
                data.speedValue.decelSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "DECELERATION", 15, patternName);
                data.speedValue.solOnTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "SOLONTIME", 10, patternName);
                data.speedValue.solOffTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "SOLOFFTIME", 10, patternName);
                data.speedValue.dwellTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "DWELLTIME", 10, patternName);

                data.headValue.stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
                data.headValue.max_X = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_X", 0, Constants.MARKING_INI_FILE);
                data.headValue.max_Y = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 0, Constants.MARKING_INI_FILE);

                data.headValue.park3DPos.X = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, Constants.MARKING_INI_FILE);
                data.headValue.park3DPos.Y = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, Constants.MARKING_INI_FILE);

                data.speedValue.initSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "INITIALSPEED", 50, Constants.MARKING_INI_FILE);
                data.speedValue.targetSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "TARGETSPEED", 50, Constants.MARKING_INI_FILE);
                data.speedValue.accelSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "ACCELERATION", 10, Constants.MARKING_INI_FILE);
                data.speedValue.decelSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "DECELERATION", 10, Constants.MARKING_INI_FILE);

                data.scanValue.max_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "MAX_U", 190, Constants.SCANNER_INI_FILE);
                data.scanValue.parkingU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, Constants.SCANNER_INI_FILE);
                data.scanValue.home_U = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, Constants.SCANNER_INI_FILE);

                data.scanValue.initSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "INITIALSPEED", 10, patternName);
                data.scanValue.targetSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "TARGETSPEED", 10, patternName);
                data.scanValue.accelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "ACCELERATION", 10, patternName);
                data.scanValue.decelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "DECELERATION", 10, patternName);

                data.scanValue.initSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "INITIALSPEED", 10, patternName);
                data.scanValue.targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "TARGETSPEED", 10, patternName);
                data.scanValue.accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "ACCELERATION", 10, patternName);
                data.scanValue.decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "DECELERATION", 10, patternName);

                data.scanValue.reverseScan = (byte)Util.GetPrivateProfileValueByte("PROFILER", "REVERSESCAN", 0, patternName); // load Max U_Scan

                data.scanValue.startU = (double)Util.GetPrivateProfileValueDouble("PROFILER", "STARTPOS", 20, patternName); // load Max U_Scan
                data.scanValue.scanLen = (double)Util.GetPrivateProfileValueDouble("PROFILER", "SCANLEN", 130, patternName);

                //data.stepLength_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);

                //Util.GetPrivateProfileValue("FONT", "NAME", "11X16", ref data.fontName, patternName); // load FONT
                //data.startX = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSX", 20, patternName);
                //data.startY = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSY", 50, patternName);
                //data.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4, patternName);
                //data.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7, patternName);
                //data.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6, patternName);

                ////data.width = (short)Util.GetPrivateProfileValueUINT("FONT", "WIDTH", 4, patternName);
                ////data.height = (short)Util.GetPrivateProfileValueUINT("FONT", "HEIGHT", 7, patternName);
                ////data.pitch = (short)Util.GetPrivateProfileValueUINT("FONT", "PITCH", 6, patternName);
                //data.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternName);
                //data.strikeCount = (short)Util.GetPrivateProfileValueUINT("FONT", "STRIKECOUNT", 0, patternName);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        //private void DisplayValue(string pattern)
        //{
        //    string value = "";
        //    string patternfile = "Parameter\\" + pattern + ".ini";

        //    try
        //    {
        //        Util.GetPrivateProfileValue("FONT", "NAME", "11X16", ref value, patternfile); // load FONT

        //        if (CB_font.Items.Contains(value))
        //            CB_font.SelectedItem = value;

        //        Util.GetPrivateProfileValue("FONT", "STARTPOSX", "", ref value, patternfile); // load X
        //        txtStartX.Text = value;

        //        Util.GetPrivateProfileValue("FONT", "STARTPOSY", "", ref value, patternfile); // load Y
        //        txtStartY.Text = value;

        //        Util.GetPrivateProfileValue("FONT", "HEIGHT", "", ref value, patternfile); // load height
        //        txtHeight.Text = value;

        //        Util.GetPrivateProfileValue("FONT", "WIDTH", "", ref value, patternfile); // load width
        //        txtWidth.Text = value;

        //        Util.GetPrivateProfileValue("FONT", "PITCH", "", ref value, patternfile); // load pitch
        //        txtPitch.Text = value;

        //        Util.GetPrivateProfileValue("FONT", "ROTATEANGLE", "", ref value, patternfile); // load angle
        //        txtAngle.Text = value;

        //        Util.GetPrivateProfileValue("FONT", "STRIKECOUNT", "", ref value, patternfile); //load strike
        //        txtStrike.Text = value;
        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //}

        private int CheckMarkControllerSensor(byte[] sensor, int length)
        {
            int retval = 0;

            return retval;
        }

        private void ShowMarkingOneLine(int xcharIndex, int fontIndex)//, Canvas showcanvas)//, int sensor)
        {
            Brush brush;
            List<FontDataClass> fdata = new List<FontDataClass>();
            Canvas showcanvas = new Canvas();

            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "ShowMarkingOneLine", "START", Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "ShowMarkingOneLine", string.Format("INDEX : CH = {0}, FT = {1}", xcharIndex, fontIndex), Thread.CurrentThread.ManagedThreadId);

                fdata = currMarkInfo.currMarkData.fontData[xcharIndex];
                FontDataClass font = fdata[fontIndex];
                string name = string.Format("cvsshowChar{0:D2}", xcharIndex);
                showcanvas = (Canvas)FindName(name);
                if (showcanvas == null)
                    return;

                brush = new SolidColorBrush(lineColor);

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
                double CharThick = currMarkInfo.currMarkData.pattern.fontValue.thickness * Util.PXPERMM * (canvaswidth / orgWidth + 0.3d);

                double heightthRatio = canvasheight / orgHeight;
                double widthRatio = canvaswidth / orgWidth;

                if (font.Flag == 1)
                {
                    //(showcanvas.Parent as Canvas).Children.Clear();
                    charline = new System.Windows.Shapes.Line();
                    //                    charline.Stroke = Brushes.Orange;
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
                    //                    charline.Stroke = Brushes.Orange;
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
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "ShowMarkingOneLine", "END", Thread.CurrentThread.ManagedThreadId);
                return;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "ShowMarkingOneLine", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }

        public async void OnMarkControllerEventFunc(object sender, MarkControllerRecievedEvnetArgs e)
        {
            string param1 = "";
            string param2 = "";
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            byte[] sensor = new byte[8];
            int retval = 0;
            //short Length;
            //short steplength;
            string value = "";
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            byte currCMD = 0;

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", "START", Thread.CurrentThread.ManagedThreadId);

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
                            ITNTTraceLog.Instance.TraceHex(1, "ManualMarkWindow::OnMarkControllerEventFunc()  RECV MARK :  ", e.receiveSize, e.receiveBuffer);

                            if (param1.Length > 0)
                                chindex = Convert.ToInt32(param1, 16);
                            if (param2.Length > 0)
                                ptindex = Convert.ToInt32(param2, 16);
                            //if (param3.Length > 0)
                            //    drindex = Convert.ToInt32(param3, 16);

                            if ((currCMD == 'R') && (m_currCMD == 'R'))
                            {
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
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", "COLD BOOT", Thread.CurrentThread.ManagedThreadId);
                            //retval = ((MainWindow)System.Windows.Application.Current.MainWindow).InitializeController().Result.execResult;
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
                            //}
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", "COLD BOOT END", Thread.CurrentThread.ManagedThreadId);
                            break;

                        case 0x38:
                            doingCommand = false;

                            if ((currCMD == 'R') && (m_currCMD == 'R'))
                            {
                                ////ITNTJobLog.Instance.Trace(0, "[4] : RECEIVE MARKING COMPLETE");

//#if MANUAL_MARK
//                                //ShowCurrentStateLabel(5);
//                                ShowCurrentStateLabelManual(4);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
//#else
//#endif
                                //
                                currMarkInfo.currMarkData.mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
                                currMarkInfo.currMarkData.mesData.marktime = DateTime.Now.ToString("HH:mm:ss");

                                Util.GetPrivateProfileValue("OPTION", "UPDATEFLAG", "0", ref value, Constants.PARAMS_INI_FILE);

                                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.currMarkData.mesData.sequence + "-" + currMarkInfo.currMarkData.mesData.vin);
                                ////ITNTJobLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.currMarkData.mesData.sequence + "-" + currMarkInfo.currMarkData.mesData.vin);

                                //currMarkInfo.Initialize();
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", "[4] : MARKING COMPLETE", Thread.CurrentThread.ManagedThreadId);
                                ////ITNTJobLog.Instance.Trace(0, "[4] : MARKING COMPLETE");
                                //markRunTimer.Stop();
                                //ShowCurrentStateLabel(7);
                                ShowLog("MARKING COMPLETE!!!!");
                                m_currCMD = 0;
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

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ManualMarkWindow", "OnMarkControllerEventFunc", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
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

        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (this.WindowState == WindowState.Maximized)
        //    {
        //        ChangeSize(this.ActualWidth, this.ActualHeight);
        //    }
        //    this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
        //}

        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    orginalWidth = this.Width;
        //    originalHeight = this.Height;
        //    ChangeSize(e.NewSize.Width, e.NewSize.Height);
        //}

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            List<FontDataClass> flist = new List<FontDataClass>();
            if (currMarkInfo.currMarkData.isReady)
            {
                for (int i = 0; i < currMarkInfo.currMarkData.fontData.Count; i++)
                {
                    flist = currMarkInfo.currMarkData.fontData[i];
                    for (int j = 0; j < flist.Count; j++)
                    {
                        ShowMarkingOneLine(i, j);
                        await Task.Delay(100);
                    }
                }
            }
        }
        //private void btnExit_Click(object sender, RoutedEventArgs e)
        //{

        //}
    }
}
