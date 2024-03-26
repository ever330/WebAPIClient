using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WebServerLogin : MonoBehaviour
{
    public GameObject IdCheckResult;            // 아이디 중복확인 결과

    public TMP_InputField IdInput;              // 로그인 아이디 입력
    public TMP_InputField PwInput;              // 로그인 비밀번호 입력

    public TMP_InputField SignupIdInput;        // 회원가입 아이디 입력
    public TMP_InputField SignupPwInput;        // 회원가입 비밀번호 입력
    public TMP_InputField SignupNicknameInput;  // 회원가입 닉네임 입력

    public TextMeshProUGUI IdCheckResultText;   // 아이디 중복확인 결과 텍스트

    public GameObject PWCheckText;              // 비밀번호 일치 확인 텍스트

    private ClientNetwork clientNetwork;             // 웹 서버를 통한 로그인 성공시 게임서버 접속 시도
    public TextMeshProUGUI TitleNicknameText;
    public GameObject TryLoginImage;
    public GameObject LoginFailImage;

    private bool hasIdCheck;                    // 아이디 중복확인 여부

    private bool isInnerNet = true;

    // Start is called before the first frame update
    void Start()
    {
        hasIdCheck = false;
        clientNetwork = ClientNetwork.Instance;

        IPAddress[] localIpAddresses = Dns.GetHostAddresses(Dns.GetHostName()); // 로컬 네트워크의 IP 주소 목록 가져오기

        foreach (IPAddress localIpAddress in localIpAddresses)
        {
            if (localIpAddress.ToString() != "172.30.1.100")
                isInnerNet = false;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoginBtnClicked()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // 인터넷 연결이 안된경우
            Debug.Log("인터넷 연결 에러");
        }
        else
        {
            // 인터넷 연결이 된 경우
            StartCoroutine(TryLogin());
        }
    }

    IEnumerator TryLogin()
    {
        string url = $"https://localhost:44341/api/Users?id={IdInput.text}&password={PwInput.text}";

        if (isInnerNet)
        {
            // 외부 접속 처리용 포트 및 IP 변경
        }

        UnityWebRequest www = UnityWebRequest.Get(url);
        TryLoginImage.SetActive(true);

        yield return www.SendWebRequest();  // 응답 대기

        if (www.error == null)
        {
            if (www.downloadHandler.text == "PW Error")
            {
                LoginFailImage.SetActive(true);
            }

            Debug.Log(www.downloadHandler.text);    // 데이터 출력
            UserInfo.Instance.Username = www.downloadHandler.text;
            TitleNicknameText.text = www.downloadHandler.text;
            clientNetwork.ConnectToServer();
            clientNetwork.Nickname = www.downloadHandler.text;
        }
        else
        {
            Debug.Log("error");
        }
    }

    public void IdCheckBtnClicked()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // 인터넷 연결이 안된경우
            Debug.Log("인터넷 연결 에러");
        }
        else
        {
            // 인터넷 연결이 된 경우
            StartCoroutine(TryIdCheck());
        }
    }

    IEnumerator TryIdCheck()
    {
        string url = $"https://localhost:44341/Users/GetUserId?Id={SignupIdInput.text}";
        
        if (isInnerNet)
        {
            // 외부 접속 처리용 포트 및 IP 변경
        }

        UnityWebRequest www = UnityWebRequest.Get(url);

        yield return www.SendWebRequest();  // 응답 대기

        if (www.error == null)
        {
            if (www.downloadHandler.text == "false")
            {
                IdCheckResultText.text = "아이디 중복";
                hasIdCheck = false;
            }
            else
            {
                IdCheckResultText.text = "아이디 사용 가능";
                hasIdCheck = true;
            }
            IdCheckResult.SetActive(true);
        }
        else
        {
            Debug.Log("error");
        }
    }

    public void SignupBtnClicked()
    {
        if (!hasIdCheck)
        {
            IdCheckResultText.text = "아이디 중복 확인 필요";
            IdCheckResult.SetActive(true);
            return;
        }

        if (PWCheckText.activeSelf)
        {
            IdCheckResultText.text = "비밀번호가 일치하지 않습니다.";
            IdCheckResult.SetActive(true);
            return;
        }
        
        if (SignupNicknameInput.text == "")
        {
            IdCheckResultText.text = "닉네임을 입력해주세요.";
            IdCheckResult.SetActive(true);
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // 인터넷 연결이 안된경우
            Debug.Log("인터넷 연결 에러");
        }
        else
        {
            // 인터넷 연결이 된 경우
            StartCoroutine(TrySignup());
        }
    }

    IEnumerator TrySignup()
    {
        string url = $"https://localhost:44341/api/Users";
        
        if (isInnerNet)
        {
            // 외부 접속 처리용 포트 및 IP 변경
        }

        WWWForm form = new WWWForm();
        form.AddField("Id", SignupIdInput.text);
        form.AddField("Password", SignupPwInput.text);
        form.AddField("Nickname", SignupNicknameInput.text);

        UnityWebRequest www = UnityWebRequest.Post(url, form);

        yield return www.SendWebRequest();  // 응답 대기

        if (www.error == null)
        {
            if (www.downloadHandler.text == "false")
            {
                IdCheckResultText.text = "아이디 중복";
            }
            else
            {
                IdCheckResultText.text = "아이디 사용 가능";
            }
            IdCheckResult.SetActive(true);
        }
        else
        {
            Debug.Log("error");
        }
    }
}
