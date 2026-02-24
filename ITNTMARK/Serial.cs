using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using ITNTUTIL;
using System.Diagnostics;

namespace ITNTCOMMM
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
    //    RECVFLAG_RECV_ERR = 0xff,
    //}

    //enum SENDFLAG
    //{
    //    SENDFLAG_IDLE = 0,
    //    SENDFLAG_SEND_REQ = 1,
    //    SENDFLAG_SEND_END = 2,
    //    SENDFLAG_SEND_ERR = 3,
    //}

    //enum COMPORTERROR
    //{
    //    ERR_NO_ERROR = 0,
    //    ERR_SEND_DATA_FAIL = -0x0001,
    //    ERR_RECV_ACK_TIMEOUT = -0x0002,
    //    ERR_RECV_RESPONSE_TIMEOUT = -0x0003,
    //    ERR_BUFFER_SIZE_ERROR = -0x0004,
    //    ERR_RECV_BCC_ERROR = -0x0005,
    //    ERR_CMD_MISSMATCH = -0x0006,
    //    ERR_PORT_NOT_OPENED = -0x0007,
    //    ERR_RECV_NAK = -0x0008,
    //    ERR_COMMAND_BUSY = -0x0009,
    //    ERR_DOING_COMMAND = -0x000A,
    //    ERR_NOT_READY = -0x000B,
    //}

    public class Serial
    {
        #region properties
        public const int ERR_SEND_DATA_FAIL = 0x0001;
        public const int ERR_RECV_ACK_TIMEOUT = 0x0002;
        public const int ERR_RECV_RESPONSE_TIMEOUT = 0x0003;
        public const int ERR_BUFFER_SIZE_ERROR = 0x0004;
        public const int ERR_RECV_BCC_ERROR = 0x0005;
        public const int ERR_CMD_MISSMATCH = 0x0006;
        public const int ERR_PORT_NOT_OPENED = 0x0007;
        public const int ERR_RECV_NAK = 0x0008;
        public const int ERR_COMMAD_BUSY = 0x0009;

        protected const byte SOH = 0x01;   //SOH Start of Header 헤더 시작
        protected const byte STX = 0x02;   //STX Start of Text   본문 시작, 헤더 종료
        protected const byte ETX = 0x03;   //ETX End of Text 본문 종료
        protected const byte EOT = 0x04;   //EOT End of Transmission 전송 종료, 데이터 링크 초기화
        protected const byte ENQ = 0x05;   //ENQ Enquiry 응답 요구
        protected const byte ACK = 0x06;   //ACK Acknowledgment  긍정응답
        protected const byte BEL = 0x07;   //Carrige Return  
        protected const byte LF = 0x0A;   //Carrige Return  
        protected const byte CR = 0x13;   //Carrige Return  
        protected const byte NAK = 0x15;   //NACK Acknowledgment  

        protected const byte SENDFLAG_IDLE = 0;
        protected const byte SENDFLAG_SEND_REQ = 1;
        protected const byte SENDFLAG_SEND_END = 2;
        protected const byte SENDFLAG_SEND_ERR = 3;

        protected const byte RECVFLAG_IDLE = 0;
        //const byte RECVFLAG_REQ_ACK = 1;
        protected const byte RECVFLAG_RECV_REQ = 1;
        protected const byte RECVFLAG_RECV_SOH = 2;
        protected const byte RECVFLAG_RECV_STX = 3;
        protected const byte RECVFLAG_RECV_ACK = 4;
        protected const byte RECVFLAG_RECV_NAK = 5;
        protected const byte RECVFLAG_RECV_ETX = 6;
        protected const byte RECVFLAG_RECV_END = 7;
        protected const byte RECVFLAG_RECV_ERR = 0xff;

        protected byte SendFlag = SENDFLAG_IDLE;
        protected byte RecvFlag = RECVFLAG_IDLE;

        protected static SerialPort Port = new SerialPort();
        protected RingBuffer cb;
        private readonly object comLock = new object();
        private object thisLock = new object();
        #endregion

        public bool IsOpen
        {
            get { return Port.IsOpen; }
        }


        public Serial()
        {
            Port = new SerialPort();
            //cb = new CircularBuffer<byte>(2048);
            cb = new RingBuffer(4096);
        }

        ~Serial()
        {
            if ((Port != null) && (Port.IsOpen))
                ClosePort();
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
                {
                    //Port.DataReceived += new SerialDataReceivedEventHandler(OnDataReceivedHandler);
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

        public void OnDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] recv = new byte[2048];
            byte[] tmp = new byte[2048];
            int readsize = 0;
            int totsize = 0;
            lock (thisLock)
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

        virtual public void ReceiveCommData()
        {
            Debug.WriteLine("DDDDD");
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

        protected int WritePort(byte[] buffer, int offset, int count)
        {
            if (!Port.IsOpen)
                return ERR_PORT_NOT_OPENED;
            lock (comLock)
            {
                Port.Write(buffer, offset, count);
            }
            if (Port.BytesToWrite > 0)
            {
                return ERR_SEND_DATA_FAIL;
            }
            return count;
        }

        protected int WriteString(string buffer)
        {
            if (!Port.IsOpen)
                return ERR_PORT_NOT_OPENED;
            lock (comLock)
            {
                Port.Write(buffer);
            }
            if (Port.BytesToWrite > 0)
            {
                return ERR_SEND_DATA_FAIL;
            }
            return 0;
        }

        protected int ReadPort(int readsize, ref int realsize, ref byte[] buffer)
        {
            lock (comLock)
            {
                if (!Port.IsOpen)
                    return ERR_PORT_NOT_OPENED;

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
    }
}
