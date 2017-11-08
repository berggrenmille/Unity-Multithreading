using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject clone;
    public int num = 10;

    public int x = 10;
    public int y = 10;
    public int z = 10;
	// Use this for initialization
	void Start () {
	    for (int i = 0; i < num; i++)
	    {
	        Instantiate(clone, new Vector3(Random.Range(-x, x), Random.Range(-y, y), Random.Range(-z, z)),Quaternion.identity,transform);
	    }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
