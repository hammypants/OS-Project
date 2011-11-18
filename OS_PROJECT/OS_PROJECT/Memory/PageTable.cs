using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class PageTable
    {
        PageTableLocation[] table = new PageTableLocation[512];

        public PageTable()
        {
            for (uint iterator = 0; iterator < 512; iterator++)
            {
                table[iterator].Frame = 257;
                table[iterator].InMemory = false;
                table[iterator].OwnerID = 31;
            }
        }

        struct PageTableLocation
        {
            public uint Frame;
            public bool InMemory;
            public uint OwnerID;
        }
    }
}
