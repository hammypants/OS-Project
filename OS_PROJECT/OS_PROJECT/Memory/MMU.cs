using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    struct FrameTableLocation
    {
        public uint Page;
        public bool IsFree;
        public uint ProcessID;
    }

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
                FrameTable[iterator].IsFree = true;
                FrameTable[iterator].Page = 0;
                FrameTable[iterator].ProcessID = 0;
            }
        }

        // WRITE A METHOD TO SEARCH FOR THE FRAME THAT HAS A PAGE HERE

        public static uint Read(uint address)
        {
            uint data = singleton.kernel.RAM.ReadDataFromMemory(address);
            return data;
        }

        public static uint[] ReadFrame(uint frame)
        {
            uint[] returnableFrame = new uint[4];
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                returnableFrame[iterator] = singleton.kernel.RAM.ReadDataFromMemory(frame * 4 + iterator);
            }
            return returnableFrame;
        }

        public static void Write(uint address, uint data)
        {
            singleton.kernel.RAM.WriteDataToMemory(address, data);
        }

        public static void WriteFrame(uint[] frameToWrite, uint frame)
        {
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                singleton.kernel.RAM.WriteDataToMemory((frame * 4 + iterator), frameToWrite[iterator]);
            }
        }

        public static uint GetFreeFrame(uint page, uint processID)
        {
            uint firstFreeFrame = (uint)Array.FindIndex<FrameTableLocation>(FrameTable, e => e.IsFree == true);
            WritePageToFrame(page, firstFreeFrame);
            FrameTable[firstFreeFrame].IsFree = false;
            FrameTable[firstFreeFrame].Page = page;
            FrameTable[firstFreeFrame].ProcessID = processID;
            FreeFrames--;
            return firstFreeFrame;
        }

        public static void FreeFrame(uint frame)
        {
            FrameTable[frame].IsFree = true;
            FrameTable[frame].Page = 0;
            FrameTable[frame].ProcessID = 0;
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                singleton.kernel.RAM.WriteDataToMemory(frame * 4 + iterator, 0);
            }
            FreeFrames++;
        }

        public static void WritePageToFrame(uint page, uint frame)
        {
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                Write(frame * 4 + iterator, singleton.kernel.Disk.ReadDataFromDisk(page * 4 + iterator));

            }
        }

        public static void WriteFrameToPage(uint frame, uint page)
        {
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                singleton.kernel.Disk.WriteDataToDisk(page * 4 + iterator, singleton.kernel.RAM.ReadDataFromMemory(frame * 4 + iterator));
            }
        }
    }
}
