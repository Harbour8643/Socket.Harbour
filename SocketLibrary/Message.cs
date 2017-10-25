using System;
using System.Net.Sockets;

namespace SocketLibrary
{
    /// <summary>
    /// 消息类
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 枚举消息类型
        /// </summary>
        public enum CommandType : byte
        {
            /// <summary>
            /// 发送消息
            /// </summary>
            SendMessage = 1,
            /// <summary>
            /// 心跳包
            /// </summary>
            Seartbeat = 2
        }

        /// <summary>
        /// 消息连接名
        /// </summary>
        public string ConnectionName;
        /// <summary>
        /// 消息类型
        /// </summary>
        public CommandType Command;
        /// <summary>
        /// 主版本号
        /// </summary>
        public byte MainVersion;
        /// <summary>
        ///  次版本号
        /// </summary>
        public byte SecondVersion;
        /// <summary>
        /// 消息内容
        /// </summary>
        public string MessageBody;
        /// <summary>
        /// 消息是否已发出
        /// </summary>

        /// <summary>
        /// 消息类
        /// </summary>
        public Message()
        {
            ConnectionName = null;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="messageBody">消息内容</param>
        public Message(string messageBody)
            : this(CommandType.SendMessage, messageBody)
        {
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="command">消息类型</param>
        /// <param name="messageBody">消息内容</param>
        public Message(CommandType command, string messageBody)
            : this(command, 0, messageBody)
        {
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="command">消息类型</param>
        /// <param name="mainVersion">主版本号</param>
        /// <param name="messageBody">消息内容</param>
        public Message(CommandType command, byte mainVersion, string messageBody)
            : this(command, mainVersion, 0, messageBody)
        {
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="command">消息类型</param>
        /// <param name="mainVersion">主版本号</param>
        /// <param name="secondVersion">次版本号</param>
        /// <param name="messageBody">消息内容</param>
        public Message(CommandType command, byte mainVersion, byte secondVersion, string messageBody)
            : this()
        {
            this.Command = command;
            this.MainVersion = mainVersion;
            this.SecondVersion = secondVersion;
            this.MessageBody = messageBody;
        }
    }
}
