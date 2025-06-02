using System.Collections.Generic;
using Google.XR.ARCoreExtensions.GeospatialCreator;
using UnityEngine;
using TMPro;

public class Waypoint : MonoBehaviour
{

    [Header("ARGeospatialAnchor")]
    public ARGeospatialCreatorAnchor Anchor;
    
    [Header("Neighbors")]
    public List<Waypoint> connectedNodes;

    [Header("Information")]
    public GameObject Sign;
    public TextMeshPro signText;
    public bool endNode = false;
    public string buildingNo = "";






    [HideInInspector] public List<GameObject> lines = new();

    [HideInInspector] public double heuristic = 0, cost = 0, totalCost = 0;

    [HideInInspector] public double lat, lng;

    [HideInInspector] public Waypoint parent = null;


    private GameObject lineOutgoing = null;
    private GameObject lineIncoming = null;


    public void SetLongLat()
    {
        lat = Anchor.Latitude;
        lng = Anchor.Longitude;
    }
    public void SetLineInc(GameObject lineInc) {
        lineIncoming = lineInc;
    }

    public void SetLineOut(GameObject lineOut) {
        lineOutgoing = lineOut;
    }
    void Start() {
        if (buildingNo == null) SetSignText("");
        else SetSignText(buildingNo);
    }

    void Update()
    { // can add a condition to check if the line has moved some threshold
        Vector3 position = Sign.transform.position;

        if (lineOutgoing != null)
        { // update position of line in case of drift
            lineOutgoing.GetComponent<LineRenderer>().SetPosition(0, position);
        }
        if (lineIncoming != null)
        {
            lineIncoming.GetComponent<LineRenderer>().SetPosition(1, position);
        }
        foreach (var line in lines)
        {
            var linesRenderer = line.GetComponent<LineRenderer>();
            var pos0 = linesRenderer.GetPosition(0);
            pos0.y = position.y;
            var pos1 = linesRenderer.GetPosition(1);
            pos1.y = position.y;
            linesRenderer.SetPosition(0, pos0);
            linesRenderer.SetPosition(1, pos1);
        }
    }
    public void SetSignText(string buildingNoText) {        
        signText.text = buildingNoText;
    }

    public void UpdateAlt(float y) {
        transform.position = new Vector3(transform.position.x, y-2, transform.position.z);
    }

    public void ToggleFlag(bool activation)  {
        if (!activation) {
            Destroy(lineOutgoing);
            Destroy(lineIncoming);
            ResetStats();
        }

        if (lines != null && lines.Count > 0) lines.ForEach(x => x.SetActive(false));
        Sign.SetActive(activation);
    }

    public (double Lat, double Lng) getLatLong() {
        return (lat, lng);
    }

    public override string ToString()
    {   
        var toString = gameObject.name + " : (" + lat + ", " + lng + ")";
        if (buildingNo != null && buildingNo != "") toString += " Endnode: " + buildingNo;
        return toString;
    }
    
    public void RotateWaypoint (Vector3 position) {
        var thisTransform = gameObject.transform;
        Vector3 waypointDirection = position - thisTransform.position;
        waypointDirection.y -= 2;

        Quaternion targetRotation = Quaternion.LookRotation(waypointDirection);
        thisTransform.rotation = targetRotation;
        
        thisTransform.Rotate(0, 0, 0);
    }
    

    public void ResetStats()
    {
        heuristic = 0;
        cost = 0;
        totalCost = 0;
        parent = null;
        lineIncoming = null;
        lineOutgoing = null;
        if (lines != null && lines.Count > 0) lines.ForEach(x => x.SetActive(true));
    }
}
