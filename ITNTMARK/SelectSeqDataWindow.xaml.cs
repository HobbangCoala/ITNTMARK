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
using System.Data;
using ITNTCOMMON;
using ITNTUTIL;
using System.Threading;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// SelectSeqDataWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SelectSeqDataWindow : Window
    {
        Dictionary<string, string> deftypedata = new Dictionary<string, string>()
            {
                {"1", "ON"},
                {"2", "NQ5"},
                {"3", "DL"},
                {"4", "MQ"},
            };
        Dictionary<string, string> typedata = new Dictionary<string, string>();

        //public DataRow selectedrow = null;
        //public string selectedvin = "";
        public MESReceivedData selectedData = new MESReceivedData();
        //public int selResult = 0;
        //public byte userDataType = 0;
        //public string cartype = "";


        public SelectSeqDataWindow()
        {
            InitializeComponent();

            Util.GetPrivateProfileKeyData("CARTYPE", deftypedata, ref typedata, Constants.PARAMS_INI_FILE);

            //TextRange rangeOfText1 = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
            //rangeOfText1.Text = "Text1 ";
            //rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
            //rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            //TextRange rangeOfWord = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
            //rangeOfWord.Text = "word ";
            //rangeOfWord.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
            //rangeOfWord.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular);

            //TextRange rangeOfText2 = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
            //rangeOfText2.Text = "Text2 ";
            //rangeOfText2.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
            //rangeOfText2.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            cbxCarType.Items.Add("Select");
            for (int i = 0; i < typedata.Count; i++)
                cbxCarType.Items.Add(typedata.ElementAt(i).Value);
            cbxCarType.SelectedIndex = 0;
            lblVINLength.Content = "0";
        }

        public SelectSeqDataWindow(DataTable dt)
        {
            InitializeComponent();

            //Dictionary<string, string> deftypedata = new Dictionary<string, string>()
            //{
            //    {"1", "ON"},
            //    {"2", "NQ5"},
            //    {"3", "DL"},
            //    {"4", "MQ"},
            //};
            //Dictionary<string, string> typedata = new Dictionary<string, string>();
            Util.GetPrivateProfileKeyData("CARTYPE", deftypedata, ref typedata, Constants.PARAMS_INI_FILE);

            cbxCarType.Items.Add("Select");
            for (int i = 0; i < typedata.Count; i++)
                cbxCarType.Items.Add(typedata.ElementAt(i).Value);
            cbxCarType.SelectedIndex = 0;
            lblVINLength.Content = "0";
            ShowSeqData(dt);
        }

        private void ShowSeqData(DataTable dt)
        {
            string className = "SelectSeqDataWindow";
            string funcName = "ShowSeqData";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (dt == null)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dt == null", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (dt.Rows.Count <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dt count <= 0", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                dgdSeqData.ItemsSource = dt.DefaultView;
                dgdSeqData.Items.Refresh();
                if (dgdSeqData.Items.Count > 0)
                {
                    dgdSeqData.UpdateLayout();
                    dgdSeqData.SelectedIndex = 0;
                    dgdSeqData.Focus();
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (dgdSeqData.SelectedIndex <= -1)
            {
                return;
            }
            DataRowView rowview = dgdSeqData.SelectedItem as DataRowView;
            DataRow row = rowview.Row;
            if (null != row)
            {
                DateTime dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
                selectedData.productdate = dateValue.ToString("yyyy-MM-dd");

                selectedData.sequence = row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                selectedData.rawcartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                selectedData.bodyno = row.ItemArray[Constants.DB_NAME_BODYNO].ToString();
                selectedData.markvin = row.ItemArray[Constants.DB_NAME_MARKVIN].ToString();

                dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESDATE].ToString());
                DateTime timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MESTIME].ToString());
                selectedData.mesdate = dateValue.ToString("yyyy-MM-dd");
                selectedData.mestime = timeValue.ToString("HH:mm:ss");

                selectedData.lastsequence = row.ItemArray[Constants.DB_NAME_LASTSEQ].ToString();
                selectedData.code219 = row.ItemArray[Constants.DB_NAME_CODE219].ToString();
                selectedData.idplate = row.ItemArray[Constants.DB_NAME_IDPLATE].ToString();
                selectedData.delete = row.ItemArray[Constants.DB_NAME_DELETE].ToString();
                selectedData.totalmsg = row.ItemArray[Constants.DB_NAME_TOTALMSGE].ToString();
                selectedData.rawbodytype = row.ItemArray[Constants.DB_NAME_RAWBODY].ToString();
                selectedData.rawtrim = row.ItemArray[Constants.DB_NAME_RAWTRIM].ToString();
                selectedData.region = row.ItemArray[Constants.DB_NAME_REGION].ToString();
                selectedData.bodytype = row.ItemArray[Constants.DB_NAME_BODYTYPE].ToString();
                selectedData.cartype = row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();
                selectedData.plcvalue = row.ItemArray[Constants.DB_NAME_PLCVALUE].ToString();
                selectedData.rawvin = row.ItemArray[Constants.DB_NAME_RAWVIN].ToString();

                dateValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKDATE].ToString());
                timeValue = Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_MARKTIME].ToString());
                selectedData.markdate = dateValue.ToString("yyyy-MM-dd");
                selectedData.marktime = timeValue.ToString("HH:mm:ss");

                selectedData.remark = row.ItemArray[Constants.DB_NAME_REMARK].ToString();
                selectedData.exist = row.ItemArray[Constants.DB_NAME_EXIST].ToString();
                selectedData.isInserted = row.ItemArray[Constants.DB_NAME_ISINSERT].ToString();

                //selectedrow = row;
                //cartype = row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                //selectedvin = row.ItemArray[Constants.DB_NAME_VIN].ToString();
                selectedData.userDataType = 1;
                selectedData.execResult = 0;
            }
            DialogResult = true;
        }

        private void txtVINData_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lblVINLength != null)
                lblVINLength.Content = txtVINData.Text.Length.ToString();
        }

        private void btnPass_Click(object sender, RoutedEventArgs e)
        {
            DateTime dateValue = DateTime.Now;// Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
            DateTime timeValue = DateTime.Now;
            selectedData.productdate = dateValue.ToString("yyyy-MM-dd");

            selectedData.sequence = "0000";
            selectedData.rawcartype = "1000";
            selectedData.bodyno = " ";
            selectedData.markvin = "           ";

            selectedData.mesdate = dateValue.ToString("yyyy-MM-dd");
            selectedData.mestime = timeValue.ToString("HH:mm:ss");

            selectedData.lastsequence = " ";
            selectedData.code219 = " ";
            selectedData.idplate = " ";
            selectedData.delete = " ";
            selectedData.totalmsg = " ";
            selectedData.rawbodytype = " ";
            selectedData.rawtrim = " ";
            selectedData.region = " ";
            selectedData.bodytype = " ";
            selectedData.cartype = " ";
            selectedData.plcvalue = " ";

            selectedData.markdate = dateValue.ToString("yyyy-MM-dd");
            selectedData.marktime = timeValue.ToString("HH:mm:ss");

            selectedData.remark = "N";
            selectedData.exist = "Y";

            //selectedvin = "     ";
            //cartype = "1000";
            selectedData.userDataType = 3;
            selectedData.execResult = 0;
            DialogResult = true;
        }

        private void MenuItem_CopyVinData(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgdSeqData.SelectedIndex < 0)
                {
                    lblErrorMessage.Content = "Select data to be copied.";
                    return;
                }
                DataRowView row = dgdSeqData.SelectedItem as DataRowView;
                string markvin = row.Row.ItemArray[Constants.DB_NAME_MARKVIN].ToString();
                txtVINData.Text = markvin;
                string seq = row.Row.ItemArray[Constants.DB_NAME_SEQUENCE].ToString();
                txtSequence.Text = seq;
                string rawtype = row.Row.ItemArray[Constants.DB_NAME_RAWCARTYPE].ToString();
                string type = row.Row.ItemArray[Constants.DB_NAME_CARTYPE].ToString();

                //string value = "";
                //string type = "";
                //if (rawtype.Length >= 1)
                //{
                //    value = rawtype.Substring(0, 1);
                //    type = ((MainWindow)System.Windows.Application.Current.MainWindow).GetCarType(value, rawtype);
                //    cbxCarType.SelectedItem = type;
                //}
                //else
                //{
                //    lblErrorMessage.Content = "Data is invalid.";
                //    return;
                //}
            }
            catch (Exception ex)
            {

            }
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            string value = "";
            string rawcartype = "1000";
            if (cbxCarType.SelectedIndex <= 0)
            {
                //MessageBox("Select car type and enter VIN data");
                lblErrorMessage.Content = "Select car type first.";
                return;
            }

            if (txtVINData.Text.Length <= 0)
            {
                //MessageBox("Select car type and enter VIN data");
                lblErrorMessage.Content = "Enter V.I.N. to be marked";
                return;
            }

            //selectedvin = txtVINData.Text;
            string typestring = cbxCarType.SelectedItem.ToString();
            Util.GetPrivateProfileValue("CARTYPE", typestring, "", ref value, Constants.PARAMS_INI_FILE);
            if (value.Length > 0)
                rawcartype = value;
            else
            {
                if (cbxCarType.SelectedIndex == 1)
                    rawcartype = "1000";
                else if (cbxCarType.SelectedIndex == 2)
                    rawcartype = "2000";
                else if (cbxCarType.SelectedIndex == 3)
                    rawcartype = "3000";
                else if (cbxCarType.SelectedIndex == 4)
                    rawcartype = "4000";
            }

            DateTime dateValue = DateTime.Now;// Convert.ToDateTime(row.ItemArray[Constants.DB_NAME_PRODUCTDATE].ToString());
            DateTime timeValue = DateTime.Now;
            selectedData.productdate = dateValue.ToString("yyyy-MM-dd");

            if (txtSequence.Text.Length <= 0)
                selectedData.sequence = "0000";
            else
                selectedData.sequence = txtSequence.Text;

            selectedData.rawcartype = rawcartype;
            selectedData.bodyno = " ";
            string tmpvin = txtVINData.Text.ToUpper().ToString();
            if (tmpvin.Length < 19)
                selectedData.markvin = tmpvin.PadRight(19, ' ');
            else
                selectedData.markvin = tmpvin;

            selectedData.mesdate = dateValue.ToString("yyyy-MM-dd");
            selectedData.mestime = timeValue.ToString("HH:mm:ss");

            selectedData.lastsequence = " ";
            selectedData.code219 = " ";
            selectedData.idplate = " ";
            selectedData.delete = " ";
            selectedData.totalmsg = " ";
            selectedData.rawbodytype = " ";
            selectedData.rawtrim = " ";
            selectedData.region = " ";
            selectedData.bodytype = " ";

            selectedData.cartype = ((MainWindow)System.Windows.Application.Current.MainWindow).GetCarTypeFromNumber(rawcartype.Trim());
            //value = selectedData.rawcartype.Trim().Substring(0, 1);
            //selectedData.cartype = ((MainWindow)System.Windows.Application.Current.MainWindow).GetCarType(value, selectedData.rawcartype);
            selectedData.plcvalue = " ";

            selectedData.markdate = dateValue.ToString("yyyy-MM-dd");
            selectedData.marktime = timeValue.ToString("HH:mm:ss");

            selectedData.remark = "N";
            selectedData.exist = "Y";

            //selectedvin = "     ";
            //cartype = "1000";
            selectedData.userDataType = 2;
            selectedData.execResult = 0;
            DialogResult = true;
        }
    }
}
