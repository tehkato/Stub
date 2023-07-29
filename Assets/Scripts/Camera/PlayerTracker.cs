using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerTracker : MonoBehaviour
{
    private GameObject cam;

    private void Start()
    {
        cam = FindObjectOfType<CinemachineVirtualCamera>().transform.gameObject;
    }

    void Update()
    {
        transform.position = new Vector3(cam.transform.position.x, transform.position.y, transform.position.z);
    }
}
