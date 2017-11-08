using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
#if !UNITY_WEBGL
using System.Threading;
#endif


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

    public int numThreads = 8;
    [SerializeField] private int activeThreads = 0;
    [SerializeField] private int waitingCoActions = 0;

    public void QueueActionOnMainThread(Action actionToRun)
    {
#if !UNITY_WEBGL
        lock (mainThreadActionQueue)
        {
            mainThreadActionQueue.Enqueue(actionToRun);
        }
#else
        mainThreadActionQueue.Enqueue(actionToRun);
#endif
    }

    public void QueueActionOnCoThread(Action actionToRun)
    {
#if !UNITY_WEBGL
        lock (coThreadActionQueue)
        {
            coThreadActionQueue.Enqueue(actionToRun);
        }
#else
        QueueActionOnMainThread(actionToRun)
#endif
    }

    private void Awake()
    {
        Debug.Log("Starting Threads");
        InitThreads();
    }

    private void Update()
    {
#if !UNITY_WEBGL
        waitingCoActions = coThreadActionQueue.Count;
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

#else
        while (mainThreadActionQueue.Count > 0)
            {
                mainThreadActionQueue.Dequeue()();
            }
#endif

    }

    private void OnDisable()
    {
        CloseThreads();
    }

    public void InitThreads()
    {
        //Initiate threads for supported platforms
#if !UNITY_WEBGL
        threads = new Thread[numThreads];                    //create thread array
        threadsActionData = new ThreadActionData[numThreads];//create the ActionData array, for all threads
        for (int index = 0; index < numThreads; index++)
        {
            threads[index] = new Thread(ThreadLoop);         //Create a new thread
            threads[index].Start(index);                     //Start the thread, and assign it its index
        }
        TriggerAvailableThreads();
#else                                                        //Using platform that does not support threads
            numThreads = 0;                                  
#endif
    }
    /*
     * NOTE: CloseThreads() closes all co-threads permanently
     */
    public void CloseThreads()
    {
#if !UNITY_WEBGL
        for (int i = 0; i < threads.Length; i++)
        {
            threadsActionData[i].closeThread = true;
        }
        TriggerAvailableThreads();
#endif
    }

    /// <summary>
    /// This function triggers all threads to empty the current queue.
    /// </summary>
    public void TriggerAvailableThreads()
    {
#if !UNITY_WEBGL
        lock (threadLocker)
        {
            Monitor.PulseAll(threadLocker);
        }
#endif

    }
    /// <summary>
    /// This function is the heart of the thread, it keeps it alive until told to shutdown.
    /// </summary>
    /// <param name="_index"></param>
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
                        break;
                    }

                    threadsActionData[index].taskToRun = GetNewCoAction(); //Assign new task
                    threadsActionData[index].isRunningTask = true;
                }

                //Run action
                if (threadsActionData[index].taskToRun != null)
                {
                    threadsActionData[index].taskToRun();
                }

                //Finish
                lock (threadLocker)
                {
                    threadsActionData[index].isRunningTask = false;
                    //Check if thread should close
                    if (threadsActionData[index].closeThread)
                    {
                        activeThreads--;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
    /// <summary>
    /// This function returns an available action from the coThreadActionQueue
    /// </summary>
    /// <returns></returns>
    public Action GetNewCoAction()
    {
        if (coThreadActionQueue.Count != 0)
            return coThreadActionQueue.Dequeue();
        else
            return null;
    }

    /// <summary>
    ///  This is keeps valuable information for each thread.
    /// </summary>
    [Serializable]
    public class ThreadActionData
    {
        public Action taskToRun;
        public bool isRunningTask = false;
        public bool isWaiting = false;
        public bool closeThread = false;

    }
}
