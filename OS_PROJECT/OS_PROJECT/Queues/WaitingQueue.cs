﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class WaitingQueue
    {
        Queue<Process> queue = new Queue<Process>();
        public Queue<Process> AccessQueue
        { get { return queue; } set { queue = value; } }

        public WaitingQueue()
        { }
    }
}
