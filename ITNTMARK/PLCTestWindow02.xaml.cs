using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ITNTMARK
{
    /// <summary>
    /// Interaction logic for PLCTestWindow02.xaml
    /// </summary>
    public partial class PLCTestWindow02 : Window
    {
        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();
        CancellationToken token = new CancellationToken();

        public PLCTestWindow02()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized;    // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize;       // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            string value = "";
            //int plcCommType = 1;


            grdLAN.Visibility = Visibility.Visible;
            //grdUART.Visibility = Visibility.Collapsed;

            Util.GetPrivateProfileValue("PLCCOMM", "ServerIP", "", ref value, Constants.PARAMS_INI_FILE);
            txbPLCIP.Text = value;

            Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "", ref value, Constants.PARAMS_INI_FILE);
            txbPLCPort.Text = value;

            Util.GetPrivateProfileValue("PLCCOMM", "RACKNO", "0", ref value, Constants.PARAMS_INI_FILE);
            txbPLCRack.Text = value;

            Util.GetPrivateProfileValue("PLCCOMM", "SLOTNO", "2", ref value, Constants.PARAMS_INI_FILE);
            txbPLCSlot.Text = value;

        }

        private void btnSavePLCPort_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnReadSignal_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnReadSignal_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string strshow = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog("Read PLC SIGNAL Start");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadSignalFromPLCAsync(0);
                if (retval.execResult == 0)
                {
                    if (retval.recvSize < 5)
                    {
                        ShowLog("Read PLC SIGNAL ERROR : RECEIVE DATA ABNORMAL.");
                    }
                    else
                    {
                        retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                        strshow = retval.recvString.Substring(4, retval.recvSize - 4);
                        ushort tmp = Convert.ToUInt16(strshow, 16);
                        ShowLog("RECEIVE SIGNAL : " + tmp.ToString("D4"));
                        //if (txbPLCReadData.CheckAccess())
                        //{
                        //    txbPLCReadData.Text = strshow;
                        //}
                        //else
                        //{
                        //    txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                        //    {
                        //        txbPLCReadData.Text = strshow;
                        //    }));
                        //}
                    }
                    //ShowLog("Read PLC SIGNAL : " + strshow);
                }
                else
                {
                    ShowLog("Read PLC SIGNAL ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Read PLC SIGNAL Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnReadCarType_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnReadSignal_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string strshow = "";
            string COMMAND = "READ PLC CAR TYPE";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadPLCCarType();
                if (retval.execResult == 0)
                {
                    if (retval.recvSize < 5)
                    {
                        ShowLog(COMMAND + " ERROR : RECEIVE DATA ABNORMAL.");
                    }
                    else
                    {
                        retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                        strshow = retval.recvString.Substring(4, retval.recvSize - 4);
                        ushort tmp = Convert.ToUInt16(strshow, 16);
                        ShowLog(COMMAND + " : " + tmp.ToString("D4"));
                    }
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnReadLinkStatus_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnReadSignal_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string strshow = "";
            string COMMAND = "READ PLC LINK STATUS";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadLinkStatusAsync();
                if (retval.execResult == 0)
                {
                    if (retval.recvSize < 5)
                    {
                        ShowLog(COMMAND + " ERROR : RECEIVE DATA ABNORMAL.");
                    }
                    else
                    {
                        retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                        strshow = retval.recvString.Substring(4, retval.recvSize - 4);
                        ushort tmp = Convert.ToUInt16(strshow, 16);
                        ShowLog(COMMAND + " : " + tmp.ToString("D4"));
                    }
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnReadSequence_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnWriteMatchResult_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string COMMAND = "READ PLC SEQUENCE";
            string strshow = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadPLCSequence();
                if (retval.execResult == 0)
                {
                    if (retval.recvSize < 5)
                    {
                        ShowLog(COMMAND + " ERROR : RECEIVE DATA ABNORMAL.");
                    }
                    else
                    {
                        retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                        strshow = retval.recvString.Substring(4, retval.recvSize - 4);
                        ushort tmp = Convert.ToUInt16(strshow, 16);
                        ShowLog(COMMAND + " : " + tmp.ToString("D4"));
                    }
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnWriteSignal_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnWriteSignal_Click";

            ITNTSendArgs arg = new ITNTSendArgs(32);
            ITNTResponseArgs retval = new ITNTResponseArgs(32);
            int ival = 0;
            string command = "WRITE PLC SIGNAL";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(command + " START");

                arg.AddrString = "SIGNAL";
                int.TryParse(txbWriteSignal.Text, out ival);
                byte[] tmp = BitConverter.GetBytes(ival);
                //Array.Reverse(tmp);

                if ((tmp == null) || (tmp.Length <= 0))
                {
                    ShowLog(command + " : ENTER DATA");
                    return;
                }

                arg.sendString = txbWriteSignal.Text;
                arg.dataSize = txbWriteSignal.Text.Length;
                arg.loglevel = 0;
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendPLCSignalAsync(tmp[0]);
                if (retval.execResult != 0)
                {
                    ShowLog(command + "ERROR : " + retval.execResult.ToString());
                }
                else
                {
                    ShowLog(command + " SUCCESS");
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(command + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnWriteMatchResult_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnWriteMatchResult_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string COMMAND = "WRITE MATCHING RESULT";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                if (rdbtnMatchResultOK.IsChecked == true)
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_OK);
                else
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG);

                if (retval.execResult == 0)
                {
                    ShowLog(COMMAND + " OK");
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnWriteMarkStatus_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnWriteMarkStatus_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string COMMAND = "WRITE MARKING STATUS";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                if (rdbtnMarkStatusIdle.IsChecked == true)
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendMarkingStatus((byte)PLCMELSEQSerial.PLC_MARK_STATUS_IDLE);
                else if (rdbtnMarkStatusMarking.IsChecked == true)
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendMarkingStatus((byte)PLCMELSEQSerial.PLC_MARK_STATUS_DOING);
                else
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendMarkingStatus((byte)PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE);

                if (retval.execResult == 0)
                {
                    ShowLog(COMMAND + " OK");
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnWriteVisionResult_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnWriteVisionResult_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string COMMAND = "WRITE VISION RESULT";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                if (rdbtnVisionResultOK.IsChecked == true)
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendVisionResult("O");
                else
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendVisionResult("N");

                if (retval.execResult == 0)
                {
                    ShowLog(COMMAND + " OK");
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnWriteSetLink_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnWriteSetLink_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string COMMAND = "WRITE SET LINK";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                if (rdbtnSetAirON.IsChecked == true)
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                else
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SetLinkAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                if (retval.execResult == 0)
                {
                    ShowLog(COMMAND + " OK");
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnWriteSetAir_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnWriteSetAir_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string COMMAND = "WRITE SET AIR";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                if (rdbtnSetAirON.IsChecked == true)
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_ON);
                else
                    retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendAirAsync(PLCControlManager.SIGNAL_PC2PLC_OFF);

                if (retval.execResult == 0)
                {
                    ShowLog(COMMAND + " OK");
                }
                else
                {
                    ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog(COMMAND + " Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnSendClearSignal_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSendMatchingOK_Click(object sender, RoutedEventArgs e)
        {

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
        //private void ChangeSize(double width, double height)
        //{
        //    try
        //    {
        //        scale.ScaleX = width / orginalWidth;
        //        scale.ScaleY = height / originalHeight;
        //        FrameworkElement rootElement = this.Content as FrameworkElement;
        //        rootElement.LayoutTransform = scale;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString());
        //    }
        //}

        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (this.WindowState == WindowState.Maximized)
        //    {
        //        ChangeSize(this.ActualWidth, this.ActualHeight);
        //    }
        //    this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
        //}

        private void btnTestLANPLCPort_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSaveLANPLCPort_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnPLCRead_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnPLCWrite_Click";

            ITNTSendArgs arg = new ITNTSendArgs(128);
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string strshow = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog("Read PLC Test Start");

                if (txbPLCReadAddress.Text.Length <= 0)
                {
                    ShowLog("ENTER PLC ADDRESS");
                    return;
                }

                arg.AddrString = txbPLCReadAddress.Text;

                //int.TryParse(txbPLCReadLength.Text, out arg.dataSize);
                //if (arg.dataSize > 4)
                arg.dataSize = 1;
                arg.loglevel = 0;

                strshow = "";
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadPLCAsync(arg);

                if (retval.execResult != 0)
                {
                    txbPLCReadData.Text = "ERROR";
                    ShowLog("Read PLC Test ERROR : " + retval.execResult.ToString());
                    return;
                }

                if (retval.recvSize < 5)
                {
                    txbPLCReadData.Text = "ERROR";
                    ShowLog("Read PLC Test ERROR : DATA LENGTH IS INVALID - " + retval.recvSize.ToString());
                    return;
                }
                else
                {
                    //retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                    strshow = retval.recvString.Substring(4, retval.recvSize - 4);
                    ushort tmp = Convert.ToUInt16(strshow, 16);
                    txbPLCReadData.Text = tmp.ToString("D4");
                }
                ShowLog("Read PLC Test End");

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Read PLC Test Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnPLCWrite_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnPLCWrite_Click";

            ITNTSendArgs arg = new ITNTSendArgs(128);
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            //int ival = 0;
            //ushort usval = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog("Write PLC Test Start");

                //byte[] tmp = StringToByteArray(txbPLCWriteData.Text);
                //byte[] tmp = Encoding.UTF8.GetBytes(txbPLCWriteData.Text);
                //byte[] tmp = BitConverter.GetBytes(ival);
                //Array.Reverse(tmp);

                if (txbPLCWriteAddress.Text.Length <= 0)
                {
                    ShowLog("ENTER PLC ADDRESS");
                    return;
                }

                if (txbPLCWriteData.Text.Length <= 0)
                {
                    ShowLog("ENTER WRITE VALUE");
                    return;
                }

                arg.AddrString = txbPLCWriteAddress.Text;
                arg.sendString = txbPLCWriteData.Text;

                arg.dataSize = 1;
                //Array.Copy(tmp, 0, arg.sendBuffer, 0, arg.dataSize);
                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.WritePLCAsync2(arg);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.WritePLCAsync(arg);
                if (retval.execResult != 0)
                {
                    ShowLog("Write PLC Test ERROR");
                }
                else
                {
                    ShowLog("Write PLC Test SUCCESS");
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Write PLC Test Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnExit_Click";

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            Close();
        }

        private void btnSendError_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSendMatchingNG_Click(object sender, RoutedEventArgs e)
        {

        }

        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    orginalWidth = this.Width;
        //    originalHeight = this.Height;
        //    ChangeSize(e.NewSize.Width, e.NewSize.Height);
        //}


        ////////////////////////////////////////////////////////////////
        ///
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

        private void ShowLog(string log)
        {
            string trace = "";
            DateTime dt = DateTime.Now;
            try
            {
                trace = dt.ToString("yyyy-MM-dd HH:mm:ss    ");
                trace += log;
                if (lsbResult.CheckAccess())
                {
                    lsbResult.Items.Add(trace);
                    if (lsbResult.Items.Count > 0)
                    {
                        lsbResult.SelectedIndex = lsbResult.Items.Count;
                        lsbResult.ScrollIntoView(lsbResult.SelectedItem);
                    }
                }
                else
                {
                    lsbResult.Dispatcher.Invoke(new Action(delegate
                    {
                        lsbResult.Items.Add(trace);
                        if (lsbResult.Items.Count > 0)
                        {
                            lsbResult.SelectedIndex = lsbResult.Items.Count;
                            lsbResult.ScrollIntoView(lsbResult.SelectedItem);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
