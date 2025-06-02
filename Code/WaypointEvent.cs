using UnityEngine;

public class WaypointEvent : MonoBehaviour
{
    void OnTriggerStay(Collider other)
    {   
        MainManager.Route.NextNode();
        Handheld.Vibrate();
    }
}
