using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    private float currentHorizontalSpeed;
    [HideInInspector] public bool isFacingRight;

    private void Start()
    {
        isFacingRight = true;
    }

    void Update()
    {
        Walk();
        Animator();
        CircleIndicator();
        FishingAnimation();
    }

    private void FixedUpdate()
    {
        if (canMove)
            TurnCheck();
    }

    #region WalkHandler

    [Header("WALKING")] [SerializeField] private float acceleration = 90;
    [SerializeField] private float moveClamp = 13;
    [SerializeField] private float deAcceleration = 60f;
    private bool canMove = true;

    private void Walk()
    {
        if (Input.GetAxisRaw("Horizontal") != 0 && canMove)
        {
            // Set horizontal move speed
            currentHorizontalSpeed += Input.GetAxisRaw("Horizontal") * acceleration * Time.deltaTime;

            // clamped by max frame movement
            currentHorizontalSpeed = Mathf.Clamp(currentHorizontalSpeed, -moveClamp, moveClamp);
        }
        else
        {
            // No input. Let's slow the character down
            currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, 0, deAcceleration * Time.deltaTime);
        }

        var move = new Vector3(currentHorizontalSpeed, 0) * Time.deltaTime;
        transform.position += move;
    }

    #endregion

    #region TurnHandler

    [SerializeField] private GameObject cameraTarget;

    private void TurnCheck()
    {
        if (Input.GetAxisRaw("Horizontal") > 0 && !isFacingRight)
        {
            //flips sprite from left to right
            Turn();
        }
        else if (Input.GetAxisRaw("Horizontal") < 0 && isFacingRight)
        {
            //flips sprite from right to left
            Turn();
        }
    }

    private void Turn()
    {
        //changes the character rotation for better camera controls
        if (isFacingRight)
        {
            Vector3 rot = new Vector3(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rot);
            cameraTarget.transform.rotation = Quaternion.Euler(rot);
            isFacingRight = !isFacingRight;
        }
        else
        {
            Vector3 rot = new Vector3(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rot);
            cameraTarget.transform.rotation = Quaternion.Euler(rot);
            isFacingRight = !isFacingRight;
        }
    }

    #endregion

    #region Animation
    [Header("ANIMATION")] [SerializeField] private Animator animator;

    private void Animator()
    {
        if (canMove)
            animator.SetFloat("Speed", Mathf.Abs(Input.GetAxis("Horizontal")));
    }

    #endregion

    #region CircleIndicator

    [Header("Circle Indicator")] [SerializeField] private Material shader; // shader affecting the circle indicator
    [SerializeField, Range(1, 5)] private float speed;
    private float modifier; // the modifier used to affect all the values of the shader
    [SerializeField] private float maxTime; // the maximum value of the modifier
    [SerializeField] AnimationCurve curve; // the curve at which the modifier will be affected through time
    private float radiusMultiplier = 4f; // the multiplier to convert the modifier to the radius value
    private float angleMultiplier = 20f; // the multiplier to convert the modifier to the angle value
    private float lightValueMultiplier = .6f; // the multiplier to convert the modifier to the color intensity value

    private void CircleIndicator()
    {
        if (Input.GetKey(KeyCode.Space) && isCaneOut && !hasThrown)
        {
            // increase the modifier value if space is held
            modifier += Time.deltaTime;
        }
        else
            // decreases the value if space is not held
            modifier -= Time.deltaTime * 5;

        // clamps the modifier to a maximum and a minimum
        if (modifier <= 0)
        {
            modifier = 0;
        }
        else if (modifier >= maxTime)
        {
            modifier = maxTime;
        }

        // shader values are then changed
        shader.SetFloat("_Radius", curve.Evaluate(modifier / (5 / speed)) * radiusMultiplier);
        shader.SetFloat("_Angle", curve.Evaluate(modifier / (5 / speed)) * angleMultiplier);
        shader.SetColor("_Color", Color.white * (4 + curve.Evaluate(modifier / (5 / speed)) * lightValueMultiplier));
    }
    #endregion

    #region FishingHook
    [Header("Fishing Hook")] [SerializeField] private GameObject hookObject;
    [SerializeField] private GameObject ropeObject;
    [SerializeField] private CinemachineTargetGroup targetGroup;
    [SerializeField] private float throwSpeed;
    [SerializeField] private float reelSpeed;
    [SerializeField] private Vector2[] hookPoints;
    [HideInInspector] public GameObject hookPosition;
    [HideInInspector] public float modifierAtRelease;
    private GameObject currentHook;
    private GameObject currentRope;
    private bool hasThrown;
    private bool isCaneOut;

    private void FishingAnimation()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isCaneOut)
                animator.SetTrigger("CaneOut");
            else
                animator.SetTrigger("CaneBack");
            isCaneOut = !isCaneOut;
        }

        if (isCaneOut && modifier > 0)
        {
            if (Input.GetKeyUp(KeyCode.Space) && !hasThrown)
            {
                animator.SetTrigger("Throw");
                modifierAtRelease = 1 + curve.Evaluate(modifier / (5 / speed));
                hasThrown = true;
                canMove = false;
            }
        }
        if (currentHook != null && currentHook.GetComponent<DistanceJoint2D>() != null)
        {
            if (hasThrown && currentHook.GetComponent<DistanceJoint2D>().distance >= 1f)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    currentHook.GetComponent<DistanceJoint2D>().distance -= reelSpeed * Time.deltaTime;
                    animator.SetBool("Reeling", true);
                }
                else
                {
                    animator.SetBool("Reeling", false);
                }

                if (Input.GetKey(KeyCode.LeftControl))
                {
                    currentHook.GetComponent<DistanceJoint2D>().enabled = false;
                    animator.SetBool("Reeling", true);
                }
                else if (Input.GetKeyUp(KeyCode.LeftControl))
                {
                    currentHook.GetComponent<DistanceJoint2D>().enabled = true;
                    animator.SetBool("Reeling", false);
                }
            }
            else
            {
                Destroy(currentRope);
                Destroy(currentHook);
                animator.SetBool("Reeling", false);
                canMove = true;
                StartCoroutine(ThrowDelay());
                HookBehavior.inWater = false;
            }
        }
    }

    IEnumerator ThrowDelay()
    {
        yield return new WaitForSeconds(1);
        hasThrown = false;
    }

    private void MoveCaneEnd(int animIndex)
    {
        hookPosition.transform.localPosition = hookPoints[animIndex];
    }

    private void FishCaught()
    {
        Destroy(FindObjectOfType<FishAI>().transform.parent.gameObject);
        isCaneOut = true;
    }

    private void GenerateHook()
    {
        if (!isFacingRight)
            throwSpeed = -throwSpeed;
        GameObject newHook = Instantiate(hookObject, hookPosition.transform.position, Quaternion.identity);
        newHook.GetComponent<Rigidbody2D>().AddForce(new Vector2(1 * throwSpeed, 1 * Mathf.Abs(throwSpeed)) * modifierAtRelease, ForceMode2D.Impulse);
        targetGroup.AddMember(newHook.transform, 1, 0);
        newHook.GetComponent<HookBehavior>().hookDistance = modifierAtRelease / maxTime;
        GameObject newRope = Instantiate(ropeObject, hookPosition.transform.position, Quaternion.identity);
        newRope.GetComponent<Rope>().segmentLength *= (int)modifierAtRelease;
        newRope.GetComponent<Rope>().hook = newHook;
        currentHook = newHook;
        currentRope = newRope;
        modifierAtRelease = 0;
    }

    #endregion
}
