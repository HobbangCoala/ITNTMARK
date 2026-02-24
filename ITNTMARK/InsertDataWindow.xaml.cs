using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ITNTCOMMM;
using ITNTUTIL;
using ITNTCOMMON;
using System.Threading;
using System.Windows.Media;

#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// InsertDataWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InsertDataWindow : Window
    {
        public InsertDataWindow()
        {
            InitializeComponent();

            stpPrev.Visibility = Visibility.Collapsed;
            stpNext.Visibility = Visibility.Collapsed;
        }

        public InsertDataWindow(DataRowView rv1, DataRowView rv2)
        {
            InitializeComponent();
            try
            {
                if (rv1 != null)
                {
                    lblCarTypePrev.Content = rv1.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    lblVINDataPrev.Content = rv1.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                    lblSequencePrev.Content = rv1.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                }
                else
                    stpPrev.Visibility = Visibility.Collapsed;

                if (rv2 != null)
                {
                    lblCarTypeNext.Content = rv2.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    lblVINDataNext.Content = rv2.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                    lblSequenceNext.Content = rv2.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                }
                else
                    stpNext.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {

            }
        }

        public InsertDataWindow(DataRowView rv1, DataRowView rv2, Window owner)
        {
            InitializeComponent();
            try
            {
                this.Owner = owner;

                if (rv1 != null)
                {
                    lblCarTypePrev.Content = rv1.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    lblVINDataPrev.Content = rv1.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                    lblSequencePrev.Content = rv1.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                }
                else
                    stpPrev.Visibility = Visibility.Collapsed;

                if (rv2 != null)
                {
                    lblCarTypeNext.Content = rv2.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                    lblVINDataNext.Content = rv2.Row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();
                    lblSequenceNext.Content = rv2.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                }
                else
                    stpNext.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {

            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            DataTable dt = new DataTable();
            string tableName = "plantable";
            int count = 0;
            var result = new List<string>();
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();

            try
            {
                if (txtCarType.Text.Length <= 0)
                {
                    return;
                }

                if (txtVINData.Text.Length <= 0)
                {
                    return;
                }

                if (txtSequence.Text.Length <= 0)
                {
                    return;
                }

                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref tableName, Constants.PARAMS_INI_FILE);
                dbwrap.ExecuteCommand(Constants.connstring, "SELECT COUNT(*) FROM " + tableName + " WHERE SEQUENCE='" + txtSequence.Text + "'", CommandMode.Scalar, CommandTypeEnum.Text, ref dt, ref obj);
                count = (int)(long)obj;
                if (count > 0)
                {
                    string msg = "";
                    result = GetUnusedCodes(tableName);
                    int num = Math.Min(3, result.Count);
                    for (int i = 0; i < num; i++)
                    {
                        msg += result[i];
                        if (i != num - 1)
                        {
                            msg += ", ";
                        }
                    }

                    msg1.Message = "THE SEQUENCE YOU ENTERED IS ALREADY IN USE";
                    msg1.Fontsize = 18;
                    msg1.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg1.VerticalContentAlignment = VerticalAlignment.Center;
                    msg1.Foreground = Brushes.Red;
                    msg1.Background = Brushes.White;

                    msg3.Message = "AVAILABLE NUMBER (" + msg + ")";
                    msg3.Fontsize = 18;
                    msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg3.VerticalContentAlignment = VerticalAlignment.Center;
                    msg3.Foreground = Brushes.Blue;
                    msg3.Background = Brushes.White;

                    msg4.Message = "IF YOU WANT TO USE CURRENT SEQUNCE, PRESS [YES] BUTTON";
                    msg4.Fontsize = 17;
                    msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg4.VerticalContentAlignment = VerticalAlignment.Center;
                    msg4.Foreground = Brushes.Black;
                    msg4.Background = Brushes.White;

                    msg5.Message = "IF YOU WANT TO USE DIFFERENT SEQUNCE, PRESS [NO] BUTTON";
                    msg5.Fontsize = 17;
                    msg5.HorizontalContentAlignment = HorizontalAlignment.Center;
                    msg5.VerticalContentAlignment = VerticalAlignment.Center;
                    msg5.Foreground = Brushes.Black;
                    msg5.Background = Brushes.White;

                    //// 사용자의 선택에 따라 분기
                    WarningWindow window = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "YES", "NO", this, 1);
                    bool? retval = window.ShowDialog();
                    if (retval == true)
                    {
                        this.DialogResult = true;
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                this.DialogResult = true;
                return;
            }
            catch (Exception ex)
            {
                this.DialogResult = true;
            }
        }

        public List<string> GetUnusedCodes(string tableName)
        {
            var result = new List<string>();
            string query = "";
            ITNTDBWrapper dbwrap = new ITNTDBWrapper();
            object obj = new object();
            DataTable dt = new DataTable();

            try
            {
                if (tableName == "plantable")
                {
                    query = @"WITH RECURSIVE numbers AS (
                                                            SELECT 1 AS num
                                                            UNION ALL
                                                            SELECT num +1 FROM numbers WHERE num< 9999
                                                        )
                                                        SELECT LPAD(num, 4, '0') AS missing_sequence
                                                        FROM numbers
                                                        WHERE LPAD(num, 4, '0') NOT IN(
                                                            SELECT SEQUENCE FROM plantable
                                                        )
                                                        ORDER BY num; ";
                }
                else
                {
                    query = @"WITH RECURSIVE numbers AS (
                                                            SELECT 1 AS num
                                                            UNION ALL
                                                            SELECT num +1 FROM numbers WHERE num< 9999
                                                        )
                                                        SELECT LPAD(num, 4, '0') AS missing_sequence
                                                        FROM numbers
                                                        WHERE LPAD(num, 4, '0') NOT IN(
                                                            SELECT SEQUENCE FROM plantable2
                                                        )
                                                        ORDER BY num; ";
                }

                dbwrap.ExecuteCommand(Constants.connstring, query, CommandMode.Reader, CommandTypeEnum.Text, ref dt, ref obj);
                DataRow[] rows = dt.Select();
                DataRow drow;
                string item2 = "";
                int num = Math.Min(3, rows.Length);
                for (int i = 0; i < num; i++)
                {
                    drow = rows[i];
                    item2 = drow.ItemArray[0].ToString();
                    result.Add(item2);
                }
            }
            catch (Exception ex)
            { }

            return result;
        }

        private void txtVINData_TextChanged(object sender, TextChangedEventArgs e)
        {
            string className = "InsertDataWindow";
            string funcName = "txtVINData_TextChanged";

            try
            {
                if (lblVINLength != null)
                    lblVINLength.Content = txtVINData.Text.Length.ToString();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}
