using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketLibrary
{
    /// <summary>
    /// Socket服务端
    /// </summary>
    public class Server : SocketBase
    {
        private const bool _isSendHeartbeat = false;

        private TcpListener _listener;
        private IPAddress ipAddress;
        private int port;

        /// <summary>
        /// 默监听所有IP的此端口
        /// </summary>
        /// <param name="port">端口号</param>
        public Server(int port) : base(_isSendHeartbeat)
        {
            this.ipAddress = IPAddress.Any;
            this.port = port;
        }
        /// <summary>
        /// 监听此IP的此端口
        /// </summary>
        /// <param name="ip">IP</param>
        /// <param name="port">端口号</param>
        public Server(string ip, int port) : base(_isSendHeartbeat)
        {
            this.ipAddress = IPAddress.Parse(ip);
            this.port = port;
        }

        /// <summary>
        /// 获取连接集合
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, Connection> GetConnections()
        {
            return this.Connections;
        }

        /// <summary>
        /// 获取指定连接名的连接,查询不到返回null
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public Connection GetConnection(string connectionName)
        {
            Connection connection;
            this.Connections.TryGetValue(connectionName, out connection);
            return connection;
        }

        private Thread _listenConnection;
        /// <summary>
        /// 打开监听
        /// </summary>
        public void StartServer()
        {
            _listener = new TcpListener(this.ipAddress, this.port);
            _listener.Start();

            _listenConnection = new Thread(new ThreadStart(Start));
            _listenConnection.Start();
        }

        private void Start()
        {
            try
            {
                while (true)
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        string piEndPoint = client.Client.RemoteEndPoint.ToString();
                        Connection connection = new Connection(client, piEndPoint);
                        this.Connections.TryAdd(piEndPoint, connection);
                        this.OnConnected(this, connection);
                    }

                    //接收数据、发送数据 心跳检测
                    this.SenRecMsg();

                    Thread.Sleep(200);
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 关闭监听
        /// </summary>
        public void StopServer()
        {
            this.EndListenAndSend();
            _listenConnection.Abort();
        }
    }
}
