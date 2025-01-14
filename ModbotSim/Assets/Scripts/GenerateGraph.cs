using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GenerateGraph {

	private List<Node> nodes;
	public Node endNode;

	public GenerateGraph() {
		//get nav mesh characteristics from pre-made nav mesh. Will write script later that generates 
		//a nav-mesh for any map.
		NavMeshTriangulation navmesh = NavMesh.CalculateTriangulation();
		//initialize triangles array
		Triangle[] meshTriangles = new Triangle[navmesh.indices.Length/3];

		//will contain mapping from a string containing the Vector3 pair side and the lane type of a node(ex: (1,2,3) and (4,5,6)
		//in "middle" will be respresented as "1,2,3 - 4,5,6 middle" with the smaller Vector3 coming first) to the node on that
		//side with that lane type.
		Dictionary<string, Node> sideToNode = new Dictionary<string, Node>();
		//will contain mapping from a Node to the list of Triangles that contain that Node on a side
		Dictionary<Node, List<Triangle>> nodeToTriangles = new Dictionary<Node, List<Triangle>>();
		nodes = new List<Node>();
		//will contain a mapping from Vector3 coordinates (ex: (1,2,3) will be represented as "1,2,3") to 
		//a Node
		Dictionary<string, Node> coordinatesToNode = new Dictionary<string, Node>();

		//Made sure nav mesh indices is a multiple of 3
		for (int i = 0; i < navmesh.indices.Length / 3; i++) {
			Vector3[] currentVectors = new Vector3[3];
			Vector3 v1 = navmesh.vertices[navmesh.indices[i*3]];
			Vector3 v2 = navmesh.vertices[navmesh.indices[i*3 + 1]];
			Vector3 v3 = navmesh.vertices[navmesh.indices[i*3 + 2]];
			meshTriangles[i] = new Triangle(v1, v2, v3, NavMesh.GetAreaCost(navmesh.areas[i]));

			List<Vector3Pair> trianglePairs = new List<Vector3Pair>();
			//Add the pair v1, v2 to trianglePairs
			trianglePairs.Add(new Vector3Pair(v1, v2));
			//Add the pair v2, v3 trianglePairs
			trianglePairs.Add(new Vector3Pair(v2, v3));
			//Add the pair v1, v3
			trianglePairs.Add(new Vector3Pair(v1, v3));
			//Calculate bisections. Needed to generate smoother paths
			foreach (Vector3Pair currentVector3Pair in trianglePairs) {
				Vector3 currentFirst = currentVector3Pair.first;
				Vector3 currentSecond = currentVector3Pair.second;
				Vector3 bisect1 = new Vector3((currentFirst.x + currentSecond.x)/2, (currentFirst.y + currentSecond.y)/2,
				                              (currentFirst.z + currentSecond.z)/2);
				Vector3 bisect2 = new Vector3((bisect1.x + currentFirst.x)/2, (bisect1.y + currentFirst.y)/2,
				                              (bisect1.z + currentFirst.z)/2);
				Vector3 bisect3 = new Vector3((bisect1.x + currentSecond.x)/2, (bisect1.y + currentSecond.y)/2,
				                              (bisect1.z + currentSecond.z)/2);
				Node bisect1Node = getNodeWithVectorCoordinates(ref coordinatesToNode, bisect1);
				Node bisect2Node = getNodeWithVectorCoordinates(ref coordinatesToNode, bisect2);
				Node bisect3Node = getNodeWithVectorCoordinates(ref coordinatesToNode, bisect3);
				AddToDictionary(ref nodeToTriangles, bisect1Node, meshTriangles[i]);
				AddToDictionary(ref nodeToTriangles, bisect2Node, meshTriangles[i]);
				AddToDictionary(ref nodeToTriangles, bisect3Node, meshTriangles[i]);
				sideToNode[GetPairString(currentFirst, currentSecond) + " middle"] = bisect1Node;
				sideToNode[GetPairString(currentFirst, currentSecond) + " outer1"] = bisect2Node;
				sideToNode[GetPairString(currentFirst, currentSecond) + " outer2"] = bisect3Node;
			}
			Vector3 currentCentroid = meshTriangles[i].Centroid ();
			Node centroidNode = getNodeWithVectorCoordinates(ref coordinatesToNode, currentCentroid);
			AddToDictionary(ref nodeToTriangles, centroidNode, meshTriangles[i]);
			sideToNode[GetPairString (currentCentroid, currentCentroid) + " middle"] = centroidNode;

		}

		//create list of item nodes
		List<Node> itemNodes = new List<Node>();
		foreach (GameObject item in ItemsAI.itemList) {
			Node currentItemNode = new Node (ItemsAI.objectToPosition [item]);
			itemNodes.Add(currentItemNode);
			PathPlanningDataStructures.nodeToCount[currentItemNode.position] = 0;
		}

		//set neighbors of each node
		foreach (var item in nodeToTriangles) {
			Node currentNode = item.Key;
			//iterate through all triangles that contain the currentNode on a side
			foreach (Triangle t in item.Value) {
				//centroid of the triangle
				Vector3 currentCentroid = t.Centroid();
				//list of sides of the triangle
				List<Vector3Pair> triangleSides = new List<Vector3Pair>();
				triangleSides.Add(new Vector3Pair(t.vertex1, t.vertex2));
				triangleSides.Add(new Vector3Pair(t.vertex2, t.vertex3));
				triangleSides.Add(new Vector3Pair(t.vertex1, t.vertex3));
				//iterate through each item node to check if it is contained within the current triangle; if so,
				//make the centroid of the triangle, currentNode, and the item neighbors of each other
				//bool[] nodeInTriangle = new bool[itemNodes.Count];
				for(int i = 0; i < itemNodes.Count; i++) {
					Node currentItemNode = itemNodes [i];
					if (t.PointInTriangle (currentItemNode.position)) {
						addNodeNeighbor(sideToNode, ref currentItemNode, currentCentroid, currentCentroid, "middle");
						itemNodes[i].neighbors.Add (currentNode);
						currentNode.neighbors.Add (itemNodes [i]);
					}
				}
				foreach (Vector3Pair triangleSide in triangleSides) {
					Vector3 currentFirst = triangleSide.first;
					Vector3 currentSecond = triangleSide.second;
					addNodeNeighbor(sideToNode, ref currentNode, currentFirst, currentSecond, "middle");
					addNodeNeighbor(sideToNode, ref currentNode, currentFirst, currentSecond, "outer1");
					addNodeNeighbor(sideToNode, ref currentNode, currentFirst, currentSecond, "outer2");
					for (int i = 0; i < itemNodes.Count; i++) {
						Node currentItemNode = itemNodes [i];
						if (t.PointInTriangle (currentItemNode.position)) {
							addNodeNeighbor(sideToNode, ref currentItemNode, currentFirst, currentSecond, "middle");
							addNodeNeighbor(sideToNode, ref currentItemNode, currentFirst, currentSecond, "outer1");
							addNodeNeighbor(sideToNode, ref currentItemNode, currentFirst, currentSecond, "outer2");
						}
					}
				}
				addNodeNeighbor(sideToNode, ref currentNode, currentCentroid, currentCentroid, "middle");
			}
			nodes.Add(currentNode);
			PathPlanningDataStructures.nodeToCount[currentNode.position] = 0;
		}
		//set end node of the cars
		endNode = getClosestNode (GameObject.Find("FinishLine").transform.position);
	}

	// <summary>
	// Returns the node that corresponds to the coordinates of the given Vector3. Checks
	// for the node corresponding to the key constructured from the coordinates in the 
	// coordinatesToNode dictionary; if not in the dictionary, creates the node and adds
	// it to the dictionary.
	// </summary>
	// <param name="coordinatesToNode"> 
	// dictionary containing mappings from the coordinates of a Vector3 to the corresponding
	// node
	// </param>
	// <param name="givenVector"> a Vector3</param>
	public Node getNodeWithVectorCoordinates(ref Dictionary<string, Node> coordinatesToNode,  
	Vector3 givenVector) {
		string vectorKey = givenVector.x + "," + givenVector.y + "," + givenVector.z;
		Node nodeOfVector;
		if (coordinatesToNode.ContainsKey(vectorKey)) {
			nodeOfVector = coordinatesToNode[vectorKey];
		} else {
			nodeOfVector = new Node(givenVector);
			coordinatesToNode.Add(vectorKey, nodeOfVector);
		}
		return nodeOfVector;
	}


	// <summary>
	// Given a Vector3 pos, returns the Node in the list of Nodes that is closest to it.
	// </summary>
	// <param name="pos"> a Vector3 </param>
	public Node getClosestNode(Vector3 pos) {
		float minimumDistance = Mathf.Infinity; 
		Node closestNode = null; 
		foreach (Node node in nodes) {
			float distance = Vector3.Distance(node.position, pos);
			if (distance < minimumDistance) {
				closestNode = node; 
				minimumDistance = distance; 
			}
		}
		return closestNode; 
	}
	// <summary>
	// Adds a neighbor to a given Node. The value of the neighbor is constructed from the
	// sideToNode dictionary that is passed in; the dictionary requires a key specified
	// by two Vector3 points and a laneName
	// </summary>
	// <param name="sideToNode">
	// Maps a key specified by two Vector3 points (to specify a side) and a 
	// laneName ("middle", "outer1", or "outer2") to a Node
	// </param>
	// <param name="givenNode"> a Node to add a neighbor to </param>
	// <param name="first"> the first Vector3 point </param>
	// <param name="second"> the second Vector3 point </param>
	// <param name="laneName"> the lane name ("middle", "outer1", or "outer2") </param>
	public void addNodeNeighbor(Dictionary<string, Node> sideToNode, ref Node givenNode, 
	Vector3 first, Vector3 second, string laneName) {
		if (sideToNode.ContainsKey(GetPairString (first, second) + " " + laneName)) {
			Node neighbor = sideToNode[GetPairString (first, second) + " " + laneName];
			if (neighbor != givenNode) {
				givenNode.neighbors.Add (neighbor);
			}
		}
	}
	
	
	// <summary>
	// Given a dictionary that maps a Node to the list of Triangles that contain 
	// that Node on a side, add the value to the list of Triangles that the key 
	// currently maps to
	// </summary>
	// <param name="dict"> 
	// the dictionary that maps a Node to the list of Triangles that contain 
	// that Node on a side
	// </param>
	// <param name="key"> a Node </param>
	// <param name="value"> a Triangle to add to the list of Triangles that the key currently maps to </param>
	public void AddToDictionary(ref Dictionary<Node, List<Triangle>> dict, Node key, Triangle value) {
		if (dict.ContainsKey (key)) {
			List<Triangle> currentNodes = dict[key];
			currentNodes.Add(value);
			dict[key] = currentNodes;
		} else {
			List<Triangle> newNodes = new List<Triangle>();
			newNodes.Add(value);
			dict.Add(key, newNodes);
		}
	}

	// <summary>
	// Given two Vector3 objects, creates a string representation of that pair with the
	// smaller Vector3 object coming first (ex: (1,2,3) and (4,5,6) will be respresented 
	// as "1,2,3 - 4,5,6" with the smaller Vector3 coming first)
	// </summary>
	// <param name="v1"> the first given Vector3 </param>
	// <param name="v2"> the second given Vector3 </param>
	public string GetPairString(Vector3 v1, Vector3 v2) {
		float[] v1Components = new float[3];
		v1Components[0] = v1.x;
		v1Components[1] = v1.y;
		v1Components[2] = v1.z;
		float[] v2Components = new float[3];
		v2Components[0] = v2.x;
		v2Components[1] = v2.y;
		v2Components[2] = v2.z;
		Vector3 first = v1;
		Vector3 second = v2;
		for (int i = 0; i < 3; i++) {
			if (v1Components[i] > v2Components[i]) {
				Vector3 temp = second;
				second = first;
				first = temp;
				break;
			} else if(v1Components[i] < v2Components[i]) {
				break;
			}
		}
		return first.x + "," + first.y + "," + first.z + " - " + second.x + "," +
			second.y + "," + second.z;
	}

	public int Size() {
		return nodes.Count;
	}

	// <summary>
	// Returns a string representation of all the triangles of the nodes
	// </summary>
	public override string ToString() {
		string return_string = "";
		foreach (Node node in nodes) {
			 return_string += "\n" + (node.position.ToString());
		}
		return return_string;
	}

	public string ToStringWithNeighbors() {
		string return_string = "";
		Dictionary<Node, int> pairToNodes =
			new Dictionary<Node, int>();
		for (int i = 0; i < nodes.Count; i++) {
			pairToNodes.Add (nodes[i], i);
		}
		for (int i = 0; i < nodes.Count; i++) {
			return_string += "\n" + "Node: " + i + " has neighbors ";
			for (int j = 0; j < nodes[i].neighbors.Count; j++) {
				return_string += pairToNodes[nodes[i].neighbors[j]] + ", ";
			}
		}
		return return_string;
	}

	public class Vector3Pair {
		public Vector3 first; //represents the first Vector3 in the Vector3Pair
		public Vector3 second; //represents the second Vector3 in the Vector3Pair

		public Vector3Pair(Vector3 first, Vector3 second) {
			this.first = first;
			this.second = second;
		}
	}
		
}