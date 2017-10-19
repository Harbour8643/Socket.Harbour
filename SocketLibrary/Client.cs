using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketLibrary
{
    /// <summary>
    /// Socket�Ŀͻ���
    /// </summary>
    public class Client : SocketBase
    {
        /// <summary>
        ///  ��ʱʱ��
        /// </summary>
        public const int CONNECTTIMEOUT = 10;
        private TcpClient client;
        private IPAddress ipAddress;
        private int port;
        private Thread _listenningClientThread;
        private string _clientName;
        /// <summary>
        /// ���ӵ�Key
        /// </summary>
        public string ClientName
        {
            get { return _clientName; }
        }
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="ipaddress">��ַ</param>
        /// <param name="port">�˿�</param>
        public Client(string ipaddress, int port)
            : this(IPAddress.Parse(ipaddress), port)
        {
        }
        /// <summary>
        ///��ʼ�� 
        /// </summary>
        /// <param name="ipaddress">��ַ</param>
        /// <param name="port">�˿�</param>
        public Client(IPAddress ipaddress, int port)
        {
            this.ipAddress = ipaddress;
            this.port = port;
            this._clientName = ipAddress + ":" + port;
        }

        /// <summary>
        /// ������
        /// </summary>
        public void StartClient()
        {
            _listenningClientThread = new Thread(new ThreadStart(Start));
            _listenningClientThread.Start();
        }
        /// <summary>
        /// �ر����Ӳ��ͷ���Դ
        /// </summary>
        public void StopClient()
        {
            //ȱ��֪ͨ������� �Լ������ر���
            _listenningClientThread.Abort();
            this.EndListenAndSend();
        }
        /// <summary>
        /// ��ȡָ��������������,��ѯ��������null
        /// </summary>
        /// <returns></returns>
        public Connection GetConnection()
        {
            Connection connection = null;
            this.Connections.TryGetValue(this.ClientName, out connection);
            return connection;
        }

        private void Start()
        {
            while (true)
            {
                if (!this.Connections.ContainsKey(this._clientName))
                {
                    try
                    {
                        client = new TcpClient();
                        client.SendTimeout = CONNECTTIMEOUT;
                        client.ReceiveTimeout = CONNECTTIMEOUT;
                        client.Connect(ipAddress, port);
                        this.Connections.TryAdd(this._clientName, new Connection(client, this._clientName));
                    }
                    catch (Exception ex)
                    { //��������ʧ���¼�
                        this.OnException(this, new ExceptionEventArgs("TcpClientAddErr", ex));
                    }
                }
                foreach (var keyValue in this.Connections)
                {
                    //�ͻ��˵��������
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
                Thread.Sleep(500);
            }
        }
    }
}
