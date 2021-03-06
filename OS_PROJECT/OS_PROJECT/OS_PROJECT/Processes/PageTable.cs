﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class PageTable
    {
        public PageTableLocation[] table = new PageTableLocation[512];

        public PageTable()
        {
            for (uint iterator = 0; iterator < 512; iterator++)
            {
                table[iterator].Frame = 257;
                table[iterator].InMemory = false;
                table[iterator].IsOwned = false;
            }
        }

        public struct PageTableLocation
        {
            public uint Frame;
            public bool InMemory;
            public bool IsOwned;
        }

        public uint ReturnFirstPage(PCB p)
        {
            uint firstPage = (uint)Array.FindIndex<PageTable.PageTableLocation>(p.PageTable.table, e => e.IsOwned == true);
            return firstPage;
        }

        public PageTableLocation Lookup(uint page)
        {
            return table[page];
        }
    }
}
