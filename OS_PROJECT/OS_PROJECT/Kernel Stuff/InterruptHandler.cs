using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OS_PROJECT
{
    public enum Interrupt
    {
        PageFault, IOFault, None
    }

    static class InterruptHandler
    {
        public static Thread thread = new Thread(new ThreadStart(Run));

        static Object Lock = new Object();

        static Queue<Process> BlockedQueue = new Queue<Process>();
        static Queue<uint> NeededPages = new Queue<uint>();

        static Queue<Process> ServicedQueue = new Queue<Process>();

        public static void Start()
        {
            thread.Start();
        }

        public static void Run()
        {
            uint frame = 257;
            while (true)
            {
                lock (Lock)
                {
                    if (BlockedQueue.Count != 0 && MMU.FreeFrames != 0)
                    {
                        frame = MMU.GetFreeFrame(NeededPages.ElementAt<uint>(0), BlockedQueue.ElementAt<Process>(0).PCB.ProcessID);
                        BlockedQueue.ElementAt<Process>(0).PCB.PageTable.table[NeededPages.ElementAt<uint>(0)].Frame = frame;
                        BlockedQueue.ElementAt<Process>(0).PCB.PageTable.table[NeededPages.ElementAt<uint>(0)].InMemory = true;
                        BlockedQueue.ElementAt<Process>(0).PCB.PageTable.table[NeededPages.ElementAt<uint>(0)].IsOwned = true;
                        ServicedQueue.Enqueue(BlockedQueue.Dequeue());
                        NeededPages.Dequeue();
                    }
                }
            }
        }

        public static uint ServicedProcessesCount()
        {
            return (uint)ServicedQueue.Count;
        }

        public static void EnqueueProcess(Process p, uint page)
        {
            lock (Lock)
            {
                NeededPages.Enqueue(page);
                BlockedQueue.Enqueue(p);
            }
        }

        public static Process DequeueProcess()
        {
            lock (Lock)
            {
                return ServicedQueue.Dequeue();
            }
        }
    }
}
