using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OS_PROJECT
{
    class OS
    {
        static void Main(string[] args)
        {
            Driver kernal = new Driver();
            kernal.RunOS();
        }
    }
}
