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
using System.Threading;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// WarningWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WarningWindow : Window
    {
        public bool DialogExitValue = false;
        byte showType = 0;

        public WarningWindow()
        {
            InitializeComponent();
        }

        public WarningWindow(string title, string msg1, string msg2, string msg3, string msg4, string msg5, Window owner)
        {
            string className = "WarningWindow";
            string funcName = "WarningWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = owner;

            lblConfrimMsg1.Content = msg1;
            lblConfrimMsg2.Content = msg2;
            lblConfrimMsg3.Content = msg3;
            lblConfrimMsg4.Content = msg4;
            lblConfrimMsg5.Content = msg5;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public WarningWindow(string title, ConfirmWindowString msg1, ConfirmWindowString msg2, ConfirmWindowString msg3, ConfirmWindowString msg4, ConfirmWindowString msg5, string btnOKText, string btnNOText, Window owner, byte showType = 0)
        {
            string className = "WarningWindow";
            string funcName = "WarningWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = owner;

            this.Title = title;

            lblConfrimMsg1.Foreground = msg1.Foreground;
            lblConfrimMsg1.Background = msg1.Background;
            lblConfrimMsg1.HorizontalContentAlignment = msg1.HorizontalContentAlignment;
            lblConfrimMsg1.VerticalContentAlignment = msg1.VerticalContentAlignment;
            lblConfrimMsg1.FontSize = msg1.Fontsize;
            lblConfrimMsg1.Content = msg1.Message;

            lblConfrimMsg2.Foreground = msg2.Foreground;
            lblConfrimMsg2.Background = msg2.Background;
            lblConfrimMsg2.HorizontalContentAlignment = msg2.HorizontalContentAlignment;
            lblConfrimMsg2.VerticalContentAlignment = msg2.VerticalContentAlignment;
            lblConfrimMsg2.FontSize = msg2.Fontsize;
            lblConfrimMsg2.Content = msg2.Message;

            lblConfrimMsg3.Foreground = msg3.Foreground;
            lblConfrimMsg3.Background = msg3.Background;
            lblConfrimMsg3.HorizontalContentAlignment = msg3.HorizontalContentAlignment;
            lblConfrimMsg3.VerticalContentAlignment = msg3.VerticalContentAlignment;
            lblConfrimMsg3.FontSize = msg3.Fontsize;
            lblConfrimMsg3.Content = msg3.Message;

            lblConfrimMsg4.Foreground = msg4.Foreground;
            lblConfrimMsg4.Background = msg4.Background;
            lblConfrimMsg4.HorizontalContentAlignment = msg4.HorizontalContentAlignment;
            lblConfrimMsg4.VerticalContentAlignment = msg4.VerticalContentAlignment;
            lblConfrimMsg4.FontSize = msg4.Fontsize;
            lblConfrimMsg4.Content = msg4.Message;

            lblConfrimMsg5.Foreground = msg5.Foreground;
            lblConfrimMsg5.Background = msg5.Background;
            lblConfrimMsg5.HorizontalContentAlignment = msg5.HorizontalContentAlignment;
            lblConfrimMsg5.VerticalContentAlignment = msg5.VerticalContentAlignment;
            lblConfrimMsg5.FontSize = msg5.Fontsize;
            lblConfrimMsg5.Content = msg5.Message;

            //btnOK.Content = btnOKText;
            //btnCancel.Content = btnNOText;

            //btnOK2.Visibility = Visibility.Hidden;
            bool useOK = false;
            bool useCancel = false;
            if (btnOKText.Length > 0)
            {
                useOK = true;
            }

            if (btnNOText.Length > 0)
            {
                useCancel = true;
            }

            if (useOK && useCancel)
            {
                btnOK.Content = btnOKText;
                btnOK.Visibility = Visibility.Visible;

                btnCancel.Content = btnNOText;
                btnCancel.Visibility = Visibility.Visible;
            }
            else if (useOK)
            {
                Thickness margin = new Thickness();
                margin = btnOK.Margin;
                margin.Left = Math.Max(0, (Width - btnOK.Width) / 2);
                btnOK.Margin = margin;
                btnOK.Content = btnOKText;
                btnOK.Visibility = Visibility.Visible;
                btnCancel.Visibility = Visibility.Hidden;
            }
            else if (useCancel)
            {
                Thickness margin = new Thickness();
                margin = btnCancel.Margin;
                margin.Left = Math.Max(0, (Width - btnCancel.Width) / 2);
                btnCancel.Margin = margin;
                btnCancel.Content = btnNOText;
                btnOK.Visibility = Visibility.Hidden;
                btnCancel.Visibility = Visibility.Visible;
            }
            else
            {
                btnOK.Visibility = Visibility.Hidden;
                btnCancel.Visibility = Visibility.Hidden;
            }
            this.showType = showType;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public WarningWindow(string ColorString, string title, string msg1, string msg2, string msg3, string msg4, string msg5, Window owner)
        {
            string className = "WarningWindow";
            string funcName = "WarningWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = owner;

            Color color = (Color)ColorConverter.ConvertFromString(ColorString);
            this.Background = new SolidColorBrush(color);

            lblConfrimMsg1.Content = msg1;
            lblConfrimMsg2.Content = msg2;
            lblConfrimMsg3.Content = msg3;
            lblConfrimMsg4.Content = msg4;
            lblConfrimMsg5.Content = msg5;

            //btnOK2.Visibility = Visibility.Hidden;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public WarningWindow(string title, string msg1, string msg2, string msg3, string msg4, string msg5)
        {
            string className = "WarningWindow";
            string funcName = "WarningWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = Application.Current.MainWindow;

            if (msg1 != null)
            {
                lblConfrimMsg1.Foreground = Brushes.Red;
                lblConfrimMsg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblConfrimMsg1.Content = msg1;
            }
            if (msg2 != null)
            {
                lblConfrimMsg2.Foreground = Brushes.Red;
                lblConfrimMsg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblConfrimMsg2.Content = msg2;
            }
            if (msg3 != null)
            {
                lblConfrimMsg3.Foreground = Brushes.Red;
                lblConfrimMsg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblConfrimMsg3.Content = msg3;
            }
            if (msg4 != null)
            {
                lblConfrimMsg4.Foreground = Brushes.Red;
                lblConfrimMsg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblConfrimMsg4.Content = msg4;
            }
            if (msg5 != null)
            {
                lblConfrimMsg5.Foreground = Brushes.Red;
                lblConfrimMsg5.HorizontalContentAlignment = HorizontalAlignment.Center;
                lblConfrimMsg5.Content = msg5;
            }

            this.Title = title;

            //btnOK2.Content = "OK";
            //btnOK2.Visibility = Visibility.Visible;
            //btnOK.Visibility = Visibility.Hidden;
            //btnCancel.Visibility = Visibility.Hidden;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public WarningWindow(string title, ConfirmWindowString msg1, ConfirmWindowString msg2, ConfirmWindowString msg3, ConfirmWindowString msg4, ConfirmWindowString msg5, string btnOKText, string btnNOText)
        {
            string className = "WarningWindow";
            string funcName = "WarningWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();

            lblConfrimMsg1.Foreground = msg1.Foreground;
            lblConfrimMsg1.Background = msg1.Background;
            lblConfrimMsg1.HorizontalContentAlignment = msg1.HorizontalContentAlignment;
            lblConfrimMsg1.VerticalContentAlignment = msg1.VerticalContentAlignment;
            lblConfrimMsg1.FontSize = msg1.Fontsize;
            lblConfrimMsg1.Content = msg1.Message;

            lblConfrimMsg2.Foreground = msg2.Foreground;
            lblConfrimMsg2.Background = msg2.Background;
            lblConfrimMsg2.HorizontalContentAlignment = msg2.HorizontalContentAlignment;
            lblConfrimMsg2.VerticalContentAlignment = msg2.VerticalContentAlignment;
            lblConfrimMsg2.FontSize = msg2.Fontsize;
            lblConfrimMsg2.Content = msg2.Message;

            lblConfrimMsg3.Foreground = msg3.Foreground;
            lblConfrimMsg3.Background = msg3.Background;
            lblConfrimMsg3.HorizontalContentAlignment = msg3.HorizontalContentAlignment;
            lblConfrimMsg3.VerticalContentAlignment = msg3.VerticalContentAlignment;
            lblConfrimMsg3.FontSize = msg3.Fontsize;
            lblConfrimMsg3.Content = msg3.Message;

            lblConfrimMsg4.Foreground = msg4.Foreground;
            lblConfrimMsg4.Background = msg4.Background;
            lblConfrimMsg4.HorizontalContentAlignment = msg4.HorizontalContentAlignment;
            lblConfrimMsg4.VerticalContentAlignment = msg4.VerticalContentAlignment;
            lblConfrimMsg4.FontSize = msg4.Fontsize;
            lblConfrimMsg4.Content = msg4.Message;

            lblConfrimMsg5.Foreground = msg5.Foreground;
            lblConfrimMsg5.Background = msg5.Background;
            lblConfrimMsg5.HorizontalContentAlignment = msg5.HorizontalContentAlignment;
            lblConfrimMsg5.VerticalContentAlignment = msg5.VerticalContentAlignment;
            lblConfrimMsg5.FontSize = msg5.Fontsize;
            lblConfrimMsg5.Content = msg5.Message;

            bool useOK = false;
            bool useCancel = false;
            if (btnOKText.Length > 0)
            {
                useOK = true;
            }

            if (btnNOText.Length > 0)
            {
                useCancel = true;
            }

            if (useOK && useCancel)
            {
                btnOK.Content = btnOKText;
                btnOK.Visibility = Visibility.Visible;

                btnCancel.Content = btnNOText;
                btnCancel.Visibility = Visibility.Visible;
            }
            else if (useOK)
            {
                Thickness margin = new Thickness();
                margin = btnOK.Margin;
                margin.Left = Math.Max(0, (Width - btnOK.Width) / 2);
                btnOK.Margin = margin;
                btnOK.Content = btnOKText;
                btnOK.Visibility = Visibility.Visible;
                btnCancel.Visibility = Visibility.Hidden;
            }
            else if (useCancel)
            {
                Thickness margin = new Thickness();
                margin = btnCancel.Margin;
                margin.Left = Math.Max(0, (Width - btnCancel.Width) / 2);
                btnCancel.Margin = margin;
                btnCancel.Content = btnNOText;
                btnOK.Visibility = Visibility.Hidden;
                btnCancel.Visibility = Visibility.Visible;
            }
            else
            {
                btnOK.Visibility = Visibility.Hidden;
                btnCancel.Visibility = Visibility.Hidden;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            //ConfirmWindowString msg1 = new ConfirmWindowString();
            //ConfirmWindowString msg2 = new ConfirmWindowString();
            //ConfirmWindowString msg3 = new ConfirmWindowString();
            //ConfirmWindowString msg4 = new ConfirmWindowString();

            ////msg2.Message = "";
            ////msg2.Fontsize = 16;
            ////msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
            ////msg2.VerticalContentAlignment = VerticalAlignment.Center;
            ////msg2.Foreground = Brushes.Red;
            ////msg2.Background = Brushes.White;

            //msg3.Message = "재각인을 중지하시겠습니까?";
            //msg3.Fontsize = 16;
            //msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
            //msg3.VerticalContentAlignment = VerticalAlignment.Center;
            //msg3.Foreground = Brushes.Red;
            //msg3.Background = Brushes.White;

            //ConfirmWindow window = new ConfirmWindow("", msg1, msg2, msg3, msg4, msg5, "YES", "NO", this);
            //window.Owner = System.Windows.Application.Current.MainWindow;
            //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //this.Topmost = false;
            //window.Topmost = true;
            //window.Focus();
            //if (window.ShowDialog() == false)
            //    return;

            //btnCancel.Visibility = Visibility.Hidden;

            //lblConfrimMsg3.Foreground = Brushes.Black;
            //lblConfrimMsg4.Foreground = Brushes.Black;

            //lblConfrimMsg2.Content = "";
            //lblConfrimMsg3.Content = "재각인을 취소 중입니다.";
            //lblConfrimMsg4.Content = "잠시만 기다려 주십시요 ! ";

            DialogExitValue = false;
            if (showType == 0)
            {
                //((MainWindow)Application.Current.MainWindow).RemarkFlag = 0;
                //if (cancelEvent != null)
                //    cancelEvent(this, EventArgs.Empty);
            }
            else
            {
                DialogResult = false;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogExitValue = true;
            if (showType == 1)
                DialogResult = true;
            else
                Close();
        }
    }

    public class ConfirmWindowString
    {
        public string Message;
        public double Fontsize;
        public Brush Foreground;
        public Brush Background;
        public HorizontalAlignment HorizontalContentAlignment;
        public VerticalAlignment VerticalContentAlignment;

        public ConfirmWindowString()
        {
            Message = "";
            Fontsize = 12;
            Foreground = Brushes.Black;
            Background = Brushes.Transparent;
            HorizontalContentAlignment = HorizontalAlignment.Left;
            VerticalContentAlignment = VerticalAlignment.Top;
        }
    }

}
