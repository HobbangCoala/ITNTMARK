using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTCOMMM;
using ITNTCOMMON;
using ITNTUTIL;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public class SocketList
    {
        public Socket m_socket;
        public bool m_connected;

        public SocketList(byte flag=0)
        {
            m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            m_connected = false;
        }
    }

    public class VisionServer2
    {
        public event VisionDataArrivedEventHandler DataArrivedEventFunc;
        private ClientDataArrivalHandler DataArrivalCallback;
        private ClientConnectionHandler ConnectionCallback;

        private bool DoingAcceptThread = false;
        RingBuffer m_RecvData;
        byte[]m_RecvBuff = new byte[1024];
        int m_RecvLength = 0;
        //private Socket m_socket;
        //private Socket m_ServerSocket;

        private Socket m_ServerSocket;
        private List<SocketList> m_ClientSocket;
        private byte[] szData;
        private Socket m_currSocket;
        public csConnStatus m_connStatus = csConnStatus.Closed;
        public csConnStatus m_connBackStatus = csConnStatus.Closed;

        private VISION_RECV_STATUS recvFlag = VISION_RECV_STATUS.VISION_RECV_NORMAL;

        //IPEndPoint remoteEndPoint = null;

        //Queue<byte[]> _sendQueue = new Queue<byte[]>();
        //bool _pending = false;
        //object _lock = new object();

        public VisionServer2(ClientDataArrivalHandler DataCallback, ClientConnectionHandler ConnectCallback)
        {
            DataArrivalCallback = DataCallback;
            ConnectionCallback = ConnectCallback;

            m_RecvData = new RingBuffer(1024);
            m_RecvBuff = new byte[1024];

            m_ClientSocket = new List<SocketList>();
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_currSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            recvFlag = VISION_RECV_STATUS.VISION_RECV_NORMAL;

            //asyncstate = new StateObject();
            //m_workSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //rb = new RingBuffer(1024);
            m_connStatus = csConnStatus.Closed;
        }

        public VisionServer2()
        {
            m_RecvData = new RingBuffer(1024);
            m_RecvBuff = new byte[1024];
            //m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            m_ClientSocket = new List<SocketList>();
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_currSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            recvFlag = VISION_RECV_STATUS.VISION_RECV_NORMAL;
        }

        public VisionServer2(int buffersize)
        {
            m_RecvData = new RingBuffer(buffersize);
            m_RecvBuff = new byte[1024];
            m_ClientSocket = new List<SocketList>();
            //m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_currSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            recvFlag = VISION_RECV_STATUS.VISION_RECV_NORMAL;
        }

        public async Task<int> StartServer(IPAddress serverIP, int serverPort)
        {
            string className = "VisionServer";
            string funcName = "AcceptCallback";

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                IPEndPoint endPoint = new IPEndPoint(serverIP, serverPort); //엔드포인트(ip주소와 포트번호)
                //if(m_ServerSocket == null)
                //    m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //else if(m_ServerSocket.Connected == false)
                    m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                m_ServerSocket.Bind(endPoint);
                m_ServerSocket.Listen(20);
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                m_ServerSocket.AcceptAsync(args);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (SocketException se)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 1 - CODE = {0:X}, MSG = {1}", se.HResult, se.Message), Thread.CurrentThread.ManagedThreadId);
                Debug.WriteLine("StartServer" + se.HResult.ToString() + " : " + se.Message);
                return se.HResult;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2 - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Debug.WriteLine("StartServer" + ex.HResult.ToString() + " : " + ex.Message);
                return ex.HResult;
            }

            return 0;
        }

        public void ServerClose()
        {
            SocketList socketlist = new SocketList();
            //bDoingCheckPartnerThread = false;
            try
            {
                DoingAcceptThread = false;

                if (m_currSocket == null)
                    return;

                if(m_currSocket.Connected == true)
                {
                    socketlist.m_socket = m_currSocket;
                    socketlist.m_socket.Disconnect(false);
                    socketlist.m_socket.Dispose();
                    socketlist.m_socket = null;
                    socketlist.m_connected = false;
                    m_ClientSocket.Remove(socketlist);

                    if(m_ServerSocket != null)
                    {
                        m_ServerSocket.Disconnect(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        #region Accept_Completed 
        private void Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            string className = "VisionServer";
            string funcName = "Accept_Completed";

            SocketList socketlist = new SocketList();
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            DeviceStatusChangedEventArgs arg = new DeviceStatusChangedEventArgs();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (e.AcceptSocket == null)
                {
                    //e.AcceptSocket = null;
                    m_connStatus = csConnStatus.Connecting;
                    if ((int)m_connStatus != (int)m_connBackStatus)
                    {
                        arg.newstatus = m_connStatus;
                        arg.oldstatus = m_connBackStatus;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        m_connBackStatus = m_connStatus;
                    }

                    if (m_ServerSocket == null)
                        m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                    m_ServerSocket.AcceptAsync(args);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "e.AcceptSocket == NULL", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                if (m_ClientSocket == null)
                    m_ClientSocket = new List<SocketList>();

                socketlist.m_socket = e.AcceptSocket;
                if (socketlist.m_socket.Connected == true)
                {
                    m_connStatus = csConnStatus.Connected;
                    if ((int)m_connStatus != (int)m_connBackStatus)
                    {
                        arg.newstatus = m_connStatus;
                        arg.oldstatus = m_connBackStatus;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        m_connBackStatus = m_connStatus;
                    }

                    socketlist.m_connected = true;
                    m_ClientSocket.Add(socketlist);
                    m_currSocket = socketlist.m_socket;
                    szData = new byte[1024];
                    args.SetBuffer(szData, 0, szData.Length);
                    args.UserToken = m_ClientSocket;
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(Receive_Completed);
                    socketlist.m_socket.ReceiveAsync(args);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VISION CONNECTED", Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    m_connStatus = csConnStatus.Connecting;
                    if ((int)m_connStatus != (int)m_connBackStatus)
                    {
                        arg.newstatus = m_connStatus;
                        arg.oldstatus = m_connBackStatus;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        m_connBackStatus = m_connStatus;
                    }

                    if (m_ServerSocket == null)
                        m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                    m_ServerSocket.AcceptAsync(args);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (SocketException se)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 1 - CODE = {0:X}, MSG = {1}", se.HResult, se.Message), Thread.CurrentThread.ManagedThreadId);
                Trace.WriteLine(string.Format("SocketException : {0}", se.Message));

                try
                {
                    m_connStatus = csConnStatus.Connecting;
                    if ((int)m_connStatus != (int)m_connBackStatus)
                    {
                        arg.newstatus = m_connStatus;
                        arg.oldstatus = m_connBackStatus;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        m_connBackStatus = m_connStatus;
                    }

                    if (e.AcceptSocket == null)
                    {
                        //e.AcceptSocket = null;
                        if (m_ServerSocket == null)
                            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                        m_ServerSocket.AcceptAsync(args);
                        return;
                    }
                }
                catch (Exception ex1)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 12 - CODE = {0:X}, MSG = {1}", ex1.HResult, ex1.Message), Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 2 - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Trace.WriteLine(string.Format("Exception : {0}", ex.Message));
                try
                {
                    m_connStatus = csConnStatus.Connecting;
                    if ((int)m_connStatus != (int)m_connBackStatus)
                    {
                        arg.newstatus = m_connStatus;
                        arg.oldstatus = m_connBackStatus;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        m_connBackStatus = m_connStatus;
                    }

                    if (e.AcceptSocket == null)
                    {
                        //e.AcceptSocket = null;
                        if (m_ServerSocket == null)
                            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                        m_ServerSocket.AcceptAsync(args);
                        return;
                    }
                }
                catch (Exception ex2)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 22 - CODE = {0:X}, MSG = {1}", ex2.HResult, ex2.Message), Thread.CurrentThread.ManagedThreadId);
                }
            }
        }
        #endregion

        #region Receive_Completed 
        private void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            string className = "VisionServer";
            string funcName = "Receive_Completed";

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            DeviceStatusChangedEventArgs arg = new DeviceStatusChangedEventArgs();
            SocketList socketlist = new SocketList();
            int iLeng = 0;

            try
            {
                //Socket ClientSocket = sender as Socket;
                socketlist.m_socket = sender as Socket;

                if (socketlist.m_socket == null)
                {
                    m_connStatus = csConnStatus.Connecting;
                    if ((int)m_connStatus != (int)m_connBackStatus)
                    {
                        arg.newstatus = m_connStatus;
                        arg.oldstatus = m_connBackStatus;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        m_connBackStatus = m_connStatus;
                    }

                    //e.AcceptSocket = null;
                    if (m_ServerSocket == null)
                        m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                    m_ServerSocket.AcceptAsync(args);
                    return;
                }

                if (socketlist.m_socket.Connected && e.BytesTransferred > 0)
                {
                    //byte[] szData = e.Buffer;
                    m_currSocket = socketlist.m_socket;
                    if(e.Buffer != null)
                    {
                        iLeng = m_RecvData.Put(e.Buffer, e.BytesTransferred);
                        AnalyzeRecvData();
                        socketlist.m_socket.ReceiveAsync(e);
                    }
                    else
                    {
                        //if (m_ServerSocket == null)
                        //    m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        //args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                        //m_ServerSocket.AcceptAsync(args);

                        e.SetBuffer(szData, 0, 1024);
                        socketlist.m_socket.ReceiveAsync(e);
                    }
                }
                else
                {
                    m_connStatus = csConnStatus.Disconnected;
                    if ((int)m_connStatus != (int)m_connBackStatus)
                    {
                        arg.newstatus = m_connStatus;
                        arg.oldstatus = m_connBackStatus;
                        // 데이터 수신 callback 함수 호출
                        if (ConnectionCallback != null)
                            ConnectionCallback(arg);

                        m_connBackStatus = m_connStatus;
                    }

                    //m_currSocket.Disconnect(false);
                    //m_currSocket.Dispose();
                    //m_currSocket = socketlist.m_socket;
                    socketlist.m_socket.Disconnect(false);
                    socketlist.m_socket.Dispose();
                    socketlist.m_socket = null;
                    socketlist.m_connected = false;
                    m_ClientSocket.Remove(socketlist);
                    if(m_ServerSocket == null)
                        m_ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    //m_ServerSocket.AcceptAsync();
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                    m_ServerSocket.AcceptAsync(args);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                m_connStatus = csConnStatus.Connecting;
                if ((int)m_connStatus != (int)m_connBackStatus)
                {
                    arg.newstatus = m_connStatus;
                    arg.oldstatus = m_connBackStatus;
                    // 데이터 수신 callback 함수 호출
                    if (ConnectionCallback != null)
                        ConnectionCallback(arg);

                    m_connBackStatus = m_connStatus;
                }

                if (m_ServerSocket == null)
                    m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                m_ServerSocket.AcceptAsync(args);
            }
            //접속한 클라이언수 보이기    
            //label1.Text = m_ClientSocket.Count.ToString();
        }
        #endregion


        private async Task AnalyzeRecvData()
        {
            string className = "VisionServer";
            string funcName = "AnalyzeRecvData";

            int iLeng = 0;
            int looksize = 0;
            byte[] temp;
            bool stxFlag = false;
            byte data = 0;
            string msg = "";
            ITNTResponseArgs respArg = new ITNTResponseArgs(1024);

            try
            {
                iLeng = m_RecvData.GetSize();
                temp = new byte[iLeng];
                looksize = m_RecvData.Look(ref temp, iLeng);
                if (looksize <= 0)
                    return;

                for (int i = 0; i < looksize; i++)
                {
                    if (temp[i] == (byte)ASCII.SOH)
                    {
                        recvFlag = VISION_RECV_STATUS.VISION_RECV_SOH;
                    }
                    //else if ((temp[i] == (byte)ASCII.STX) && (recvFlag == VISION_RECV_STATUS.VISION_RECV_SOH))
                    else if (temp[i] == (byte)ASCII.STX)
                    {
                        stxFlag = true;
                        recvFlag = VISION_RECV_STATUS.VISION_RECV_STX;
                    }
                    //else if ((temp[i] == (byte)ASCII.ETX) && (recvFlag == VISION_RECV_STATUS.VISION_RECV_STX))
                    else if ((temp[i] == (byte)ASCII.ETX) && stxFlag)
                    {
                        recvFlag = VISION_RECV_STATUS.VISION_RECV_ETX;
                    }
                    //else if ((temp[i] == (byte)ASCII.CR) && (recvFlag == VISION_RECV_STATUS.VISION_RECV_ETX))
                    else if ((temp[i] == (byte)ASCII.CR) && stxFlag)
                    {
                        recvFlag = VISION_RECV_STATUS.VISION_RECV_CRT;
                        m_RecvData.Get(ref temp, i + 1);
                        if ((i + 1) > 4)
                        {
                            msg = Encoding.UTF8.GetString(temp, 2, (i + 1) - 4);
                            respArg.recvString = msg;
                            DataArrivalCallback?.Invoke(msg);
                            //OnVisionDataArrivedEventHandlerFunc(respArg);
                        }
                        //AnalyzerReceivedData(temp, i + 1);
                        stxFlag = false;
                    }
                    else
                    {
                        if (!stxFlag)
                            m_RecvData.Get(ref data);
                    }
                }
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        //protected void AnalyzerReceivedData(byte[] data, int size)
        //{
        //    string className = "VisionServer";
        //    string funcName = "AnalyzerReceivedData";

        //    string msg;
        //    try
        //    {
        //        if (size < 4)
        //            return;
        //        byte[] temp = new byte[size - 4];
        //        Array.Copy(data, 2, temp, 0, size - 4);
        //        msg = Encoding.UTF8.GetString(temp, 0, size - 4);
        //        ITNTResponseArgs arg = new ITNTResponseArgs();
        //        arg.recvString = msg;
        //        OnVisionDataArrivedEventHandlerFunc(arg);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}


        protected void OnVisionDataArrivedEventHandlerFunc(ITNTResponseArgs e)
        {
            //DataArrivedEventFunc?.Invoke(this, e);

            ////VisionDataArrivedEventHandler handler = DataArrivedEventFunc;
            ////if (handler != null)
            ////    handler(this, e);
        }

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
                if ((m_currSocket == null) || !m_currSocket.Connected)
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
                if ((m_currSocket != null) && m_currSocket.Connected)
                {
                    m_currSocket.Send(sendbuff, sendleng, SocketFlags.None);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage After : " + smsg, Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage : NOT CONNECTED", Thread.CurrentThread.ManagedThreadId);
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
            string className = "VisionServer";
            string funcName = "Send";
            string msg = "";
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            DeviceStatusChangedEventArgs arg = new DeviceStatusChangedEventArgs();

            // Begin sending the data to the remote device.  
            try
            {
                if ((m_currSocket == null) || (m_connStatus != csConnStatus.Connected))
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SEND ERROR - DISCONNECTED");
                    //if (m_currSocket == null)
                    //    m_currSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    //if (remoteEndPoint != null)
                    //{
                    //    args.RemoteEndPoint = remoteEndPoint;
                    //    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                    //    //m_currSocket.NoDelay = true;
                    //    m_currSocket.ConnectAsync(args);
                    //}

                    //m_connStatus = csConnStatus.Closed;
                    //if ((int)m_connStatus != (int)m_connStatusBack)
                    //{
                    //    arg.newstatus = m_connStatus;
                    //    arg.oldstatus = m_connStatusBack;
                    //    // 데이터 수신 callback 함수 호출
                    //    if (ConnectionCallback != null)
                    //        ConnectionCallback(arg);

                    //    m_connStatusBack = m_connStatus;
                    //}
                    //m_connStatus = csConnStatus.Disconnected;
                    return;
                }

                msg = Encoding.UTF8.GetString(data, 0, size);
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SEND2 : " + msg);
                //if (m_currSocket == null)
                //{
                //    m_currSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                //    args.RemoteEndPoint = remoteEndPoint;
                //    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                //    m_currSocket.ConnectAsync(args);

                //    return;
                //}

                //asyncstate.workSocket.BeginSend(data, 0, size, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
                m_currSocket.Send(data, 0, size, 0);
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SEND2 : END");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
                //if (m_currSocket == null)
                //m_currSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //if (m_currSocket.Connected)
                //{
                //    m_currSocket.Disconnect(false);
                //    m_currSocket.Dispose();
                //}

                //if (remoteEndPoint != null)
                //{
                //    args.RemoteEndPoint = remoteEndPoint;
                //    args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCallback);
                //    //m_currSocket.NoDelay = true;
                //    m_currSocket.ConnectAsync(args);
                //}

                //m_connStatus = csConnStatus.Closed;
                //if ((int)m_connStatus != (int)m_connStatusBack)
                //{
                //    arg.newstatus = m_connStatus;
                //    arg.oldstatus = m_connStatusBack;
                //    // 데이터 수신 callback 함수 호출
                //    if (ConnectionCallback != null)
                //        ConnectionCallback(arg);

                //    m_connStatusBack = m_connStatus;
                //}
            }
        }

#if TEST_DEBUG_VISION
        async Task AsyncTcpProcess(object o)
        {
            TcpClient tc = (TcpClient)o;

            int MAX_SIZE = 1024;  // 가정
            NetworkStream stream = tc.GetStream();

            // 비동기 수신            
            var buff = new byte[MAX_SIZE];
            var nbytes = await stream.ReadAsync(buff, 0, buff.Length).ConfigureAwait(false);
            if (nbytes > 0)
            {
                string msg = Encoding.ASCII.GetString(buff, 0, nbytes);
                Console.WriteLine($"{msg} at {DateTime.Now}");

                // 비동기 송신
                await stream.WriteAsync(buff, 0, nbytes).ConfigureAwait(false);
            }

            stream.Close();
            tc.Close();
        }
#endif
        public int InitializeServer()
        {
            int retval = 0;
            //count = 0;
            ////connClients.Clear();

            return retval;
        }

#if TEST_DEBUG_VISION
        //async void AcceptClientThread(object obj)
        async void AcceptClientThread()
        {
            string value = "";
            string valueback = "";
            try
            {
                while (DoingAcceptThread)
                {
                    Util.GetPrivateProfileValue("VISION_TEST", "RESULT", "V0O", ref value, "TEST.ini");
                    if(value != valueback)
                    {
                        ITNTResponseArgs arg = new ITNTResponseArgs();
                        arg.recvString = value;
                        valueback = value;
                        OnVisionDataArrivedEventHandlerFunc(arg);
                    }
                    //listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), listener);
                    await Task.Delay(100);
                }
            }
            catch (SocketException se)
            {
                Debug.WriteLine("AcceptClientThread" + se.HResult.ToString() + " : " + se.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AcceptClientThread" + ex.HResult.ToString() + " : " + ex.Message);
            }
        }
#endif
        //private async void AcceptCallback(IAsyncResult ar)
        //{
        //    string className = "VisionServer";
        //    string funcName = "AcceptCallback";
        //    try
        //    {
        //        ConnectedClient conn = new ConnectedClient();
        //        conn.socket = listener.EndAcceptTcpClient(ar);
        //        SetTcpKeepAlive(conn.socket.Client, 1000, 5);
        //        conn.socket.NoDelay = true;
        //        conn.Id = count;
        //        conn.stream = conn.socket.GetStream();
        //        count++;
        //        connClients = conn;
        //        //lock (connClients)
        //        //{
        //        //    connClients.Add(conn);
        //        //}
        //        //                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "Client Accepted : " + conn.socket.NoDelay);
        //        //string m1 = conn.socket.Client.LocalEndPoint.ToString();
        //        //string m2 = conn.socket.Client.RemoteEndPoint.ToString();
        //        //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "local : " + m1 + ", remote : " + m2);
        //        conn.stream.BeginRead(conn.buffer, 0, (conn.buffer.Length - 1), new AsyncCallback(ReadCallback), conn);
        //        //listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), listener);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
        //        Debug.WriteLine(ex);
        //    }
        //}

        //public void ServerClose()
        //{
        //    //bDoingCheckPartnerThread = false;
        //    DoingAcceptThread = false;
        //    try
        //    {
        //        //if (connClients.Count() > 0)
        //        //{
        //        //    lock (connClients)
        //        //    {
        //        //        foreach (ConnectedClient cc in connClients)
        //        //        {
        //        //            if(cc.socket.Connected)
        //        //                cc.socket.Close();
        //        //        }
        //        //    }
        //        //}

        //        if (connClients.socket.Connected)
        //        {
        //            connClients.socket.GetStream().Close();
        //            connClients.socket.Close();
        //        }

        //        if (listener != null)
        //            listener.Stop();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex);
        //    }
        //}

        //public async Task<int> SendMessage(ITNTSendArgs msg)
        //{
        //    string className = "VisionServer";
        //    string funcName = "SendMessage";

        //    int retval = 0;
        //    int sendleng = 0;
        //    byte[] sendbuff;
        //    int i = 0;
        //    string smsg = "";
        //    try
        //    {
        //        if ((connClients.socket == null) || !connClients.socket.Connected)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SendMessage Error : (connClients.socket == null) || !connClients.socket.Connected");
        //            return -1;
        //        }
        //        if (connClients.stream == null)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SendMessage Error : connClients.stream == null");
        //            return -2;
        //        }

        //        if (msg.sendString.Length > 0)
        //        {
        //            sendleng = msg.sendString.Length;
        //            sendbuff = new byte[sendleng + 4];
        //            byte[] tmp = new byte[sendleng];
        //            tmp = Encoding.UTF8.GetBytes(msg.sendString);

        //            sendbuff[i++] = (byte)ASCII.SOH;
        //            sendbuff[i++] = (byte)ASCII.STX;
        //            Array.Copy(tmp, 0, sendbuff, i, sendleng);
        //            i += sendleng;
        //            sendbuff[i++] = (byte)ASCII.ETX;
        //            sendbuff[i++] = (byte)ASCII.CR;
        //            sendleng += 4;
        //            smsg = msg.sendString;
        //        }
        //        else
        //        {
        //            sendleng = msg.dataSize;
        //            sendbuff = new byte[sendleng];
        //            Array.Copy(msg.sendBuffer, sendbuff, sendleng);
        //            smsg = Encoding.UTF8.GetString(sendbuff, 0, sendleng);
        //        }

        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SendMessage Before : " + smsg);
        //        await connClients.stream.WriteAsync(sendbuff, 0, sendleng);
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SendMessage After : " + smsg);
        //    }
        //    catch (Exception ex)
        //    {
        //        retval = ex.HResult;
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
        //    }

        //    return retval;
        //}

        //public async Task<ITNTResponseArgs> SendCommand(ITNTSendArgs arg)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    ITNTSendArgs sendData = new ITNTSendArgs();
        //    int length = arg.sendString.Length;
        //    byte[] buff = new byte[length];
        //    int idx = 0;
        //    string className = "VisionServer";
        //    string funcName = "SendCommand";
        //    string msg = "";

        //    try
        //    {
        //        sendData.sendBuffer[idx++] = (byte)ASCII.SOH;
        //        sendData.sendBuffer[idx++] = (byte)ASCII.STX;
        //        buff = Encoding.UTF8.GetBytes(arg.AddrString);
        //        Array.Copy(buff, 0, sendData.sendBuffer, idx, length);
        //        idx += length;
        //        sendData.sendBuffer[idx++] = (byte)ASCII.ETX;
        //        sendData.sendBuffer[idx++] = (byte)ASCII.CR;
        //        msg = Encoding.UTF8.GetString(sendData.sendBuffer);
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SendMessage Before : " + msg);
        //        await connClients.stream.WriteAsync(sendData.sendBuffer, 0, length + 4);
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "SendMessage After : " + msg);
        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
        //        return retval;
        //    }
        //}

        //public async Task<ITNTResponseArgs> ExecuteCommand(ITNTSendArgs msg)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        retval.execResult = ex.HResult;
        //    }
        //    return retval;
        //}

        //protected void AnalyzerReceivedData(byte[] data, int size)
        //{
        //    string msg;
        //    try
        //    {
        //        if (size < 4)
        //            return;
        //        byte[] temp = new byte[size - 4];
        //        Array.Copy(data, 2, temp, 0, size - 4);
        //        msg = Encoding.UTF8.GetString(temp);
        //        ITNTResponseArgs arg = new ITNTResponseArgs();
        //        arg.recvString = msg;
        //        OnVisionDataArrivedEventHandlerFunc(arg);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        //protected void OnVisionDataArrivedEventHandlerFunc(ITNTResponseArgs e)
        //{
        //    VisionDataArrivedEventHandler handler = DataArrivedEventFunc;
        //    if (handler != null)
        //        handler(this, e);
        //}

        //protected void ReadCallback(IAsyncResult result)
        //{
        //    string className = "VisionServer";
        //    string funcName = "ReadCallback";

        //    try
        //    {
        //        int retval = 0;
        //        //ConnectedClient conn = (ConnectedClient)result.AsyncState;
        //        connClients = (ConnectedClient)result.AsyncState;
        //        byte[] temp;
        //        byte data = 0;
        //        //int idx = 0;
        //        int getsize = 0;
        //        bool stxFlag = false;

        //        int readCount = connClients.stream.EndRead(result);
        //        if (readCount > 0)
        //        {
        //            retval = connClients.RecvBuff.Put(connClients.buffer, readCount);
        //            if (retval > 0)
        //            {
        //                getsize = connClients.RecvBuff.GetSize();
        //                temp = new byte[getsize];
        //                retval = connClients.RecvBuff.Look(ref temp, getsize);
        //                if (retval >= 1)
        //                {
        //                    for (int i = 0; i < getsize; i++)
        //                    {
        //                        if (temp[i] == (byte)ASCII.SOH)
        //                        {
        //                            recvFlag = VISION_RECV_STATUS.VISION_RECV_SOH;
        //                        }
        //                        //else if ((temp[i] == (byte)ASCII.STX) && (recvFlag == VISION_RECV_STATUS.VISION_RECV_SOH))
        //                        else if (temp[i] == (byte)ASCII.STX)
        //                        {
        //                            stxFlag = true;
        //                            recvFlag = VISION_RECV_STATUS.VISION_RECV_STX;
        //                        }
        //                        //else if ((temp[i] == (byte)ASCII.ETX) && (recvFlag == VISION_RECV_STATUS.VISION_RECV_STX))
        //                        else if ((temp[i] == (byte)ASCII.ETX) && stxFlag)
        //                        {
        //                            recvFlag = VISION_RECV_STATUS.VISION_RECV_ETX;
        //                        }
        //                        //else if ((temp[i] == (byte)ASCII.CR) && (recvFlag == VISION_RECV_STATUS.VISION_RECV_ETX))
        //                        else if ((temp[i] == (byte)ASCII.CR) && stxFlag)
        //                        {
        //                            recvFlag = VISION_RECV_STATUS.VISION_RECV_CRT;
        //                            retval = connClients.RecvBuff.Get(ref temp, i + 1);
        //                            AnalyzerReceivedData(temp, i + 1);
        //                            stxFlag = false;
        //                        }
        //                        else
        //                        {
        //                            if (!stxFlag)
        //                                connClients.RecvBuff.Get(ref data);
        //                            //if (recvFlag != VISION_RECV_STATUS.VISION_RECV_STX)
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "PUT BUFFER FAIL : " + retval.ToString());

        //            connClients.stream.BeginRead(connClients.buffer, 0, (connClients.buffer.Length - 1), new AsyncCallback(ReadCallback), connClients);
        //        }
        //        else
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, "receive count = 0");

        //            //if (connClients.socket.Connected)
        //            //    connClients.socket.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
        //        Debug.WriteLine(ex);
        //    }
        //}

        //public static void SetTcpKeepAlive(Socket socket, uint keepaliveTime, uint keepaliveInterval)
        //{
        //    /* the native structure
        //    struct tcp_keepalive {
        //    ULONG onoff;
        //    ULONG keepalivetime;
        //    ULONG keepaliveinterval;
        //    };
        //    */

        //    // marshal the equivalent of the native structure into a byte array
        //    uint dummy = 0;
        //    byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
        //    BitConverter.GetBytes((uint)(keepaliveTime)).CopyTo(inOptionValues, 0);
        //    BitConverter.GetBytes((uint)keepaliveTime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
        //    BitConverter.GetBytes((uint)keepaliveInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

        //    // write SIO_VALS to Socket IOControl
        //    socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        //}
    }
}
