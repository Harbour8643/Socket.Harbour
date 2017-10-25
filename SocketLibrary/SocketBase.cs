using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace SocketLibrary
{
    /// <summary>
    /// SocketBase,Socketͨ�Ż���
    /// </summary>
    public abstract class SocketBase
    {
        /// <summary>
        /// �����ӵ�Socket
        /// </summary>
        protected ConcurrentDictionary<string, Connection> Connections
        {
            get { return _connections; }
            //protected set { _connections = value; }
        }
        private ConcurrentDictionary<string, Connection> _connections;
        //�Ƿ��������
        private bool _isSendHeartbeat = false;
        private const int _aliveTimeout = 3;

        /// <summary>
        /// �����ʼ��
        /// </summary>
        public SocketBase(bool isSendHeartbeat)
        {
            this._connections = new ConcurrentDictionary<string, Connection>();
            this._isSendHeartbeat = isSendHeartbeat;
        }

        /// <summary>
        /// ��������
        /// </summary>
        protected void EndListenAndSend()
        {
            Thread.Sleep(200);//�Է���Ϣû�з��꣬������
            foreach (var keyValue in this._connections)
            {
                Connection remConn;
                this._connections.TryRemove(keyValue.Key, out remConn);
                remConn.Stop();
            }
        }

        /// <summary>
        /// �������ݡ��������� �������
        /// </summary>
        protected void SenRecMsg()
        {
            foreach (var keyValue in this.Connections)
            {
                //�ͻ��˵��������
                if (_isSendHeartbeat && !this.HeartbeatCheck(keyValue.Value))
                {
                    connClose(keyValue.Key);
                    continue;
                }

                this.Receive(keyValue.Value);//��������

                //�ж��Ƿ��20sû�и��¾���Ϊû�д��
                double timSpan = (DateTime.Now - keyValue.Value.LastConnTime).TotalSeconds;
                if (timSpan > _aliveTimeout && !this.HeartbeatCheck(keyValue.Value))
                {
                    connClose(keyValue.Key);
                    continue;
                }
                //��������
                this.Send(keyValue.Value);
            }
        }


        /// <summary>
        /// �������� ȱ���ط�����
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
        /// ��������
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
                    //����������ʱ���������¼�
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
        /// �������
        /// </summary>
        private bool HeartbeatCheck(Connection connection)
        {
            bool bol = false;
            byte[] buffer = ToBytes(new Message(Message.CommandType.Seartbeat, "���������ɺ���"));
            try
            {
                lock (this)
                {
                    connection.NetworkStream.Write(buffer, 0, buffer.Length);
                    bol = true;
                    connection.LastConnTime = DateTime.Now;
                }
            }
            catch (Exception ex) //�����Ѿ��Ͽ�
            {
            }
            return bol;
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="networkStream"></param>
        /// <returns></returns>
        private Message Parse(string connectionName, NetworkStream networkStream)
        {
            int messageLength = 0;
            Message message = new Message();
            //�ȶ���ǰ�ĸ��ֽڣ���Message����
            byte[] buffer = new byte[4];
            if (networkStream.DataAvailable)
            {
                int count = networkStream.Read(buffer, 0, 4);
                if (count == 4)
                {
                    messageLength = BitConverter.ToInt32(buffer, 0);
                }
                else
                    throw new Exception("���������Ȳ���ȷ");
            }
            else
                throw new Exception("Ŀǰ���粻�ɶ�");
            //������Ϣ�������ֽ�
            buffer = new byte[messageLength - 4];
            if (networkStream.DataAvailable)
            {
                int count = networkStream.Read(buffer, 0, buffer.Length);
                if (count == messageLength - 4)
                {
                    message.Command = (Message.CommandType)buffer[0];
                    message.MainVersion = buffer[1];
                    message.SecondVersion = buffer[2];

                    //������Ϣ��
                    message.MessageBody = SocketFactory.DefaultEncoding.GetString(buffer, 3, buffer.Length - 3);
                    message.ConnectionName = connectionName;

                    return message;
                }
                else
                    throw new Exception("���������Ȳ���ȷ");
            }
            else
                throw new Exception("Ŀǰ���粻�ɶ�");
        }
        /// <summary>
        /// ����Ϣת�����ֽ�����
        /// </summary>
        /// <param name="message">��Ϣ</param>
        /// <returns></returns>
        private byte[] ToBytes(Message message)
        {
            int messageLength = 7 + SocketFactory.DefaultEncoding.GetByteCount(message.MessageBody);//������Ϣ�ܳ��ȡ���Ϣͷ����Ϊ7������Ϣ��ĳ��ȡ�
            byte[] buffer = new byte[messageLength];
            //�Ƚ����ȵ�4���ֽ�д�뵽�����С�
            BitConverter.GetBytes(messageLength).CopyTo(buffer, 0);
            //��CommandHeaderд�뵽������
            buffer[4] = (byte)message.Command;
            //�����汾��д�뵽������
            buffer[5] = (byte)message.MainVersion;
            //���ΰ汾��д�뵽������
            buffer[6] = (byte)message.SecondVersion;

            //��Ϣͷ��д�꣬����д��Ϣ�塣
            byte[] body = new byte[messageLength - 7];
            SocketFactory.DefaultEncoding.GetBytes(message.MessageBody).CopyTo(buffer, 7);
            return buffer;
        }

        /// <summary>
        /// �������ӹر�
        /// </summary>
        /// <param name="key"></param>
        protected void connClose(string key)
        {
            Connection remConn;
            this.Connections.TryRemove(key, out remConn);
            connClose(remConn, new Exception("��ʱ��δ���´��ʱ��"));
        }
        /// <summary>
        /// �������ӹر�
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
        /// �������ӹر�
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

        #region ���ӹر��¼�
        /// <summary>
        /// ���ӹر��¼�ί��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ConCloseMessagesHandler(object sender, ConCloseMessagesEventArgs e);
        /// <summary>
        /// ���ӹر��¼�
        /// </summary>
        public event ConCloseMessagesHandler ConnectionClose;
        /// <summary>
        /// ���ӹر��¼�
        /// </summary>
        protected void OnConnectionClose(object sender, ConCloseMessagesEventArgs e)
        {
            if (ConnectionClose != null)
                this.ConnectionClose(sender, e);
        }
        #endregion

        #region ���ӽ����¼�
        /// <summary>
        /// �����¼�ί��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ConnectedEventArgs(object sender, Connection e);
        /// <summary>
        /// �����ӽ����¼�
        /// </summary>
        public event ConnectedEventArgs Connected;
        /// <summary>
        /// ���ӽ����¼�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnConnected(object sender, Connection e)
        {
            if (Connected != null)
                this.Connected(sender, e);
        }
        #endregion

        #region Message�¼�
        /// <summary>
        /// ������Ϣί��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);

        /// <summary>
        /// ���յ���Ϣ�¼�
        /// </summary>
        public event MessageEventHandler MessageReceived;
        /// <summary>
        /// ���յ���Ϣ�¼�
        /// </summary>
        protected void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (MessageReceived != null)
                this.MessageReceived(sender, e);
        }

        /// <summary>
        /// ��Ϣ�ѷ����¼�
        /// </summary>
        public event MessageEventHandler MessageSent;
        /// <summary>
        /// ��Ϣ�ѷ����¼�
        /// </summary>
        protected void OnMessageSent(object sender, MessageEventArgs e)
        {
            if (MessageSent != null)
                this.MessageSent(sender, e);
        }
        #endregion
    }
}
