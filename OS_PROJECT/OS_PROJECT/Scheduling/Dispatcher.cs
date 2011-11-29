using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class Dispatcher
    {
        Driver kernal;
        ReadyQueue RQ;

        Object Lock = new Object();

        public Dispatcher(Driver k)
        {
            kernal = k;
            RQ = k.ReadyQueue;
        }

        public void DispatchProcess(CPU cpu)
        {
            lock (Lock)
            {
                if (RQ.AccessQueue.Count != 0)
                {
                    cpu.CurrentProcess = RQ.AccessQueue.Dequeue();
                    if (cpu.CurrentProcess != null)
                        cpu.CPU_PCB = cpu.CurrentProcess.PCB;
                }
            }
        }
    }
}
