using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITNTUTIL;
using ITNTCOMMON;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014


namespace ITNTMARK
{
    public class ITNTClientAsync2
    {

        Socket m_workSocket;
        private ClientDataArrivalHandler DataArrivalCallback;
        private ClientConnectionHandler ConnectionCallback;
        private csConnStatus clientStatus = csConnStatus.Closed;
        private csConnStatus clientStatusBack = csConnStatus.Closed;
        RingBuffer rb = null;
        IPEndPoint remoteEndPoint = null;

        public ITNTClientAsync2(ClientDataArrivalHandler DataCallback, ClientConnectionHandler ConnectCallback)
        {
            DataArrivalCallback = DataCallback;
            ConnectionCallback = ConnectCallback;

            //asyncstate = new StateObject();
            m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            rb = new RingBuffer(1024);
            clientStatus = csConnStatus.Closed;
        }

        public void StartClient(string IP, int port)
        {
            string className = "ITNTClientAsync";
            string funcName = "StartClient";
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();

            // Connect to a remote device.  
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
                remoteEndPoint = remoteEP;

                // Create a TCP/IP socket.
                // 
                if(m_workSocket == null)
                    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //this.IP = IP;
                //this.port = port;
                //// Connect to the remote endpoint.  
                clientStatus = csConnStatus.Connecting;

                args.RemoteEndPoint = remoteEP;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                m_workSocket.NoDelay = true;
                m_workSocket.ConnectAsync(args);

                //asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                //connectDone.WaitOne();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //Console.WriteLine(e.ToString());
            }
        }

        public void StartClient()
        {
            string className = "ITNTClientAsync";
            string funcName = "StartClient";
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            string serverPort = "";
            string serverIP = "";
            int port = 0;

            // Connect to a remote device.  
            try
            {
                Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                int.TryParse(serverPort, out port);

                IPAddress ipAddress = IPAddress.Parse(serverIP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.
                // 
                if (m_workSocket == null)
                    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //this.IP = IP;
                //this.port = port;
                //// Connect to the remote endpoint.  
                clientStatus = csConnStatus.Connecting;

                args.RemoteEndPoint = remoteEP;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                m_workSocket.NoDelay = true;
                m_workSocket.ConnectAsync(args);

                //asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                //connectDone.WaitOne();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //Console.WriteLine(e.ToString());
            }
        }

        public void CloseClient()
        {
            if (m_workSocket != null)
                m_workSocket.Disconnect(false);

            m_workSocket?.Dispose();
            //asyncstate.workSocket.Close();
            clientStatus = csConnStatus.Closed;
        }

        private void ConnectCallback(object sender, SocketAsyncEventArgs e)
        {
            string className = "ITNTClientAsync";
            string funcName = "ConnectCallback";

            DeviceStatusChangedEventArgs arg = new DeviceStatusChangedEventArgs();
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            //IPEndPoint remoteEndPoint;
            string serverPort = "";
            string serverIP = "";
            int port = 0;

            try
            {
                if(e.ConnectSocket == null)
                {
                    if (m_workSocket == null)
                        m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    //if(remoteEndPoint != null)
                    {
                        //args.RemoteEndPoint = remoteEndPoint;
                        //args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                        //m_workSocket.ConnectAsync(args);

                        Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                        int.TryParse(serverPort, out port);
                        IPAddress ipAddress = IPAddress.Parse(serverIP);
                        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                        args.RemoteEndPoint = remoteEP;
                        args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                        m_workSocket.NoDelay = true;
                        m_workSocket.ConnectAsync(args);
                    }
                    return;
                }

                m_workSocket = (Socket)e.ConnectSocket;

                if(e.ConnectSocket.Connected == true)
                {
                    clientStatus = csConnStatus.Connected;
                    if ((int)clientStatus != (int)clientStatusBack)
                    {
                        arg.newstatus = clientStatus;
                        arg.oldstatus = clientStatusBack;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        clientStatusBack = clientStatus;
                    }
                    //ConnectionCallback();
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "isConnected = true");
                    Receive();
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "Recv() Start");
                }
                else
                {
                    if (m_workSocket == null)
                        m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    //if (remoteEndPoint != null)
                    {
                        Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                        int.TryParse(serverPort, out port);
                        IPAddress ipAddress = IPAddress.Parse(serverIP);
                        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                        args.RemoteEndPoint = remoteEP;
                        args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                        m_workSocket.NoDelay = true;
                        m_workSocket.ConnectAsync(args);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
                Console.WriteLine(ex.ToString());

                if (m_workSocket == null)
                    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //if (remoteEndPoint != null)
                {
                    Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                    int.TryParse(serverPort, out port);
                    IPAddress ipAddress = IPAddress.Parse(serverIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    args.RemoteEndPoint = remoteEP;
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                    m_workSocket.NoDelay = true;
                    m_workSocket.ConnectAsync(args);
                }

                clientStatus = csConnStatus.Closed;
                if ((int)clientStatus != (int)clientStatusBack)
                {
                    arg.newstatus = clientStatus;
                    arg.oldstatus = clientStatusBack;
                    // 데이터 수신 callback 함수 호출
                    if (ConnectionCallback != null)
                        ConnectionCallback(arg);

                    clientStatusBack = clientStatus;
                }

            }
        }

        public void Receive()
        {
            string className = "ITNTClientAsync";
            string funcName = "Receive";

            byte[] szData = new byte[1024];
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            DeviceStatusChangedEventArgs e = new DeviceStatusChangedEventArgs();
            string serverPort = "";
            string serverIP = "";
            int port = 0;

            try
            {
                // Create the state object.  
                //StateObject state = new StateObject();
                //state.workSocket = client;

                // Begin receiving the data from the remote device.  
                //byte[] bytebuff = new byte[1024];
                //asyncstate.workSocket.BeginReceive(asyncstate.buffer, 0, asyncstate.buffer.Length, 0, new AsyncCallback(ReceiveCallback), asyncstate);

                args.SetBuffer(szData, 0, szData.Length);
                args.UserToken = m_workSocket;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCallback);
                m_workSocket.ReceiveAsync(args);

            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //Console.WriteLine(e.ToString());
                if (m_workSocket == null)
                    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //if (remoteEndPoint != null)
                {
                    Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                    int.TryParse(serverPort, out port);
                    IPAddress ipAddress = IPAddress.Parse(serverIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    args.RemoteEndPoint = remoteEP;
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                    m_workSocket.NoDelay = true;
                    m_workSocket.ConnectAsync(args);
                }

                clientStatus = csConnStatus.Closed;
                if ((int)clientStatus != (int)clientStatusBack)
                {
                    e.newstatus = clientStatus;
                    e.oldstatus = clientStatusBack;
                    // 데이터 수신 callback 함수 호출
                    if (ConnectionCallback != null)
                        ConnectionCallback(e);

                    clientStatusBack = clientStatus;
                }
            }
        }


        private void ReceiveCallback(object sender, SocketAsyncEventArgs e)
        {
            string className = "ITNTClientAsync";
            string funcName = "ReceiveCallback";
            string msg = "";
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            byte[] szData = new byte[1024];
            DeviceStatusChangedEventArgs arg = new DeviceStatusChangedEventArgs();
            string serverPort = "";
            string serverIP = "";
            int port = 0;

            try
            {
                m_workSocket = sender as Socket;

                if(m_workSocket == null)
                {
                    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    //e.AcceptSocket = null;
                    //m_ServerSocket.AcceptAsync(e);
                    Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                    int.TryParse(serverPort, out port);
                    IPAddress ipAddress = IPAddress.Parse(serverIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    args.RemoteEndPoint = remoteEP;
                    m_workSocket.NoDelay = true;
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                    m_workSocket.ConnectAsync(args);
                    return;
                }

                //StateObject so = (StateObject)ar.AsyncState;
                //Socket s = so.workSocket;

                //int read = s.EndReceive(ar);
                //if (read > 0)
                if(m_workSocket.Connected && (e.BytesTransferred > 0))
                {
                    //so.sb.Append(Encoding.ASCII.GetString(so.buffer, 0, read));
                    msg = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "RECV : " + msg);
                    msg = msg.Replace("\0", "");

                    rb.Put(e.Buffer, e.BytesTransferred);

                    if (rb.GetSize() > 1)
                    {
                        int idxSOH = -1;
                        int idxETX = -1;
                        byte[] recvData = new byte[1024];
                        rb.Look(ref recvData, rb.GetSize());
                        if ((recvData.Contains<byte>(0x01)) && (recvData.Contains<byte>(0x02)) && (recvData.Contains<byte>(0x03)))
                        {
                            idxSOH = Array.IndexOf<byte>(recvData, 0x01);
                            idxETX = Array.IndexOf<byte>(recvData, 0x03);
                            if ((idxSOH >= 0) && (idxETX >= 0) && ((idxETX - idxSOH) > 0))
                            {
                                recvData.Initialize();
                                rb.Get(ref recvData, idxETX + 1);
                                AnalysisRecvData(recvData, idxETX + 1);
                            }
                        }
                    }

                    args.SetBuffer(szData, 0, szData.Length);
                    args.UserToken = m_workSocket;
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCallback);
                    m_workSocket.ReceiveAsync(args);
                    //s.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
                }
                else
                {
                    if (m_workSocket == null)
                        m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    if(m_workSocket.Connected)
                    {
                        m_workSocket.Disconnect(false);
                        m_workSocket.Dispose();
                    }
                    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    //if (remoteEndPoint != null)
                    {
                        Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                        int.TryParse(serverPort, out port);
                        IPAddress ipAddress = IPAddress.Parse(serverIP);
                        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                        args.RemoteEndPoint = remoteEP;
                        args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                        m_workSocket.NoDelay = true;
                        m_workSocket.ConnectAsync(args);
                    }

                    clientStatus = csConnStatus.Closed;
                    if ((int)clientStatus != (int)clientStatusBack)
                    {
                        arg.newstatus = clientStatus;
                        arg.oldstatus = clientStatusBack;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        clientStatusBack = clientStatus;
                    }

                    //if (rb.GetSize() > 1)
                    //{
                    //    int idxSOH = -1;
                    //    int idxETX = -1;
                    //    byte[] recvData = new byte[1024];
                    //    rb.Look(ref recvData, rb.GetSize());
                    //    if ((recvData.Contains<byte>(0x01)) && (recvData.Contains<byte>(0x02)) && (recvData.Contains<byte>(0x03)))
                    //    {
                    //        idxSOH = Array.IndexOf(recvData, 0x01);
                    //        idxETX = Array.IndexOf(recvData, 0x03);
                    //        if ((idxSOH >= 0) && (idxETX >= 0) && ((idxETX - idxSOH) > 0))
                    //        {
                    //            recvData.Initialize();
                    //            rb.Get(ref recvData, idxETX + 1);
                    //            msg = Encoding.UTF8.GetString(recvData, 0, idxETX + 1);//, read);
                    //            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "RECV : " + msg);
                    //            AnalysisRecvData(recvData, idxETX + 1);
                    //        }
                    //    }
                    //}
                    //s.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);

                    ////receiveDone.Set();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
                Console.WriteLine(ex.ToString());
                if (m_workSocket == null)
                    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //if (remoteEndPoint != null)
                {
                    Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                    int.TryParse(serverPort, out port);
                    IPAddress ipAddress = IPAddress.Parse(serverIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    args.RemoteEndPoint = remoteEP;
                    m_workSocket.NoDelay = true;
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                    m_workSocket.ConnectAsync(args);
                }

                clientStatus = csConnStatus.Closed;
                if ((int)clientStatus != (int)clientStatusBack)
                {
                    arg.newstatus = clientStatus;
                    arg.oldstatus = clientStatusBack;
                    // 데이터 수신 callback 함수 호출
                    if (ConnectionCallback != null)
                        ConnectionCallback(arg);

                    clientStatusBack = clientStatus;
                }
            }
        }


        private int AnalysisRecvData(byte[] recvMsg, int count)
        {
            string className = "ITNTClientAsync";
            string funcName = "AnalysisRecvData";

            int i = 0;
            int etx = 0;
            int stx = 0;
            string msg = "";

            for (i = 0; i < count; i++)
            {
                if (recvMsg[i] == 0x02)
                    stx = i;
                if (recvMsg[i] == 0x03)
                    etx = i;
            }

            msg = Encoding.UTF8.GetString(recvMsg, stx + 1, Math.Max(0, (etx - stx - 1)));
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "RECV : " + msg);
            DataArrivalCallback?.Invoke(msg);

            return 0;
        }

        //private void Send(String data)
        //{
        //    // Convert the string data to byte data using ASCII encoding.  
        //    // Begin sending the data to the remote device.  
        //    string className = "ITNTClientAsync";
        //    string funcName = "Send";
        //    string msg = "";
        //    SocketAsyncEventArgs args = new SocketAsyncEventArgs();

        //    try
        //    {
        //        byte[] byteData = Encoding.ASCII.GetBytes(data);
        //        msg = Encoding.UTF8.GetString(byteData);
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SEND : " + msg);

        //        if(m_workSocket == null)
        //        {
        //            if (m_workSocket == null)
        //                m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        //            if (remoteEndPoint != null)
        //            {
        //                args.RemoteEndPoint = remoteEndPoint;
        //                args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
        //                m_workSocket.ConnectAsync(args);
        //            }
        //            return;
        //        }
        //        asyncstate.workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
        //    }
        //}
        public async Task<int> SendMessage(ITNTSendArgs msg)
        {
            string className = "VisionServer";
            string funcName = "SendMessage";

            int retval = 0;
            int sendleng = 0;
            byte[] sendbuff;
            int i = 0;
            string smsg = "";
            try
            {
                if ((m_workSocket == null) || !m_workSocket.Connected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage Error : (connClients.socket == null) || !connClients.socket.Connected", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }

                if (msg.sendString.Length > 0)
                {
                    sendleng = msg.sendString.Length;
                    sendbuff = new byte[sendleng + 4];
                    byte[] tmp = new byte[sendleng];
                    tmp = Encoding.UTF8.GetBytes(msg.sendString);

                    sendbuff[i++] = (byte)ASCII.SOH;
                    sendbuff[i++] = (byte)ASCII.STX;
                    Array.Copy(tmp, 0, sendbuff, i, sendleng);
                    i += sendleng;
                    sendbuff[i++] = (byte)ASCII.ETX;
                    sendbuff[i++] = (byte)ASCII.CR;
                    sendleng += 4;
                    smsg = msg.sendString;
                }
                else
                {
                    sendleng = msg.dataSize;
                    sendbuff = new byte[sendleng];
                    Array.Copy(msg.sendBuffer, sendbuff, sendleng);
                    smsg = Encoding.UTF8.GetString(sendbuff, 0, sendleng);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage Before : " + smsg, Thread.CurrentThread.ManagedThreadId);
                if ((m_workSocket != null) && m_workSocket.Connected)
                {
                    m_workSocket.Send(sendbuff, sendleng, SocketFlags.None);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage After : " + smsg, Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage : NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }
            }
            catch (Exception ex)
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            return retval;
        }


        public void Send(byte[] data, int size)
        {
            // Convert the string data to byte data using ASCII encoding.  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);
            string className = "ITNTClientAsync";
            string funcName = "Send";
            string msg = "";
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            DeviceStatusChangedEventArgs arg = new DeviceStatusChangedEventArgs();
            string serverPort = "";
            string serverIP = "";
            int port = 0;

            // Begin sending the data to the remote device.  
            try
            {
                if ((m_workSocket == null) || (clientStatus != csConnStatus.Connected))
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SEND ERROR - DISCONNECTED");
                    if (m_workSocket == null)
                        m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    //if (remoteEndPoint != null)
                    {
                        Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                        Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                        int.TryParse(serverPort, out port);
                        IPAddress ipAddress = IPAddress.Parse(serverIP);
                        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                        args.RemoteEndPoint = remoteEP;
                        args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                        //m_workSocket.NoDelay = true;
                        m_workSocket.ConnectAsync(args);
                    }

                    clientStatus = csConnStatus.Closed;
                    if ((int)clientStatus != (int)clientStatusBack)
                    {
                        arg.newstatus = clientStatus;
                        arg.oldstatus = clientStatusBack;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        clientStatusBack = clientStatus;
                    }
                    //clientStatus = csConnStatus.Disconnected;
                    return;
                }

                msg = Encoding.UTF8.GetString(data, 0, size);
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SEND2 : " + msg);
                //if (m_workSocket == null)
                //{
                //    m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                //    args.RemoteEndPoint = remoteEndPoint;
                //    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                //    m_workSocket.ConnectAsync(args);

                //    return;
                //}

                //asyncstate.workSocket.BeginSend(data, 0, size, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
                m_workSocket.Send(data, 0, size, 0);
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SEND2 : END");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
                //if (m_workSocket == null)
                m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                if (m_workSocket.Connected)
                {
                    m_workSocket.Disconnect(false);
                    m_workSocket.Dispose();
                }

                //if (remoteEndPoint != null)
                {
                    Util.GetPrivateProfileValue("VISION", "SERVERPORT", "", ref serverPort, Constants.PARAMS_INI_FILE);
                    Util.GetPrivateProfileValue("VISION", "SERVERIP", "", ref serverIP, Constants.PARAMS_INI_FILE);

                    int.TryParse(serverPort, out port);
                    IPAddress ipAddress = IPAddress.Parse(serverIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    args.RemoteEndPoint = remoteEP;
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                    //m_workSocket.NoDelay = true;
                    m_workSocket.ConnectAsync(args);
                }

                clientStatus = csConnStatus.Closed;
                if ((int)clientStatus != (int)clientStatusBack)
                {
                    arg.newstatus = clientStatus;
                    arg.oldstatus = clientStatusBack;
                    // 데이터 수신 callback 함수 호출
                    if (ConnectionCallback != null)
                        ConnectionCallback(arg);

                    clientStatusBack = clientStatus;
                }
            }
        }

        public csConnStatus GetConnectionStatus()
        {
            return clientStatus;
        }
    }
}
