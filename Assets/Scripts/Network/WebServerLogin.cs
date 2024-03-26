using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WebServerLogin : MonoBehaviour
{
    public GameObject IdCheckResult;            // ���̵� �ߺ�Ȯ�� ���

    public TMP_InputField IdInput;              // �α��� ���̵� �Է�
    public TMP_InputField PwInput;              // �α��� ��й�ȣ �Է�

    public TMP_InputField SignupIdInput;        // ȸ������ ���̵� �Է�
    public TMP_InputField SignupPwInput;        // ȸ������ ��й�ȣ �Է�
    public TMP_InputField SignupNicknameInput;  // ȸ������ �г��� �Է�

    public TextMeshProUGUI IdCheckResultText;   // ���̵� �ߺ�Ȯ�� ��� �ؽ�Ʈ

    public GameObject PWCheckText;              // ��й�ȣ ��ġ Ȯ�� �ؽ�Ʈ

    private ClientNetwork clientNetwork;             // �� ������ ���� �α��� ������ ���Ӽ��� ���� �õ�
    public TextMeshProUGUI TitleNicknameText;
    public GameObject TryLoginImage;
    public GameObject LoginFailImage;

    private bool hasIdCheck;                    // ���̵� �ߺ�Ȯ�� ����

    private bool isInnerNet = true;

    // Start is called before the first frame update
    void Start()
    {
        hasIdCheck = false;
        clientNetwork = ClientNetwork.Instance;

        IPAddress[] localIpAddresses = Dns.GetHostAddresses(Dns.GetHostName()); // ���� ��Ʈ��ũ�� IP �ּ� ��� ��������

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
            // ���ͳ� ������ �ȵȰ��
            Debug.Log("���ͳ� ���� ����");
        }
        else
        {
            // ���ͳ� ������ �� ���
            StartCoroutine(TryLogin());
        }
    }

    IEnumerator TryLogin()
    {
        string url = $"https://localhost:44341/api/Users?id={IdInput.text}&password={PwInput.text}";

        if (isInnerNet)
        {
            // �ܺ� ���� ó���� ��Ʈ �� IP ����
        }

        UnityWebRequest www = UnityWebRequest.Get(url);
        TryLoginImage.SetActive(true);

        yield return www.SendWebRequest();  // ���� ���

        if (www.error == null)
        {
            if (www.downloadHandler.text == "PW Error")
            {
                LoginFailImage.SetActive(true);
            }

            Debug.Log(www.downloadHandler.text);    // ������ ���
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
            // ���ͳ� ������ �ȵȰ��
            Debug.Log("���ͳ� ���� ����");
        }
        else
        {
            // ���ͳ� ������ �� ���
            StartCoroutine(TryIdCheck());
        }
    }

    IEnumerator TryIdCheck()
    {
        string url = $"https://localhost:44341/Users/GetUserId?Id={SignupIdInput.text}";
        
        if (isInnerNet)
        {
            // �ܺ� ���� ó���� ��Ʈ �� IP ����
        }

        UnityWebRequest www = UnityWebRequest.Get(url);

        yield return www.SendWebRequest();  // ���� ���

        if (www.error == null)
        {
            if (www.downloadHandler.text == "false")
            {
                IdCheckResultText.text = "���̵� �ߺ�";
                hasIdCheck = false;
            }
            else
            {
                IdCheckResultText.text = "���̵� ��� ����";
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
            IdCheckResultText.text = "���̵� �ߺ� Ȯ�� �ʿ�";
            IdCheckResult.SetActive(true);
            return;
        }

        if (PWCheckText.activeSelf)
        {
            IdCheckResultText.text = "��й�ȣ�� ��ġ���� �ʽ��ϴ�.";
            IdCheckResult.SetActive(true);
            return;
        }
        
        if (SignupNicknameInput.text == "")
        {
            IdCheckResultText.text = "�г����� �Է����ּ���.";
            IdCheckResult.SetActive(true);
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // ���ͳ� ������ �ȵȰ��
            Debug.Log("���ͳ� ���� ����");
        }
        else
        {
            // ���ͳ� ������ �� ���
            StartCoroutine(TrySignup());
        }
    }

    IEnumerator TrySignup()
    {
        string url = $"https://localhost:44341/api/Users";
        
        if (isInnerNet)
        {
            // �ܺ� ���� ó���� ��Ʈ �� IP ����
        }

        WWWForm form = new WWWForm();
        form.AddField("Id", SignupIdInput.text);
        form.AddField("Password", SignupPwInput.text);
        form.AddField("Nickname", SignupNicknameInput.text);

        UnityWebRequest www = UnityWebRequest.Post(url, form);

        yield return www.SendWebRequest();  // ���� ���

        if (www.error == null)
        {
            if (www.downloadHandler.text == "false")
            {
                IdCheckResultText.text = "���̵� �ߺ�";
            }
            else
            {
                IdCheckResultText.text = "���̵� ��� ����";
            }
            IdCheckResult.SetActive(true);
        }
        else
        {
            Debug.Log("error");
        }
    }
}
