using System;
using System.Threading;
using UnityEngine;


namespace UniGLTF
{
    class ThraedExecutor<T> : CustomYieldInstruction
    {
        Thread m_thread;
        public ThraedExecutor(Func<T> task)
        {
            m_thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Result = task();
                }
                catch (Exception ex)
                {
                    Error = ex;
                }
                finally
                {
                    m_keepWaiting = false;
                }
            }));
            m_thread.Start();
        }
        bool m_keepWaiting = true;
        public override bool keepWaiting
        {
            get { return m_keepWaiting; }
        }
        public T Result
        {
            get;
            private set;
        }
        public Exception Error
        {
            get;
            private set;
        }
    }

    static class CoroutineUtil
    {
        public static ThraedExecutor<T> RunOnThread<T>(Func<T> task)
        {
            return new ThraedExecutor<T>(task);
        }
    }
}
