using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq;

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
            SortBatch("SJF");
            InsertBatchInMemory();
            //AddNewProcessesToWaitingQueue();
            //AddWaitingProcessesToReadyQueue();
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
            for (int i = 0; i < 15; i++)
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
                case("SJF"):
                    batchList.Sort(CompareJob);
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
            uint addressCounter = 0;
            foreach (Process p in batchList)
            {
                p.PCB.MemoryAddress = addressCounter;
                for (uint i = p.PCB.DiskAddress; i < p.PCB.DiskAddress+p.PCB.JobLength; i++)
                {
                    RAM.WriteDataToMemory(addressCounter++, disk.ReadDataFromDisk(i));
                }
            }
        }

        void AddNewProcessesToWaitingQueue()
        {
            foreach (Process p in batchList)
            {
                WQ.AccessQueue.Enqueue(p);
            }
        }

        void AddWaitingProcessesToReadyQueue()
        {
            if (WQ.AccessQueue.Count != 0)
            {
                if (RQ.AccessQueue.Count < RQ.Limit)
                {
                    int freeSlots = (int)RQ.Limit - RQ.AccessQueue.Count;
                    for (int i = 0; (i < freeSlots) && WQ.AccessQueue.Count != 0; i++)
                    {
                        RQ.AccessQueue.Enqueue(WQ.AccessQueue.Dequeue());
                        RQ.AccessQueue.ElementAt<Process>(i).PCB._waitingTime.Start();
                    }
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
