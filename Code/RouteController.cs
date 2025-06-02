using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using NetTopologySuite.Geometries;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;


public class RouteController : MonoBehaviour
{
    [Header("Line")]
    public Material lineMat;
    public Material lineMatRoute;
    
    [Header("RouteObjects")]
    public GameObject nodes;
    public AREarthManager earthManager; // we could fetch this from UpdateARInfo



    [Header("Polygons")]
    public TextAsset polygons;
    

    public Dictionary<Waypoint, Dictionary<Waypoint, double>> graph = null;


    [HideInInspector] public List<Waypoint> allNodes;

    private List<Waypoint> endNodes = null;
    private Waypoint endNode = null;
    private static int count;

    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        Debug.Log("Running in Editor - skipping ARCore session checks.");
        MainManager.phone = false;
#endif
        
        MainManager.routeController = this;
        CreateGraph();
        CreatePolygons();
    }


    public List<Waypoint> GetEndNodes() {
        return endNodes;
    }
    public Waypoint GetEndNode() {
        return endNode;
    }

    public void CreateLineRenderer(Waypoint from, Waypoint to, int mat = 0) { // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/LineRenderer.SetPosition.html
        var name = (mat == 0) ? "RouteLine" : "Line";
        GameObject line = new(name + count++); // create a line and make the from node the parent


        
        // create line renderer object and adjust it
        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 1f;
        lineRenderer.endWidth = 1f;
        lineRenderer.material = (mat == 0) ? lineMatRoute : lineMat;
        lineRenderer.positionCount = 2; // all lines between nodes must have 2 connections (to and from)
        // we do not need to set positions here, the node updates it each frame
        if (mat == 0)
        {
            from.SetLineOut(line);
            to.SetLineInc(line);
        }
        else
        {
            if (from.lines.Contains(line)) return;
            from.lines.Add(line);
            lineRenderer.SetPosition(0, from.transform.position);
            lineRenderer.SetPosition(1, to.transform.position);
        }
    }

    private GameObject playerLine = null;
    public void CreatePlayerLine(Transform player) {
        var position =  player.transform.position;
        var position2 = MainManager.Route.currentNode.Sign.transform.position;
        position.y -= 5;


        LineRenderer lineRenderer;
        if (playerLine == null)
        {
            playerLine = new("Player Line");

            lineRenderer = playerLine.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 1f;
            lineRenderer.endWidth = 1f;
            lineRenderer.material = lineMatRoute;
            lineRenderer.positionCount = 2;
        }
        else lineRenderer = playerLine.GetComponent<LineRenderer>();

        // set positions based on node anchors
        lineRenderer.SetPosition(0, position);
        lineRenderer.SetPosition(1, position2);
    }

    private void CreateGraph() {
        graph = new(); // initialize the dictionary
        allNodes = nodes.GetComponentsInChildren<Waypoint>().ToList();
        endNodes = new();

        allNodes.ForEach(x => x.SetLongLat()); // find a way to avoid having to use this loop to set coordinates

        foreach (Waypoint node in allNodes)
        { 
            if (node.endNode)
            {
                endNodes.Add(node);
            }

            graph[node] = new Dictionary<Waypoint, double>(); // create dictionary at each node to store neighbors
            var (nodeLat, nodeLng) = node.getLatLong();

            foreach (Waypoint neighbor in node.connectedNodes)
            { // find all neighbors, calculate distance and add edge to graph
                var (neighborLat, neighborLng) = neighbor.getLatLong();
                var distance = MainManager.HaversineDistance(nodeLat, nodeLng, neighborLat, neighborLng); // calculate the distance between nodes in meters
                
                graph[node][neighbor] = distance;
            }
            node.ToggleFlag(false);
        }
    }

    public bool CreateRoute() {
        endNode = endNodes.Find(x => x.buildingNo == MainManager.SelectedDestination);

        Dictionary<Waypoint, double> distanceToPlayerNodes = FindNearestNode(endNode);
        if (distanceToPlayerNodes.Count == 0) return false;

        var distanceToPlayerNodesSorted = distanceToPlayerNodes.OrderBy(node => node.Value).ToArray();

        (Waypoint, double) bestNode = (null, 1000000);

        for (int i = 0; i < distanceToPlayerNodesSorted.Count(); i++)
        {
            var route = new Route(distanceToPlayerNodesSorted[i].Key, endNode);
            var length = distanceToPlayerNodesSorted[i].Value + route.routeLength;

            if (length < bestNode.Item2) {
                bestNode = (distanceToPlayerNodesSorted[i].Key, length);
            }
        }

        MainManager.Route = new Route(bestNode.Item1, endNode);
        var totalWaypoints = MainManager.Route.route.Count;
        for (int i = 0; i < MainManager.Route.route.Count; i++)
        {
            var currentNode = MainManager.Route.route[i];
            if (currentNode == endNode) currentNode.SetSignText(currentNode.buildingNo); 
            else currentNode.SetSignText((i + 1) + "/" + totalWaypoints);
            
        }
        
        return true;
    }



    private Dictionary<Waypoint, double> FindNearestNodeUNITY(Waypoint endNode) {
        GameObject player = GameObject.Find("Main Camera");
        (double Latitude, double Longitude) pose = (player.transform.position.x, player.transform.position.z);
        
        Dictionary<Waypoint, double> bestNodes = new();
        Waypoint bestNode = null;
        double min = 0;
        foreach (Waypoint node in graph.Keys) { // check which node is closest to the user, does not include heuristic
            var (Lat, Lng) = node.getLatLong(); // get coordinates for the node 
            if (CheckPolygonOverlapUNITY(new Coordinate(pose.Latitude, pose.Longitude), node)) {
                continue; // if the node overlaps with a polygon, continue to next evaluation
            }

            if (bestNode == null) { // if no node is set, set the first one seen
                bestNode = node;
                min = MainManager.UnityDistanceBetweenTwoObjects(node.gameObject, player) + 
                        MainManager.UnityDistanceBetweenTwoObjects(endNode.gameObject, node.gameObject);
                continue;
            } 
            var contender = MainManager.UnityDistanceBetweenTwoObjects(node.gameObject, player) + 
                            MainManager.UnityDistanceBetweenTwoObjects(endNode.gameObject, node.gameObject);

            bestNodes[node] = contender;
            if (contender < min) { // if contender is less than min, choose that node 
                min = contender;
                bestNode = node;
            }
        }
        return bestNodes;
    }

    private Dictionary<Waypoint, double> FindNearestNode(Waypoint endNode) {
        if (!MainManager.phone) return FindNearestNodeUNITY(endNode);

        // get pose for user lat/long
        var pose = earthManager.CameraGeospatialPose;

        Dictionary<Waypoint, double> bestNodes = new();
        Waypoint bestNode = null;
        double min = 0;
        foreach (Waypoint node in graph.Keys) { // check which node is closest to the user, does not include heuristic

            var (nodeLat, nodeLng) = node.getLatLong(); // get coordinates for the node 
            if (CheckPolygonOverlap(new Coordinate(pose.Latitude, pose.Longitude), node)) {
                continue; // if the node overlaps with a polygon, continue to next evaluation
            }

            if (bestNode == null) { // if no node is set, set the first one seen
                bestNode = node;
                min = MainManager.HaversineDistance(nodeLat, nodeLng, pose.Latitude, pose.Longitude) + 
                        MainManager.HaversineDistance(endNode.lat, endNode.lng, nodeLat, nodeLng);
                continue;
            } 
            var contender = MainManager.HaversineDistance(nodeLat, nodeLng, pose.Latitude, pose.Longitude) + 
                            MainManager.HaversineDistance(endNode.lat, endNode.lng, nodeLat, nodeLng);

            bestNodes[node] = contender;
            if (contender < min)
            { // if contender is less than min, choose that node 
                min = contender;
                bestNode = node;
            }
        }
        return bestNodes;
    }

    private bool CheckPolygonOverlapUNITY(Coordinate player, Waypoint node) {
        

        var coordinateNode = new Coordinate(node.transform.position.x, node.transform.position.z);
        LineString line = new(new Coordinate[]{player, coordinateNode});

        // check if the line overlaps any polygon, if it does then its no good and return false
        bool result = Polygons.Any(x => x.Intersects(line)); 

        return result;
    }
    private bool CheckPolygonOverlap(Coordinate player, Waypoint node) {
        var coordinateNode = new Coordinate(node.lat, node.lng);
        LineString line = new(new Coordinate[]{player, coordinateNode}); 

        // check if the line overlaps any polygon, if it does then its no good and return false
        bool result = Polygons.Any(x => x.Intersects(line)); 

        return result;
    }


    private List<Polygon> Polygons = new();
    private void CreatePolygons() {
        // create object that will create polygons
        GeometryFactory geometryFactory = new();

        if (!MainManager.phone)
        {
            Transform[] corners = GameObject.Find("Polygon").GetComponentsInChildren<Transform>().Skip(5).ToArray();
            List<Coordinate> polygonCoords = new();
            foreach (Transform corner in corners)
            {
                polygonCoords.Add(new Coordinate(corner.transform.position.x, corner.transform.position.z));
            }
            Polygon poly = geometryFactory.CreatePolygon(polygonCoords.ToArray());
            Polygons.Add(poly);
            return;
        }

        // convert json file into dictionary of polygons
        Dictionary<int, List<double>> coords = JsonConvert.DeserializeObject<Dictionary<int, List<double>>>(polygons.text);
        
        foreach(List<double> coordinates in coords.Values)
        { // create each polygon and store in a list
            List<Coordinate> polygonCoords = new();
            for (int i = 0; i < coordinates.Count; i++) {
                polygonCoords.Add(new Coordinate(coordinates[++i], coordinates[i-1])); // we list lat and long seperately, this means we have to add 1 to the indexer 
            }
            
            Polygon poly = geometryFactory.CreatePolygon(polygonCoords.ToArray());
            Polygons.Add(poly);
        }
    }


    public void Next() {
        MainManager.Route.NextNode();
    }
}
