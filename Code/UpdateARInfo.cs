using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using Google.XR.ARCoreExtensions;
using System;
using UnityEngine.Android;
using System.Linq;

public class UpdateARInfo : MonoBehaviour
{
    
    [Header("AR Components")]
    public XROrigin Origin;
    public ARSession Session;
    public AREarthManager EarthManager;
    public ARCoreExtensions _ARCoreExtensions;
    private GeospatialPose pose;

    [Header("UI Components")]
    public GameObject UIOverlay;
    public GameObject Debugger;
    public TMP_Text DebugText;
    public TMP_Text HorizontalYawInfo;
    public TMP_Text RouteInfoText;
    public TMP_Text RouteFinishedText;
    public GameObject RouteDoneCanvas;
    public GameObject RouteInfoCanvas;
    public GameObject NavArrow;
    public GameObject InfoPopup;
    public GameObject WarningPopUp;
    
    [Header("Effects")]
    public ParticleSystem particleEffect;
    private ParticleSystem currentParticleEffect;
    private bool anchorsPlaced = false;
    private const int HorizontalThreshold = 5;
    private const int OrientationThreshold = 15;

    public enum State {
        NoRoute,
        Route,
        RouteComplete
    };
    private State currentState = State.NoRoute;


    // Start is called before the first frame update
    public void Start()
    {
        Debug.Log("starting app");
        
        #if UNITY_EDITOR
        Debug.Log("Running in Editor - skipping ARCore session checks.");
            MainManager.phone = false;
            Origin.gameObject.SetActive(true);
        #else
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            StartCoroutine(StartLocationService());
        #endif

    }

    // used to start location services, like session, ARCore and location input....
    private IEnumerator StartLocationService()
    {
        // enable origin, session and ARCore
        try
        {
            Origin.gameObject.SetActive(true);
            Session.gameObject.SetActive(true);
            _ARCoreExtensions.gameObject.SetActive(true);
        }
        catch
        {
            Debug.Log("no phone");
            MainManager.phone = false;
        }

        if (MainManager.phone)
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
                yield return new WaitForSeconds(3.0f);
            }

            if (!Input.location.isEnabledByUser)
            {
                yield break;
            }

            Input.location.Start();

            while (Input.location.status == LocationServiceStatus.Initializing)
            {
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarningFormat(
                    "Location service ended with {0} status.", Input.location.status);
                Input.location.Stop();
            }
        }

    }

    public State GetState() {
        return currentState;
    }

    public void SetState(State state) {
        currentState = state;
    }

    public void ChangeDebugState() {
        Debugger.SetActive(!Debugger.activeSelf);
    }

    public GeospatialPose getPose() {
        if (!EarthManager.gameObject.activeSelf) return new GeospatialPose();
        return EarthManager.CameraGeospatialPose;
    }

    public void ToggleButton(GameObject obj) {
        obj.SetActive(!obj.activeSelf);
    }
    
    public void ToggleMainMenuWarning(int activation) {
        // activation 
        // 0 = click on menu button
        // 1 = click on No on pop up
        // 2 = click on Yes on pop up
        if (activation == 0 || activation == 2) {
            if (currentState == State.NoRoute || activation == 2) { // if no route or 2, then load main menu, and reset route info if set
                
                MainManager.Route?.ResetRoute(); // if not null reset route           
                if (GameObject.Find("Player Line") != null) Destroy(GameObject.Find("Player Line")); // if player line exists, destroy it
                GameObject map = GameObject.Find("Map");
                if (map != null) map.SetActive(false);
                MainManager.Route = null;
                SetState(State.NoRoute);
                MainManager.sceneNavigation.LoadScene("Main Menu");
                WarningPopUp.SetActive(false);
                return;
            }  
        } 
        WarningPopUp.SetActive(activation == 0);
    }
    public void RouteCompleteButton(GameObject obj) {
        MainManager.Route.ResetRoute();            
        MainManager.Route = null;
        obj.SetActive(!obj.activeSelf);
    }
    private void ParticleFireworkEffect()
    {
        if (currentParticleEffect != null) // Remove older effect
            Destroy(currentParticleEffect.gameObject);

        if (MainManager.Route != null && MainManager.Route.endNode != null)
        {
            particleEffect.transform.position = MainManager.Route.endNode.transform.position; // Set the position for the effect

            currentParticleEffect = Instantiate(particleEffect); // Create a new instance of the effect
            currentParticleEffect.Play();
            Destroy(currentParticleEffect.gameObject, 5f);
        }
    }


    private float lastUpdate; // update text every second, as we dont move that fast...
    private void UpdateDebugger() {
        // this is the debugger text
        string routeText = "Debugger: \n";
        
        if (lastUpdate + 1 < Time.time) { // update every second
            if (!anchorsPlaced){ 
                routeText += "CANNOT PLACE ANCHORS YET!";
                DebugText.text = routeText;
                return;
            }
            if (MainManager.Route != null && MainManager.Route.route != null) {
                foreach (Waypoint node in MainManager.Route.route) {
                    routeText += node.ToString() + "\n";
                } // add full route to the debug screen
                lastUpdate = Time.time;
            }

        } else return;
        DebugText.text = routeText;
    }

    // variables to check time accurate (required time will be 2 seconds)
    private float requiredAccuracyTime; // varies based on the last time the pose was accurate
    private float timer; // the timer for accuracy
    private bool accurate = false; // must be accurate for 2 seconds
    private void CheckARAccuracy() {
        if (!anchorsPlaced) { 
            if (pose.OrientationYawAccuracy < OrientationThreshold && pose.HorizontalAccuracy < HorizontalThreshold) {
                // must be accurate for at least 2 seconds                                        
                timer = Time.time;

                if (!accurate) {
                    accurate = true;
                    requiredAccuracyTime = Time.time + 2; // will only b
                } 
   
                // if 2 seconds has passed, activate anchors and remove the info regarding gps activation
                if (timer >= requiredAccuracyTime) {
                    anchorsPlaced = true;
                    InfoPopup.SetActive(false);
                }
            } else if (!anchorsPlaced){ 
                accurate = false;
            } 
        } 
    }
    

    public void UpdateRoute() {
        if (MainManager.Route == null) return;

        UpdateDebugger(); // update ui components
        CheckARAccuracy(); // update ar ui components and other information
        if (anchorsPlaced) {
            var camera = GameObject.Find("Main Camera");
            if (!RouteInfoCanvas.activeSelf) RouteInfoCanvas.SetActive(true);
            MainManager.routeController.graph.Keys.ToList().ForEach(x => x.UpdateAlt(camera.transform.position.y));

            string formattedInfo = string.Format("Destination: {1}{0}" +
                                                "Distance remaining: {3}m{0}" +
                                                "Distance to next node: {2}m",
                                                Environment.NewLine,
                                                MainManager.SelectedDestination,
                                                DistanceToNextNode(), 
                                                MainManager.Route.DistanceRemaining() + DistanceToNextNode()); // update speed if 2 seconds has passed
            RouteInfoText.text = formattedInfo;
            PlaceRouteObjects(); // returns immediately if they have already been toggled

            // update player line every frame after the anchors have been placed
            MainManager.routeController.CreatePlayerLine(GameObject.Find("Main Camera").transform);
        } else {
            if (RouteInfoText.text != "") RouteInfoText.text = "";
            if (!InfoPopup.activeSelf) InfoPopup.SetActive(true);
            HorizontalYawInfo.text = string.Format("This app works best outside.{0}Too inaccurate, cannot place waypoints{0}" +
                                                        "To help improve accuracy faster, {0}move your phone in a figure-eight motion{0}" +
                                                        "Horizontal (BELOW {3}): {0}{1}{0} " +
                                                        "OrientationYaw (BELOW {4}): {0}{2}",  
                                                        Environment.NewLine,
                                                        pose.HorizontalAccuracy, 
                                                        pose.OrientationYawAccuracy,
                                                        HorizontalThreshold,
                                                        OrientationThreshold);
        }   
    }


    // unity's update method
    public void Update() { 
        // check gps availability
        if (!MainManager.phone) {
            switch(currentState) {
                case State.Route:
                    UpdateRoute();
                    break;

                case State.RouteComplete: // if route is done then reset some variables
                        if (!RouteDoneCanvas.activeSelf) {
                            RouteDoneCanvas.SetActive(true);
                            UpdateStats();
                            anchorsPlaced = false;
                            RouteInfoCanvas.SetActive(false);
                            NavArrow.SetActive(false);
                            ParticleFireworkEffect();
                        }
                        if (GameObject.Find("Player Line") != null) Destroy(GameObject.Find("Player Line"));
                        SetState(State.NoRoute);
                    break;
                case State.NoRoute:
                    break;
            }
            return;
        }

        bool earthTrackingState = EarthManager.EarthTrackingState == TrackingState.Tracking; // Gets the tracking state of Earth for the latest frame. https://docs.unity3d.com/Packages/com.unity.xr.arsubsystems@4.2/api/UnityEngine.XR.ARSubsystems.TrackingState.html
        // if the state is tracking, then use the cameras geospatial pose. https://developers.google.com/ar/reference/unity-arf/class/Google/XR/ARCoreExtensions/AREarthManager#camerageospatialpose
        bool isSessionReady = ARSession.state == ARSessionState.SessionTracking && 
                                Input.location.status == LocationServiceStatus.Running; 

        if (isSessionReady && earthTrackingState) {
            // always need a pose when using AR, otherwise nothing will work
            pose = getPose();

            if (MainManager.Route != null && MainManager.Route.routeFinished && currentState == State.Route) {
                SetState(State.RouteComplete);
            }

            switch(currentState) {
                case State.Route:
                    UpdateRoute();
                    break;

                case State.RouteComplete: // if route is done then reset some variables
                        if (!RouteDoneCanvas.activeSelf) {
                            RouteDoneCanvas.SetActive(true);
                            UpdateStats();
                            anchorsPlaced = false;
                            RouteInfoCanvas.SetActive(false);
                            NavArrow.SetActive(false);
                            ParticleFireworkEffect();
                        }
                        if (GameObject.Find("Player Line") != null) Destroy(GameObject.Find("Player Line"));
                        SetState(State.NoRoute);
                    break;
                case State.NoRoute:
                    break;
            }
        }

    }

    private double DistanceToNextNode() {
        var (nodeLat, NodeLong) = MainManager.Route.currentNode.getLatLong();
        var userLat = pose.Latitude;
        var userLong = pose.Longitude;
        return MainManager.HaversineDistance(nodeLat, NodeLong, userLat, userLong);
    }

    private void UpdateStats() {
        // when route is finished, get stats from the route and display them on the end screen.
        RouteStats routeStats = MainManager.Route.GetStats();
        
        RouteFinishedText.text = string.Format("You have reached the final destination!{0}" +
                                "Total distance {1}m{0}" +
                                "Time taken: {2} h/m/s{0}" +
                                "Your average speed was {3}km/h",
                                Environment.NewLine,
                                routeStats.Distance,
                                routeStats.TimeTaken,
                                routeStats.AvgKmt);
    }

    private void PlaceRouteObjects() {
        // if route is not null and there are flags in the route, place them.
        if (MainManager.Route.route != null && MainManager.Route.route.Count > 0) {  
            MainManager.routeController.allNodes.ForEach(x => x.RotateWaypoint(Camera.main.transform.position));
            
            if (MainManager.Route.route[0].Sign.activeSelf)
            {
                return;
            }; // if already placed, return
            /*foreach (Node currentNode in MainManager.routeController.allNodes)
            {
                currentNode.connectedNodes.ForEach(neighbor => MainManager.routeController.CreateLineRenderer(currentNode, neighbor, 1));
            }*/
            RouteInfoCanvas.SetActive(false); // deactive information regarding accuracy
            NavArrow.SetActive(true); // enable the navigation arrow
            
            // toggle flags and create lines between relevant nodes
            foreach (Waypoint current in MainManager.Route.route) { 
                if (!current.Sign.activeSelf) {
                    current.ToggleFlag(true);
                } 
                if (current.parent != null) {
                    MainManager.routeController.CreateLineRenderer(current.parent, current, 0);
                }
            }
        }
    }

}


