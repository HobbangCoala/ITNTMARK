using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ITNTUTIL;
using ITNTCOMMON;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Windows.Interop;


#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    //enum RECVFLAG
    //{
    //    RECVFLAG_IDLE = 0,
    //    RECVFLAG_RECV_REQ = 1,
    //    RECVFLAG_RECV_SOH = 2,
    //    RECVFLAG_RECV_STX = 3,
    //    RECVFLAG_RECV_ACK = 4,
    //    RECVFLAG_RECV_NAK = 5,
    //    RECVFLAG_RECV_ETX = 6,
    //    RECVFLAG_RECV_END = 7,
    //    RECVFLAG_RECV_S30 = 8,
    //    RECVFLAG_RECV_S38 = 9,
    //    RECVFLAG_RECV_ERR = 0xff,
    //}

    //enum SENDFLAG
    //{
    //    SENDFLAG_IDLE = 0,
    //    SENDFLAG_SEND_REQ = 1,
    //    SENDFLAG_SEND_END = 2,
    //    SENDFLAG_SEND_ERR = 3,
    //}

    //enum COMMUNICATIONERROR
    //{
    //    ERR_SEND_DATA_NO_ERROR = 0,
    //    ERR_SEND_DATA_FAIL = -0x0001,
    //    ERR_RECV_ACK_TIMEOUT = -0x0002,
    //    ERR_RECV_RESPONSE_TIMEOUT = -0x0003,
    //    ERR_BUFFER_SIZE_ERROR = -0x0004,
    //    ERR_RECV_BCC_ERROR = -0x0005,
    //    ERR_CMD_MISSMATCH = -0x0006,
    //    ERR_PORT_NOT_OPENED = -0x0007,
    //    ERR_RECV_NAK = -0x0008,
    //    ERR_COMMAD_BUSY = -0x0009,
    //}

    class DistanceSensor2
    {
        RingBuffer rb = null;
        StateObject asyncstate = null;
        bool isConnected = false;
        bool doingCmdFlag = false;
        string serverIP = "";
        int serverPort = 0;
        bool doingCmdFlag2 = false;

        private readonly object lockbuff = new object();

        protected byte SendFlag = (byte)SENDFLAG.SENDFLAG_IDLE;
        protected byte RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
        byte[] RecvFrameData = new byte[64];
        int RecvFrameLength = 0;
        csConnStatus currentConnStatus = csConnStatus.Disconnected;
        csConnStatus currentConnBackup = csConnStatus.Disconnected;

        //DisplaceConnectionStatusChangedEventHandler callBackFunc;
        ConnectionChangedEventHandler callBackFunc;

        //string sDeviceCode = "8";
        //string sDeviceName = "DISPLACEMENT";

        private CancellationTokenSource _cancellationTokenSource;


        public DistanceSensor2(ConnectionChangedEventHandler callback)//distanceSensorDataArrivedCallbackHandler DataCallback)//, ClientConnectionHandler ConnectCallback)
        {
            callBackFunc = callback;
            asyncstate = new StateObject();
            rb = new RingBuffer(1024);
            isConnected = false;
        }

        public void StartClient()
        {
            string className = "DistanceSensor";
            string funcName = "StartClient";
            string value = "";

            // Connect to a remote device.  
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (isConnected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ALREADY CONNECTED!!", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }

                _cancellationTokenSource = new CancellationTokenSource();

                Util.GetPrivateProfileValue("SERVER", "SERVERIP", "192.168.0.50", ref value, Constants.DISPLACEMENT_INI_FILE);
                //IP = value;
                serverIP = value;
                Util.GetPrivateProfileValue("SERVER", "SERVERPORT", "64000", ref value, Constants.DISPLACEMENT_INI_FILE);
                int.TryParse(value, out serverPort);

                IPAddress ipAddress = IPAddress.Parse(serverIP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                // Create a TCP/IP socket.  
                asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                //connectDone.WaitOne();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
//                Console.WriteLine(ex.ToString());
            }
        }

        public async Task<int> StartClientSync(int timeout)
        {
            string className = "DistanceSensor";
            string funcName = "StartClientSync";
            string value = "";
            Stopwatch sw = new Stopwatch();

            // Connect to a remote device.  
            try
            {
                Util.GetPrivateProfileValue("SERVER", "SERVERIP", "192.168.0.50", ref serverIP, Constants.DISPLACEMENT_INI_FILE);
                Util.GetPrivateProfileValue("SERVER", "SERVERPORT", "64000", ref value, Constants.DISPLACEMENT_INI_FILE);
                int.TryParse(value, out serverPort);

                IPAddress ipAddress = IPAddress.Parse(serverIP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                // Create a TCP/IP socket.  
                asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
#if TEST_DEBUG_DISPLACEMENT
                return 0;
#else


                // Connect to the remote endpoint.  
                sw.Start();
                asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
                {
                    if (isConnected == true)
                        break;
                    await Task.Delay(50);
                }
                sw.Stop();
                if (isConnected == true)
                    return 0;
                else
                    return -1;
                //connectDone.WaitOne();
#endif

            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //                Console.WriteLine(ex.ToString());
                return ex.HResult;
            }
        }

//        public async Task<int> StartClientSync(int timeout)
//        {
//            string className = "DistanceSensor";
//            string funcName = "StartClientSync";
//            string value = "";
//            Stopwatch sw = new Stopwatch();

//            // Connect to a remote device.  
//            try
//            {
//                Util.GetPrivateProfileValue("SERVER", "SERVERIP", "192.168.0.50", ref value, Constants.DISPLACEMENT_INI_FILE);
//                //IP = value;
//                serverIP = value;
//                Util.GetPrivateProfileValue("SERVER", "SERVERPORT", "64000", ref value, Constants.DISPLACEMENT_INI_FILE);
//                int.TryParse(value, out serverPort);

//                IPAddress ipAddress = IPAddress.Parse(serverIP);
//                IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

//                // Create a TCP/IP socket.  
//                asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
//#if TEST_DEBUG_DISPLACEMENT
//                return 0;
//#else
//                // Connect to the remote endpoint.  
//                sw.Start();
//                asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
//                while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
//                {
//                    if (isConnected == true)
//                        break;
//                    await Task.Delay(50);
//                }
//                sw.Stop();
//                if (isConnected == true)
//                    return 0;
//                else
//                    return -1;
//                //connectDone.WaitOne();
//#endif

//            }
//            catch (Exception ex)
//            {
//                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
//                //                Console.WriteLine(ex.ToString());
//                return ex.HResult;
//            }
//        }

        public void CloseClient(byte flag)
        {
            asyncstate.workSocket.Close();
        }

        //public async Task<int> StartClientAsync(string IP, int port)
        //{
        //    // Connect to a remote device.  
        //    int retval = 0;
        //    try
        //    {
        //        // Establish the remote endpoint for the socket.  
        //        IPAddress ipAddress = IPAddress.Parse(IP);
        //        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

        //        // Create a TCP/IP socket.  
        //        asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        //        this.IP = IP;
        //        this.port = port;

        //        await asyncstate.workSocket.ConnectAsync(IP, port);

        //        // Connect to the remote endpoint.  
        //        //visionClient.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), visionClient);
        //        //connectDone.WaitOne();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }

        //    return retval;
        //}

        private void ConnectCallback(IAsyncResult ar)
        {
            string className = "DistanceSensor";
            string funcName = "ConnectCallback";

            try
            {
                //if (isConnected)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "callBack Connected already");
                //    return;
                //}


                asyncstate.workSocket = (Socket)ar.AsyncState;
                asyncstate.workSocket.NoDelay = true;

                //SetTcpKeepAlive(asyncstate.workSocket, 1000, 5);
                //if (!asyncstate.workSocket.Connected)
                //{
                //    //StartClient(IP, port);
                //    IPAddress ipAddress = IPAddress.Parse(IP);
                //    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                //    // Create a TCP/IP socket.  
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "beginConnect again");
                //    asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //    asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                //    return;
                //}
                // Complete the connection.  
                asyncstate.workSocket.EndConnect(ar);

                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RemoteEndPoint = " + asyncstate.workSocket.RemoteEndPoint.ToString());
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LocalEndPoint = " + asyncstate.workSocket.LocalEndPoint.ToString());

                isConnected = true;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "isConnected = true", Thread.CurrentThread.ManagedThreadId);
                Receive();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Recv() Start", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(ex.ToString());
            }
        }

        //private void Receive(Socket client)
        //{
        //    try
        //    {
        //        // Create the state object.  
        //        StateObject state = new StateObject();
        //        state.workSocket = client;

        //        // Begin receiving the data from the remote device.  
        //        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //}

        public void Receive()
        {
            try
            {
                // Create the state object.  
                //StateObject state = new StateObject();
                //state.workSocket = client;

                // Begin receiving the data from the remote device.  
                //byte[] bytebuff = new byte[1024];
                asyncstate.workSocket.BeginReceive(asyncstate.buffer, 0, asyncstate.buffer.Length, 0, new AsyncCallback(ReceiveCallback), asyncstate);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            string className = "DistanceSensor";
            string funcName = "ReceiveCallback";
            string msg = "";

            try
            {
                StateObject so = (StateObject)ar.AsyncState;
                Socket s = so.workSocket;

                int read = s.EndReceive(ar);
                if (read > 0)
                {
                    msg = Encoding.UTF8.GetString(so.buffer, 0, read);
                    ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RECV : " + msg, Thread.CurrentThread.ManagedThreadId);
                    if (msg.Length <= 0)
                        return;

                    lock (lockbuff)
                    {
                        rb.Put(so.buffer, read);
                    }
                    ReceiveCommData();

                    s.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                }
                else
                {
                    s.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(ex.ToString());
            }
        }

        public void ReceiveCommData()
        {
            string className = "DistanceSensor";
            string funcName = "ReceiveCommData";
            //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START : " + cb.Count().ToString());

            byte tmp = 0;
            int i = 0;
            string msg = "";
            byte[] look = new byte[1024];
            int checkFlag = 0;
            int idx = 0;

            try
            {
                lock (lockbuff)
                {
                    int leng = rb.GetSize();
                    rb.Look(ref look, leng);
                    for (i = 0; i < leng; i++)
                    {
                        tmp = look[i];
                        if(checkFlag == 0)
                        {
                            if ((tmp == 'M') || (tmp == 'E'))
                            {
                                checkFlag = 1;
                                RecvFrameData[idx++] = tmp;
                            }
                            else
                            {
                            }
                        }
                        else if(checkFlag == 1)
                        {
                            if(tmp == 0x0D)
                                checkFlag = 2;
                            else
                                RecvFrameData[idx++] = tmp;
                        }
                        else if(checkFlag == 2)
                        {
                            if (tmp == 0x0A)
                            {
                                RecvFrameLength = idx;
                                ITNTResponseArgs arg = new ITNTResponseArgs();
                                arg.recvString = msg;
                                arg.recvSize = msg.Length;
                                rb.Get(ref arg.recvBuffer, RecvFrameLength); 
                                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_END;
                                checkFlag = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void Send(String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            // Begin sending the data to the remote device.  
            string className = "DistanceSensor";
            string funcName = "Send";
            string msg = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RemoteEndPoint = " + asyncstate.workSocket.RemoteEndPoint.ToString(), Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "LocalEndPoint = " + asyncstate.workSocket.LocalEndPoint.ToString(), Thread.CurrentThread.ManagedThreadId);

                byte[] byteData = Encoding.ASCII.GetBytes(data);
                msg = Encoding.UTF8.GetString(byteData, 0, byteData.Length);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND : " + msg, Thread.CurrentThread.ManagedThreadId);
                asyncstate.workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void Send(byte[] data, int size, int loglevel=0)
        {
            // Convert the string data to byte data using ASCII encoding.  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);
            string className = "DistanceSensor";
            string funcName = "Send";
            string msg = "";

            // Begin sending the data to the remote device.  
            try
            {
                msg = Encoding.UTF8.GetString(data, 0, size);
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SEND2 : " + msg, Thread.CurrentThread.ManagedThreadId);
                asyncstate.workSocket.BeginSend(data, 0, size, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public csConnStatus GetConnectionStatus()
        {
            return csConnStatus.Connected;
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SetTcpKeepAlive(Socket socket, uint keepaliveTime, uint keepaliveInterval)
        {
            // marshal the equivalent of the native structure into a byte array
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(keepaliveTime)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepaliveTime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)keepaliveInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

            // write SIO_VALS to Socket IOControl
            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync(string cmd, byte[] msg, int size, int retrycnt, int loglevel, int timeout = 2000)
        {
            string className = "DistanceSensor";// MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = "ExecuteCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;

            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            int idx = 0;
            string sCurrentFunc = "EXECUTE COMMAND";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromMilliseconds(timeout))
                {
                    if (!doingCmdFlag)
                        break;

                    await Task.Delay(50);
                }
                sw.Stop();
                if (doingCmdFlag)
                {
                    retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : DEVICE BUSY (" + cmd + ")", Thread.CurrentThread.ManagedThreadId);

                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                    retval.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS BUSY2 (" + cmd + ")";

                    //retval.errorInfo.sErrorDevMsg = "DEVICE BUSY 2 : " + cmd;
                    //retval.errorInfo.sErrorCode = sDeviceCode + "01" + "02";
                    return retval;
                }

                doingCmdFlag = true;
                for (idx = 0; idx < retrycnt; idx++)
                {
                    InitializeExecuteCommand();
                    retval = await SendCommandMsgAsync(cmd, msg, size, loglevel, timeout);
                    if (retval.execResult < 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, idx), Thread.CurrentThread.ManagedThreadId);
                        continue;
                    }
                    retval = await RecvResponseMsgAsync(cmd, loglevel, timeout);
                    if (retval.execResult == 0)
                        break;
                    else
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, idx), Thread.CurrentThread.ManagedThreadId);
                        if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "NAK RECEIVED", Thread.CurrentThread.ManagedThreadId);
                            await Task.Delay(200);
                        }
                    }
                }
                doingCmdFlag = false;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                doingCmdFlag = false;
            }

            return retval;
        }

        //public async Task<ITNTResponseArgs> ExecuteCommandMsgAsync2(byte[] msg, int size, int retrycnt, int loglevel, int timeout = 2)
        //{
        //    string className = "DistanceSensor";
        //    string funcName = "ExecuteCommandMsgAsync";
        //    ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    //int retrycount = 0;
        //    int i = 0;

        //    try
        //    {
        //        Stopwatch sw = new Stopwatch();
        //        sw.Start();
        //        while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
        //        {
        //            if (!doingCmdFlag)
        //                break;

        //            await Task.Delay(50);
        //        }
        //        sw.Stop();
        //        if (doingCmdFlag)
        //        {
        //            retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY", Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }

        //        doingCmdFlag = true;
        //        if (retrycnt <= 0)
        //            retrycnt = 1;

        //        for (i = 0; i < retrycnt; i++)
        //        {
        //            InitializeExecuteCommand();
        //            retval.execResult = await SendCommandMsgAsync(msg, size, loglevel, timeout);
        //            if (retval.execResult < 0)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, i), Thread.CurrentThread.ManagedThreadId);
        //                continue;
        //            }
        //            retval = await RecvResponseMsgAsync(loglevel, timeout);
        //            if (retval.execResult == 0)
        //                break;
        //            else
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, i), Thread.CurrentThread.ManagedThreadId);
        //                if (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK)
        //                {
        //                    await Task.Delay(200);
        //                }
        //            }
        //        }
        //        doingCmdFlag = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //        doingCmdFlag = false;
        //    }

        //    return retval;
        //}

        //public async Task<ITNTResponseArgs> ExecuteCommand(byte[] msg, int size, int loglevel, int timeout)
        //{
        //    string className = "DistanceSensor";// MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = "ExecuteCommandMsgAsync";// MethodBase.GetCurrentMethod().Name;
        //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    ITNTResponseArgs recvArg = new ITNTResponseArgs();
        //    int retrycount = 0;
        //    //int iretval = 0;

        //    try
        //    {
        //        Stopwatch sw = new Stopwatch();
        //        sw.Start();
        //        while (sw.Elapsed < TimeSpan.FromSeconds(timeout))
        //        {
        //            if (!doingCmdFlag)
        //                break;

        //            await Task.Delay(50);
        //        }
        //        sw.Stop();
        //        if (doingCmdFlag)
        //        {
        //            //retval.execResult = (int)COMPORTERROR.ERR_COMMAD_BUSY;
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY", Thread.CurrentThread.ManagedThreadId);


        //            retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
        //            string tmp = Encoding.UTF8.GetString(msg, 0, size);
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : DEVICE BUSY " + tmp, Thread.CurrentThread.ManagedThreadId);
        //            currentCommand = "";

        //            //retval.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
        //            retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
        //            retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
        //            retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
        //            retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
        //            retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_CAUSE1_COMM + (-retval.execResult).ToString("X2");
        //            retval.errorInfo.devErrorInfo.sErrorMessage = "LASER DEVICE IS BUSY2 (" + cmd + ")";

        //            //retval.errorInfo.sErrorDevMsg = "DEVICE BUSY 2 : " + cmd;
        //            //retval.errorInfo.sErrorCode = sDeviceCode + "01" + "02";
        //            return retval;

        //            return retval;
        //        }

        //        doingCmdFlag = true;
        //        for (retrycount = 0; retrycount < 3; retrycount++)
        //        {
        //            InitializeExecuteCommand();
        //            retval = await SendCommandMsgAsync(msg, size, loglevel, timeout);
        //            if (retval <= 0)
        //            {
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("SendCommandMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
        //                continue;
        //            }
        //            recvArg = await RecvResponseMsgAsync(loglevel, timeout);
        //            //doingCmdFlag = false;
        //            if (retval == 0)
        //                break;
        //            //else if (retval.execResult == (int)COMPORTERROR.ERR_RECV_NAK)
        //            //{
        //            //    lock (bufferLock)
        //            //    {
        //            //        cb.Clear();
        //            //    }
        //            //}
        //            else
        //            {
        //                //doingCmdFlag = false;
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RecvResponseMsg ERROR : {0} / {1}", retval, retrycount), Thread.CurrentThread.ManagedThreadId);
        //                //if (retval == (int)COMPORTERROR.ERR_RECV_NAK)
        //                //{
        //                //    await Task.Delay(200);
        //                //}
        //            }
        //        }
        //        doingCmdFlag = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval = ex.HResult;
        //        doingCmdFlag = false;
        //    }

        //    //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> SendCommandMsgAsync(string cmd, byte[] msg, int size, int loglevel, int timeout = 2)
        {
            string className = "DistanceSensor";
            string funcName = "SendCommandMsgAsync";
            string sCurrentFunc = "SEND DATA";
            ITNTResponseArgs retval = new ITNTResponseArgs();

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                Send(msg, size, loglevel);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_REQ;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;

                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                return retval;
            }
        }

        private async Task<ITNTResponseArgs> RecvResponseMsgAsync(string cmd, int loglevel, int timeout = 2)
        {
            string className = "DistanceSensor";
            string funcName = "RecvResponseMsgAsync";

            Stopwatch sw = new Stopwatch();
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "RECEIVE DATA";

            try
            {
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                while (sw.Elapsed < TimeSpan.FromMilliseconds(timeout))
                {
                    if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_REQ)
                        break;
                    await Task.Delay(10);
                }
                sw.Stop();

                //if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_REQ)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ACK_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                //    //retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ACK_TIMEOUT;
                //    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;

                //    //retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_COMM +  (-retval.execResult).ToString("X2");
                //    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ACK_TIMEOUT;
                //    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_ACK_TIMEOUT;
                //    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                //    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                //    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                //    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ")" + " : RECEIVE TIMEOUT";
                //    retval.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");

                //    return retval;
                //}

                //sw.Restart();
                //while (sw.Elapsed < TimeSpan.FromMilliseconds(timeout))
                //{
                //    if (RecvFlag == (byte)RECVFLAG.RECVFLAG_RECV_END)
                //        break;
                //    await Task.Delay(10);
                //}
                //sw.Stop();

                if (RecvFlag != (byte)RECVFLAG.RECVFLAG_RECV_END)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RESPONSE_TIMEOUT", Thread.CurrentThread.ManagedThreadId);
                    RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;

                    retval.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.execResult = (int)COMMUNICATIONERROR.ERR_RECV_RESPONSE_TIMEOUT;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " : RECEIVE TIMEOUT";
                    retval.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_COMM + (-retval.execResult).ToString("X2");
                    return retval;
                }

                Array.Copy(RecvFrameData, retval.recvBuffer, RecvFrameLength);
                retval.recvSize = RecvFrameLength;
                ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                RecvFlag = (byte)RECVFLAG.RECVFLAG_RECV_ERR;
                retval.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.execResult = ex.HResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc + " (" + cmd + ")";
                retval.errorInfo.devErrorInfo.sErrorMessage = sCurrentFunc + " (" + cmd + ") EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                sw.Stop();
                return retval;
            }
            string msg = Encoding.UTF8.GetString(retval.recvBuffer, 0, retval.recvSize);
            ITNTTraceLog.Instance.Trace(loglevel, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("RECV : {0} / {1}", msg, retval.recvSize), Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        //public async Task<ITNTResponseArgs> ReadSensor(int loglevel)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    ITNTSendArgs sendArg = new ITNTSendArgs();

        //    sendArg.sendBuffer[0] = (byte)'M';
        //    sendArg.sendBuffer[1] = (byte)'0';
        //    sendArg.sendBuffer[2] = (byte)0x0D;
        //    sendArg.sendBuffer[3] = (byte)0x0A;

        //    sendArg.dataSize = 4;

        //    retval = await ExecuteCommandMsgAsync(sendArg.sendBuffer, sendArg.dataSize, loglevel, 1000);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> ReadSensor(int retrycount, int loglevel, CancellationToken token=default)
        {
            string className = "DistanceSensor";
            string funcName = "ReadSensor";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendArg = new ITNTSendArgs();
            Stopwatch sw = new Stopwatch();
            string cmd = "M0";

            try
            {
                sendArg.sendBuffer[0] = (byte)'M';
                sendArg.sendBuffer[1] = (byte)'0';
                sendArg.sendBuffer[2] = (byte)0x0D;
                sendArg.sendBuffer[3] = (byte)0x0A;

                sendArg.dataSize = 4;

                //if (doingCmdFlag2 == true)
                //{
                //    sw.Start();
                //    while (sw.Elapsed < TimeSpan.FromSeconds(4 * 1))
                //    {
                //        if (!doingCmdFlag2)
                //            break;

                //        await Task.Delay(50);
                //    }
                //    sw.Stop();
                //    sw.Reset();
                //    if (doingCmdFlag2)
                //    {
                //        retval.execResult = (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY;
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : BUSY2 (M0)", Thread.CurrentThread.ManagedThreadId);
                //        doingCmdFlag2 = false;
                //        return retval;
                //    }
                //}

                doingCmdFlag2 = true;

                retval = await ExecuteCommandMsgAsync(cmd, sendArg.sendBuffer, sendArg.dataSize, retrycount, loglevel, 1000);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK))
                {
                    retval = await ExecuteCommandMsgAsync(cmd, sendArg.sendBuffer, sendArg.dataSize, retrycount, loglevel, 1000);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_RECV_NAK))
                    {
                        retval = await ExecuteCommandMsgAsync(cmd, sendArg.sendBuffer, sendArg.dataSize, retrycount, loglevel, 1000);
                    }
                }

                if((retval.execResult != (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) && (retval.execResult != (int)COMMUNICATIONERROR.ERR_NO_ERROR))
                {
                    currentConnStatus = csConnStatus.Disconnected;
                    if (currentConnStatus != currentConnBackup)
                    {
                        DeviceStatusChangedEventArgs arg = new DeviceStatusChangedEventArgs();
                        arg.oldstatus = currentConnBackup;
                        arg.newstatus = currentConnStatus;
                        currentConnStatus = currentConnBackup;
                        callBackFunc?.Invoke(arg); 
                    }
                }
                doingCmdFlag2 = false;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                doingCmdFlag2 = false;
            }

            doingCmdFlag2 = false;
            return retval;
        }

        private void InitializeExecuteCommand()
        {
            RecvFlag = (byte)RECVFLAG.RECVFLAG_IDLE;
            RecvFrameData.Initialize();
            RecvFrameLength = 0;
        }

    }
}
