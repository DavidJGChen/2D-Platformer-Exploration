using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothCamera : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    private Vector2 smoothDamp;

    // Update is called once per frame
    void FixedUpdate(){

        var goal = Vector2.SmoothDamp(transform.position, target.position, ref smoothDamp, smoothTime);

        transform.position = (Vector3)goal - Vector3.forward * 10;
    }
}
