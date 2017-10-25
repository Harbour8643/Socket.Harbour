using System;
using System.Collections.Generic;
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
        //�Ƿ��������
        private const bool _isSendHeartbeat = true;
        /// <summary>
        ///  ��ʱʱ��
        /// </summary>
        /// 
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
        public Client(IPAddress ipaddress, int port) : base(_isSendHeartbeat)
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
            this.EndListenAndSend();
            //ȱ��֪ͨ������� �Լ������ر���
            _listenningClientThread.Abort();
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
                        Connection conn = new Connection(client, this._clientName);
                        this.Connections.TryAdd(this._clientName, conn);
                        this.OnConnected(this, conn);
                    }
                    catch (Exception ex)
                    {
                        this.connClose(this._clientName, new List<Message>() { }, ex);
                    }
                }

                //�������ݡ��������� �������
                this.SenRecMsg();

                Thread.Sleep(1000);
            }
        }
    }
}
