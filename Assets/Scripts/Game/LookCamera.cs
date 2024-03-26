using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookCamera : MonoBehaviour
{
    private GameObject cam;


    void Start()
    {

    }

    void Update()
    {
        if (cam == null)
        {
            cam = GameObject.FindWithTag("Player").transform.GetChild(2).gameObject;
        }

        transform.rotation = cam.transform.rotation;
    }
}
