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
using System.Runtime.InteropServices;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    enum VISION_RECV_STATUS
    {
        VISION_RECV_NORMAL = 0,
        VISION_RECV_SOH = 1,
        VISION_RECV_STX = 2,
        VISION_RECV_ETX = 3,
        VISION_RECV_CRT = 4,
    }

    public class VisionServer// : ITNTTCPServer
    {
        public event VisionDataArrivedEventHandler DataArrivedEventFunc;

        //const byte VISION_COMM_RECV_FLAG
        private VISION_RECV_STATUS recvFlag = VISION_RECV_STATUS.VISION_RECV_NORMAL;
        private byte sendFlag = 0;
        //private ConnectedClient connectList = new ConnectedClient();

        public event ServerReceivedEventHandler receivedEvent = null;
        public event ServerStatusChangedEventHandler statusEvent = null;

        private TcpListener listener = null;
        private bool DoingAcceptThread = false;

        Int64 count = 0;
        //List<ConnectedClient> connClients = new List<ConnectedClient>();
        protected ConnectedClient connClients = new ConnectedClient();
        protected const int RECV_FRAME_LENGTH = 313;

        //Thread acceptThread;

        public VisionServer()
        {

        }

        void OnServerStatusChanged(ServerStatusChangedEventArgs e)
        {
            if (statusEvent != null)
                statusEvent(this, e);
        }


        public async Task<int> StartServer(IPAddress serverIP, int serverPort)
        {
            try
            {
#if TEST_DEBUG_VISION
                //Action act = AcceptClientThread;

                DoingAcceptThread = true;
                //Task task = new Task(act);
                //task.Start();
                //TcpClient tc = await listener.AcceptTcpClientAsync();
                acceptThread = new Thread(AcceptClientThread);
                //DoingAcceptThread = true;
                acceptThread.IsBackground = true;
                acceptThread.Start();

                // 새 쓰레드에서 처리
                //await Task.Factory.StartNew(AsyncTcpProcess, tc);
#else
                //IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
                //IPEndPoint serverAddress = new IPEndPoint(, serverPort);
                listener = new TcpListener(IPAddress.Any, serverPort);
                //this.serverIP = serverIP;
                //this.serverPort = serverPort;
                listener.Start();

                //Action act = AcceptClientThread;

                //DoingAcceptThread = true;
                //Task task = new Task(act);
                //task.Start();
                //TcpClient tc = await listener.AcceptTcpClientAsync();
                listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), listener);
                //Thread acceptThread = new Thread(new ParameterizedThreadStart(AcceptClientThread));
                //DoingAcceptThread = true;
                //acceptThread.IsBackground = true;
                //acceptThread.Start(listener);

                // 새 쓰레드에서 처리
                //await Task.Factory.StartNew(AsyncTcpProcess, tc);
#endif
            }
            catch (SocketException se)
            {
                Debug.WriteLine("StartServer" + se.HResult.ToString() + " : " + se.Message);
                return -2;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StartServer" + ex.HResult.ToString() + " : " + ex.Message);
                return -1;
            }

            return 0;
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
            count = 0;
            //connClients.Clear();

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
        private async void AcceptCallback(IAsyncResult ar)
        {
            string className = "VisionServer";
            string funcName = "AcceptCallback";
            try
            {
                ConnectedClient conn = new ConnectedClient();
                conn.socket = listener.EndAcceptTcpClient(ar);
                SetTcpKeepAlive(conn.socket.Client, 1000, 5);
                conn.socket.NoDelay = true;
                conn.Id = count;
                conn.stream = conn.socket.GetStream();
                count++;
                connClients = conn;
                //lock (connClients)
                //{
                //    connClients.Add(conn);
                //}
//                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Client Accepted : " + conn.socket.NoDelay);
                //string m1 = conn.socket.Client.LocalEndPoint.ToString();
                //string m2 = conn.socket.Client.RemoteEndPoint.ToString();
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "local : " + m1 + ", remote : " + m2);
                conn.stream.BeginRead(conn.buffer, 0, (conn.buffer.Length - 1), new AsyncCallback(ReadCallback), conn);
                //listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Debug.WriteLine(ex);
            }
        }

        public void ServerClose()
        {
            //bDoingCheckPartnerThread = false;
            DoingAcceptThread = false;
            try
            {
                //if (connClients.Count() > 0)
                //{
                //    lock (connClients)
                //    {
                //        foreach (ConnectedClient cc in connClients)
                //        {
                //            if(cc.socket.Connected)
                //                cc.socket.Close();
                //        }
                //    }
                //}

                if (connClients.socket.Connected)
                {
                    connClients.socket.GetStream().Close();
                    connClients.socket.Close();
                }

                if (listener != null)
                    listener.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
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
                if ((connClients.socket == null) || !connClients.socket.Connected)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage Error : (connClients.socket == null) || !connClients.socket.Connected", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }
                if (connClients.stream == null)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage Error : connClients.stream == null", Thread.CurrentThread.ManagedThreadId);
                    return -2;
                }

                if(msg.sendString.Length > 0)
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
                await connClients.stream.WriteAsync(sendbuff, 0, sendleng);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage After : " + smsg, Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)    
            {
                retval = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> SendCommand(ITNTSendArgs arg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            ITNTSendArgs sendData = new ITNTSendArgs();
            int length = arg.sendString.Length;
            byte[] buff = new byte[length];
            int idx = 0;
            string className = "VisionServer";
            string funcName = "SendCommand";
            string msg = "";

            try
            {
                sendData.sendBuffer[idx++] = (byte)ASCII.SOH;
                sendData.sendBuffer[idx++] = (byte)ASCII.STX;
                buff = Encoding.UTF8.GetBytes(arg.AddrString);
                Array.Copy(buff, 0, sendData.sendBuffer, idx, length);
                idx += length;
                sendData.sendBuffer[idx++] = (byte)ASCII.ETX;
                sendData.sendBuffer[idx++] = (byte)ASCII.CR;
                msg = Encoding.UTF8.GetString(sendData.sendBuffer);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage Before : " + msg, Thread.CurrentThread.ManagedThreadId);
                await connClients.stream.WriteAsync(sendData.sendBuffer, 0, length + 4);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SendMessage After : " + msg, Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        public async Task<ITNTResponseArgs> ExecuteCommand(ITNTSendArgs msg)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {

            }
            catch(Exception ex)
            {
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        protected void AnalyzerReceivedData(byte[] data, int size)
        {
            string msg;
            try
            {
                if (size < 4)
                    return;
                byte[] temp = new byte[size - 4];
                Array.Copy(data, 2, temp, 0, size - 4);
                msg = Encoding.UTF8.GetString(temp, 0, size - 4);
                ITNTResponseArgs arg = new ITNTResponseArgs();
                arg.recvString = msg;
                OnVisionDataArrivedEventHandlerFunc(arg);
            }
            catch (Exception ex)
            {

            }
        }

        protected void OnVisionDataArrivedEventHandlerFunc(ITNTResponseArgs e)
        {
            VisionDataArrivedEventHandler handler = DataArrivedEventFunc;
            if (handler != null)
                handler(this, e);
        }

        protected void ReadCallback(IAsyncResult result)
        {
            string className = "VisionServer";
            string funcName = "ReadCallback";
            byte[] temp;
            byte data = 0;
            //int idx = 0;
            int getsize = 0;
            bool stxFlag = false;
            int retval = 0;
            int readCount = 0;

            try
            {
                //ConnectedClient conn = (ConnectedClient)result.AsyncState;
                connClients = (ConnectedClient)result.AsyncState;

                readCount = connClients.stream.EndRead(result);
                if (readCount > 0)
                {
                    retval = connClients.RecvBuff.Put(connClients.buffer, readCount);
                    if (retval > 0)
                    {
                        getsize = connClients.RecvBuff.GetSize();
                        temp = new byte[getsize];
                        retval = connClients.RecvBuff.Look(ref temp, getsize);
                        if(retval >= 1)
                        {
                            for (int i = 0; i < getsize; i++)
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
                                    retval = connClients.RecvBuff.Get(ref temp, i+1);
                                    AnalyzerReceivedData(temp, i+1);
                                    stxFlag = false;
                                }
                                else
                                {
                                    if(!stxFlag)
                                        connClients.RecvBuff.Get(ref data);
                                    //if (recvFlag != VISION_RECV_STATUS.VISION_RECV_STX)
                                }
                            }
                        }
                    }
                    else
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PUT BUFFER FAIL : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);

                    connClients.stream.BeginRead(connClients.buffer, 0, (connClients.buffer.Length - 1), new AsyncCallback(ReadCallback), connClients);
                }
                else
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "receive count = 0", Thread.CurrentThread.ManagedThreadId);

                    //if (connClients.socket.Connected)
                    //    connClients.socket.Close();
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                Debug.WriteLine(ex);
            }
        }

        public static void SetTcpKeepAlive(Socket socket, uint keepaliveTime, uint keepaliveInterval)
        {
            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            */

            // marshal the equivalent of the native structure into a byte array
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(keepaliveTime)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepaliveTime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)keepaliveInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

            // write SIO_VALS to Socket IOControl
            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }
    }
}
