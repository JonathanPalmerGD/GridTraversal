using UnityEngine;
using System.Collections;
//including some .NET for dynamic arrays called List in C#
using System.Collections.Generic;

public class Flocking : MonoBehaviour
{
	// Each vehicle contains a CharacterController which
	// makes it easier to deal with the relationship between
	// movement initiated by the character and the forces
	// generated by contact with the terrain & other game objects.
	private CharacterController characterController = null;
	private Steering steerer = null;
	private FlockManager flockManager = null;
	//public float[] Wants;
	//public float[] WantRate;

	private int currentDesire = 0;
	public LineRenderer lineRenderer;
	public Queue<int> wantQueue;
	public Stack<int> currentPath;
	public float[] distances;
	public float dottedPathDist = 5.0f;
	public Object headPrefab;
	public GameObject head;
	public int objective;
	public Object pathSegment;
	public Vector2 lineStartEnd;

	private List<GameObject> pathSegments = new List<GameObject>();
	public List<GameObject> PathSegments { get { return pathSegments; } }

	public int totalWants = 8;

	#region flockingStuff
	// a unique identification number assigned by the flock manager 
	private int index = -1;
	public int Index {
		get { return index; }
		set { index = value; }
	}

	public int[] currentNodeList;
	public int maxPaths;

	//movement variables
	private float gravity = 20.0f;
	private Vector3 moveDirection;

	//steering variable
	private Vector3 steeringForce;

	//list of nearby flockers
	private List<GameObject> nearFlockers = new List<GameObject> ();
	private List<float> nearFlockersDistances = new List<float> ();
	#endregion

	public void Start ()
	{
		currentPath = new Stack<int>();
		head = ((GameObject)Instantiate(headPrefab, new Vector3(transform.position.x, transform.position.y+1, transform.position.z), Quaternion.identity));
		head.transform.SetParent(this.transform);
		
		//get component reference
		characterController = gameObject.GetComponent<CharacterController> ();
		steerer = gameObject.GetComponent<Steering> ();
		moveDirection = transform.forward;


		List<Vector2> tempList = new List<Vector2>();
		Stack<Vector2> tempStack = new Stack<Vector2>();
		for(int i = 0; i < 6; i++)
		{
			Vector2 newV2 = new Vector2(i, Random.Range(5, 50));
			tempList.Add(newV2);
			//Debug.Log(newV2);
		}

		//Debug.Log("Sorting");

		foreach (Vector2 v2 in SortStack(tempList))
		{
			//Debug.Log(v2);
		}

	}

	public void SetUpWants()
	{
		for (int i = 0; i < 5; i++)
		{
			wantQueue.Enqueue(Random.Range(0, 8));
		}
	}

	public void SetWant(int newWant)
	{
		wantQueue.Clear();
		currentPath.Clear();
		wantQueue.Enqueue(newWant);
		currentDesire = newWant;

		head.GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);
		GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);
	}

	public void UpdateWants(bool newFrame)
	{
		currentDesire = wantQueue.Peek();

		//Body and head coloring for visibility
		head.GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);
		GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);
	}

	// get a reference to the manager's FlockManager component (script)
	public void setFlockManager(GameObject fManager)
	{
		wantQueue = new Queue<int>();
		lineRenderer = GetComponent<LineRenderer>();
		flockManager = fManager.GetComponent<FlockManager> ();
		totalWants = flockManager.colors.Length;
		SetUpWants();
		maxPaths = flockManager.nodeGridSize * 2;
	}

	#region Flocking Functions
	private Vector3 Alignment ()
	{
		//This one isn't worth giving a color. You're ALWAYS aligning to the swarm.
		return steerer.AlignTo(flockManager.FlockDirection);
	}

	private Vector3 Cohesion ()
	{
		if (Vector3.Distance(transform.position, flockManager.centroid.transform.position) > flockManager.tightDist)
		{
			//We're too far away, show it!
			GetComponent<Renderer>().material.color = Color.blue;
			return steerer.Seek(flockManager.Centroid);
		}
		else
		{
			return new Vector3(0, 0, 0);
		}
	}

	private Vector3 Separation ()
	{
		//empty our lists
		nearFlockers.Clear ();
		nearFlockersDistances.Clear ();
		
		//I didnt really get how you wanted us to do this.
		//I tried a couple other versions.
		//I plan on only calculating the distances once when I work on the 3d world.


		for (int i = 0; i < flockManager.Flockers.Count; i++)
		{
			float dist = Vector3.Distance(flockManager.Flockers[i].transform.position, transform.position);
			if (dist < flockManager.separationDist && dist > 0)
			{
				Debug.DrawLine(transform.position, flockManager.Flockers[i].transform.position, Color.magenta, .5f);
				//Say that we're spacing ourselves from others
				GetComponent<Renderer>().material.color = Color.red;
				return steerer.Flee(flockManager.Flockers[i]);
			}
		}

		//******* write this - it won't work as is ***********
		
		Vector3 dv = transform.forward;



		return steerer.AlignTo(dv);
	}

	// tether type containment - not very good!
	private Vector3 stayInBounds ( float radius, Vector3 center)
	{
		if(Vector3.Distance(transform.position, center) > radius)
			return steerer.Seek(center);
		else
			return Vector3.zero;
	}
	#endregion

	private void ClampSteering()
	{
		if (steeringForce.magnitude > steerer.maxForce)
		{
			steeringForce.Normalize();
			steeringForce *= steerer.maxForce;
		}
	}

	private void Arrival(float percentDecline)
	{
		steeringForce *= (steerer.maxForce * percentDecline);
	}

	// Update is called once per frame
	public void Update ()
	{
		//First time of the frame we want to update our wants and call aStar if needed.
		UpdateWants(true);

		head.GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);
		GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);

		//These are important
		CalcSteeringForce ();
		ClampSteering ();

		int myIndex = flockManager.Flockers.IndexOf(this.gameObject);
		//If the distance of this [(position + vector3) to objective] is more than [position to objective]

		// movedirection equals velocity
		moveDirection = transform.forward * steerer.Speed;

		//add acceleration
		moveDirection += 4 * steeringForce * Time.deltaTime;

		//update speed
		steerer.Speed = moveDirection.magnitude;

		if (steerer.Speed != moveDirection.magnitude) 
		{
			moveDirection = moveDirection.normalized * steerer.Speed;
		}

		//orient transform
		if (moveDirection != Vector3.zero)
			transform.forward = moveDirection;
		
		// Apply gravity
		moveDirection.y -= gravity;
		
		// the CharacterController moves us subject to physical constraints
		characterController.Move(moveDirection * Time.deltaTime);
		if (head != null)
		{
			//Update body and head color.
			GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);
			head.GetComponent<Renderer>().material.color = flockManager.GetFlockerColor(currentDesire);
			head.transform.position = new Vector3(transform.position.x, transform.position.y + 1.3f, transform.position.z);
		}

		if (flockManager.debugLines)
		{
			//Draw the debug path.
			DrawPath();
		}
	}

	private float GetDistance(Vector3 firstPos, Vector3 secondPos)
	{
		float dist = 0;

		dist = Mathf.Sqrt((firstPos.x + secondPos.x) + (firstPos.y + secondPos.y));

		return dist;
	}

	private int FindNearestNode()
	{
		//Figure out our x column
		int xAxis = 0;
		float currentX = 1000000f;
		//Figure out our z row
		int zAxis = 0;
		float currentZ = 1000000f;

		for (int i = 0; i < flockManager.nodeGridSize; i++)
		{
			if (Mathf.Abs((transform.position.x - flockManager.NodeList[(flockManager.nodeGridSize * i)].transform.position.x)) < currentX)
			{
				xAxis = i;
				currentX = Mathf.Abs((transform.position.x - flockManager.NodeList[(flockManager.nodeGridSize * i)].transform.position.x));
			}
			if (Mathf.Abs((transform.position.z - flockManager.NodeList[i].transform.position.z)) < currentZ)
			{
				zAxis = i;
				currentZ = Mathf.Abs((transform.position.z - flockManager.NodeList[i].transform.position.z));
			}
		}
		//Return the node location. I love modulus.
		return ((xAxis * flockManager.nodeGridSize) + zAxis);
	}

	private void DrawDottedPath(Vector3 firstPosition, Vector3 secondPosition, Color colorOfPath)
	{
		float distBetweenPoints = Vector2.Distance(new Vector2(firstPosition.x, firstPosition.z), new Vector2(secondPosition.x, secondPosition.z));
		//Debug.Log("Player Pos: " + transform.position.ToString() + "\nFirst Pos: " + firstPosition);
		//Vector3.Distance(firstPosition, secondPosition);
		//Debug.Log("DBP: " + distBetweenPoints);
		for (float i = 0.0f; i < (int)distBetweenPoints; i += dottedPathDist)
		{
			float dz = firstPosition.z - secondPosition.z;
			float dx = firstPosition.x - secondPosition.x;
			float angle = (Mathf.Asin(dz / distBetweenPoints) * 180 / Mathf.PI);
			Vector3 positionOfNewPoint = new Vector3(firstPosition.x + (dottedPathDist * Mathf.Cos(angle)), secondPosition.y, firstPosition.z + (dottedPathDist * Mathf.Sin(angle)));
			//Vector3 positionOfNewPoint = new Vector3(firstPosition.x * dottedPathDist * Mathf.Cos(theta), firstPosition.y * dottedPathDist * Mathf.Sin(theta), firstPosition.y - 1);
			Debug.Log("DX: " + dx + "\nDZ: " + dz + "\nPONP: " + positionOfNewPoint.ToString() + "\nAngle: " + angle);

			//Debug.DrawLine(firstPosition, positionOfNewPoint, Color.yellow, 5.0f);
			//Debug.DrawLine(positionOfNewPoint, secondPosition, Color.green, 5.0f);

			CreatePathPoint(positionOfNewPoint, angle, colorOfPath);
		}



// 		for (float i = 0.0f; i < (int)distBetweenPoints; i += dottedPathDist)
// 		{
// 			float dz = secondPosition.z - firstPosition.z;
// 			float dx = secondPosition.x - firstPosition.x;
// 			float theta = Mathf.Asin(dz / distBetweenPoints);
// 			Vector3 positionOfNewPoint = new Vector3(firstPosition.x + (dottedPathDist * Mathf.Cos(theta)), firstPosition.y - 1, firstPosition.z + ( dottedPathDist * Mathf.Sin(theta))); 
// 			//Vector3 positionOfNewPoint = new Vector3(firstPosition.x * dottedPathDist * Mathf.Cos(theta), firstPosition.y * dottedPathDist * Mathf.Sin(theta), firstPosition.y - 1);
// 			//Debug.Log("DX: " + dx + "\nDZ: " + dz + "\nPONP: " + positionOfNewPoint.ToString() + "\nTHTA: " + theta);
// 			createPathPoint(positionOfNewPoint, theta, colorOfPath);
// 		}
	}

	private void CreatePathPoint(Vector3 positionOfSegment, float angle, Color colorOfSegment)
	{
//		PathSegment ps = new PathSegment();
//		ps.transform.position = positionOfSegment;
// 		ps.transform.rotation = Quaternion.identity;
// 		ps.transform.RotateAround(Vector3.up, theta * 180 / Mathf.PI);
		//ps.renderer.material.color = colorOfSegment;

		pathSegments.Add((GameObject)Instantiate(pathSegment, positionOfSegment, Quaternion.identity));
		PathSegments[PathSegments.Count - 1].GetComponent<Renderer>().material.color = colorOfSegment;
		PathSegments[PathSegments.Count - 1].GetComponent<Renderer>().transform.rotation = Quaternion.identity;
		PathSegments[PathSegments.Count - 1].GetComponent<Renderer>().transform.RotateAround(Vector3.up, angle);
	
 	}

	private void DrawPath()
	{
		int[] nodePath = currentPath.ToArray();
		Vector3 targetVector;
		float heightDiff = .0f;
		lineRenderer.SetWidth(lineStartEnd.x, lineStartEnd.y);
		lineRenderer.material.color = flockManager.GetFlockerColor(currentDesire);
		lineRenderer.SetVertexCount(nodePath.Length + 1);

		targetVector = new Vector3(transform.position.x, heightDiff, transform.position.z);
		lineRenderer.SetPosition(0, targetVector);

		if (nodePath.Length > 0)
		{
			lineRenderer.SetPosition(0, targetVector);
			for (int i = 0; i < nodePath.Length; i++)
			{
				targetVector = new Vector3(flockManager.NodeList[nodePath[i]].transform.position.x, flockManager.NodeList[nodePath[i]].transform.position.y + heightDiff, flockManager.NodeList[nodePath[i]].transform.position.z);
				lineRenderer.SetPosition(i + 1, targetVector);
			}
		}
	}

	private void CalcSteeringForce()
	{
		steeringForce = Vector3.zero;

		//If we have no path currently
		if (currentPath.Count == 0)
		{
			//Find the next highest want
			UpdateWants(false);

			//Construct a path to the objective.
			currentPath = aStar(FindNearestNode(), flockManager.wantedNodes[currentDesire]);
		}
		else
		{
			if (flockManager.NodeList[currentPath.Peek()].GetComponent<PathNode>().passable)
			{
				if (Vector3.Distance(transform.position, flockManager.NodeList[currentPath.Peek()].transform.position) > flockManager.toTargetDist)
				{
					//If we aren't at the next node in the list
					//Go to the next node in the NodeList
					steeringForce += steerer.Seek(flockManager.NodeList[currentPath.Peek()]) * 8;
				}
				else
				{
					//Does what it says on the tin.
					ReachedNode();
				}
			}
			else
			{
				if (Vector3.Distance(transform.position, flockManager.NodeList[currentPath.Peek()].transform.position) > flockManager.toTargetDist * 2.3f)
				{
					//If we aren't at the next node in the list
					//Go to the next node in the NodeList
					steeringForce += steerer.Seek(flockManager.NodeList[currentPath.Peek()]) * 8;
				}
				else
				{
					//Does what it says on the tin.
					ReachedNode();
				}
			}
		}

		steeringForce += Separation();

		#region Bad Avoid
		/*//Avoid others
		//This is avoid code. I commented it out because it looked really awful with the camera's following.
		//I'd want to rewrite the camera
		float lowest = float.MaxValue;
		int lowestIndex = 0;
		for (int i = 0; i < flockManager.Flockers.Count; i++)
		{
			if (flockManager.getDistance(Index, i) < flockManager.separationDist)
			{
				Debug.DrawLine(transform.position, flockManager.Flockers[i].transform.position, Color.magenta, 3.5f);
				Debug.Log("Somebody is in my personal space");
				steeringForce += steerer.Flee(flockManager.Flockers[i]) * flockManager.separationWt;
			}


			//if (flockManager.getDistance(Index, i) < float.MaxValue)
			//{
			//    lowest = flockManager.getDistance(Index, i);
			//    lowestIndex = i;
			//}

			////Avoid the closest flocker.
			//if (lowest < flockManager.separationDist)
			//{
			//    Debug.DrawLine(transform.position, flockManager.Flockers[lowestIndex].transform.position, Color.magenta, 3.0f);
			//    //Debug.Log("Somebody is in my personal space");
			//    steeringForce += steerer.Flee(flockManager.Flockers[lowestIndex]) * flockManager.separationWt;
			//}
		}*/
		#endregion
	}

	public void ReachedNode()
	{
		int[] nodePath = currentPath.ToArray();
		//If we're in debug mode.
		if (flockManager.debugLines)
		{
			//Debug.DrawLine(transform.position, flockManager.NodeList[currentPath.Peek()].transform.position, Color.red, 3.0f);
		}
		//If we reached the objective
		if (currentPath.Peek() == flockManager.wantedNodes[currentDesire])
		{
			wantQueue.Dequeue();

			//Add a new objective
			wantQueue.Enqueue(Random.Range(0, 7));

			currentDesire = wantQueue.Peek();

			//aStar to find the new path.
			currentPath = aStar(FindNearestNode(), flockManager.wantedNodes[currentDesire]);
		}
		else
		{	
			//Move on to the next node.
			currentPath.Pop();
		}
	}

	public void ResetPath()
	{
		currentPath.Clear();
	}

	public Stack<int> aStar(int start, int destination)
	{
		//Nodes that need to be visited.
		Stack<int> openList = new Stack<int>();
		//Visited nodes
		Stack<int> closedList = new Stack<int>();
		//To show where we've been. This maps each node to it's most efficient path home.
		Dictionary<int, int> map = new Dictionary<int, int>();

		//Start our list
		openList.Push(start);

		//While there are viable nodes to check.... figure it out
		while (openList.Count > 0)
		{
			//Get the necessary component
			PathNode currentNode = flockManager.NodeList[openList.Peek()].GetComponent<PathNode>();
			
			//Pop the current node off the open list
			openList.Pop();

			#region Destination Check
			//Are we there yet?
			if (currentNode.nodeIndex == destination)
			{
				//Send back a reconstructed path.
				return ReconstructPath(map, start, destination);
			}
			#endregion

			//Make a queue for the adjacents in order of priority.
			Stack<int> orderedAdj = new Stack<int>();
			List<Vector2> possibleOptions = new List<Vector2>();

			distances = new float[8];
			float[] distToNode = new float[8];
			List<int> unaddedAdj = new List<int>();

			#region Distance Calculation
			for (int distanceIndex = 0; distanceIndex < distances.Length; distanceIndex++)
			{
				//If it isnt an edge case (-1 represents edge case)
				if (currentNode.adjacent[distanceIndex] != -1)
				{
					//Debug.DrawLine(flockManager.NodeList[currentNode.nodeIndex].transform.position, flockManager.NodeList[currentNode.adjacent[distanceIndex]].transform.position, Color.magenta, 2.5f);

					//Calc the general heuristic distance.
					distToNode[distanceIndex] = FindActualDist(currentNode.nodeIndex, currentNode.adjacent[distanceIndex]);
					distances[distanceIndex] = FindActualDist(currentNode.adjacent[distanceIndex], destination);
					unaddedAdj.Add(distanceIndex);
				}
				else
				{
					//Otherwise say we can't go that way.
					distances[distanceIndex] = int.MaxValue;
				}
			}
			#endregion

			#region Find closest path
			for (int i = 0; i < distances.Length; i++)
			{
				//Dead path
				#region If Dead End or Destination Checks
				if (distances[i] > 1000)
				{
					//Do nothing
				}
				else if(currentNode.adjacent[i] == destination)
				{
					//If we are next to our destination. Go straight their. Don't bother with anything else.
					//orderedAdj.Push(i);
					possibleOptions.Add(new Vector2(i, distances[i] + distToNode[i]));
					i = distances.Length;
				}
				#endregion
				else
				{
					for (int k = 0; k < unaddedAdj.Count; k++)
					{
						int highestDistIndex = 9;
						for (int j = 0; j < unaddedAdj.Count; j++)
						{
							if (distances[i] + distToNode[i] >= distToNode[unaddedAdj[j]] + distances[unaddedAdj[j]])
							{
								highestDistIndex = unaddedAdj[j];
							}
						}
						if (highestDistIndex != 9)
						{
							unaddedAdj.Remove(highestDistIndex);
							possibleOptions.Add(new Vector2(highestDistIndex, distances[highestDistIndex] + distToNode[highestDistIndex]));
							orderedAdj.Push(highestDistIndex);
						}

						#region Old Highest Dist Calc
						/*
						int highestDistIndex = 9;
						for (int j = 0; j < unaddedAdj.Count; j++)
						{
							if (distances[i] >= distances[unaddedAdj[j]])
							{
								highestDistIndex = unaddedAdj[j];
							}
						}
						if (highestDistIndex != 9)
						{
							unaddedAdj.Remove(highestDistIndex);
							orderedAdj.Push(highestDistIndex);
						}*/
						#endregion
					}
					#region Old
					//int count = 0;

					//for (int j = 0; j < unaddedAdj.Count; j++)
					//{
					//    if (distances[i] <= distances[unaddedAdj[j]])
					//    {
					//        count++;
					//    }
					//}
					//if (count > unaddedAdj.Count - 1)
					//{
					//    Vector3 positionOfCurNode = flockManager.NodeList[currentNode.nodeIndex].transform.position + Vector3.up * .2f;
					//    string UAADJstring = "";
					//    for (int val = 0; val < unaddedAdj.Count; val++)
					//    {
					//        UAADJstring += unaddedAdj[val];
					//    }
					//    //Debug.Log("I : " + i + " UAADJ : " + UAADJstring);
					//    int selectedNeighborIndex = flockManager.NodeList[currentNode.nodeIndex].GetComponent<PathNode>().adjacent[i];
					//    Vector3 positionOfSelected = flockManager.NodeList[selectedNeighborIndex].transform.position + Vector3.up * .2f;
					//    //Debug.DrawLine(positionOfCurNode, positionOfSelected, Color.blue, 3.0f);
					//    unaddedAdj.Remove(i);
					//    orderedAdj.Push(i);
					//}
					#endregion
				}
			}
			#endregion

			#region Old Adjacent Pushing
			/*
			if (orderedAdj.Count > 0)
			{
				//		7	0	4
				//		3		1
				//		6	2	5
				
				#region Check adjacent[0]'s sides
				if (orderedAdj.Peek() == 0)
				{
					//Debug.Log("0 is direct");
					if (distances[7] < distances[4])
					{
						if (isTrue)
						{
							orderedAdj.Push(7);
							orderedAdj.Push(3);
							orderedAdj.Push(4);
							orderedAdj.Push(6);
							orderedAdj.Push(1);
							orderedAdj.Push(2);
							orderedAdj.Push(5);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(4);
							orderedAdj.Push(1);
							orderedAdj.Push(7);
							orderedAdj.Push(5);
							orderedAdj.Push(3);
							orderedAdj.Push(2);
							orderedAdj.Push(6);
						}
					}
				}
				#endregion
				#region Check adjacent[1]'s sides
				else if (orderedAdj.Peek() == 1)
				{
					//Debug.Log("1 is direct");
					if (distances[4] < distances[5])
					{
						if (isTrue)
						{
							orderedAdj.Push(4);
							orderedAdj.Push(0);
							orderedAdj.Push(5);
							orderedAdj.Push(7);
							orderedAdj.Push(3);
							orderedAdj.Push(2);
							orderedAdj.Push(6);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(5);
							orderedAdj.Push(2);
							orderedAdj.Push(4);
							orderedAdj.Push(6);
							orderedAdj.Push(0);
							orderedAdj.Push(4);
							orderedAdj.Push(7);
						}
					}
				}
				#endregion
				#region Check adjacent[2]'s sides
				else if (orderedAdj.Peek() == 2)
				{
					//Debug.Log("2 is direct");
					if (distances[5] < distances[6])
					{
						if (isTrue)
						{
							orderedAdj.Push(5);
							orderedAdj.Push(1);
							orderedAdj.Push(6);
							orderedAdj.Push(4);
							orderedAdj.Push(0);
							orderedAdj.Push(3);
							orderedAdj.Push(7);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(6);
							orderedAdj.Push(3);
							orderedAdj.Push(5);
							orderedAdj.Push(7);
							orderedAdj.Push(0);
							orderedAdj.Push(1);
							orderedAdj.Push(4);
						}
					}
				}
				#endregion
				#region Check adjacent[3]'s sides
				else if (orderedAdj.Peek() == 3)
				{
					//Debug.Log("3 is direct");
					if (distances[6] < distances[7])
					{
						if (isTrue)
						{
							orderedAdj.Push(6);
							orderedAdj.Push(2);
							orderedAdj.Push(7);
							orderedAdj.Push(5);
							orderedAdj.Push(0);
							orderedAdj.Push(1);
							orderedAdj.Push(4);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(7);
							orderedAdj.Push(0);
							orderedAdj.Push(6);
							orderedAdj.Push(4);
							orderedAdj.Push(1);
							orderedAdj.Push(2);
							orderedAdj.Push(5);
						}
					}
				}
				#endregion

				#region Check adjacent[4]'s sides
				else if (orderedAdj.Peek() == 4)
				{
					//Debug.Log("0 is direct");
					if (distances[0] < distances[1])
					{
						if (isTrue)
						{
							orderedAdj.Push(0);
							orderedAdj.Push(7);
							orderedAdj.Push(1);
							orderedAdj.Push(3);
							orderedAdj.Push(5);
							orderedAdj.Push(2);
							orderedAdj.Push(6);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(1);
							orderedAdj.Push(5);
							orderedAdj.Push(0);
							orderedAdj.Push(2);
							orderedAdj.Push(7);
							orderedAdj.Push(6);
							orderedAdj.Push(3);
						}
					}
				}
				#endregion
				#region Check adjacent[5]'s sides
				else if (orderedAdj.Peek() == 5)
				{
					//Debug.Log("1 is direct");
					if (distances[1] < distances[2])
					{
						if (isTrue)
						{
							orderedAdj.Push(1);
							orderedAdj.Push(4);
							orderedAdj.Push(2);
							orderedAdj.Push(0);
							orderedAdj.Push(7);
							orderedAdj.Push(6);
							orderedAdj.Push(3);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(2);
							orderedAdj.Push(6);
							orderedAdj.Push(1);
							orderedAdj.Push(4);
							orderedAdj.Push(3);
							orderedAdj.Push(7);
							orderedAdj.Push(0);
						}
					}
				}
				#endregion
				#region Check adjacent[6]'s sides
				else if (orderedAdj.Peek() == 6)
				{
					//Debug.Log("2 is direct");
					if (distances[3] < distances[2])
					{
						if (isTrue)
						{
							orderedAdj.Push(3);
							orderedAdj.Push(7);
							orderedAdj.Push(2);
							orderedAdj.Push(0);
							orderedAdj.Push(5);
							orderedAdj.Push(4);
							orderedAdj.Push(1);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(2);
							orderedAdj.Push(5);
							orderedAdj.Push(3);
							orderedAdj.Push(1);
							orderedAdj.Push(7);
							orderedAdj.Push(4);
							orderedAdj.Push(0);
						}
					}
				}
				#endregion
				#region Check adjacent[7]'s sides
				else			//Else 7
				{
					//Debug.Log("3 is direct");
					if (distances[0] < distances[3])
					{
						if (isTrue)
						{
							orderedAdj.Push(0);
							orderedAdj.Push(4);
							orderedAdj.Push(3);
							orderedAdj.Push(1);
							orderedAdj.Push(6);
							orderedAdj.Push(2);
							orderedAdj.Push(5);
						}
					}
					else
					{
						if (isTrue)
						{
							orderedAdj.Push(3);
							orderedAdj.Push(6);
							orderedAdj.Push(0);
							orderedAdj.Push(2);
							orderedAdj.Push(4);
							orderedAdj.Push(1);
							orderedAdj.Push(5);
						}
					}
				}
				#endregion
			}
			*/
			#endregion

			#region Sorting Stack Mapping
			Stack<Vector2> sortedOptions = SortStack(possibleOptions);

			int numCount = sortedOptions.Count;
			for (int i = 0; i < numCount; i++)
			{
				//If we haven't mapped it
				if (!map.ContainsKey(currentNode.adjacent[(int)sortedOptions.Peek().x]))
				{
					//Debug.DrawLine(currentNode.transform.position + Vector3.up * .2f, flockManager.NodeList[currentNode.adjacent[(int)sortedOptions.Peek().x]].transform.position + Vector3.up * .2f, Color.green, 2.0f);

					//Add it to the open list.
					openList.Push(currentNode.adjacent[(int)sortedOptions.Peek().x]);

					//And map it to it's parent as the most efficient way (so far) to get here.
					map.Add(currentNode.adjacent[(int)sortedOptions.Peek().x], currentNode.nodeIndex);
				}
				else
				{
					//Evaluate to see if we have found a better path to this node. 
					//Not really needed when obstacles are implemented.
				}

				//Remove the most recent adjacent from the queue.
				sortedOptions.Pop();
			}
			#endregion

			#region Mapping Section
			/*
			//So we don't break out for loop by changing what's in orderedAdj
			int numCount = orderedAdj.Count;
			for (int i = 0; i < numCount; i++)
			{
				//If we haven't mapped it
				if (!map.ContainsKey(currentNode.adjacent[orderedAdj.Peek()]))
				{
					Debug.DrawLine(currentNode.transform.position + Vector3.up * .2f, flockManager.NodeList[currentNode.adjacent[orderedAdj.Peek()]].transform.position + Vector3.up * .2f, Color.green, 6.0f);

					//Add it to the open list.
					openList.Push(currentNode.adjacent[orderedAdj.Peek()]);

					//And map it to it's parent as the most efficient way (so far) to get here.
					map.Add(currentNode.adjacent[orderedAdj.Peek()], currentNode.nodeIndex);
				}
				else// if(map[currentNode.adjacent[orderedAdj.Peek()]]
				{
					//Evaluate to see if we have found a better path to this node. 
					//Not really needed when obstacles are implemented.
				}

				//Remove the most recent adjacent from the queue.
				orderedAdj.Pop();
			}
			 * */
			#endregion
		}

		//This will send the flocker directly to the destination if they fail.
		//This shouldn't occur unless obstacles make the route impossible to find.
		openList = new Stack<int>();
		openList.Push(destination);
		return openList;
	}

	public Stack<Vector2> SortStack(List<Vector2> options)
	{
		Stack<Vector2> newStack = new Stack<Vector2>();

		while(options.Count > 0)
		{
			int bestIndex = 0;
			for (int i = 0; i < options.Count; i++)
			{
				if (options[bestIndex].y > options[i].y)
				{
					bestIndex = i;
				}
			}

			newStack.Push(options[bestIndex]);
			options.RemoveAt(bestIndex);
		}

		return newStack;
	}

	public float FindActualDist(int index, int destination)
	{
		PathNode currentNode = flockManager.NodeList[index].GetComponent<PathNode>();
		
		//To support obstacles when they are added.
		if (currentNode.passable)
		{
			Vector2 indexV2 = new Vector2(flockManager.NodeList[index].transform.position.x, flockManager.NodeList[index].transform.position.z);
			Vector2 destV2 = new Vector2(flockManager.NodeList[destination].transform.position.x, flockManager.NodeList[destination].transform.position.z);

			//Debug.DrawLine(new Vector3(indexV2.x, transform.position.y + 6.5f, indexV2.y), new Vector3(destV2.x, transform.position.y - 1, destV2.y), Color.red, 3.0f);
			return Vector2.Distance(indexV2, destV2);
		}
		else
		{
			return int.MaxValue;
		}
	}

	public int FindDistance(int index, int destination)
	{
		//Get our current node
		PathNode currentNode = flockManager.NodeList[index].GetComponent<PathNode>();
		
		//To support obstacles when they are added.
		if (currentNode.passable)
		{
			//Find x,z coordinate of the destination
			int xDest = destination % flockManager.nodeGridSize;
			int zDest = (int)(destination / flockManager.nodeGridSize);

			//Find the x,z coord we are at right now
			int xCurr = index % flockManager.nodeGridSize;
			int zCurr = (int)(index / flockManager.nodeGridSize);

			//Find the absolute difference. Multiply by 10.
			int xDiff = Mathf.Abs(xCurr - xDest) * 10;
			int zDiff = Mathf.Abs(zCurr - zDest) * 10;

			//Diagnols are an option if you solve and multiply by 14 (Diagnol is approx 1.4 distance)
			//This would allow for good approximation.

			Debug.Log(xDiff + zDiff);

			//Return x and z added.
			return (xDiff + zDiff);
		}
		else
		{
			//Not passible.
			return int.MaxValue;
		}
	}

	public Stack<int> ReconstructPath(Dictionary<int, int> cameFrom, int start, int currentNode)
	{
		//This one is a doosey. It took a bit to figure out.

		//Our stack to return
		Stack<int> returnToSender = new Stack<int>();

		//The node we're presently evaluating
		int evalNode = currentNode;

		//Add the first node to the stack
		returnToSender.Push(evalNode);

		//If we haven't gotten back to start, keep going
		while (evalNode != start)
		{
			//Add the best node from current eval node
			returnToSender.Push(cameFrom[evalNode]);

			//Change the evaluation node
			evalNode = returnToSender.Peek();
		}

		//Return our path
		return returnToSender;
	}
}