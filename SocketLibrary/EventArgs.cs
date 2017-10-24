using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace SocketLibrary
{
    /// <summary>
    /// 连接关闭事件
    /// </summary>
    public class ConCloseMessagesEventArgs : EventArgs
    {
        /// <summary>
        /// 连接名
        /// </summary>
        public string ConnectionName { get; }
        /// <summary>
        /// 未发送的消息集合
        /// </summary>
        public Message[] MessageQueue { get; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="messageQueue"></param>
        /// <param name="exception"></param>
        public ConCloseMessagesEventArgs(string connectionName, ConcurrentQueue<Message> messageQueue, Exception exception)
        {
            this.ConnectionName = connectionName;
            this.MessageQueue = messageQueue.ToArray();
            this.Exception = exception;
        }
    }

    /// <summary>
    /// 错误事件
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// 连接名
        /// </summary>
        public string ExceptionName { get; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="exceptionName"></param>
        /// <param name="exception"></param>
        public ExceptionEventArgs(string exceptionName, Exception exception)
        {
            this.ExceptionName = exceptionName;
            this.Exception = exception;
        }
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的消息
        /// </summary>
        public Message Message { get; }
        /// <summary>
        /// 发送此消息的连接
        /// </summary>
        public Connection Connecction { get; }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="message"></param>
        /// <param name="connection"></param>
        public MessageEventArgs(Message message, Connection connection)
        {
            this.Message = message;
            this.Connecction = connection;
        }
    }

    /// <summary>
    /// 消息发送失败事件
    /// </summary>
    public class MessageSentErrEventArgs : EventArgs
    {
        /// <summary>
        /// 连接名
        /// </summary>
        public string ConnectionName { get; }
        /// <summary>
        /// 未发送的消息集合
        /// </summary>
        public Message Message { get; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public MessageSentErrEventArgs(string connectionName, Message message, Exception exception)
        {
            this.ConnectionName = connectionName;
            this.Message = message;
            this.Exception = exception;
        }
    }

}
