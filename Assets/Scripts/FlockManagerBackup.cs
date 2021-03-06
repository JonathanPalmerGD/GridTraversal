using UnityEngine;
using System.Collections;
//including some .NET for dynamic arrays called List in C#
using System.Collections.Generic;

public class FlockManagerBackup : MonoBehaviour
{
	// weight parameters are set in editor and used by all flockers 
	// if they are initialized here, the editor will override settings	 
	// weights used to arbitrate btweeen concurrent steering forces 
	public float alignmentWt;
	public float separationWt;
	public float cohesionWt;
	public float avoidWt;
	public float inBoundsWt;
	public float tightDist;
	public float toTargetDist = 4;
	private int maxPaths;			//To tell the astar algorithm to give up
	
	// these distances modify the respective steering behaviors
	public float avoidDist;
	public float separationDist;

	public bool debugLines = true;

	public Color[] colors = new Color[8] { Color.red, Color.green, Color.blue, Color.cyan, Color.yellow, Color.magenta, Color.gray, Color.black };
	private List<GameObject> nodeList = new List<GameObject>();
	public List<GameObject> NodeList { get { return nodeList; } }
	public int nodeGridSize = 4;
	public int nodeHeight = 5;
	public int[] wantedNodes = new int[8];

	public float flockerMaxSpeed = 8;

	// set in editor to promote reusability.
	public int numberOfFlockers;
	public Object flockerPrefab;
	public Object obstaclePrefab;
	public Object pathNodePrefab;
	
	//values used by all flockers that are calculated by controller on update
	private Vector3 flockDirection;
	public GameObject centroid;
	
	//accessors
	public Vector3 FlockDirection { get {return flockDirection;} }
	public GameObject Centroid { get {return centroid; } }
		
	// list of flockers with accessor
	private List<GameObject> flockers = new List<GameObject>();
	public List<GameObject> Flockers {get{return flockers;}}

	// array of obstacles with accessor
	private  GameObject[] obstacles;
	public GameObject[] Obstacles {get{return obstacles;}}
	
	// this is a 2-dimensional array for distances between flockers
	// it is recalculated each frame on update
	private float[,] distances;
	
	public void Start ()
	{
		setUpNodes();
		//construct our 2d array based on the value set in the editor
		distances = new float[numberOfFlockers, numberOfFlockers];

		
	
		obstacles = GameObject.FindGameObjectsWithTag("obstacle");
		AddFlockers(numberOfFlockers);
	}

	public void AddFlockers(int newFlockerCount)
	{
		//reference to Vehicle script component for each flocker
		Flocking flocker; // reference to flocker scripts
		for (int i = 0; i < newFlockerCount; i++)
		{
			//Instantiate, set its variables.
			flockers.Add((GameObject)Instantiate(flockerPrefab, new Vector3(transform.position.x + 10 + i, 5, transform.position.z + i * 1.2f), Quaternion.identity));
			//grab a component reference
			flocker = flockers[i].GetComponent<Flocking>();
			flockers[i].GetComponent<Steering>().maxSpeed = flockerMaxSpeed;
			//set values in the Vehicle script
			flocker.Index = i;
			flocker.setFlockManager(Centroid);
		}
	}

	public void AddFlocker()
	{
		GameObject flockerGO = (GameObject)Instantiate(flockerPrefab, new Vector3(transform.position.x + 10, 5, transform.position.z), Quaternion.identity);
		//grab a component reference
		Flocking flocker = flockerGO.GetComponent<Flocking>();
		flockerGO.GetComponent<Steering>().maxSpeed = flockerMaxSpeed;
		//set values in the Vehicle script
		flocker.Index = flockers.Count;
		flocker.setFlockManager(Centroid);
		flockers.Add(flockerGO);
	}

	public void changeFlockerSpeed(float newSpeed)
	{
		flockerMaxSpeed = newSpeed;
		for (int i = 0; i < numberOfFlockers; i++)
		{
			flockers[i].GetComponent<Steering>().maxSpeed = flockerMaxSpeed;
		}
	}

	public void setUpNodes()
	{
		maxPaths = nodeGridSize * 2;

		PathNode node; // reference to flocker scripts
		for (int i = 0; i < nodeGridSize; i++)
		{
			for (int j = 0; j < nodeGridSize; j++)
			{
				NodeList.Add((GameObject)Instantiate(pathNodePrefab, new Vector3(i * 10 + transform.position.x, nodeHeight, j * 10 + transform.position.z), Quaternion.identity));
				
				//get the node reference
				node = NodeList[(i * nodeGridSize) + (j)].GetComponent<PathNode>();
				//Set the  node's index and manager
				node.nodeIndex = (i * nodeGridSize) + (j);
				
				node.setNodeManager(Centroid);

				#region Top
				if (i == 0)
				{
					node.adjacent[0] = -1;
				}
				else
				{
					node.adjacent[0] = node.nodeIndex - (nodeGridSize + 0);
				}
				#endregion
				#region Right
				if (j == nodeGridSize - 1)
				{
					node.adjacent[1] = -1;
				}
				else
				{
					node.adjacent[1] = node.nodeIndex + 1;
				}
				#endregion
				#region Bottom
				if (i == nodeGridSize-1)
				{
					node.adjacent[2] = -1;
				}
				else
				{
					node.adjacent[2] = node.nodeIndex + nodeGridSize;
				}
				#endregion
				#region Left
				if (j == 0)
				{
					node.adjacent[3] = -1;
				}
				else
				{
					node.adjacent[3] = node.nodeIndex - 1;
				}
				#endregion
				#region Top Right
				if (i == 0 ||j == nodeGridSize - 1)
				{
					node.adjacent[4] = -1;
				}
				else
				{
					node.adjacent[4] = node.nodeIndex - (nodeGridSize - 1);
						//(j - 1) * nodeGridSize + (i + 1);
				}
				#endregion
				#region Bottom Right
				if (i == nodeGridSize - 1 || j == nodeGridSize - 1)
				{
					node.adjacent[5] = -1;
				}
				else
				{
					node.adjacent[5] = node.nodeIndex + (nodeGridSize + 1);
						//(j + 1) * nodeGridSize + (i + 1);
				}
				#endregion
				#region Bottom Left
				if (i == nodeGridSize - 1 || j == 0)
				{
					node.adjacent[6] = -1;
				}
				else
				{
					node.adjacent[6] = node.nodeIndex + (nodeGridSize - 1);
						//(j + 1) * nodeGridSize + (i - 1);
				}
				#endregion
				#region Top Left
				if (i == 0 || j == 0)
				{
					node.adjacent[7] = -1;
				}
				else
				{
					node.adjacent[7] = node.nodeIndex - (nodeGridSize + 1);
						//(j - 1) * nodeGridSize + (i - 1);
				}
				#endregion

				#region Old Adjacents
				/*//Top
				if (i == 0)
				{
					node.adjacent[0] = -1;
				}
				else
				{
					node.adjacent[0] = ((i - 1) * nodeGridSize) + (j);
				}

				//Right
				if (j == nodeGridSize - 1)
				{
					node.adjacent[1] = -1;
				}
				else
				{
					node.adjacent[1] = ((i) * nodeGridSize) + (j+1);
				}

				//Bottom
				if (i == nodeGridSize-1)
				{
					node.adjacent[2] = -1;
				}
				else
				{
					node.adjacent[2] = ((i + 1) * nodeGridSize) + (j);
				}

				//Left
				if (j == 0)
				{
					node.adjacent[3] = -1;
				}
				else
				{
					node.adjacent[3] = ((i) * nodeGridSize) + (j - 1);
				}
			*/
				#endregion
			}
			
		}

		//Pick 8 nodes at random
		#region Pick points at random.
		for (int i = 0; i < 8; i++)
		{
			wantedNodes[i] = (int)Random.Range(0, nodeGridSize * nodeGridSize);
			//Debug.Log(wantedNodes[i]);
			for (int j = 0; j < i; j++)
			{
				if (wantedNodes[i] == wantedNodes[j])
				{
					wantedNodes[i] = (int)Random.Range(0, nodeGridSize * nodeGridSize);
					j = 0;
					//Debug.Log("Repick: " + wantedNodes[i]);
				}
			}
			//Debug.Log("Node " + wantedNodes[i] + " is " + colors[i]);
			nodeList[wantedNodes[i]].GetComponent<Renderer>().material.color = colors[i];

			//Debug.Log(wantedNodes[i] + " is passable");

			PathNode pNode = nodeList[wantedNodes[i]].GetComponent<PathNode>();
			pNode.passable = true;

		}
		#endregion

		/*
		Debug.Log("Crackle");
		for (int i = 0; i < 3; i++)
		{
			int passableIndex = (int)Random.Range(0, nodeGridSize * nodeGridSize);
			for (int j = 0; j < 8; j++)
			{
				if (wantedNodes[j] == passableIndex)
				{
					i--;
				}
				if (j == 7)
				{
					Debug.Log(passableIndex);
					PathNode pNode = nodeList[passableIndex].GetComponent<PathNode>();
					pNode.passable = false;
				}
			}
		}*/

	}

	public void Update()
	{
		if (numberOfFlockers > 0)
		{
			calcCentroid();//find average position of each flocker 
			calcFlockDirection();//find average "forward" for each flocker
			calcDistances();
		}
	}

	public Color GetFlockerColor(int index)
	{
		if (index < colors.Length)
		{
			return colors[index];
		}
		else
		{
			//Debug.Log("Out of bounds: " + index);
		}
		return Color.white;
	}

	void calcDistances( )
	{
		float dist;
		for(int i = 0 ; i < numberOfFlockers; i++)
		{
			for( int j = i+1; j < numberOfFlockers; j++)
			{
				dist = Vector3.Distance(flockers[i].transform.position, flockers[j].transform.position);
				distances[i, j] = dist;
				distances[j, i] = dist;
			}
		}
	}

	public float getDistance(int i, int j)
	{
		return distances[i, j];
	}

	public void ResetFlockers()
	{
		nodeList.Clear();
		flockers.Clear();
		obstacles = new GameObject[1];
		Start();
	}

	private void calcCentroid ()
	{
		// calculate the current centroid of the flock
		Vector3 newCentroid = new Vector3(0, 0, 0);

		for (int i = 0; i < numberOfFlockers; i++)
		{
			newCentroid += Flockers[i].transform.position;
		}
		//newCentroid += new Vector3(0, 0, 0);
		centroid.transform.position = newCentroid / numberOfFlockers;
		
	}

	private void calcFlockDirection()
	{
		Vector3 newFacing = new Vector3(0, 0, 0);
		for(int i = 0; i < numberOfFlockers; i++)
		{
			newFacing += Flockers[i].transform.forward;
		}
		flockDirection = newFacing;

		centroid.transform.forward = flockDirection;

		// calculate the average heading of the flock
		//Loop through all members of the flock
		//Add direction to a V3.
		//Normalize the v3.

		//Face the centroid based on this direction
		// use transform.
	}
	
}