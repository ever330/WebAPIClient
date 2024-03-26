using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientNetwork : MonoBehaviour
{
    private static ClientNetwork instance = null;
    private readonly object lockObject = new object();

    private int tcpPort = 3000;
    private int udpPort = 3001;
    private string hostIp = "127.0.0.1";
    private byte[] recvBuffer;
    private const int MAXSIZE = 1024;
    public float sendInterval = 0.1f; // ���� ���� ���� (�� ����)

    private int FREQUENCY = 44100; // ����ũ ���ø� ����Ʈ

    private Socket tcpSocket;
    private StreamQueue sendQ;
    private StreamQueue recvQ;

    private UdpClient udpSendClient;
    private UdpClient udpReceiveClient;

    private Thread packetAnalysisThread;
    private Thread udpPosSendThread;
    private Thread udpVoiceSendThread;
    private Thread udpReceiveThread;

    private AudioSource audioSource;
    private AudioClip audioClip;

    private bool runUdpThread;

    [SerializeField] private GameObject TitlePanel;
    [SerializeField] private GameObject LoginPanel;
    [SerializeField] private GameObject TryLoginImage;
    [SerializeField] private TextMeshProUGUI TitleNicknameText;

    [SerializeField] private Title TitleObj;

    public int NowRoomNum { get; set; }
    public string Nickname { get; set; }

    public Queue<PlayerInfo> AddPlayers { get; private set; }

    public string CurrentSceneName { get; set; }

    public float PlayerPosX { get; set; }
    public float PlayerPosY { get; set; }
    public float PlayerPosZ { get; set; }
    public float PlayerForX { get; set; }
    public float PlayerForY { get; set; }
    public float PlayerForZ { get; set; }

    private int currentPosition = 0;

    void Awake()
    {
        if (null == instance)
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public static ClientNetwork Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        recvQ = new StreamQueue(MAXSIZE * 4);
        sendQ = new StreamQueue(MAXSIZE * 4);
        recvBuffer = new byte[MAXSIZE];
        tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        AddPlayers = new Queue<PlayerInfo>();

        FindPort();

        runUdpThread = false;

        audioSource = GetComponent<AudioSource>();

        IPAddress[] localIpAddresses = Dns.GetHostAddresses(Dns.GetHostName()); // ���� ��Ʈ��ũ�� IP �ּ� ��� ��������

        foreach (IPAddress localIpAddress in localIpAddresses)
        {
            if (localIpAddress.ToString() != "172.30.1.100")
            {
                // �ܺ� ���� ó���� ��Ʈ �� IP ����
            }
        }
    }

    void FindPort()
    {
        for (int i = 3010; i < 4000; i++)
        {
            try
            {
                udpSendClient = new UdpClient(i);
                udpReceiveClient = new UdpClient(i + 1);
                break;
            }
            catch (SocketException)
            {
                // �ش� ��Ʈ�� �̹� ��� ���� ��� ���� ��Ʈ�� �̵�
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void ConnectToServer()
    {
        Debug.Log("���� ���� ���� �����");
        try
        {
            tcpSocket.BeginConnect(hostIp, tcpPort, new AsyncCallback(ConnectCallBack), tcpSocket);
            packetAnalysisThread = new Thread(PacketAnalysis);
            packetAnalysisThread.Start();

            if (SceneManager.GetActiveScene().name == "MainScene")
            {
                TitlePanel.SetActive(true);
                LoginPanel.SetActive(false);
                TryLoginImage.SetActive(false);
            }
        }
        catch (SocketException e)
        {
            Debug.Log("���ӽ���");
            ConnectToServer();
            Debug.Log(e.Message);
        }
    }

    private void ConnectCallBack(IAsyncResult IAR)
    {
        try
        {
            Socket tempSocket = (Socket)IAR.AsyncState;
            IPEndPoint ipEp = (IPEndPoint)tempSocket.RemoteEndPoint;

            Debug.Log("���Ӽ���");

            tempSocket.EndConnect(IAR);
            tcpSocket = tempSocket;
            tcpSocket.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None,
                new AsyncCallback(OnReceiveCallBack), tcpSocket);

            ClientConnectPacket pac = new ClientConnectPacket
            {
                Nickname = UserInfo.Instance.Username
            };
            byte[] sendData = new Packet<ClientConnectPacket>(pac).Serialize();

            PacketSend(new Packet<ClientConnectPacket>(pac).Serialize(), PacketId.ClientConnect);
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.NotConnected)
            {
                Debug.Log("���ӽ���");
                ConnectToServer();
            }
        }
    }

    public void Receive()
    {
        tcpSocket.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None,
            new AsyncCallback(OnReceiveCallBack), tcpSocket);
    }

    private void OnReceiveCallBack(IAsyncResult IAR)
    {
        try
        {
            Socket tempSock = (Socket)IAR.AsyncState;
            int readSize = tempSock.EndReceive(IAR);

            if (tempSock == null)
            {
                return;
            }

            // �����κ��� ���۹��� �����Ͱ� ������� ������ť�� �־��ش�.
            if (readSize != 0)
            {
                Debug.Log("������ ����");

                recvQ.WriteData(recvBuffer, readSize);
            }
            Receive();
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                ConnectToServer();
            }
        }
    }

    public void PacketSend(byte[] packet, PacketId packetId)
    {
        SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();

        sendEventArgs.Completed += SendCompleted;
        sendEventArgs.UserToken = this;

        // ��Ŷ�� �� ����� ����Ʈ �迭�� ��ȯ
        byte[] sizeBytes = BitConverter.GetBytes(packet.Length);
        // ��Ŷ�� ���̵� ����Ʈ �迭�� ��ȯ
        byte[] idBytes = BitConverter.GetBytes((short)packetId);

        // �� ������� ���빰�� ��ĥ ����Ʈ �迭
        byte[] sendData = new byte[sizeBytes.Length + idBytes.Length + packet.Length];

        // �� �տ� ������ �߰�
        Array.Copy(sizeBytes, 0, sendData, 0, sizeBytes.Length);
        // ���̵� �߰�
        Buffer.BlockCopy(idBytes, 0, sendData, sizeBytes.Length, idBytes.Length);
        // �� ������ ��Ŷ ���빰 �߰�
        Buffer.BlockCopy(packet, 0, sendData, sizeBytes.Length + idBytes.Length, packet.Length);

        sendEventArgs.SetBuffer(sendData, 0, sendData.Length);

        bool pending = tcpSocket.SendAsync(sendEventArgs);

        if (!pending)
        {
            SendCompleted(null, sendEventArgs);
        }
    }

    private void SendCompleted(object sender, SocketAsyncEventArgs e)
    {
        //Debug.Log("��Ŷ ���� ����");
    }

    private void PacketAnalysis()
    {
        while (tcpSocket != null)
        {
            if (recvQ.dataCnt == 0)
                continue;

            byte[] totalData = recvQ.ReadData();

            int packetSize = BitConverter.ToInt32(totalData, 0);
            short packetId = BitConverter.ToInt16(totalData, 4);

            // ��Ŷ ũ�⸸ŭ�� �����͸� ����
            byte[] packetData = new byte[packetSize];
            Buffer.BlockCopy(totalData, 6, packetData, 0, packetSize);

            switch ((PacketId)packetId)
            {
                case PacketId.ResCreateRoom:
                    //Nickname = TitleNicknameText.text;
                    ResCreateRoomPacket result = Packet<ResCreateRoomPacket>.Deserialize(packetData);
                    NowRoomNum = result.RoomNum;
                    //UdpStart();
                    TitleObj.isCreateRoom = true;
                    break;

                case PacketId.ResEnterRoom:
                    //Nickname = TitleNicknameText.text;
                    ResEnterRoomPacket enterRes = Packet<ResEnterRoomPacket>.Deserialize(packetData);
                    if (enterRes.Result)
                    {
                        TitleObj.isEnterRoom = true;
                        //UdpStart();
                    }
                    else
                    {
                        TitleObj.isEnterFail = true;
                        NowRoomNum = 0;
                    }
                    break;


                case PacketId.ResRoomPlayers:
                    ResRoomPlayersPacket roomPlayer = Packet<ResRoomPlayersPacket>.Deserialize(packetData);
                    PlayerInfo playerInfo = new PlayerInfo();
                    playerInfo.NickName = Encoding.UTF8.GetString(roomPlayer.Nickname);
                    int nullIndex = playerInfo.NickName.IndexOf('\0');
                    playerInfo.NickName = playerInfo.NickName.Substring(0, nullIndex);
                    Debug.Log(playerInfo.NickName + "�߰�");
                    playerInfo.PosX = roomPlayer.PosX;
                    playerInfo.PosY = roomPlayer.PosY;
                    playerInfo.PosZ = roomPlayer.PosZ;
                    playerInfo.ForX = roomPlayer.ForX;
                    playerInfo.ForY = roomPlayer.ForY;
                    playerInfo.ForZ = roomPlayer.ForZ;
                    AddPlayers.Enqueue(playerInfo);
                    break;

                case PacketId.S2CEchoChat:
                    S2CEchoChat chatPacket = Packet<S2CEchoChat>.Deserialize(packetData);
                    string nickname = Encoding.UTF8.GetString(chatPacket.Nickname);
                    string chat = Encoding.UTF8.GetString(chatPacket.Chat);
                    int nicknameNullIndex = nickname.IndexOf('\0');
                    int chatNullIndex = nickname.IndexOf('\0');
                    string reName = nickname.Substring(0, nicknameNullIndex);
                    string reChat = chat.Substring(0, chatNullIndex);

                    lock (lockObject)
                    {
                        InGameManager.Instance.ChatQueue.Enqueue(new Tuple<string, string>(reName, reChat));
                    }
                    break;

                case PacketId.S2CNewPlayer:
                    S2CNewPlayerPacket newPlayer = Packet<S2CNewPlayerPacket>.Deserialize(packetData);
                    string name = Encoding.UTF8.GetString(newPlayer.Nickname);
                    int nameNullIndex = name.IndexOf('\0');
                    string renewName = name.Substring(0, nameNullIndex);
                    InGameManager.Instance.NewPlayerQueue.Enqueue(renewName);
                    break;


                default:
                    break;
            }

            Thread.Sleep(100);
        }
    }

    public void UdpStart()
    {
        runUdpThread = true;

        udpPosSendThread = new Thread(SendPosUDP);
        udpPosSendThread.Start();
        udpReceiveThread = new Thread(ReceiveUDP);
        udpReceiveThread.Start();

        //udpVoiceSendThread = new Thread(SendVoiceUDP);
        //udpVoiceSendThread.Start();

        StartCoroutine(SendVoice());
    }

    private void SendPosUDP()
    {
        while (runUdpThread)
        {
            C2SPlayerInfoPacket packet = new C2SPlayerInfoPacket();
            packet.PosX = PlayerPosX;
            packet.PosY = PlayerPosY;
            packet.PosZ = PlayerPosZ;
            packet.ForX = PlayerForX;
            packet.ForY = PlayerForY;
            packet.ForZ = PlayerForZ;
            packet.Nickname = UserInfo.Instance.Username;
            packet.RoomNum = NowRoomNum;

            byte[] pacData = new Packet<C2SPlayerInfoPacket>(packet).Serialize();

            // ��Ŷ�� �� ����� ����Ʈ �迭�� ��ȯ
            byte[] sizeBytes = BitConverter.GetBytes(pacData.Length);
            // ��Ŷ�� ���̵� ����Ʈ �迭�� ��ȯ
            byte[] idBytes = BitConverter.GetBytes((short)PacketId.C2SPlayerInfo);

            // �� ������� ���빰�� ��ĥ ����Ʈ �迭
            byte[] sendData = new byte[sizeBytes.Length + idBytes.Length + pacData.Length];

            // �� �տ� ������ �߰�
            Array.Copy(sizeBytes, 0, sendData, 0, sizeBytes.Length);
            // ���̵� �߰�
            Buffer.BlockCopy(idBytes, 0, sendData, sizeBytes.Length, idBytes.Length);
            // �� ������ ��Ŷ ���빰 �߰�
            Buffer.BlockCopy(pacData, 0, sendData, sizeBytes.Length + idBytes.Length, pacData.Length);

            //byte[] compressByte = Compress(sendData);

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), udpPort);

            udpSendClient.Send(sendData, sendData.Length, serverEndPoint);

            Thread.Sleep(50);
        }
    }

    private IEnumerator SendVoice()
    {
        audioClip = Microphone.Start(null, true, 1, FREQUENCY);
        //audioSource.clip = AudioClip.Create("test", 1 * FREQUENCY, audioClip.channels, FREQUENCY, false);

        //�����̸� ���̱� ���� �߰��� �ڵ�
        while (!(Microphone.GetPosition(null) > 0)) { }

        ////����ũ ���� ���
        //audioSource.Play();

        int currentPosition = 0;
        while (runUdpThread)
        {
            // ����ũ �Է� ������ ��������
            float[] data = new float[FREQUENCY / 10]; // ���ø�
            int offset = currentPosition % FREQUENCY;
            audioClip.GetData(data, offset); // AudioClip���κ��� ������ ��������

            // ����� �����͸� ����Ʈ �迭�� ��ȯ�Ͽ� ����
            byte[] pacData = new byte[data.Length * 4]; // float�� 4����Ʈ
            Buffer.BlockCopy(data, 0, pacData, 0, pacData.Length);

            // ��Ŷ�� �� ����� ����Ʈ �迭�� ��ȯ
            byte[] sizeBytes = BitConverter.GetBytes(pacData.Length);
            // ��Ŷ�� ���̵� ����Ʈ �迭�� ��ȯ
            byte[] idBytes = BitConverter.GetBytes((short)PacketId.C2SVoice);

            byte[] roomNumBytes = BitConverter.GetBytes(NowRoomNum);
            byte[] nicknameBytes = Encoding.UTF8.GetBytes(Nickname);

            // �� ������� ���빰�� ��ĥ ����Ʈ �迭
            byte[] sendData = new byte[sizeBytes.Length + idBytes.Length + roomNumBytes.Length + nicknameBytes.Length + pacData.Length];

            // �� �տ� ������ �߰�
            Array.Copy(sizeBytes, 0, sendData, 0, sizeBytes.Length);
            // ���̵� �߰�
            Buffer.BlockCopy(idBytes, 0, sendData, sizeBytes.Length, idBytes.Length);
            // ���ȣ �߰�
            Buffer.BlockCopy(roomNumBytes, 0, sendData, sizeBytes.Length + idBytes.Length, roomNumBytes.Length);
            // �г��� �߰�
            Buffer.BlockCopy(nicknameBytes, 0, sendData, sizeBytes.Length + idBytes.Length + roomNumBytes.Length, nicknameBytes.Length);
            // �� ������ ��Ŷ ���빰 �߰�
            Buffer.BlockCopy(pacData, 0, sendData, sizeBytes.Length + idBytes.Length + roomNumBytes.Length + nicknameBytes.Length, pacData.Length);

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), udpPort);

            udpSendClient.Send(sendData, sendData.Length, serverEndPoint);

            yield return new WaitForSeconds(sendInterval);

            currentPosition += data.Length;

            if (currentPosition >= FREQUENCY)
                currentPosition = 0;
        }
    }

    private void ReceiveUDP()
    {
        while (runUdpThread)
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpReceiveClient.Receive(ref serverEndPoint);

            if (data.Length == 0)
                continue;

            int packetSize = BitConverter.ToInt32(data, 0);
            short packetId = BitConverter.ToInt16(data, 4);

            switch ((PacketId)packetId)
            {
                case PacketId.S2CPlayerInfo:
                    // ��Ŷ ũ�⸸ŭ�� �����͸� ����
                    byte[] packetData = new byte[packetSize];
                    Buffer.BlockCopy(data, 6, packetData, 0, packetSize);

                    S2CPlayerInfoPacket playerInfoPac = Packet<S2CPlayerInfoPacket>.Deserialize(packetData);
                    PlayerInfo info = new PlayerInfo();
                    info.NickName = playerInfoPac.Nickname;
                    info.PosX = playerInfoPac.PosX;
                    info.PosY = playerInfoPac.PosY;
                    info.PosZ = playerInfoPac.PosZ;
                    info.ForX = playerInfoPac.ForX;
                    info.ForY = playerInfoPac.ForY;
                    info.ForZ = playerInfoPac.ForZ;
                    InGameManager.Instance.PlayerMoveQueue.Enqueue(info);
                    break;

                case PacketId.S2CVoice:
                    int nicknameLength = data.Length - packetSize - 6;
                    byte[] nicknameBytes = new byte[nicknameLength];

                    Array.Copy(data, 6, nicknameBytes, 0, data.Length - packetSize - 6);
                    string nick = Encoding.UTF8.GetString(nicknameBytes);

                    // ��Ŷ ũ�⸸ŭ�� �����͸� ����
                    byte[] voiceData = new byte[packetSize];
                    Buffer.BlockCopy(data, 6 + nicknameLength, voiceData, 0, packetSize);

                    PlayerVoice playerVoice = new PlayerVoice();
                    playerVoice.NickName = nick;
                    // ����Ʈ �迭�� float �迭�� ��ȯ
                    playerVoice.VoiceData = new float[packetSize / 4];
                    System.Buffer.BlockCopy(voiceData, 0, playerVoice.VoiceData, 0, voiceData.Length);
                    InGameManager.Instance.PlayerVoiceQueue.Enqueue(playerVoice);
                    break;
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (tcpSocket != null)
        {
            ClientDisconnectPacket disconnect = new ClientDisconnectPacket();
            byte[] sendData = new Packet<ClientDisconnectPacket>(disconnect).Serialize();
            PacketSend(sendData, PacketId.ClientDisconnect);

            tcpSocket.Close();
            tcpSocket.Dispose();

            runUdpThread = false;

            //PacketAnalysisThread.Join();
            //UdpSendThread.Join();
            //UdpReceiveThread.Join();
        }
    }
}
