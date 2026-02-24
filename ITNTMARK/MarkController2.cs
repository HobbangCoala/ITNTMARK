using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ITNTCOMMM;
using ITNTUTIL;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using Microsoft.VisualBasic;
using System.Windows;
using ITNTCOMMON;
using System.Windows.Threading;
using System.IO;
using System.ComponentModel;
using static ITNTMARK.ControllerSettingWindow;
using System.Drawing;
using Brushes = System.Windows.Media.Brushes;

namespace ITNTMARK
{
    public struct MPOINT // TEXT
    {
        public Double X;
        public Double Y;
        public Double S;
        public Double U;
    }

    public struct m_font  // TEXT
    {
        public byte cN, fN;
        public UInt16 mX, mY;
        public byte mF;
    }
    public class M_FONT
    {
        public byte cN, fN;
        public UInt16 mX, mY;
        public byte mF;
        public M_FONT()
        {
            cN = fN = 0;
            mX = mY = 0;
            mF = 0;
        }
    }


    class MarkController2
    {
        public static bool BatchJobComplte = false;
        public static bool BatchJobStop = false;
        private byte[] RecvFrameData = new byte[2048];
        private readonly object cmdLock = new object();
        public static int Mark_Counter = 0;
        public static bool Pointdone = false;
        private readonly object LockJob = new object();

        public static MPOINT NOW_POS;
        static M_FONT[] fontdata = new M_FONT[500];
        private static int M_Count;


        protected static SerialPort Port = new SerialPort();


        public MarkController2()
        {
            for (int i = 0; i < fontdata.Length; i++)
                fontdata[i] = new M_FONT();
        }


        public int OpenDevice(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string ClassName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "START");

            int retval = 0;

#if TEST_DEBUG
            retval = 0;
#else       
            retval = OpenPort(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
            if (retval == 0)
            {

                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("OpenPort SUCCESS : {0}", retval));
            }
            else
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("OpenPort ERROR : {0}", retval));
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "END");
            return retval;
        }

        /// <summary>
        /// Close Port
        /// </summary>
        /// <returns></returns>
        public int CloseDevice()
        {
            string ClassName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "START");


#if TEST_DEBUG
            int retval = 0;
#else

            int retval = 0;

            {
                ClosePort();
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "END");
            return retval;
        }



        protected int OpenPort(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            int retval = 0;

            try
            {
                Port.PortName = port;// new SerialComm2(port);
                Port.Parity = parity;
                Port.StopBits = stopbit;
                Port.BaudRate = baud;
                Port.DataBits = databits;
                Port.Handshake = Handshake.None;
                Port.ReadTimeout = readtimeout;
                Port.WriteTimeout = writetimeout;

                if (Port.IsOpen)
                    return 0;

                Port.Open();

                Port.DiscardInBuffer();
                Port.DiscardOutBuffer();
                Port.DtrEnable = true;

                if (Port.IsOpen)
                    retval = 0;
                else
                    retval = -1;
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
            }

            return retval;
        }


        protected int ClosePort()
        {
            try
            {
                if (!Port.IsOpen)
                    return 0;

                Port.Close();
                if (!Port.IsOpen)
                    return 0;
                else
                    return -1;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }

        }

        protected int WritePort(byte[] buffer, int offset, int count)
        {
            if (!Port.IsOpen)
                return (int)COMPORTERROR.ERR_PORT_NOT_OPENED;

            {
                Port.Write(buffer, offset, count);
            }
            if (Port.BytesToWrite > 0)
            {
                return (int)COMPORTERROR.ERR_SEND_DATA_FAIL;
            }
            return count;
        }

        protected int WriteString(string buffer)
        {
            if (!Port.IsOpen)
                return (int)COMPORTERROR.ERR_PORT_NOT_OPENED;

            {
                Port.Write(buffer);
            }
            if (Port.BytesToWrite > 0)
            {
                return (int)COMPORTERROR.ERR_SEND_DATA_FAIL;
            }
            return 0;
        }

        protected int ReadPort(int readsize, ref int realsize, ref byte[] buffer)
        {

            {
                if (!Port.IsOpen)
                    return (int)COMPORTERROR.ERR_PORT_NOT_OPENED;

                realsize = Port.Read(buffer, 0, readsize);
                return realsize;
            }
        }

        protected bool IsPortOpen()
        {
            if (Port == null)
                return false;

            return Port.IsOpen;
        }

        protected string[] GetSerialPorts()
        {
            string[] portNames = SerialPort.GetPortNames();
            return portNames;
        }

        internal int GetBinHexAscii(byte a)
        {
            if ((a >= (byte)'0') && (a <= (byte)'9')) return (int)(a & 0x0f);
            else
            {
                a -= (byte)'A';
                return (int)(a & 0x0f) + 10;
            }
        }

        public int SendCommandMsg(int screenNo, int loglevel, string cmd, string payload)
        {
            string ClassName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(3, "{loglevel}::{1}()  {2}", ClassName, FuncName, "START");

            int retval = 0;
            int i = 0;
            byte[] sendmsg = new byte[128];
            int leng = 0;
            string strLeng = "";
            byte[] byLeng = new byte[2];
            int sendLength = payload.Length;

            Window window = new Window();

            try
            {
                if (screenNo == 0)
                    window = new MainWindow();
                else if (screenNo == 1)
                    window = new SetControllerWindow();
                else if (screenNo == 2)
                    window = new ManualMarkWindow();
                else
                    window = new MainWindow();

                byte[] tmp2 = Encoding.UTF8.GetBytes(cmd);
                leng = sendLength + 5;
                strLeng = leng.ToString("X2");
                byLeng = Encoding.UTF8.GetBytes(strLeng);

                sendmsg[i++] = (byte)ASCII.SOH;
                sendmsg[i++] = byLeng[0];
                sendmsg[i++] = byLeng[1];
                sendmsg[i++] = tmp2[0];
                sendmsg[i++] = (byte)ASCII.STX;
                byte[] tmp = Encoding.UTF8.GetBytes(payload);
                Array.Copy(tmp, 0, sendmsg, i, sendLength);
                i += sendLength;
                sendmsg[i++] = (byte)ASCII.ETX;
                sendmsg[i++] = GetBCC(sendmsg, 1, i - 1);
                sendmsg[i++] = (byte)ASCII.CR;
                lock (LockJob)
                {
                    retval = WritePort(sendmsg, 0, i);

                    if (retval <= 0)
                    {

                        return retval;
                    }

                    byte[] buffer = new byte[64];
                    int count;
                    int offset;
                    bool exitFlag = false;
                    retval = 0;

                    while (!exitFlag)
                    {
                        offset = 0;
                        while ((byte)Port.ReadByte() != (byte)ASCII.SOH) Thread.Sleep(1);           // SOH
                        buffer[0] = (byte)Port.ReadByte(); buffer[1] = (byte)Port.ReadByte();       // LEN 1, 2 (HEX ASCII)
                        count = (GetBinHexAscii(buffer[0]) << 4) + GetBinHexAscii(buffer[1]);
                        while (count > 0)
                        {
                            var readCount = Port.Read(buffer, offset, count);
                            offset += readCount;
                            count -= readCount;
                        }
                        Array.Copy(buffer, RecvFrameData, count);
                        switch (buffer[0])
                        {
                            case (byte)'R':
                            case (byte)'r':
                            case (byte)'H':
                            case (byte)'J':
                            case (byte)'C':
                            case (byte)'M':
                            case (byte)'K':
                            case (byte)'U':
                            case (byte)'h':
                            case (byte)'j':
                            case (byte)'k':          // Working
                                {
                                    string param2 = Encoding.UTF8.GetString(buffer, 7, 4);
                                    string param1 = Encoding.UTF8.GetString(buffer, 3, 4);
                                    switch (buffer[1])
                                    {
                                        case (byte)'0':
                                            int chindex = 0, ptindex = 0;

                                            if (param1.Length > 0)
                                                chindex = Convert.ToInt32(param1, 16);
                                            if (param2.Length > 0)
                                                ptindex = Convert.ToInt32(param2, 16);

                                            if (screenNo == 0)
                                                ((MainWindow)System.Windows.Application.Current.MainWindow).DelegateShowMarkingOneLine(chindex, ptindex);

                                            //window.Dispatcher.Invoke(new Action(delegate
                                            //{
                                            //    ((MainWindow)System.Windows.Application.Current.MainWindow).ShowMarkingOneLine(chindex, ptindex);
                                            //}));

                                            break;

                                        case (byte)'1':
                                            chindex = Convert.ToInt32(param1, 16);
                                            ptindex = Convert.ToInt32(param2, 16);
                                            if (buffer[0] == (byte)'H' || buffer[0] == (byte)'J' || buffer[0] == (byte)'M' || buffer[0] == (byte)'K')
                                            {
                                                //window.Dispatcher.Invoke(new Action(delegate
                                                //{
                                                //    window.TXT_CURRENT_X.Text = chindex.ToString();
                                                //    window.TXT_CURRENT_Y.Text = ptindex.ToString();

                                                //}));
                                            }
                                            else
                                            {
                                                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                                                //{
                                                //    ControlWindow.Txt_Current_U.Text = Convert.ToString(chindex);
                                                //}));
                                            }
                                            break;

                                        case (byte)'2':
                                            chindex = Convert.ToInt32(param1, 16);

                                            //ControlWindow.Dispatcher.Invoke(new Action(delegate
                                            //{
                                            //    ControlWindow.Txt_Current_U.Text = Convert.ToString(chindex);
                                            //}));

                                            break;

                                        case (byte)'8':
                                            chindex = Convert.ToInt32(param1, 16);
                                            ptindex = Convert.ToInt32(param2, 16);
                                            if (buffer[0] == (byte)'U' || buffer[0] == (byte)'h' || buffer[0] == (byte)'j' || buffer[0] == (byte)'k')
                                            {
                                                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                                                //{
                                                //    ControlWindow.Txt_Current_U.Text = Convert.ToString(chindex);

                                                //}));

                                            }
                                            else
                                            {
                                                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                                                //{
                                                //    ControlWindow.TXT_CURRENT_X.Text = chindex.ToString();
                                                //    ControlWindow.TXT_CURRENT_Y.Text = ptindex.ToString();
                                                //}));

                                            }
                                            if (buffer[0] == (byte)'R')
                                            {

                                                BatchJobComplte = true;
                                                //S_TIME = DateTime.Now;
                                                //ControlWindow.Dispatcher.Invoke(new Action(delegate
                                                //{
                                                //    if (BatchJobStop) { ControlWindow.Batch_Start.IsEnabled = true; }
                                                //    ControlWindow.lB_Marking_count.Text = "Marking Count: " + Mark_Counter;
                                                //    ControlWindow.txt_log.AppendText(DateAndTime.Now + " Mark Sequence Complete (" + Mark_Counter + ")" + Environment.NewLine);
                                                //    ControlWindow.txt_log.ScrollToEnd();
                                                //    ControlWindow.cycle_time.Content = " CYCLE TIME:  " + (S_TIME - W_TIME).Minutes + "분: " + (S_TIME - W_TIME).Seconds + "초: " + (S_TIME - W_TIME).Milliseconds;
                                                //}));
                                            }
                                            MarkController2.Pointdone = true;

                                            exitFlag = true;
                                            break;

                                        default:
                                            break;
                                    }
                                }
                                break;
                            case (byte)'V':
                            case (byte)'I':
                                retval = (buffer[3] & 0x0f) << 12 + (buffer[4] & 0x0f) << 8 + (buffer[5] & 0x0f) << 4 + (buffer[6] & 0x0f);
                                break;

                            default:
                                exitFlag = true;
                                break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                return -3;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, "END");
            return retval;
        }

        public byte GetBCC(byte[] inputStream, int offset, int count)
        {
            byte bcc = 0;
            if ((inputStream != null) && (inputStream.Length > 0))
            {
                for (int i = offset; i < count; i++)
                {
                    if (i < inputStream.Length)
                        bcc ^= inputStream[i];
                    else
                        break;
                }
            }
            return bcc;
        }

        public int ExecuteCommandMsg2(int screenNo, int loglevel, int timeoutsec, string cmd, string payload)
        {
            string ClassName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string FuncName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, "START");

            Stopwatch sw = new Stopwatch();

            try
            {
                var retval = SendCommandMsg(screenNo, loglevel, cmd, payload);

                if (retval < 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("SendCommandMsg ERROR : {0}", retval));

                    return retval;
                }

                ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", ClassName, FuncName, "END");

                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));

                return -10;
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// //절대 좌표 직선이동 LINE
        /// </summary>
        /// <param name="movetype">
        ///     0 = Off --- Off
        ///     1 = Off --- On	// 0, 1    : Free Run    
        ///     2 = On  --- x
        ///     3 = On  --- Off	// Not used
        ///     4 = On  --- Off
        /// </>
        //////////// /// XY STAGE////////////////////
        ///
        public void FontFlush(int screenNo)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, "B", "");
        }

        public void FontData(int screenNo, string cmd, string PointData)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, PointData);
        }

        public void Opmode(int screenNo, string cmd, string opmode)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, opmode);
        }

        public void StrikeNo(int screenNo, string cmd, string strike)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, strike);
        }

        public void Run_S(int screenNo, string cmd, string run)
        {

            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 5, cmd, run);
        }

        public void SolOnOffTime(int screenNo, string cmd, string onofftime)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, onofftime);

        }

        public void DwellTime(int screenNo, string cmd, string dwelltime)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, dwelltime);
        }

        public void FreeSpeed(int screenNo, string cmd, string Speed)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Speed);
        }


        public void LoadSpeed(int screenNo, string cmd, string Speed)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Speed);

        }

        public void Resume(int screenNo, string cmd, string R_value)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, R_value);

        }

        public void GoHome(int screenNo, string cmd, string OffsetXY)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 3, cmd, OffsetXY);
        }


        public void Jog_XY(int screenNo, string cmd, string XY)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, XY);

        }

        public void TestSolFet(int screenNo, string cmd, short Fet, bool Sol)
        {
            string OnOff;
            if (Sol == true)
            {
                OnOff = string.Concat(Fet.ToString("X4"), 1.ToString("X4"));
            }
            else
            {
                OnOff = string.Concat(Fet.ToString("X4"), 0.ToString("X4"));
            }

            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, OnOff);

        }


        public void TestBox4(int screenNo, string cmd)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, "");
        }

        public void GoPoint(int screenNo, string cmd, string XYS)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, XYS);
        }

        public void GoParking(int screenNo, string cmd, string XYS)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, XYS);
        }

        public void GearRatio(int screenNo, string cmd, String value)
        {

            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, value);

        }

        public void SetMaxMinXY(int screenNo, string cmd, String Value)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Value);
        }

        public void Set_Head_Area(int screenNo, string cmd, string Head_Lenght)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Head_Lenght);

        }

        public void Set_Head_Scan(int screenNo, string cmd, string Head_Lenght)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Head_Lenght);
        }

        public int GetFwVersion(int screenNo, string cmd)
        {
            int response;

            lock (cmdLock)
            {
                response = ExecuteCommandMsg2(screenNo, 0, 2, cmd, "");
            }

            return response;
        }

        public int Inport(int screenNo, string cmd)
        {
            int response;

            lock (cmdLock)
            {
                response = ExecuteCommandMsg2(screenNo, 0, 2, cmd, "");
            }

            return response;
        }


        ///////////// Profile Scan/////////////////////
        public void Scan(int screenNo, string cmd, string U_value)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, U_value);
        }

        public void Profile_Speed(int screenNo, string cmd, string Speed_value)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Speed_value);

        }

        public void Home_U(int screenNo, string cmd, string Offset_Value)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Offset_Value);

        }


        public void Jog_U(int screenNo, string cmd, string Jog_Value)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Jog_Value);
        }

        public void GoParking_U(int screenNo, string cmd, string Parking_U)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Parking_U);
        }

        public void GearRatio_U(int screenNo, string cmd, string Gear_Value)
        {
            lock (cmdLock) _ = ExecuteCommandMsg2(screenNo, 0, 2, cmd, Gear_Value);
        }

        public async void START_BATCH(int screenNo, String ini_fn)
        {
            {
                MPOINT SP = new MPOINT();
                MPOINT[] Rev_Point = new MPOINT[1];
                int XF;
                int YF;
                string[] TEMPF;
                double Step_W;
                double Step_H;
                string Font = "";
                string X_Position = "";
                string Y_Position = "";
                string Height = "";
                string Width = "";
                string Pitch = "";
                string Angle = "";
                string Vinnum = "";
                Stopwatch sw = new Stopwatch();
                TimeSpan timeout = TimeSpan.FromSeconds(25);

                short Ispeed = (short)Util.GetPrivateProfileValueInt("LOAD", "INITIAL", 0, ini_fn);    // load  Initial Speed ;
                short Tspeed = (short)Util.GetPrivateProfileValueInt("LOAD", "TARGET", 0, ini_fn);     // load  Target Speed ;
                short Accel = (short)Util.GetPrivateProfileValueInt("LOAD", "ACCEL", 0, ini_fn);       // load  Accel Speed ;
                short Decel = (short)Util.GetPrivateProfileValueInt("LOAD", "DECEL", 0, ini_fn);       // load  Decel Speed ;

                short fIspeed = (short)Util.GetPrivateProfileValueInt("NOLOAD", "INITIAL", 0, ini_fn);    // free  Initial Speed ;
                short fTspeed = (short)Util.GetPrivateProfileValueInt("NOLOAD", "TARGET", 0, ini_fn);     // free  Target Speed ;
                short fAccel = (short)Util.GetPrivateProfileValueInt("NOLOAD", "ACCEL", 0, ini_fn);       // free  Accel Speed ;
                short fDecel = (short)Util.GetPrivateProfileValueInt("NOLOAD", "DECEL", 0, ini_fn);       // free  Decel Speed ;

                short Sol_On = (short)Util.GetPrivateProfileValueInt("SOL", "SOL_ON", 0, ini_fn);      // load  Sol_on Time;
                short Sol_Off = (short)Util.GetPrivateProfileValueInt("SOL", "SOL_OFF", 0, ini_fn);     // load  Sol_Off_Time;

                short Strike = (short)Util.GetPrivateProfileValueInt("VINDATA", "STRIKE", 1, ini_fn);  // load  Strike;


                var SetSpeed = String.Concat(Ispeed.ToString("X4"), Tspeed.ToString("X4"), Accel.ToString("X4"), Decel.ToString("X4")); // set speed
                LoadSpeed(screenNo, "L", SetSpeed);  // send speed to MCU
                var fSetSpeed = String.Concat(fIspeed.ToString("X4"), fTspeed.ToString("X4"), fAccel.ToString("X4"), fDecel.ToString("X4")); // set speed
                FreeSpeed(screenNo, "F", fSetSpeed);  // send speed to MCU

                var SetSol_OnOff = String.Concat(Sol_On.ToString("X4"), Sol_Off.ToString("X4"));  // set Sol OnOff time 
                SolOnOffTime(screenNo, "S", SetSol_OnOff);   // send Sol OnOff time 

                var Strikes = String.Concat(Strike.ToString("X4")); // set Strike 
                StrikeNo(screenNo, "N", Strikes);  // send Marking couter

                string curDir = AppDomain.CurrentDomain.BaseDirectory;
                string filepath = curDir + "vinbatch.txt";

                if (!File.Exists(filepath))
                {
                    return;
                }
                using (StreamReader file = new StreamReader(filepath))
                {

                    string ln;
                    string[] PARAMETER;
                    BatchJobComplte = false;

                    var spin = new SpinWait();

                    while ((ln = file.ReadLine()) != null)
                    {
                        //W_TIME = DateTime.Now;
                        //FontFlush();

                        //PARAMETER = ln.Split('/');

                        //if (PARAMETER.Length > 7)
                        //{
                        //    Vinnum = PARAMETER[0];
                        //    Font = PARAMETER[1];
                        //    X_Position = PARAMETER[2];
                        //    Y_Position = PARAMETER[3];
                        //    Height = PARAMETER[4];
                        //    Width = PARAMETER[5];
                        //    Pitch = PARAMETER[6];
                        //    Angle = PARAMETER[7];
                        //}

                        //Pattern pattern = new Pattern();
                        //pattern.fontName = Font;
                        //pattern.width = Convert.ToDouble(Width);
                        //pattern.height = Convert.ToDouble(Height);
                        //pattern.pitch = Convert.ToDouble(Pitch);
                        //pattern.thickness = 0.4;
                        //string vin = Vinnum;
                        //ControlWindow.ClearCurrnetMarkingInformation();
                        //ControlWindow.showRecogCharacters(vin, pattern);
                        //currMarkInfo.Initialize();
                        //string ErrorCode = "";

                        //for (int t = 0; t < vin.Length; t++)
                        //{
                        //    List<FontData> fontData = new List<FontData>();
                        //    GetOneCharacterFontData(vin[t], pattern.fontName, ref fontData, out currMarkInfo.fontSizeX, out currMarkInfo.fontSizeY, out ErrorCode);
                        //    currMarkInfo.fontData.Add(fontData);

                        //}
                        //currMarkInfo.isReady = true;
                        Mode_File.LOAD_FONT(Font + ".fon"); // load Font
                        TEMPF = (Mode_File.FONT_[1]).Split(','); // 5X7; 11X16
                        XF = int.Parse(TEMPF[0]);
                        YF = int.Parse(TEMPF[1]);
                        Step_W = double.Parse(Width) / (XF - 1) * Mode_File.Step_Length;
                        Step_H = double.Parse(Height) / (YF - 1) * Mode_File.Step_Length;

                        SP.X = double.Parse(X_Position) * Mode_File.Step_Length;
                        SP.Y = double.Parse(Y_Position) * Mode_File.Step_Length;

                        Mode_File.gMaxX = 0;
                        Mode_File.gMaxY = 0;
                        Mode_File.gMinX = 20000;
                        Mode_File.gMinY = 20000;
                        Mode_File.AREA_Test = false;

                        int i;
                        int j;
                        int idx = 0;
                        string[] xy_Data;
                        string[] xy_;

                        Mode_File.GET_START_POS_LINEAR(Vinnum.Length, SP, SP, double.Parse(Pitch) * Mode_File.Step_Length, double.Parse(Angle), ref Rev_Point);

                        int charNo = 0;
                        for (i = 0; i < Rev_Point.Length; i++)
                        {
                            if (ln.Substring(i, 1) != " ")      //Space Skip
                            {
                                Int32.TryParse(ln.Substring(i, 1), out charNo);
                                xy_Data = (Mode_File.FONT_[charNo]).Split(';');
                                //xy_Data = Mode_File.FONT_[Strings.Asc(ln.Substring(i, 1))].Split(';');

                                for (j = 0; j < xy_Data.Length - 1; j++) //From big Arrays GET SMALL Arrays in Font
                                {
                                    xy_ = xy_Data[j].Split(',');

                                    MPOINT M = new MPOINT
                                    {
                                        X = Rev_Point[i].X + (int.Parse(xy_[0]) * Step_W),
                                        Y = Rev_Point[i].Y + (int.Parse(xy_[1]) * Step_H)
                                    };

                                    switch (Font)
                                    {
                                        case "S_5X7":
                                            M.Y = Rev_Point[i].Y + ((double.Parse(xy_[1]) - 3) * Step_H);
                                            break;
                                        case "S_11X16":
                                            M.Y = Rev_Point[i].Y + ((double.Parse(xy_[1]) - 5) * Step_H);
                                            break;

                                    }

                                    M = Mode_File.Rotate_Point(M.X, M.Y, Rev_Point[i].X, Rev_Point[i].Y, double.Parse(Angle));

                                    M.S = double.Parse(xy_[2]);
                                    fontdata[idx].cN = (byte)i; fontdata[idx].fN = (byte)j;
                                    fontdata[idx].mX = Convert.ToUInt16(M.X + 0.5); fontdata[idx].mY = Convert.ToUInt16(M.Y + 0.5); fontdata[idx].mF = Convert.ToByte(M.S);
                                    var m_font = string.Concat(fontdata[idx].cN.ToString("X4"), fontdata[idx].fN.ToString("X4"), fontdata[idx].mX.ToString("X4"), fontdata[idx].mY.ToString("X4"), fontdata[idx].mF.ToString("X4"));
                                    idx++;

                                    FontData(screenNo, "D", m_font); // send Point to MCU
                                }

                            }
                        }
                        Mark_Counter++;

                        Run_S(screenNo, "R", 0.ToString("X4"));

                        sw.Start();
                        while (BatchJobComplte == false)
                        {
                            spin.SpinOnce();
                        }
                        sw.Stop();
                        if (!BatchJobStop) { BatchJobComplte = false; }
                        else
                        {
                            file.Close();
                            break;
                        }
                    }
                    file.Close();
                    string name = "BATCH START";
                    //ControlWindow.Dispatcher.Invoke(new Action(delegate
                    //{
                    //    ControlWindow.Batch_Start.Content = name;
                    //    ControlWindow.Batch_Start.Background = Brushes.Green;
                    //}));
                }

            }
        }

        private void Btn_AddData_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Start_TEXT(int screenNo, string DATA_, string Pattern) // Start Text
        {
            COMMAND_RESULT retval = new COMMAND_RESULT();

            MPOINT SP = new MPOINT();
            MPOINT CP = new MPOINT();
            MPOINT[] Rev_Point = new MPOINT[1];
            int XF;
            int YF;
            string[] TEMPF;
            double Step_W;
            double Step_H;
            string Font = "";

            Util.GetPrivateProfileValue("VINDATA", "FONT", "", ref Font, Pattern);                 // load Font

            string X_Position = "";

            Util.GetPrivateProfileValue("VINDATA", "X", "0.0", ref X_Position, Pattern);            // load X position

            string Y_Position = "";
            Util.GetPrivateProfileValue("VINDATA", "Y", "0.0", ref Y_Position, Pattern);           // load Y position ;

            string Height = "";
            Util.GetPrivateProfileValue("VINDATA", "HEIGHT", "0.0", ref Height, Pattern);          // load Height;

            string Width = "";
            Util.GetPrivateProfileValue("VINDATA", "WIDTH", "0.0", ref Width, Pattern);           // load  Width;

            string Pitch = "";
            Util.GetPrivateProfileValue("VINDATA", "PITCH", "0.0", ref Pitch, Pattern);           // load Pitch;

            string Angle = "";
            Util.GetPrivateProfileValue("VINDATA", "ANGLE", "0.0", ref Angle, Pattern);           // load  Angle;

            short Ispeed = (short)Util.GetPrivateProfileValueInt("LOAD", "INITIAL", 0, Pattern);    // load  Initial Speed ;

            short Tspeed = (short)Util.GetPrivateProfileValueInt("LOAD", "TARGET", 0, Pattern);     // load  Target Speed ;

            short Accel = (short)Util.GetPrivateProfileValueInt("LOAD", "ACCEL", 0, Pattern);       // load  Accel Speed ;

            short Decel = (short)Util.GetPrivateProfileValueInt("LOAD", "DECEL", 0, Pattern);       // load  Decel Speed ;

            short Sol_On = (short)Util.GetPrivateProfileValueInt("SOL", "SOL_ON", 0, Pattern);      // load  Sol_on Time;

            short Sol_Off = (short)Util.GetPrivateProfileValueInt("SOL", "SOL_OFF", 0, Pattern);     // load  Sol_Off_Time;

            short Dwell_Time = (short)Util.GetPrivateProfileValueInt("SOL", "DWELL", 0, Pattern);     // load  Dwell_Time;

            short Strike = (short)Util.GetPrivateProfileValueInt("VINDATA", "STRIKE", 0, Pattern);    // load  Strike;


            var SetSpeed = String.Concat(Ispeed.ToString("X4"), Tspeed.ToString("X4"), Accel.ToString("X4"), Decel.ToString("X4"));               // set speed
            LoadSpeed(screenNo, "L", SetSpeed);               // send speed to MCU
            var SetSol_OnOff = String.Concat(Sol_On.ToString("X4"), Sol_Off.ToString("X4"));                                                      // set Sol OnOff time 
            SolOnOffTime(screenNo, "S", SetSol_OnOff);        // send Sol OnOff time
            var SetDwell_Time = String.Concat(Dwell_Time.ToString("X4"));                                                                        // set Dwell time 
            DwellTime(screenNo, "d", SetDwell_Time);          // send Dwell time 
            var Strikes = String.Concat(Strike.ToString("X4"));                                                                                  // set Strike 
            StrikeNo(screenNo, "N", Strikes);                 // send Marking couter


            Mode_File.LOAD_FONT(Font + ".fon");      // load Font
            TEMPF = (Mode_File.FONT_[1]).Split(','); // 5X7; 11X16
            XF = int.Parse(TEMPF[0]);
            YF = int.Parse(TEMPF[1]);
            Step_W = double.Parse(Width) / (XF - 1) * Mode_File.Step_Length;
            Step_H = double.Parse(Height) / (YF - 1) * Mode_File.Step_Length;

            SP.X = double.Parse(X_Position) * Mode_File.Step_Length;
            SP.Y = double.Parse(Y_Position) * Mode_File.Step_Length;
            CP.X = (double.Parse(Pitch) * (DATA_.Length - 1) + double.Parse(Width)) * Mode_File.Step_Length / 2.0;
            CP.Y = double.Parse(Height) * Mode_File.Step_Length / 2.0;
            CP.X += SP.X; CP.Y += SP.Y;

            Mode_File.gMaxX = 0;
            Mode_File.gMaxY = 0;
            Mode_File.gMinX = 20000;
            Mode_File.gMinY = 20000;
            Mode_File.AREA_Test = false;

            int i;
            int j;
            int idx = 0;
            string[] xy_Data;
            string[] xy_;

            Mode_File.GET_START_POS_LINEAR(DATA_.Length, CP, SP, double.Parse(Pitch) * Mode_File.Step_Length, double.Parse(Angle), ref Rev_Point);

            for (i = 0; i < Rev_Point.Length; i++)
            {
                int charNo = 0;
                if (DATA_.Substring(i, 1) != " ")      //Space Skip
                {
                    Int32.TryParse(DATA_.Substring(i, 1), out charNo);
                    //xy_Data = (Mode_File.FONT_[Strings.Asc(DATA_.Substring(i, 1))]).Split(';');
                    xy_Data = (Mode_File.FONT_[charNo]).Split(';');

                    for (j = 0; j < xy_Data.Length - 1; j++) //From big Arrays GET SMALL Arrays in Font
                    {
                        xy_ = (xy_Data[j]).Split(',');

                        MPOINT M = new MPOINT
                        {
                            X = Rev_Point[i].X + int.Parse(xy_[0]) * Step_W,
                            Y = Rev_Point[i].Y + int.Parse(xy_[1]) * Step_H
                        };

                        switch (Font)
                        {
                            case "S_5X7":
                                M.Y = Rev_Point[i].Y + ((double.Parse(xy_[1])) - 3) * Step_H;
                                break;
                            case "S_11X16":
                                M.Y = Rev_Point[i].Y + ((double.Parse(xy_[1])) - 5) * Step_H;
                                break;

                        }

                        M = Mode_File.Rotate_Point(M.X, M.Y, Rev_Point[i].X, Rev_Point[i].Y, double.Parse(Angle));

                        M.S = double.Parse(xy_[2]);
                        fontdata[idx].cN = (byte)i; fontdata[idx].fN = (byte)j;
                        fontdata[idx].mX = Convert.ToUInt16(M.X + 0.5); fontdata[idx].mY = Convert.ToUInt16(M.Y + 0.5); fontdata[idx].mF = Convert.ToByte(M.S);
                        var m_font = string.Concat(fontdata[idx].cN.ToString("X4"), fontdata[idx].fN.ToString("X4"), fontdata[idx].mX.ToString("X4"), fontdata[idx].mY.ToString("X4"), fontdata[idx].mF.ToString("X4"));

                        if (M.S != 0.0)
                        {
                            if (M.X > Mode_File.gMaxX)
                            {
                                Mode_File.gMaxX = (short)(M.X + 0.5);
                            }

                            if (M.X < Mode_File.gMinX)
                            {
                                Mode_File.gMinX = (short)(M.X + 0.5);
                            }

                            if (M.Y > Mode_File.gMaxY)
                            {
                                Mode_File.gMaxY = (short)(M.Y + 0.5);
                            }

                            if (M.Y < Mode_File.gMinY)
                            {
                                Mode_File.gMinY = (short)(M.Y + 0.5);
                            }

                            Mode_File.AREA_Test = true;
                            Mode_File.Download_Data = true;
                        }

                        idx++;

                        FontData(screenNo, "D", m_font); // send Point to MCU

                    }
                }
            }
            M_Count = idx;
            Mark_Counter++;
        }

        public async void Range_Test(int screenNo)
        {
            try
            {
                var Range_MaxMinXY = string.Concat(screenNo, Mode_File.gMinX.ToString("X4"), Mode_File.gMaxX.ToString("X4"), Mode_File.gMinY.ToString("X4"), Mode_File.gMaxY.ToString("X4"));

                var jobtask = Task.Run(() => SetMaxMinXY(screenNo, "X", Range_MaxMinXY));
                await jobtask;
                Thread.Sleep(1);
                var jobtask1 = Task.Run(() => TestBox4(screenNo, "C"));
                await jobtask1;
            }
            catch (Exception)
            {
                MessageBox.Show("Range_Test() error", "Error", MessageBoxButton.OK);
            }
        }
        public void Head_Range_Test(int screenNo)
        {
            COMMAND_RESULT retval = new COMMAND_RESULT();
            Stopwatch sw = new Stopwatch();
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            try
            {
                var Range_MinXY = string.Concat(0.ToString("X4"), 0.ToString("X4"), 0.ToString("X4"));
                var Range_MinXMaxY = string.Concat(0.ToString("X4"), Mode_File.MAX_Y.ToString("X4"), 0.ToString("X4"));
                var Range_MaxXMaxY = string.Concat(Mode_File.MAX_X.ToString("X4"), Mode_File.MAX_Y.ToString("X4"), 0.ToString("X4"));
                var Range_MaxXMinY = string.Concat(Mode_File.MAX_X.ToString("X4"), 0.ToString("X4"), 0.ToString("X4"));

                Pointdone = false;
                GoPoint(screenNo, "M", Range_MinXMaxY);
                sw.Start();
                while (Pointdone == false)
                {
                    Task.Delay(100);
                    if (sw.Elapsed > timeout)
                        return;
                }
                sw.Stop();

                Pointdone = false;
                GoPoint(screenNo, "M", Range_MaxXMaxY);
                while (Pointdone == false)
                {
                    Task.Delay(100);
                    if (sw.Elapsed > timeout)
                        return;
                }
                sw.Stop();

                Pointdone = false;
                GoPoint(screenNo, "M", Range_MaxXMinY);
                while (Pointdone == false)
                {
                    Task.Delay(100);
                    if (sw.Elapsed > timeout)
                        return;
                }
                sw.Stop();

                Pointdone = false;
                GoPoint(screenNo, "M", Range_MinXY);
                while (Pointdone == false)
                {
                    Task.Delay(100);
                    if (sw.Elapsed > timeout)
                        return;
                }
                sw.Stop();

                Pointdone = false;
                GoPoint(screenNo, "M", Range_MinXMaxY);
                while (Pointdone == false)
                {
                    Task.Delay(100);
                    if (sw.Elapsed > timeout)
                        return;
                }
                sw.Stop();

                Pointdone = false;
                GoPoint(screenNo, "M", Range_MaxXMinY);
                while (Pointdone == false)
                {
                    Task.Delay(100);
                    if (sw.Elapsed > timeout)
                        return;
                }
                sw.Stop();

                Pointdone = false;
                GoPoint(screenNo, "M", Range_MinXMaxY);
                while (Pointdone == false)
                {
                    Task.Delay(100);
                    if (sw.Elapsed > timeout)
                        return;
                }
                sw.Stop();
            }

            catch (Exception)
            {

            }
        }

        public void READY_IO(int screenNo, bool OnOff)
        {
            COMMAND_RESULT retval = new COMMAND_RESULT();

            if (OnOff == true)
            {
                TestSolFet(screenNo, "O", 0, true);
            }
            else
            {
                TestSolFet(screenNo, "O", 0, false);
            }
        }

        public void DONE_IO(int screenNo, bool OnOff)
        {

            COMMAND_RESULT retval = new COMMAND_RESULT();

            if (OnOff == true)
            {
                TestSolFet(screenNo, "O", 1, true);
            }
            else
            {
                TestSolFet(screenNo, "O", 1, false);
            }
        }
    }

    public class COMMAND_RESULT : ICloneable
    {
        public int execResult;
        public int recvType;
        int bufSize;
        public string recvString;
        public byte[] recvBuffer;
        public int recvSize;

        public COMMAND_RESULT()
        {
            execResult = 0;
            recvType = 0;
            recvString = "";
            recvBuffer = new byte[1024];
            bufSize = 1024;
            recvSize = 0;
        }

        public COMMAND_RESULT(int bufsize)
        {
            execResult = 0;
            recvType = 0;
            recvString = "";
            recvBuffer = new byte[bufsize];
            this.bufSize = bufsize;
            recvSize = 0;
        }

        public object Clone()
        {
            COMMAND_RESULT ret = new COMMAND_RESULT(this.bufSize);
            ret.execResult = execResult;
            ret.recvType = recvType;
            ret.bufSize = bufSize;
            ret.recvString = recvString;
            Array.Copy(recvBuffer, ret.recvBuffer, bufSize);
            ret.recvSize = recvSize;
            return ret;
        }
    }
}
