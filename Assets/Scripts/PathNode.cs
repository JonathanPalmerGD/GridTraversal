using UnityEngine;
using System.Collections;

public class PathNode : MonoBehaviour 
{
	public int[] adjacent = new int[8];
	public bool passable;
	public int nodeIndex = -1;
	public bool drawDebug;
	public GameObject blockprefab;
	public GameObject block;
	public bool generateWalls = false;

	public FlockManager flockManager;

	// Use this for initialization
	void Start () 
	{
		if (generateWalls)
		{
			if (Random.Range(0, 100) < 10)
			{
				passable = false;
				for (int i = 0; i < flockManager.wantedNodes.Length; i++)
				{
					if (nodeIndex == flockManager.wantedNodes[i])
					{
						passable = true;
					}
				}
			}
			else
			{
				passable = true;
			}
			block = ((GameObject)Instantiate(blockprefab, new Vector3(transform.position.x, transform.position.y + 1, transform.position.z), Quaternion.identity));
			block.SetActive(false);
		}
		else
		{

		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (passable && block!= null && block.activeSelf)
		{
			block.SetActive(false);
		}
		else if (!passable && block != null && !block.activeSelf)
		{
			block.SetActive(true);
		}

		//for (int i = 0; i < adjacent.Length; i++)
		//{
		//    if (adjacent[i] != -1)
		//    {
		//        Debug.DrawLine(transform.position + Vector3.up * 2, flockManager.NodeList[adjacent[i]].transform.position - Vector3.up * 3, Color.white);
		//    }
		//}
	}

	public void setNodeManager(GameObject fManager)
	{
		flockManager = fManager.GetComponent<FlockManager>();
	}
}
