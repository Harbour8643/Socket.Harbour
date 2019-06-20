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
        public NetworkStream NetworkStream { get; private set; }

        /// <summary>
        /// ���ӵ�Key
        /// </summary>
        public string ConnectionName { get; private set; }

        /// <summary>
        /// ���ӱ�����ֻ��IP��û�ж˿ں�
        /// </summary>
        public string ConnectionNickName { get; set; }

        /// <summary>
        /// �����ӵ���Ϣ����
        /// </summary>
        public ConcurrentQueue<Message> MessageQueue { get; private set; }

        /// <summary>
        /// TcpClient
        /// </summary>
        public TcpClient TcpClient { get; private set; }

        /// <summary>
        /// �������ͨ��ʱ��
        /// </summary>
        public DateTime LastConnTime { get; set; }
        #endregion

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="tcpClient">�ѽ������ӵ�TcpClient</param>
        /// <param name="connectionName">������</param>
        public Connection(TcpClient tcpClient, string connectionName)
        {
            this.TcpClient = tcpClient;
            this.ConnectionName = connectionName;
            this.ConnectionNickName = this.ConnectionName.Split(':')[0];
            this.NetworkStream = this.TcpClient.GetStream();
            this.MessageQueue = new ConcurrentQueue<Message>();
        }

        /// <summary>
        /// �ж����Ӳ��ͷ���Դ
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
        /// ������Ϣ
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(string msg)
        {
            Message message = new Message();
            this.MessageQueue.Enqueue(new Message(msg));
        }
        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(Message msg)
        {
            this.MessageQueue.Enqueue(msg);
        }

    }
}
