using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ITNTUTIL;
using ITNTCOMMON;

namespace ITNTMARK
{

    // 메인 Program은 Socket을 상속받고 클라이언트 Socket으로 사용한다.
    //class ITNTClientAsync : Socket
    //{
        //// 메시지는 개행으로 구분한다.
        //private static char CR = (char)0x0D;
        //private static char LF = (char)0x0A;
        //// 메시지를 모으기 위한 버퍼
        //private byte[] buffer = new byte[1024];
        //private StringBuilder sb = new StringBuilder();
        //public ITNTClientAsync() : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        //{
        //    // 접속한다.
        //    base.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10000), Connect, this);
        //    while (true)
        //    {
        //        // 콘솔로 부터 메시지를 받으면 서버로 보낸다.
        //        string k = Console.ReadLine();
        //        Send(k + "\r\n");
        //        // exit면 종료한다.
        //        if ("exit".Equals(k, StringComparison.OrdinalIgnoreCase))
        //        {
        //            break;
        //        }
        //    }
        //}
        //// 접속되면 호출된다.
        //private void Connect(IAsyncResult result)
        //{
        //    // 접속 대기를 끝낸다.
        //    base.EndConnect(result);
        //    // buffer로 메시지를 받고 Receive함수로 메시지가 올 때까지 대기한다.
        //    base.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Receive, this);
        //}
        //// 메시지가 오면 호출된다.
        //private void Receive(IAsyncResult result)
        //{
        //    if (Connected)
        //    {
        //        // EndReceive를 호출하여 데이터 사이즈를 받는다.
        //        // EndReceive는 대기를 끝내는 것이다.
        //        int size = this.EndReceive(result);
        //        // 데이터를 string으로 변환한다.
        //        sb.Append(Encoding.ASCII.GetString(buffer, 0, size));
        //        // 메시지의 끝이 이스케이프 \r\n와 >의 형태이면 클라이언트에 표시한다.
        //        if (sb.Length >= 3 && sb[sb.Length - 3] == CR && sb[sb.Length - 2] == LF && sb[sb.Length - 1] == '>')
        //        {
        //            // string으로 변환한다.
        //            string msg = sb.ToString();
        //            // 콘솔에 출력한다.
        //            Console.Write(msg);
        //            // 버퍼를 비운다.
        //            sb.Clear();
        //        }
        //        // buffer로 메시지를 받고 Receive함수로 메시지가 올 때까지 대기한다.
        //        base.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Receive, this);
        //    }
        //}
        //// Send도 비동기 식으로 만들 수 있는데.. 굳이 send는 그럴 필요가 없습니다.
        //// 메시지를 보내는 함수
        //private void Send(string msg)
        //{
        //    byte[] data = Encoding.ASCII.GetBytes(msg);
        //    //base.BeginSend(data, 0, data.Length, SocketFlags.None, Send, this);
        //    // Client로 메시지 전송
        //    Send(data, data.Length, SocketFlags.None);
        //}
        //// Send 비동기 식임.. 현재는 미사용.
        //private void Send(IAsyncResult result)
        //{
        //    // 접속이 연결되어 있으면...
        //    if (base.Connected)
        //    {
        //        base.EndSend(result);
        //    }
        //}
        //// 프로그램 시작 함수
        //static void Main(string[] args)
        //{
        //    new ITNTClientAsync();
        //}
    //}



    public class ITNTClientAsync
    {

        public ITNTClientAsync()
        {
        }

        public int ConnetcToServer(string ip, int port)
        {
            int retval = 0;
            IPAddress localAddr = IPAddress.Parse(ip);
            //base.BeginAccept(new IPEndPoint(IPAddress.Parse(ip), port), ConnectCallback, this);
            return retval;
        }

        private void ConnectCallback(IAsyncResult result)
        {

        }

    }
}
