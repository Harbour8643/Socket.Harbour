using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketLibrary
{
    /// <summary>
    /// Socket的客户端
    /// </summary>
    public class Client : SocketBase
    {
        //是否心跳检测
        private const bool _isSendHeartbeat = true;
        /// <summary>
        ///  超时时间
        /// </summary>
        /// 
        public const int CONNECTTIMEOUT = 10;

        private TcpClient client;
        private IPAddress ipAddress;
        private int port;
        private Thread _listenningClientThread;
        private string _clientName;
        /// <summary>
        /// 连接的Key
        /// </summary>
        public string ClientName
        {
            get { return _clientName; }
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="ipaddress">地址</param>
        /// <param name="port">端口</param>
        public Client(string ipaddress, int port)
            : this(IPAddress.Parse(ipaddress), port)
        {
        }
        /// <summary>
        ///初始化 
        /// </summary>
        /// <param name="ipaddress">地址</param>
        /// <param name="port">端口</param>
        public Client(IPAddress ipaddress, int port) : base(_isSendHeartbeat)
        {
            this.ipAddress = ipaddress;
            this.port = port;
            this._clientName = ipAddress + ":" + port;
        }

        /// <summary>
        /// 打开链接
        /// </summary>
        public void StartClient()
        {
            _listenningClientThread = new Thread(new ThreadStart(Start));
            _listenningClientThread.Start();
        }
        /// <summary>
        /// 关闭连接并释放资源
        /// </summary>
        public void StopClient()
        {
            this.EndListenAndSend();
            //缺少通知给服务端 自己主动关闭了
            _listenningClientThread.Abort();
        }
        /// <summary>
        /// 获取指定连接名的连接,查询不到返回null
        /// </summary>
        /// <returns></returns>
        public Connection GetConnection()
        {
            Connection connection = null;
            this.Connections.TryGetValue(this.ClientName, out connection);
            return connection;
        }

        private void Start()
        {
            while (true)
            {
                if (!this.Connections.ContainsKey(this._clientName))
                {
                    try
                    {
                        client = new TcpClient();
                        client.SendTimeout = CONNECTTIMEOUT;
                        client.ReceiveTimeout = CONNECTTIMEOUT;
                        client.Connect(ipAddress, port);
                        Connection conn = new Connection(client, this._clientName);
                        this.Connections.TryAdd(this._clientName, conn);
                        this.OnConnected(this, conn);
                    }
                    catch (Exception ex)
                    {
                        this.connClose(this._clientName, new List<Message>() { }, ex);
                    }
                }

                //接收数据、发送数据 心跳检测
                this.SenRecMsg();

                Thread.Sleep(1000);
            }
        }
    }
}
