using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCP_Client
{
    internal class Program
    {
        private static readonly byte[] Buffer = new byte[1024];

        private static void Main()
        {
            try
            {
                Uri uri = new Uri("http://127.0.0.1:9876");

                //①创建一个Socket
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //②连接到指定服务器的指定端口
                socket.Connect("127.0.0.1", 4433);
                WriteLine("Client: Connect to server success!", ConsoleColor.White);

                //③实现异步接受消息的方法 客户端不断监听消息
                socket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), socket);

                //④接受用户输入，将消息发送给服务器端
                while (true)
                {
                    var message = "zheis11111" + socket.RemoteEndPoint; //System.IO.File.ReadAllText("d:\\text.txt"); //Console.ReadLine();
                    if (message != null)
                    {
                        var outputBuffer = new Byte[1] ;//Encoding.UTF8.GetBytes(message);
                        socket.BeginSend(outputBuffer, 0, outputBuffer.Length, SocketFlags.None, null, null);
                    }
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                WriteLine("Client: Error " + ex.Message, ConsoleColor.Red);
            }
            finally
            {
                Console.Read();
            }
        }

        // 接收信息
        public static void ReceiveMessage(IAsyncResult ar)
        {
            try
            {
                var socket = ar.AsyncState as Socket;

                //方法参考：http://msdn.microsoft.com/zh-cn/library/system.net.sockets.socket.endreceive.aspx
                if (socket != null)
                {
                    var length = socket.EndReceive(ar);
                    var message = Encoding.ASCII.GetString(Buffer, 0, length);
                    WriteLine(message, ConsoleColor.White);
                }

                //接收下一个消息
                if (socket != null)
                {
                    socket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), socket);
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex.Message, ConsoleColor.Red);
            }
        }

        public static void WriteLine(string str, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("[{0:MM-dd HH:mm:ss}] {1}", DateTime.Now, str);
        }
    }
}
