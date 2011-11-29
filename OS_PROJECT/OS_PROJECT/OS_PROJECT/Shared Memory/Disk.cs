using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class Disk
    {
        protected uint[][] diskMemory = new uint[512][];

        Object diskLock = new Object();

        public Disk()
        {
            for (uint p = 0; p < diskMemory.GetLength(0); p++)
            {
                diskMemory[p] = new uint[4];
            }
        }

        public void WriteDataToDisk(uint physicalAddress, uint data)
        {
            lock (diskLock)
            {
                try { diskMemory[GetPage(physicalAddress)][GetOffset(physicalAddress)] = data; }
                catch { Console.WriteLine("Could not write to specified disk location. Please check for out of bounds errors."); }
            }
        }

        public uint ReadDataFromDisk(uint physicalAddress)
        {
            lock (diskLock)
            {
                try { return diskMemory[GetPage(physicalAddress)][GetOffset(physicalAddress)]; }
                catch
                {
                    Console.WriteLine("Could not read data from disk. Please check for out of bounds errors.");
                    return 0;
                }
            }
        }

        uint GetPage(uint physicalAddress)
        {
            uint page = physicalAddress / 4;
            return page;
        }

        uint GetOffset(uint physicalAddress)
        {
            uint offset = physicalAddress % 4;
            return offset;
        }

        public int GetDiskSize()
        {
            return diskMemory.GetLength(0);
        }
    }
}
