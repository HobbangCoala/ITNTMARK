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
using System.Reflection;
using System.Data;
using ITNTCOMMON;
using ITNTUTIL;
using System.Threading;


#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// SearchCompleteWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SearchCompleteWindow : Window
    {
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();

        public SearchCompleteWindow()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            //rbtnSearchAllPrintedType.IsChecked = true;
            //rbtnSearchNormalPrintedType.IsChecked = false;
            //rbtnSearchRePrintedType.IsChecked = false;

            dpkStartDate.SelectedDate = DateTime.Now.AddDays(-30);
            dpkStartDate.DisplayDate = DateTime.Now.AddDays(-30);
            //RemarkDataList = new List<MarkDataRowView>();
            //RemarkDataList.Clear();
            ShowReport();
        }

        public SearchCompleteWindow(byte type)
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            //rbtnSearchAllPrintedType.IsChecked = true;
            //rbtnSearchNormalPrintedType.IsChecked = false;
            //rbtnSearchRePrintedType.IsChecked = false;

            dpkStartDate.SelectedDate = DateTime.Now.AddDays(-30);
            dpkStartDate.DisplayDate = DateTime.Now.AddDays(-30);
            ShowReport();
        }

        void ShowReport()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;

            //ITNTDBManage dbmanager = new ITNTDBManage(Constants.connstring_comp);
            //DataTable dbDataTable = new DataTable();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            //object obj = new DataTable();
            string value = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //dbmanager.CommandText = "select * from completetable";
                ////lock (((MainWindow)System.Windows.Application.Current.MainWindow).CompDBLock)
                //{
                //    dbmanager.Open(Constants.connstring_comp);
                //    dbmanager.ExecuteCommandReader(CommandTypeEnum.Text, ref dbDataTable);
                //    dbmanager.Close();
                //}
                //dbwrap.ExecuteCommand(Constants.connstring_comp, "select * from completetable", CommandMode.Reader, CommandTypeEnum.Text, ref dbDataTable, ref obj);

                //dgdComplete.ItemsSource = dbDataTable.DefaultView;
                //dgdComplete.Items.Refresh();

                //lblCompDataCount.Content = dgdComplete.Items.Count.ToString();
                //if (dgdComplete.Items.Count > 0)
                //    dgdComplete.SelectedIndex = 0;

                cbxSearchTypeCombo.Items.Add("All");
                Util.GetPrivateProfileValue("CARTYPE", "TYPE", "SP|AY|QY|KY", ref value, Constants.PARAMS_INI_FILE);
                string[] vals = value.Split('|');
                for (int i = 0; i < vals.Length; i++)
                {
                    if (vals[i].Length > 0)
                        cbxSearchTypeCombo.Items.Add(vals[i]);
                }
                cbxSearchTypeCombo.SelectedIndex = 0;
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  SetFullSize DB Exception : {3}, {4}", className, funcName, de.HResult, de.Message, Thread.CurrentThread.ManagedThreadId);
                //dbmanager.Close();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  SetFullSize Exception : {3}, {4}", className, funcName, ex.HResult, ex.Message, Thread.CurrentThread.ManagedThreadId);
                //dbmanager.Close();
            }

            //if (dbDataTable.Rows.Count > 0)
            //{
            //    string temp = "";
            //    foreach (DataRow row in dbDataTable.Rows)
            //    {
            //        temp = row["RAWCARTYPE"].ToString().Trim();
            //        if (!cbxSearchTypeCombo.Items.Contains(temp))
            //        {
            //            cbxSearchTypeCombo.Items.Add(temp);
            //        }
            //    }
            //}

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        /*******************************************************************************/
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
        /*******************************************************************************/

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();

            try
            {
                //ITNTDBManage compDB = new ITNTDBManage(Constants.connstring_comp);
                //string selcartype = "";

                string selvinno = "";
                if (tbxSearchVIN.Text.Length > 0)
                    selvinno = tbxSearchVIN.Text;

                DateTime beginTime = dpkStartDate.SelectedDate.Value;
                DateTime EndTime = dpkEndDate.SelectedDate.Value;
                DateTime now = DateTime.Now;
                DataTable dt = new DataTable();

                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).CompDBLock)
                {
                    //compDB.Open(Constants.connstring_comp);
                    string start = beginTime.ToString("yyyy-MM-dd");
                    string end = EndTime.ToString("yyyy-MM-dd");

                    string searchstring = string.Format("select * from completetable where (DATE(MARKDATE) between '" + start + "' AND '" + end + "')");

                    if (cbxSearchTypeCombo.SelectedIndex > 0)
                        searchstring += " and RAWCARTYPE = '" + cbxSearchTypeCombo.SelectedItem.ToString() + "'";

                    if (selvinno.Length > 0)
                    {
                        if (cbxSelPartialVIN.IsChecked == true)
                            searchstring += " and VIN LIKE '%" + selvinno + "%'";
                        else
                            searchstring += " and VIN = '" + selvinno + "'";
                    }

                    dbwrap.ExecuteCommand(Constants.connstring_comp, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);

                    ////if (rbtnSearchNormalPrintedType.IsChecked == true)
                    ////    searchstring += " and REMARK = 'N'";
                    ////else if (rbtnSearchRePrintedType.IsChecked == true)
                    ////    searchstring += " and REMARK = 'Y'";

                    //compDB.CommandText = searchstring;
                    //compDB.ExecuteCommandReader(CommandTypeEnum.Text, ref dt);
                    //compDB.Close();
                }

                lblCompDataCount.Content = dt.Rows.Count.ToString();

                dgdComplete.ItemsSource = dt.DefaultView;
                dgdComplete.Items.Refresh();

                if (dgdComplete.Items.Count > 0)
                    dgdComplete.SelectedIndex = 0;
                //dgdComplete.SelectedItem = 0;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("DB ERROR " + ex.Message);
                string ErrorCode = string.Format("00RE{0:X8}", Math.Abs(ex.HResult));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnallSearch_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new DataTable();

            try
            {
                DataTable dt = new DataTable();
                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).CompDBLock)
                {
                    //ITNTDBManage compDB = new ITNTDBManage(Constants.connstring_comp);
                    dbwrap.ExecuteCommand(Constants.connstring_comp, "select * from completetable", CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);

                    //compDB.Open(Constants.connstring_comp);
                    //string searchstring = string.Format("select * from completetable");
                    ////searchstring += "order by no desc";
                    //compDB.CommandText = searchstring;

                    //compDB.ExecuteCommandReader(CommandTypeEnum.Text, ref dt);
                    //compDB.Close();
                }

                lblCompDataCount.Content = dt.Rows.Count.ToString();

                dgdComplete.ItemsSource = dt.DefaultView;
                dgdComplete.Items.Refresh();
                dgdComplete.SelectedIndex = 0;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        private void btnSearchExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
