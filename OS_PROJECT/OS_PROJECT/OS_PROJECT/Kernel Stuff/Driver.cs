﻿using System;
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

        public List<Process> deadProcesses = new List<Process>();

        Disk disk = new Disk();
        public Disk Disk
        { get { return disk; } }

        RAM memory = new RAM();
        public RAM RAM
        { get { return memory; } }

        Dispatcher dispatcher;
        public Dispatcher Dispatcher
        { get { return dispatcher; } }

        // Main program initalization.
        Loader loader;
        LongTermScheduler LTS;

        int numberOfCPUs = 4;
        List<CPU> cpuList;

        bool shouldRun = true;

        public Driver()
        { }

        public void RunOS()
        {
            InterruptHandler.Start();
            MMU.Instantiate(this);
            loader = new Loader(this);
            LTS = new LongTermScheduler(this);
            dispatcher = new Dispatcher(this);
            cpuList = new List<CPU>();

            for (int i = 0; i < numberOfCPUs; i++)
            {
                cpuList.Add(new CPU(this, i));
            }

            // Main program.
            loader.Run();
            LTS.Run();

            int cpu = 0;

            while (shouldRun)
            {
                if (ReadyQueue.AccessQueue.Count == 0)
                {
                    shouldRun = false;
                }
                if (cpuList[cpu].isActive == true)  
                {
                    // Do nothing.
                }
                else
                {
                    // Tell the CPU to go again.
                    cpuList[cpu].ResumeCPU();
                }
                cpu++;
                cpu %= numberOfCPUs; 
            }

            bool wait = true;
            while (wait)
            {
                int finished = 0;
                foreach (CPU c in cpuList)
                {
                    if (!c.isActive)
                    {
                        finished++;
                    }
                }
                if (finished == numberOfCPUs)
                {
                    wait = false;
                }
            }

        }

        void RunCPUs()
        {
            for (int i = 0; i < numberOfCPUs; i++)
            {
                cpuList[i].RunCPU();
            }
        }

        void ResumeCPUs()
        {
            for (int i = 0; i < numberOfCPUs; i++)
            {
                cpuList[i].ResumeCPU();
            }
        }
    }
}
