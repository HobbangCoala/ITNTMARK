using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ITNTUTIL;
using ITNTCOMMON;
using ITNTCOMMM;
using System.Runtime.InteropServices;

namespace ITNTMARK
{
    public delegate void ClientDataArrivalHandler(string msg);
    public delegate void ClientConnectionHandler();

    class MESClient
    {
        StateObject asyncstate = null;

        private ClientDataArrivalHandler DataArrivalCallback;
        private ClientConnectionHandler ConnectionCallback;

        bool isConnected = false;
        string serverIP = "";
        int serverport = 0;
        RingBuffer rb = new RingBuffer(32 * 1024);

        public MESClient(ClientDataArrivalHandler DataCallback, ClientConnectionHandler ConnectCallback)
        {
            DataArrivalCallback = DataCallback;
            ConnectionCallback = ConnectCallback;

            asyncstate = new StateObject(32*1024);
            //rb = new RingBuffer(1024);
        }


        public void StartClient(string IP, int port)
        {
            string className = "ITNTClientAsync";
            string funcName = "StartClient";

            // Connect to a remote device.  
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.  
                asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                this.serverIP = IP;
                this.serverport = port;
                // Connect to the remote endpoint.  
                asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                //connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void CloseClient()
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
            string ClassName = "ITNTClientAsync";
            string FuncName = "ConnectCallback";

            try
            {
                //if (isConnected)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "callBack Connected already");
                //    return;
                //}

                asyncstate.workSocket = (Socket)ar.AsyncState;
                SetTcpKeepAlive(asyncstate.workSocket, 1000, 5);
                //if (!asyncstate.workSocket.Connected)
                //{
                //    //StartClient(IP, port);
                //    IPAddress ipAddress = IPAddress.Parse(IP);
                //    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                //    // Create a TCP/IP socket.  
                //    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "beginConnect again");
                //    asyncstate.workSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //    asyncstate.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), asyncstate.workSocket);
                //    return;
                //}
                // Complete the connection.  
                asyncstate.workSocket.EndConnect(ar);

                isConnected = true;
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "isConnected = true");
                Receive();
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "Recv() Start");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
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
                //byte[] bytebuff = new byte[32*1024];
                asyncstate.workSocket.BeginReceive(asyncstate.buffer, 0, asyncstate.buffer.Length, 0, new AsyncCallback(ReceiveCallback), asyncstate);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private void ReceiveCallback(IAsyncResult ar)
        {
            string ClassName = "ITNTClientAsync";
            string FuncName = "ReceiveCallback";
            string msg = "";
            try
            {
                StateObject so = (StateObject)ar.AsyncState;
                Socket s = so.workSocket;

                int read = s.EndReceive(ar);
                if (read > 0)
                {
                    //so.sb.Append(Encoding.ASCII.GetString(so.buffer, 0, read));
                    msg = Encoding.UTF8.GetString(so.buffer, 0, read);
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "RECV : " + msg);

                    rb.Put(so.buffer, read);
                }

                if (rb.GetSize() > 0)
                    AnalysisRecvData();

                s.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceiveCallback), so);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                Console.WriteLine(ex.ToString());
            }
        }


        private int AnalysisRecvData(byte[] recvMsg, int count)
        {
            string ClassName = "ITNTClientAsync";
            string FuncName = "AnalysisRecvData";

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
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "RECV : " + msg);
            DataArrivalCallback?.Invoke(msg);

            return 0;
        }

        private int AnalysisRecvData()
        {
            string ClassName = "ITNTClientAsync";
            string FuncName = "AnalysisRecvData";

            //int i = 0;
            //int etx = 0;
            //int stx = 0;
            //string msg = "";

            //for (i = 0; i < count; i++)
            //{
            //    if (recvMsg[i] == 0x02)
            //        stx = i;
            //    if (recvMsg[i] == 0x03)
            //        etx = i;
            //}

            //msg = Encoding.UTF8.GetString(recvMsg, stx + 1, Math.Max(0, (etx - stx - 1)));
            //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "RECV : " + msg);
            //DataArrivalCallback?.Invoke(msg);

            return 0;
        }

        private void Send(String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            // Begin sending the data to the remote device.  
            string ClassName = "ITNTClientAsync";
            string FuncName = "Send";
            string msg = "";

            try
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                msg = Encoding.UTF8.GetString(byteData);
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "SEND : " + msg);
                asyncstate.workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
            }
        }

        public void Send(byte[] data, int size)
        {
            // Convert the string data to byte data using ASCII encoding.  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);
            string ClassName = "ITNTClientAsync";
            string FuncName = "Send";
            string msg = "";

            // Begin sending the data to the remote device.  
            try
            {
                msg = Encoding.UTF8.GetString(data, 0, size);
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, "SEND2 : " + msg);
                asyncstate.workSocket.BeginSend(data, 0, size, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", ClassName, FuncName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
            }
        }

        public csConnStatus GetConnectionStatus()
        {
            return csConnStatus.Connected;
        }

        //public void Send(byte[] byteData, int size)
        //{
        //    // Convert the string data to byte data using ASCII encoding.  
        //    //byte[] byteData = Encoding.ASCII.GetBytes(data);

        //    // Begin sending the data to the remote device.  
        //    asyncstate.workSocket.BeginSend(byteData, 0, size, 0, new AsyncCallback(SendCallback), asyncstate.workSocket);
        //}

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                //sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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


    public class StateObject
    {
        // Client socket.  
        public Socket workSocket;
        // Size of receive buffer.  
        public int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer;
        // Received data string.  
        //public StringBuilder sb;

        public StateObject(int size = 0)
        {
            workSocket = null;
            if (size <= 0)
                BufferSize = 1024;
            else
                BufferSize = size;
            buffer = new byte[BufferSize];
            //sb = new StringBuilder();
        }
    }

}
