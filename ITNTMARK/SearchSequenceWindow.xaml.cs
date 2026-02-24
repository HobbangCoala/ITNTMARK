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
using System.Threading;
using ITNTCOMMON;
using ITNTUTIL;
using System.Data;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// Interaction logic for SearchSequenceWindow.xaml
    /// </summary>
    public partial class SearchSequenceWindow : Window
    {
        public SearchSequenceWindow()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }


        private void SetLabelValue(Label label, string value, Brush forecolor)
        {
            if (label == null)
                return;

            if (label.CheckAccess() == true)
            {
                if (forecolor != null)
                    label.Foreground = forecolor;
                label.Content = value;
            }
            else
            {
                label.Dispatcher.Invoke(new Action(delegate
                {
                    if (forecolor != null)
                        label.Foreground = forecolor;
                    label.Content = value;
                }));
            }
        }

        /// ///////////////////////////////////////////////////////////////////////
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnGo2SearchData_Click(object sender, RoutedEventArgs e)
        {
            string className = "SearchSequenceWindow";
            string funcName = "SetMarkPoint";

            DataRowView rowview = null;

            try
            {
                if (dgSearch.Items.Count <= 0)
                {
                    //lblMessage.Content = "데이터가 없습니다";
                    SetLabelValue(lblMessage, "THERE IS NO DATA", Brushes.Red);
                    return;
                }

                if (dgSearch.SelectedIndex < 0)
                {
                    //lblMessage.Content = "데이터를 선택해주세요";
                    SetLabelValue(lblMessage, "SELECT DATA", Brushes.Red);
                    return;
                }

                rowview = dgSearch.SelectedItem as DataRowView;
                if (rowview == null)
                {
                    return;
                }

                ((MainWindow)System.Windows.Application.Current.MainWindow).ShowSelectedPlanData(((MainWindow)System.Windows.Application.Current.MainWindow).dgdPlanData, rowview);
            }
            catch (Exception ex)
            {
                SetLabelValue(lblMessage, "EXCEPTION - " + ex.Message, Brushes.Red);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}/{0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnSearchBySeq_Click(object sender, RoutedEventArgs e)
        {
            string className = "SearchSequenceWindow";
            string funcName = "btnSearchBySeq_Click";

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            string commandstring = "";
            DataTable datatable = new DataTable();
            object obj = new object();
            //int iseq = 0;
            string seqstring = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                lblMessage.Content = "";

                if (tbxSeq.Text.Length <= 0)
                {
                    SetLabelValue(lblMessage, "ENTER SEQUENCE", Brushes.Red);
                    return;
                }

                //int.TryParse(tbxSeq.Text, out iseq);
                //seqstring = string.Format("{0:D5}", iseq);
                if (tbxSeq.Text.Length > 4)
                    seqstring = tbxSeq.Text.Substring(4);
                else if (tbxSeq.Text.Length == 4)
                    seqstring = tbxSeq.Text;
                else
                    seqstring = tbxSeq.Text.PadLeft(4, '0');

                commandstring = "SELECT * from " + ((MainWindow)System.Windows.Application.Current.MainWindow).tableName + " WHERE SEQUENCE='" + seqstring + "' ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
                dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.Reader, CommandTypeEnum.Text, ref datatable, ref obj);
                dgSearch.ItemsSource = datatable.DefaultView;
                dgSearch.Items.Refresh();
                if (dgSearch.Items.Count > 0)
                {
                    dgSearch.SelectedIndex = 0;
                    dgSearch.UpdateLayout();
                    SetLabelValue(lblSearchCount, dgSearch.Items.Count.ToString(), null);
                }
                else
                {
                    SetLabelValue(lblMessage, "NO DATA FOUND", Brushes.Red);
                    SetLabelValue(lblSearchCount, "0", null);
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                SetLabelValue(lblMessage, "EXCEPTION - " + ex.Message, Brushes.Red);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}/{0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

        }

        private void btnSearchByVin_Click(object sender, RoutedEventArgs e)
        {
            string className = "SearchSequenceWindow";
            string funcName = "btnSearchByVin_Click";

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            string commandstring = "";
            DataTable datatable = new DataTable();
            object obj = new object();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                lblMessage.Content = "";

                if (tbxVin.Text.Length <= 0)
                {
                    SetLabelValue(lblMessage, "ENTER VIN", Brushes.Red);
                    return;
                }

                if (cbxPartialComapre.IsChecked == true)
                    commandstring = "SELECT * from " + ((MainWindow)System.Windows.Application.Current.MainWindow).tableName + " WHERE VIN LIKE '%" + tbxVin.Text + "%' ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
                else
                    commandstring = "SELECT * from " + ((MainWindow)System.Windows.Application.Current.MainWindow).tableName + " WHERE VIN='" + tbxVin.Text + "' ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
                dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.Reader, CommandTypeEnum.Text, ref datatable, ref obj);
                dgSearch.ItemsSource = datatable.DefaultView;
                dgSearch.Items.Refresh();
                if (dgSearch.Items.Count > 0)
                {
                    dgSearch.SelectedIndex = 0;
                    dgSearch.UpdateLayout();
                    SetLabelValue(lblSearchCount, dgSearch.Items.Count.ToString(), null);
                }
                else
                {
                    SetLabelValue(lblMessage, "NO DATA FOUND", Brushes.Red);
                    SetLabelValue(lblSearchCount, "0", null);
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                SetLabelValue(lblMessage, "EXCEPTION - " + ex.Message, Brushes.Red);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}/{0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

        }

        private void btnSearchBySeqVin_Click(object sender, RoutedEventArgs e)
        {
            string className = "SearchSequenceWindow";
            string funcName = "btnSearchBySeqVin_Click";

            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            string commandstring = "";
            DataTable datatable = new DataTable();
            object obj = new object();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                lblMessage.Content = "";

                if (tbxSeq.Text.Length <= 0)
                {
                    //lblMessage.Foreground = Brushes.Red;
                    //lblMessage.Content = "서열번호를 입력하여 주세요";
                    SetLabelValue(lblMessage, "ENTER SEQUENCE", Brushes.Red);
                    return;
                }

                if (tbxVin.Text.Length <= 0)
                {
                    //lblMessage.Foreground = Brushes.Red;
                    //lblMessage.Content = "차대번호를 입력하여 주세요";
                    SetLabelValue(lblMessage, "ENTER VIN", Brushes.Red);
                    return;
                }


                if (cbxPartialComapre.IsChecked == true)
                    commandstring = "SELECT * from " + ((MainWindow)System.Windows.Application.Current.MainWindow).tableName + " WHERE VIN LIKE '%" + tbxVin.Text + "%" + "' AND SEQUENCE='" + tbxSeq.Text + "' ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";
                else
                    commandstring = "SELECT * from " + ((MainWindow)System.Windows.Application.Current.MainWindow).tableName + " WHERE VIN='" + tbxVin.Text + "' AND SEQUENCE='" + tbxSeq.Text + "' ORDER BY DATE(PRODUCTDATE) ASC, SEQUENCE ASC";

                dbwrap.ExecuteCommand(Constants.connstring, commandstring, CommandMode.Reader, CommandTypeEnum.Text, ref datatable, ref obj);
                dgSearch.ItemsSource = datatable.DefaultView;
                dgSearch.Items.Refresh();
                if (dgSearch.Items.Count > 0)
                {
                    dgSearch.SelectedIndex = 0;
                    dgSearch.UpdateLayout();
                    SetLabelValue(lblSearchCount, dgSearch.Items.Count.ToString(), null);
                }
                else
                {
                    //lblMessage.Foreground = Brushes.Red;
                    //lblMessage.Content = "찾는 데이터가 없습니다";
                    SetLabelValue(lblMessage, "NO DATA FOUND", Brushes.Red);
                    SetLabelValue(lblSearchCount, "0", null);
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                SetLabelValue(lblMessage, "EXCEPTION - " + ex.Message, Brushes.Red);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}/{0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

        }
    }
}
