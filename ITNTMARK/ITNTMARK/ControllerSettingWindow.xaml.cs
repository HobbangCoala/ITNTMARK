using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// ControllerSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ControllerSettingWindow : Window
    {
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        public ControllerSettingWindow()
        {
            InitializeComponent();
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

        private void btnOPEN_Click(object sender, RoutedEventArgs e)
        {
            //int retval = 0;
            ////retval = ((MainWindow)System.Windows.Application.Current.MainWindow).mark.OpenDevice("COM3", 38400, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);
            //retval = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.OpenDevice("COM3", 38400, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);

        }

        private void btnGetSetting_Click(object sender, RoutedEventArgs e)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //MARK_COMMAND_RESULT retval = new MARK_COMMAND_RESULT();
            //int respSize = 0;
            //byte cmd = 0;
            byte[] data = new byte[1024];

            //if (((MainWindow)System.Windows.Application.Current.MainWindow).mark.IsOpen)
            //{
            //    cmd = 0x4f;
            //    retval = ((MainWindow)System.Windows.Application.Current.MainWindow).mark.GetCurrentSetting().Result;// .get ExecuteCommandMsg2(0, 0x30, "0", "0", "0", "0", "0", "0", "0", "0", "");//, ref data, ref respSize);
            //}
            if (((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.IsOpen)
            {
                //cmd = 0x4f;
                retval = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.markComm.GetCurrentSetting().Result;// .get ExecuteCommandMsg2(0, 0x30, "0", "0", "0", "0", "0", "0", "0", "0", "");//, ref data, ref respSize);
                if(retval.execResult != 0)
                {
                    string str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ERROR : NOT OPEN";
                    lbxStatus.Items.Add(str);
                    lblStatus.Content = "ERROR : " + retval.execResult.ToString();
                }
            }
            else
            {
                string str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ERROR : NOT OPEN";
                lbxStatus.Items.Add(str);
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
                Debug.WriteLine("ChangeSize() Exception : " + ex.HResult.ToString() + " / " + ex.Message);
            }
        }

    }
}
