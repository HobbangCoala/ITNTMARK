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
using ITNTCOMMON;
using ITNTUTIL;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    /// <summary>
    /// PLCTestWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PLCTestWindow : Window
    {
        string[] baud = { "9600", "14400", "19200", "38400", "57600", "115200" };

        double orginalWidth, originalHeight;
        ScaleTransform scale = new ScaleTransform();
        CancellationToken token = new CancellationToken();

        public PLCTestWindow()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            WindowState = WindowState.Maximized; // 모니터의 해상도 크기로 변경
            ResizeMode = ResizeMode.NoResize; // Window의 크기를 변경 불가
            this.Loaded += new RoutedEventHandler(Window_Loaded);

            string value = "";
            int plcCommType = 1;
            plcCommType = (int)Util.GetPrivateProfileValueUINT("PLCCOMM", "COMMTYPE", 1, Constants.PARAMS_INI_FILE);
            if(plcCommType == (int)PLC_COMM_TYPE.PLC_COMM_TYPE_RS232)
            {
                grdUART.Visibility = Visibility.Visible;
                grdLAN.Visibility = Visibility.Collapsed;
                for (int i = 0; i < baud.Length; i++)
                    cbxPLCBaudRate.Items.Add(baud[i]);

                int ibaud = (int)Util.GetPrivateProfileValueUINT("PLCCOMM", "BAUDRATE", 19200, Constants.PARAMS_INI_FILE);

                if (cbxPLCBaudRate.Items.Count > 0)
                {
                    if (cbxPLCBaudRate.Items.Contains(ibaud.ToString()))
                        cbxPLCBaudRate.SelectedItem = ibaud.ToString();
                    else
                        cbxPLCBaudRate.SelectedIndex = 0;
                }

                string[] portname = ((MainWindow)System.Windows.Application.Current.MainWindow).MarkControll.GetSerialPorts();
                for (int j = 0; j < portname.Length; j++)
                {
                    if (portname[j].Contains("COM") == true)
                        cbxPLCPortNum.Items.Add(portname[j]);
                }

                string port = "";
                Util.GetPrivateProfileValue("PLCCOMM", "PORT", "COM1", ref port, Constants.PARAMS_INI_FILE);

                if (cbxPLCPortNum.Items.Count > 0)
                {
                    if (cbxPLCPortNum.Items.Contains(port))
                        cbxPLCPortNum.SelectedItem = port;
                    else
                        cbxPLCPortNum.SelectedIndex = 0;
                }
            }
            else if(plcCommType == (int)PLC_COMM_TYPE.PLC_COMM_TYPE_TCP)
            {
                grdLAN.Visibility = Visibility.Visible;
                grdUART.Visibility = Visibility.Collapsed;

                Util.GetPrivateProfileValue("PLCCOMM", "ServerIP", "", ref value, Constants.PARAMS_INI_FILE  );
                txbPLCIP.Text = value;

                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "", ref value, Constants.PARAMS_INI_FILE);
                txbPLCPort.Text = value;

                Util.GetPrivateProfileValue("PLCCOMM", "RACKNO", "0", ref value, Constants.PARAMS_INI_FILE);
                txbPLCRack.Text = value;

                Util.GetPrivateProfileValue("PLCCOMM", "SLOTNO", "2", ref value, Constants.PARAMS_INI_FILE);
                txbPLCSlot.Text = value;
            }

            Util.GetPrivateProfileValue("CONFIG", "USELINK", "0", ref value, Constants.SCANNER_INI_FILE);
            //Util.GetPrivateProfileValue("OPTION", "USELPM", "0", ref value, Constants.LENZ_INI_FILE);
            if (value != "0")
                btnReadLinkStatus.Visibility = Visibility.Visible;
            else
                btnReadLinkStatus.Visibility = Visibility.Collapsed;

            Util.GetPrivateProfileValue("OPTION", "CARTYPECOMPTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
            if (value == "5") //HMI 3
            {
                btnReadCarType.Visibility = Visibility.Collapsed;
                btnReadBodyNumber.Visibility = Visibility.Visible;
            }
            else
            {
                btnReadCarType.Visibility = Visibility.Visible;
                btnReadBodyNumber.Visibility = Visibility.Collapsed;
            }

            Util.GetPrivateProfileValue("OPTION", "ISDUALHEAD", "0", ref value, Constants.MARKING_INI_FILE);
            if (value != "0")
                btnReadHeadNum.Visibility = Visibility.Visible;
            else
                btnReadHeadNum.Visibility = Visibility.Collapsed;
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


        private async void btnSavePLCPort_Click(object sender, RoutedEventArgs e)
        {
            string port = "";
            string baudstr = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();

            if (cbxPLCPortNum.SelectedItem.ToString().Length <= 0)
                return;

            if (cbxPLCBaudRate.SelectedItem.ToString().Length <= 0)
                return;

            port = cbxPLCPortNum.SelectedItem.ToString();
            baudstr = cbxPLCBaudRate.SelectedItem.ToString();

            string oldport = "";
            string oldbaud = "";
            Util.GetPrivateProfileValue("PLCCOMM", "PORT", "COM3", ref oldport, Constants.PARAMS_INI_FILE);
            Util.GetPrivateProfileValue("PLCCOMM", "BAUDRATE", "38400", ref oldbaud, Constants.PARAMS_INI_FILE);

            Util.WritePrivateProfileValue("PLCCOMM", "PORT", port, Constants.PARAMS_INI_FILE);
            Util.WritePrivateProfileValue("PLCCOMM", "BAUDRATE", baudstr, Constants.PARAMS_INI_FILE);

            ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ClosePLC();
            retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.OpenPLCAsync();
            if (retval.execResult != 0)
            {
                ShowLog("FAIL TO COMMUNICATION");
                ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ClosePLC();
                Util.WritePrivateProfileValue("PLCCOMM", "PORT", oldport, Constants.PARAMS_INI_FILE);
                Util.WritePrivateProfileValue("PLCCOMM", "BAUDRATE", oldbaud, Constants.PARAMS_INI_FILE);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.OpenPLCAsync();
            }
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
                int.TryParse(arg.AddrString, out arg.Address);

                int.TryParse(txbPLCReadLength.Text, out arg.dataSize);
                if (arg.dataSize > 4)
                    arg.dataSize = 4;
                arg.loglevel = 0;

                strshow = "";
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadPLCAsync(arg);

                if (retval.execResult == 0)
                {
                    if (retval.recvSize < 5)
                    {

                    }
                    else
                    {
                        retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                        strshow = retval.recvString.Substring(4, retval.recvSize - 4);
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog("Read PLC Test End");
                }
                //if (retval.execResult == 0)
                //{

                //    Array.Reverse(retval.recvBuffer, 0, retval.recvSize);
                //    for(int i = 0; i < retval.recvSize; i++)
                //    {
                //        strshow += retval.recvBuffer[i].ToString("X2");
                //    }

                //    if (txbPLCReadData.CheckAccess())
                //    {
                //        txbPLCReadData.Text = strshow;
                //    }
                //    else
                //    {
                //        txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                //        {
                //            txbPLCReadData.Text = strshow;
                //        }));
                //    }
                //    //string str = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //    //if (str.Length >= 8)
                //    //    str = str.Substring(4, 4);
                //    //if (txbPLCReadData.CheckAccess())
                //    //{
                //    //    txbPLCReadData.Text = str;
                //    //}
                //    //else
                //    //{
                //    //    txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                //    //    {
                //    //        txbPLCReadData.Text = str;
                //    //    }));
                //    //}



                //    ShowLog("Read PLC Test SUCCESS");
                //}
                else
                {
                    if (txbPLCReadData.CheckAccess())
                    {
                        txbPLCReadData.Text = "ERROR";
                    }
                    else
                    {
                        txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                        {
                            txbPLCReadData.Text = "ERROR";
                        }));
                    }
                    ShowLog("Read PLC Test ERROR : " + retval.execResult.ToString());
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //ShowLog("Read PLC Test Start");

                //if(txbPLCReadAddress.Text.Length <= 0)
                //{
                //    ShowLog("ENTER PLC ADDRESS");
                //    return;
                //}

                //arg.AddrString = txbPLCReadAddress.Text;
                //int.TryParse(arg.AddrString, out arg.Address);

                //int.TryParse(txbPLCReadLength.Text, out arg.dataSize);
                //if (arg.dataSize > 4)
                //    arg.dataSize = 4;
                //arg.loglevel = 0;

                //strshow = "";
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadPLCAsync(arg);
                //if (retval.execResult == 0)
                //{

                //    Array.Reverse(retval.recvBuffer, 0, retval.recvSize);
                //    for(int i = 0; i < retval.recvSize; i++)
                //    {
                //        strshow += retval.recvBuffer[i].ToString("X2");
                //    }

                //    if (txbPLCReadData.CheckAccess())
                //    {
                //        txbPLCReadData.Text = strshow;
                //    }
                //    else
                //    {
                //        txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                //        {
                //            txbPLCReadData.Text = strshow;
                //        }));
                //    }
                //    //string str = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
                //    //if (str.Length >= 8)
                //    //    str = str.Substring(4, 4);
                //    //if (txbPLCReadData.CheckAccess())
                //    //{
                //    //    txbPLCReadData.Text = str;
                //    //}
                //    //else
                //    //{
                //    //    txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                //    //    {
                //    //        txbPLCReadData.Text = str;
                //    //    }));
                //    //}



                //    ShowLog("Read PLC Test SUCCESS");
                //}
                //else
                //{
                //    if (txbPLCReadData.CheckAccess())
                //    {
                //        txbPLCReadData.Text = "ERROR";
                //    }
                //    else
                //    {
                //        txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                //        {
                //            txbPLCReadData.Text = "ERROR";
                //        }));
                //    }
                //    ShowLog("Read PLC Test ERROR : " + retval.execResult.ToString());
                //}
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
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
            int ival = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog("Write PLC Test Start");

                arg.AddrString = txbPLCWriteAddress.Text;
                if(arg.AddrString.Length < 4)
                    arg.AddrString = arg.AddrString.PadLeft(4, '0');
                int.TryParse(txbPLCWriteAddress.Text, out arg.Address);
                int.TryParse(txbPLCWriteData.Text, out ival);

                //byte[] tmp = StringToByteArray(txbPLCWriteData.Text);
                //byte[] tmp = Encoding.UTF8.GetBytes(txbPLCWriteData.Text);
                byte[] tmp = BitConverter.GetBytes(ival);
                //Array.Reverse(tmp);

                if (tmp == null)
                {
                    ShowLog("Enter Data");
                    return;
                }

                arg.sendString = txbPLCWriteData.Text;
                arg.dataSize = txbPLCWriteData.Text.Length;
                //arg.dataSize = tmp.Length;// (txbPLCWriteData.Text.Length + 1)/ 2;
                arg.loglevel = 0;
                //Array.Copy(tmp, 0, arg.sendBuffer, 0, arg.dataSize);
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.WritePLCAsync2(arg);
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
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //ShowLog("Write PLC Test Start");

                //arg.AddrString = txbPLCWriteAddress.Text;
                //arg.AddrString = arg.AddrString.PadLeft(4, '0');
                //int.TryParse(txbPLCWriteAddress.Text, out arg.Address);
                //byte[] tmp = StringToByteArray(txbPLCWriteData.Text);
                //if(tmp == null)
                //{
                //    ShowLog("Enter Data");
                //    return;
                //}

                //arg.dataSize = tmp.Length;// (txbPLCWriteData.Text.Length + 1)/ 2;
                //arg.loglevel = 0;
                //Array.Copy(arg.sendBuffer, 0, tmp, 0, arg.dataSize);
                //retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.WritePLCAsync(arg);
                //if (retval.execResult != 0)
                //{
                //    ShowLog("Write PLC Test ERROR");
                //}
                //else
                //{
                //    ShowLog("Write PLC Test SUCCESS");
                //}
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Write PLC Test Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSendClearSignal_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnSendClearSignal_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                ShowLog("Send Clear Signal Test Start");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendSignal(0);
                if (retval.execResult != 0)
                {
                    ShowLog("Send Clear Signal Test ERROR");
                }
                else
                {
                    ShowLog("Send Clear Signal Test SUCCESS");
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Send Clear Signal Test Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSendMatchingOK_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnSendMatchingOK_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog("Send Clear Signal Test Start");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendSignal(1);
                if (retval.execResult != 0)
                {
                    ShowLog("Send Clear Signal Test ERROR");
                }
                else
                {
                    ShowLog("Send Clear Signal Test SUCCESS");
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Send Clear Signal Test Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnSendMatchingNG_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnSendMatchingNG_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog("Send Clear Signal Test Start");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendSignal(2);
                if (retval.execResult != 0)
                {
                    ShowLog("Send Clear Signal Test ERROR");
                }
                else
                {
                    ShowLog("Send Clear Signal Test SUCCESS");
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Send Clear Signal Test Exception - " + ex.Message);
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

        private async void btnSendError_Click(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendSignal(4);
        }

        private async void btnSaveLANPLCPort_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnExit_Click";

            string value = "";
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if (txbPLCIP.Text.Length <= 0)
                {
                    ShowLog("Enter Server IP Address");
                    return;
                }

                if (txbPLCPort.Text.Length <= 0)
                {
                    ShowLog("Enter Listen Port Number");
                    return;
                }

                string oldport = "";
                string oldport2 = "";
                string oldIP = "";
                Util.GetPrivateProfileValue("PLCCOMM", "ServerIP", "", ref oldIP, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "ListenPort", "44818", ref oldport, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("PLCCOMM", "SERVERPORT", "44818", ref oldport2, Constants.PARAMS_INI_FILE);

                Util.WritePrivateProfileValue("PLCCOMM", "ServerIP", txbPLCIP.Text.ToString(), Constants.PARAMS_INI_FILE);
                Util.WritePrivateProfileValue("PLCCOMM", "ListenPort", txbPLCPort.Text.ToString(), Constants.PARAMS_INI_FILE);
                Util.WritePrivateProfileValue("PLCCOMM", "SERVERPORT", txbPLCPort.Text.ToString(), Constants.PARAMS_INI_FILE);

                ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ClosePLC();
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.OpenPLCAsync();
                if (retval.execResult != 0)
                {
                    ShowLog("FAIL TO COMMUNICATION");
                    ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ClosePLC();
                    Util.WritePrivateProfileValue("PLCCOMM", "ServerIP", oldIP, Constants.PARAMS_INI_FILE);
                    Util.WritePrivateProfileValue("PLCCOMM", "BAUDRATE", oldport, Constants.PARAMS_INI_FILE);
                    Util.WritePrivateProfileValue("PLCCOMM", "ListenPort", oldport, Constants.PARAMS_INI_FILE);
                    Util.WritePrivateProfileValue("PLCCOMM", "SERVERPORT", oldport2, Constants.PARAMS_INI_FILE);
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.OpenPLCAsync();
                }

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Send Clear Signal Test Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private async void btnTestLANPLCPort_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnTestLANPLCPort_Click";

            string value = "";
            ConfirmWindowString msg1 = new ConfirmWindowString();
            ConfirmWindowString msg2 = new ConfirmWindowString();
            ConfirmWindowString msg3 = new ConfirmWindowString();
            ConfirmWindowString msg4 = new ConfirmWindowString();
            ConfirmWindowString msg5 = new ConfirmWindowString();
            bool ret = false;
            string cmd = "CONNECTION TEST";
            string sIP = "", sPort = "", sRack = "0", sSlot = "";
            int iPort = 0;//, iRack = 0, iSlot = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                sIP = txbPLCIP.Text.ToString();
                sPort = txbPLCPort.Text.ToString();
                sRack = txbPLCRack.Text.ToString();
                sSlot = txbPLCSlot.Text.ToString();

                if (sIP.Length < 0)
                {
                    ShowLog(cmd + " - IP를 입력하세요");
                    return;
                }

                if (sPort.Length < 0)
                {
                    ShowLog(cmd + " - 포트 번호를 입력하세요");
                    return;
                }

                if (sRack.Length < 0)
                {
                    ShowLog(cmd + " - 랙 번호를 입력하세요");
                    return;
                }

                if (sSlot.Length < 0)
                {
                    ShowLog(cmd + " - 슬롯 번호를 입력하세요");
                    return;
                }

                msg2.Message = "PLC 통신 테스트를 실행하시겠습니까?";
                msg2.Fontsize = 18;
                msg2.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg2.VerticalContentAlignment = VerticalAlignment.Center;
                msg2.Foreground = Brushes.Red;
                msg2.Background = Brushes.White;

                msg3.Message = "** 주의 : PLC와 통신이 끊어집니다.";
                msg3.Fontsize = 20;
                msg3.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg3.VerticalContentAlignment = VerticalAlignment.Center;
                msg3.Foreground = Brushes.Red;
                msg3.Background = Brushes.White;

                msg4.Message = "자동 실행 중인 경우에는 [CANCEL] 버튼을 눌러주세요";
                msg4.Fontsize = 20;
                msg4.HorizontalContentAlignment = HorizontalAlignment.Center;
                msg4.VerticalContentAlignment = VerticalAlignment.Center;
                msg4.Foreground = Brushes.Red;
                msg4.Background = Brushes.White;

                if (CheckAccess())
                {
                    WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                    warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    ret = warning.ShowDialog().Value;
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WarningWindow warning = new WarningWindow("", msg1, msg2, msg3, msg4, msg5, "OK", "NO", this, 1);
                        warning.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        ret = warning.ShowDialog().Value;
                    }));
                }

                if(ret == false)
                {
                    return;
                }

                //ShowLog(className, funcName, 0, LOGTYPE.LOG_NORMAL, cmd, "기존 통신 끊기");
                ShowLog(cmd + " - 기존 통신 끊기");
                ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ClosePLC();

                int.TryParse(sPort, out iPort);
                ShowLog(cmd + " - 접속 테스트 실행");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.OpenPLCAsync(sIP, iPort, sRack, sSlot);
                if(retval.execResult != 0)
                {
                    ShowLog(cmd + " - 접속 테스트 실패 : " + retval.ToString());
                }
                else
                {
                    ShowLog(cmd + " - 접속 테스트 성공");
                }

                ShowLog(cmd + " - 테스트 통신 끊기");
                ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ClosePLC();

                ShowLog(cmd + " - 기존 통신 재접속 실행");
                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.OpenPLCAsync();
                if (retval.execResult != 0)
                {
                    ShowLog(cmd + " - 접속 실패 : " + retval.ToString());
                }
                else
                {
                    ShowLog(cmd + " - 접속 성공");
                }

                ShowLog(cmd + " - 종료");

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ShowLog("Connection Test Exception - " + ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public static byte[] StringToByteArray(string hexstring)
        {
            int NumberChars = 0;
            byte[] retbytes = null;
            byte tmp = 0;
            string stmp = "";
            try
            {
                NumberChars = hexstring.Length;
                if ((NumberChars % 2) != 0)
                {
                    hexstring += "0";
                    NumberChars = hexstring.Length;
                }

                retbytes = new byte[NumberChars / 2];
                for(int i= 0; i < NumberChars; i += 2)
                {
                    stmp = hexstring.Substring(i, 2);
                    byte.TryParse(stmp, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out tmp);
                    retbytes[i / 2] = tmp;
                }
            }
            catch(Exception ex)
            {
                retbytes = null;
            }
            //int NumberChars = hex.Length;
            //byte[] bytes = new byte[NumberChars / 2];
            //for (int i = 0; i < NumberChars; i += 2)
            //    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return retbytes;
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
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog("Read PLC SIGNAL : " + strshow);
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
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog(COMMAND + " : " + strshow);
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

                retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadLinkStatusAsync();
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
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog(COMMAND + " : " + strshow);
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

        //private async void btnReadAutoManual_Click(object sender, RoutedEventArgs e)
        //{
        //    string className = "PLCTestWindow";
        //    string funcName = "btnReadSignal_Click";

        //    ITNTResponseArgs retval = new ITNTResponseArgs(64);
        //    string strshow = "";
        //    string COMMAND = "READ PLC AUTO SWITCH";

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
        //        ShowLog(COMMAND + " START");

        //        retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.read(0);
        //        if (retval.execResult == 0)
        //        {
        //            if (retval.recvSize < 5)
        //            {
        //                ShowLog(COMMAND + " ERROR : RECEIVE DATA ABNORMAL.");
        //            }
        //            else
        //            {
        //                retval.recvString = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
        //                strshow = retval.recvString.Substring(4, retval.recvSize - 4);
        //                if (txbPLCReadData.CheckAccess())
        //                {
        //                    txbPLCReadData.Text = strshow;
        //                }
        //                else
        //                {
        //                    txbPLCReadData.Dispatcher.Invoke(new Action(delegate
        //                    {
        //                        txbPLCReadData.Text = strshow;
        //                    }));
        //                }

        //            }
        //            ShowLog(COMMAND + " : " + strshow);
        //        }
        //        else
        //        {
        //            ShowLog(COMMAND + " ERROR : " + retval.execResult.ToString());
        //        }
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //    }
        //    catch (Exception ex)
        //    {
        //        ShowLog(COMMAND + " Exception - " + ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION ERROR = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        private async void btnReadSequence_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnReadSignal_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string strshow = "";
            string COMMAND = "READ PLC SEQUENCE";

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
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog(COMMAND + " : " + strshow);
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

        private async void btnWriteCarType_Click(object sender, RoutedEventArgs e)
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

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadPLCCarType(0);
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
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog(COMMAND + " : " + strshow);
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

        //private async void btnWriteSequence_Click(object sender, RoutedEventArgs e)
        //{

        //}

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
                    retval = await ((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.SendMarkingStatus((byte)PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE);

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

        private async void btnReadBodyNumber_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnReadBodyNumber_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string strshow = "";
            string COMMAND = "READ PLC BODY NUMBER";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadBodyNum();
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
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog(COMMAND + " : " + strshow);
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

        private async void btnReadHeadNum_Click(object sender, RoutedEventArgs e)
        {
            string className = "PLCTestWindow";
            string funcName = "btnReadHeadNum_Click";

            ITNTResponseArgs retval = new ITNTResponseArgs(64);
            string strshow = "";
            string COMMAND = "READ HEAD NUMBER";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                ShowLog(COMMAND + " START");

                retval = await((MainWindow)System.Windows.Application.Current.MainWindow).plcComm.ReadUseLaserNum();
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
                        if (txbPLCReadData.CheckAccess())
                        {
                            txbPLCReadData.Text = strshow;
                        }
                        else
                        {
                            txbPLCReadData.Dispatcher.Invoke(new Action(delegate
                            {
                                txbPLCReadData.Text = strshow;
                            }));
                        }

                    }
                    ShowLog(COMMAND + " : " + strshow);
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
        //private async void btnSendBuzzer_Click(object sender, RoutedEventArgs e)
        //{

        //}

        //private async void btnExit_Click(object sender, RoutedEventArgs e)
        //{

        //}
    }
}
