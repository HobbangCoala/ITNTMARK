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
using System.Threading;
using ITNTUTIL;
using System.IO;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// ManualMarkWindow2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManualMarkWindow2 : Window
    {
        public ManualMarkWindow2()
        {
            InitializeComponent();
        }

        private void txtVINNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            string className = "SetControllerWindow2";
            string funcName = "txtVIN_TextChanged";

            try
            {
                if (lblVINLength != null)
                    lblVINLength.Content = txtVINNumber.Text.Length.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public ManualMarkWindow2(string patternName, string vin)
        {
            InitializeComponent();

            //this.Owner = Application.Current.MainWindow;

            //WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            //ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            //this.Loaded += new RoutedEventHandler(Window_Loaded);

            //List<string> fnames = new List<string>();
            //string fontfile = "Parameter\\FONT.ini";
            //string value = "";
            //int i = 0;
            //Util.GetPrivateProfileValue("FONTLIST", "NAME", "11X16|5X7|OCR|HMC5", ref value, fontfile);
            //string[] fonts = value.Split('|');
            //if (fonts.Length <= 0)
            //{
            //    CB_font.Items.Add("11X16");
            //    CB_font.Items.Add("5X7");
            //    CB_font.Items.Add("OCR");
            //    CB_font.Items.Add("HMC5");
            //}
            //else
            //{
            //    for (i = 0; i < fonts.Length; i++)
            //        CB_font.Items.Add(fonts[i]);
            //    CB_font.SelectedIndex = 0;
            //}

            //Util.GetPrivateProfileValue("CONFIG", "STEP_LENGTH", "100", ref value, Constants.SCANNER_INI_FILE);
            //if (value.Length <= 0)
            //    stepLength_u = 100;
            //else
            //    short.TryParse(value, out stepLength_u);
            //if (stepLength_u <= 0)
            //    stepLength_u = 100;

            //Util.GetPrivateProfileValue("MARK", "STEP_LENGTH", "50", ref value, Constants.MARKING_INI_FILE);
            //if (value.Length <= 0)
            //    stepLength = 50;
            //else
            //    short.TryParse(value, out stepLength);
            //if (stepLength <= 0)
            //    stepLength = 50;

            //string selPattern = "";
            //PatternValueEx pat = new PatternValueEx();
            //List<string> names = new List<string>();
            //string patternfile = AppDomain.CurrentDomain.BaseDirectory + Constants.PATTERN_PATH;
            //names = DirFileSearch(patternfile, "*.ini").Result;
            //for (i = 0; i < names.Count; i++)
            //    cbxPatternList.Items.Add(names[i]);

            //if (cbxPatternList.Items.Count > 0)
            //{
            //    if (names.Contains(patternName))
            //    {
            //        cbxPatternList.SelectedItem = patternName;
            //        selPattern = patternName;
            //    }
            //    else
            //    {
            //        cbxPatternList.SelectedIndex = 0;
            //        selPattern = cbxPatternList.SelectionBoxItem.ToString();
            //    }
            //    //LoadPatternData(selPattern, ref pat);
            //    //if (CB_font.Items.Count > 0)
            //    //{
            //    //    if (CB_font.Items.Contains(pat.fontName))
            //    //    {
            //    //        CB_font.SelectedItem = pat.fontName;
            //    //    }
            //    //    else
            //    //    {
            //    //        CB_font.SelectedIndex = 0;
            //    //        pat.fontName = CB_font.SelectedItem.ToString();
            //    //    }
            //    //}
            //    //DisplayPatternData(pat);
            //}
            //else
            //{
            //    ShowDefaultValue();
            //}

            //txtVINNumber.Text = vin;
            //Util.GetPrivateProfileValue("COLOR", "CHARACTERLINE", "#FF5072DD", ref value, Constants.PARAMS_INI_FILE);
            //if (value.Length > 8)
            //    lineColor = (Color)ColorConverter.ConvertFromString(value);
            //else
            //    lineColor = (Color)ColorConverter.ConvertFromString("#FF5072DD");

            ////((MainWindow)System.Windows.Application.Current.MainWindow).currentWindow = 1;
            //((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.MarkControllerDataArrivedEventFunc += OnMarkControllerEventFunc;
        }

        private void ShowDefaultValue()
        {
            //txtStartX.Text = "25";
            //txtStartY.Text = "20";
            //txtHeight.Text = "7";
            //txtWidth.Text = "4";
            //txtPitch.Text = "6";
            //txtAngle.Text = "0";
            //txtStrike.Text = "1";
        }

        public void SetPatternName(string patternName)
        {
            //this.patternName = patternName;
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

        private void cbxPatternList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnSendData_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnMarkStart_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
