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

        Dispatcher dispatcher;
        public Dispatcher Dispatcher
        { get { return dispatcher; } }

        // Main program initalization.
        Loader loader;
        LongTermScheduler LTS;

        uint numberOfCPUs = 4;
        List<CPU> cpuList;

        uint pausedCPUs = 0;

        bool shouldRun = true;

        public Driver()
        {
            
        }

        void CPUPausedEvent(object o, EventArgs e)
        {
            pausedCPUs++;
        }

        public void RunOS()
        {
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
                    RunCPUs();
                    counter++;
                }
                else
                {
                    ResumeCPUs();
                }
            }

            //string selectedOption;
            //while (shouldRun)
            //{
            //    Console.WriteLine(
            //        "Please type a command:\n"
            //    +"[lb] will load the next batch of jobs.\n"
            //    +"[rb] will run the next batch of jobs.\n"
            //    +"[q] will quit the program.\n"
            //        );

            //    selectedOption = Console.ReadLine();

            //    switch (selectedOption)
            //    {
            //        case "lb":
            //            if (NewProcessQueue.AccessQueue.Count != 0)
            //            {
            //                LTS.Run();
            //            }
            //            else
            //            {
            //                Console.WriteLine("Cannot load next batch-- no more jobs to run!\n");
            //            }
            //            break;
            //        case "rb":
            //            if (ReadyQueue.AccessQueue.Count != 0)
            //            {
            //                if (counter == 0)
            //                    RunCPUs();
            //                else
            //                    ResumeCPUs();
            //            }
            //            else
            //            {
            //                Console.WriteLine("Cannot run the current batch of jobs-- there is no batch of jobs to run!\n");
            //            }
            //            break;
            //        case "q":
            //            shouldRun = false;
            //            break;
            //    }
            //}

            // End Run().
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
