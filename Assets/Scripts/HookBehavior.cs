using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class HookBehavior : MonoBehaviour
{
    private GameObject player;
    private CinemachineVirtualCamera virtualCam;

    private void Start()
    {
        player = FindObjectOfType<PlayerController>().transform.gameObject;
        virtualCam = FindObjectOfType<CinemachineVirtualCamera>();
        FishAI[] fishes = FindObjectsOfType<FishAI>();
        foreach (FishAI fish in fishes)
        {
            fish.hook = gameObject;
        }
    }

    private void Update()
    {
        HookThrown();
    }

    [Header("Hook Settings")]
    [SerializeField] private float waterHeight;
    [SerializeField] private float waterGravity;
    [SerializeField] private float waterDrag;
    [SerializeField] private float camZoomSpeed;
    [SerializeField] private float camMaxOrthoSize;
    [HideInInspector] public float hookDistance;
    public static bool inWater = false;


    void HookThrown()
    {
        if (transform.position.y < waterHeight && !inWater)
        {
            WaterHit();
        }

        if (transform.position.x - player.transform.position.x >= 5)
        {
            StartCoroutine(CameraController());
        }
    }

    IEnumerator CameraController()
    {
        virtualCam.GetCinemachineComponent<CinemachineFramingTransposer>().m_XDamping = 3;
        float distance = Mathf.Clamp(hookDistance * camMaxOrthoSize, 5, camMaxOrthoSize);
        float elapsed = 0f;

        while (elapsed < camZoomSpeed)
        {
            virtualCam.m_Lens.OrthographicSize = Mathf.Lerp(5, distance, elapsed / camZoomSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        virtualCam.m_Lens.OrthographicSize = distance;
        virtualCam.GetCinemachineComponent<CinemachineFramingTransposer>().m_XDamping = 20;
    }

    [Header("Ripple Settings")]
    [SerializeField] private GameObject ripplePrefab; // ripple prefab used to generate a ripple effect

    void WaterHit()
    {
        inWater = true;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        GetComponent<Rigidbody2D>().gravityScale = waterGravity;
        GetComponent<Rigidbody2D>().drag = waterDrag;
        GameObject ripple = Instantiate(ripplePrefab, new Vector3(transform.position.x, transform.position.y, 10), Quaternion.identity);
        GetComponent<DistanceJoint2D>().enabled = true;
        GetComponent<DistanceJoint2D>().connectedAnchor = player.GetComponent<PlayerController>().hookPosition.transform.position;
    }
}
