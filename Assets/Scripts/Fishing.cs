using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class Fishing : MonoBehaviour
{
    [SerializeField] private Animator animator;

    void Update()
    {
        CircleIndicator();
        FishingAnimation();
    }

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
        if (Input.GetKey(KeyCode.Space) && isCaneOut)
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
    [SerializeField] private CinemachineTargetGroup targetGroup;
    [SerializeField] private GameObject hookPosition;
    [SerializeField] private float throwSpeed;
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
            }
        }
    }

    public void GenerateHook()
    {
        GameObject newHook = Instantiate(hookObject, hookPosition.transform.position, Quaternion.identity);
        newHook.GetComponent<Rigidbody2D>().AddForce(new Vector2(1, 1) * throwSpeed * modifier, ForceMode2D.Impulse);
        targetGroup.AddMember(newHook.transform, 1, 0);
    }

    #endregion
}
