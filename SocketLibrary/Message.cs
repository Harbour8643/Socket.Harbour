using System;
using System.Net.Sockets;

namespace SocketLibrary
{
    /// <summary>
    /// ��Ϣ��
    /// </summary>
    public class Message
    {
        /// <summary>
        /// ö����Ϣ����
        /// </summary>
        public enum CommandType : byte
        {
            /// <summary>
            /// ������Ϣ
            /// </summary>
            SendMessage = 1,
            /// <summary>
            /// ������
            /// </summary>
            Seartbeat = 2
        }

        /// <summary>
        /// ��Ϣ������
        /// </summary>
        public string ConnectionName;
        /// <summary>
        /// ��Ϣ����
        /// </summary>
        public CommandType Command;
        /// <summary>
        /// ���汾��
        /// </summary>
        public byte MainVersion;
        /// <summary>
        ///  �ΰ汾��
        /// </summary>
        public byte SecondVersion;
        /// <summary>
        /// ��Ϣ����
        /// </summary>
        public string MessageBody;
        /// <summary>
        /// ��Ϣ�Ƿ��ѷ���
        /// </summary>

        /// <summary>
        /// ��Ϣ��
        /// </summary>
        public Message()
        {
            ConnectionName = null;
        }
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="messageBody">��Ϣ����</param>
        public Message(string messageBody)
            : this(CommandType.SendMessage, messageBody)
        {
        }
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="command">��Ϣ����</param>
        /// <param name="messageBody">��Ϣ����</param>
        public Message(CommandType command, string messageBody)
            : this(command, 0, messageBody)
        {
        }
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="command">��Ϣ����</param>
        /// <param name="mainVersion">���汾��</param>
        /// <param name="messageBody">��Ϣ����</param>
        public Message(CommandType command, byte mainVersion, string messageBody)
            : this(command, mainVersion, 0, messageBody)
        {
        }
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="command">��Ϣ����</param>
        /// <param name="mainVersion">���汾��</param>
        /// <param name="secondVersion">�ΰ汾��</param>
        /// <param name="messageBody">��Ϣ����</param>
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
