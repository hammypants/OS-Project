using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class RAM
    {
        protected uint[][] RAM_Memory = new uint[256][];

        volatile Object rqLock = new Object();

        public RAM()
        {
            for (uint f = 0; f < RAM_Memory.GetLength(0); f++)
            {
                RAM_Memory[f] = new uint[4];
            }
        }

        public void WriteDataToMemory(uint physicalAddress, uint data)
        {
            lock (rqLock)
            {
                try { RAM_Memory[GetFrame(physicalAddress)][GetOffset(physicalAddress)] = data; }
                catch { Console.WriteLine("Could not write to specified memory location. Please check for out of bounds errors."); }
            }
        }

        public uint ReadDataFromMemory(uint physicalAddress)
        {
            lock (rqLock)
            {
                try { return RAM_Memory[GetFrame(physicalAddress)][GetOffset(physicalAddress)]; }
                catch
                {
                    Console.WriteLine("Could not read data from memory. Please check for out of bounds errors.");
                    return 0;
                }
            }
        }

        uint GetFrame(uint physicalAddress)
        {
            uint frame = physicalAddress / 4;
            return frame;
        }

        uint GetOffset(uint physicalAddress)
        {
            uint offset = physicalAddress % 4;
            return offset;
        }

        public int GetMemorySize()
        {
            return RAM_Memory.GetLength(0);
        }
    }
}
