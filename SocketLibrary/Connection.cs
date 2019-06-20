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
        public NetworkStream NetworkStream { get; private set; }

        /// <summary>
        /// 连接的Key
        /// </summary>
        public string ConnectionName { get; private set; }

        /// <summary>
        /// 连接别名，只有IP，没有端口号
        /// </summary>
        public string ConnectionNickName { get; set; }

        /// <summary>
        /// 此链接的消息队列
        /// </summary>
        public ConcurrentQueue<Message> MessageQueue { get; private set; }

        /// <summary>
        /// TcpClient
        /// </summary>
        public TcpClient TcpClient { get; private set; }

        /// <summary>
        /// 最后连接通信时间
        /// </summary>
        public DateTime LastConnTime { get; set; }
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="tcpClient">已建立连接的TcpClient</param>
        /// <param name="connectionName">连接名</param>
        public Connection(TcpClient tcpClient, string connectionName)
        {
            this.TcpClient = tcpClient;
            this.ConnectionName = connectionName;
            this.ConnectionNickName = this.ConnectionName.Split(':')[0];
            this.NetworkStream = this.TcpClient.GetStream();
            this.MessageQueue = new ConcurrentQueue<Message>();
        }

        /// <summary>
        /// 中断连接并释放资源
        /// </summary>
        public void Stop()
        {
            try
            {
                TcpClient.Client.Disconnect(false);
                NetworkStream.Close();
                TcpClient.Close();
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
            this.MessageQueue.Enqueue(new Message(msg));
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(Message msg)
        {
            this.MessageQueue.Enqueue(msg);
        }

    }
}
