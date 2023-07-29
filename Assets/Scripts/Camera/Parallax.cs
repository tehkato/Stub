using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private Camera cam; // the main camera
    [SerializeField] private Transform subject; // the player in the scene
    private Vector2 startPos; // checks the starting position of the plane
    private float startZ; // insures that the z coordinate isn't affected
    private Vector2 travel => (Vector2)cam.transform.position - startPos; // the distance the plane has to travel
    private float distanceFromSubject => transform.position.z - subject.position.z; // the distance between the player and the plane
    private float clippingPlane => cam.transform.position.z 
        + (distanceFromSubject > 0 ? cam.farClipPlane : cam.nearClipPlane); // what is the closest clipping plane
    private float parallaxFactor => Mathf.Abs(distanceFromSubject) / clippingPlane; // the calculated speed at which the plane will move

    void Start()
    {
        startPos = transform.position;
        startZ = transform.position.z;
    }

    void Update()
    {
        Vector2 newPos = startPos + travel * parallaxFactor;
        transform.position = new Vector3(newPos.x, startPos.y, startZ);
    }
}
