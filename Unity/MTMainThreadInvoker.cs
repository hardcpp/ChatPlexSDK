#if CP_SDK_UNITY
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace CP_SDK.Unity
{
    /// <summary>
    /// Main thread task system
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public class MTMainThreadInvoker : MonoBehaviour
    {
        /// <summary>
        /// Max queue size
        /// </summary>
        const int MAX_QUEUE_SIZE = 1000;
        /// <summary>
        /// Self instance (singleton)
        /// </summary>
        static MTMainThreadInvoker m_Instance;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Queue class
        /// </summary>
        private class Queue
        {
            public Action[] Data = new Action[MAX_QUEUE_SIZE];
            public int ReadPos = 0;
            public int WritePos = 0;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maximum single frame task execution time
        /// </summary>
        private static TimeSpan m_YieldAfterTime = TimeSpan.FromMilliseconds(.5);
        /// <summary>
        /// Stop watch instance
        /// </summary>
        private static Stopwatch m_StopWatch = new Stopwatch();
        /// <summary>
        /// Queues instance
        /// </summary>
        private static Queue[] m_Queues = new Queue[2]
        {
            new Queue(),
            new Queue()
        };
        /// <summary>
        /// Have queued actions
        /// </summary>
        private static volatile bool m_Queued = false;
        /// <summary>
        /// Current front queue
        /// </summary>
        private static int m_FrontQueue = 0;
        private static int m_ProcessingQueue = -1;
        /// <summary>
        /// Main thread reference
        /// </summary>
        private static Thread _mainThread;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unity GameObject initialize
        /// </summary>
        internal static void Initialize()
        {
            if (m_Instance == null)
            {
                m_Instance = new GameObject("[CP_SDK.Unity.MTMainThreadInvoker]").AddComponent<MTMainThreadInvoker>();
                DontDestroyOnLoad(m_Instance.gameObject);
            }
        }
        /// <summary>
        /// Stop
        /// </summary>
        internal static void Destroy()
        {
            if (!m_Instance)
                return;

            /// Clear queues
            m_Queues = new Queue[2]
            {
                new Queue(),
                new Queue()
            };
            m_FrontQueue = 0;
            m_ProcessingQueue = -1;
            m_Queued = false;

            GameObject.Destroy(m_Instance.gameObject);
            m_Instance = null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Are we currently on the main thread?
        /// </summary>
        /// <returns></returns>
        public static bool IsMainThread()
        {
            return _mainThread.Equals(System.Threading.Thread.CurrentThread);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enqueue a new action
        /// </summary>
        /// <param name="p_Action">Action to enqueue</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Enqueue(Action p_Action)
        {
            if (p_Action == null)
                return;

            lock (m_Queues)
            {
                var l_Queue = m_Queues[m_FrontQueue];
                if (l_Queue.WritePos >= MAX_QUEUE_SIZE)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK.Unity][MTMainThreadInvoker.Enqueue] Too many actions pushed!");
                    return;
                }

                l_Queue.Data[l_Queue.WritePos++] = p_Action;
                m_Queued = true;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unity GameObject update
        /// </summary>
        private void Update()
        {
            if (m_ProcessingQueue == -1)
            {
                lock (m_Queues)
                {
                    if (!m_Queued)
                        return;

                    m_ProcessingQueue = m_FrontQueue;
                    m_FrontQueue = (m_FrontQueue + 1) & 1;
                    m_Queued = false;
                }
            }

            // Finish the older batch before swapping in work queued during its execution.
            var l_Queue = m_Queues[m_ProcessingQueue];

            m_StopWatch.Restart();

            do
            {
                var l_Action = l_Queue.Data[l_Queue.ReadPos];
                l_Queue.Data[l_Queue.ReadPos++] = null;
                try
                {
                    l_Action();
                }
                catch (Exception l_Exception)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK.Unity][MTMainThreadInvoker.Update] Error:");
                    ChatPlexSDK.Logger.Error(l_Exception);
                }

            } while (l_Queue.ReadPos < l_Queue.WritePos && m_StopWatch.Elapsed < m_YieldAfterTime);

            if (l_Queue.ReadPos == l_Queue.WritePos)
            {
                l_Queue.ReadPos = 0;
                l_Queue.WritePos = 0;
                m_ProcessingQueue = -1;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On component awake
        /// </summary>
        private void Awake()
        {
            _mainThread = System.Threading.Thread.CurrentThread;
        }
    }
}
#endif
