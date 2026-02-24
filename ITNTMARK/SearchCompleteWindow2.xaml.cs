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

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014


namespace ITNTMARK
{
    /// <summary>
    /// SearchCompleteWindow2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SearchCompleteWindow2 : Window
    {
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();
        private CancellationTokenSource _cts;

        public SearchCompleteWindow2()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            SetCompletetDataGrid();
            //rbtnSearchAllPrintedType.IsChecked = true;
            //rbtnSearchNormalPrintedType.IsChecked = false;
            //rbtnSearchRePrintedType.IsChecked = false;

            dpkStartDate.SelectedDate = DateTime.Now.AddDays(-30);
            dpkStartDate.DisplayDate = DateTime.Now.AddDays(-30);
            //RemarkDataList = new List<MarkDataRowView>();
            //RemarkDataList.Clear();
            ShowReport();
        }


        public SearchCompleteWindow2(byte type)
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            SetCompletetDataGrid();
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
            string value = "";

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //ITNTDBManage dbmanager = new ITNTDBManage(Constants.connstring_comp);
            DataTable dbDataTable = new DataTable();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            string commandstring = "";
            DateTime beginTime = dpkStartDate.SelectedDate.Value;
            DateTime EndTime = dpkEndDate.SelectedDate.Value;

            string start = beginTime.ToString("yyyy-MM-dd");
            string end = EndTime.ToString("yyyy-MM-dd");

            try
            {
                commandstring = "select * from completetable where (DATE(MARKDATE) between '" + start + "' AND '" + end + "') ORDER BY NO DESC";
                dbwrap.ExecuteCommand(Constants.connstring_comp, commandstring, CommandMode.Reader, CommandTypeEnum.Text, ref dbDataTable, ref obj);

                //dbmanager.CommandText = "select * from completetable";
                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).CompDBLock)
                //{
                //    dbmanager.Open(Constants.connstring_comp);
                //    dbmanager.ExecuteCommandReader(CommandTypeEnum.Text, ref dbDataTable);
                //    dbmanager.Close();
                //}

                dgdComplete.ItemsSource = dbDataTable.DefaultView;
                dgdComplete.Items.Refresh();

                lblCompDataCount.Content = dgdComplete.Items.Count.ToString();
                if (dgdComplete.Items.Count > 0)
                    dgdComplete.SelectedIndex = 0;

                cbxSearchTypeCombo.Items.Add("All");
                cbxSearchTypeCombo.SelectedIndex = 0;

                Util.GetPrivateProfileValue("CARTYPE", "TYPE", "", ref value, Constants.PARAMS_INI_FILE);
                string[] vals = value.Split('|');
                for (int i = 0; i < vals.Length; i++)
                {
                    if (vals[i].Length > 0)
                        cbxSearchTypeCombo.Items.Add(vals[i].Trim());
                }
            }
            catch (DataException de)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("DB EXCEPTION - CODE = {0:X}, MSG = {1}", de.HResult, de.Message), Thread.CurrentThread.ManagedThreadId);
                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  SetFullSize DB Exception : {3}, {4}", className, funcName, de.HResult, de.Message);
                //dbmanager.Close();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  SetFullSize Exception : {3}, {4}", className, funcName, ex.HResult, ex.Message);
                //dbmanager.Close();
            }

            //if (dbDataTable.Rows.Count > 0)
            //{
            //    string temp = "";
            //    foreach (DataRow row in dbDataTable.Rows)
            //    {
            //        temp = row["CARTYPE"].ToString().Trim();
            //        if (!cbxSearchTypeCombo.Items.Contains(temp))
            //        {
            //            cbxSearchTypeCombo.Items.Add(temp);
            //        }
            //    }
            //}

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        void SetCompletetDataGrid()
        {
            string value = "";
            string header = "";
            int count = 0;
            string name = "";
            int colsize = 0;
            string binding = "";
            int totcount = 0;
            string format = "";

            try
            {
                totcount = dgdComplete.Columns.Count;

                Util.GetPrivateProfileValue("SETTING", "COUNT", "4", ref value, "./Parameter/SearchDataGrid.ini");
                int.TryParse(value, out count);

                for (int i = 0; i < count; i++)
                {
                    Util.GetPrivateProfileValue(i.ToString(), "NAME", "SEQUENCE", ref name, "./Parameter/SearchDataGrid.ini");
                    Util.GetPrivateProfileValue(i.ToString(), "SIZE", "4", ref value, "./Parameter/SearchDataGrid.ini");
                    int.TryParse(value, out colsize);

                    Util.GetPrivateProfileValue(i.ToString(), "BIND", "SEQUENCE", ref binding, "./Parameter/SearchDataGrid.ini");
                    Util.GetPrivateProfileValue(i.ToString(), "FORMAT", "", ref format, "./Parameter/SearchDataGrid.ini");
                    //Util.GetPrivateProfileValue(i.ToString(), "NAME", "SEQUENCE", ref name, "./Parameter/NextDataGrid");

                    // 특정 인덱스의 컬럼을 가져옵니다.
                    var column = dgdComplete.Columns[i]; // 또는 원하는 인덱스

                    // 1. 헤더 이름 변경
                    column.Header = name;

                    // 비율 기반 너비
                    column.Width = new DataGridLength(colsize, DataGridLengthUnitType.Star);

                    // 3. 바인딩 변경 (텍스트 컬럼일 때만 가능)
                    if (column is DataGridTextColumn textColumn)
                    {
                        if (format.Length > 0)
                        {
                            textColumn.Binding = new Binding(binding)
                            {
                                StringFormat = format,
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                            };
                        }
                        else
                        {
                            textColumn.Binding = new Binding(binding)
                            {
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                            };
                        }
                    }

                    column.Visibility = Visibility.Visible;
                }

                for (int k = count; k < totcount; k++)
                {
                    var column = dgdComplete.Columns[k];
                    column.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {

            }
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

        private DataTable LoadDataTableFromMySQL(string searchstring, CancellationToken token)
        {
            //var dataTable = new DataTable();
            //DateTime beginTime = dpkStartDate.SelectedDate.Value;
            //DateTime EndTime = dpkEndDate.SelectedDate.Value;
            //DateTime now = DateTime.Now;
            DataTable dt = new DataTable();
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();

            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            //string selvinno = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                dbwrap.ExecuteCommand(Constants.connstring_comp, searchstring, CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);

                token.ThrowIfCancellationRequested();

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return dt;
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("DB ERROR " + ex.Message);
                string ErrorCode = string.Format("00RE{0:X8}", Math.Abs(ex.HResult));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                token.ThrowIfCancellationRequested();
                return dt;
            }
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string selvinno = "";
            DateTime beginTime = dpkStartDate.SelectedDate.Value;
            DateTime EndTime = dpkEndDate.SelectedDate.Value;
            DateTime now = DateTime.Now;

            try
            {
                _cts = new CancellationTokenSource();
                var loadingWindow = new LoadingWindow();
                loadingWindow.Owner = this;
                loadingWindow.Canceled += () => _cts.Cancel();

                loadingWindow.Show();

                try
                {
                    if (tbxSearchVIN.Text.Length > 0)
                        selvinno = tbxSearchVIN.Text;

                    //compDB.Open(Constants.connstring_comp);
                    string start = beginTime.ToString("yyyy-MM-dd");
                    string end = EndTime.ToString("yyyy-MM-dd");

                    string searchstring = string.Format("select * from completetable where (DATE(MARKDATE) between '" + start + "' AND '" + end + "')");

                    if (cbxSearchTypeCombo.SelectedIndex > 0)
                        searchstring += " and CARTYPE = '" + cbxSearchTypeCombo.SelectedItem.ToString() + "'";

                    if (selvinno.Length > 0)
                    {
                        if (cbxSelPartialVIN.IsChecked == true)
                            searchstring += " and VIN LIKE '%" + selvinno + "%'";
                        else
                            searchstring += " and VIN = '" + selvinno + "'";
                    }

                    searchstring += " ORDER BY NO DESC";
                    var dataTable = await Task.Run(() => LoadDataTableFromMySQL(searchstring, _cts.Token));
                    Dispatcher.Invoke(() =>
                    {
                        dgdComplete.ItemsSource = dataTable.DefaultView;
                        lblCompDataCount.Content = dataTable.Rows.Count.ToString();
                    });
                }
                //catch (OperationCanceledException)
                //{
                //    MessageBox.Show("User cancled.");
                //}
                catch (Exception ex)
                {
                    //MessageBox.Show("DB ERROR: " + ex.Message);
                }
                finally
                {
                    loadingWindow.Close();
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("DB ERROR " + ex.Message);
                string ErrorCode = string.Format("00RE{0:X8}", Math.Abs(ex.HResult));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSearch_Click22(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            string selvinno = "";

            try
            {
                //ITNTDBManage compDB = new ITNTDBManage(Constants.connstring_comp);
                //string selcartype = "";
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if (tbxSearchVIN.Text.Length > 0)
                    selvinno = tbxSearchVIN.Text;

                DateTime beginTime = dpkStartDate.SelectedDate.Value;
                DateTime EndTime = dpkEndDate.SelectedDate.Value;
                DateTime now = DateTime.Now;
                DataTable dt = new DataTable();
                ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                object obj = new object();

                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).CompDBLock)
                {
                    //compDB.Open(Constants.connstring_comp);
                    string start = beginTime.ToString("yyyy-MM-dd");
                    string end = EndTime.ToString("yyyy-MM-dd");

                    string searchstring = string.Format("select * from completetable where (DATE(MARKDATE) between '" + start + "' AND '" + end + "')");

                    if (cbxSearchTypeCombo.SelectedIndex > 0)
                        searchstring += " and CARTYPE = '" + cbxSearchTypeCombo.SelectedItem.ToString() + "'";

                    if (selvinno.Length > 0)
                    {
                        if (cbxSelPartialVIN.IsChecked == true)
                            searchstring += " and VIN LIKE '%" + selvinno + "%'";
                        else
                            searchstring += " and VIN = '" + selvinno + "'";
                    }

                    searchstring += " ORDER BY NO DESC";

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

        private async void btnallSearch_Click(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;

            string selvinno = "";
            DateTime beginTime = dpkStartDate.SelectedDate.Value;
            DateTime EndTime = dpkEndDate.SelectedDate.Value;
            DateTime now = DateTime.Now;
            string searchstring = "select * from completetable ORDER BY NO DESC";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                _cts = new CancellationTokenSource();
                var loadingWindow = new LoadingWindow();
                loadingWindow.Owner = this;
                loadingWindow.Canceled += () => _cts.Cancel();

                loadingWindow.Show();

                try
                {
                    var dataTable = await Task.Run(() => LoadDataTableFromMySQL(searchstring, _cts.Token));
                    Dispatcher.Invoke(() =>
                    {
                        dgdComplete.ItemsSource = dataTable.DefaultView;
                        lblCompDataCount.Content = dataTable.Rows.Count.ToString();
                    });
                }
                //catch (OperationCanceledException)
                //{
                //    MessageBox.Show("User cancled.");
                //}
                catch (Exception ex)
                {
                    //MessageBox.Show("DB ERROR: " + ex.Message);
                }
                finally
                {
                    loadingWindow.Close();
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                string ErrorCode = string.Format("00RE{0:X8}", Math.Abs(ex.HResult));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnallSearch_Click22(object sender, RoutedEventArgs e)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                DataTable dt = new DataTable();
                ITNTDBWrapper dbwrap = new ITNTDBWrapper();
                object obj = new object();
                dbwrap.ExecuteCommand(Constants.connstring_comp, "select * from completetable ORDER BY NO DESC", CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);

                //lock (((MainWindow)System.Windows.Application.Current.MainWindow).CompDBLock)
                //{
                //    ITNTDBManage compDB = new ITNTDBManage(Constants.connstring_comp);
                //    compDB.Open(Constants.connstring_comp);
                //    string searchstring = string.Format("select * from completetable");
                //    //searchstring += "order by no desc";
                //    compDB.CommandText = searchstring;

                //    compDB.ExecuteCommandReader(CommandTypeEnum.Text, ref dt);
                //    compDB.Close();
                //}

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
