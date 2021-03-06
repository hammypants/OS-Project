﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class Dispatcher
    {
        Driver kernal;
        ReadyQueue RQ;

        public Dispatcher(Driver k)
        {
            kernal = k;
            RQ = k.ReadyQueue;
        }

        public void DispatchProcess(CPU cpu)
        {
            if (RQ.AccessQueue.Count != 0)
            {
                cpu.CurrentProcess = RQ.AccessQueue.Dequeue();
                cpu.CPU_PCB = cpu.CurrentProcess.PCB;
            }
        }

        public void SwapOut(CPU cpu)
        {

        }
    }
}
