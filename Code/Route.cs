using System;
using System.Collections.Generic;

[System.Serializable]
public class Route
{
    public List<Waypoint> nodesPassed = new();
    public List<Waypoint> route = null;
    public Waypoint startNode;
    public Waypoint currentNode;
    public Waypoint endNode;
    public bool routeFinished = false;

    public double routeLength = 0;

    private DateTime routeStarted = DateTime.Now;
    private DateTime routeEnded;
    public Route(Waypoint starter, Waypoint end)
    {
        if (MainManager.phone) CreateRouteAStar(starter, end);
        else CreateRouteAStarUNITY(starter, end);

        currentNode = starter;
    }
    public void ResetRoute()
    {
        // route is finished
        routeFinished = true;

        // if it route is not null and contains elements, user ended early and we must disable rest of flags
        if (route != null && route.Count != 0)
        {
            route.ForEach(x => x.ToggleFlag(false));
        }

        // set route null and reset selected destination
        route = null;
        MainManager.SelectedDestination = null;
    }
    public double DistanceRemaining()
    {
        return endNode.cost - currentNode.cost;
    }

    public void NextNode()
    {
        if (route != null && route.Count > 0)
        {
            // find flag to remove
            var toRemove = route[0];
            nodesPassed.Add(toRemove);
            route.RemoveAt(0);

            // reset stats and remove flag and line from world
            toRemove.ToggleFlag(false);

            // deduct cost from total cost of route
            
            // select next node in route
            if (route.Count > 0)
            {
                currentNode = route[0];
            }  else ResetRoute(); // done! 
        }
    }

    public RouteStats GetStats()
    {
        // finding the time it took to end the route,
        // the distance travelled
        // the average speed walked

        routeEnded = DateTime.Now;
        var timeTaken = routeEnded - routeStarted;
        var avgKmh = routeLength / timeTaken.Seconds * 3.6;

        return new RouteStats
        {
            TimeTaken = timeTaken,
            AvgKmt = Math.Round(avgKmh, 2),
            Distance = Math.Round(routeLength, 2)
        };
    }


    public void CreateRouteAStar(Waypoint startNode, Waypoint endNode)
    {
        // h = heuristic
        // g = cost
        // f = totalCost
        route = new();
        List<Waypoint> openList = new();
        List<Waypoint> closedList = new();
        openList.Add(startNode);
        
        startNode.heuristic = MainManager.HaversineDistance(startNode.lat, startNode.lng, endNode.lat, endNode.lng);
        startNode.cost = 0;
        startNode.totalCost = startNode.heuristic + startNode.cost;
        startNode.parent = null;

        Waypoint currentNode;
        
        while (openList.Count > 0)
        { // while list of open nodes is not empty
            currentNode = openList[0]; // start with the first in the list
            foreach (Waypoint next in openList)
            { // choose current node based on lowest cost in list
                if (next.totalCost < currentNode.totalCost)
                { 
                    currentNode = next;
                } 
            }
            if (currentNode == endNode) { // if it is the end node, stop, we are done.
                route = FindRoute(currentNode);
                routeLength = currentNode.cost;
                this.endNode = endNode;
                break;
            }
            
            openList.Remove(currentNode); // remove from open list and add to closed list, this node is done (all neighbors are to be visited now)
            closedList.Add(currentNode);

            foreach (Waypoint neighbor in currentNode.connectedNodes)
            { 
                if (closedList.Contains(neighbor))
                {
                    continue; // if evaluated, go next
                }
                
                // travel distance to the neighbor
                var tentativeG = currentNode.cost + MainManager.HaversineDistance(neighbor.lat, neighbor.lng, currentNode.lat, currentNode.lng);
                    
                // if the open list dont have the neighbor, add it
                if (!openList.Contains(neighbor))
                {
                    neighbor.cost = tentativeG;
                    neighbor.heuristic = MainManager.HaversineDistance(neighbor.lat, neighbor.lng, endNode.lat, endNode.lng);
                    neighbor.totalCost = neighbor.cost + neighbor.heuristic;
                    neighbor.parent = currentNode;
                    openList.Add(neighbor);
                } // if its already in the list, check if this path is quicker
                else if (tentativeG < neighbor.cost)
                {
                    neighbor.cost = tentativeG;
                    neighbor.totalCost = neighbor.cost + neighbor.heuristic;
                    neighbor.parent = currentNode;
                }
            }
        }
        // if goal not reached
        if (route.Count == 0)
        {
            route = null;
        } 
    }

    public void CreateRouteAStarUNITY(Waypoint startNode, Waypoint endNode)
    {
        // h = heuristic
        // g = cost
        // f = totalCost
        route = new();
        List<Waypoint> openList = new();
        List<Waypoint> closedList = new();
        openList.Add(startNode);
        
        startNode.heuristic = MainManager.UnityDistanceBetweenTwoObjects(startNode.gameObject, endNode.gameObject);
        startNode.cost = 0;
        startNode.totalCost = startNode.heuristic + startNode.cost;
        startNode.parent = null;

        Waypoint currentNode;
        while (openList.Count > 0) { // while list of open nodes is not empty
            currentNode = openList[0]; // start with the first in the list
            foreach (Waypoint next in openList)
            { // choose current node based on lowest cost in list
                if (next.totalCost < currentNode.totalCost)
                { 
                    currentNode = next;
                } 
            }
            if (currentNode == endNode)
            { // if it is the end node, stop, we are done.
                route = FindRoute(currentNode);
                routeLength = currentNode.cost;
                this.endNode = endNode;
                break;
            }
            
            openList.Remove(currentNode); // remove from open list and add to closed list, this node is done (all neighbors are to be visited now)
            closedList.Add(currentNode);

            foreach (Waypoint neighbor in currentNode.connectedNodes)
            { 
                if (closedList.Contains(neighbor))
                {
                    continue; // if evaluated, go next
                }
                
                // travel distance to the neighbor
                var tentativeG = currentNode.cost + MainManager.UnityDistanceBetweenTwoObjects(neighbor.gameObject, currentNode.gameObject);
                    
                // if the open list dont have the neighbor, add it
                if (!openList.Contains(neighbor))
                {
                    neighbor.cost = tentativeG;
                    neighbor.heuristic = MainManager.UnityDistanceBetweenTwoObjects(neighbor.gameObject, currentNode.gameObject);
                    neighbor.totalCost = neighbor.cost + neighbor.heuristic;
                    neighbor.parent = currentNode;
                    openList.Add(neighbor);
                } // if its already in the list, check if this path is quicker
                else if (tentativeG < neighbor.cost)
                {
                    neighbor.cost = tentativeG;
                    neighbor.totalCost = neighbor.cost + neighbor.heuristic;
                    neighbor.parent = currentNode;
                }
            }
        }
        // if goal not reached
        if (route.Count == 0) {
            
            route = null;
        } 
    }

    
    private List<Waypoint> FindRoute(Waypoint endNode)
    {
        // finds the end node's parent, and the parents parent, until there are no more, this is the entire route.
        Waypoint current = endNode;
        List<Waypoint> route = new();

        while (current != null) {
            route.Add(current);
            current = current.parent;
        }

        // the route was added in reverse, so we reverse it again in order to have the starter node at index 0.
        route.Reverse();
        currentNode = route[0];
        return route;
    }
}

public class RouteStats
{ // class to hold the stats
    public double Distance { get; set; }
    public TimeSpan TimeTaken { get; set; }
    public double AvgKmt { get; set; }
}