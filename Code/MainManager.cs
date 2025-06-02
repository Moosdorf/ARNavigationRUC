using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance; // holds the instance of this class, which can be accessed in other scripts
    public static RouteController routeController;
    public static SceneNavigation sceneNavigation = null;
    public static string SelectedDestination = null;
    public static Route Route;
    public static Scene currentScene;
    public static bool phone = true;
    public static bool arSupport = true;

    private void Awake()
    {
        if (Instance != null)
        { // if already created, destroy new instance and return 
            Destroy(gameObject);
            return;
        }
        Instance = this; // if no instance made, make it and set it as DontDestroyOnLoad, to keep it persistent throughout scene loads
        DontDestroyOnLoad(gameObject);
    }
       

    public static double HaversineDistance(double node1Lat, double node1Long, double node2Lat, double node2Long) { //  https://stackoverflow.com/questions/41621957/a-more-efficient-haversine-function
        const double earthRadius = 6371e3; // meters

        var lat1 = node1Lat * Math.PI/180d;
        var lat2 = node2Lat * Math.PI/180d;

        var latDif = (node2Lat - node1Lat) * Math.PI/180d;
        var longDif = (node2Long - node1Long) * Math.PI/180d;

        var a = Math.Sin(latDif/2) * Math.Sin(latDif/2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(longDif/2) * Math.Sin(longDif/2);

        var c = 2 * Math.Asin(Math.Sqrt(a));

        var distance = earthRadius * c;

        return Math.Round(distance, 2);
    } // https://www.movable-type.co.uk/scripts/latlong.html FORMULA CREATION   
    public static float UnityDistanceBetweenTwoObjects(GameObject obj1, GameObject obj2) {
        float distance = Vector3.Distance (obj1.transform.position, obj2.transform.position);
        return distance;
    }
}
