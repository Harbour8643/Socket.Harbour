using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SocketLibrary
{
    /// <summary>
    /// Connection 的摘要说明。
    /// </summary>
    public class Connection
    {
        #region 属性

        /// <summary>
        ///  提供用于网络访问的基础数据流
        /// </summary>
        public NetworkStream NetworkStream
        {
            get { return _networkStream; }
            private set { _networkStream = value; }
        }
        private NetworkStream _networkStream;

        /// <summary>
        /// 连接的Key
        /// </summary>
        public string ConnectionName
        {
            get { return _connectionName; }
            private set { _connectionName = value; }
        }
        private string _connectionName;

        /// <summary>
        /// 连接别名，只有IP，没有端口号
        /// </summary>
        public string ConnectionNickName
        {
            get { return _connectionnickName; }
            set { _connectionnickName = value; }
        }
        private string _connectionnickName;

        /// <summary>
        /// 此链接的消息队列
        /// </summary>
        public ConcurrentQueue<Message> MessageQueue
        {
            get { return _messageQueue; }
            private set { _messageQueue = value; }
        }
        private ConcurrentQueue<Message> _messageQueue;

        /// <summary>
        /// TcpClient
        /// </summary>
        public TcpClient TcpClient
        {
            get { return _tcpClient; }
            private set { _tcpClient = value; }
        }
        private TcpClient _tcpClient;

        /// <summary>
        /// 最后连接通信时间
        /// </summary>
        public DateTime LastConnTime
        {
            get { return _lastConnTime; }
            set { _lastConnTime = value; }
        }
        private DateTime _lastConnTime;
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="tcpClient">已建立连接的TcpClient</param>
        /// <param name="connectionName">连接名</param>
        public Connection(TcpClient tcpClient, string connectionName)
        {
            this._tcpClient = tcpClient;
            this._connectionName = connectionName;
            this.ConnectionNickName = this._connectionName.Split(':')[0];
            this._networkStream = this._tcpClient.GetStream();
            this._messageQueue = new ConcurrentQueue<Message>();
        }

        /// <summary>
        /// 中断连接并释放资源
        /// </summary>
        public void Stop()
        {
            try
            {
                _tcpClient.Client.Disconnect(false);
                _networkStream.Close();
                _tcpClient.Close();
            }
            catch (Exception ex)
            { }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(string msg)
        {
            Message message = new Message();
            this._messageQueue.Enqueue(new Message(msg));
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(Message msg)
        {
            this._messageQueue.Enqueue(msg);
        }

    }
}
