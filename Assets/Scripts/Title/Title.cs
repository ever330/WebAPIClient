using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title : MonoBehaviour
{
    [SerializeField] private GameObject SignupPanel;      // ȸ������ �г�
    [SerializeField] private GameObject IdCheckResult;    // ���̵� �ߺ�Ȯ�� ���â
    [SerializeField] private TMP_InputField SignupPW;         // ȸ������ â ��й�ȣ
    [SerializeField] private TMP_InputField SignupPWCheck;    // ȸ������ â ��й�ȣ Ȯ��
    [SerializeField] private GameObject PWCheckText;      // ��й�ȣ ��ġ Ȯ�� �ؽ�Ʈ

    [SerializeField] private GameObject RoomCreateWaitImg;
    [SerializeField] private GameObject RoomEnterWaitImg;
    [SerializeField] private GameObject EnterRoomPanel;
    [SerializeField] private TMP_InputField EnterRoomNum;
    [SerializeField] private GameObject RoomEnterResult;

    private ClientNetwork clientNetwork;

    private bool isWaitingServer;

    public bool isCreateRoom { get; set; }
    public bool isEnterRoom { get; set; }
    public bool isEnterFail { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        clientNetwork = ClientNetwork.Instance;
        isWaitingServer = false;
        isCreateRoom = false;
        isEnterRoom = false;
        isEnterFail = false;

        clientNetwork.CurrentSceneName = "MainScene";

    }

    // Update is called once per frame
    void Update()
    {
        if (SignupPW.text != SignupPWCheck.text && !PWCheckText.activeSelf)
        {
            PWCheckText.SetActive(true);
        }
        else if (SignupPW.text == SignupPWCheck.text && PWCheckText.activeSelf)
        {
            PWCheckText.SetActive(false);
        }

        if (isWaitingServer && isCreateRoom)
        {
            isWaitingServer = false;
            isCreateRoom = false;
            RoomCreateWaitImg.SetActive(false);
            LoadSceneManager.LoadScene("InGame");
            //SceneManager.LoadScene("InGame");
        }

        if (isWaitingServer && isEnterRoom)
        {
            isWaitingServer = false;
            isEnterRoom = false;
            EnterRoomPanel.SetActive(false);
            LoadSceneManager.LoadScene("InGame");
        }

        if (isWaitingServer && isEnterFail)
        {
            isWaitingServer = false;
            RoomEnterWaitImg.SetActive(false);
            RoomEnterResult.SetActive(true);
        }
    }

    public void OpenSignupPanel()
    {
        SignupPanel.SetActive(true);
    }

    public void CloseSignupPanel()
    {
        SignupPanel.SetActive(false);
    }

    public void CloseIdCheckResult()
    {
        IdCheckResult.SetActive(false);
    }

    public void CreateRoomBtnClicked()
    {
        ReqCreateRoomPacket reqPac = new ReqCreateRoomPacket();
        byte[] sendData = new Packet<ReqCreateRoomPacket>(reqPac).Serialize();

        isWaitingServer = true;
        RoomCreateWaitImg.SetActive(true);
        ClientNetwork.Instance.PacketSend(sendData, PacketId.ReqCreateRoom);
    }

    public void EnterRoomBtnClicked()
    {
        ReqEnterRoomPacket reqPac = new ReqEnterRoomPacket();
        reqPac.RoomNum = Convert.ToInt32(EnterRoomNum.text);
        ClientNetwork.Instance.NowRoomNum = reqPac.RoomNum;
        byte[] sendData = new Packet<ReqEnterRoomPacket>(reqPac).Serialize();

        isWaitingServer = true;
        RoomEnterWaitImg.SetActive(true);
        ClientNetwork.Instance.PacketSend(sendData, PacketId.ReqEnterRoom);
        EnterRoomNum.text = "";
    }
}
