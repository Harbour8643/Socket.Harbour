using SocketLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServerTest
{
    class Program
    {
        static string ip = "192.168.32.180";
        static int i = 0;
        static Server _server;
        static void Main(string[] args)
        {
            _server = new Server(ip, 4444);
            _server.MessageReceived += _server_MessageReceived;
            _server.Connected += _server_Connected;
            _server.ConnectionClose += _server_ConnectionClose;
            _server.MessageSent += _server_MessageSent;
            _server.StartServer();
            while (true)
            {
                System.Threading.Thread.Sleep(200);
            }
        }

        static void _server_MessageSent(object sender, SocketBase.MessageEventArgs e)
        {
            Console.WriteLine(e.Connecction.ConnectionName + "服务端发送成功");
        }
        private static void _server_ConnectionClose(object sender, SocketBase.ConCloseMessagesEventArgs e)
        {
            Console.WriteLine(e.ConnectionName + "连接关闭");
        }
        private static void _server_Connected(object sender, Connection e)
        {
            Console.WriteLine(e.ConnectionName + "连接成功");
        }
        private static void _server_MessageReceived(object sender, SocketBase.MessageEventArgs e)
        {
            string ss = e.Message.MessageBody;
            Console.WriteLine(e.Connecction.ConnectionName + ss);
            //SendMsg();
        }

        private static void SendMsg()
        {
            i += 1;
            Connection connection = null;
            foreach (var keyValue in _server.GetConnections())
            {
                if (ip.Equals(keyValue.Value.NickName))
                {
                    connection = keyValue.Value;
                }
            }
            if (connection != null)
            {
                connection.SendMsg(i + "服务端发送消息体");
            }
            else
            {
                Console.WriteLine("发送失败！");
            }
        }

    }
}
