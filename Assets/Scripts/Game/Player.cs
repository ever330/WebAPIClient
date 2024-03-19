using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Vector3 dir;

    private CharacterController cc;
    private Transform cameraTransform;

    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float gravity = -20;

    private float yVelocity = 0;

    [SerializeField] private float mouseSpeed = 8f;
    private float mouseX = 0f; //좌우 회전값을 담을 변수
    private float mouseY = 0f; //위아래 회전값을 담을 변수

    private Thread playerInfoSendThread;

    private float infoSendTimer;
    private static float sendTime = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        cameraTransform = transform.GetChild(2).transform;
        //ThreadPool.QueueUserWorkItem(PlayerInfoSend);
        //PlayerInfoSendThread = new Thread(PlayerInfoSend);
        //PlayerInfoSendThread.Start();
        infoSendTimer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (!InGameManager.Instance.isUIMode)
        {
            mouseX += Input.GetAxis("Mouse X") * mouseSpeed;
            mouseY += Input.GetAxis("Mouse Y") * mouseSpeed;

            mouseY = Mathf.Clamp(mouseY, -50f, 30f);
            cameraTransform.localEulerAngles = new Vector3(-mouseY, 0, 0);
            this.transform.localEulerAngles = new Vector3(0, mouseX, 0);
        }

        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        dir = new Vector3(h, 0, v) * moveSpeed;
        dir = cc.transform.TransformDirection(dir);
        // 캐릭터가 지면에 있는 경우
        if (cc.isGrounded)
        {
            yVelocity = 0;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                yVelocity = jumpSpeed;
            }
        }

        yVelocity += (gravity * Time.deltaTime);
        dir.y = yVelocity;
        cc.Move(dir * Time.deltaTime);

        ClientNetwork.Instance.PlayerPosX = transform.position.x;
        ClientNetwork.Instance.PlayerPosY = transform.position.y;
        ClientNetwork.Instance.PlayerPosZ = transform.position.z;
        ClientNetwork.Instance.PlayerForX = transform.forward.x;
        ClientNetwork.Instance.PlayerForY = transform.forward.y;
        ClientNetwork.Instance.PlayerForZ = transform.forward.z;

        //infoSendTimer += Time.deltaTime;
        //if (dir != Vector3.zero && infoSendTimer >= sendTime)
        //{
        //    PlayerInfoSend();
        //}
    }

    private void PlayerInfoSend()
    {
        C2SPlayerInfoPacket infoPacket = new C2SPlayerInfoPacket();
        infoPacket.Nickname = ClientNetwork.Instance.Nickname;
        infoPacket.RoomNum = ClientNetwork.Instance.NowRoomNum;
        infoPacket.PosX = transform.position.x;
        infoPacket.PosY = transform.position.y;
        infoPacket.PosZ = transform.position.z;
        infoPacket.ForX = transform.forward.x;
        infoPacket.ForY = transform.forward.y;
        infoPacket.ForZ = transform.forward.z;
        byte[] sendData = new Packet<C2SPlayerInfoPacket>(infoPacket).Serialize();
        //Debug.Log("내 위치 정보 전송");
        ClientNetwork.Instance.PacketSend(sendData, PacketId.C2SPlayerInfo);
        infoSendTimer = 0;
    }
}
