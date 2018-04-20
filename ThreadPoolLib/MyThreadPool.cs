using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ThreadPoolLib
{

    public class MyThreadPool : IDisposable
    {
        /// <summary>
        /// Maximum number of threads
        /// </summary>
        private const int max = 4;

        /// <summary>
        /// Thread pool
        /// </summary>
        private Thread[] threads;
        private Dictionary<int, ManualResetEvent> threadsEvent;

        /// <summary>
        /// Task queue
        /// </summary>
        private List<MyTask> taskList;

        /// <summary>
        /// Control thread
        /// </summary>
        private Thread controlThread;
        private ManualResetEvent contorlEvent;

        /// <summary>
        /// Check pool is stoped or not
        /// </summary>
        private bool stoped;
        private ManualResetEvent stopEvent;

        /// <summary>
        /// Check pool is disposed or not
        /// </summary>
        private bool disposed;



        /// <summary>
        /// Constuctor for initializing pool
        /// </summary>
        public MyThreadPool()
        {
            this.stopEvent = new ManualResetEvent(false);

            this.contorlEvent = new ManualResetEvent(false);
            this.controlThread = new Thread(SearchFreeThread) { Name = " Schedule Thread", IsBackground = true };
            this.controlThread.Start();

            
            this.threads = new Thread[max];
            this.threadsEvent = new Dictionary<int, ManualResetEvent>(max);

            for (int i = 0; i < max; i++)
            {
                this.threads[i] = new Thread(StartWork) { Name = "ThreadID " + (i + 1), IsBackground = true };
                threadsEvent.Add(threads[i].ManagedThreadId, new ManualResetEvent(false));
                this.threads[i].Start();
            }

            this.taskList = new List<MyTask>();
        }

        /// <summary>
        /// Set tasks to queue
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool Execute(MyTask task)
        {
            if (task == null)
            {
                throw new Exception("Task is null!");
            }
            lock(new object())
            {
                if (this.stoped)
                    return false;

                this.AddWork(task);
                return true;
            }
        }

        /// <summary>
        /// Stops thread pool work with pending all tasks
        /// </summary>
        public void Stop()
        {
            lock(new object())
            {
                this.stoped = true;
            }
            while(this.taskList.Count>0)
            {
                this.stopEvent.WaitOne();
                this.stopEvent.Reset();
            }
            this.Dispose(true);
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }



        /// <summary>
        /// Free up resources, which is used by thread pool
        /// </summary>
        /// <param name="dispose"></param>
        private void Dispose(bool dispose)
        {
            if(!this.disposed)
            {
                if(dispose)
                {
                    this.controlThread.Abort();
                    this.contorlEvent.Dispose();

                    for (int i = 0; i < max; i++)
                    {
                        this.threads[i].Abort();
                        this.threadsEvent[this.threads[i].ManagedThreadId].Dispose();
                    }
                }
                this.disposed = true;
            }
        }

        /// <summary>
        /// Add task to task queue
        /// </summary>
        /// <param name="work"></param>
        private void AddWork(MyTask task)
        {
            if (task == null)
                throw new Exception("Work is null!");

            lock(this.taskList)
            {
                this.taskList.Add(task);
            }
            this.contorlEvent.Set();
        }

        /// <summary>
        /// Remove task from task queue
        /// </summary>
        /// <param name="task"></param>
        private void RemoveWork(MyTask task)
        {
            lock (this.taskList)
            {
                this.taskList.Remove(task);
            }
            if (this.taskList.Count > 0 && this.taskList.Where(t => !t.IsRunning).Count() > 0)
            {
                this.contorlEvent.Set();
            }
        }

        /// <summary>
        /// Start task invocation
        /// </summary>
        private void StartWork()
        {
            while(true)
            {
                this.threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();

                MyTask task = SelectWork();
                if (task != null) 
                {
                    try
                    {
                        task.Invoke();
                    }
                    finally
                    {
                        this.RemoveWork(task);
                        if (this.stoped)
                            this.stopEvent.Set();
                        this.threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                    }
                }
            }
        }

        /// <summary>
        /// Return first task in queue
        /// </summary>
        /// <returns></returns>
        private MyTask SelectWork()
        {
            lock(this.taskList)
            {
                if (this.taskList.Count == 0)
                {
                    throw new Exception("Queue is empty!");
                }
                return this.taskList.Where(t => !t.IsRunning).First();
            }

        }

        /// <summary>
        /// Search free thread,which can do next task
        /// </summary>
        private void SearchFreeThread()
        {
            while (true)
            {
                this.contorlEvent.WaitOne();
                lock (this.threads)
                {
                    foreach (var thread in this.threads)
                    {
                        if (this.threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                        {
                            this.threadsEvent[thread.ManagedThreadId].Set();
                            break;
                        }
                    }
                }
                this.contorlEvent.Reset();
            }
        }
    }
}
