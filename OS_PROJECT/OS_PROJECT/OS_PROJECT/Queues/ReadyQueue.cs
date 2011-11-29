using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class ReadyQueue
    {
        bool locked;
        public bool Locked
        { get { return locked; } set { locked = value; } }

        Object rqLock = new Object();

        Queue<Process> queue = new Queue<Process>();
        public Queue<Process> AccessQueue
        { get { return queue; } set { queue = value; } }

        public ReadyQueue()
        { }
    }
}
