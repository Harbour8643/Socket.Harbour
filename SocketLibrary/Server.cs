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
        private TcpListener _listener;
        private IPAddress ipAddress;
        private int port;

        /// <summary>
        /// 默监听所有IP的此端口
        /// </summary>
        /// <param name="port">端口号</param>
        public Server(int port)
        {
            this.ipAddress = IPAddress.Any;
            this.port = port;
        }
        /// <summary>
        /// 监听此IP的此端口
        /// </summary>
        /// <param name="ip">IP</param>
        /// <param name="port">端口号</param>
        public Server(string ip, int port)
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

                    foreach (var keyValue in this.Connections)
                    {
                        this.Receive(keyValue.Value);//接收数据

                        //判断是否存活，20s没有更新就认为没有存活
                        double timSpan = (DateTime.Now - keyValue.Value.LastConnTime).TotalSeconds;
                        if (timSpan > 2)
                        {
                            Connection remConn;
                            this.Connections.TryRemove(keyValue.Key, out remConn);

                            ConCloseMessagesEventArgs ce = new ConCloseMessagesEventArgs(keyValue.Value.ConnectionName,
                                new ConcurrentQueue<Message>(keyValue.Value.messageQueue), new Exception("长时间未更新存活时间"));
                            this.OnConnectionClose(this, ce);
                            continue;
                        }

                        this.Send(keyValue.Value); //发送数据
                    }

                    Thread.Sleep(200);
                }
            }
            catch(Exception ex)
            {
            }
        }

        /// <summary>
        /// 关闭监听
        /// </summary>
        public void StopServer()
        {
            _listenConnection.Abort();
            this.EndListenAndSend();
        }
    }
}
