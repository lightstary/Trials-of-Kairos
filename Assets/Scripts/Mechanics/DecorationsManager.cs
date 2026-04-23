using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorationsManager : MonoBehaviour
{
    [Header("Decorations List")]
    public List<GameObject> allDecor = new List<GameObject>();
	
	public float floatHeight = 0.5f;
	public float floatSpeed = 0.3f;
	
	private List<Vector3> startPositions = new List<Vector3>();
	private List<float> randomOffsets = new List<float>();
	
	void Start()
	{
		foreach (GameObject decor in allDecor)
		{
			startPositions.Add(decor.transform.position);
			randomOffsets.Add(Random.Range(0f, Mathf.PI * 2f));
		}
	}
	
	void Update()
	{
		for (int i = 0; i < allDecor.Count; i++)
		{
			if(allDecor[i] == null) continue;
			
			Vector3 tempPos = startPositions[i];
			tempPos.y += Mathf.Sin(Time.time * floatSpeed + randomOffsets[i]) * floatHeight;
			allDecor[i].transform.position = tempPos;
		}
	}
}
