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
    /// ConfirmWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConfirmWindow : Window
    {
        public ConfirmWindow()
        {
            InitializeComponent();
        }

        public ConfirmWindow(string title, string msg1, string msg2, string msg3, string msg4, Window owner)
        {
            string className = "ConfirmWindow";
            string funcName = "ConfirmWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = owner;

            lblConfrimMsg1.Content = msg1;
            lblConfrimMsg2.Content = msg2;
            lblConfrimMsg3.Content = msg3;
            lblConfrimMsg4.Content = msg4;

            btnOK2.Visibility = Visibility.Hidden;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public ConfirmWindow(string title, ConfirmWindowString msg1, ConfirmWindowString msg2, ConfirmWindowString msg3, ConfirmWindowString msg4, string btnOKText, string btnNOText, Window owner)
        {
            string className = "ConfirmWindow";
            string funcName = "ConfirmWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = owner;

            Title = title;

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

            btnOK.Content = btnOKText;
            btnCancel.Content = btnNOText;

            btnOK2.Visibility = Visibility.Hidden;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public ConfirmWindow(string ColorString, string title, string msg1, string msg2, string msg3, string msg4, Window owner)
        {
            string className = "ConfirmWindow";
            string funcName = "ConfirmWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = owner;

            Color color = (Color)ColorConverter.ConvertFromString(ColorString);
            this.Background = new SolidColorBrush(color);

            lblConfrimMsg1.Content = msg1;
            lblConfrimMsg2.Content = msg2;
            lblConfrimMsg3.Content = msg3;
            lblConfrimMsg4.Content = msg4;

            btnOK2.Visibility = Visibility.Hidden;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public ConfirmWindow(string title, string msg1, string msg2, string msg3, string msg4)
        {
            string className = "ConfirmWindow";
            string funcName = "ConfirmWindow";
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

            this.Title = title;

            btnOK2.Content = "OK";
            btnOK2.Visibility = Visibility.Visible;
            btnOK.Visibility = Visibility.Hidden;
            btnCancel.Visibility = Visibility.Hidden;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public ConfirmWindow(string title, ConfirmWindowString msg1, ConfirmWindowString msg2, ConfirmWindowString msg3, ConfirmWindowString msg4, string btnOKText, string btnNOText, string btnOK2Text, Window owner)
        {
            string className = "ConfirmWindow";
            string funcName = "ConfirmWindow";
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            InitializeComponent();
            this.Owner = owner;

            if (btnOKText.Length <= 0)
                btnOK.Visibility = Visibility.Hidden;
            else
            {
                btnOK.Visibility = Visibility.Visible;
                btnOK.Content = btnOKText;
            }

            if (btnNOText.Length <= 0)
                btnCancel.Visibility = Visibility.Hidden;
            else
            {
                btnCancel.Content = btnNOText;
                btnCancel.Visibility = Visibility.Visible;
            }

            if (btnOK2Text.Length <= 0)
                btnOK2.Visibility = Visibility.Hidden;
            else
            {
                btnOK2.Content = btnOK2Text;
                btnOK2.Visibility = Visibility.Visible;
            }

            Title = title;

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

            btnOK.Content = btnOKText;
            btnCancel.Content = btnNOText;

            //btnOK2.Visibility = Visibility.Hidden;

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }

}
