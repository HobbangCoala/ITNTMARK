using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTCOMMM;
using ITNTCOMMON;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
//using ITNTCOMMON;
using ITNTUTIL;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public class MESServer// : ITNTTCPServer
    {
        public event ServerReceivedEventHandler receivedEvent = null;
        public event ServerStatusChangedEventHandler statusEvent = null;

        private TcpListener listener = null;
        private bool DoingAcceptThread = false;

        Int64 count = 0;
        //List<ConnectedClient> connClients = new List<ConnectedClient>();
        protected ConnectedClient connClients = new ConnectedClient();
        protected const int RECV_FRAME_LENGTH = 315;

        Thread acceptThread;

        public MESServer()
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
#if TEST_DEBUG_MES
                //Action act = AcceptClientThread;

                //DoingAcceptThread = true;
                //Task task = new Task(act);
                //task.Start();
                //TcpClient tc = await listener.AcceptTcpClientAsync();
                acceptThread = new Thread(new ParameterizedThreadStart(AcceptClientThread));
                DoingAcceptThread = true;
                //acceptThread.IsBackground = true;
                //acceptThread.Start(listener);

                // 새 쓰레드에서 처리
                //await Task.Factory.StartNew(AsyncTcpProcess, tc);
#else
                listener = new TcpListener(IPAddress.Any, serverPort);
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

        public int InitializeServer()
        {
            int retval = 0;
            count = 0;
            //connClients.Clear();

            return retval;
        }

        private async void AcceptCallback(IAsyncResult result)
        {
            try
            {
                ConnectedClient conn = new ConnectedClient();
                conn.socket = listener.EndAcceptTcpClient(result);
                conn.Id = count;
                conn.stream = conn.socket.GetStream();
                count++;
                connClients = conn;
                //lock (connClients)
                //{
                //    connClients.Add(conn);
                //}

                conn.stream.BeginRead(conn.buffer, 0, (conn.buffer.Length - 1), new AsyncCallback(ReadCallback), conn);
                listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        //        async void AcceptClientThread()
        async void AcceptClientThread(object obj)
        {
            string value = "";
            try
            {
                while (DoingAcceptThread)
                {

                    await Task.Delay(100);
                }
                //Util.GetPrivateProfileValue("MES", "VISION", "V0O", ref value, "TEST.ini");
                //retval.recvString = value;
                //retval.recvBuffer = Encoding.UTF8.GetBytes(retval.recvString);
                //retval.recvSize = 4;

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
                    connClients.socket.Close();
                }

                if (listener != null)
                    listener.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            //if (threadServerRcv != null && threadServerRcv.IsAlive)
            //{
            //    threadServerRcv.Abort();
            //    threadServerRcv.Join();
            //}

            //if (threadChkPartnerDeath != null && threadChkPartnerDeath.IsAlive)
            //{
            //    threadChkPartnerDeath.Abort();
            //    threadChkPartnerDeath.Join();
            //}


            //if (clientForServer != null)
            //{
            //    //clientForServer.shutdown;
            //    clientForServer.Close();
            //}

            //serverStatus = csConnStatus.Closed;
            //OnConnectionStatusChanged(serverStatus);
        }









        public void SendCommand(ServerReceivedEventArgs arg)
        {
            try
            {
                connClients.stream.Write(arg.recvBuffer, 0, arg.recvSize);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MESServer", "SendCommand", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return;
            }
        }












        public int OnServerReceived(ServerReceivedEventArgs e)
        {
            if (receivedEvent != null)
                return receivedEvent(this, e);
            else
                return 0;
        }

        protected void ReadCallback(IAsyncResult result)
        {
            int retval = 0;
            ConnectedClient conn = (ConnectedClient)result.AsyncState;
            try
            {
                //ConnectedClient conn = null;
                //foreach (ConnectedClient cc in connClients)
                //{
                //    if(cc.Id == conCL.Id)
                //    {
                //        conn = cc;
                //        break;
                //    }
                //}

                //if(conn == null)
                //{
                //    lock(connClients)
                //    {
                //        conn.Id = count;
                //        connClients.Add(conn);
                //        count++;
                //    }
                //}

                int readCount = conn.stream.EndRead(result);
                if (readCount > 0)
                {
                    retval = conn.RecvBuff.Put(conn.buffer, readCount);
                    if (retval > 0)
                    {
                        if (conn.RecvBuff.GetSize() >= RECV_FRAME_LENGTH)
                        {
                            byte[] sendbuff = new byte[RECV_FRAME_LENGTH];
                            retval = conn.RecvBuff.Get(ref sendbuff, RECV_FRAME_LENGTH);
                            if (retval > 0)
                            {
                                //conn.stream.Write(sendbuff, 0, RECV_FRAME_LENGTH);
                                ServerReceivedEventArgs args = new ServerReceivedEventArgs(RECV_FRAME_LENGTH);
                                args.recvMsg = Encoding.ASCII.GetString(sendbuff);
                                Array.Copy(sendbuff, args.recvBuffer, RECV_FRAME_LENGTH);
                                args.recvSize = RECV_FRAME_LENGTH;
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ITNTTcpSever", "ReadCallback", "RECV DATA : " + args.recvMsg, Thread.CurrentThread.ManagedThreadId);

                                retval = OnServerReceived(args);
                            }
                            else
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ITNTTcpSever", "ReadCallback", "GET BUFFER FAIL : " + retval.ToString(), Thread.CurrentThread.ManagedThreadId);
                                //conn.RecvBuff.Clear();
                            }
                        }
                        else
                        {
                            //conn.RecvBuff.Clear();
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ITNTTcpSever", "ReadCallback", "SIZE < RECV_FRAME_LENGTH", Thread.CurrentThread.ManagedThreadId);
                        }
                    }
                    else
                    {
                        //conn.RecvBuff.Clear();
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ITNTTcpSever", "ReadCallback", "SIZE < RECV_FRAME_LENGTH", Thread.CurrentThread.ManagedThreadId);
                    }

                    conn.stream.BeginRead(conn.buffer, 0, (conn.buffer.Length - 1), new AsyncCallback(ReadCallback), conn);
                }
                else
                {
                    if(conn.socket.Connected)
                        conn.socket.Close();
                }

                //if (readCount >= RECV_FRAME_LENGTH)
                //{
                //    //conn.sp += readCount;
                //    conn.stream.Write(conn.buffer, 0, RECV_FRAME_LENGTH);
                //    ServerReceivedEventArgs args = new ServerReceivedEventArgs();
                //    args.recvMsg = Encoding.ASCII.GetString(conn.buffer, 0, RECV_FRAME_LENGTH);
                //    conn.sp -= RECV_FRAME_LENGTH;
                //    OnServerReceived(args);
                //    conn.stream.BeginRead(conn.buffer, conn.sp, conn.buffer.Length - 1 - conn.sp, new AsyncCallback(ReadCallback), conn);
                //}
                //else if(readCount > 0)
                //{
                //    conn.sp += readCount;
                //    if ((readCount + conn.sp) > RECV_FRAME_LENGTH)
                //    {
                //        conn.stream.Write(conn.buffer, 0, RECV_FRAME_LENGTH);
                //        ServerReceivedEventArgs args = new ServerReceivedEventArgs();
                //        args.recvMsg = Encoding.ASCII.GetString(conn.buffer, 0, RECV_FRAME_LENGTH);
                //        conn.sp -= RECV_FRAME_LENGTH;
                //    }
                //    conn.stream.BeginRead(conn.buffer, conn.sp, conn.buffer.Length - 1 - conn.sp, new AsyncCallback(ReadCallback), conn);
                //}
                //else
                //{
                //    lock(connClients)
                //    {
                //        if(connClients.Count > 0)
                //            connClients.Remove(conn);
                //    }
                //}
            }
            catch (Exception ex)
            {
                conn.RecvBuff.Clear();
                Debug.WriteLine(ex);
            }
        }
    }
}
