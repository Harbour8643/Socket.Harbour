using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace SocketLibrary
{
    /// <summary>
    /// SocketBase,Socket通信基类
    /// </summary>
    public abstract class SocketBase
    {
        /// <summary>
        /// 已连接的Socket
        /// </summary>
        protected ConcurrentDictionary<string, Connection> Connections
        {
            get { return _connections; }
            //protected set { _connections = value; }
        }
        private ConcurrentDictionary<string, Connection> _connections;
        //是否心跳检测
        private bool _isSendHeartbeat = false;
        private const int _aliveTimeout = 3;

        /// <summary>
        /// 基类初始化
        /// </summary>
        public SocketBase(bool isSendHeartbeat)
        {
            this._connections = new ConcurrentDictionary<string, Connection>();
            this._isSendHeartbeat = isSendHeartbeat;
        }

        /// <summary>
        /// 结束监听
        /// </summary>
        protected void EndListenAndSend()
        {
            Thread.Sleep(200);//以防消息没有发完，或收完
            foreach (var keyValue in this._connections)
            {
                Connection remConn;
                this._connections.TryRemove(keyValue.Key, out remConn);
                remConn.Stop();
            }
        }

        /// <summary>
        /// 接收数据、发送数据 心跳检测
        /// </summary>
        protected void SenRecMsg()
        {
            foreach (var keyValue in this.Connections)
            {
                //客户端的心跳检测
                if (_isSendHeartbeat && !this.HeartbeatCheck(keyValue.Value))
                {
                    connClose(keyValue.Key);
                    continue;
                }

                this.Receive(keyValue.Value);//接收数据

                //判断是否存活，20s没有更新就认为没有存活
                double timSpan = (DateTime.Now - keyValue.Value.LastConnTime).TotalSeconds;
                if (timSpan > _aliveTimeout && !this.HeartbeatCheck(keyValue.Value))
                {
                    connClose(keyValue.Key);
                    continue;
                }
                //发送数据
                this.Send(keyValue.Value);
            }
        }


        /// <summary>
        /// 发送数据 缺少重发机制
        /// </summary>
        /// <param name="connection"></param>
        private bool Send(Connection connection)
        {
            bool bol = false;
            Message message = null;
            try
            {
                while (connection.MessageQueue.TryDequeue(out message))
                {
                    byte[] buffer = ToBytes(message);
                    lock (this)
                    {
                        connection.NetworkStream.Write(buffer, 0, buffer.Length);
                        connection.LastConnTime = DateTime.Now;
                        bol = true;
                    }
                    this.OnMessageSent(this, new MessageEventArgs(message, connection));
                }
            }
            catch (Exception ex)
            {
                bol = false;
                var messageList = connection.MessageQueue.ToList();
                if (message != null)
                    messageList.Insert(0, message);

                connection.Stop();
                connClose(connection.ConnectionName, messageList, ex);
            }

            return bol;
        }
        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="connection"></param>
        private bool Receive(Connection connection)
        {
            bool bol = false;
            try
            {
                if (connection.NetworkStream.CanRead && connection.NetworkStream.DataAvailable)
                {
                    Message message = Parse(connection.ConnectionName, connection.NetworkStream);
                    //不是心跳包时触发接收事件
                    if (!message.Command.Equals(Message.CommandType.Seartbeat))
                        this.OnMessageReceived(this, new MessageEventArgs(message, connection));
                    connection.LastConnTime = DateTime.Now;
                    bol = true;
                }
            }
            catch (Exception ex)
            {
                bol = false;
            }
            return bol;
        }
        /// <summary>
        /// 心跳检测
        /// </summary>
        private bool HeartbeatCheck(Connection connection)
        {
            bool bol = false;
            byte[] buffer = ToBytes(new Message(Message.CommandType.Seartbeat, "心跳包，可忽略"));
            try
            {
                lock (this)
                {
                    connection.NetworkStream.Write(buffer, 0, buffer.Length);
                    bol = true;
                    connection.LastConnTime = DateTime.Now;
                }
            }
            catch (Exception ex) //连接已经断开
            {
            }
            return bol;
        }

        /// <summary>
        /// 解析消息
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="networkStream"></param>
        /// <returns></returns>
        private Message Parse(string connectionName, NetworkStream networkStream)
        {
            int messageLength = 0;
            Message message = new Message();
            //先读出前四个字节，即Message长度
            byte[] buffer = new byte[4];
            if (networkStream.DataAvailable)
            {
                int count = networkStream.Read(buffer, 0, 4);
                if (count == 4)
                {
                    messageLength = BitConverter.ToInt32(buffer, 0);
                }
                else
                    throw new Exception("网络流长度不正确");
            }
            else
                throw new Exception("目前网络不可读");
            //读出消息的其它字节
            buffer = new byte[messageLength - 4];
            if (networkStream.DataAvailable)
            {
                int count = networkStream.Read(buffer, 0, buffer.Length);
                if (count == messageLength - 4)
                {
                    message.Command = (Message.CommandType)buffer[0];
                    message.MainVersion = buffer[1];
                    message.SecondVersion = buffer[2];

                    //读出消息体
                    message.MessageBody = SocketFactory.DefaultEncoding.GetString(buffer, 3, buffer.Length - 3);
                    message.ConnectionName = connectionName;

                    return message;
                }
                else
                    throw new Exception("网络流长度不正确");
            }
            else
                throw new Exception("目前网络不可读");
        }
        /// <summary>
        /// 把消息转换成字节数组
        /// </summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        private byte[] ToBytes(Message message)
        {
            int messageLength = 7 + SocketFactory.DefaultEncoding.GetByteCount(message.MessageBody);//计算消息总长度。消息头长度为7加上消息体的长度。
            byte[] buffer = new byte[messageLength];
            //先将长度的4个字节写入到数组中。
            BitConverter.GetBytes(messageLength).CopyTo(buffer, 0);
            //将CommandHeader写入到数组中
            buffer[4] = (byte)message.Command;
            //将主版本号写入到数组中
            buffer[5] = (byte)message.MainVersion;
            //将次版本号写入到数组中
            buffer[6] = (byte)message.SecondVersion;

            //消息头已写完，现在写消息体。
            byte[] body = new byte[messageLength - 7];
            SocketFactory.DefaultEncoding.GetBytes(message.MessageBody).CopyTo(buffer, 7);
            return buffer;
        }

        /// <summary>
        /// 触发连接关闭
        /// </summary>
        /// <param name="key"></param>
        protected void connClose(string key)
        {
            Connection remConn;
            this.Connections.TryRemove(key, out remConn);
            connClose(remConn, new Exception("长时间未更新存活时间"));
        }
        /// <summary>
        /// 触发连接关闭
        /// </summary>
        /// <param name="remConn"></param>
        /// <param name="ex"></param>
        protected void connClose(Connection remConn, Exception ex)
        {
            if (remConn != null)
                remConn.Stop();
            connClose(remConn.ConnectionName, remConn.MessageQueue.ToList(), ex);
        }
        /// <summary>
        /// 触发连接关闭
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="messageQueue"></param>
        /// <param name="ex"></param>
        protected void connClose(string connectionName, List<Message> messageQueue, Exception ex)
        {
            ConCloseMessagesEventArgs ce = new ConCloseMessagesEventArgs(connectionName,
                messageQueue, ex);
            this.OnConnectionClose(this, ce);
        }

        #region 连接关闭事件
        /// <summary>
        /// 连接关闭事件委托
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ConCloseMessagesHandler(object sender, ConCloseMessagesEventArgs e);
        /// <summary>
        /// 连接关闭事件
        /// </summary>
        public event ConCloseMessagesHandler ConnectionClose;
        /// <summary>
        /// 连接关闭事件
        /// </summary>
        protected void OnConnectionClose(object sender, ConCloseMessagesEventArgs e)
        {
            if (ConnectionClose != null)
                this.ConnectionClose(sender, e);
        }
        #endregion

        #region 连接接入事件
        /// <summary>
        /// 连接事件委托
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ConnectedEventArgs(object sender, Connection e);
        /// <summary>
        /// 新连接接入事件
        /// </summary>
        public event ConnectedEventArgs Connected;
        /// <summary>
        /// 连接接入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnConnected(object sender, Connection e)
        {
            if (Connected != null)
                this.Connected(sender, e);
        }
        #endregion

        #region Message事件
        /// <summary>
        /// 接收消息委托
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);

        /// <summary>
        /// 接收到消息事件
        /// </summary>
        public event MessageEventHandler MessageReceived;
        /// <summary>
        /// 接收到消息事件
        /// </summary>
        protected void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (MessageReceived != null)
                this.MessageReceived(sender, e);
        }

        /// <summary>
        /// 消息已发出事件
        /// </summary>
        public event MessageEventHandler MessageSent;
        /// <summary>
        /// 消息已发出事件
        /// </summary>
        protected void OnMessageSent(object sender, MessageEventArgs e)
        {
            if (MessageSent != null)
                this.MessageSent(sender, e);
        }
        #endregion
    }
}
