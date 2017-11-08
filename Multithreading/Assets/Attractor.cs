using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Attractor : MonoBehaviour
{
    private const float G = 6.63f;
    private static List<Attractor> attractors = new List<Attractor>();
    static object lockObj = new object();
    public float mass;
    
    public Vector3 transPos = Vector3.zero;
    int currAttr = 0;
    public Rigidbody rb;
    // Use this for initialization
    void Start()
    {
        
        rb = GetComponent<Rigidbody>();
        transPos = transform.position;
        mass = rb.mass;
        lock (attractors)
        {
            attractors.Add(this);
        }
        
        
    }

    // Update is called once per frame
    void Update()
    {
        lock (lockObj)
        {
            transPos = transform.position;

        }

    }

    void FixedUpdate()
    {

        ThreadQueuer.Instance.QueueActionOnCoThread(Attract);
    }

    void Attract()
    {

        lock (attractors)
        { 
            if (attractors.Count > 1)
            {
                for (int i = 0; i < attractors.Count; i++)
                {
                    if (attractors[i].transPos != transPos)
                    {
 
                           
                        float force = G * (mass * attractors[i].mass);
                        Vector3 dir = attractors[i].transPos - transPos;
                        float distance = dir.sqrMagnitude;
                        force /= distance;
                        Action forceA = () =>
                            {
                                rb.AddForce(dir.normalized * force);
                            };
                            ThreadQueuer.Instance.QueueActionOnMainThread(forceA);
                        
                    }
                }
            }
        }
    }
}
