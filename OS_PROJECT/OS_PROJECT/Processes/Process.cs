using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class Process
    {
        PCB processControlBlock;
        public PCB PCB
        { get { return processControlBlock; } set { processControlBlock = value; } }

        public Process(PCB pcb)
        {
            PCB = pcb;
            PCB.ProcessID++;
        }
    }
}
