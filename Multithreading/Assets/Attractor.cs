using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attractor : MonoBehaviour
{
    private const float G = 6.63f;
    private static List<Attractor> attractors = new List<Attractor>();
    static object lockObj = new object();
    public float mass;
    public Rigidbody rb;
    // Use this for initialization
    void Start()
    {
        
        rb = GetComponent<Rigidbody>();
        
        mass = rb.mass;
        lock (lockObj)
        {
            attractors.Add(this);
        }
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        ThreadQueuer.Instance.QueueActionOnMainThread(Attract);
    }

    void Attract()
    {
        if (attractors.Count > 1)
        {
            foreach (var attractor in attractors)
            {
                if (attractor != this && attractor.transform.position!= transform.position)
                {
                    Vector3 dir = attractor.transform.position - transform.position;
                    float distance = dir.sqrMagnitude;
                    float force = G * (mass * attractor.mass) / distance;
                    rb.AddForce(dir.normalized*force);
                }
            }
        }
    }
}
