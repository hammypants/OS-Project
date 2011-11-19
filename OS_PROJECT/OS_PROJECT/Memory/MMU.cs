﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class MMU
    {
        private static MMU singleton;
        Driver kernel;

        static FrameTableLocation[] FrameTable = new FrameTableLocation[256];

        public static uint FreeFrames = 256;

        public static void Instantiate(Driver k)
        {
            singleton = new MMU(k);
        }

        private MMU(Driver k)
        {
            kernel = k;
            for (uint iterator = 0; iterator < 256; iterator++)
            {
                FrameTable[iterator].Free = true;
                FrameTable[iterator].Page = 513;
            }
        }

        public static uint Read(uint address)
        {
            uint data = singleton.kernel.RAM.ReadDataFromMemory(address);
            return data;
        }

        public static void Write(uint address, uint data)
        {
            singleton.kernel.RAM.WriteDataToMemory(address, data);
        }

        public static uint GetFreeFrame(uint page)
        {
            uint firstFreeFrame = (uint)Array.FindIndex<FrameTableLocation>(FrameTable, e => e.Free == true);
            WritePageToFrame(page, firstFreeFrame);
            FrameTable[firstFreeFrame].Free = false;
            return firstFreeFrame;
        }

        static bool IsFree(FrameTableLocation l)
        {
            if (l.Free)
                return true;
            else return false;
        }

        public static void WritePageToFrame(uint page, uint frame)
        {
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                Write(frame * 4 + iterator, singleton.kernel.Disk.ReadDataFromDisk(page * 4 + iterator));
            }
        }

        public static void GrabPageFromAddressToFrame(uint address, uint frame)
        {
            uint page = address / 4;
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                Write(frame * 4 + iterator, singleton.kernel.Disk.ReadDataFromDisk(page * 4 + iterator));
            }
        }

        struct FrameTableLocation
        {
            public uint Page;
            public bool Free;
        }
    }
}
