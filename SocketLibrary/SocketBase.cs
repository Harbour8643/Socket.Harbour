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
                if (_isSendHeartbeat)
                    this.HeartbeatCheck(keyValue.Value);

                this.Receive(keyValue.Value);//��������

                //�ж��Ƿ��20sû�и��¾���Ϊû�д��
                double timSpan = (DateTime.Now - keyValue.Value.LastConnTime).TotalSeconds;
                if (timSpan > 2)
                {
                    Connection remConn;
                    this.Connections.TryRemove(keyValue.Key, out remConn);

                    ConCloseMessagesEventArgs ce = new ConCloseMessagesEventArgs(keyValue.Value.ConnectionName,
                        new ConcurrentQueue<Message>(keyValue.Value.messageQueue), new Exception("��ʱ��δ���´��ʱ��"));
                    this.OnConnectionClose(this, ce);
                    continue;
                }
                this.Send(keyValue.Value); //��������
            }
        }

        /// <summary>
        /// �������� ȱ���ط�����
        /// </summary>
        /// <param name="connection"></param>
        private void Send(Connection connection)
        {
            try
            {
                //if (!connection.NetworkStream.CanWrite)
                //    return;
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
        private void Receive(Connection connection)
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
                    connection.LastConnTime = DateTime.Now;
                }
            }
            catch (Exception ex) //�����Ѿ��Ͽ�
            {
                this.OnException(this, new ExceptionEventArgs("HeartbeatCheckErr", ex));
            }
            return bol;
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

        #region ȫ�ִ����¼�
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
