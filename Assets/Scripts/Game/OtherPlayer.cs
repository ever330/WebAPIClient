using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    private CharacterController cc;
    public Vector3 TargetPosition { get; set; }
    public Vector3 TargetForward { get; set; }

    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float gravity = -20;

    private float rotationInterpolationSpeed = 8f;

    // Start is called before the first frame update
    void Start()
    {
        cc = gameObject.GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        //float distance = Vector3.Distance(gameObject.transform.position, TargetPosition);
        //if (distance < 1.0f)
        //{
        //    Vector3 direction = (TargetPosition - transform.position).normalized;
        //    cc.Move(direction * moveSpeed * Time.deltaTime);
        //}

        //Vector3 direction = (TargetPosition - transform.position).normalized;
        //cc.Move(direction * moveSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, TargetPosition, moveSpeed * Time.deltaTime);
        transform.forward = Vector3.Lerp(transform.forward, TargetForward, rotationInterpolationSpeed * Time.deltaTime);
        

        //if (gameObject.transform.forward != TargetForward)
        //{
        //    gameObject.transform.forward = TargetForward;
        //}
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
}
