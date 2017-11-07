using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CubeScript : MonoBehaviour {
    System.Random rand = new System.Random();
	// Use this for initialization
	void Start () {
	    ThreadQueuer.Instance.QueueActionOnCoThread(() => Foo());
    }
	
	// Update is called once per frame
	void Update () {
	    
    }

    void Foo() //Multithread Test
    {

        Vector3 newPos = new Vector3((float)Math.Sin((float)DateTime.Now.Second + (float)DateTime.Now.Millisecond / 1000), 0,0) + Vector3.zero;

        Action a = () => //will run on main thread
        {
            transform.position = newPos;
        };
        ThreadQueuer.Instance.QueueActionOnMainThread(a);

        Thread.Sleep(2);
        ThreadQueuer.Instance.QueueActionOnCoThread(() => Foo());
    }
}
