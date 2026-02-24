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
using System.Reflection;
using System.Data;
using System.Threading;


#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// ErrorListWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        public ErrorListWindow()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가

            dpkEndDate.DisplayDate = DateTime.Now;
            dpkEndDate.SelectedDate = DateTime.Now;
            DateTime dt = DateTime.Now.AddMonths(-1);
            dt = dt.AddDays(1);
            dpkStartDate.SelectedDate = dt;
            dpkStartDate.DisplayDate = dt;

            DateTime beginTime = dpkStartDate.SelectedDate.Value;
            DateTime EndTime = dpkEndDate.SelectedDate.Value;
            ShowErrorList(beginTime, EndTime);
        }

        private void ShowErrorList(DateTime beginTime, DateTime EndTime)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTDBManage dbmanager = new ITNTDBManage(Constants.connstring_error);
            DataTable dbDataTable = new DataTable();
            try
            {
                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).ErrorDBLock)
                {
                    string start = beginTime.ToString("yyyy-MM-dd");
                    string end = EndTime.ToString("yyyy-MM-dd");

                    string searchstring = string.Format("select * from errortable where (DATE(Date) between '" + start + "' AND '" + end + "') order by no desc");

                    ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                    object obj = new object();
                    dbwrap.ExecuteCommand(Constants.connstring_error, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbDataTable, ref obj);

                    //dbmanager.Open(Constants.connstring_error);
                    //dbmanager.CommandText = searchstring;

                    //dbmanager.ExecuteCommandReader(CommandTypeEnum.Text, ref dbDataTable);
                    //dbmanager.Close();
                }

                dgdErrorList.ItemsSource = dbDataTable.DefaultView;
                dgdErrorList.Items.Refresh();

                if (dgdErrorList.Items.Count > 0)
                    dgdErrorList.SelectedIndex = 0;
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DB EXCEPTION : CODE = {0:X}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  DB EXCEPTION : CODE = {2:X}, MSG = {3}", className, funcName, de.HResult, de.Message, Thread.CurrentThread.ManagedThreadId);
                //dbmanager.Close();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DB EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  EXCEPTION : CODE = {2:X}, MSG = {3}", className, funcName, ex.HResult, ex.Message, Thread.CurrentThread.ManagedThreadId);
                //dbmanager.Close();
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            DateTime beginTime = dpkStartDate.SelectedDate.Value;
            DateTime EndTime = dpkEndDate.SelectedDate.Value;
            ShowErrorList(beginTime, EndTime);
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void btnSearchAll_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                DataTable dt = new DataTable();
                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).ErrorDBLock)
                {
                    string searchstring = string.Format("select * from errortable ORDER BY NO DESC");
                    ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                    object obj = new object();
                    dbwrap.ExecuteCommand(Constants.connstring_error, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);
                }

                dgdErrorList.ItemsSource = dt.DefaultView;
                dgdErrorList.Items.Refresh();
                dgdErrorList.SelectedIndex = 0;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            //var selectedItem = dgdErrorList.SelectedItem;
            //if (selectedItem != null)
            //{
            //    dgdErrorList.Items.Remove(selectedItem);
            //}
            DeleteDataRemarkList();
        }

        private void btnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            //ConfirmWindowString msg5 = new ConfirmWindowString();

            if (dgdErrorList.Items.Count <= 0)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NO DATA, NO SELECT", Thread.CurrentThread.ManagedThreadId);
                return;
            }

            try
            {
                int selectedinex = dgdErrorList.Items.Count;

                msg2.Message = "Do you want to delete all data?";
                msg2.Fontsize = 16;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                ConfirmWindow window = new ConfirmWindow("DELETE", msg1, msg2, msg3, msg4, "YES", "NO", this);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (window.ShowDialog() == false)
                    return;

                DataTable dt = new DataTable();
                object obj = new object();
                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).ErrorDBLock)
                {
                    string deletestring = "DELETE FROM errortable";
                    string searchstring = string.Format("select * from errortable");
                    DataTable dbMainDataTable = dgdErrorList.ItemsSource as DataTable;
                    ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                    dbwrap.ExecuteCommand(Constants.connstring_error, deletestring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dt, ref obj);
                    dbwrap.ExecuteCommand(Constants.connstring_error, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);
                }

                dgdErrorList.ItemsSource = dt.DefaultView;
                dgdErrorList.Items.Refresh();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }


        private void DeleteDataRemarkList()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            if ((dgdErrorList.Items.Count <= 0) || (dgdErrorList.SelectedIndex < 0))
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NO DATA, NO SELECT", Thread.CurrentThread.ManagedThreadId);
                return;
            }

            try
            {
                int selectedinex = dgdErrorList.Items.Count;

                msg2.Message = "Do you want to delete selected data?";
                msg2.Fontsize = 16;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                ConfirmWindow window = new ConfirmWindow("DELETE", msg1, msg2, msg3, msg4, "YES", "NO", this);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (window.ShowDialog() == false)
                    return;
                //DataRowView drv = (DataRowView)dgdErrorList.SelectedItems;
                //DataGridItem list = dgdErrorList.SelectedItems;
                //List<DataRowView> selNo = new List<DataRowView>();
                List<int> selIdx = new List<int>();
                List<DataRowView> selRows = new List<DataRowView>();
                foreach (DataRowView selrow in dgdErrorList.SelectedItems)
                {
                    selIdx.Add(dgdErrorList.SelectedIndex);
                    selRows.Add(selrow);
                    int no = (int)selrow.Row.ItemArray[0];
                    //selrow.Row.Delete();
                    //lock (((MainWindow)System.Windows.Application.Current.MainWindow).ErrorDBLock)
                    {
                        string deletestring = "DELETE FROM errortable WHERE NO='" + no.ToString() + "'";
                        DataTable dbMainDataTable = dgdErrorList.ItemsSource as DataTable;
                        ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                        DataTable dt = new DataTable();
                        object obj = new object();
                        dbwrap.ExecuteCommand(Constants.connstring_error, deletestring, CommandMode.NonQuery, CommandTypeEnum.Text, ref dt, ref obj);
                    }
                }

                for (int i = 0; i < selRows.Count; i++)
                {
                    selRows[i].Row.Delete();
                }
                //dgdErrorList.ItemsSource = RemarkDataList;
                //dgdErrorList.Items.Refresh();

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);

                if (dgdErrorList.Items.Count > selectedinex)
                    dgdErrorList.SelectedIndex = selectedinex;
                else if (dgdErrorList.Items.Count >= 0)
                    dgdErrorList.SelectedIndex = dgdErrorList.Items.Count - 1;
                else
                    dgdErrorList.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
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
    }
}
