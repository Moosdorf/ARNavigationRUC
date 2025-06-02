using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RotateArrow : MonoBehaviour
{
    private UpdateARInfo info;
    private GameObject mainCamera;

    void Start()
    {
        info = GameObject.Find("UpdateInfo").GetComponent<UpdateARInfo>();
        mainCamera = GameObject.Find("Main Camera");

    }

    void Update()
    { 
        if (info.GetState() != UpdateARInfo.State.Route) return;
        var currentWaypoint = MainManager.Route.currentNode;
        // Direction from camera to current waypoint.
        Vector3 direction = mainCamera.transform.position - currentWaypoint.transform.position;
        Vector3 forward = mainCamera.transform.forward;

        float angle = Vector3.SignedAngle(direction, forward, Vector3.up);

        transform.rotation = Quaternion.Euler(0, 0, angle-176);       
    

    }
}
