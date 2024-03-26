using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    private CharacterController cc;
    public Vector3 TargetPosition { get; set; }
    public Vector3 TargetForward { get; set; }

    [SerializeField] private float moveSpeed = 10f;

    private float rotationInterpolationSpeed = 8f;

    private List<float> voiceBuffer1 = new List<float>();
    private List<float> voiceBuffer2 = new List<float>();

    private int voiceBufferNum = 1;

    private int FREQUENCY = 44100; // 샘플링 주파수
    private float timer = 0f;
    private float playInterval = 0.5f; // 재생 간격 (초)

    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        cc = gameObject.GetComponent<CharacterController>();
        TargetPosition = InGameManager.Instance.SpawnPoint.position;
        TargetForward = InGameManager.Instance.SpawnPoint.forward;
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, TargetPosition, moveSpeed * Time.deltaTime);
        transform.forward = Vector3.Lerp(transform.forward, TargetForward, rotationInterpolationSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        // 음성 데이터를 일정 간격마다 재생
        if (timer >= playInterval)
        {
            if (voiceBuffer1.Count >= FREQUENCY * 0.5 && voiceBufferNum == 2)
            {
                // AudioClip 생성
                AudioClip audioClip = AudioClip.Create("ReceivedAudio", voiceBuffer1.Count, 1, FREQUENCY, false);
                audioClip.SetData(voiceBuffer1.ToArray(), 0);

                // AudioSource에 할당하여 재생
                audioSource.clip = audioClip;
                audioSource.Play();

                // 버퍼 비우기
                voiceBuffer1.Clear();
            }
            else if (voiceBuffer2.Count >= FREQUENCY * 0.5 && voiceBufferNum == 1)
            {
                // AudioClip 생성
                AudioClip audioClip = AudioClip.Create("ReceivedAudio", voiceBuffer2.Count, 1, FREQUENCY, false);
                audioClip.SetData(voiceBuffer2.ToArray(), 0);

                // AudioSource에 할당하여 재생
                audioSource.clip = audioClip;
                audioSource.Play();

                // 버퍼 비우기
                voiceBuffer2.Clear();
            }

            timer = 0f;
        }
    }

    public void PlayerMove(Vector3 playerPos, Vector3 playerForward)
    {
        Vector3 direction = (playerPos - transform.position).normalized;
        cc.Move(direction * moveSpeed * Time.deltaTime);
        if (gameObject.transform.forward != playerForward)
        {
            gameObject.transform.forward = playerForward;
        }
    }

    // 음성 데이터를 버퍼에 추가하는 함수
    public void AddVoiceData(float[] newData)
    {
        // AudioClip 생성
        //AudioClip audioClip = AudioClip.Create("ReceivedAudio", newData.Length, 1, FREQUENCY, false);
        //audioClip.SetData(newData, 0);

        //// AudioSource에 할당하여 재생
        //audioSource.clip = audioClip;
        //audioSource.Play();

        if (voiceBufferNum == 1)
        {
            voiceBuffer1.AddRange(newData);
            if (voiceBuffer1.Count >= FREQUENCY * playInterval)
                voiceBufferNum = 2;
        }
        else if (voiceBufferNum == 2)
        {
            voiceBuffer2.AddRange(newData);
            if (voiceBuffer2.Count >= FREQUENCY * playInterval)
                voiceBufferNum = 1;
        }
    }
}
