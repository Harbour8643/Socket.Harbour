using SocketLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClientTest
{
    class Program
    {
        static Client client;
        static void Main(string[] args)
        {
            client = new Client("192.168.3.150", 8088);//此处输入自己的计算机IP地址，端口不能改变
            client.MessageReceived += _client_MessageReceived;
            client.MessageSent += client_MessageSent;
            client.StartClient();
            while (true)
            {
                System.Threading.Thread.Sleep(200);
                sendMsg();
            }
        }

        private static void client_MessageSent(object sender, SocketBase.MessageEventArgs e)
        {
            Console.WriteLine(e.Connecction.ConnectionName + "发送成功");
        }
        private static void _client_MessageReceived(object sender, SocketBase.MessageEventArgs e)
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
            else
            {
                Console.WriteLine("发送失败！");
            }
        }
    }
}
