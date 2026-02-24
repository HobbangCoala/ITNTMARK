using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITNTCOMMON;
using ITNTUTIL;
//using SerialPortLib;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public class MESComm
    {
        //public event VisionDataArrivedEventHandler DataArrivedEventFunc;
        public event MESClientReceivedEventHandler receivedEvent = null;
        public event MESClientStatusChangedEventHandler ClientStatusChangedEvent = null;

        private bool DoingAcceptThread = false;
        RingBuffer m_RecvData;
        byte[] m_RecvBuff = new byte[1024];
        int m_RecvLength = 0;

        private Socket m_ServerSocket;
        private List<SocketList> m_ClientSocket;
        private byte[] szData;
        private Socket m_currSocket;

        private VISION_RECV_STATUS recvFlag = VISION_RECV_STATUS.VISION_RECV_NORMAL;

        public MESComm()
        {
            m_RecvData = new RingBuffer(1024);
            m_RecvBuff = new byte[1024];
            //m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            m_ClientSocket = new List<SocketList>();
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_currSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            recvFlag = VISION_RECV_STATUS.VISION_RECV_NORMAL;
        }

        public MESComm(int buffersize)
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

                if (m_currSocket.Connected == true)
                {
                    socketlist.m_socket = m_currSocket;
                    socketlist.m_socket.Disconnect(false);
                    socketlist.m_socket.Dispose();
                    socketlist.m_socket = null;
                    socketlist.m_connected = false;
                    m_ClientSocket.Remove(socketlist);

                    if (m_ServerSocket != null)
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

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (e.AcceptSocket == null)
                {
                    //e.AcceptSocket = null;
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
            SocketList socketlist = new SocketList();
            int iLeng = 0;

            try
            {
                //Socket ClientSocket = sender as Socket;
                socketlist.m_socket = sender as Socket;

                if (socketlist.m_socket == null)
                {
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

                    if (e.Buffer != null)
                    {
                        iLeng = m_RecvData.Put(e.Buffer, e.BytesTransferred);
                        //AnalyzeRecvData();
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
                    //m_currSocket.Disconnect(false);
                    //m_currSocket.Dispose();
                    m_currSocket = socketlist.m_socket;
                    socketlist.m_socket.Disconnect(false);
                    socketlist.m_socket.Dispose();
                    socketlist.m_socket = null;
                    socketlist.m_connected = false;
                    m_ClientSocket.Remove(socketlist);
                    if (m_ServerSocket == null)
                        m_ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    //m_ServerSocket.AcceptAsync();
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
                    m_ServerSocket.AcceptAsync(args);
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
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
            //ITNTResponseArgs respArg = new ITNTResponseArgs(1024);
            MESClientReceivedEventArgs recvArg = new MESClientReceivedEventArgs();

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
                            //respArg.recvString = msg;
                            //OnVisionDataArrivedEventHandlerFunc(respArg);
                            recvArg.recvMsg = msg;
                            OnVisionDataArrivedEventHandlerFunc(recvArg);
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
            catch (Exception ex)
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


        //protected void OnVisionDataArrivedEventHandlerFunc(ITNTResponseArgs e)
        //{
        //    //DataArrivedEventFunc?.Invoke(this, e);
        //    receivedEvent?.Invoke(this, e);
        //    //VisionDataArrivedEventHandler handler = DataArrivedEventFunc;
        //    //if (handler != null)
        //    //    handler(this, e);
        //}

        protected void OnVisionDataArrivedEventHandlerFunc(MESClientReceivedEventArgs e)
        {
            //DataArrivedEventFunc?.Invoke(this, e);
            receivedEvent?.Invoke(this, e);
            //VisionDataArrivedEventHandler handler = DataArrivedEventFunc;
            //if (handler != null)
            //    handler(this, e);
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


        //public void Send(byte[] sendBuff)
        //{
        //    lock (_lock)
        //    {
        //        _sendQueue.Enqueue(sendBuff);
        //        if (!_pending)
        //        {
        //            RegisterSend();
        //        }
        //    }
        //}


        //void RegisterSend()
        //{
        //    string className = "VisionServer";
        //    string funcName = "RegisterSend";

        //    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
        //    bool pending = false;

        //    try
        //    {
        //        _pending = true;
        //        byte[] buff = _sendQueue.Dequeue();
        //        args.SetBuffer(buff, 0, buff.Length);
        //        if((m_currSocket != null) && m_currSocket.Connected)
        //            pending = m_currSocket.SendAsync(args);
        //        if (!pending)
        //        {
        //            OnSendCompleted(null, args);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //}

        //void OnSendCompleted(object Sender, SocketAsyncEventArgs args)
        //{
        //    try
        //    {
        //        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        //        {
        //            lock (_lock)
        //            {
        //                if (_sendQueue.Count > 0)
        //                {
        //                    RegisterSend();
        //                }
        //                else
        //                {
        //                    _pending = false;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //}



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
    }
}
