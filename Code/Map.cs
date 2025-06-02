using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

public class Map : MonoBehaviour
{
    private UpdateARInfo updateARInfo;
    
    [Header("Api")]
    public string apiKey;

    // Current values
    private float lat;
    private float lon;
    private List<Waypoint> route;

    // Previous values
    private string previousApiKey;
    private float previousLat;
    private float previousLon;
    private List<Waypoint> previousRoute;
   
    // map info
    private int mapWidth;
    private int mapHeight;
    private bool updateMap = true;


    void Start()
    {
        // Set the textture of the API picture to the same size as Map component's size.
        mapWidth = (int)Math.Round(GetComponent<RectTransform>().rect.width);
        mapHeight = (int)Math.Round(GetComponent<RectTransform>().rect.height);
        updateARInfo = GameObject.Find("UpdateInfo").GetComponent<UpdateARInfo>();
    }

    void Update()
    {
        if (updateARInfo != null && MainManager.phone)
        {
            // Get pose info and route.
            var pose = updateARInfo.getPose();
            lat = (float)pose.Latitude;
            lon = (float)pose.Longitude;
            route = MainManager.Route.route;

            // Update the map
            if (updateMap && (previousApiKey != apiKey ||
                !Mathf.Approximately(previousLat, lat) ||
                !Mathf.Approximately(previousLon, lon) ||
                HasRouteChanged()))
            {
                StartCoroutine(GetGoogleMap(route));
                updateMap = false; // So it does not update multiple times
            }
        }
    }

    private bool HasRouteChanged() // When any changes happens for the route, return true.
    {
        if (route == null && previousRoute == null) return false;
        if (route == null || previousRoute == null) return true;
        if (route.Count != previousRoute.Count) return true;

        for (int i = 0; i < route.Count; i++)
        {
            if (!Mathf.Approximately((float)previousRoute[i].lat, (float)route[i].lat) ||
                !Mathf.Approximately((float)previousRoute[i].lng, (float)route[i].lng))
                return true;
        }

        return false;
    }

    IEnumerator GetGoogleMap(List<Waypoint> currentRoute)
    {
        string url = "";
        string path = "";
        string finalPath = "";
        string endNodeMarker = "";
        string lastLat = "";
        string lastLon = "";

        if (currentRoute != null && currentRoute.Count > 0)
        {
            foreach (Waypoint node in currentRoute) // determine the path and the endnode.
            {
                string latStr = ((float)node.lat).ToString().Replace(',', '.');
                string lonStr = ((float)node.lng).ToString().Replace(',', '.');

                lastLat = latStr;
                lastLon = lonStr;

                path += "|" + latStr + "," + lonStr;
            }
        }
        else
        {
            if (currentRoute == null)
            {
                path += "|" + lat.ToString() + "," + lon.ToString();
            }
        }

        if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(lastLat) && !string.IsNullOrEmpty(lastLon)) // if there is a path and endnode, display it on the map otherwise do not display it.
        {
            finalPath = $"&path=color:0x0000ff80|weight:5|{lat},{lon}{path}";
            endNodeMarker = $"&markers=color:blue%7Clabel:B%7C{lastLat},{lastLon}";
        }

        url = $"https://maps.googleapis.com/maps/api/staticmap?&center={lat},{lon}&size={mapWidth}x{mapHeight}&maptype=satellite&markers=color:red%7Clabel:A%7C{lat},{lon}{endNodeMarker}{finalPath}&key={apiKey}";

        UnityWebRequest requestAPI = UnityWebRequestTexture.GetTexture(url); // Creates a GET request that expects an image.
        yield return requestAPI.SendWebRequest(); // Waits for the request to complete.

        if (requestAPI.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("The API request was not successful, error: " + requestAPI.error);
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(requestAPI);
            gameObject.GetComponent<RawImage>().texture = texture;

            previousApiKey = apiKey;
            previousLat = lat;
            previousLon = lon;
            previousRoute = currentRoute;
            updateMap = true;
        }
    }
}

