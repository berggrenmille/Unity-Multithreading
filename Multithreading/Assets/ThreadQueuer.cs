using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;
using UnityEngine;

public class ThreadQueuer : Singleton<ThreadQueuer>
{
    private readonly Queue<Action> mainThreadActionQueue = new Queue<Action>();
    private readonly Queue<Action> coThreadActionQueue = new Queue<Action>();
#if !UNITY_WEBGL
    //Array of all threads
    private Thread[] threads;
    [SerializeField]
    private ThreadActionData[] threadsActionData;
#endif
    private readonly object threadLocker = new object();

    public int maxThreads = 8;
    [SerializeField] private int activeThreads = 0;
    public int actionsRunning = 0;
    public int waitingCoActions = 0;

    public void QueueActionOnMainThread(Action actionToRun)
    {
        lock (mainThreadActionQueue)
        {
            mainThreadActionQueue.Enqueue(actionToRun);
        }
    }

    public void QueueActionOnCoThread(Action actionToRun)
    {
#if !UNITY_WEBGL
        lock (coThreadActionQueue)
        {
            coThreadActionQueue.Enqueue(actionToRun);
        }
#else
        lock (mainThreadActionQueue)
        {
            QueueActionOnMainThread(actionToRun)
        }
#endif
    }

    private void Awake()
    {
        Debug.Log("Starting Threads");
        InitThreads();
    }

    private void Update()
    {
        if (coThreadActionQueue.Count != 0)
        {
            TriggerAvailableThreads();
        }
        lock (mainThreadActionQueue)
        {
            while (mainThreadActionQueue.Count > 0)
            {
                mainThreadActionQueue.Dequeue()();
            }
        }
        waitingCoActions = coThreadActionQueue.Count;
    }

    private void OnDisable()
    {
        CloseThreads();
    }

    public void InitThreads()
    {
                                                             //Initiate threads for supported platforms
#if !UNITY_WEBGL
        threads = new Thread[maxThreads];                    //create thread array
        threadsActionData = new ThreadActionData[maxThreads];//create the ActionData array, for all threads
        for (int index = 0; index < maxThreads; index++)
        {
            threads[index] = new Thread(ThreadLoop);         //Create a new thread
            threads[index].Start(index);                     //Start the thread, and assign it its index
        }
        TriggerAvailableThreads();                    
#else                                                        //Using platform that does not support threads
            ThreadCount=0;                                  
#endif
    }
    /*
     * NOTE: CloseThreads() closes all co-threads permanently
     */
    public void CloseThreads()                               
    {
        for (int i = 0; i < threads.Length; i++)
        {
            threadsActionData[i].closeThread = true;
        }
    }

    /*
     * Note: 
     * TriggerAvailableThreads triggers all threads to empty the current queue
     */
    public void TriggerAvailableThreads()
    {
        if (maxThreads > 0)
        {
            lock (threadLocker)
            {
                Monitor.PulseAll(threadLocker);
            }
        }
        else
        {
            lock (coThreadActionQueue)
            {
                while (coThreadActionQueue.Count > 0)
                {
                    coThreadActionQueue.Dequeue()();
                }
            }
        }
    }

    private void ThreadLoop(object _index)
    {
        int index = (int)_index;
        activeThreads++;
        threadsActionData[index] = new ThreadActionData();
        while (true)
        {
            try
            {
                lock (threadLocker)
                {
                    while (coThreadActionQueue.Count == 0)
                    {
                        Monitor.Wait(threadLocker); //Wait for master to tell threads to start working
                        if (threadsActionData[index].closeThread)
                            return;
                    }

                    threadsActionData[index].taskToRun = GetNewCoAction(); //Assign new task

                    threadsActionData[index].isRunningTask = true;
                    actionsRunning++;
                }

                //Run action
                if (threadsActionData[index].taskToRun != null)
                {
                    threadsActionData[index].taskToRun();
                }

                //Finish
                lock (threadLocker)
                {
                    actionsRunning--;
                    threadsActionData[index].isRunningTask = false;
                }
                if (threadsActionData[index].closeThread)
                {
                    activeThreads--;
                    return;
                }
            }
            catch (Exception e)
            {
                return;
            }
        }
    }

    public Action GetNewCoAction()
    {
        if (coThreadActionQueue.Count != 0)
            return coThreadActionQueue.Dequeue();
        else
            return null;
    }
    
    [Serializable]
    public class ThreadActionData
    {
        public Action taskToRun;
        public bool isRunningTask = false;
        public bool closeThread = false;

    }
}
