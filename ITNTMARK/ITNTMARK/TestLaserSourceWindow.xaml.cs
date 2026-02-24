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

namespace ITNTMARK
{
    /// <summary>
    /// Interaction logic for TestLaserSourceWindow.xaml
    /// </summary>
    public partial class TestLaserSourceWindow : Window
    {
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        public TestLaserSourceWindow()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

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

        private async void btnConfigWaveForm_Click(object sender, RoutedEventArgs e)
        {
            string[] pcfg;
            string[] prls;
            //string[] sel;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //PatternValue pattern = new PatternValue();

            try
            {
                //ImageProcessManager.GetPatternDataEx("Pattern_DEF", ref pattern);

                ShowLog("[CONFIG WAVE FORM] START");
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ConfigWaveformMode(0);
                if (retval.execResult != 0)
                {
                    ShowLog("[CONFIG WAVE FORM] FAIL : " + retval.execResult.ToString());
                    return;
                }

                pcfg = retval.recvString.Split('[', ']');
                for (int i = 0; i < pcfg.Length; i++)
                {
                    ShowLog(pcfg[i]);
                }

                ShowLog("[CONFIG WAVE FORM] END");
            }
            catch (Exception ex)
            {
                ShowLog("[CONFIG WAVE FORM] EXCEPTION : " + ex.Message);
            }
        }

        private void btnOPEN_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCLOSE_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnReadDeviceStatus_Click(object sender, RoutedEventArgs e)
        {
            string[] st;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                ShowLog("[READ DEVICE STATUS] START");
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadDeviceStatus(0);
                if (retval.execResult != 0)
                {
                    ShowLog("[READ DEVICE STATUS] FAIL : " + retval.execResult.ToString());
                    return;
                }

                st = retval.recvString.Split(':'); LASERSTATUS Status = (LASERSTATUS)UInt32.Parse(st[1]);
                for (int i = 0; i < st.Length; i++)
                {
                    ShowLog(st[i]);
                }
                ShowLog("[READ DEVICE STATUS] END");
            }
            catch (Exception ex)
            {
                ShowLog("[READ DEVICE STATUS] EXCEPTION : " + ex.Message);
            }
        }

        private async void btnEMISSION_Click(object sender, RoutedEventArgs e)
        {
            string[] st;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string btnString = "";
            string newString = "";

            try
            {
                btnString = btnEMISSION.Content.ToString();
                ShowLog("[" + btnString + "] START");

                if (btnString.Contains("OFF") == true)
                {
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    newString = "EMISSION ON";
                }
                else
                {
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.StopEmission();
                    btnString = "EMISSION OFF";
                }
                if (retval.execResult != 0)
                {
                    ShowLog("[" + btnString + "] FAIL : " + retval.execResult.ToString());
                    return;
                }
                btnEMISSION.Content = newString;
                ShowLog("[" + btnString + "] END");
            }
            catch (Exception ex)
            {
                ShowLog("[" + btnString + "] EXCEPTION : " + ex.Message);
            }
        }

        private async void btnSelectProfile_Click(object sender, RoutedEventArgs e)
        {
            string[] st;
            string[] prsel;
            string[] sel;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            PatternValueEx pattern = new PatternValueEx();
            string value = "";
            byte bHeadType = 0;


            try
            {
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);

                ImageProcessManager.GetPatternValue("Pattern_DEFAULT", bHeadType, ref pattern);

                ShowLog("[SELECT PROFIEL] START");
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SelectProfile(pattern.laserValue.waveformNum.ToString());
                if (retval.execResult != 0)
                {
                    ShowLog("[SELECT PROFIEL] FAIL : " + retval.execResult.ToString());
                    return;
                }
                prsel = retval.recvString.Split('[', ']');
                if (prsel[0] == "PRSEL: ")
                {
                    sel = prsel[1].Split(':');
                    if (pattern.laserValue.waveformNum.ToString() != sel[0])
                    {
                        ShowLog("[SELECT PROFIEL] FAIL : PROFILE SETTING ERROR!");
                        return;
                    }

                    for (int i = 0; i < prsel.Length; i++)
                    {
                        ShowLog(prsel[i]);
                    }
                }
                else
                {
                    //Debug.WriteLine("Profile setting response Error!"); // Error
                    ShowLog("[SELECT PROFIEL] FAIL : PROFILE SETTING RESPONSE ERROR!");
                    return;
                }
                ShowLog("[SELECT PROFIEL] START");
            }
            catch (Exception ex)
            {
                ShowLog("[SELECT PROFIEL] EXCEPTION : " + ex.Message);
            }
        }

        private async void btnAimingBeam_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string btnString = "";
            string newString = "";

            try
            {
                btnString = btnEMISSION.Content.ToString();
                ShowLog("[" + btnString + "] START");

                if (btnString.Contains("OFF") == true)
                {
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamOFF();
                    newString = "AIMING ON";
                }
                else
                {
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.AimingBeamON();
                    btnString = "AIMING OFF";
                }

                if (retval.execResult != 0)
                {
                    ShowLog("[" + btnString + "] FAIL : " + retval.execResult.ToString());
                    return;
                }

                btnEMISSION.Content = newString;
                ShowLog("[" + btnString + "] END");
            }
            catch (Exception ex)
            {
                ShowLog("[" + btnString + "] EXCEPTION : " + ex.Message);
            }
        }

        private async void btnReadInfo_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                ShowLog("[LASER INFORMATION] START");
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadSWVersion();
                if (retval.execResult != 0)
                {
                    ShowLog("[LASER INFORMATION] FAIL(SW VERSION) : " + retval.execResult.ToString());
                    return;
                }
                ShowLog("    SW VER : " + retval.recvString);

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadSerialNumber();
                if (retval.execResult != 0)
                {
                    ShowLog("[LASER INFORMATION] FAIL(SERIAL NO) : " + retval.execResult.ToString());
                    return;
                }
                ShowLog("    SERIAL : " + retval.recvString);

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadLaserVersion();
                if (retval.execResult != 0)
                {
                    ShowLog("[LASER INFORMATION] FAIL(LASER VERSION) : " + retval.execResult.ToString());
                    return;
                }
                ShowLog("    LASER VER : " + retval.recvString);

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.ReadLaserConfiguration();
                if (retval.execResult != 0)
                {
                    ShowLog("[LASER INFORMATION] FAIL(CONFIG) : " + retval.execResult.ToString());
                    return;
                }
                ShowLog("    CONFIG : " + retval.recvString);

                ShowLog("[LASER INFORMATION] END");
            }
            catch (Exception ex)
            {
                ShowLog("[LASER INFORMATION] EXCEPTION : " + ex.Message);
            }
        }

        private async void btnSetExtControl_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                //ImageProcessManager.GetPatternDataEx("Pattern_DEF", ref pattern);

                ShowLog("[SET EXT BEAN CONTROL] START");
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).laserSource.SetExternalAimingBeamControll(0);
                if (retval.execResult != 0)
                {
                    ShowLog("[SET EXT BEAN CONTROL] FAIL : " + retval.execResult.ToString());
                    return;
                }
                ShowLog("[SET EXT BEAN CONTROL] END");
            }
            catch (Exception ex)
            {
                ShowLog("[SET EXT BEAN CONTROL] EXCEPTION : " + ex.Message);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {

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

    }
}
