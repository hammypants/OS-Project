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
            //SortBatch();
            InsertBatchInMemory();
            AddNewProcessesToWaitingQueue();
            AddWaitingProcessesToReadyQueue();
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

        void SortBatch()
        {
            foreach (Process p in batchList)
            {
                RQ.AccessQueue.Enqueue(p);
            }
            RQ.AccessQueue.OrderBy<Process, uint>(ExtractPID);

            //// First come first serve. Automatic.
            //var sorted = from process in batchList
            //        orderby process.PCB.Priority
            //        select process;
            //foreach (Process p in sorted)
            //{
            //    RQ.AccessQueue.Enqueue(p);
            //}
        }

        uint ExtractPID(Process p)
        {
            return p.PCB.Priority;
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
