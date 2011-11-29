using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class LongTermScheduler
    {
        Driver kernel;
        Disk disk;
        RAM RAM;
        NewProcessQueue NPQ;
        WaitingQueue WQ;
        ReadyQueue RQ;

        List<Process> batchList = new List<Process>();

        public int batch = 0;

        public LongTermScheduler(Driver k)
        {
            kernel = k;
            disk = k.Disk;
            RAM = k.RAM;
            NPQ = k.NewProcessQueue;
            WQ = k.WaitingQueue;
            RQ = k.ReadyQueue;
        }

        public void Run()
        {
            GetBatch();
            Console.WriteLine("BATCH COUNT: " + batchList.Count);
            SortBatch("FCFS");
            InsertBatchInMemory();
            ClearBatch();
        }

        public bool NoMoreProcesses()
        {
            if (NPQ.AccessQueue.Count == 0)
            { return true; }
            return false;
        }

        void GetBatch()
        {
            int _bl = NPQ.AccessQueue.Count;
            for (int i = 0; i < _bl; i++)
            {
                Process p = NPQ.AccessQueue.Dequeue();
                batchList.Add(p);
            }
            batch++;
        }

        void SortBatch(string algorithm)
        {
            switch (algorithm)
            {
                case ("Priority"):
                    batchList.Sort(ComparePriority);
                    foreach (Process p in batchList)
                    {
                        RQ.AccessQueue.Enqueue(p);
                        p.PCB._waitingTime.Start();
                    }
                    break;
                case ("SJF"):
                    batchList.Sort(CompareJob);
                    foreach (Process p in batchList)
                    {
                        RQ.AccessQueue.Enqueue(p);
                        p.PCB._waitingTime.Start();
                    }
                    break;
                case ("FCFS"):
                    foreach (Process p in batchList)
                    {
                        RQ.AccessQueue.Enqueue(p);
                        p.PCB._waitingTime.Start();
                    }
                    break;
                default:
                    break;
            }
        }

        private int ComparePriority(Process p1, Process p2)
        {
            if (p1.PCB.Priority > p2.PCB.Priority)
                return -1;
            else if (p1.PCB.Priority == p2.PCB.Priority)
                return 0;
            else return 1;
        }

        private int CompareJob(Process p1, Process p2)
        {
            if (p1.PCB.InstructionLength > p2.PCB.InstructionLength)
                return 1;
            else if (p1.PCB.InstructionLength == p2.PCB.InstructionLength)
                return 0;
            else return -1;
        }

        void InsertBatchInMemory()
        {
            foreach (Process p in batchList)
            {
                uint firstPage = (uint)Array.FindIndex<PageTable.PageTableLocation>(p.PCB.PageTable.table, e => e.IsOwned == true);
                uint frame;
                for (uint iterator = firstPage; iterator < firstPage + 4; iterator++)
                {
                    frame = MMU.GetFreeFrame(iterator, p.PCB.ProcessID);
                    if (iterator == firstPage)
                    {
                        p.PCB.MemoryAddress = frame * 4;
                    }
                    p.PCB.PageTable.table[iterator].InMemory = true;
                    p.PCB.PageTable.table[iterator].Frame = frame;
                }
            }
        }

        void ClearBatch()
        {
            batchList.Clear();
        }
        //
    }
}