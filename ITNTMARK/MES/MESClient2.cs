using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Windows.Threading;
using ITNTUTIL;
using ITNTCOMMON;
using System.Diagnostics;
using System.IO;
using System.Threading;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    enum MESCOMMERROR
    {
        ERR_MESCOMM_NO_ERROR = 0,
        ERR_NOT_CONNECTED = -0x0001,
        ERR_CONNECT_TIMEOUT = -0x0002,
        ERR_RECV_RESPONSE_TIMEOUT = -0x0003,
        ERR_BUFFER_SIZE_ERROR = -0x0004,
        ERR_RECV_BCC_ERROR = -0x0005,
        ERR_CMD_MISSMATCH = -0x0006,
        ERR_PORT_NOT_OPENED = -0x0007,
        ERR_RECV_NAK = -0x0008,
        ERR_COMMAD_BUSY = -0x0009,
        ERR_SOCKET_NULL = -0x000A,
    }

    enum MESRECVFLAG
    {
        RECVFLAG_RECV_IDLE = 0,     // 대기 상태
        //RECVFLAG_REQ_START = 1,  
        //RECVFLAG_REQ_END = 2,
        RECVFLAG_INFO_START = 1,    //REQUST 요청 후 Data Info 수신 대기
        RECVFLAG_INFO_END = 2,      //REQUST 요청 후 Data Info 수신 왼료
        RECVFLAG_BLOCK_START = 3,   //REQUST 요청 후 nth Block Data 수신 대기
        RECVFLAG_BLOCK_END = 4,     //REQUST 요청 후 nth Block Data 수신 완료
        RECVFLAG_RECV_END = 5,      //모든 데이터 수신 완료
        RECVFLAG_RECV_ERR = 0xff,
    }

    class MESClient2
    {
        public event MESClientReceivedEventHandler receivedEvent = null;
        public event MESClientStatusChangedEventHandler statusChangedEvent = null;

        const int MES_MSG_DATA = 100;
        public string serverIP = "";
        public int serverPort = 0;
        RingBuffer rb = null;
        //StateObject asyncstate = null;
        //bool isConnected = false;
        bool doingCmdFlag = false;
        //byte sendFlag = (byte)MESSENDFLAG.SENDFLAG_SEND_IDLE;
        byte recvFlag = (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE;
        DispatcherTimer mesRunningTimer = new DispatcherTimer();
        //byte curState = (byte)PROCESSSTATE.STATE_IDLE;

        private readonly object lockbuff = new object();

        Socket workSocket = null;
        byte[] ReceivedFrame;
        //byte[] socketBuffer = new byte[512 * 1024];
        byte[] socketBuffer = new byte[32 * 1024];

        int iBlockSize = 0;
        bool bRequestFlag = false;
        csConnStatus connstatus = csConnStatus.Closed;
        csConnStatus connbefore = csConnStatus.Closed;

        int ErrorCount = 0;

        public MESClient2()
        {
            //ReceivedFrame = new byte[512 * 2014];
            ReceivedFrame = new byte[32 * 2014];
            //rb = new RingBuffer(512 * 1024);
            rb = new RingBuffer(32 * 1024);
            recvFlag = (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE;

            mesRunningTimer.Tick += mesTimerHandler;
            //mesRunningTimer.Interval = TimeSpan.FromSeconds(10);
            mesRunningTimer.IsEnabled = false;
            mesRunningTimer.Stop();
            //DataArrivalCallback = DataCallback;
        }

        public async Task<int> StartMESClient()
        {
            int retval = 0;

            mesRunningTimer.Interval = TimeSpan.FromSeconds(1);
            mesRunningTimer.Start();
            return retval;
        }

        public async Task<int> OpenMESAsync(TimeSpan connectTimeout)
        {
            int retval = 0;
            string className = "MESClient";
            string funcName = "OpenMESAsync";
            string value = "";
            Socket wsocket = null;
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();

            // Connect to a remote device.  
            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //if (isConnected)
                //if (Volatile.Read(ref isConnected) == true)
                //{
                //    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                //    return 0;
                //}

                if ((GetWorkSocket() != null) && (GetWorkSocket().Connected == true))
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALEADY CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return 0;
                }

                Util.GetPrivateProfileValue("MES", "SERVERIP", "192.168.0.50", ref value, Constants.PARAMS_INI_FILE);
                serverIP = value;
                Util.GetPrivateProfileValue("MES", "SERVERPORT", "64000", ref value, Constants.PARAMS_INI_FILE);
                int.TryParse(value, out serverPort);

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + serverIP + ", PORT = " + serverPort.ToString(), Thread.CurrentThread.ManagedThreadId);

                IPAddress ipAddress = IPAddress.Parse(serverIP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NEW", Thread.CurrentThread.ManagedThreadId);
                // Create a TCP/IP socket.  
                wsocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if(wsocket == null)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "wsocket is null", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }
                e.RemoteEndPoint = remoteEP;
                e.UserToken = wsocket;
                e.Completed += connectComplete;
                wsocket.ConnectAsync(e);
                connbefore = connstatus;
                connstatus = csConnStatus.Connecting;

                //arg.oldstatus = connbefore;
                //arg.newstatus = connstatus;
                //statusChangedEvent?.Invoke(this, arg);

                //workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ////workSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                //e.RemoteEndPoint = remoteEP;
                //e.UserToken = workSocket;
                //e.Completed += connectComplete;
                //workSocket.ConnectAsync(e);

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(ex.ToString());
            }

            return retval;
        }

        //public int OpenMES()
        //{
        //    int retval = 0;

        //    return retval;
        //}
        public async Task<int> RequestData()
        {
            int retval = 0;
            if(mesRunningTimer.IsEnabled == true)
            {
                mesRunningTimer.Stop();
                bRequestFlag = true;
                mesRunningTimer.Interval = TimeSpan.FromMilliseconds(500);
                mesRunningTimer.Start();
            }
            else
                bRequestFlag = true;
            return retval;
        }

        public async Task<int> CloseMES(byte timerflag)
        {
            string className = "MESClient";
            string funcName = "CloseMES";
            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            if(timerflag != 0)
                mesRunningTimer.Stop();

            //if (workSocket == null)
            //if(GetWorkSocket() == null)
            //{
            //    //workSocket.Disconnect(false);
            //    //workSocket.Close();
            //    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "workSocket = null", Thread.CurrentThread.ManagedThreadId);

            //    ////isConnected = false;
            //    ////Volatile.Write(ref isConnected, false);

            //    connbefore = connstatus;
            //    connstatus = csConnStatus.Closed;

            //    if (connstatus != connbefore)
            //    {
            //        ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
            //        arg.oldstatus = connbefore;
            //        arg.newstatus = connstatus;
            //        statusChangedEvent?.Invoke(this, arg);
            //    }
            //    return 0;
            //}

            //if (GetWorkSocket().Connected == false)
            //{
            //    //workSocket.Disconnect(false);
            //    //workSocket.Close();
            //    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "workSocket.Connected = false", Thread.CurrentThread.ManagedThreadId);

            //    //isConnected = false;
            //    //Volatile.Write(ref isConnected, false);

            //    connbefore = connstatus;
            //    connstatus = csConnStatus.Closed;

            //    if (connstatus != connbefore)
            //    {
            //        ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
            //        arg.oldstatus = connbefore;
            //        arg.newstatus = connstatus;
            //        statusChangedEvent?.Invoke(this, arg);
            //    }
            //    //workSocket = null;
            //    SetWorkSocket(null);
            //    return 0;
            //}

            ////if (workSocket != null)
            ////{
            ////    workSocket.Disconnect(false);
            ////    workSocket.Close();
            ////    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CLOSE CONNECTION", Thread.CurrentThread.ManagedThreadId);

            ////    //isConnected = false;
            ////    //Volatile.Write(ref isConnected, false);

            ////    connbefore = connstatus;
            ////    connstatus = csConnStatus.Closed;

            ////    if(connstatus != connbefore)
            ////    {
            ////        ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
            ////        arg.oldstatus = connbefore;
            ////        arg.newstatus = connstatus;
            ////        statusChangedEvent?.Invoke(this, arg);
            ////    }
            ////    return 0;
            ////}



            ////if (Volatile.Read(ref isConnected) == false)
            ////if (workSocket.Connected == false)
            ////{
            ////    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
            ////    //isConnected = false;
            ////    //Volatile.Write(ref isConnected, false);

            ////    connbefore = connstatus;
            ////    connstatus = csConnStatus.Closed;

            ////    if (connstatus != connbefore)
            ////    {
            ////        ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
            ////        arg.oldstatus = connbefore;
            ////        arg.newstatus = connstatus;
            ////        statusChangedEvent?.Invoke(this, arg);
            ////    }
            ////    return 0;
            ////}

            ////isConnected = false;
            ////Volatile.Write(ref isConnected, false);

            //workSocket.Disconnect(true);
            //workSocket.Close();

            //connbefore = connstatus;
            //connstatus = csConnStatus.Closed;

            //if (connstatus != connbefore)
            //{
            //    ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
            //    arg.oldstatus = connbefore;
            //    arg.newstatus = connstatus;
            //    statusChangedEvent?.Invoke(this, arg);
            //}
            await DisconnectServer();
            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return 0;
        }


        private static void DisconnectCallback(IAsyncResult ar)
        {
            string className = "MESClient";
            string funcName = "DisconnectCallback";
            // Complete the disconnect request.
            Socket client = (Socket)ar.AsyncState;
            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                client.EndDisconnect(ar);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private static void DisconnectCallback2(object sender, SocketAsyncEventArgs e)
        {
            string className = "MESClient";
            string funcName = "DisconnectCallback";
            // Complete the disconnect request.
            //Socket client = null;// (Socket)e..AsyncState;
            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                e.Dispose();
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        public async Task<int> ConnectServer(int timeoutsec)
        {
            int retval = 0;
            string className = "MESClient";
            string funcName = "ConnectServer";
            TimeSpan ts = TimeSpan.FromSeconds(timeoutsec);
            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                retval = await OpenMESAsync(ts);
                if (retval != 0)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "OpenMESAsync ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                    return retval;
                }

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "BEGIN CONNECT", Thread.CurrentThread.ManagedThreadId);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(timeoutsec))
                {
                    //if (!isConnected)
                    if(GetWorkSocket() != null)
                    {
                        if (GetWorkSocket().Connected)
                            break;
                    }
                    //if (Volatile.Read(ref isConnected) == true)
                    //    break;

                    await Task.Delay(50);
                }
                sw.Stop();

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECT FINISH - " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);

                //if (isConnected == false)
                //if (Volatile.Read(ref isConnected) == false)
                //if ((workSocket == null) || (workSocket.Connected == false))
                if ((GetWorkSocket() == null) || (GetWorkSocket().Connected == false))
                {
                    //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "isConnected = false", Thread.CurrentThread.ManagedThreadId);
                    retval = (int)MESCOMMERROR.ERR_NOT_CONNECTED;
                }
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public async Task<int> DisconnectServer()
        {
            string className = "MESClient";
            string funcName = "DisconnectServer";
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();

            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                if (GetWorkSocket() != null)
                {
                    //if ((workSocket.Connected) || (Volatile.Read(ref isConnected) == true))
                    if (GetWorkSocket().Connected)
                    {
                        if (GetWorkSocket() == null)
                        {
                            //connbefore = connstatus;
                            //connstatus = csConnStatus.Closed;

                            //if (connstatus != connbefore)
                            //{
                            //    ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
                            //    arg.oldstatus = connbefore;
                            //    arg.newstatus = connstatus;
                            //    statusChangedEvent?.Invoke(this, arg);
                            //}
                            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END1", Thread.CurrentThread.ManagedThreadId);
                            return 0;
                        }
                        GetWorkSocket().Shutdown(SocketShutdown.Both);

                        //if (GetWorkSocket() == null)
                        //{
                        //    connbefore = connstatus;
                        //    connstatus = csConnStatus.Closed;

                        //    if (connstatus != connbefore)
                        //    {
                        //        ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
                        //        arg.oldstatus = connbefore;
                        //        arg.newstatus = connstatus;
                        //        statusChangedEvent?.Invoke(this, arg);
                        //    }
                        //    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END2", Thread.CurrentThread.ManagedThreadId);
                        //    return 0;
                        //}

                        //GetWorkSocket().BeginDisconnect(true, new AsyncCallback(DisconnectCallback), GetWorkSocket());

                        ////e.Completed += new EventHandler<SocketAsyncEventArgs>(DisconnectCallback2);
                        ////e.DisconnectReuseSocket = true;
                        ////GetWorkSocket().DisconnectAsync(e);
                        //Stopwatch sw = new Stopwatch();
                        //sw.Start();
                        //while (sw.Elapsed < TimeSpan.FromSeconds(6))
                        //{
                        //    if (GetWorkSocket() != null)
                        //    {
                        //        if (GetWorkSocket().Connected == false)
                        //            break;
                        //    }
                        //    else
                        //        break;

                        //    //if (Volatile.Read(ref isConnected) == true)
                        //    //    break;

                        //    await Task.Delay(50);
                        //}
                        //sw.Stop();

                        if (GetWorkSocket() == null)
                        {
                            //connbefore = connstatus;
                            //connstatus = csConnStatus.Closed;

                            //if (connstatus != connbefore)
                            //{
                            //    ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
                            //    arg.oldstatus = connbefore;
                            //    arg.newstatus = connstatus;
                            //    statusChangedEvent?.Invoke(this, arg);
                            //}
                            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END1", Thread.CurrentThread.ManagedThreadId);
                            return 0;
                        }
                        GetWorkSocket().Disconnect(false);

                        if (GetWorkSocket() == null)
                        {
                            //connbefore = connstatus;
                            //connstatus = csConnStatus.Closed;

                            //if (connstatus != connbefore)
                            //{
                            //    ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
                            //    arg.oldstatus = connbefore;
                            //    arg.newstatus = connstatus;
                            //    statusChangedEvent?.Invoke(this, arg);
                            //}
                            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END1", Thread.CurrentThread.ManagedThreadId);
                            return 0;
                        }
                        GetWorkSocket().Close();

                        //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                        //isConnected = false;
                    }
                    //workSocket.Disconnect(false);
                    //workSocket.Close();
                    //workSocket = null;
                    if (GetWorkSocket() != null)
                        SetWorkSocket(null);
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CLOSE CONNECTION", Thread.CurrentThread.ManagedThreadId);
                }

                //if (GetWorkSocket() != null)
                //{
                //    GetWorkSocket().Shutdown(SocketShutdown.Both);
                //    GetWorkSocket().Disconnect(true);
                //    GetWorkSocket().Close();
                //}

                //if (Volatile.Read(ref isConnected) == false)
                //{
                //    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                //    //isConnected = false;
                //}

                //Volatile.Write(ref isConnected, false);
                //connbefore = connstatus;
                //connstatus = csConnStatus.Closed;

                //if (connstatus != connbefore)
                //{
                //    ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();
                //    arg.oldstatus = connbefore;
                //    arg.newstatus = connstatus;
                //    statusChangedEvent?.Invoke(this, arg);
                //}
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch(Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
            //int retval = 0;
        }

        //public int RequestData()
        //{
        //    int retval = 0;
        //    bRequestFlag = true;
        //    return retval;
        //}

        public void ReceiveCommData2(byte state, int size)
        {
            string className = "MESClient";
            string funcName = "ReceiveCommData2";

            int leng = 0;
            //byte[] look = new byte[512 * 1024];
            byte[] look = new byte[32 * 1024];
            MESDATAINFO mesDataInfo = new MESDATAINFO();
            MESDATABLOCKHEADER mesDataHeader = new MESDATABLOCKHEADER();

            try
            {
                lock (lockbuff)
                {
                    leng = rb.GetSize();
                    if (leng <= 0)
                        return;

                    rb.Look(ref look, leng);
                    switch (state)
                    {
                        case (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE:
                            break;

                        case (byte)MESRECVFLAG.RECVFLAG_INFO_START:
                            if (leng >= size)
                            {
                                rb.Get(ref ReceivedFrame, size);
                                SetRecvFlag((byte)MESRECVFLAG.RECVFLAG_INFO_END);
                                //Volatile.Write(ref recvFlag, (byte)MESRECVFLAG.RECVFLAG_INFO_END);
                                //recvFlag = (byte)MESRECVFLAG.RECVFLAG_INFO_END;
                            }
                            break;

                        case (byte)MESRECVFLAG.RECVFLAG_INFO_END:
                            break;

                        case (byte)MESRECVFLAG.RECVFLAG_BLOCK_START:
                            if ((size >= MES_MSG_DATA) && (leng >= size))
                            {
                                rb.Get(ref ReceivedFrame, size);
                                SetRecvFlag((byte)MESRECVFLAG.RECVFLAG_BLOCK_END);
                                //Volatile.Write(ref recvFlag, (byte)MESRECVFLAG.RECVFLAG_BLOCK_END);
                                //recvFlag = (byte)MESRECVFLAG.RECVFLAG_BLOCK_END;
                            }
                            break;

                        case (byte)MESRECVFLAG.RECVFLAG_BLOCK_END:
                            break;

                        case (byte)MESRECVFLAG.RECVFLAG_RECV_END:
                            break;

                        default:
                            break;
                    }
                    //while(leng >= MES_MSG_DATA)
                    //{
                    //leng = rb.GetSize();
                    //}
                }
                //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END-5", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private async void connectComplete(object sender, SocketAsyncEventArgs e)
        {
            bool bret = false;
            Stopwatch sw = new Stopwatch();
            string value = "";
            string className = "MESClient";
            string funcName = "connectComplete";
            SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
            //Socket wsocket = null;

            try
            {
                //workSocket = (Socket)e.UserToken;
                //wsocket = (Socket)e.UserToken;
                //if (e.SocketError == SocketError.Success)
                //    Volatile.Write(ref isConnected, true);
                //else
                //    Volatile.Write(ref isConnected, false);
                //isConnected = e.SocketError == SocketError.Success;
                //if (isConnected)
                //if (Volatile.Read(ref isConnected) == true)
                //if((workSocket != null) && (workSocket.Connected))
                if (e.SocketError == SocketError.Success)
                {
                    //workSocket = new Socket();
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MESClient", "connectComplete", "isConnected = true", Thread.CurrentThread.ManagedThreadId);
                    //connbefore = connstatus;
                    //connstatus = csConnStatus.Connected;
                    //if (connstatus != connbefore)
                    //{
                    //    ServerStatusChangedEventArgs earg = new ServerStatusChangedEventArgs();
                    //    earg.oldstatus = connbefore;
                    //    earg.newstatus = connstatus;
                    //    statusChangedEvent?.Invoke(this, earg);
                    //}

                    //workSocket = (Socket)e.UserToken;
                    SetWorkSocket((Socket)e.UserToken);
                    arg.UserToken = GetWorkSocket();
                    //GetWorkSocket().NoDelay = true;
                    //arg.UserToken = workSocket;
                    //arg.DisconnectReuseSocket = true;
                    arg.Completed += receiveComplete;
                    arg.SetBuffer(socketBuffer, 0, socketBuffer.Length);
                    bret = GetWorkSocket().ReceiveAsync(arg);
                    //bret = workSocket.ReceiveAsync(arg);
                    if (bret == false)
                    {

                    }
                    //mesRunningTimer.Start();
                }
                else
                {
                    //sw.Start();
                    //while(sw.Elapsed < TimeSpan.FromSeconds(5))
                    //{
                    //    await Task.Delay(50);
                    //}
                    //sw.Stop();

                    //wsocket = (Socket)e.UserToken;
                    //wsocket.Close();

                    //Util.GetPrivateProfileValue("MES", "SERVERIP", "192.168.0.50", ref value, Constants.PARAMS_INI_FILE);
                    //serverIP = value;
                    //Util.GetPrivateProfileValue("MES", "SERVERPORT", "64000", ref value, Constants.PARAMS_INI_FILE);
                    //int.TryParse(value, out serverPort);
                    ////ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "IP = " + serverIP + ", PORT = " + serverPort.ToString(), Thread.CurrentThread.ManagedThreadId);

                    //IPAddress ipAddress = IPAddress.Parse(serverIP);
                    //IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                    //// Create a TCP/IP socket.  
                    //wsocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    //arg.RemoteEndPoint = remoteEP;
                    //arg.UserToken = wsocket;
                    //arg.Completed += connectComplete;
                    //wsocket.ConnectAsync(arg);
                    //connbefore = connstatus;
                    //connstatus = csConnStatus.Connecting;
                    //if (connstatus != connbefore)
                    //{
                    //    ServerStatusChangedEventArgs earg = new ServerStatusChangedEventArgs();
                    //    earg.oldstatus = connbefore;
                    //    earg.newstatus = connstatus;
                    //    statusChangedEvent?.Invoke(this, earg);
                    //}

                    //workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    //arg.RemoteEndPoint = remoteEP;
                    //arg.UserToken = workSocket;
                    //arg.Completed += connectComplete;
                    //workSocket.ConnectAsync(arg);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void receiveComplete(object sender, SocketAsyncEventArgs e)
        {
            string className = "MESClient";
            string funcName = "receiveComplete";
            SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
            string value = "";
            bool bret = false;
            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV SIZE : " + e.BytesTransferred.ToString(), Thread.CurrentThread.ManagedThreadId);
            Socket wsocket = null;

            try
            {
                //ReceiveCommData();
                wsocket = (Socket)e.UserToken;
                if(wsocket == null)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "WSOCKET IS NULL", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (e.BytesTransferred <= 0)
                {
                    //if ((wsocket != null) && wsocket.Connected)
                    //{
                    //    //await DisconnectServer ();
                    //    //wsocket.Disconnect(false);
                    //    //wsocket.Close();
                    //    //workSocket = null;

                    //    //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSE", Thread.CurrentThread.ManagedThreadId);
                    //    //workSocket.Shutdown(SocketShutdown.Both);
                    //    //workSocket.Close();
                    //    //mesRunningTimer.Stop();
                    //    //asyncstate.workSocket.Close();
                    //}
                    //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSED", Thread.CurrentThread.ManagedThreadId);
                    ////connstatus = csConnStatus.Closed;
                    //connbefore = connstatus;
                    //connstatus = csConnStatus.Closed;
                    //if (connstatus != connbefore)
                    //{
                    //    ServerStatusChangedEventArgs earg = new ServerStatusChangedEventArgs();
                    //    earg.oldstatus = connbefore;
                    //    earg.newstatus = connstatus;
                    //    statusChangedEvent?.Invoke(this, earg);
                    //}
                    ////Volatile.Write(ref isConnected, false);
                    ////isConnected = false;
                    return;
                }

                value = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                ITNTMESLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV : " + value, Thread.CurrentThread.ManagedThreadId);

                lock (lockbuff)
                {
                    rb.Put(e.Buffer, e.BytesTransferred);
                }
                //ReceiveCommData();

                arg.UserToken = wsocket;
                arg.Completed += receiveComplete;
                arg.SetBuffer(socketBuffer, 0, socketBuffer.Length);
                bret = wsocket.ReceiveAsync(arg);
                if (!bret)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV ASYNC ERROR", Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        //private async void receiveComplete(object sender, SocketAsyncEventArgs e)
        //{
        //    string className = "MESClient";
        //    string funcName = "receiveComplete";
        //    SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
        //    string value = "";
        //    bool bret = false;
        //    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV SIZE : " + e.BytesTransferred.ToString(), Thread.CurrentThread.ManagedThreadId);
        //    Socket wsocket = null;

        //    try
        //    {
        //        //ReceiveCommData();
        //        if (e.BytesTransferred <= 0)
        //        {
        //            if ((workSocket != null) && workSocket.Connected)
        //            {
        //                //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSE", Thread.CurrentThread.ManagedThreadId);
        //                //workSocket.Shutdown(SocketShutdown.Both);
        //                //workSocket.Close();
        //                //mesRunningTimer.Stop();
        //                //asyncstate.workSocket.Close();
        //            }
        //            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSED", Thread.CurrentThread.ManagedThreadId);
        //            isConnected = false;
        //            return;
        //        }

        //        value = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
        //        ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV : " + value, Thread.CurrentThread.ManagedThreadId);

        //        lock (lockbuff)
        //        {
        //            rb.Put(e.Buffer, e.BytesTransferred);
        //        }
        //        //ReceiveCommData();

        //        arg.UserToken = workSocket;
        //        arg.Completed += receiveComplete;
        //        arg.SetBuffer(socketBuffer, 0, socketBuffer.Length);
        //        bret = workSocket.ReceiveAsync(arg);
        //        if (!bret)
        //        {
        //            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV ASYNC ERROR", Thread.CurrentThread.ManagedThreadId);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}


        private int Send(String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            // Begin sending the data to the remote device.  
            string className = "MESClient";
            string funcName = "Send";

            try
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                //msg = Encoding.UTF8.GetString(byteData);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND : " + data, Thread.CurrentThread.ManagedThreadId);

                //if (workSocket == null)
                if (GetWorkSocket() == null)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERR_SOCKET_NULL", Thread.CurrentThread.ManagedThreadId);
                    //Volatile.Write(ref isConnected, false);
                    //isConnected = false;
                    return (int)MESCOMMERROR.ERR_SOCKET_NULL;
                }

                //if (isConnected != true)
                //if (Volatile.Read(ref isConnected) == false)
                if(GetWorkSocket().Connected == false)
                {
                    //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSE", Thread.CurrentThread.ManagedThreadId);
                    //workSocket.Shutdown(SocketShutdown.Both);
                    //workSocket.Close();
                    //workSocket = null;
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERR_NOT_CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return (int)MESCOMMERROR.ERR_NOT_CONNECTED;
                }

                GetWorkSocket().Send(byteData, 0, byteData.Length, 0);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
                //asyncstate.workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
            }
            catch (Exception ex)
            {
                //if (workSocket != null)
                //{
                //    workSocket.Disconnect(false);
                //    workSocket.Close();
                //    workSocket = null;
                //}
                //isConnected = false;
                //Volatile.Write(ref isConnected, false);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public int Send(byte[] data, int size)
        {
            // Convert the string data to byte data using ASCII encoding.  
            string className = "MESClient";
            string funcName = "Send2";
            string msg = "";

            // Begin sending the data to the remote device.  
            try
            {
                msg = Encoding.UTF8.GetString(data, 0, size);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND : " + msg, Thread.CurrentThread.ManagedThreadId);
                //if (workSocket == null)
                if (GetWorkSocket() == null)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERR_SOCKET_NULL", Thread.CurrentThread.ManagedThreadId);
                    //isConnected = false;
                    return (int)MESCOMMERROR.ERR_SOCKET_NULL;
                }

                //if (isConnected != true)
                //if (Volatile.Read(ref isConnected) == false)
                if(GetWorkSocket().Connected == false)
                {
                    //ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSE", Thread.CurrentThread.ManagedThreadId);
                    //workSocket.Shutdown(SocketShutdown.Both);
                    //workSocket.Close();
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERR_NOT_CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    //workSocket = null;
                    return (int)MESCOMMERROR.ERR_NOT_CONNECTED;
                }

                GetWorkSocket().Send(data, 0, size, 0);
                //asyncstate.workSocket.BeginSend(data, 0, size, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch (Exception ex)
            {
                //if (workSocket != null)
                //    workSocket.Close();
                //if (workSocket != null)
                //{
                //    workSocket.Disconnect(false);
                //    workSocket.Close();
                //    workSocket = null;
                //}
                //isConnected = false;
                //Volatile.Write(ref isConnected, false);

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        async Task<MESDATAINFO> AnalyzeRequestDataMessage(byte[] buff, int size)
        {
            MESDATAINFO retval = new MESDATAINFO();
            string msg = Encoding.UTF8.GetString(buff, 0, size);
            int idx = 0;
            int leng = 0;
            string value = "";

            try
            {
                leng = 14;
                retval.sendDate = msg.Substring(idx, leng);
                idx += leng;

                leng = 7;
                retval.deviceID = msg.Substring(idx, leng);
                idx += leng;

                leng = 1;
                retval.cmdKind = msg.Substring(idx, leng);
                idx += leng;

                leng = 14;
                retval.orderDate = msg.Substring(idx, leng);
                idx += leng;

                leng = 10;
                retval.totalSize = msg.Substring(idx, leng);
                idx += leng;

                leng = 5;
                retval.totalCount = msg.Substring(idx, leng);
                idx += leng;

                leng = 5;
                retval.recordSize = msg.Substring(idx, leng);
                idx += leng;

                leng = 6;
                retval.blockCount = msg.Substring(idx, leng);
                idx += leng;

                leng = 6;
                retval.blockSize = msg.Substring(idx, leng);
                idx += leng;

                leng = 32;
                retval.reserved = msg.Substring(idx, leng);
                idx += leng;

                long.TryParse(retval.totalSize, out retval.ltotalSize);
                int.TryParse(retval.blockCount, out retval.iblockCount);
                int.TryParse(retval.blockSize, out iBlockSize);
                int.TryParse(retval.totalCount, out retval.itotalCount);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
            return retval;
        }

        async Task<MESDATABLOCKHEADER> SaveReceiveMessage(int curIndex, int blockCount, string orderdate, byte[] buff, int size)
        {
            //StreamWriter sw = new StreamWriter();
            MESDATABLOCKHEADER retval = new MESDATABLOCKHEADER();
            string msg = "";
            int idx = 0;
            int leng = 0;
            string value = "";
            string className = "MESClient";
            string funcName = "SaveReceiveMessage";
            Stopwatch sw = new Stopwatch();
            //MESClientReceivedEventArgs arg = new MESClientReceivedEventArgs();
            string recorddata = "";

            try
            {
                msg = Encoding.UTF8.GetString(buff, 0, size);

                leng = 14;
                retval.sendDate = msg.Substring(idx, leng);
                idx += leng;

                leng = 7;
                retval.deviceID = msg.Substring(idx, leng);
                idx += leng;

                leng = 1;
                retval.cmdKind = msg.Substring(idx, leng);
                idx += leng;

                leng = 6;
                retval.serial = msg.Substring(idx, leng);
                idx += leng;

                leng = 10;
                retval.dataSize = msg.Substring(idx, leng);
                idx += leng;

                leng = 5;
                retval.count = msg.Substring(idx, leng);
                idx += leng;

                leng = 5;
                retval.recordSize = msg.Substring(idx, leng);
                idx += leng;

                leng = 52;
                retval.reserved = msg.Substring(idx, leng);
                idx += leng;

                int.TryParse(retval.recordSize, out retval.irecordSize);
                int.TryParse(retval.count, out retval.icount);

                recorddata = msg.Substring(idx);

                sw.Start();
                if (curIndex == 0)
                    retval.execResult = await WriteCCRFile3(curIndex, orderdate + retval.count + retval.recordSize + recorddata, FileMode.CreateNew);
                else
                    retval.execResult = await WriteCCRFile3(curIndex, orderdate + retval.count + retval.recordSize + recorddata, FileMode.Append);
                //if (curIndex == 0)
                //    await WriteCCRFile3(curIndex, orderdate + retval.count + retval.recordSize + recorddata, FileMode.CreateNew);
                //else
                //    await WriteCCRFile3(curIndex, orderdate + retval.count + retval.recordSize + recorddata, FileMode.Append);
                sw.Stop();

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "TIME SPAN = " + sw.ElapsedMilliseconds, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                return retval;
            }
            return retval;
        }


        public async Task<int> SendRequestData()
        {
            int retval = 0;
            string msg = "";
            string space = "        ";
            string className = "MESClient";
            string funcName = "SendRequestData";
            //string tmp = "";

            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            msg = DateTime.Now.ToString("yyyyMMddHHmmss");
            string value = "";
            Util.GetPrivateProfileValue("MES", "DEVICEID", "DOWP001", ref value, Constants.PARAMS_INI_FILE);
            msg += value;
            msg += "R";
            if (bRequestFlag)
            {
                //bRequestFlag = false;
                value = "              ";
                //value = "00000000000000";
            }
            else
                Util.GetPrivateProfileValue("MES", "ORDERDATETIME", "              ", ref value, Constants.PARAMS_INI_FILE);

            msg += value;
            for (int i = 0; i < 8; i++)
                msg += space;
            //msg += "      ";

            retval = Send(msg);
            if (retval != 0)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Send ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            SetRecvFlag((byte)MESRECVFLAG.RECVFLAG_INFO_START);
            //Volatile.Write(ref recvFlag, (byte)MESRECVFLAG.RECVFLAG_INFO_START);
            //recvFlag = (byte)MESRECVFLAG.RECVFLAG_INFO_START;
            bRequestFlag = false;
            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<int> SendAnswerData(string serial, int idx)
        {
            int retval = 0;
            string msg = "";
            string space32 = "                                ";
            string className = "MESClient";
            string funcName = "SendAnswerData";

            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            msg = DateTime.Now.ToString("yyyyMMddHHmmss");
            string value = "";
            Util.GetPrivateProfileValue("MES", "DEVICEID", "DOWP001", ref value, Constants.PARAMS_INI_FILE);
            msg += value;
            msg += "B";
            if (serial.Length > 0)
                msg += serial;
            else
                msg += idx.ToString("D6");
            msg += "OK";

            //msg += value;
            for (int i = 0; i < 5; i++)
                msg += space32;

            msg += "          ";

            retval = Send(msg);
            if (retval != 0)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Send ERROR : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }


        private void InitializeParam()
        {
            rb.Clear();
            ReceivedFrame.Initialize();
            SetRecvFlag((byte)MESRECVFLAG.RECVFLAG_RECV_IDLE);
            //Volatile.Write(ref recvFlag, (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE);
            //recvFlag = (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE;
        }

        private async void mesTimerHandler(object sender, EventArgs e)
        {
            string className = "MESClient";
            string funcName = "mesTimerHandler";
            int retval = 0;
            //TimeSpan ts = TimeSpan.FromSeconds(10);
            Stopwatch sw = new Stopwatch();
            MESDATAINFO mesDataInfo = new MESDATAINFO();
            MESDATABLOCKHEADER mesHeader = new MESDATABLOCKHEADER();
            MESClientReceivedEventArgs arg = new MESClientReceivedEventArgs();
            int receiveSize = 0;

            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                mesRunningTimer.Interval = TimeSpan.FromSeconds(300);
                doingCmdFlag = true;
                mesRunningTimer.Stop();
                //((DispatcherTimer)sender).IsEnabled = false;
                //((DispatcherTimer)sender).Stop();
                //if(ErrorCount > 20)
                //{

                //}

                InitializeParam();

                if((GetWorkSocket() == null) || (GetWorkSocket().Connected == false))
                {
                    retval = await ConnectServer(6);
                    if (retval != 0)
                    {
                        ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CONNECTD FAIL", Thread.CurrentThread.ManagedThreadId);
                        doingCmdFlag = false;
                        mesRunningTimer.Start();
                        //((DispatcherTimer)sender).Start();
                        ErrorCount++;

                        connbefore = connstatus;
                        connstatus = csConnStatus.Closed;

                        if (connstatus != connbefore)
                        {
                            ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                            stsarg.oldstatus = connbefore;
                            stsarg.newstatus = connstatus;
                            statusChangedEvent?.Invoke(this, stsarg);
                        }
                        return;
                    }

                    connbefore = connstatus;
                    connstatus = csConnStatus.Connected;

                    if (connstatus != connbefore)
                    {
                        ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                        stsarg.oldstatus = connbefore;
                        stsarg.newstatus = connstatus;
                        statusChangedEvent?.Invoke(this, stsarg);
                    }
                }

                //sendFlag = (byte)MESSENDFLAG.SENDFLAG_SEND_REQ;
                SetRecvFlag((byte)MESRECVFLAG.RECVFLAG_RECV_IDLE);
                //Volatile.Write(ref recvFlag, (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE);
                //recvFlag = (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE;
                retval = await SendRequestData();
                if (retval != 0)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SENDREQUEST FAIL", Thread.CurrentThread.ManagedThreadId);
                    doingCmdFlag = false;
                    await DisconnectServer();
                    mesRunningTimer.Start();
                    ErrorCount++;
                    //((DispatcherTimer)sender).Start();
                    connbefore = connstatus;
                    connstatus = csConnStatus.Closed;

                    if (connstatus != connbefore)
                    {
                        ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                        stsarg.oldstatus = connbefore;
                        stsarg.newstatus = connstatus;
                        statusChangedEvent?.Invoke(this, stsarg);
                    }

                    return;
                }
                //recvFlag = (byte)MESRECVFLAG.RECVFLAG_RECV_REQ;

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INFO START", Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromSeconds(60))
                {
                    ReceiveCommData2((byte)MESRECVFLAG.RECVFLAG_INFO_START, MES_MSG_DATA);
                    //if (Volatile.Read(ref recvFlag) == (byte)MESRECVFLAG.RECVFLAG_INFO_END)
                    if (GetRecvFlag() == (byte)MESRECVFLAG.RECVFLAG_INFO_END)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "TimeSpan = " + sw.ElapsedMilliseconds.ToString(), Thread.CurrentThread.ManagedThreadId);
                sw.Reset();

                //if ((Volatile.Read(ref recvFlag) == (byte)MESRECVFLAG.RECVFLAG_INFO_START) || (Volatile.Read(ref recvFlag) == (byte)MESRECVFLAG.RECVFLAG_RECV_ERR))
                if ((GetRecvFlag() == (byte)MESRECVFLAG.RECVFLAG_INFO_START) || (GetRecvFlag() == (byte)MESRECVFLAG.RECVFLAG_RECV_ERR))
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "INFO TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    doingCmdFlag = false;
                    await DisconnectServer();
                    mesRunningTimer.Start();

                    connbefore = connstatus;
                    connstatus = csConnStatus.Disconnected;

                    if (connstatus != connbefore)
                    {
                        ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                        stsarg.oldstatus = connbefore;
                        stsarg.newstatus = connstatus;
                        statusChangedEvent?.Invoke(this, stsarg);
                    }

                    //((DispatcherTimer)sender).Start();
                    return;
                }

                //if (Volatile.Read(ref recvFlag) == (byte)MESRECVFLAG.RECVFLAG_INFO_END)
                if (GetRecvFlag() == (byte)MESRECVFLAG.RECVFLAG_INFO_END)
                {
                    ErrorCount = 0;
                    mesDataInfo = await AnalyzeRequestDataMessage(ReceivedFrame, MES_MSG_DATA);
                    if (mesDataInfo.ltotalSize <= 0)
                    {
                        ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "total size <= 0", Thread.CurrentThread.ManagedThreadId);
                        doingCmdFlag = false;

                        mesRunningTimer.Start();

                        //접속 유지 현대 울산 51라인
                        await DisconnectServer();
                        connbefore = connstatus;
                        connstatus = csConnStatus.Disconnected;

                        if (connstatus != connbefore)
                        {
                            ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                            stsarg.oldstatus = connbefore;
                            stsarg.newstatus = connstatus;
                            statusChangedEvent?.Invoke(this, stsarg);
                        }

                        //((DispatcherTimer)sender).Start();
                        return;
                    }
                }

                for (int i = 0; i < mesDataInfo.iblockCount; i++)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "BLOCK START", Thread.CurrentThread.ManagedThreadId);
                    SetRecvFlag((byte)MESRECVFLAG.RECVFLAG_BLOCK_START);
                    //Volatile.Write(ref recvFlag, (byte)MESRECVFLAG.RECVFLAG_BLOCK_START);
                    //recvFlag = (byte)MESRECVFLAG.RECVFLAG_BLOCK_START;
                    if (i == mesDataInfo.iblockCount - 1)
                    {
                        if(mesDataInfo.ltotalSize > (long)(i * iBlockSize))
                            receiveSize = (int)(mesDataInfo.ltotalSize - (long)(i*iBlockSize)) + MES_MSG_DATA;
                        else
                            receiveSize = iBlockSize + MES_MSG_DATA;
                    }
                    else
                        receiveSize = iBlockSize + MES_MSG_DATA;
                    sw.Restart();
                    while (sw.Elapsed < TimeSpan.FromSeconds(30))
                    {
                        ReceiveCommData2((byte)MESRECVFLAG.RECVFLAG_BLOCK_START, receiveSize);
                        //if (Volatile.Read(ref recvFlag) == (byte)MESRECVFLAG.RECVFLAG_BLOCK_END)
                        if (GetRecvFlag() == (byte)MESRECVFLAG.RECVFLAG_BLOCK_END)
                            break;

                        await Task.Delay(50);
                    }
                    sw.Stop();
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "TimeSpan2 = " + sw.ElapsedMilliseconds.ToString(), Thread.CurrentThread.ManagedThreadId);
                    sw.Reset();

                    //if (Volatile.Read(ref recvFlag) == (byte)MESRECVFLAG.RECVFLAG_BLOCK_START)
                    if (GetRecvFlag() == (byte)MESRECVFLAG.RECVFLAG_BLOCK_START)
                    {
                        doingCmdFlag = false;
                        mesRunningTimer.Start();
                        //((DispatcherTimer)sender).Start();
                        await DisconnectServer();

                        connbefore = connstatus;
                        connstatus = csConnStatus.Disconnected;

                        if (connstatus != connbefore)
                        {
                            ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                            stsarg.oldstatus = connbefore;
                            stsarg.newstatus = connstatus;
                            statusChangedEvent?.Invoke(this, stsarg);
                        }

                        ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "BLOCK TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    mesHeader = await SaveReceiveMessage(i, mesDataInfo.iblockCount, mesDataInfo.orderDate, ReceivedFrame, iBlockSize + MES_MSG_DATA);
                    if(mesHeader.execResult != 0)
                    {
                        doingCmdFlag = false;
                        mesRunningTimer.Start();
                        //((DispatcherTimer)sender).Start();
                        await DisconnectServer();
                        ErrorCount++;

                        connbefore = connstatus;
                        connstatus = csConnStatus.Closed;

                        if (connstatus != connbefore)
                        {
                            ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                            stsarg.oldstatus = connbefore;
                            stsarg.newstatus = connstatus;
                            statusChangedEvent?.Invoke(this, stsarg);
                        }

                        ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SaveReceiveMessage  ERROR = " + mesHeader.execResult.ToString()), Thread.CurrentThread.ManagedThreadId);
                        return;
                    }
                    //mesHeader = await AnalyzeBlockDataHeaderMessage(i, mesDataInfo.iblockCount, mesDataInfo.orderDate, ReceivedFrame, iBlockSize + MES_MSG_DATA);
                    retval = await SendAnswerData(mesHeader.serial, i);
                    if (retval != 0)
                    {
                        ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SENDANSWER FAIL", Thread.CurrentThread.ManagedThreadId);
                        doingCmdFlag = false;
                        await DisconnectServer();
                        mesRunningTimer.Start();
                        ErrorCount++;

                        connbefore = connstatus;
                        connstatus = csConnStatus.Closed;

                        if (connstatus != connbefore)
                        {
                            ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                            stsarg.oldstatus = connbefore;
                            stsarg.newstatus = connstatus;
                            statusChangedEvent?.Invoke(this, stsarg);
                        }

                        //((DispatcherTimer)sender).Start();
                        return;
                    }
                }

                ErrorCount = 0;
                string value = mesDataInfo.orderDate.Trim();
                Util.WritePrivateProfileValue("MES", "ORDERDATETIME", value, Constants.PARAMS_INI_FILE);

                ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);

                arg.blockCount = mesDataInfo.iblockCount;
                arg.itotalCount = mesDataInfo.itotalCount;
                arg.recordSize = mesHeader.irecordSize;
                //arg.recvMsg = recorddata;
                receivedEvent?.Invoke(this, arg);


                //접속 유지 현대 울산 51라인
                if((GetWorkSocket() != null) && GetWorkSocket().Connected)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SOCKET CLOSE END MESSAGE", Thread.CurrentThread.ManagedThreadId);
                    await DisconnectServer();
                    //CloseMES(0);
                    //workSocket.Shutdown(SocketShutdown.Both);
                    //workSocket.Close();
                    //isConnected = false;
                    //workSocket = null;

                    connbefore = connstatus;
                    connstatus = csConnStatus.Disconnected;

                    if (connstatus != connbefore)
                    {
                        ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                        stsarg.oldstatus = connbefore;
                        stsarg.newstatus = connstatus;
                        statusChangedEvent?.Invoke(this, stsarg);
                    }
                }

                SetRecvFlag((byte)MESRECVFLAG.RECVFLAG_RECV_IDLE);
                //Volatile.Write(ref recvFlag, (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE);
                //recvFlag = (byte)MESRECVFLAG.RECVFLAG_RECV_IDLE;
                doingCmdFlag = false;
                mesRunningTimer.Start();
                //((DispatcherTimer)sender).Start();
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                doingCmdFlag = false;
                await DisconnectServer();
                mesRunningTimer.Start();
                ErrorCount++;
                //((DispatcherTimer)sender).Start();
                return;
            }
            //doingCmdFlag = false;
            //mesRunningTimer.Start();
            //((DispatcherTimer)sender).Start();
        }



        private async Task<int> WriteCCRFile3(int index, string data, FileMode mode)
        {
            StreamWriter writer = null;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string className = "MESClient";
            string funcName = "WriteCCRFile3";
            string fname = "";
            string value = "";
            FileInfo fi;
            Stopwatch sw = new Stopwatch();

            try
            {
                if(((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "mesDBUpdateFlag = MES_UPDATE_STATUS_INSERTING", Thread.CurrentThread.ManagedThreadId);

                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromSeconds(10))
                    {
                        if (((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag != (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING)
                            break;
                        await Task.Delay(50);
                    }
                    sw.Stop();

                    //return -3;
                }

                if (((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING)
                    ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_SAVING_FILE;

                ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_SAVING_FILE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
                curDir = curDir + "DATA\\";// CCR.dat";
                if (System.IO.Directory.Exists(curDir) == false)
                    System.IO.Directory.CreateDirectory(curDir);

                //fname = curDir + "\\CCR" + index.ToString("D2") + ".dat";
                if (value == "plantable")
                    fname = curDir + "CCR2.DAT";
                else
                    fname = curDir + "CCR.DAT";
                fi = new FileInfo(fname);

                if (mode == FileMode.CreateNew)
                {
                    //writer = new StreamWriter(fname, false);
                    writer = File.CreateText(fname);
                    if (writer == null)
                        return -1;
                    writer.WriteLine(data);
                    //await writer.WriteAsync(string.Empty);
                    //await writer.WriteAsync(data);
                    writer.Close();
                }
                else if (mode == FileMode.Append)
                {
                    //writer = new StreamWriter(fname, true);
                    writer = File.AppendText(fname);
                    if (writer == null)
                        return -2;
                    writer.WriteLine(data);
                    //await writer.WriteAsync(data);
                    writer.Close();
                }
                return 0;
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("WriteFile() exception = " + ex.HResult.ToString() + "||||" + data);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        async Task WriteCCRFile3Async(int index, string data, FileMode mode)
        {
            StreamWriter writer;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string fname = "";
            string value = "";
            FileInfo fi;
            string className = "MESClient";
            string funcName = "WriteCCRFile3";

            try
            {
                if (((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "mesDBUpdateFlag = MES_UPDATE_STATUS_INSERTING", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_SAVING_FILE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
                curDir = curDir + "DATA\\";// CCR.dat";
                if (System.IO.Directory.Exists(curDir) == false)
                    System.IO.Directory.CreateDirectory(curDir);

                //fname = curDir + "\\CCR" + index.ToString("D2") + ".dat";
                if (value == "plantable")
                    fname = curDir + "CCR2.DAT";
                else
                    fname = curDir + "CCR.DAT";
                fi = new FileInfo(fname);

                if (mode == FileMode.CreateNew)
                {
                    //writer = new StreamWriter(fname, false);
                    writer = File.CreateText(fname);
                    await writer.WriteLineAsync(data);
                    //await writer.WriteAsync(string.Empty);
                    //await writer.WriteAsync(data);
                    writer.Close();
                }
                else if (mode == FileMode.Append)
                {
                    //writer = new StreamWriter(fname, true);
                    writer = File.AppendText(fname);
                    await writer.WriteLineAsync(data);
                    //await writer.WriteAsync(data);
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteFile() exception = " + ex.HResult.ToString() + "||||" + data);
            }
        }


        private void SetWorkSocket(Socket sc)
        {
            Volatile.Write(ref workSocket, sc);
        }

        private Socket GetWorkSocket()
        {
            return Volatile.Read(ref workSocket);
        }

        private void SetRecvFlag(byte flag)
        {
            Volatile.Write(ref recvFlag, flag);
        }

        private byte GetRecvFlag()
        {
            return Volatile.Read(ref recvFlag);
        }


    }


    public class MESDATABLOCKHEADER
    {
        public int execResult;
        public string sendDate;
        public string deviceID;
        public string cmdKind;
        public string serial;
        public string dataSize;
        public string count;
        public string recordSize;
        public string reserved;
        public int irecordSize;
        public int icount;

        public MESDATABLOCKHEADER()
        {
            execResult = 0;
            sendDate = "";
            deviceID = "";
            cmdKind = "";
            serial = "";
            dataSize = "";
            count = "";
            recordSize = "";
            reserved = "";
            irecordSize = 0;
            icount = 0;
        }
    }

    public class MESDATAINFO
    {
        public int execResult;
        public string sendDate;
        public string deviceID;
        public string cmdKind;
        public string orderDate;
        public string totalSize;
        public string totalCount;
        public string recordSize;
        public string blockCount;
        public string blockSize;
        public string reserved;
        public long ltotalSize;
        public int iblockCount;
        public int itotalCount;
        public string totalMsg;

        public MESDATAINFO()
        {
            execResult = 0;
            sendDate = "";
            deviceID = "";
            cmdKind = "";
            orderDate = "";
            totalSize = "";
            totalCount = "";
            recordSize = "";
            blockCount = "";
            blockSize = "";
            reserved = "";
            ltotalSize = 0;
            iblockCount = 0;
            itotalCount = 0;
            totalMsg = "";
        }
    }
}

