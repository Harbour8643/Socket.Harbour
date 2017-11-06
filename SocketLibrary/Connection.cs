using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SocketLibrary
{
    /// <summary>
    /// Connection ��ժҪ˵����
    /// </summary>
    public class Connection
    {
        #region ����

        /// <summary>
        ///  �ṩ����������ʵĻ���������
        /// </summary>
        public NetworkStream NetworkStream
        {
            get { return _networkStream; }
            private set { _networkStream = value; }
        }
        private NetworkStream _networkStream;

        /// <summary>
        /// ���ӵ�Key
        /// </summary>
        public string ConnectionName
        {
            get { return _connectionName; }
            private set { _connectionName = value; }
        }
        private string _connectionName;

        /// <summary>
        /// ���ӱ�����ֻ��IP��û�ж˿ں�
        /// </summary>
        public string ConnectionNickName
        {
            get { return _connectionnickName; }
            set { _connectionnickName = value; }
        }
        private string _connectionnickName;

        /// <summary>
        /// �����ӵ���Ϣ����
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
        /// �������ͨ��ʱ��
        /// </summary>
        public DateTime LastConnTime
        {
            get { return _lastConnTime; }
            set { _lastConnTime = value; }
        }
        private DateTime _lastConnTime;
        #endregion

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="tcpClient">�ѽ������ӵ�TcpClient</param>
        /// <param name="connectionName">������</param>
        public Connection(TcpClient tcpClient, string connectionName)
        {
            this._tcpClient = tcpClient;
            this._connectionName = connectionName;
            this.ConnectionNickName = this._connectionName.Split(':')[0];
            this._networkStream = this._tcpClient.GetStream();
            this._messageQueue = new ConcurrentQueue<Message>();
        }

        /// <summary>
        /// �ж����Ӳ��ͷ���Դ
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
        /// ������Ϣ
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(string msg)
        {
            Message message = new Message();
            this._messageQueue.Enqueue(new Message(msg));
        }
        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(Message msg)
        {
            this._messageQueue.Enqueue(msg);
        }

    }
}
