using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();
    [SerializeField] private float RopeSegLength = .05f;
    public int segmentLength;
    [SerializeField] private float lineWidth = .1f;
    [SerializeField] private float ropeGravity = .02f;
    [SerializeField, Range(.1f, 1f)] private float rigidity = 2f;
    [SerializeField] private int simulationAmount = 50;
    private PlayerController player;
    [HideInInspector] public GameObject hook;

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        lineRenderer = GetComponent<LineRenderer>();
        Vector3 ropeStartPoint = player.hookPosition.transform.position;
        for (int i = 0; i < segmentLength; i++)
        {
            ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y -= RopeSegLength;
        }
    }

    // Update is called once per frame
    void Update()
    {
        RopeLength();
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();
    }

    private void RopeLength()
    {
        if (Vector2.Distance(ropeSegments[segmentLength - 1].posNow, ropeSegments[segmentLength - 2].posNow) >= RopeSegLength * 3)
        {
            segmentLength++;
            ropeSegments.Add(new RopeSegment(player.hookPosition.transform.position));
        }
        else if (Vector2.Angle(ropeSegments[0].posNow, ropeSegments[1].posNow) >= .3 && HookBehavior.inWater == true && segmentLength >= 5)
        {
            segmentLength--;
            ropeSegments.RemoveAt(0);
        }
    }

    private void Simulate()
    {
        //simulation
        Vector2 forceGravity = new Vector2(0f, -ropeGravity);
        for (int i = 0; i < segmentLength; i++)
        {
            RopeSegment firstSegment = ropeSegments[i];
            Vector2 velocity = firstSegment.posNow - firstSegment.posOld;
            firstSegment.posOld = firstSegment.posNow;
            firstSegment.posNow += velocity;
            firstSegment.posNow += forceGravity * Time.fixedDeltaTime;
            ropeSegments[i] = firstSegment;
        }

        //constraints
        for (int i = 0; i < simulationAmount; i++)
        {
            ApplyConstraint();
        }
    }

    private void ApplyConstraint()
    {
        RopeSegment firstSegment = ropeSegments[0];
        firstSegment.posNow = hook.transform.position;
        ropeSegments[0] = firstSegment;

        RopeSegment endSegment = ropeSegments[segmentLength - 1];
        endSegment.posNow = player.hookPosition.transform.position;
        ropeSegments[segmentLength - 1] = endSegment;

        for (int i = 0; i < segmentLength - 1; i++)
        {
            RopeSegment firstSeg = ropeSegments[i];
            RopeSegment secondSeg = ropeSegments[i + 1];

            float dist = (firstSeg.posNow - secondSeg.posNow).magnitude;
            float error = dist - RopeSegLength;
            Vector2 changeDir = Vector2.zero;

            if (dist > RopeSegLength)
            {
                changeDir = (firstSeg.posNow - secondSeg.posNow).normalized;
            }
            else if (dist < RopeSegLength)
            {
                changeDir = (secondSeg.posNow - firstSeg.posNow).normalized;
            }

            Vector2 changeAmount = changeDir * error;
            if (i != 0)
            {
                firstSeg.posNow -= changeAmount * rigidity;
                ropeSegments[i] = firstSeg;
                secondSeg.posNow += changeAmount * rigidity;
                ropeSegments[i + 1] = secondSeg;
            }
            else
            {
                secondSeg.posNow += changeAmount;
                ropeSegments[i + 1] = secondSeg;
            }
        }
    }
    private void DrawRope()
    {
        float lineWidth = this.lineWidth;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Vector3[] ropePositions = new Vector3[segmentLength];
        for (int i = 0; i < segmentLength; i++)
        {
            ropePositions[i] = ropeSegments[i].posNow;
        }

        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    public struct RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;

        public RopeSegment(Vector2 pos)
        {
            posNow = pos;
            posOld = pos;
        }
    }
}
