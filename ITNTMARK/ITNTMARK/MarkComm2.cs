using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ITNTCOMMM;
using ITNTCOMMON;
using ITNTUTIL;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;

namespace ITNTMARK
{
    class MarkComm2
    {
        #region Property
        public event MarkControllerStatusDataEventHandler MarkControllerDataArrivedEventFunc;

        private byte NAKError = 0;
        private byte[] RecvEventData = new byte[2048];
        private byte[] RecvCommandData = new byte[2048];
        private int RecvEventLength = 0;
        private int RecvFrameLength = 0;
        private readonly object bufferLock = new object();
        private readonly object cmdLock = new object();

        //int cmdIndex = 0;

        bool doingCommand = false;

        //명령 실행 완료 확인 - 완료 응답 수신 여부
        bool isReceiveEndFlag = false;

        Thread statusThread;
        bool doingThread = false;

        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;

        protected static SerialPort Port = new SerialPort();
        protected RingBuffer cb;
        private readonly object comLock = new object();
        //private readonly object cbLock = new object();
        private object evtLock = new object();
        public bool IsOpen = false;
        #endregion

        public MarkComm2()
        {
            Port = new SerialPort();
            cb = new RingBuffer(0x2000);

            SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        }

        ~MarkComm2()
        {
            if ((Port != null) && (Port.IsOpen))
                ClosePort();
        }


        public int OpenMarkDevice()
        {
            int retval = 0;
            string value = "";
            string portnum = "COM";
            int baud = 19200;

            Util.GetPrivateProfileValue("MARK", "PORT", "COM1", ref portnum, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            //portnum += value;
            Util.GetPrivateProfileValue("MARK", "BAUDRATE", "COM1", ref value, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            Int32.TryParse(value, out baud);

            retval = OpenDevice(portnum, baud, 8, Parity.None, StopBits.One);
            return retval;
        }

        public async Task<int> OpenMarkDeviceAsync()
        {
            int retval = 0;
            string value = "";
            string portnum = "COM";
            int baud = 19200;

            Util.GetPrivateProfileValue("MARK", "PORT", "COM1", ref portnum, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            //portnum += value;
            Util.GetPrivateProfileValue("MARK", "BAUDRATE", "COM1", ref value, ITNTCOMMON.Constants.PARAMS_INI_FILE);
            Int32.TryParse(value, out baud);

            retval = await OpenDeviceAsync(portnum, baud, 8, Parity.None, StopBits.One);
            return retval;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="baud"></param>
        /// <param name="databits"></param>
        /// <param name="parity"></param>
        /// <param name="stopbit"></param>
        /// <param name="readtimeout"></param>
        /// <param name="writetimeout"></param>
        /// <returns></returns>
        public int OpenDevice(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;

#if TEST_DEBUG_MARK
            retval = 0;
            doingThread = true;
            statusThread = new Thread(ThreadStatusCheck2);
            statusThread.Start();

#else
            if ((Port != null) && Port.IsOpen)
                return 0;

            retval = OpenPort(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
            if (retval == 0)
            {
                statusThread = new Thread(ThreadStatusCheck2);
                statusThread.Start();
                doingThread = true;
                Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                IsOpen = true;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<int> OpenDeviceAsync(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            int retval = 0;

#if TEST_DEBUG_MARK
            retval = 0;
#else
            if ((Port != null) && (Port.IsOpen))
                return 0;

            retval = await OpenPortAsync(port, baud, databits, parity, stopbit, readtimeout, writetimeout);
            if (retval == 0)
            {
                statusThread = new Thread(ThreadStatusCheck2);
                statusThread.Start();
                doingThread = true;
                Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort SUCCESS : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("OpenPort ERROR : {0}", retval), Thread.CurrentThread.ManagedThreadId);
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
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
                if (Port.IsOpen)
                {
                    retval = 0;
                }
                else
                    retval = -1;
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
            }

            return retval;
        }

        protected async Task<int> OpenPortAsync(string port, int baud, int databits, Parity parity, StopBits stopbit, int readtimeout = -1, int writetimeout = -1)
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

                Task task = new Task(new Action(delegate
                {
                    Port.Open();
                }));

                await task;
                if (Port.IsOpen)
                    retval = 0;
                else
                    retval = -1;
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
            }

            //Port.DataReceived += new SerialDataReceivedEventHandler(OnDataReceivedHandler);

            return retval;
        }

        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] recv = new byte[0x2000];
            byte[] tmp = new byte[0x2000];
            int readsize = 0;
            int totsize = 0;
            lock (bufferLock)
            {
                SerialPort port = (SerialPort)sender;
                int size = port.BytesToRead;
                if (size <= 0)
                    return;
                while (size > 0)
                {
                    readsize = port.Read(recv, 0, size);
                    cb.Put(recv, readsize);
                    size = port.BytesToRead;
                    totsize += readsize;
                }
                ReceiveCommData();
            }
        }

        /// <summary>
        /// Close Port
        /// </summary>
        /// <returns></returns>
        public int CloseDevice()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            doingThread = false;

            //DoReading = false;
            //_readThread.Join(); //block until exits
            //_readThread.Abort();
            //_readThread = null;
#if TEST_DEBUG_MARK
            int retval = 0;
#else

            if ((Port == null) || (Port.IsOpen == false))
                return 0;

            doingThread = false;
            //statusThread.Join();

            int retval = 0;
            lock (cmdLock)
            {
                Port.DataReceived -= DataReceivedHandler;
                ClosePort();
            }
#endif
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
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

            //lock (thisLock)
            //{
            //}
        }

        public string[] GetSerialPorts()
        {
            string[] portNames = SerialPort.GetPortNames();
            return portNames;
        }

        /// <summary>
        /// Thread for Reading Serial Port
        /// </summary>
        //private async void ReadPortThread()
        //{
        //    int readsize = 0;
        //    while (DoReading)
        //    {
        //        if (Port.IsOpen && (Port.BytesToRead > 0))
        //        {
        //            byte[] readBuffer = new byte[Port.ReadBufferSize + 1];
        //            try
        //            {
        //                // If there are bytes available on the serial port,
        //                // Read returns up to "count" bytes, but will not block (wait)
        //                // for the remaining bytes. If there are no bytes available
        //                // on the serial port, Read will block until at least one byte
        //                // is available on the port, up until the ReadTimeout milliseconds
        //                // have elapsed, at which time a TimeoutException will be thrown.
        //                int count = ReadPort(Port.ReadBufferSize, ref readsize, ref readBuffer);
        //                //ITNTTraceLog.Instance.TraceHex(0, "ITNTSerialComm::ReadPortThread()  RECV DATA : ", readsize, ref readBuffer, "");
        //                if (readsize > 0)
        //                {
        //                    lock (bufferLock)
        //                    {
        //                        //cb.Put(readBuffer, 0, readsize);
        //                        cb.Put(readBuffer, readsize);
        //                    }
        //                    ReceiveCommData();
        //                }
        //            }
        //            catch (TimeoutException) { }
        //        }
        //        else
        //        {
        //            //TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, 50);
        //            //Thread.Sleep(waitTime);
        //            await Task.Delay(50);
        //        }
        //    }
        //}

        /// <summary>
        /// 수신 데이터 분석 및 수신 버퍼에 저장
        /// </summary>
        //public void ReceiveCommData2()
        //{
        //    string className = "ITNTSerialComm";
        //    string funcName = "ReceiveCommData";
        //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString());

        //    int count = 0;
        //    int retval = 0;
        //    byte[] tmp = new byte[2];
        //    try
        //    {
        //        count = cb.GetSize();
        //        if (count <= 0)
        //        {
        //            return;
        //        }

        //        retval = cb.LookReverse(tmp, 2);
        //        if ((retval >= 2) && (tmp[0] == 0x05) && (tmp[1] == LF))
        //        {
        //            cb.Get(ref RecvCommandData, count);
        //            RecvFlag = RECVFLAG_RECV_END;
        //            RecvFrameLength = count;
        //        }
        //        else
        //            return;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //}


        private async void OnMarkControllerEventFunc(MarkControllerRecievedEvnetArgs e)
        {
            string param1 = "";
            string param2 = "";
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            //int drindex = 0;
            byte[] sensor = new byte[8];
            int retval = 0;
            short Length;
            short steplength;
            //ITNTResponseArgs retArg = new ITNTResponseArgs();
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            byte currCMD = 0;

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                //lock(eventLock)
//                {
//                    i = 6;
//                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
//                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
//                    Array.Copy(e.receiveBuffer, i, sensor, 0, 4);
//                    retval = CheckMarkControllerSensor(sensor, 4);

//                    //if (currentWindow != 0)
//                    //    return;
//                    currCMD = e.execmd;

//                    switch (e.stscmd)
//                    {
//                        case 0x30:      //stand by
//                            ITNTTraceLog.Instance.TraceHex(1, "MainWindow::OnMarkControllerEventFunc()  RECV MARK :  ", e.receiveSize, e.receiveBuffer);

//                            if (param1.Length > 0)
//                                chindex = Convert.ToInt32(param1, 16);
//                            if (param2.Length > 0)
//                                ptindex = Convert.ToInt32(param2, 16);

//                            if ((currCMD == 'R') && (m_currCMD == 'R'))
//                            {
//                                if (!m_bDoingMarkingFlag)
//                                    m_bDoingMarkingFlag = true;

//                                if (this.CheckAccess())
//                                    ShowMarkingOneLine(chindex, ptindex);
//                                else
//                                {
//                                    this.Dispatcher.Invoke(new Action(delegate
//                                    {
//                                        ShowMarkingOneLine(chindex, ptindex);
//                                    }));
//                                }
//                            }
//                            //Task.Delay(100);
//                            break;

//                        case 0x31:      //running
//                            break;

//                        case 0x32:      //run ok
//                            break;

//                        //case 0x33:      //home ok
//                        //    break;
//                        //case 0x34:      //jog ok
//                        //    break;
//                        //case 0x35:      //test ok
//                        //    break;
//                        //case 0x36:      //go ok
//                        //    break;
//                        case 0x37:      //cold boot
//                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "COLD BOOT");
//                            retval = InitializeController().Result.execResult;
//                            if (retval == 0)
//                            {
//                                doingCommand = true;
//                                Stopwatch sw = new Stopwatch();
//                                sw.Start();
//                                while (sw.Elapsed < TimeSpan.FromSeconds(6))
//                                {
//                                    if (!doingCommand)
//                                        break;

//                                    await Task.Delay(50);
//                                }
//                            }
//                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "COLD BOOT END", Thread.CurrentThread.ManagedThreadId);
//                            break;

//                        case 0x38:
//                            //count = 0;
//                            double dvalue = 0.0d;
//                            double.TryParse(param1, out dvalue);
//                            homePt.X = dvalue;
//                            double.TryParse(param1, out dvalue);
//                            homePt.Y = dvalue;

//                            doingCommand = false;
//                            if ((currCMD == 'R') && (m_currCMD == 'R'))
//                            {
//                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : RECEIVE MARKING COMPLETE");

//#if MANUAL_MARK
//                                //ShowCurrentStateLabel(5);
//                                ShowCurrentStateLabelManual(4);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
//#else
//                                ShowCurrentStateLabel(6);
//#endif
//                                ShowLabelData("[4] : MARKING COMPLETE", lblPLCData);
//                                ShowLabelData("MARKING COMPLETE", lblCheckResult);

//                                //
//                                currMarkInfo.mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
//                                currMarkInfo.mesData.marktime = DateTime.Now.ToString("HH:mm:ss");

//                                SaveMarkResultData(currMarkInfo.mesData, 0, 0);
//                                WriteCompleteData(currMarkInfo.mesData, 0);

//#if AGING_TEST_PLC
//#else
//                                row = GetNextMarkPointData();
//                                UpdatePlanDatabase(row);
//#endif
//                                //DeletePlanData();
//                                recvarg = await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE);
//                                if (recvarg.execResult != 0)
//                                {
//                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "SendMarkingStatus ERROR - " + recvarg.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
//                                    ShowErrorMessage("SEND COMPLETE SIGNAL TO PLC ERROR", false);
//                                }

//                                await ShowCurrentMarkingInformation(false, currMarkInfo.mesData.vin, currMarkInfo.mesData.sequence, currMarkInfo.mesData.rawcartype, currMarkInfo.pattern, 0);

//                                //markCompleteFlag = true;
//                                //Length = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, ITNTCOMMON.Constants.MARKING_INI_FILE);
//                                //steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);

//                                //Length = (short)(Length * steplength);
//                                //recvarg = await MarkControll.GoHome(0, Length);

//                                m_bDoingMarkingFlag = false;
//                                //m_bDoingNextVINFlag = false;

//                                Util.WritePrivateProfileValue("CURRENT", "VIN", currMarkInfo.mesData.vin, ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);
//                                //Util.WritePrivateProfileValue("CURRENT", "INDEX", m_CurrentMarkNum.ToString(), ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);
//                                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", currMarkInfo.mesData.sequence + "|" + currMarkInfo.mesData.vin, ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);

//                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.mesData.sequence + "-" + currMarkInfo.mesData.vin, Thread.CurrentThread.ManagedThreadId);
//                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.mesData.sequence + "-" + currMarkInfo.mesData.vin);

//                                //currMarkInfo.Initialize();
//                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : MARKING COMPLETE", Thread.CurrentThread.ManagedThreadId);
//                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : MARKING COMPLETE");
//                                markRunTimer.Stop();
//                                ShowCurrentStateLabel(7);
//                                m_currCMD = 0;

//                                int ivalue = 0;
//                                ivalue = (int)Util.GetPrivateProfileValueUINT("OPTION", "MarkingCount", 0, ITNTCOMMON.Constants.PARAMS_INI_FILE);
//                                ivalue++;
//                                string value = ivalue.ToString();
//                                ShowLabelData(value, lblMarkingCount);
//                                Util.WritePrivateProfileValue("OPTION", "MarkingCount", value, ITNTCOMMON.Constants.PARAMS_INI_FILE);
//                            }
//                            else
//                            {

//                            }
//                            break;

//                        case 0x39:      //emergency
//                            break;

//                        default:
//                            break;
//                    }
//                }

//                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        /// <summary>
        /// 수신 데이터 분석 및 수신 버퍼에 저장
        /// </summary>
        protected void ReceiveCommData()
        {
            string className = "ITNTSerialComm";
            string funcName = "ReceiveCommData";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString());

            int count = 0;
            //int retval = 0;
            int size = 0;
            int i = 0;
            byte tmp = 0;
            byte[] recv = new byte[0x2000];
            int idxSOH = -1;
            int idxCR = -1;
            string scount = "";
            int frameLeng = 0;
            //byte[] tmp = new byte[2];
            try
            {
                while ((count = cb.GetSize()) > 7)
                {
                    cb.Look(ref recv, count);
                    if (recv.Contains((byte)ASCII.SOH))
                    {
                        idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                        if (idxSOH >= 0)
                        {
                            if (idxSOH > 0)
                            {
                                for (i = 0; i < idxSOH; i++)
                                    cb.Get(ref tmp);
                                idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                            }

                            scount = Encoding.UTF8.GetString(recv, 1, 2);
                            frameLeng = Convert.ToInt32(scount, 16);
                            if (size < frameLeng)
                                return;


                            //if (recv.Contains((byte)ASCII.CR))
                            //{
                            //    //idxSOH = Array.IndexOf(recv, (byte)ASCII.SOH);
                            //    idxCR = Array.IndexOf(recv, (byte)ASCII.CR);//, idxSOH);
                            //    if (idxCR < 0)
                            //        return;
                            //}
                            //else
                            //    return;

                            //if (idxCR > idxSOH)
                            //{
                            //    size = idxCR - idxSOH + 1;
                            //    string cnt = Encoding.UTF8.GetString(recv, 1, 2);
                            //    int icnt = Convert.ToInt32(cnt, 16);

                            //    if ((recv[idxSOH + 4] >= '0') && (recv[idxSOH + 4] <= '9'))
                            //    {
                            //        size = cb.Look(ref RecvEventData, size);
                            //        if (size < 18)
                            //        {
                            //            ITNTTraceLog.Instance.Trace(0, "MarkComm::ReceiveCommData() SIZE={0}CR={1}", size, idxCR);
                            //            return;
                            //        }
                            //        size = cb.Get(ref RecvEventData, size);
                            //        RecvEventLength = size;
                            //        ITNTTraceLog.Instance.Trace(2, "MarkComm::ReceiveCommData() CR={0} SO={1}", idxCR, idxSOH);

                            //        MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();
                            //        arg.execmd = RecvEventData[idxSOH + 3];
                            //        arg.stscmd = RecvEventData[idxSOH + 4];
                            //        Array.Copy(RecvEventData, arg.receiveBuffer, RecvEventLength);
                            //        arg.receiveSize = RecvEventLength;
                            //        RecvEventData.Initialize();
                            //        RecvEventLength = 0;
                            //        idxCR = -1;
                            //        idxSOH = -1;
                            //        ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()  SEND MARK :  ", arg.receiveSize, arg.receiveBuffer);



                            //        //OnMarkControllerStatusDataEventHandler(arg);
                            //    }
                            //    else
                            //    {
                            //        cb.Get(ref RecvCommandData, size);
                            //        RecvFrameLength = size;
                            //        RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                            //        idxCR = -1;
                            //        idxSOH = -1;
                            //    }
                            //}
                        }
                        else// if (idxSOH < 0)
                            return;
                    }
                    else
                    {
                        Debug.WriteLine("!!!!");
                        return;
                    }
                    recv.Initialize();
                }
            }
            catch (Exception ex)
            {
                //IsEventFlag = false;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public async Task<int> SendCommandMsg(int loglevel, byte type, byte[] sendData, int sendLength)
        {
            string className = "MarkComm";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "SendCommandMsg";     // MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}::{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            //int size = 0;
            int retval = 0;
            int i = 0;
            byte[] sendmsg = new byte[128];
            int leng = 0;
            string strLeng = "";
            byte[] byLeng = new byte[2];
            try
            {
                leng = sendLength + 5;
                strLeng = leng.ToString("X2");
                byLeng = Encoding.UTF8.GetBytes(strLeng);

                sendmsg[i++] = (byte)ASCII.SOH;
                sendmsg[i++] = byLeng[0];
                sendmsg[i++] = byLeng[1];
                sendmsg[i++] = type;
                sendmsg[i++] = (byte)ASCII.STX;
                Array.Copy(sendData, 0, sendmsg, i, sendLength);
                i += sendLength;
                sendmsg[i++] = (byte)ASCII.ETX;
                sendmsg[i++] = GetBCC(sendmsg, 1, i - 1);
                sendmsg[i++] = (byte)ASCII.CR;

                retval = WritePort(sendmsg, 0, i);
                if (retval <= 0)
                {
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                    SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_ERR;
                    return retval;
                }
                isReceiveEndFlag = false;
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                SendFlag = (byte)SENDFLAG.SENDFLAG_SEND_END;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loglevel"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<ITNTResponseArgs> RecvResponseMsg(int loglevel, int timeout = 2)
        {
            //int retval = 0;
            string className = "MarkComm";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "RecvResponseMsg";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] respMsg = new byte[2048];
            int respSize = 0;
            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMPORTERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    return retval;
                }

                Array.Copy(RecvCommandData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK (SIZE = {0})", retval.recvSize), Thread.CurrentThread.ManagedThreadId);
                //respSize = RecvFrameLength-6;
                //if(respSize < 0)
                //{
                //    respSize = 0;
                //}
                //if(respSize > 0)
                //{
                //    retval.recvString = Encoding.UTF8.GetString(respMsg, 4, respSize);
                //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV : {0} / {1}", retval.recvString, respSize));
                //}
                //else
                //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK"));
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                return retval;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> RecvResponseMsgAsync2(int loglevel, int timeout1 = 2, int timeout2 = 2)
        {
            //int retval = 0;
            string className = "MarkComm";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "RecvResponseMsg";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] respMsg = new byte[2048];
            int respSize = 0;
            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout1))
                {
                    if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMPORTERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    return retval;
                }

                Array.Copy(RecvCommandData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK (SIZE = {0})", retval.recvSize), Thread.CurrentThread.ManagedThreadId);

                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout2))
                {
                    if (isReceiveEndFlag)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (!isReceiveEndFlag)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERR_DOING_COMMAND", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = (int)COMPORTERROR.ERR_DOING_COMMAND;
                    return retval;
                }

                //respSize = RecvFrameLength-6;
                //if(respSize < 0)
                //{
                //    respSize = 0;
                //}
                //if(respSize > 0)
                //{
                //    retval.recvString = Encoding.UTF8.GetString(respMsg, 4, respSize);
                //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV : {0} / {1}", retval.recvString, respSize));
                //}
                //else
                //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV OK"));
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                return retval;
            }
            return retval;
        }

        protected int WritePort(byte[] buffer, int offset, int count)
        {
            if (!Port.IsOpen)
                return (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
            lock (comLock)
            {
                Port.Write(buffer, offset, count);
            }
            if (Port.BytesToWrite > 0)
            {
                return (int)COMPORTERROR.ERR_SEND_DATA_FAIL;
            }
            return count;
        }

        public async Task<ITNTResponseArgs> ExecuteCommandMsg(int loglevel, int timeoutsec, byte type, byte[] sendData, int sendLength)
        {
            string className = "MarkComm";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ExecuteCommandMsg";// MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START - {0}", type), Thread.CurrentThread.ManagedThreadId);

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeoutsec))
                {
                    if (doingCommand == false)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (doingCommand == true)
                {
                    retval.execResult = (int)COMPORTERROR.ERR_COMMAND_BUSY;
                    return retval;
                }

                if (Port.IsOpen == false)
                {
                    retval.execResult = (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
                    doingCommand = false;
                    return retval;
                }

                doingCommand = true;

                //lock (cmdLock)
                {
                    for (retrycount = 0; retrycount < 3; retrycount++)
                    {
                        InitializeExecuteCommand();
                        retval.execResult = await SendCommandMsg(loglevel, type, sendData, sendLength);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval.execResult, retrycount), Thread.CurrentThread.ManagedThreadId);
                            continue;
                        }
                        retval = await RecvResponseMsg(loglevel, timeoutsec);
                        if (retval.execResult == 0)
                            break;
                        else if (retval.execResult == (int)COMPORTERROR.ERR_RECV_NAK)
                        {
                            lock (bufferLock)
                            {
                                cb.Clear();
                            }
                        }
                        else
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval.execResult, retrycount), Thread.CurrentThread.ManagedThreadId);
                    }
                    if (retval.execResult != 0)
                    {
                        lock (bufferLock)
                        {
                            cb.Clear();
                        }
                    }
                    doingCommand = false;
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END - {0}", type), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCommand = false;
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync2(int loglevel, int timeoutsec, int timeoutsec2, byte type, byte[] sendData, int sendLength)
        {
            string className = "MarkComm";
            string funcName = "ExecuteCommandMsgAsync2";
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("START - {0}", type), Thread.CurrentThread.ManagedThreadId);

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int retrycount = 0;
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeoutsec + timeoutsec2))
                {
                    if (doingCommand == false)
                        break;

                    await Task.Delay(10);
                }
                sw.Stop();

                if (doingCommand == true)
                {
                    retval.execResult = (int)COMPORTERROR.ERR_COMMAND_BUSY;
                    return retval;
                }

                if (Port.IsOpen == false)
                {
                    retval.execResult = (int)COMPORTERROR.ERR_PORT_NOT_OPENED;
                    doingCommand = false;
                    return retval;
                }

                doingCommand = true;

                //lock (cmdLock)
                {
                    for (retrycount = 0; retrycount < 3; retrycount++)
                    {
                        InitializeExecuteCommand();
                        retval.execResult = await SendCommandMsg(loglevel, type, sendData, sendLength);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval.execResult, retrycount), Thread.CurrentThread.ManagedThreadId);
                            continue;
                        }

                        retval = await RecvResponseMsgAsync2(loglevel, timeoutsec, timeoutsec2);
                        if (retval.execResult == 0)
                            break;
                        else if (retval.execResult == (int)COMPORTERROR.ERR_RECV_NAK)
                        {
                            lock (bufferLock)
                            {
                                cb.Clear();
                            }
                        }
                        else
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval.execResult, retrycount), Thread.CurrentThread.ManagedThreadId);
                    }
                    if (retval.execResult != 0)
                    {
                        lock (bufferLock)
                        {
                            cb.Clear();
                        }
                    }
                    doingCommand = false;
                    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END - {0}", type), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCommand = false;
                return retval;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitializeExecuteCommand()
        {
            //SendFlag = SENDFLAG_IDLE;
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
            RecvCommandData.Initialize();
            RecvFrameLength = 0;
            NAKError = 0;
            isReceiveEndFlag = false;
            //cmdIndex = 0;
        }

        public byte GetBCC(byte[] inputStream)
        {
            byte bcc = 0;

            if (inputStream != null && inputStream.Length > 0)
            {
                for (int i = 0; i < inputStream.Length; i++)
                    bcc ^= inputStream[i];
            }
            return bcc;
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

        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public async Task<ITNTResponseArgs> InitializeController(short maxY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            short initSpeed4NoLoad = 0;
            short targetSpeed4NoLoad = 0;
            short accelSpeed4NoLoad = 0;
            short decelSpeed4NoLoad = 0;

            short initSpeed4Scan = 0;
            short targetSpeed4Scan = 0;
            short accelSpeed4Scan = 0;
            short decelSpeed4Scan = 0;

            short initSpeed4ScanFree = 0;
            short targetSpeed4ScanFree = 0;
            short accelSpeed4ScanFree = 0;
            short decelSpeed4ScanFree = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "InitializeController", "START", Thread.CurrentThread.ManagedThreadId);
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                initSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "INITIALSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
                targetSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "TARGETSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
                accelSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "ACCELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);
                decelSpeed4NoLoad = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "DECELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);

                initSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "INITIALSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
                targetSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "TARGETSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
                accelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "ACCELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
                decelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "DECELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);

                initSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "INITIALSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
                targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "TARGETSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
                accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "ACCELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
                decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "DECELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);

                //sendstring = initSpeed4NoLoad.ToString("X4") + targetSpeed4NoLoad.ToString("X4") + accelSpeed4NoLoad.ToString("X4") + decelSpeed4NoLoad.ToString("X4");
                //sdata = Encoding.UTF8.GetBytes(sendstring);
                retval = await LoadSpeed((byte)'F', initSpeed4NoLoad, targetSpeed4NoLoad, accelSpeed4NoLoad, decelSpeed4NoLoad);    //noload
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "InitializeController", string.Format("LoadSpeed-F ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                retval = await LoadSpeed((byte)'f', initSpeed4ScanFree, targetSpeed4ScanFree, accelSpeed4ScanFree, decelSpeed4ScanFree);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "InitializeController", string.Format("LoadSpeed-f ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
                retval = await LoadSpeed((byte)'l', initSpeed4Scan, targetSpeed4Scan, accelSpeed4Scan, decelSpeed4Scan);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "InitializeController", string.Format("LoadSpeed-l ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                retval = await TestSolFet(12, true);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "InitializeController", string.Format("TestSolFet ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                retval = await GoHome(0, maxY);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "InitializeController", string.Format("GoHome ERROR : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "InitializeController", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        //private void GetMarkStatus()
        //{
        //MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();
        //string value = "";
        //Util.GetPrivateProfileValue("MARKER", "STATUS", "", ref value, "Test.ini");
        //arg.cmd = Convert.ToByte(value.Substring(4, 1));// RecvEventData[idxSOH + 3];
        //byte[] data = Encoding.UTF8.GetBytes(value);
        //Array.Copy(data, arg.receiveBuffer, data.Length);
        //arg.cmd = data[4];// RecvEventData[idxSOH + 3];
        //arg.receiveSize = data.Length;
        //OnMarkControllerStatusDataEventHandler(arg);
        //}

        public async Task<ITNTResponseArgs> GetCurrentSetting()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
            //GetMarkStatus();
#else
            //retval = await ExecuteCommandMsg(0, 5, (byte)'0', sdata, sleng);
#endif
            return retval;
        }


        //        public async void ThreadStatusCheck()
        //        {
        //            int retval = 0;
        //            while(doingThread)
        //            {
        //#if TEST_DEBUG_MARK
        //#else
        //                if ((Port == null) || (!Port.IsOpen))
        //                {
        //                    await Task.Delay(100);
        //                    continue;
        //                }
        //#endif
        //                await GetCurrentSetting();
        //                await Task.Delay(3000);
        //            }
        //        }

        public async void ThreadStatusCheck2()
        {
            int retval = 0;
            ITNTResponseArgs recv = new ITNTResponseArgs();
            MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();

            while (doingThread)
            {
                //if ((Port == null) || (!Port.IsOpen))
                //{
                //    await Task.Delay(100);
                //    continue;
                //}

                recv = await GetStatus();
                if (recv.execResult == 0)
                {
                    arg.receiveSize = recv.recvSize;
                    arg.execmd = recv.exeCmd;
                    arg.stscmd = recv.stsCmd;
                    Array.Copy(recv.recvBuffer, arg.receiveBuffer, recv.recvSize);
                    OnMarkControllerStatusDataEventHandler(arg);
                }

                //await SendVIN();
                await Task.Delay(2000);
            }
        }

        public async Task<ITNTResponseArgs> GetStatus()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //MarkControllerRecievedEvnetArgs arg = new MarkControllerRecievedEvnetArgs();

#if TEST_DEBUG_MARK
            string value = "";
            int use = 0;
            Util.GetPrivateProfileValue("MARKER", "USE", "0", ref value, "TEST.ini");
            int.TryParse(value, out use);
//            int use2 = (int)Util.GetPrivateProfileValueUINT("MARKER", "USE", 0, "TEST.ini");
            if (use == 0)
            {
                Util.GetPrivateProfileValue("MARKER", "USE", "0", ref value, "TEST.ini");
                retval.execResult = -1;
                return retval;
            }

            Util.GetPrivateProfileValue("MARKER", "EXECOMMAND", "R", ref value, "TEST.ini");
            byte.TryParse(value, out retval.exeCmd);

            Util.GetPrivateProfileValue("MARKER", "STSCOMMAND", "9", ref value, "TEST.ini");
            byte.TryParse(value, out retval.stsCmd);

            Util.GetPrivateProfileValue("MARKER", "MARKSTATUS", "012345000000000000", ref value, "TEST.ini");

            retval.recvString = value;
            byte[] test = Encoding.UTF8.GetBytes(value);
            Array.Copy(test, retval.recvBuffer, test.Length);
            retval.recvSize = test.Length;

            //ITNTTraceLog.Instance.TraceHex(2, "MarkComm::ReceiveCommData()  SEND MARK :  ", arg.receiveSize, arg.receiveBuffer);
#endif
            //byte[] sendData = new byte[16];
            //retval = await ExecuteCommandMsg(2, 5, 0x53, sendData, 0);
            return retval;
        }

        private void OnMarkControllerStatusDataEventHandler(MarkControllerRecievedEvnetArgs e)
        {
            string param1 = "";
            string param2 = "";
            //string param3 = "";
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            //int drindex = 0;
            byte[] sensor = new byte[8];
            int retval = 0;
            //DataRowView row = null;
            short Length;
            short steplength;
            //ITNTResponseArgs retArg = new ITNTResponseArgs();
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            byte currCMD = 0;

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                //lock(eventLock)
                {
                    i = 6;
                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    Array.Copy(e.receiveBuffer, i, sensor, 0, 4);
                    retval = CheckMarkControllerSensor(sensor, 4);

                    //if (currentWindow != 0)
                    //    return;

                    switch (e.stscmd)
                    {
                        case 0x30:      //stand by
                            //if (!m_bDoingMarkingFlag)
                            //    m_bDoingMarkingFlag = true;

                            ITNTTraceLog.Instance.TraceHex(1, "MainWindow::OnMarkControllerEventFunc()  RECV MARK :  ", e.receiveSize, e.receiveBuffer);

                            if (param1.Length > 0)
                                chindex = Convert.ToInt32(param1, 16);
                            if (param2.Length > 0)
                                ptindex = Convert.ToInt32(param2, 16);

                            //if (param3.Length > 0)
                            //    drindex = Convert.ToInt32(param3, 16);

                            //if (CheckAccess())
                            //    ShowMarkingOneLine(chindex, ptindex);
                            //else
                            //{
                            //    Dispatcher.Invoke(new Action(delegate
                            //    {
                            //        ShowMarkingOneLine(chindex, ptindex);
                            //    }));
                            //}
                            //Task.Delay(100);
                            break;

                        case 0x31:      //running
                            break;

                        case 0x32:      //run ok
                            break;

                        //case 0x33:      //home ok
                        //    break;
                        //case 0x34:      //jog ok
                        //    break;
                        //case 0x35:      //test ok
                        //    break;
                        //case 0x36:      //go ok
                        //    break;
                        //case 0x37:      //scan ok
                        //    break;

                        case 0x38:
                            //count = 0;
                            currCMD = e.execmd;
                            isReceiveEndFlag = true;

//                            //doingCommand = false;
//                            if ((currCMD == 'R') && (m_currCMD == 'R'))
//                            {
//                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : 각인완료 수신");

//#if MANUAL_MARK
//                                //ShowCurrentStateLabel(5);
//                                ShowCurrentStateLabelManual(4);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
//                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
//#else
//                                ShowCurrentStateLabel(6);
//#endif
//                                ShowLabelData("[4] : 각인완료", lblPLCData);
//                                ShowLabelData("각 인 완 료", lblCheckResult);

//                                //
//                                currMarkInfo.mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
//                                currMarkInfo.mesData.marktime = DateTime.Now.ToString("HH:mm:ss");

//                                SaveMarkResultData(currMarkInfo.mesData, 0, 0);
//                                WriteCompleteData(currMarkInfo.mesData, 0);

//#if AGING_TEST_PLC
//#else
//                                row = GetNextMarkPointData();
//                                UpdatePlanDatabase(row);
//#endif
//                                //DeletePlanData();
//                                recvarg = await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE);
//                                if (recvarg.execResult != 0)
//                                {
//                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "SendMarkingStatus ERROR - " + recvarg.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
//                                    ShowErrorMessage("SEND COMPLETE SIGNAL TO PLC ERROR", false);
//                                }
//                                //markCompleteFlag = true;
//                                Length = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, ITNTCOMMON.Constants.MARKING_INI_FILE);
//                                steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);

//                                Length = (short)(Length * steplength);
//                                recvarg = await MarkControll.GoHome(0, Length);

//                                m_bDoingMarkingFlag = false;
//                                //m_bDoingNextVINFlag = false;

//                                Util.WritePrivateProfileValue("CURRENT", "VIN", currMarkInfo.mesData.vin, ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);
//                                //Util.WritePrivateProfileValue("CURRENT", "INDEX", m_CurrentMarkNum.ToString(), ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);
//                                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", currMarkInfo.mesData.sequence + "|" + currMarkInfo.mesData.vin, ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);

//                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.mesData.sequence + "-" + currMarkInfo.mesData.vin);
//                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.mesData.sequence + "-" + currMarkInfo.mesData.vin);

//                                //currMarkInfo.Initialize();
//                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : 각인완료 종료");
//                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : 각인완료 종료");
//                                markRunTimer.Stop();
//                                ShowCurrentStateLabel(7);
//                                m_currCMD = 0;
                            //}
                            break;

                        case 0x39:      //emergency
                            break;

                        default:
                            break;
                    }
                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //            MarkControllerDataArrivedEventFunc?.Invoke(this, e);
        }

        private int CheckMarkControllerSensor(byte[] sensor, int length)
        {
            int retval = 0;

            return retval;
        }


        /// //////////////////////////////////////////////////////////////////////////////////////////

        /// //////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ITNTResponseArgs> LoadFontData(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(2, 5, (byte)'D', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> Opmode(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'A', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> StrikeNo(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'N', sdata, sleng);
#endif
            return retval;
        }

        //        public async Task<ITNTResponseArgs> RunStart(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'R', sdata, sleng);
        //#endif
        //            return retval;
        //        }


        public async Task<ITNTResponseArgs> SolOnOffTime(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'S', sdata, sleng);
#endif
            return retval;
        }

        //public async Task<ITNTResponseArgs> FreeSpeed(string cmd, string Speed)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandMsg(0, 20, cmd, Speed);
        //    return retval;
        //}

        //        public async Task<ITNTResponseArgs> LoadSpeed(byte[]sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'L', sdata, sleng);
        //#endif
        //            return retval;
        //        }

        //public async Task<ITNTResponseArgs> Resume(string cmd, string R_value)
        //{
        //    ITNTResponseArgs retval = await ExecuteCommandMsg(0, 20, cmd, R_value);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> GoHome(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'H', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> Jog_XY(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'J', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> TestSolFet(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'O', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> TestBox4(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'C', sdata, sleng);
#endif
            return retval;
        }

        //        public async Task<ITNTResponseArgs> GoPoint(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'M', sdata, sleng);
        //#endif
        //            return retval;
        //        }

        //        public async Task<ITNTResponseArgs> GoParking(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'K', sdata, sleng);
        //#endif
        //            return retval;
        //        }

        public async Task<ITNTResponseArgs> GearRatio(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'G', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> SetMaxMinXY(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'X', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> FontFlush()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[16];
            int sleng = 0;
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'B', sdata, sleng);
#endif
            return retval;
        }

        public async Task<ITNTResponseArgs> Inport()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[16];
            int sleng = 0;
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'I', sdata, sleng);
#endif
            return retval;
        }


        public async Task<ITNTResponseArgs> ScanJog(byte direction, short resolution, double scanstpelength)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string value = "";
            //double stepLength = 0.0d;
            short srtValue = 0;
            string sstrring = "";
            try
            {
                if (direction == 0)
                    srtValue = (short)(-(resolution * scanstpelength));
                else
                    srtValue = (short)(resolution * scanstpelength);
                sstrring = srtValue.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 2, (byte)'j', sdata, sdata.Length);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> Profile_Speed(byte flag, short ispeed, short tspeed, short acspeed, short despeed)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            byte cmd = 0;
            try
            {
                if (flag == 0)
                    cmd = (byte)'l';
                else
                    cmd = (byte)'f';

                sstrring = ispeed.ToString("X4") + tspeed.ToString("X4") + acspeed.ToString("X4") + despeed.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 2, cmd, sdata, sdata.Length);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
        }


        public async Task<ITNTResponseArgs> GoHome_Z(short homeposition)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            try
            {
                sstrring = homeposition.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 2, (byte)'h', sdata, sdata.Length);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ScanProfile(short length)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            try
            {
                sstrring = length.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 2, (byte)'U', sdata, sdata.Length);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> MoveScanProfile(short position, short steplength)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            try
            {
                sstrring = ((short)(position * steplength)).ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'k', sdata, sdata.Length);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> MoveScanProfile(short position)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sstrring = "";
            try
            {
                sstrring = position.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sstrring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'k', sdata, sdata.Length);
#endif
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> TestSolFet(short Fet, bool Sol)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            short zero = 0;
            short one = 1;
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "TestSolFet", "START", Thread.CurrentThread.ManagedThreadId);
                if (Sol)
                    sendstring = Fet.ToString("X4") + one.ToString("X4");
                else
                    sendstring = Fet.ToString("X4") + zero.ToString("X4");

                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                //retval = await ExecuteCommandMsg(0, 20, (byte)'O', sdata, sdata.Length);
                retval = await ExecuteCommandMsg(1, 5, (byte)'O', sdata, sdata.Length);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "TestSolFet", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoHome(short posX, short posY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "GoHome", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'H', sdata, sdata.Length);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "GoHome", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParking(short posX, short posY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "GoParking", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'K', sdata, sdata.Length);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "GoParking", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> RunStart(short count)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "RunStart", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = count.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'R', sdata, sdata.Length);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "RunStart", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> Move2LimitXY(short maxX, short maxY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "Move2LimitXY", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = maxX.ToString("X4") + maxY.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'W', sdata, sdata.Length);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "Move2LimitXY", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> Move2LimitU(short maxu)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "Move2LimitU", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = maxu.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'w', sdata, sdata.Length);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "Move2LimitU", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }
        public async Task<ITNTResponseArgs> GoPoint(short posX, short posY, short pos)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[64];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "GoPoint", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = posX.ToString("X4") + posY.ToString("X4") + pos.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'M', sdata, sdata.Length);
#endif
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "GoPoint", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> LoadSpeed(byte cmd, short initSpeed, short targetSpeed, short accel, short decel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "LoadSpeed", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = initSpeed.ToString("X4") + targetSpeed.ToString("X4") + accel.ToString("X4") + decel.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, cmd, sdata, sdata.Length);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "LoadSpeed", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> LoadFontData(short chidx, short ptidx, short posX, short poxY, short flag)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "LoadFontData", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = chidx.ToString("X4") + ptidx.ToString("X4") + posX.ToString("X4") + poxY.ToString("X4") + flag.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'D', sdata, sdata.Length);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "LoadFontData", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> SolOnOffTime(short solontime, short solofftime)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "SolOnOffTime", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = solontime.ToString("X4") + solofftime.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'S', sdata, sdata.Length);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "SolOnOffTime", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<ITNTResponseArgs> StrikeNo(short count)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] sdata = new byte[128];
            string sendstring = "";
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "StrikeNo", "START", Thread.CurrentThread.ManagedThreadId);
                sendstring = count.ToString("X4");
                sdata = Encoding.UTF8.GetBytes(sendstring);
#if TEST_DEBUG_MARK
#else
                retval = await ExecuteCommandMsg(1, 5, (byte)'N', sdata, sdata.Length);
#endif
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkComm", "StrikeNo", "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        ///////////// Profile Scan/////////////////////
        public async Task<ITNTResponseArgs> Scan(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'U', sdata, sleng);
#endif
            return retval;
        }

        //        public async Task<ITNTResponseArgs> Profile_Speed(byte flag, )
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'i', sdata, sleng);
        //#endif
        //            return retval;
        //        }
        //        public async Task<ITNTResponseArgs> Home_U(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'U', sdata, sleng);
        //#endif
        //            return retval;
        //        }
        //        public async Task<ITNTResponseArgs> Jog_U(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'j', sdata, sleng);
        //#endif
        //            return retval;
        //        }
        //        public async Task<ITNTResponseArgs> GoParking_U(byte[] sdata, int sleng)
        //        {
        //            ITNTResponseArgs retval = new ITNTResponseArgs();
        //#if TEST_DEBUG_MARK
        //#else
        //            retval = await ExecuteCommandMsg(0, 5, (byte)'k', sdata, sleng);
        //#endif
        //            return retval;
        //        }



        public async Task<ITNTResponseArgs> GearRatio_U(byte[] sdata, int sleng)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
#else
            retval = await ExecuteCommandMsg(1, 5, (byte)'g', sdata, sleng);
#endif
            return retval;
        }






        private async void OnMarkControllerEventFunc2(MarkControllerRecievedEvnetArgs e)
        {
            string param1 = "";
            string param2 = "";
            //string param3 = "";
            int i = 0;
            int chindex = 0;
            int ptindex = 0;
            //int drindex = 0;
            byte[] sensor = new byte[8];
            int retval = 0;
            short Length;
            short steplength;
            //ITNTResponseArgs retArg = new ITNTResponseArgs();
            ITNTResponseArgs recvarg = new ITNTResponseArgs();
            byte currCMD = 0;

            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                lock (evtLock)
                {
                    i = 6;
                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                    Array.Copy(e.receiveBuffer, i, sensor, 0, 4);
                    //retval = CheckMarkControllerSensor(sensor, 4);

                    switch (e.stscmd)
                    {
                        case 0x30:
                            if (param1.Length > 0)
                                chindex = Convert.ToInt32(param1, 16);
                            if (param2.Length > 0)
                                ptindex = Convert.ToInt32(param2, 16);

                            break;

                        case 0x38:
                            currCMD = e.execmd;
                            break;

                        case 0x39:
                            break;
                    }
                }

                //                //lock(eventLock)
                //                {
                //                    i = 6;
                //                    param1 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                //                    param2 = Encoding.UTF8.GetString(e.receiveBuffer, i, 4); i += 4;
                //                    Array.Copy(e.receiveBuffer, i, sensor, 0, 4);
                //                    retval = CheckMarkControllerSensor(sensor, 4);

                //                    switch (e.stscmd)
                //                    {
                //                        case 0x30:      //stand by
                //                            ITNTTraceLog.Instance.TraceHex(1, "MainWindow::OnMarkControllerEventFunc()  RECV MARK :  ", e.receiveSize, e.receiveBuffer);

                //                            if (param1.Length > 0)
                //                                chindex = Convert.ToInt32(param1, 16);
                //                            if (param2.Length > 0)
                //                                ptindex = Convert.ToInt32(param2, 16);
                //                            //if (param3.Length > 0)
                //                            //    drindex = Convert.ToInt32(param3, 16);

                //                            //if (CheckAccess())
                //                            //    ShowMarkingOneLine(chindex, ptindex);
                //                            //else
                //                            //{
                //                            //    Dispatcher.Invoke(new Action(delegate
                //                            //    {
                //                            //        ShowMarkingOneLine(chindex, ptindex);
                //                            //    }));
                //                            //}
                //                            //Task.Delay(100);
                //                            break;

                //                        case 0x31:      //running
                //                            break;

                //                        case 0x32:      //run ok
                //                            break;

                //                        //case 0x33:      //home ok
                //                        //    break;
                //                        //case 0x34:      //jog ok
                //                        //    break;
                //                        //case 0x35:      //test ok
                //                        //    break;
                //                        //case 0x36:      //go ok
                //                        //    break;
                //                        //case 0x37:      //scan ok
                //                        //    break;

                //                        case 0x38:
                //                            //count = 0;
                //                            currCMD = e.execmd;

                //                            doingCommand = false;
                //                            if ((currCMD == 'R') && (m_currCMD == 'R'))
                //                            {
                //                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : 각인완료 수신");

                //#if MANUAL_MARK
                //                                //ShowCurrentStateLabel(5);
                //                                ShowCurrentStateLabelManual(4);
                //                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                //                                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
                //#else
                //                                ShowCurrentStateLabel(6);
                //#endif
                //                                ShowLabelData("[4] : 각인완료", lblPLCData);
                //                                ShowLabelData("각 인 완 료", lblCheckResult);

                //                                //
                //                                currMarkInfo.mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
                //                                currMarkInfo.mesData.marktime = DateTime.Now.ToString("HH:mm:ss");

                //                                SaveMarkResultData(currMarkInfo.mesData, 0, 0);
                //                                WriteCompleteData(currMarkInfo.mesData, 0);

                //#if AGING_TEST_PLC
                //#else
                //                                row = GetNextMarkPointData();
                //                                UpdatePlanDatabase(row);
                //#endif
                //                                //DeletePlanData();
                //                                recvarg = await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE);
                //                                if (recvarg.execResult != 0)
                //                                {
                //                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "SendMarkingStatus ERROR - " + recvarg.execResult.ToString());
                //                                    ShowErrorMessage("SEND COMPLETE SIGNAL TO PLC ERROR", false);
                //                                }
                //                                //markCompleteFlag = true;
                //                                Length = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, ITNTCOMMON.Constants.MARKING_INI_FILE);
                //                                steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);

                //                                Length = (short)(Length * steplength);
                //                                recvarg = await MarkControll.GoHome(0, Length);

                //                                Util.WritePrivateProfileValue("CURRENT", "VIN", currMarkInfo.mesData.vin, ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);
                //                                //Util.WritePrivateProfileValue("CURRENT", "INDEX", m_CurrentMarkNum.ToString(), ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);
                //                                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", currMarkInfo.mesData.sequence + "|" + currMarkInfo.mesData.vin, ITNTCOMMON.Constants.DATA_CUR_COMPLETE_FILE);

                //                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.mesData.sequence + "-" + currMarkInfo.mesData.vin);
                //                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "Marking Complete  " + currMarkInfo.mesData.sequence + "-" + currMarkInfo.mesData.vin);

                //                                //currMarkInfo.Initialize();
                //                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : 각인완료 종료");
                //                                ITNTJobLog.Instance.Trace(0, "{0}::{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "[4] : 각인완료 종료");
                //                                markRunTimer.Stop();
                //                                ShowCurrentStateLabel(7);
                //                                m_currCMD = 0;
                //                            }
                //                            break;

                //                        case 0x39:      //emergency
                //                            break;

                //                        default:
                //                            break;
                //                    }
                //                }

                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "OnMarkControllerEventFunc", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

    }
}
