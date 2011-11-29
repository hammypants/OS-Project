using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class Disk
    {
        protected uint[] diskMemory = new uint[2048];

        public Disk()
        { }

        public void WriteDataToDisk(uint physicalAddress, uint data)
        {
            try { diskMemory[physicalAddress] = data; }
            catch { Console.WriteLine("Could not write to specified disk location. Please check for out of bounds errors."); }
        }

        public uint ReadDataFromDisk(uint physicalAddress)
        {
            try { return diskMemory[physicalAddress]; }
            catch { Console.WriteLine("Could not read data from disk. Please check for out of bounds errors.");
                return 0; }
        }

        public int GetDiskSize()
        {
            return diskMemory.GetLength(0);
        }
    }
}
