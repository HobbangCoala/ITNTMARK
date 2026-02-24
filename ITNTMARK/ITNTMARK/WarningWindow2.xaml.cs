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
using System.Threading;

#pragma warning disable 1998
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// WarningWindow2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WarningWindow2 : Window
    {
        public enum DialogResultType { Retry, Continue, Cancel }
        public DialogResultType Result { get; private set; }

        public WarningWindow2()
        {
            InitializeComponent();
        }

        public WarningWindow2(string title, ConfirmWindowString msg1, ConfirmWindowString msg3, ConfirmWindowString msg4, ConfirmWindowString msg5, string btnText1, string btnText2, string btnText3, Window owner)
        {
            string className = "WarningWindow";
            string funcName = "WarningWindow";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                InitializeComponent();

                this.Owner = owner;
                this.Title = title;

                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                //lblConfrimMsg1.Foreground = msg1.Foreground;
                //lblConfrimMsg1.Background = msg1.Background;
                //lblConfrimMsg1.HorizontalContentAlignment = msg1.HorizontalContentAlignment;
                //lblConfrimMsg1.VerticalContentAlignment = msg1.VerticalContentAlignment;
                //lblConfrimMsg1.FontSize = msg1.Fontsize;
                //lblConfrimMsg1.Content = msg1.Message;

                //lblConfrimMsg2.Foreground = msg2.Foreground;
                //lblConfrimMsg2.Background = msg2.Background;
                //lblConfrimMsg2.HorizontalContentAlignment = msg2.HorizontalContentAlignment;
                //lblConfrimMsg2.VerticalContentAlignment = msg2.VerticalContentAlignment;
                //lblConfrimMsg2.FontSize = msg2.Fontsize;
                //lblConfrimMsg2.Content = msg2.Message;

                tbkMessage1.Foreground = msg1.Foreground;
                tbkMessage1.Background = msg1.Background;
                tbkMessage1.TextAlignment = TextAlignment.Center;
                tbkMessage1.FontSize = msg1.Fontsize;
                tbkMessage1.Text = msg1.Message;

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


                if (btnText1.Length > 0)
                {
                    btnRetry.Content = btnText1;
                    btnRetry.Visibility = Visibility.Visible;
                }
                else
                {
                    btnRetry.Visibility = Visibility.Collapsed;
                }

                if (btnText2.Length > 0)
                {
                    btnContinue.Content = btnText2;
                    btnContinue.Visibility = Visibility.Visible;
                    lblSpace1.Visibility = Visibility.Visible;
                }
                else
                {
                    btnContinue.Visibility = Visibility.Collapsed;
                    lblSpace1.Visibility = Visibility.Collapsed;
                }

                if (btnText3.Length > 0)
                {
                    btnCancel.Content = btnText3;
                    btnCancel.Visibility = Visibility.Visible;
                    lblSpace2.Visibility = Visibility.Visible;
                }
                else
                {
                    btnCancel.Visibility = Visibility.Collapsed;
                    lblSpace2.Visibility = Visibility.Collapsed;
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {

            }

        }

        public WarningWindow2(string title, ConfirmWindowString msg1, ConfirmWindowString msg3, ConfirmWindowString msg4, ConfirmWindowString msg5, string btnText1, string btnText2, string btnText3)
        {
            string className = "WarningWindow";
            string funcName = "WarningWindow";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                InitializeComponent();

                //this.Owner = owner;
                //this.Title = title;

                //lblConfrimMsg1.Foreground = msg1.Foreground;
                //lblConfrimMsg1.Background = msg1.Background;
                //lblConfrimMsg1.HorizontalContentAlignment = msg1.HorizontalContentAlignment;
                //lblConfrimMsg1.VerticalContentAlignment = msg1.VerticalContentAlignment;
                //lblConfrimMsg1.FontSize = msg1.Fontsize;
                //lblConfrimMsg1.Content = msg1.Message;

                //lblConfrimMsg2.Foreground = msg2.Foreground;
                //lblConfrimMsg2.Background = msg2.Background;
                //lblConfrimMsg2.HorizontalContentAlignment = msg2.HorizontalContentAlignment;
                //lblConfrimMsg2.VerticalContentAlignment = msg2.VerticalContentAlignment;
                //lblConfrimMsg2.FontSize = msg2.Fontsize;
                //lblConfrimMsg2.Content = msg2.Message;

                tbkMessage1.Foreground = msg1.Foreground;
                tbkMessage1.Background = msg1.Background;
                tbkMessage1.TextAlignment = TextAlignment.Center;
                tbkMessage1.FontSize = msg1.Fontsize;
                tbkMessage1.Text = msg1.Message;


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


                if (btnText1.Length > 0)
                {
                    btnRetry.Content = btnText1;
                    btnRetry.Visibility = Visibility.Visible;
                }
                else
                {
                    btnRetry.Visibility = Visibility.Collapsed;
                }

                if (btnText2.Length > 0)
                {
                    btnContinue.Content = btnText2;
                    btnContinue.Visibility = Visibility.Visible;
                    lblSpace1.Visibility = Visibility.Visible;
                }
                else
                {
                    btnContinue.Visibility = Visibility.Collapsed;
                    lblSpace1.Visibility = Visibility.Collapsed;
                }

                if (btnText3.Length > 0)
                {
                    btnCancel.Content = btnText3;
                    btnCancel.Visibility = Visibility.Visible;
                    lblSpace2.Visibility = Visibility.Visible;
                }
                else
                {
                    btnCancel.Visibility = Visibility.Collapsed;
                    lblSpace2.Visibility = Visibility.Collapsed;
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {

            }

        }

        private void btnRetry_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResultType.Retry;
            this.DialogResult = true;
            Close();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResultType.Continue;
            this.DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResultType.Cancel;
            this.DialogResult = true;
            Close();
        }
    }
}
