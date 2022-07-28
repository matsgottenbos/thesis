/*
 * Helper methods for multithreading
*/

using System;
using System.Threading;

namespace DriverPlannerShared {
    public static class ThreadHandler {
        public static (CancellationTokenSource, ManualResetEvent) ExecuteInThreadWithCancellation(XorShiftRandom threadRand, Action<CancellationToken, XorShiftRandom> body) {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            ManualResetEvent handle = new ManualResetEvent(false);
            ThreadInfo threadInfo = new ThreadInfoWithCancellation(token, threadRand, body, handle);
            ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteInThreadWithCancellationInner), threadInfo);
            return (cts, handle);
        }

        static void ExecuteInThreadWithCancellationInner(object a) {
            ThreadInfoWithCancellation threadInfo = (ThreadInfoWithCancellation)a;
            CancellationToken cancellationToken = threadInfo.Token;
            XorShiftRandom threadRand = threadInfo.ThreadRand;
            Action<CancellationToken, XorShiftRandom> body = threadInfo.BodyWithCancellation;
            ManualResetEvent handle = threadInfo.Handle;

            try {
                body(cancellationToken, threadRand);
            } finally {
                handle.Set();
            }
        }


        public static ManualResetEvent ExecuteInThread(XorShiftRandom threadRand, Action<XorShiftRandom> body) {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            ManualResetEvent handle = new ManualResetEvent(false);
            ThreadInfo threadInfo = new ThreadInfo(threadRand, body, handle);
            ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteInThreadInner), threadInfo);
            return handle;
        }

        static void ExecuteInThreadInner(object a) {
            ThreadInfo threadInfo = (ThreadInfo)a;
            XorShiftRandom threadRand = threadInfo.ThreadRand;
            Action<XorShiftRandom> body = threadInfo.Body;
            ManualResetEvent handle = threadInfo.Handle;

            try {
                body(threadRand);
            } finally {
                handle.Set();
            }
        }
    }

    public class ThreadInfo {
        public readonly XorShiftRandom ThreadRand;
        public readonly Action<XorShiftRandom> Body;
        public readonly ManualResetEvent Handle;

        public ThreadInfo(XorShiftRandom threadRand, Action<XorShiftRandom> body, ManualResetEvent handle) {
            ThreadRand = threadRand;
            Body = body;
            Handle = handle;
        }
    }

    public class ThreadInfoWithCancellation : ThreadInfo {
        public readonly CancellationToken Token;
        public readonly Action<CancellationToken, XorShiftRandom> BodyWithCancellation;

        public ThreadInfoWithCancellation(CancellationToken token, XorShiftRandom threadRand, Action<CancellationToken, XorShiftRandom> bodyWithCancellation, ManualResetEvent handle) : base(threadRand, null, handle) {
            Token = token;
            BodyWithCancellation = bodyWithCancellation;
        }
    }
}
