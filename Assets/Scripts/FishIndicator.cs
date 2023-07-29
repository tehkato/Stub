using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FishIndicator : MonoBehaviour
{
    [SerializeField] private GameObject fishIndicatorObject;
    [SerializeField] private GameObject parentFish;
    private Material fishShader;
    private NavMeshAgent parentAgent;

    private void Start()
    {
        fishShader = fishIndicatorObject.GetComponent<SpriteRenderer>().material;
        parentAgent = parentFish.GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        Animator();
    }

    [Header("Animation Settings")]
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float indicatorHeight;
    [SerializeField] private float indicatorDepthOpacity;

    void Animator()
    {
        Vector3 dir = parentAgent.destination - parentFish.transform.position;

        float dist = dir.magnitude;

        float speed = Mathf.Lerp(minSpeed, maxSpeed, dist / maxSpeed);

        if (parentAgent.enabled == true && parentAgent.remainingDistance > .1f)
        {
            Quaternion rot = Quaternion.LookRotation(Vector3.forward, dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
        }

        if (transform.rotation.z <= 0)
        {
            fishIndicatorObject.GetComponent<SpriteRenderer>().flipY = true;
        }
        else
        {
            fishIndicatorObject.GetComponent<SpriteRenderer>().flipY = false;
        }

        fishShader.SetFloat("_Tail_Strength", .1f + ModifierValue(speed) / 40f);
        fishShader.SetFloat("_Tail_Speed", .1f + ModifierValue(speed) / 20f);
        fishShader.SetFloat("_Body_Strength", .1f + ModifierValue(speed) / 50f);
        fishShader.SetFloat("_Body_Speed", .1f + ModifierValue(speed) / 20f);
        fishShader.SetFloat("_YPosition", -transform.position.y + indicatorHeight);
        fishShader.SetFloat("_Opacity", 1 - Mathf.Clamp(-parentFish.transform.position.y / indicatorDepthOpacity, 0f, 1f));
        if (parentFish.transform.position.y >= indicatorHeight)
        {
            fishShader.SetFloat("_Opacity", 0);
        }

        transform.position = new Vector3(parentFish.transform.position.x, 0, 0);
    }

    float ModifierValue(float speed)
    {
        if (speed < 1f)
            return 1f;
        if (speed >= 1f && speed < 5f)
            return 4f;
        if (speed >= 5f && speed < 9f)
            return 8f;
        else
            return 10f;
    }
}
