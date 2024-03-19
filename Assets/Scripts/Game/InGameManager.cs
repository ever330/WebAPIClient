using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameManager : MonoBehaviour
{
    private static InGameManager instance = null;

    private readonly object lockObject = new object();

    private GameObject User;
    [SerializeField] private TextMeshProUGUI RoomNumText;
    [SerializeField] private TextMeshProUGUI NicknameText;

    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private GameObject OtherPlayerPrefab;

    [SerializeField] public Transform SpawnPoint;

    [SerializeField] private GameObject ChatBox;
    [SerializeField] private TMP_InputField ChatInputField;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject ChatPrefab;
    [SerializeField] private Transform ContentBox;

    [SerializeField] private GameObject OptionBox;

    public bool isUIMode;

    public Queue<Tuple<string, string>> ChatQueue;

    private Dictionary<string, GameObject> Players;

    public Queue<string> NewPlayerQueue;
    public Queue<PlayerInfo> PlayerMoveQueue;

    void Awake()
    {
        if (null == instance)
        {
            instance = this;

            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public static InGameManager Instance
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
        isUIMode = false;
        User = Instantiate(PlayerPrefab);
        User.transform.position = SpawnPoint.position;
        User.transform.forward = SpawnPoint.forward;
        Debug.Log(SpawnPoint.forward.x);
        Debug.Log(SpawnPoint.forward.y);
        Debug.Log(SpawnPoint.forward.z);
        ChatQueue = new Queue<Tuple<string, string>>();
        RoomNumText.text = Convert.ToString(ClientNetwork.Instance.NowRoomNum);
        NicknameText.text = ClientNetwork.Instance.Nickname;
        Players = new Dictionary<string, GameObject>();
        NewPlayerQueue = new Queue<string>();
        PlayerMoveQueue = new Queue<PlayerInfo>();

        ReqRoomPlayersPacket reqPlayers = new ReqRoomPlayersPacket();
        reqPlayers.RoomNum = ClientNetwork.Instance.NowRoomNum;
        byte[] sendData = new Packet<ReqRoomPlayersPacket>(reqPlayers).Serialize();
        ClientNetwork.Instance.PacketSend(sendData, PacketId.ReqRoomPlayers);

        ClientNetwork.Instance.CurrentSceneName = "InGame";
        ClientNetwork.Instance.UdpStart();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (ChatBox.activeSelf)
            {
                SendChat(ChatInputField.text);
                ChatInputField.text = "";
                ChatBox.SetActive(false);
                isUIMode = false;
            }
            else
            {
                ChatBox.SetActive(true);
                isUIMode = true;
                ChatInputField.Select();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (OptionBox.activeSelf)
            {
                OptionBox.SetActive(false);
                isUIMode = false;
            }
            else
            {
                OptionBox.SetActive(true);
                isUIMode = true;
            }
        }


        if (ChatQueue.Count != 0)
        {
            ShowChat();
        }

        if (NewPlayerQueue.Count != 0)
        {
            NewPlayerIn(NewPlayerQueue.Dequeue());
        }

        if (ClientNetwork.Instance.AddPlayers.Count != 0)
        {
            PlayerInfo tempInfo = ClientNetwork.Instance.AddPlayers.Dequeue();
            Vector3 position = new Vector3 { x = tempInfo.PosX, y = tempInfo.PosY, z = tempInfo.PosZ };
            Vector3 forward = new Vector3 { x = tempInfo.ForX, y = tempInfo.ForY, z = tempInfo.ForZ };
            AddPlayer(tempInfo.NickName, position, forward);
        }

        if (PlayerMoveQueue.Count != 0)
        {
            PlayerInfo info = PlayerMoveQueue.Dequeue();
            PlayerMove(info.NickName, new Vector3 { x = info.PosX, y = info.PosY, z = info.PosZ }, new Vector3 { x = info.ForX, y = info.ForY, z = info.ForZ });
        }
    }

    private void SendChat(string chat)
    {
        if (chat == "")
            return;

        C2SEchoChat chatPacket = new C2SEchoChat();
        chatPacket.Chat = chat;
        byte[] sendData = new Packet<C2SEchoChat>(chatPacket).Serialize();
        ClientNetwork.Instance.PacketSend(sendData, PacketId.C2SEchoChat);
    }

    public void ShowChat()
    {
        lock (lockObject)
        {
            Tuple<string, string> tempChat = ChatQueue.Dequeue();
            //string nickname = tempChat.Item1;
            //string chat = tempChat.Item2;
            string chatBox = string.Format("{0} : {1}", tempChat.Item1.Trim(), tempChat.Item2.Trim());
            GameObject newChat = Instantiate(ChatPrefab, ContentBox);
            newChat.GetComponent<TextMeshProUGUI>().text = chatBox;

            if (ContentBox.childCount > 10)
            {
                Destroy(ContentBox.GetChild(0).gameObject);
            }
            scrollRect.verticalNormalizedPosition = 0.0f;
        }
    }

    public void NewPlayerIn(string nickname)
    {
        GameObject newPlayer = Instantiate(OtherPlayerPrefab);
        newPlayer.transform.position = SpawnPoint.position;
        newPlayer.transform.forward = SpawnPoint.forward;

        Players.Add(nickname, newPlayer);

        lock (lockObject)
        {
            GameObject newChat = Instantiate(ChatPrefab, ContentBox);
            newChat.GetComponent<TextMeshProUGUI>().text = string.Format("{0}님이 입장하셨습니다.", nickname);

            if (ContentBox.childCount > 10)
            {
                Destroy(ContentBox.GetChild(0).gameObject);
            }
            scrollRect.verticalNormalizedPosition = 0.0f;
        }
    }

    public void PlayerMove(string nickname, Vector3 playerPos, Vector3 playerForward)
    {
        Debug.Log(nickname + "이동");
        if (Players.ContainsKey(nickname))
        {
            GameObject player = Players[nickname];
            player.GetComponent<OtherPlayer>().TargetPosition = playerPos;
            player.GetComponent<OtherPlayer>().TargetForward = playerForward;
            //player.GetComponent<OtherPlayer>().PlayerMove(playerPos, playerForward);
        }
    }

    public void AddPlayer(string nickname, Vector3 playerPos, Vector3 playerForward)
    {
        GameObject player = Instantiate(OtherPlayerPrefab);
        player.GetComponent<OtherPlayer>().TargetPosition = playerPos;
        player.GetComponent<OtherPlayer>().TargetForward = playerForward;
        Players.Add(nickname, player);
    }

    public void GoTitleBtnClicked()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void GameQuitBtnClicked()
    {
        Application.Quit();
    }
}
