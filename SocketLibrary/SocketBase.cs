using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SocketLibrary
{
    /// <summary>
    /// SocketBase,Socketͨ�Ż���
    /// </summary>
    public abstract class SocketBase
    {
        #region ����

        /// <summary>
        /// �����ӵ�Socket
        /// </summary>
        protected ConcurrentDictionary<string, Connection> Connections
        {
            get { return _connections; }
            //protected set { _connections = value; }
        }
        private ConcurrentDictionary<string, Connection> _connections;

        #endregion

        /// <summary>
        /// �����ʼ��
        /// </summary>
        protected SocketBase()
        {
            this._connections = new ConcurrentDictionary<string, Connection>();
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


        #region �ܱ����ķ���

        /// <summary>
        /// �������� ȱ���ط�����
        /// </summary>
        /// <param name="connection"></param>
        protected void Send(Connection connection)
        {
            try
            {
                if (!connection.NetworkStream.CanWrite)
                    return;
                Message message;
                while (connection.messageQueue.TryDequeue(out message))
                {
                    try
                    {
                        byte[] buffer = message.ToBytes();
                        lock (this)
                        {
                            connection.NetworkStream.Write(buffer, 0, buffer.Length);
                            message.Sent = true;
                        }
                        connection.LastConnTime = DateTime.Now;
                        this.OnMessageSent(this, new MessageEventArgs(message, connection));
                    }
                    catch (Exception ex)
                    {
                        this.OnMessageSentErr(this, new MessageSentErrEventArgs(connection.ConnectionName, message, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                this.OnException(this, new ExceptionEventArgs("SendErr", ex));
            }
        }
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="connection"></param>
        protected void Receive(Connection connection)
        {
            try
            {
                if (connection.NetworkStream.CanRead && connection.NetworkStream.DataAvailable)
                {
                    Message message = connection.Parse();
                    //����������ʱ���������¼�
                    if (!message.Command.Equals(Message.CommandType.Seartbeat))
                    {
                        this.OnMessageReceived(this, new MessageEventArgs(message, connection));
                    }
                    connection.LastConnTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                this.OnException(this, new ExceptionEventArgs("ReceiveErr", ex));
            }
        }
        /// <summary>
        /// �������
        /// </summary>
        protected bool HeartbeatCheck(Connection connection)
        {
            bool bol = false;
            byte[] buffer = new Message(Message.CommandType.Seartbeat, "���������ɺ���").ToBytes();
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
                this.OnException(this, new ExceptionEventArgs("HeartbeatCheckErr", ex));
            }
            return bol;
        }

        #endregion

        #region ���ӹر��¼�
        /// <summary>
        /// ���ӹر��¼�
        /// </summary>
        public class ConCloseMessagesEventArgs : EventArgs
        {
            /// <summary>
            /// ������
            /// </summary>
            public string ConnectionName { get; }
            /// <summary>
            /// δ���͵���Ϣ����
            /// </summary>
            public Message[] MessageQueue { get; }
            /// <summary>
            /// ������Ϣ
            /// </summary>
            public Exception Exception { get; }
            /// <summary>
            /// ��ʼ��
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

        #region ȫ�ִ����¼�
        /// <summary>
        /// �����¼�
        /// </summary>
        public class ExceptionEventArgs : EventArgs
        {
            /// <summary>
            /// ������
            /// </summary>
            public string ExceptionName { get; }
            /// <summary>
            /// ������Ϣ
            /// </summary>
            public Exception Exception { get; }
            /// <summary>
            /// ��ʼ��
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
        /// �����¼�
        /// </summary>
        public delegate void ExceptionHandler(object sender, ExceptionEventArgs e);
        /// <summary>
        /// �����¼�
        /// </summary>
        public event ExceptionHandler Exception;
        /// <summary>
        /// �����¼�
        /// </summary>
        protected void OnException(object sender, ExceptionEventArgs e)
        {
            if (Exception != null)
                this.Exception(sender, e);
        }
        #endregion

        #region Message�¼�
        /// <summary>
        /// ������Ϣ
        /// </summary>
        public class MessageEventArgs : EventArgs
        {
            /// <summary>
            /// ���յ�����Ϣ
            /// </summary>
            public Message Message { get; }
            /// <summary>
            /// ���ʹ���Ϣ������
            /// </summary>
            public Connection Connecction { get; }
            /// <summary>
            /// ��ʼ��
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

        #region ��Ϣ����ʧ���¼�
        /// <summary>
        /// ��Ϣ����ʧ���¼�
        /// </summary>
        public class MessageSentErrEventArgs : EventArgs
        {
            /// <summary>
            /// ������
            /// </summary>
            public string ConnectionName { get; }
            /// <summary>
            /// δ���͵���Ϣ����
            /// </summary>
            public Message Message { get; }
            /// <summary>
            /// ������Ϣ
            /// </summary>
            public Exception Exception { get; }
            /// <summary>
            /// ��ʼ��
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
        /// <summary>
        /// ��Ϣ����ʧ���¼�ί��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void MessageSentErrHandler(object sender, MessageSentErrEventArgs e);
        /// <summary>
        /// ��Ϣ����ʧ���¼�
        /// </summary>
        public event MessageSentErrHandler MessageSentErr;
        /// <summary>
        /// ��Ϣ����ʧ���¼�
        /// </summary>
        protected void OnMessageSentErr(object sender, MessageSentErrEventArgs e)
        {
            if (MessageSentErr != null)
                this.MessageSentErr(sender, e);
        }
        #endregion
    }
}
