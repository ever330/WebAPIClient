using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
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
    private int ucpPort = 3001;
    private const string hostIp = "127.0.0.1";
    private byte[] recvBuffer;
    private const int MAXSIZE = 1024;

    private Socket TcpSocket;
    private StreamQueue SendQ;
    private StreamQueue RecvQ;

    private UdpClient udpSendClient;
    private UdpClient udpReceiveClient;

    private Thread PacketAnalysisThread;
    private Thread UdpSendThread;
    private Thread UdpReceiveThread;

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
        RecvQ = new StreamQueue(MAXSIZE * 4);
        SendQ = new StreamQueue(MAXSIZE * 4);
        recvBuffer = new byte[MAXSIZE];
        TcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        AddPlayers = new Queue<PlayerInfo>();

        FindPort();

        runUdpThread = false;
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
                // 해당 포트가 이미 사용 중인 경우 다음 포트로 이동
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ConnectToServer()
    {
        Debug.Log("게임 서버 접속 대기중");
        try
        {
            TcpSocket.BeginConnect(hostIp, tcpPort, new AsyncCallback(ConnectCallBack), TcpSocket);
            PacketAnalysisThread = new Thread(PacketAnalysis);
            PacketAnalysisThread.Start();

            if (SceneManager.GetActiveScene().name == "MainScene")
            {
                TitlePanel.SetActive(true);
                LoginPanel.SetActive(false);
                TryLoginImage.SetActive(false);
            }
        }
        catch (SocketException e)
        {
            Debug.Log("접속실패");
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

            Debug.Log("접속성공");

            tempSocket.EndConnect(IAR);
            TcpSocket = tempSocket;
            TcpSocket.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None,
                new AsyncCallback(OnReceiveCallBack), TcpSocket);

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
                Debug.Log("접속실패");
                ConnectToServer();
            }
        }
    }

    public void Receive()
    {
        TcpSocket.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None,
            new AsyncCallback(OnReceiveCallBack), TcpSocket);
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

            // 서버로부터 전송받은 데이터가 있을경우 데이터큐에 넣어준다.
            if (readSize != 0)
            {
                Debug.Log("데이터 수신");

                RecvQ.WriteData(recvBuffer, readSize);
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

        // 패킷의 총 사이즈를 바이트 배열로 변환
        byte[] sizeBytes = BitConverter.GetBytes(packet.Length);
        // 패킷의 아이디를 바이트 배열로 변환
        byte[] idBytes = BitConverter.GetBytes((short)packetId);

        // 총 사이즈와 내용물을 합칠 바이트 배열
        byte[] sendData = new byte[sizeBytes.Length + idBytes.Length + packet.Length];

        // 맨 앞에 사이즈 추가
        Array.Copy(sizeBytes, 0, sendData, 0, sizeBytes.Length);
        // 아이디 추가
        Buffer.BlockCopy(idBytes, 0, sendData, sizeBytes.Length, idBytes.Length);
        // 그 다음에 패킷 내용물 추가
        Buffer.BlockCopy(packet, 0, sendData, sizeBytes.Length + idBytes.Length, packet.Length);

        sendEventArgs.SetBuffer(sendData, 0, sendData.Length);

        bool pending = TcpSocket.SendAsync(sendEventArgs);

        if (!pending)
        {
            SendCompleted(null, sendEventArgs);
        }
    }

    private void SendCompleted(object sender, SocketAsyncEventArgs e)
    {
        //Debug.Log("패킷 전송 성공");
    }

    private void PacketAnalysis()
    {
        while (TcpSocket != null)
        {
            if (RecvQ.dataCnt == 0)
                continue;

            byte[] totalData = RecvQ.ReadData();

            int packetSize = BitConverter.ToInt32(totalData, 0);
            short packetId = BitConverter.ToInt16(totalData, 4);

            // 패킷 크기만큼의 데이터를 추출
            byte[] packetData = new byte[packetSize];
            Buffer.BlockCopy(totalData, 6, packetData, 0, packetSize);

            switch ((PacketId)packetId)
            {
                case PacketId.ResCreateRoom:
                    Nickname = TitleNicknameText.text;
                    ResCreateRoomPacket result = Packet<ResCreateRoomPacket>.Deserialize(packetData);
                    NowRoomNum = result.RoomNum;
                    //UdpStart();
                    TitleObj.isCreateRoom = true;
                    break;

                case PacketId.ResEnterRoom:
                    Nickname = TitleNicknameText.text;
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
                    Nickname = TitleNicknameText.text;
                    ResRoomPlayersPacket roomPlayer = Packet<ResRoomPlayersPacket>.Deserialize(packetData);
                    //Vector3 position = new Vector3 { x = roomPlayer.PosX, y = roomPlayer.PosY, z = roomPlayer.PosZ };
                    //Vector3 forward = new Vector3 { x = roomPlayer.ForX, y = roomPlayer.ForY, z = roomPlayer.ForZ };
                    //InGameManager.Instance.AddPlayer(roomPlayer.Nickname, position, forward);
                    PlayerInfo playerInfo = new PlayerInfo();
                    playerInfo.NickName = Encoding.UTF8.GetString(roomPlayer.Nickname);
                    int nullIndex = playerInfo.NickName.IndexOf('\0');
                    playerInfo.NickName.Substring(0, nullIndex);
                    Debug.Log(playerInfo.NickName + "추가");
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
                    lock (lockObject)
                    {
                        InGameManager.Instance.ChatQueue.Enqueue(new Tuple<string, string>(nickname.Substring(0, nicknameNullIndex), chat.Substring(0, chatNullIndex)));
                    }
                    break;

                case PacketId.S2CNewPlayer:
                    S2CNewPlayerPacket newPlayer = Packet<S2CNewPlayerPacket>.Deserialize(packetData);
                    string name = Encoding.UTF8.GetString(newPlayer.Nickname);
                    int nameNullIndex = name.IndexOf('\0');
                    name.Substring(0, nameNullIndex);
                    InGameManager.Instance.NewPlayerQueue.Enqueue(name);
                    break;

                //case PacketId.S2CPlayerInfo:
                //    if (CurrentSceneName == "InGame")
                //    {
                //        S2CPlayerInfoPacket playerInfoPac = Packet<S2CPlayerInfoPacket>.Deserialize(packetData);
                //        //Vector3 playerPos = new Vector3 { x = playerInfoPac.PosX, y = playerInfoPac.PosY, z = playerInfoPac.PosZ };
                //        //Vector3 playerFor = new Vector3 { x = playerInfoPac.ForX, y = playerInfoPac.ForY, z = playerInfoPac.ForZ };
                //        PlayerInfo info = new PlayerInfo();
                //        info.NickName = playerInfoPac.Nickname;
                //        info.PosX = playerInfoPac.PosX;
                //        info.PosY = playerInfoPac.PosY;
                //        info.PosZ = playerInfoPac.PosZ;
                //        info.ForX = playerInfoPac.ForX;
                //        info.ForY = playerInfoPac.ForY;
                //        info.ForZ = playerInfoPac.ForZ;
                //        InGameManager.Instance.PlayerMoveQueue.Enqueue(info);
                //        //InGameManager.Instance.PlayerMove(playerInfoPac.Nickname, playerPos, playerFor);
                //    }
                //    break;

                default:
                    break;
            }

            Thread.Sleep(100);
        }
    }

    public void UdpStart()
    {
        UdpSendThread = new Thread(SendUDP);
        UdpSendThread.Start();
        UdpReceiveThread = new Thread(ReceiveUDP);
        UdpReceiveThread.Start();

        runUdpThread = true;
    }

    private void SendUDP()
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

            // 패킷의 총 사이즈를 바이트 배열로 변환
            byte[] sizeBytes = BitConverter.GetBytes(pacData.Length);
            // 패킷의 아이디를 바이트 배열로 변환
            byte[] idBytes = BitConverter.GetBytes((short)PacketId.C2SPlayerInfo);

            // 총 사이즈와 내용물을 합칠 바이트 배열
            byte[] sendData = new byte[sizeBytes.Length + idBytes.Length + pacData.Length];

            // 맨 앞에 사이즈 추가
            Array.Copy(sizeBytes, 0, sendData, 0, sizeBytes.Length);
            // 아이디 추가
            Buffer.BlockCopy(idBytes, 0, sendData, sizeBytes.Length, idBytes.Length);
            // 그 다음에 패킷 내용물 추가
            Buffer.BlockCopy(pacData, 0, sendData, sizeBytes.Length + idBytes.Length, pacData.Length);

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), ucpPort);

            udpSendClient.Send(sendData, sendData.Length, serverEndPoint);

            Thread.Sleep(50);
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

            // 패킷 크기만큼의 데이터를 추출
            byte[] packetData = new byte[packetSize];
            Buffer.BlockCopy(data, 6, packetData, 0, packetSize);

            switch ((PacketId)packetId)
            {
                case PacketId.S2CPlayerInfo:
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
            }
        }
    }

    private void OnApplicationQuit()
    {
        //if (PacketAnalysisThread.IsAlive)
        //    PacketAnalysisThread.Join();

        if (TcpSocket != null)
        {
            ClientDisconnectPacket disconnect = new ClientDisconnectPacket();
            byte[] sendData = new Packet<ClientDisconnectPacket>(disconnect).Serialize();
            PacketSend(sendData, PacketId.ClientDisconnect);

            TcpSocket.Close();
            TcpSocket.Dispose();

            runUdpThread = false;

            //PacketAnalysisThread.Join();
            //UdpSendThread.Join();
            //UdpReceiveThread.Join();
        }
    }
}
