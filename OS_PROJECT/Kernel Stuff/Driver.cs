using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace OS_PROJECT
{
    class Driver
    {
        // Setting up virtual hardware and shared memory.
        NewProcessQueue newProcessQueue = new NewProcessQueue();
        public NewProcessQueue NewProcessQueue
        { get { return newProcessQueue; } }

        WaitingQueue waitingQueue = new WaitingQueue();
        public WaitingQueue WaitingQueue
        { get { return waitingQueue; } }

        ReadyQueue readyQueue = new ReadyQueue();
        public ReadyQueue ReadyQueue
        { get { return readyQueue; } }

        Disk disk = new Disk();
        public Disk Disk
        { get { return disk; } }

        RAM memory = new RAM();
        public RAM RAM
        { get { return memory; } }

        // Main program initalization.
        Loader loader;
        LongTermScheduler LTS;

        uint numberOfCPUs = 4;
        List<CPU> cpuList;

        bool shouldRun = true;

        public Driver()
        {
        }

        public void RunOS()
        {
            loader = new Loader(this);

            LTS = new LongTermScheduler(this);
            cpuList = new List<CPU>();

            for (int i = 0; i < numberOfCPUs; i++)
            {
                cpuList.Add(new CPU(this, i));
            }

            // Main program.
            loader.Run();

            int counter = 0;
            while (shouldRun)
            {
                if (ReadyQueue.AccessQueue.Count == 0)
                {
                    if (LTS.NoMoreProcesses())
                    {
                        shouldRun = false;
                    }
                    else
                    {
                        LTS.Run();
                    }
                }
                if (shouldRun == false)
                {
                    break;
                }
                if (counter == 0)
                {
                    for (int i = 0; i < numberOfCPUs; i++)
                    {
                        cpuList[i].RunCPU();
                        counter++;
                    }
                }
                else
                {
                    for (int i = 0; i < numberOfCPUs; i++)
                    {
                        cpuList[i].ResumeCPU();
                    }
                }
            }

            //LTS.Run();
            //Console.WriteLine();
            //Console.WriteLine("NPQ COUNT: " + NewProcessQueue.AccessQueue.Count);
            //Console.WriteLine("WQ COUNT: " + WaitingQueue.AccessQueue.Count);
            //Console.WriteLine("RQ COUNT: " + ReadyQueue.AccessQueue.Count);
            
            
            Console.WriteLine();
        }

        void LoopLogic()
        {
            for (int i = 0; i < numberOfCPUs; i++)
            {
                cpuList[i].RunCPU();
            }
        }
    }
}
