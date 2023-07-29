using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum FishState
{
    Rest,
    Chase,
    Hooked,
    Caught
}

public class FishAI : MonoBehaviour
{
    [Header("Scene info")]
    [SerializeField] private LayerMask hookLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private NavMeshAgent fishAgent;
    [SerializeField] private GameObject fishObject;
    private FishState currentState;
    [HideInInspector] public GameObject hook;
    private Transform hookTransform;

    private void Start()
    {
        ReturnToRest();
        StartCoroutine(DetectionDelay());
        angleIncrease = fovSize / rayCount;
        currentState = FishState.Rest;
        fishAgent.updateRotation = false;
        alertTime = Time.time + alertDelay;
        fishShader = fishObject.GetComponent<SpriteRenderer>().material;
        playerAnimator = FindObjectOfType<PlayerController>().transform.gameObject.GetComponent<Animator>();
    }

    private void Update()
    {
        Animator();
        if (hook != null)
        {
            hookTransform = hook.transform;
        }
        switch (currentState)
        {
            case FishState.Rest:
                Rest();
                break;

            case FishState.Chase:
                Chase();
                break;

            case FishState.Hooked:
                Hooked();
                break;

            case FishState.Caught:
                Caught();
                break;
        }
    }

    #region HookDetection

    [Header("Detection Zone Paramaters")]
    [SerializeField] private Transform detectorOrigin;
    [SerializeField] private float detectorRadius = 1f;
    [SerializeField] private float detectionDelay = 0.3f;
    private bool hookDetected;

    IEnumerator DetectionDelay()
    {
        yield return new WaitForSeconds(detectionDelay);
        Detection();
        View();
        StartCoroutine(DetectionDelay());
    }

    // this method allows the NavMesh Agent to check if the hook is around itself.
    // If so, it will shoot out a ray to check if obstacles are in its way.
    public void Detection() //Detection(Vector3 position, Float radius, string objectName)
    {
        hookDetected = false;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(detectorOrigin.position, detectorRadius);//...OverlapCircleAll(position, radius)
        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.CompareTag("Hook") == true) //CompareTag(objectName)
            {
                float distToHook = Vector2.Distance(transform.position, collider.transform.position);
                Vector2 dirToHook = (collider.transform.position - transform.position).normalized;
                RaycastHit2D ray = Physics2D.Raycast(transform.position, dirToHook, distToHook + .5f, obstacleLayer);
                if (ray.collider == null)
                {
                    hookDetected = true;
                }
            }
        }
    }

    #endregion

    #region FishFOV

    [Header("Field of View Parameters")]
    [SerializeField] private float fovSize = 90f;
    [SerializeField] private float viewDistance = 5f;
    [SerializeField] private int rayCount = 50;
    private float startingAngle = 0f;
    private float lastStartingAngle;
    private float angleIncrease;
    private bool hookSeen;

    public void View()
    {
        hookSeen = false;

        if (fishAgent.remainingDistance >= .01f)
        {
            WhatisStartingAngle(fishAgent.destination);
            if (currentState == FishState.Chase && hookTransform != null)
            {
                WhatisStartingAngle(hookTransform.position);
            }
        }
        else
            startingAngle = lastStartingAngle;

        for (int i = 0; i <= rayCount; i++)
        {
            RaycastHit2D ray = Physics2D.Raycast(transform.position, GetVectorFromAngle(startingAngle), viewDistance, hookLayer);
            Debug.DrawRay(transform.position, GetVectorFromAngle(startingAngle), Color.red, 2f);
            if (ray.collider != null && ray.collider.CompareTag("Hook") == true && hookDetected == true)
            {
                hookSeen = true;
                hookTransform = FindObjectOfType<HookBehavior>().transform;
            }
            startingAngle -= angleIncrease;
        }
    }

    void WhatisStartingAngle(Vector3 destination)
    {
        startingAngle = (GetAngleFromVector(destination - transform.position) - fovSize / 2f) + 90;
        lastStartingAngle = startingAngle;
    }
    #endregion

    #region Rest

    [Header("Rest Settings")]
    [SerializeField] private Transform restZoneCentre;
    [SerializeField] private float moveRange = 1f;
    [SerializeField] private float nextMoveTimeMin = 8f;
    [SerializeField] private float nextMoveTimeMax = 15f;
    [SerializeField] private float alertDelay = 1f;
    [HideInInspector] public float nextMoveTime;
    private bool isAlert = false;
    private float alertTime;
    private Vector3 lastHeardPosition;

    private void Rest()
    {
        if (hookSeen != true)
        {
            if (hookDetected != true)
            {
                if (fishAgent.remainingDistance <= fishAgent.stoppingDistance && nextMoveTime <= Time.time)
                {
                    FindRandomDestination();
                }
                if (alertTime < Time.time + 1)
                {
                    fishAgent.SetDestination((Vector2)lastHeardPosition);
                }
                alertTime = Time.time + alertDelay;
            }
            else if (hookTransform != null)
            {
                lastHeardPosition = hookTransform.position;
                if (alertTime >= Time.time)
                {
                    if (isAlert == false)
                    {
                        isAlert = true;
                    }
                }
                else
                    fishAgent.SetDestination((Vector2)lastHeardPosition);
            }
        }
        else
        {
            StartCoroutine(HookDetected());
            if (Vector3.Distance(hookTransform.position, transform.position) <= chaseRange + 1)
            {
                currentState = FishState.Chase;
            }
        }

        if (hookTransform != null && Vector3.Distance(hookTransform.position, transform.position) <= 1f)
        {
            StartCoroutine(HookDetected());
            currentState = FishState.Chase;
        }
    }

    IEnumerator HookDetected()
    {
        hookDetected = true;
        if (Vector3.Distance(hookTransform.position, transform.position) <= chaseRange + 1 && hookDetected == false)
        {
            fishAgent.SetDestination(hookTransform.position - (hookTransform.position - transform.position) / 4);
            yield return null;
        }
    }

    private void ReturnToRest()
    {
        FindRandomDestination();
        alertTime = Time.time + alertDelay;
        isAlert = false;
        hookDetected = false;
        currentState = FishState.Rest;
    }

    void FindRandomDestination()
    {
        Vector2 point;
        if (RandomPoint(restZoneCentre.position, moveRange, out point))
        {
            fishAgent.SetDestination(point);
            nextMoveTime = Random.Range(nextMoveTimeMin, nextMoveTimeMax) + Time.time;
        }
    }

    bool RandomPoint(Vector2 center, float range, out Vector2 result)
    {
        Vector2 randomPoint = center + Random.insideUnitCircle * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 1f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = Vector2.zero;
        return false;
    }

    #endregion

    #region Chase

    [Header("Chase Settings")]
    [SerializeField] private float chaseRange = 2f; // Range in which the enemy can attack the player
    [SerializeField] private float biteRange = .3f;
    [SerializeField] private float returnToRestTime = 7f;
    [SerializeField] private float returnToRestRange = 5f;
    private float biteChance;
    private float biteChanceMultiplier = 1f;
    private float lastSeenTime = 0f; // Time in seconds when the player was last seen
    private Coroutine chaseCoroutine;

    private void Chase()
    {
        if (Vector3.Distance(transform.position, hookTransform.position) <= biteRange && biteChance >= 2f)
        {
            currentState = FishState.Hooked;
            alertTime = Time.time;
            StopAllCoroutines();
        }

        if (chaseCoroutine == null)
        {
            chaseCoroutine = StartCoroutine(HookChase());
        }

        if (Vector3.Distance(hookTransform.position, transform.position) >= returnToRestRange)
        {
            ReturnToRest();
        }

        if (Vector3.Distance(hookTransform.position, transform.position) >= chaseRange)
        {
            if (lastSeenTime <= Time.time - returnToRestTime && hookSeen != true)
            {
                ReturnToRest();
            }
        }

        if (Vector3.Distance(hookTransform.position, transform.position) <= biteRange)
        {
            biteChanceMultiplier = 5f;
        }
        else
        {
            biteChanceMultiplier = 1f;
        }

        if (hookSeen)
        {
            lastSeenTime = Time.time;
            biteChance += Time.deltaTime * biteChanceMultiplier;
        }
    }

    IEnumerator HookChase()
    {
        fishAgent.SetDestination(hookTransform.position - (hookTransform.position - transform.position) / 2);
        yield return new WaitForSeconds(Random.Range(0f, 2f));
        StartCoroutine(HookChase());
    }

    #endregion

    #region Hooked

    [Header("Hooked Settings")]
    [SerializeField] private GameObject caneEnd;
    [SerializeField] private float hookedMinSpeed;
    [SerializeField] private float hookedMaxSpeed;
    [SerializeField] private float reelingSpeed;
    [SerializeField] private float YDestination;
    [SerializeField] private float YMin;
    [SerializeField] private float YMax;
    public bool fishHooked { get; private set; }
    private Coroutine escapeCoroutine;
    private Animator playerAnimator;
    private bool pulling;

    private void Hooked()
    {
        if (!fishHooked)
        {
            if (alertTime >= Time.time + 2f)
                ReturnToRest();
            fishAgent.speed = maxSpeed;
            fishAgent.SetDestination(hookTransform.position);
            if (Vector2.Distance(hookTransform.position, fishObject.transform.position) <= .1f)
            {
                fishHooked = true;
                playerAnimator.SetTrigger("Hooked");
            }
        }
        else
        {
            hook.transform.position = fishObject.transform.position + new Vector3(0, 0, 0.1f);

            if (Input.GetKey(KeyCode.Space))
            {
                float distanceToCaneX = Mathf.Abs(fishAgent.transform.position.x - caneEnd.transform.position.x + .5f);
                fishAgent.Move(new Vector3(-reelingSpeed * Mathf.Clamp(distanceToCaneX, 0, 1) * Time.deltaTime, reelingSpeed / 10 * Time.deltaTime, 0));
            }

            if (Input.GetKey(KeyCode.A) && !pulling)
            {
                pulling = true;
                playerAnimator.SetTrigger("Pull");
                StartCoroutine(PullFish());
            }

            if (escapeCoroutine == null)
            {
                escapeCoroutine = StartCoroutine(HookEscape());
            }

            if (Vector2.Distance(fishObject.transform.position, caneEnd.transform.position) <= 3f)
            {
                currentState = FishState.Caught;
                StopAllCoroutines();
            }
        }
    }

    IEnumerator PullFish()
    {
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            float distanceToCaneX = Mathf.Abs(fishAgent.transform.position.x - caneEnd.transform.position.x + .5f);
            fishAgent.Move(new Vector3(-.5f * Time.deltaTime + (-reelingSpeed * Mathf.Clamp(distanceToCaneX, 0, 1) * Time.deltaTime), -.15f * Time.deltaTime + (reelingSpeed / 10 * Time.deltaTime)));
            elapsed += Time.deltaTime;
            yield return null;
        }
        pulling = false;
    }

    IEnumerator HookEscape()
    {
        Vector3 newDestination = fishObject.transform.position + new Vector3(10f, Random.Range(-YDestination, YDestination));
        newDestination = new Vector3(newDestination.x, Mathf.Clamp(newDestination.y, YMin, YMax), newDestination.z);
        fishAgent.SetDestination(newDestination);
        yield return new WaitForSeconds(Random.Range(.5f, 1f));
        hookedSpeed = Random.Range(hookedMinSpeed, hookedMaxSpeed);
        StartCoroutine(HookEscape());
        yield return null;
    }

    #endregion

    #region Caught

    [Header("Caught Settings")]
    [SerializeField] private float caughtSpeed;
    [SerializeField] private GameObject catchingPos;
    private bool isBeingCaught;
    private bool isInHand;

    private void Caught()
    {
        if (hook != null)
            hook.transform.position = fishObject.transform.position + new Vector3(0, 0, 0.1f);

        if (!isBeingCaught)
        {
            isBeingCaught = true;
            fishAgent.enabled = false;
            playerAnimator.SetTrigger("Catching");
            StartCoroutine(CatchingAnimation());
        }

        if (isInHand)
        {
            fishObject.transform.parent.transform.position = catchingPos.transform.position;
        }
    }

    IEnumerator CatchingAnimation()
    {
        float elapsed = 0f;
        Vector3 startingPos = fishObject.transform.parent.transform.position;
        while (elapsed < caughtSpeed)
        {
            fishObject.transform.parent.transform.position = Vector3.Lerp(startingPos, catchingPos.transform.position, elapsed / caughtSpeed);
            fishObject.transform.rotation = Quaternion.Slerp(fishObject.transform.rotation, new Quaternion(0, 0, -0.707106829f, 0.707106829f), elapsed / caughtSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        fishObject.transform.parent.transform.position = catchingPos.transform.position;
        fishObject.transform.rotation = new Quaternion(0, 0, -0.707106829f, 0.707106829f);
        isInHand = true;
    }

    #endregion

    #region Animation

    [Header("Animation Settings")]
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed;
    private float hookedSpeed = .5f;
    [SerializeField] private float rotationSpeed;
    private Material fishShader;

    void Animator()
    {
        Vector3 dir = fishAgent.destination - transform.position;

        float dist = dir.magnitude;

        float speed = Mathf.Lerp(minSpeed, maxSpeed, dist / maxSpeed);

        if (currentState == FishState.Hooked && fishHooked)
        {
            speed = hookedSpeed;
        }

        fishAgent.speed = speed;

        if (fishAgent.enabled == true && fishAgent.remainingDistance > .1f)
        {
            Quaternion rot = Quaternion.LookRotation(Vector3.forward, dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
        }

        if (transform.rotation.z <= 0)
        {
            fishObject.GetComponent<SpriteRenderer>().flipY = true;
        }
        else
        {
            fishObject.GetComponent<SpriteRenderer>().flipY = false;
        }

        if (currentState == FishState.Hooked)
        {
            speed = maxSpeed;
        }

        if (currentState == FishState.Caught)
        {
            speed = 0;
        }

        fishShader.SetFloat("_Tail_Strength", .1f + ModifierValue(speed) / 40f);
        fishShader.SetFloat("_Tail_Speed", .1f + ModifierValue(speed) / 20f);
        fishShader.SetFloat("_Body_Strength", .1f + ModifierValue(speed) / 50f);
        fishShader.SetFloat("_Body_Speed", .1f + ModifierValue(speed) / 20f);
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

    #endregion

    #region Gizmos

    [Header("Gizmo Paramaters")]
    [SerializeField] private bool showGizmo = true;
    public Color gizmoIdleColor = Color.green;
    public Color gizmoDetectedColor = Color.red;
    public Color biteRangeColor = Color.magenta;
    public Color ChaseRangeColor = Color.yellow;
    public Color returnToRestColor = Color.cyan;

    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoIdleColor;
            if (hookDetected)
                Gizmos.color = gizmoDetectedColor;
            Gizmos.DrawWireSphere(detectorOrigin.position, detectorRadius);
            if (fishAgent != null)
                Gizmos.DrawLine(transform.position, fishAgent.destination);
            if (currentState == FishState.Rest)
            {
                Gizmos.color = Color.blue;
                if (restZoneCentre != null)
                    Gizmos.DrawWireSphere(restZoneCentre.transform.position, moveRange);
            }

            if (currentState == FishState.Chase)
            {
                Gizmos.color = biteRangeColor;
                Gizmos.DrawWireSphere((Vector2)transform.position, biteRange);
                Gizmos.color = ChaseRangeColor;
                Gizmos.DrawWireSphere((Vector2)transform.position, chaseRange);
                Gizmos.color = returnToRestColor;
                Gizmos.DrawWireSphere((Vector2)transform.position, returnToRestRange);
            }
        }
    }

    #endregion

    #region UtilityFunctions

    //returns a Vector3 using an angle in degrees, 0 being top middle
    private Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    //returns an angle in degrees using a Vector, Vector2.up being 0 degrees
    private float GetAngleFromVector(Vector2 vector)
    {
        vector = vector.normalized;
        float n = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        if (n < 0) n += 360;
        return n;
    }

    #endregion
}
