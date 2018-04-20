using System;

namespace ThreadPoolLib
{
    public class MyTask
    {
        /// <summary>
        /// Work, that must be done
        /// </summary>
        private Action work;

        /// <summary>
        /// Check running
        /// </summary>
        private bool isRunning;
        public bool IsRunning { get => isRunning; }

        /// <summary>
        /// C-tor with one parameter
        /// </summary>
        /// <param name="givenWork"></param>
        public MyTask(Action givenWork)
        {
            this.work = givenWork;
        }

        /// <summary>
        /// Work delegate invocation
        /// </summary>
        public void Invoke()
        {
            lock(this)
            {
                this.isRunning = true;
            }
            this.work();
        }
    }
}
