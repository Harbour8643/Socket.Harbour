using SocketLibrary;
using System;

namespace SocketClientTest
{
    class Program
    {
        static string ip = "192.168.20.210";
        static Client client;
        static void Main(string[] args)
        {
            client = new Client(ip, 4444);//此处输入自己的计算机IP地址，端口不能改变
            client.MessageReceived += _client_MessageReceived;
            client.MessageSent += client_MessageSent;
            client.ConnectionClose += Client_ConnectionClose;
            client.Connected += Client_Connected;
            client.StartClient();
            while (true)
            {
                System.Threading.Thread.Sleep(200);
                sendMsg();
            }
        }

        private static void Client_Connected(object sender, Connection e)
        {
            Console.WriteLine(e.ConnectionName + "连接成功");
        }

        private static void Client_ConnectionClose(object sender, ConCloseMessagesEventArgs e)
        {
            Console.WriteLine(e.ConnectionName + "连接关闭");
        }

        private static void client_MessageSent(object sender, MessageEventArgs e)
        {
            Console.WriteLine(e.Connecction.ConnectionName + "发送成功");
        }
        private static void _client_MessageReceived(object sender, MessageEventArgs e)
        {
            string msg = e.Message.MessageBody;

            Console.WriteLine(e.Connecction.ConnectionName + msg + ":发送成功");
        }
        private static void sendMsg()
        {
            Connection connection = client.GetConnection();
            if (connection != null)
            {
                connection.SendMsg("消息体");
            }
        }
    }
}
