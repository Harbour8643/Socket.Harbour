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
        /// �����߳�
        /// </summary>
        private Thread _listenningthread;

        /// <summary>
        /// ��ʼ����
        /// </summary>
        protected void StartListenAndSend()
        {
            _listenningthread = new Thread(new ThreadStart(Listenning));
            _listenningthread.Start();
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
            _listenningthread.Abort();
        }

        private void Listenning()
        {
            while (true)
            {
                Thread.Sleep(200);
                foreach (var keyValue in this._connections)
                {
                    //�������
                    if (!this.HeartbeatCheck(keyValue.Value))
                    {
                        Connection remConn;
                        this._connections.TryRemove(keyValue.Key, out remConn);
                        continue;
                    }
                    try
                    {
                        this.Receive(keyValue.Value);//��������
                        this.Send(keyValue.Value); //��������
                    }
                    catch (Exception ex)
                    {
                        keyValue.Value.NetworkStream.Close();
                        ConCloseMessagesEventArgs ce = new ConCloseMessagesEventArgs(keyValue.Value.ConnectionName, new ConcurrentQueue<Message>(keyValue.Value.messageQueue), ex);
                        this.OnConnectionClose(this, ce);
                    }
                }
            }
        }

        #region ˽�еķ���

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="connection"></param>
        private void Send(Connection connection)
        {
            if (connection.NetworkStream.CanWrite)
            {
                Message message;
                while (connection.messageQueue.TryDequeue(out message))
                {
                    byte[] buffer = message.ToBytes();
                    lock (this)
                    {
                        connection.NetworkStream.Write(buffer, 0, buffer.Length);
                        message.Sent = true;
                    }
                    this.OnMessageSent(this, new MessageEventArgs(message, connection));
                }
            }
        }
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="connection"></param>
        private void Receive(Connection connection)
        {
            if (connection.NetworkStream.CanRead && connection.NetworkStream.DataAvailable)
            {
                Message message = connection.Parse();
                //����������ʱ���������¼�
                if (!message.Command.Equals(Message.CommandType.Seartbeat))
                {
                    this.OnMessageReceived(this, new MessageEventArgs(message, connection));
                }
            }
        }
        /// <summary>
        /// �������
        /// </summary>
        private bool HeartbeatCheck(Connection connection)
        {
            bool bol = false;
            byte[] buffer = new Message(Message.CommandType.Seartbeat, "���������ɺ���").ToBytes();
            try
            {
                lock (this)
                {
                    connection.NetworkStream.Write(buffer, 0, buffer.Length);
                    bol = true;
                }
            }
            catch (Exception ex) //�����Ѿ��Ͽ�
            {
                connection.NetworkStream.Close();
                ConCloseMessagesEventArgs ce = new ConCloseMessagesEventArgs(connection.ConnectionName, new ConcurrentQueue<Message>(connection.messageQueue), ex);
                this.OnConnectionClose(this, ce);
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
        private void OnConnectionClose(object sender, ConCloseMessagesEventArgs e)
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
        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (MessageReceived != null)
                this.MessageReceived(sender, e);
        }
        
        /// <summary>
        /// ��Ϣ�ѷ����¼�
        /// </summary>
        public event MessageEventHandler MessageSent;
        private void OnMessageSent(object sender, MessageEventArgs e)
        {
            if (MessageSent != null)
                this.MessageSent(sender, e);
        }

        #endregion

    }
}
